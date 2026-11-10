import adapter from '@sveltejs/adapter-static';
import type { Adapter } from '@sveltejs/kit';
import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';

const output = '../Taurus.Server/wwwroot';
const indexFile = output + '/index.html';
const absoluteAssets = '/_app/';
const relativeAssets = './_app/';
const scriptPattern = /<script>([\s\S]*?)<\/script>/g;
const hashAlgorithm = 'sha256';
const hashEncoding = 'base64';

function hash(script: string): string {
    return hashAlgorithm + '-'
        + createHash(hashAlgorithm).update(script).digest(hashEncoding);
}

export function portableAdapter(): Adapter {
    const standard = adapter({ pages: output, assets: output });

    return {
        name: 'taurus-portable-static',
        async adapt(builder) {
            await standard.adapt(builder);

            const original = await readFile(indexFile, 'utf8');
            let portable = original.replaceAll(absoluteAssets, relativeAssets);

            for (const match of original.matchAll(scriptPattern)) {
                const script = match[1];
                const replacement = script.replaceAll(absoluteAssets, relativeAssets);

                portable = portable.replaceAll(hash(script), hash(replacement));
            }

            await writeFile(indexFile, portable);
        }
    };
}
