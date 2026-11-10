const csrfHeader = 'x-taurus-csrf';
const saltHeader = 'x-taurus-csrf-salt';
const proofBytes = 32;
const hashAlgorithm = 'SHA-256';

function base64(bytes: Uint8Array): string {
    return btoa(String.fromCharCode(...bytes));
}

export async function csrfHeaders(seed: string): Promise<Record<string, string>> {
    const salt = crypto.getRandomValues(new Uint8Array(proofBytes));
    const data = new Uint8Array(proofBytes + proofBytes);
    const decoded = Uint8Array.from(atob(seed), character => character.charCodeAt(0));

    if (decoded.length !== proofBytes)
        throw new Error('Invalid browser security context');

    data.set(decoded);
    data.set(salt, proofBytes);

    const proof = await crypto.subtle.digest(hashAlgorithm, data);

    return {
        [csrfHeader]: base64(new Uint8Array(proof)),
        [saltHeader]: base64(salt)
    };
}
