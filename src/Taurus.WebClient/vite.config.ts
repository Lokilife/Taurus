import { portableAdapter } from './build/portableAdapter';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const announcerStyleHash =
    'sha256-S8qMpvofolR8Mpjy4kQvEm7m1q8clzU4dfDH0AmvZjo=';

export default defineConfig({
    plugins: [sveltekit({
        adapter: portableAdapter(),
        paths: { relative: true },
        router: { type: 'hash' },
        csp: {
            mode: 'hash',
            directives: {
                'default-src': ['self'],
                'script-src': ['self', 'wasm-unsafe-eval'],
                'style-src': ['self'],
                'style-src-attr': ['unsafe-hashes', announcerStyleHash],
                'connect-src': ['self'],
                'base-uri': ['none']
            }
        }
    })],
    build: {
        commonjsOptions: { include: [/authentication_pb\.js$/, /node_modules/] }
    }
});
