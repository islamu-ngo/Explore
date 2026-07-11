// ABOUTME: localStorage adapter for schema-versioned dock layout snapshots.
// ABOUTME: Keeps storage key construction out of Blazor components and behind IDockLayoutPersistence.

const PREFIX = 'dock_layout:v1:';

export function get(layoutKey) {
    return localStorage.getItem(PREFIX + layoutKey);
}

export function set(layoutKey, value) {
    localStorage.setItem(PREFIX + layoutKey, value);
}

export function remove(layoutKey) {
    localStorage.removeItem(PREFIX + layoutKey);
}
