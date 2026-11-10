// Connect to a separately launched Chrome instance with remote debugging enabled.
const origin = process.env.TAURUS_TEST_ORIGIN;
const prefix = process.env.TAURUS_TEST_PREFIX ?? '/_taurus';
const standalone = process.env.TAURUS_TEST_STANDALONE === 'true';
const redirects = process.env.TAURUS_TEST_LOGIN_REDIRECT !== 'false';
const username = process.env.TAURUS_TEST_USERNAME ?? 'root';
const password = process.env.TAURUS_TEST_PASSWORD;
const debuggerTimeoutMilliseconds = 5000;

if (!origin || !password)
    throw new Error('Set TAURUS_TEST_ORIGIN and TAURUS_TEST_PASSWORD');

const pages = await (await fetch('http://127.0.0.1:9222/json')).json();
const ws = new WebSocket(pages[0].webSocketDebuggerUrl);
await new Promise(resolve => ws.addEventListener('open', resolve));
let nextId = 0;
const pending = new Map<number, (message: any) => void>();
const errors: string[] = [];
ws.addEventListener('message', event => {
    const message = JSON.parse(event.data);
    if (message.id) {
        pending.get(message.id)?.(message);
        pending.delete(message.id);
    }
    if (message.method === 'Runtime.exceptionThrown')
        errors.push(message.params.exceptionDetails.text);
    if (message.method === 'Log.entryAdded'
        && message.params.entry.level === 'error')
        errors.push(message.params.entry.text);
});
function send(method: string, params = {}): Promise<any> {
    const id = ++nextId;
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => {
            pending.delete(id);
            reject(new Error('Chrome debugger request timed out'));
        }, debuggerTimeoutMilliseconds);

        pending.set(id, message => {
            clearTimeout(timeout);

            if (message.error)
                reject(new Error('Chrome debugger request failed'));
            else
                resolve(message);
        });
        ws.send(JSON.stringify({ id, method, params }));
    });
}
async function evaluate(expression: string) {
    const result = await send('Runtime.evaluate', {
        expression, returnByValue: true, awaitPromise: true
    });
    if (result.result?.exceptionDetails)
        throw new Error('Browser evaluation failed');
    return result.result?.result?.value;
}
async function wait(expression: string) {
    for (let i = 0; i < 150; i++) {
        if (await evaluate(expression))
            return;
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    throw new Error('Timed out waiting for browser state');
}
await send('Runtime.enable');
await send('Log.clear');
await send('Log.enable');
await send('Network.enable');
await send('Network.clearBrowserCookies');
await send('Network.setExtraHTTPHeaders', { headers: {
    'X-Taurus-User': 'forged',
    'X-Taurus-User-Id': 'forged',
    'X-Taurus-Tenant': 'forged'
} });
await send('Network.setCookie', {
    name: 'application', value: 'test', url: origin + '/', secure: true
});
await send('Page.navigate', {
    url: origin + (redirects ? '/' : prefix + '/ui/')
});
await wait("document.querySelector('button[type=submit]')?.disabled === false");
const credentials = JSON.stringify({ username, password });

await evaluate(`
    const credentials = ${credentials};
    document.querySelector('#username').value = credentials.username;
    document.querySelector('#username').dispatchEvent(new Event('input', {bubbles:true}));
    document.querySelector('#password').value = credentials.password;
    document.querySelector('#password').dispatchEvent(new Event('input', {bubbles:true}));
    document.querySelector('form').requestSubmit();
`);
if (standalone) {
    await wait("document.body?.innerText.includes('You are signed in')");
} else {
    await wait("document.body?.innerText.includes('user=')"
        + " && !document.querySelector('form')");
}

console.log('Browser login completed');
const cookies = (await send('Network.getAllCookies')).result.cookies;
const expectedCookie = origin.startsWith('https:')
    ? '__Host-TaurusSession' : 'TaurusDevelopmentSession';
const session = cookies.find(cookie => cookie.name === expectedCookie);
if (!session?.httpOnly || session.path !== '/'
    || session.secure !== origin.startsWith('https:')
    || session.sameSite !== 'Strict')
    throw new Error('Session cookie flags invalid');
if (!standalone) {
    const body = await evaluate('document.body.innerText');

    if (body.includes('__Host-Taurus') || body.includes('application=test'))
        throw new Error('Secret or unapproved cookies reached application');

    if (!body.includes('user=' + encodeURIComponent(username))
        || body.includes('forged'))
        throw new Error('Identity headers were not replaced');
}
await send('Page.navigate', { url: origin + prefix + '/ui/' });
await wait("document.body?.innerText.includes('You are signed in')");
await evaluate("document.querySelector('button').click()");
await wait("document.querySelector('button[type=submit]')?.disabled === false");
await send('Page.navigate', {
    url: origin + (redirects ? '/' : prefix + '/ui/')
});
await wait("document.querySelector('button[type=submit]')?.disabled === false");
if (errors.length)
    throw new Error(JSON.stringify(errors));
console.log('Logout returned to login; no browser errors');
ws.close();
