// =====================================================================================
// posShortcuts.js — F1–F7 keyboard shortcuts for the POS screen (Pos.razor only).
// -------------------------------------------------------------------------------------
// A single window `keydown` listener (bubble phase) that intercepts ONLY the mapped
// function keys and forwards them to Blazor via [JSInvokable] Pos.HandleShortcut.
//
// Why this cannot collide with the barcode-scan interceptors:
//   * scaleBarcode.js listens in CAPTURE phase on window and only swallows fast bursts
//     of SINGLE-character keys (e.key.length === 1). F-keys (e.key.length > 1) make it
//     reset its buffer and return without preventDefault — so our bubble-phase listener
//     still sees them and can suppress the browser default (F1 help, F5 refresh).
//   * purchaseScan.js only acts on the purchase receiving input, and is not mounted on
//     the POS page.
//   * We only ever touch e.key values in SHORTCUT_KEYS, never characters/Enter, so
//     manual typing and scanner bursts pass through untouched.
// =====================================================================================

// Single lookup table — to add F8..F12 later, just append to this Set (and map the key
// to its action in Pos.razor's _shortcuts dictionary). Nothing else changes.
const SHORTCUT_KEYS = new Set(['F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7']);

let dotNet = null;
let attached = false;

function onKeyDown(e) {
    if (!SHORTCUT_KEYS.has(e.key)) return; // scanner bursts / typing / everything else untouched
    // Suppress browser defaults for these specific keys only (F1 help, F5 refresh, ...).
    e.preventDefault();
    if (dotNet) {
        try {
            // Fire-and-forget: Blazor decides whether the action may run (it mirrors the
            // on-screen button's disabled state), so no guard is bypassed.
            dotNet.invokeMethodAsync('HandleShortcut', e.key);
        } catch (err) {
            console.error('[posShortcuts] invoke error:', err);
        }
    }
}

export function attach(dotNetRef) {
    dotNet = dotNetRef;
    if (!attached) {
        window.addEventListener('keydown', onKeyDown);
        attached = true;
    }
}

export function detach() {
    if (attached) {
        window.removeEventListener('keydown', onKeyDown);
        attached = false;
    }
    dotNet = null;
}
