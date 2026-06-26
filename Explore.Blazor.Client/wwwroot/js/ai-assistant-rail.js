// ABOUTME: Keyboard helpers for the shell AI assistant rail prompt composer.
// ABOUTME: Keeps Enter-to-send behavior precise while preserving native Shift+Enter newlines.

const promptHandlers = new WeakMap();

export function attachPromptKeyboardHandler(element, dotNetReference) {
    if (!element) {
        return;
    }

    detachPromptKeyboardHandler(element);

    const handler = event => {
        const hasModifier = event.shiftKey || event.altKey || event.ctrlKey || event.metaKey;
        const canSelectReference = element.dataset.referenceAutocompleteSelectable === 'true';

        if (canSelectReference && !hasModifier && event.key === 'Tab') {
            event.preventDefault();
            dotNetReference.invokeMethodAsync('SelectPromptReferenceFromKeyboardAsync');
            return;
        }

        if (canSelectReference && !hasModifier && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
            event.preventDefault();
            dotNetReference.invokeMethodAsync(
                'MovePromptReferenceSelectionAsync',
                event.key === 'ArrowDown' ? 1 : -1);
            return;
        }

        if (event.key !== 'Enter' || hasModifier) {
            return;
        }

        event.preventDefault();
        dotNetReference.invokeMethodAsync('SendPromptFromKeyboardAsync');
    };

    element.addEventListener('keydown', handler);
    promptHandlers.set(element, handler);
}

export function focusPrompt(element) {
    if (!element) {
        return;
    }

    element.focus();
}

export function detachPromptKeyboardHandler(element) {
    const handler = promptHandlers.get(element);
    if (!handler) {
        return;
    }

    element.removeEventListener('keydown', handler);
    promptHandlers.delete(element);
}
