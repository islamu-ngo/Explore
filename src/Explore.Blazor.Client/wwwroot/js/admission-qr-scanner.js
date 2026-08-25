// ABOUTME: Provides a minimal native BarcodeDetector gate for caller-owned image sources.
// ABOUTME: Returns transient typed data without DOM, storage, network, logging, or URL side effects.

async function hasNativeQrSupport() {
    if (!globalThis.isSecureContext || typeof globalThis.BarcodeDetector !== "function") {
        return false;
    }

    const formats = await globalThis.BarcodeDetector.getSupportedFormats();
    return Array.isArray(formats) && formats.includes("qr_code");
}

export async function getCapability() {
    try {
        return { status: await hasNativeQrSupport() ? "supported" : "unsupported" };
    } catch {
        return { status: "unsupported" };
    }
}

export async function detect(imageSource) {
    try {
        if (!imageSource || !await hasNativeQrSupport()) {
            return { status: "unsupported" };
        }

        const detector = new globalThis.BarcodeDetector({ formats: ["qr_code"] });
        const results = await detector.detect(imageSource);
        if (!Array.isArray(results) || results.length === 0) {
            return { status: "noCode" };
        }
        if (results.length !== 1) {
            return { status: "multiple" };
        }

        return { status: "single", value: typeof results[0].rawValue === "string" ? results[0].rawValue : "" };
    } catch {
        return { status: "failure" };
    }
}
