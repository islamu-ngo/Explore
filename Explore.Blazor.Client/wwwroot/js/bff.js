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
