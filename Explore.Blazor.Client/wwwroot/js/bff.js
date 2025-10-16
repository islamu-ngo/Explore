export function withCsrf(init) {
    const token = document.cookie
        .split("; ")
        .find((c) => c.startsWith("XSRF-TOKEN="))
        ?.split("=")[1];
    return {
        ...(init || {}),
        headers: { ...(init?.headers || {}), "X-CSRF-TOKEN": token },
        credentials: "include",
    };
}