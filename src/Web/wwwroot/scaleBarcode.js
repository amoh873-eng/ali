// =====================================================================================
// scaleBarcode.js — Non-invasive global scale/scanner barcode interceptor for the POS.
// -------------------------------------------------------------------------------------
// Loaded as an ES module (see ScaleBarcodeInterceptor.razor -> import("./scaleBarcode.js")).
// A single global `keydown` listener (capture phase) watches for the ultra-fast
// keystroke bursts that hardware barcode scanners emit (typically 5-30 ms apart).
// Human typing is slower than `scanSpeedMs`, so it is never swallowed.
//
// When a complete EAN-13 (13-digit) code starting with a configured prefix (e.g. 2x)
// arrives followed by Enter, it validates the GS1 check digit and parses:
//    digits 1-2   -> scale prefix
//    digits 3-7   -> product SKU / item code
//    digits 8-12  -> weight or total price (raw integer)
//    digit  13    -> check digit
//    value        = rawDigits / Divisor   (e.g. 01500 / 1000 = 1.500 kg)
//
// The structured payload is handed to the Blazor component via [JSInvokable]
// ReceiveScaleBarcode, which re-injects a canonical barcode into the existing POS
// search field and simulates Enter — so the EXISTING Pos.razor search pipeline
// (BarcodeParser + HandleSearchKeyDown) does the work. No DB schema / page / backend
// logic changes are required.
// =====================================================================================

const state = window.__erpScaleBarcodeState || (window.__erpScaleBarcodeState = {
    attached: false,
    dotNet: null,            // DotNetObjectReference for [JSInvokable] calls
    config: {
        prefixes: ['21', '22'],
        mode: 'weight',
        divisor: 1000,
        scanSpeedMs: 80,
        searchSelector: '.pos-search input'
    },
    enabled: false,
    buffer: '',
    lastMs: 0
});

// EAN-13 / GS1-13 check-digit validation (modulo-10 weighting 1,3,1,3,...)
function isValidEan13(raw) {
    if (typeof raw !== 'string' || raw.length !== 13 || !/^\d{13}$/.test(raw)) return false;
    let sum = 0;
    for (let i = 0; i < 12; i++) {
        const digit = Number(raw[i]);
        sum += (i % 2 === 0) ? digit : digit * 3;
    }
    const expected = (10 - (sum % 10)) % 10;
    return Number(raw[12]) === expected;
}

// Parse an EAN-13 scale barcode into a structured payload.
function tryParse(raw) {
    if (!isValidEan13(raw)) return null;
    const prefix = raw.slice(0, 2);
    if (!state.config.prefixes.includes(prefix)) return null;

    const sku = raw.slice(2, 7);              // digits 3-7
    const rawDigits = raw.slice(7, 12);       // digits 8-12
    const divisor = state.config.divisor > 0 ? state.config.divisor : 1000;
    const value = Number.parseInt(rawDigits, 10) / divisor;

    return {
        sku: sku,
        value: value,
        mode: state.config.mode || 'weight',
        raw: raw
    };
}

// Global keydown handler (capture phase so we see the events first).
function onKeyDown(e) {
    if (!state.enabled) return;

    // Ignore IME composition.
    if (e.isComposing || e.keyCode === 229) return;

    // Enter (scanner terminator) — finalize the accumulated buffer.
    if (e.key === 'Enter' || e.key === 'NumpadEnter' || e.key === 'Return') {
        const code = state.buffer;
        state.buffer = '';
        state.lastMs = 0;
        if (!code) return;

        const parsed = tryParse(code);
        if (parsed) {
            // A valid scale barcode: swallow the Enter and route to Blazor.
            e.preventDefault();
            e.stopPropagation();
            void routeToDotNet(parsed);
        }
        // Otherwise let the Enter go through normally (user is typing text).
        return;
    }

    // Only interested in plain digits here.
    if (e.key.length !== 1 || e.key < '0' || e.key > '9') {
        // Any other printable key breaks a scanner burst.
        if (e.key.length === 1) { state.buffer = ''; }
        return;
    }

    const now = Date.now();
    const gap = state.lastMs === 0 ? now : now - state.lastMs;

    if (gap > state.config.scanSpeedMs) {
        // Too slow to be a scanner -> human typing. Do NOT swallow; restart buffer.
        state.buffer = e.key;
        state.lastMs = now;
        return;
    }

    // Scanner-speed burst: buffer and swallow the digit so the raw scan never
    // partially lands in the search field (avoids double-processing).
    state.lastMs = now;
    state.buffer += e.key;
    if (state.buffer.length > 13) state.buffer = state.buffer.slice(-13);
    e.preventDefault();
    e.stopPropagation();
}

async function routeToDotNet(payload) {
    if (!state.dotNet) return;
    try {
        await state.dotNet.invokeMethodAsync('ReceiveScaleBarcode', payload);
    } catch (err) {
        // Never break the POS flow on interceptor errors — the code can still be
        // typed manually into the search box.
        console.error('[scaleBarcode] interceptor error:', err);
    }
}

// __PART2__
// Fill the existing POS search input with a barcode and simulate Enter so the
// existing HandleSearchKeyDown / BarcodeParser pipeline processes it exactly like
// a real scan. Returns true when the field was found and populated.
function injectSearch(code) {
    const selector = state.config.searchSelector || '.pos-search input';
    const input = document.querySelector(selector);
    if (!input) return false;

    // Use the native value setter so frameworks observing input events see the change.
    const valueSetter = Object.getOwnPropertyDescriptor(
        window.HTMLInputElement.prototype, 'value').set;
    if (valueSetter) valueSetter.call(input, code);
    else input.value = code;

    // Notify Blazor's @bind-Value (Immediate) that the value changed.
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));

    // Simulate the Enter keypress the scanner would have sent.
    input.dispatchEvent(new KeyboardEvent('keydown', {
        key: 'Enter', code: 'Enter', bubbles: true, cancelable: true
    }));
    return true;
}

// -------------------------------------------------------------------------------
// Public API — used by ScaleBarcodeInterceptor.razor
// -------------------------------------------------------------------------------
export function init(dotNetRef, config) {
    state.dotNet = dotNetRef;
    if (config) Object.assign(state.config, config);
}

export function setEnabled(flag) {
    state.enabled = !!flag;
    if (!state.enabled) { state.buffer = ''; state.lastMs = 0; }
}

export function getState() {
    return { enabled: state.enabled, prefixes: state.config.prefixes };
}

export function injectBarcode(code) {
    return injectSearch(code);
}

// Attach the global listener exactly once per browser tab.
if (!state.attached) {
    window.addEventListener('keydown', onKeyDown, true);
    state.attached = true;
}