// ABOUTME: Browser EventSource bridge for authenticated notification refresh hints.
// ABOUTME: Keeps payloads minimal and lets the browser reconnect while polling stays as fallback.

let notificationRefreshSource = null;

export function startNotificationRefresh(url, dotNetRef) {
    stopNotificationRefresh();

    notificationRefreshSource = new EventSource(url, { withCredentials: true });

    notificationRefreshSource.addEventListener('notification-refresh', event => {
        if (!event.data) {
            return;
        }

        const hint = JSON.parse(event.data);
        dotNetRef.invokeMethodAsync(
            'HandleNotificationRefresh',
            hint.unreadCount ?? hint.UnreadCount ?? 0,
            hint.hasUnread ?? hint.HasUnread ?? false,
            hint.reason ?? hint.Reason ?? 'refresh',
            hint.generatedAt ?? hint.GeneratedAt ?? null);
    });

    notificationRefreshSource.onerror = () => {
        dotNetRef.invokeMethodAsync('HandleNotificationRefreshError');
    };
}

export function stopNotificationRefresh() {
    if (notificationRefreshSource !== null) {
        notificationRefreshSource.close();
        notificationRefreshSource = null;
    }
}
