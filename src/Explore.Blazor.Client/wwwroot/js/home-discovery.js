// ABOUTME: One-shot browser geolocation adapter used only after an explicit home discovery action.
// ABOUTME: Requests coarse accuracy and returns coordinates without writing to browser or external side channels.

export function getCurrentPosition() {
    if (!navigator.geolocation) {
        return Promise.resolve({ status: "unavailable" });
    }

    return new Promise((resolve) => {
        navigator.geolocation.getCurrentPosition(
            (position) => resolve({
                status: "available",
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            }),
            (error) => resolve({
                status: error.code === error.PERMISSION_DENIED ? "denied" : "unavailable"
            }),
            {
                enableHighAccuracy: false,
                timeout: 10000,
                maximumAge: 300000
            });
    });
}
