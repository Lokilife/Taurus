import assert from 'node:assert/strict';
import { request as httpRequest } from 'node:http';
import { request as httpsRequest } from 'node:https';
import { createRequire } from 'node:module';
import { csrfHeaders } from '../src/Taurus.WebClient/src/lib/security.ts';

const require = createRequire(import.meta.url);
const opaque = require('../src/Taurus.WebClient/node_modules/@serenity-kit/opaque');
const contractPath = '../src/Taurus.WebClient/src/lib/generated/authentication_pb.js';
const contracts = require(contractPath);
const origin = process.env.TAURUS_TEST_ORIGIN;
const password = process.env.TAURUS_TEST_PASSWORD;
const username = process.env.TAURUS_TEST_USERNAME ?? 'root';
const prefix = process.env.TAURUS_TEST_PREFIX ?? '/_taurus';
const service = '/taurus.v1.Authentication/';
const frameHeaderBytes = 5;
const trailerBit = 128;
const unknownField = new Uint8Array([120, 1]);
const cookies = new Map<string, string>();

if (!origin || !password)
    throw new Error('Set TAURUS_TEST_ORIGIN and TAURUS_TEST_PASSWORD');

async function rpc(
    method: string,
    payload = new Uint8Array(),
    extra: Record<string, string> = {}
): Promise<{ status: string; payload: Uint8Array }> {
    const frame = new Uint8Array(frameHeaderBytes + payload.length);

    new DataView(frame.buffer).setUint32(1, payload.length);
    frame.set(payload, frameHeaderBytes);

    const response = await fetch(origin + prefix + service + method, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/grpc-web+proto',
            'x-grpc-web': '1',
            Origin: origin!,
            Cookie: [...cookies.values()].join('; '),
            ...extra
        },
        body: frame
    });

    for (const cookie of response.headers.getSetCookie()) {
        const pair = cookie.split(';', 1)[0];
        const name = pair.slice(0, pair.indexOf('='));

        cookies.set(name, pair);
    }

    const body = new Uint8Array(await response.arrayBuffer());
    let status = response.headers.get('grpc-status') ?? '';
    let result = new Uint8Array();
    let position = 0;

    while (position + frameHeaderBytes <= body.length) {
        const kind = body[position];
        const size = new DataView(body.buffer).getUint32(position + 1);
        const start = position + frameHeaderBytes;
        const frame = body.slice(start, start + size);

        assert.ok(start + size <= body.length, 'Truncated response');

        if (kind & trailerBit) {
            const trailers = new TextDecoder().decode(frame);

            status = /grpc-status:\s*(\d+)/.exec(trailers)?.[1] ?? status;
        } else {
            result = frame;
        }

        position = start + size;
    }

    return { status, payload: result };
}

const ui = await fetch(origin + prefix + '/ui/', { redirect: 'manual' });

assert.equal(ui.status, 200, 'Login page redirects or is unavailable');

const html = await ui.text();
const asset = /href="(\.\/_app\/[^" ]+)"/.exec(html)?.[1];

assert.ok(asset, 'Login page lacks portable assets');
assert.ok(!html.includes('style="'), 'Inline styles violate CSP');

const javascript = await fetch(new URL(asset, origin + prefix + '/ui/'));

assert.equal(javascript.status, 200, 'Login page asset missing');

const canonical = await fetch(origin + prefix + '/ui', { redirect: 'manual' });

assert.equal(canonical.status, 302, 'Missing trailing slash was not normalized');
assert.equal(new URL(canonical.headers.get('location')!, origin).href,
    origin + prefix + '/ui/');

await opaque.ready;

async function login(): Promise<string> {
    const start = opaque.client.startLogin({ password });
    const request = new contracts.BeginLoginRequest();

    request.setUsername(username);
    request.setRequest(start.startLoginRequest);

    const serialized = request.serializeBinary();
    const undocumented = new Uint8Array(serialized.length + unknownField.length);

    undocumented.set(serialized);
    undocumented.set(unknownField, serialized.length);

    assert.equal((await rpc('BeginLogin', undocumented)).status, '3',
        'Unknown fields in OPAQUE start were accepted');

    const begun = await rpc('BeginLogin', serialized);

    assert.equal(begun.status, '0', 'OPAQUE begin failed');

    const challenge = contracts.BeginLoginResponse.deserializeBinary(begun.payload);
    const proof = opaque.client.finishLogin({
        password,
        clientLoginState: start.clientLoginState,
        loginResponse: challenge.getResponse(),
        keyStretching: 'memory-constrained'
    });

    assert.ok(proof, 'OPAQUE client rejected authentication');

    const finish = new contracts.LoginRequest();

    finish.setUsername(username);
    finish.setRequest(proof.finishLoginRequest);

    assert.equal((await rpc('Login', finish.serializeBinary())).status, '7',
        'Missing CSRF was accepted');

    const headers = await csrfHeaders(challenge.getCsrfSeed());
    const result = await rpc('Login', finish.serializeBinary(), headers);

    assert.equal(result.status, '0', 'OPAQUE finish failed');
    assert.notEqual((await rpc('Login', finish.serializeBinary(), headers)).status, '0',
        'Consumed challenge was accepted');

    const session = contracts.SessionResponse.deserializeBinary(result.payload);

    assert.deepEqual(session.serializeBinary(), result.payload,
        'Session response contains undocumented wire fields');

    assert.ok(session.getSessionId(), 'Public session identifier missing');

    return session.getCsrfSeed();
}

const seed = await login();

const current = await rpc('CurrentSession');

assert.equal(current.status, '0');

const identity = contracts.SessionResponse.deserializeBinary(current.payload);
const cookieName = origin.startsWith('https:')
    ? '__Host-TaurusSession' : 'TaurusDevelopmentSession';
const originalCookie = cookies.get(cookieName)!;

cookies.set(cookieName, cookieName + '=' + identity.getSessionId());

assert.equal((await rpc('CurrentSession')).status, '16',
    'Public session identifier was accepted as a bearer token');

cookies.set(cookieName, originalCookie);
assert.equal((await rpc('CurrentSession', unknownField)).status, '3',
    'Unknown field accepted on actual wire path');

const sessionCookies = new Map(cookies);

assert.equal((await rpc('Logout')).status, '7', 'Missing logout proof accepted');
assert.equal((await rpc('Logout', undefined, await csrfHeaders(seed))).status, '0');

cookies.clear();

for (const [name, value] of sessionCookies)
    cookies.set(name, value);

assert.equal((await rpc('CurrentSession')).status, '16', 'Revoked token accepted');
console.log('OPAQUE, CSRF, wire rejection and session revocation checks passed');

await login();

const navigation = {
    Cookie: [...cookies.values()].join('; '),
    'Sec-Fetch-Site': 'same-origin',
    'Sec-Fetch-Mode': 'navigate',
    'Sec-Fetch-Dest': 'document',
    'Sec-Fetch-User': '?1'
};
function navigate(site: string): Promise<{
    status: number; location: string; cookies: string[]
}> {
    return new Promise((resolve, reject) => {
        const request = origin!.startsWith('https:') ? httpsRequest : httpRequest;
        const outgoing = request(origin + prefix + '/logout', {
            headers: { ...navigation, 'Sec-Fetch-Site': site }
        }, response => {
            response.resume();
            response.on('end', () => resolve({
                status: response.statusCode!,
                location: response.headers.location ?? '',
                cookies: response.headers['set-cookie'] ?? []
            }));
        });

        outgoing.on('error', reject);
        outgoing.end();
    });
}

const rejected = await navigate('cross-site');

assert.equal(rejected.status, 403, 'Cross-site navigation revoked the session');
assert.equal((await rpc('CurrentSession')).status, '0');

const loggedOut = await navigate('same-origin');

assert.equal(loggedOut.status, 302);
assert.equal(new URL(loggedOut.location, origin).href, origin + prefix + '/ui/');
assert.ok(loggedOut.cookies.some(cookie => cookie.startsWith(cookieName + '=;')),
    'Session cookie was not cleared');
assert.equal((await rpc('CurrentSession')).status, '16',
    'Visiting the logout endpoint did not revoke the saved session cookie');
console.log('Browser logout navigation and persistent revocation checks passed');
