// =====================================================================================
// purchaseScan.js — Scanner-Burst coalescer for the Purchase-Invoice barcode-receiving field.
// -------------------------------------------------------------------------------------
// Mirrors the POS scale interceptor pattern (scaleBarcode.js): a hardware scanner fires a
// very fast burst of keydowns (typically 20–60ms apart). Without coalescing, EVERY keydown
// (including each Enter) is sent to the Blazor Server circuit, flooding it and dropping
// scans during rapid receiving. This module swallows the burst in the browser and emits a
// SINGLE injected Enter keydown per scan (with the value already committed via input+change),
// so Blazor handles exactly one event per scanned item.
//
// Human typing (gaps > scanSpeedMs) is never swallowed: each key passes through normally.
// =====================================================================================

const KEY = '__erpPurchaseScan';
const state = window[KEY] || (window[KEY] = {
    target: null,
    buf: '',
    swallowed: 0,         // digits silently consumed during the current burst
    lastMs: 0,
    speedMs: 60,          // scanner burst threshold (ms). Adjust if a scanner is faster/slower.
    attached: false
});

// Refresh the target element (the dialog re-renders; re-query each attach() call).
function refreshTarget() {
    state.target = document.querySelector('input[placeholder*="امسح"], input[placeholder*="Scan barcode"]');
}

// تخبر هل العنصر هو حقل المسح (مطابقة placeholder — مستقل عن مرجع العقدة،
// فيصمد أمام إعادة رسم Blazor التي قد تستبدل العقدة أثناء الطفرة)
function isScanTarget(el) {
    if (!el || el.tagName !== 'INPUT') return false;
    const ph = el.getAttribute ? (el.getAttribute('placeholder') || '') : '';
    return ph.indexOf('امسح') >= 0 || ph.indexOf('Scan barcode') >= 0;
}

function onKeyDown(e) {
    // أحداث حقننا أنفسها — دعها تمر مباشرة إلى Blazor دون إعادة التقاط (كسر الحلقة)
    if (e.__erpScanInjected) return;

    // مهما كانت العقدة (حتى القديمة المبدَّلة) — إن كانت حقل المسح نتقبلها
    const el = e.target;
    if (!isScanTarget(el)) return;

    // Ignore IME composition.
    if (e.isComposing || e.keyCode === 229) return;

    // Enter — scanner terminator (or a human pressing Enter after typing slowly).
    if (e.key === 'Enter' || e.key === 'NumpadEnter' || e.key === 'Return') {
        // إذا ابتُلعت أحرف في هذه الطفرة، الثقة في buf (الرمز المجمَّع)؛
        // وإلا (إدخال يدوي/لا امتصاص) استخدم النص الفعلي المعروض في الحقل.
        const wasBurst = state.swallowed > 0;
        const code = wasBurst ? state.buf : (el.value || '').trim();
        state.buf = '';
        state.swallowed = 0;
        state.lastMs = 0;
        if (!code) return; // no burst captured -> normal human Enter, let it flow

        // Commit the coalesced code into the field and emit a SINGLE Enter keydown.
        e.preventDefault();
        e.stopPropagation();
        const valueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        if (valueSetter) valueSetter.call(el, code);
        else el.value = code;
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        const injectedEnter = new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true });
        injectedEnter.__erpScanInjected = true; // لا يُلتقط مرة أخرى
        el.dispatchEvent(injectedEnter);
        return;
    }

    // Any non-single-char key breaks a scanner burst (only plain characters matter).
    if (e.key.length !== 1) { state.buf = ''; state.swallowed = 0; state.lastMs = 0; return; }

    const now = Date.now();
    const gap = state.lastMs === 0 ? now : now - state.lastMs;
    if (gap > state.speedMs) {
        // Too slow to be a scanner burst -> human typing. Capture it as the start of a
        // possible burst but DON'T swallow it (Blazor still sees the keystroke/input).
        state.buf = e.key;
        state.swallowed = 0;
        state.lastMs = now;
        return;
    }

    // Scanner-speed burst: capture this char and swallow it so Blazor never sees it.
    state.buf += e.key;
    state.swallowed += 1;
    state.lastMs = now;
    e.preventDefault();
    e.stopPropagation();
}

// Public API used by PurchaseInvoiceFormDialog.
export function attach() {
    refreshTarget();
    if (!state.attached) {
        document.addEventListener('keydown', onKeyDown, true);
        state.attached = true;
    }
}

export function detach() {
    if (state.attached) {
        document.removeEventListener('keydown', onKeyDown, true);
        state.attached = false;
    }
    state.target = null;
    state.buf = '';
}