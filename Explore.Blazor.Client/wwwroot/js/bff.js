// BFF (Backend-For-Frontend) JavaScript utilities for Blazor
// Used by BffClient.cs for CSRF token extraction and auth status checks

/**
 * Read a cookie value by name from document.cookie.
 * Used by BffClient to read the XSRF-TOKEN cookie for CSRF protection on mutations.
 * @param {string} name - The cookie name to read
 * @returns {string|null} The cookie value, or null if not found
 */
export function getCookie(name) {
    const match = document.cookie.match(new RegExp('(^|;\\s*)' + name + '=([^;]*)'));
    return match ? decodeURIComponent(match[2]) : null;
}

/**
 * Check authentication status by calling the server's /auth/status endpoint.
 * @returns {Promise<{isAuthenticated: boolean, name: string|null}>}
 */
export async function checkAuthStatus() {
    try {
        const response = await fetch('/auth/status', {
            method: 'GET',
            credentials: 'same-origin'
        });

        if (response.ok && response.status === 200) {
            const authInfo = await response.json();
            return {
                isAuthenticated: authInfo.isAuthenticated || false,
                name: authInfo.name || null
            };
        }

        return { isAuthenticated: false, name: null };

    } catch (error) {
        console.log('Auth status check failed:', error);
        return { isAuthenticated: false, name: null };
    }
}

// ── Setup secret BFF helpers ─────────────────────────────────────────
// These use browser fetch so cookies (setup-secret, XSRF-TOKEN) flow correctly
// regardless of Blazor render mode (InteractiveServer, Auto, or WebAssembly).

/**
 * Persist setup secret via the BFF. The server sets an HttpOnly cookie on the response.
 * @param {string} secret - The setup secret to persist
 * @returns {Promise<{ok: boolean, status: number, error: string|null}>}
 */
export async function persistSetupSecret(secret) {
    return await _bffPost('/bff/setup-secret', { secret });
}

/**
 * Sync setup secret for an authenticated session via the BFF.
 * @param {string|null} secret - Optional secret; server falls back to cookie/session if omitted
 * @returns {Promise<{ok: boolean, status: number, error: string|null}>}
 */
export async function syncSetupSecret(secret) {
    return await _bffPost('/bff/setup-secret/sync', { secret: secret || '' });
}

/**
 * Delete the persisted setup secret via the BFF.
 * @returns {Promise<{ok: boolean, status: number, error: string|null}>}
 */
export async function deleteSetupSecret() {
    return await _bffMutate('DELETE', '/bff/setup-secret');
}

/**
 * Get the current setup secret status (persisted? valid?).
 * @returns {Promise<{hasPersistedSecret: boolean, isValid: boolean, error: string|null}>}
 */
export async function getSetupSecretStatus() {
    try {
        const response = await fetch('/bff/setup-secret', {
            method: 'GET',
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        });

        if (response.ok) {
            return await response.json();
        }

        return { hasPersistedSecret: false, isValid: false, error: 'Status check failed.' };
    } catch (error) {
        console.log('Setup secret status check failed:', error);
        return { hasPersistedSecret: false, isValid: false, error: error.message };
    }
}

/** @private Shared POST helper for BFF setup-secret mutations. */
async function _bffPost(url, body) {
    return await _bffMutate('POST', url, body);
}

/**
 * Simple GET + JSON parse helper for browser-side fetch.
 * @param {string} url - The URL to fetch
 * @returns {Promise<any>} The parsed JSON response
 */
export async function fetchJson(url) {
    const response = await fetch(url, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
    });
    if (!response.ok) {
        throw new Error('Fetch failed: ' + response.status);
    }
    return await response.json();
}

/**
 * Sends a same-origin JSON command and normalizes API responses into the
 * InstanceCommandResponseModel shape used by onboarding pages.
 * @param {string} method - HTTP method
 * @param {string} url - The URL to call
 * @param {any} body - Optional JSON body
 * @returns {Promise<{success: boolean, id: string, message: string, errors: string[]}>}
 */
export async function sendCommand(method, url, body) {
    try {
        const headers = {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        };

        const xsrf = getCookie('XSRF-TOKEN');
        if (xsrf) {
            headers['X-CSRF-TOKEN'] = xsrf;
        }

        const response = await fetch(url, {
            method,
            credentials: 'same-origin',
            headers,
            body: body === undefined ? undefined : JSON.stringify(body)
        });

        const text = await response.text();
        const payload = text ? tryParseJson(text) : null;

        if (payload && typeof payload === 'object' && payload.success !== undefined) {
            return {
                success: payload.success === true,
                id: typeof payload.id === 'string' ? payload.id : '00000000-0000-0000-0000-000000000000',
                message: typeof payload.message === 'string'
                    ? payload.message
                    : (response.ok ? 'OK' : `Failed with status ${response.status}.`),
                errors: Array.isArray(payload.errors)
                    ? payload.errors.filter(error => typeof error === 'string')
                    : []
            };
        }

        if (payload && typeof payload === 'object' && payload.title !== undefined) {
            return {
                success: false,
                id: '00000000-0000-0000-0000-000000000000',
                message: typeof payload.title === 'string' ? payload.title : 'Validation failed.',
                errors: flattenProblemDetailsErrors(payload.errors)
            };
        }

        return {
            success: response.ok,
            id: '00000000-0000-0000-0000-000000000000',
            message: response.ok
                ? 'OK'
                : (payload?.detail || payload?.title || `Failed with status ${response.status}.`),
            errors: []
        };
    } catch (error) {
        return {
            success: false,
            id: '00000000-0000-0000-0000-000000000000',
            message: 'Request failed.',
            errors: [error instanceof Error ? error.message : String(error)]
        };
    }
}

/** @private Shared mutation helper. Reads XSRF token from cookie if present. */
async function _bffMutate(method, url, body) {
    try {
        const headers = { 'Accept': 'application/json' };
        if (body) {
            headers['Content-Type'] = 'application/json';
        }

        const xsrf = getCookie('XSRF-TOKEN');
        if (xsrf) {
            headers['X-CSRF-TOKEN'] = xsrf;
        }

        const response = await fetch(url, {
            method,
            credentials: 'same-origin',
            headers,
            body: body ? JSON.stringify(body) : undefined
        });

        let error = null;
        if (!response.ok) {
            try {
                const problem = await response.json();
                error = problem.detail || problem.title || 'Request failed.';
            } catch {
                error = 'Request failed with status ' + response.status;
            }
        }

        return { ok: response.ok, status: response.status, error };
    } catch (error) {
        console.log('BFF mutation failed:', error);
        return { ok: false, status: 0, error: error.message };
    }
}

function tryParseJson(text) {
    try {
        return JSON.parse(text);
    } catch {
        return null;
    }
}

function flattenProblemDetailsErrors(errors) {
    if (!errors || typeof errors !== 'object') {
        return [];
    }

    return Object.values(errors)
        .flatMap(value => Array.isArray(value) ? value : [])
        .filter(value => typeof value === 'string');
}
