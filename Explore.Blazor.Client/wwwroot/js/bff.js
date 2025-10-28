window.checkAuthStatus = async function() {
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