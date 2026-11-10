<script lang="ts">
    import './page.css';
    import { onMount } from 'svelte';
    import * as opaque from '@serenity-kit/opaque';
    import { csrfHeaders } from '../lib/security';
    import { AuthenticationClient } from '../lib/generated/AuthenticationServiceClientPb';
    import {
        EmptyRequest,
        LoginRequest,
        BeginLoginRequest
    } from '../lib/generated/authentication_pb';

    const uiSuffix = '/ui/';

    let prefix = '';
    let client: AuthenticationClient;
    const defaultDestination = '/';

    let username = $state('');
    let password = $state('');
    let csrf = $state('');
    let signedInAs = $state('');
    let busy = $state(true);
    let message = $state('');

    onMount(async () => {
        try {
            prefix = window.location.pathname.slice(0, -uiSuffix.length);
            client = new AuthenticationClient(prefix, null, null);

            await opaque.ready;

            try {
                const session = await client.currentSession(new EmptyRequest(), {});

                signedInAs = session.getUsername();
                csrf = session.getCsrfSeed();
            } catch {
                signedInAs = '';
            }
        } catch {
            message = 'Taurus is unavailable. Please try again later.';
        } finally {
            busy = false;
        }
    });

    function destination(): string {
        const requested = new URLSearchParams(window.location.search).get('return');

        if (!requested)
            return defaultDestination;

        try {
            const target = new URL(requested, window.location.origin);

            if (target.origin !== window.location.origin)
                return defaultDestination;

            if (target.pathname.startsWith(prefix + uiSuffix))
                return defaultDestination;

            return target.pathname + target.search;
        } catch {
            return defaultDestination;
        }
    }

    async function login(event: SubmitEvent) {
        event.preventDefault();
        busy = true;
        message = '';

        const request = new LoginRequest();

        try {
            const start = opaque.client.startLogin({ password });
            const begin = new BeginLoginRequest();

            begin.setUsername(username);
            begin.setRequest(start.startLoginRequest);

            const challenge = await client.beginLogin(begin, {});
            const proof = opaque.client.finishLogin({
                password,
                clientLoginState: start.clientLoginState,
                loginResponse: challenge.getResponse(),
                keyStretching: 'memory-constrained'
            });

            if (!proof)
                throw new Error('Authentication could not be completed');

            request.setUsername(username);
            request.setRequest(proof.finishLoginRequest);

            const session = await client.login(request,
                await csrfHeaders(challenge.getCsrfSeed()));

            csrf = session.getCsrfSeed();
            signedInAs = session.getUsername();
            window.location.assign(destination());
        } catch {
            message = 'Authentication could not be completed.';
        } finally {
            request.setRequest('');
            password = '';
            busy = false;
        }
    }

    async function logout() {
        busy = true;
        message = '';

        try {
            await client.logout(new EmptyRequest(), await csrfHeaders(csrf));

            signedInAs = '';
            csrf = '';
        } catch {
            message = 'Sign out could not be completed.';
        } finally {
            busy = false;
        }
    }
</script>

<svelte:head>
    <title>Taurus — Sign in</title>
    <meta name="description" content="Sign in securely with Taurus" />
</svelte:head>

<main>
    <section aria-labelledby="heading">
        <p class="brand">TAURUS</p>
        <h1 id="heading">{signedInAs ? 'You are signed in' : 'Welcome back'}</h1>
        <p class="intro">{signedInAs || 'Sign in to continue to your application.'}</p>

        {#if signedInAs}
            <a href={destination()}>Continue to application</a>
            <button onclick={logout} disabled={busy}>Sign out</button>
        {:else}
            <form onsubmit={login}>
                <label for="username">Username</label>
                <input id="username" name="username" autocomplete="username"
                    bind:value={username} maxlength="128" required />
                <label for="password">Password</label>
                <input id="password" name="password" type="password"
                    autocomplete="current-password" bind:value={password}
                    maxlength="1024" required />
                <button type="submit" disabled={busy}>
                    {busy ? 'Please wait…' : 'Sign in'}
                </button>
            </form>
        {/if}

        <p role="status" aria-live="polite">{message}</p>
    </section>
</main>
