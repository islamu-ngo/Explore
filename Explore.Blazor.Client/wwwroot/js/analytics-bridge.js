let state = {
    provider: "none",
    enabled: false,
    consentMode: "pseudonymous",
    transportMode: "direct",
    allowIdentify: false,
    apiKey: "",
    endpointUrl: "",
    ready: false,
    adapter: null,
    sessionId: null
};

function noopAdapter() {
    return {
        identify: async () => {},
        track: async () => {},
        page: async () => {}
    };
}

function loadScript(src, attrs = {}) {
    return new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[data-analytics-src="${src}"]`);
        if (existing) {
            resolve();
            return;
        }

        const script = document.createElement("script");
        script.src = src;
        script.async = true;
        script.dataset.analyticsSrc = src;

        Object.entries(attrs).forEach(([key, value]) => {
            if (value !== undefined && value !== null && value !== "") {
                script.setAttribute(key, String(value));
            }
        });

        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load analytics script: ${src}`));
        document.head.appendChild(script);
    });
}

async function createPostHogAdapter(apiKey, endpointUrl) {
    const host = (endpointUrl && endpointUrl.trim()) || "https://us.i.posthog.com";
    await loadScript(`${host.replace(/\/$/, "")}/static/array.js`);

    if (!window.posthog || typeof window.posthog.init !== "function") {
        return noopAdapter();
    }

    window.posthog.init(apiKey, { api_host: host });

    return {
        identify: async (distinctId, traits) => {
            window.posthog.identify(distinctId, traits || {});
        },
        track: async (eventName, properties) => {
            window.posthog.capture(eventName, properties || {});
        },
        page: async (pagePath, properties) => {
            const payload = Object.assign({}, properties || {}, { $current_url: pagePath });
            window.posthog.capture("$pageview", payload);
        }
    };
}

async function createPlausibleAdapter(apiKey, endpointUrl) {
    const base = (endpointUrl && endpointUrl.trim()) || "https://plausible.io";
    await loadScript(`${base.replace(/\/$/, "")}/js/script.js`, {
        defer: "defer",
        "data-domain": apiKey || "localhost",
        "data-api": `${base.replace(/\/$/, "")}/api/event`
    });

    if (typeof window.plausible !== "function") {
        return noopAdapter();
    }

    return {
        identify: async () => {},
        track: async (eventName, properties) => {
            window.plausible(eventName, { props: properties || {} });
        },
        page: async () => {
            window.plausible("pageview");
        }
    };
}

async function createRybbitAdapter(apiKey, endpointUrl) {
    const host = (endpointUrl && endpointUrl.trim()) || "https://rybbit.com";
    await loadScript(`${host.replace(/\/$/, "")}/api/script.js`, {
        defer: "defer",
        "data-site-id": apiKey || ""
    });

    if (!window.rybbit || typeof window.rybbit.event !== "function") {
        return noopAdapter();
    }

    return {
        identify: async () => {
        },
        track: async (eventName, properties) => {
            window.rybbit.event(eventName, properties || {});
        },
        page: async () => {
            if (typeof window.rybbit.pageview === "function") {
                window.rybbit.pageview();
            }
        }
    };
}

async function createRudderStackAdapter(apiKey, endpointUrl) {
    await loadScript("https://cdn.rudderlabs.com/v1.1/rudder-analytics.min.js");

    if (!window.rudderanalytics || typeof window.rudderanalytics.load !== "function") {
        return noopAdapter();
    }

    window.rudderanalytics.load(apiKey, endpointUrl || "");

    return {
        identify: async (distinctId, traits) => {
            window.rudderanalytics.identify(distinctId, traits || {});
        },
        track: async (eventName, properties) => {
            window.rudderanalytics.track(eventName, properties || {});
        },
        page: async (pagePath, properties) => {
            window.rudderanalytics.page(pagePath, properties || {});
        }
    };
}

async function postRelayEvent(payload) {
    const response = await fetch((state.endpointUrl && state.endpointUrl.trim()) || "/api/a/t", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "include",
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        throw new Error(`Analytics relay failed with status ${response.status}`);
    }
}

function createRelayAdapter() {
    return {
        identify: async () => {},
        track: async (eventName, properties) => {
            await postRelayEvent({
                eventType: "track",
                distinctId: state.sessionId,
                eventName,
                properties: properties || {}
            });
        },
        page: async (pagePath, properties) => {
            await postRelayEvent({
                eventType: "pageview",
                distinctId: state.sessionId,
                pagePath,
                properties: properties || {}
            });
        }
    };
}

export async function initAnalytics(provider, enabled, consentMode, transportMode, allowIdentify, apiKey, endpointUrl) {
    state.provider = (provider || "none").toString().toLowerCase();
    state.enabled = !!enabled;
    state.consentMode = (consentMode || "pseudonymous").toString().toLowerCase();
    state.transportMode = (transportMode || "direct").toString().toLowerCase();
    state.allowIdentify = !!allowIdentify;
    state.apiKey = apiKey || "";
    state.endpointUrl = endpointUrl || "";
    state.adapter = noopAdapter();
    state.ready = false;
    state.sessionId = (globalThis.crypto && typeof globalThis.crypto.randomUUID === "function")
        ? globalThis.crypto.randomUUID()
        : `session-${Date.now()}`;

    if (!state.enabled || state.provider === "none") {
        state.ready = true;
        return;
    }

    if (state.transportMode === "relay") {
        state.adapter = createRelayAdapter();
        state.ready = true;
        return;
    }

    if (!state.apiKey.trim()) {
        state.ready = true;
        return;
    }

    try {
        if (state.provider === "posthog") {
            state.adapter = await createPostHogAdapter(state.apiKey, state.endpointUrl);
        } else if (state.provider === "plausible") {
            state.adapter = await createPlausibleAdapter(state.apiKey, state.endpointUrl);
        } else if (state.provider === "rybbit") {
            state.adapter = await createRybbitAdapter(state.apiKey, state.endpointUrl);
        } else if (state.provider === "rudderstack") {
            state.adapter = await createRudderStackAdapter(state.apiKey, state.endpointUrl);
        }
    } catch {
        state.adapter = noopAdapter();
    }

    state.ready = true;
}

export async function trackEvent(eventName, properties) {
    if (!state.ready || !state.adapter) {
        return;
    }

    await state.adapter.track(eventName, properties || {});
}

export async function identifyUser(distinctId, traits) {
    if (!state.ready || !state.adapter) {
        return;
    }

    if (!state.allowIdentify) {
        return;
    }

    await state.adapter.identify(distinctId, traits || {});
}

export async function trackPageView(pagePath, properties) {
    if (!state.ready || !state.adapter) {
        return;
    }

    await state.adapter.page(pagePath, properties || {});
}
