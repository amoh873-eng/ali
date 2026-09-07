// =====================================================================================
// scaleBarcode.js — Scanner-Burst coalescer for the POS (scale AND regular barcodes).
// -------------------------------------------------------------------------------------
// Loaded as an ES module (see ScaleBarcodeInterceptor.razor -> import("./scaleBarcode.js")).
// A single global `keydown` listener (capture phase) watches for the ultra-fast
// keystroke bursts that hardware barcode scanners emit (typically 5-30 ms apart).
// Human typing is slower than `scanSpeedMs`, so it is never swallowed.
//
// This mirrors the Purchase-Invoice method (purchaseScan.js) exactly:
//   * The whole burst is swallowed in the browser (Blazor never sees each keydown —
//     otherwise every scan floods the circuit and drops during rapid scanning).
//   * Terminating Enter commits the coalesced code into the existing POS search field
//     (native value setter + input/change) and emits a SINGLE marked Enter keydown,
//     so the EXISTING Pos.razor search pipeline (BarcodeParser + HandleSearchKeyDown)
//     adds the item / shows the green "added" flash / the red "unknown" message —
//     exactly one Blazor event per scanned item.
//
// Scale EAN-13 codes starting with a configured prefix (e.g. 2x) keep their special
// path: validated + parsed (digits 3-7 = SKU, 8-12 = weight/price) and handed to the
// Blazor component via [JSInvokable] ReceiveScaleBarcode, which rebuilds a canonical
// barcode and re-injects it into the search field. No DB schema / backend changes.
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
    swallowed: 0,         // عدد الأحرف التي ابتُلعت في الطفرة الحالية (يُميّز المسح عن الكتابة البطيئة)
    lastMs: 0,
    seq: 0                // رقم تسلسلي لكل التزام (يمنع مراقب المسح القديم من مسح التزام أحدث)
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

// أداة مسح حقل البحث بعد الإدخال (Enter) — تُبقي الشاشة جاهزة للرمز التالي
// فوراً، تماماً كما في برامج نقاط البيع العالمية (الحقل يُمسح ويبقى مركّزاً).
// مرحلتان:
//   المرحلة 1 (~40ms): مسح DOM فقط بدون حدث input — فلا نطمس قيمة _search التي ما زال
//     الخادم يعالج بها الـ Enter المتزامن (لو أرسلنا input('') مبكراً لضاع الإضافة
//     والتزم الخادم text='' عند دخول الـ Enter).
//   المرحلة 2 (~300ms): إرسال input('') — إعادة تعيين الحالة الداخلية
//     لـ MudBlazor (Immediate) حتى يرى المسح التالي — ولو بنفس الباركود — تغييراً.
//   المراقبة (حتى ~2s): Blazor Server قد يعيد لاحقاً قيمة قديمة (صدى حدث input مؤخر) —
//     نلتقط أي تواجد لقيمتنا ونمسحه + نعيد التعيين، دون التلامس مع كتابة المستخدم.
function clearSearchFieldDeferred(code) {
    const mySeq = ++state.seq; // هذا الالتزام — أي تحديث أحدث يُبطل كل مؤقتات هذا المسح
    // القيمة المرجعة: 0=قيمة غريبة/مشغول/التزام أحدث  1=نظيف  2=مُمسح فعلاً
    const clearDom = (fireInputEvent) => {
        if (state.seq !== mySeq) return 0;   // لا تلمس — جاء التزام أحدث (مسح تالٍ/نفس الرمز)
        const el = document.querySelector(state.config.searchSelector || '.pos-search input');
        if (!el || state.swallowed !== 0 || state.buffer) return 0; // طفرة/كتابة جديدة
        const v = el.value;
        if (v !== '' && v !== code) return 0;      // المستخدم كتب محتوى آخر — لا نلمسه
        if (v === '') { if (fireInputEvent) el.dispatchEvent(new Event('input', { bubbles: true })); return 1; }
        const clearSetter = Object.getOwnPropertyDescriptor(
            window.HTMLInputElement.prototype, 'value').set;
        if (clearSetter) clearSetter.call(el, '');
        else el.value = '';
        if (fireInputEvent) el.dispatchEvent(new Event('input', { bubbles: true }));
        if (document.activeElement === el || document.activeElement === document.body) el.focus();
        return 2;
    };

    // المرحلة 1: مسح DOM فوري (لا يوجد حدث input، فلا نزاحم معالجة Enter)
    setTimeout(() => clearDom(false), 40);

    // المرحلة 2: بعد معالجة Enter — إعادة تعيين MudBlazor عبر input('') دائماً
    setTimeout(() => { clearDom(true); }, 300);

    // المراقبة المستمرة: يمسح أي صدى متأخر لقيمتنا ويعيد التهيئة (ويتوقف فور
    // كتابة المستخدم محتوى مختلفاً أو بدء طفرة جديدة — لا نمسح عند المستخدم أبداً).
    let elapsed = 0;
    const poll = setInterval(() => {
        elapsed += 120;
        if (elapsed > 2000) { clearInterval(poll); return; }
        if (clearDom(false) === 2) clearDom(true);
    }, 120);
}

// Global keydown handler (capture phase so we see the events first).
function onKeyDown(e) {
    if (!state.enabled) return;

    // أحداث حقننا أنفسها — دعها تمر مباشرة إلى Blazor دون إعادة التقاط (كسر الحلقة)
    if (e.__erpScanInjected) return;

    // Ignore IME composition.
    if (e.isComposing || e.keyCode === 229) return;

    // Enter (scanner terminator) — finalize the accumulated burst / typed term.
    if (e.key === 'Enter' || e.key === 'NumpadEnter' || e.key === 'Return') {
        const wasBurst = state.swallowed > 0;
        const buf = state.buffer;
        const fieldEl = document.querySelector(state.config.searchSelector || '.pos-search input');
        const fieldValue = (fieldEl && typeof fieldEl.value === 'string') ? fieldEl.value.trim() : '';
        state.buffer = '';
        state.swallowed = 0;
        state.lastMs = 0;

        // كتابة يدوية (لا طفرة): القيمة مطبوعة فعلياً وحالة @bind-Value محدّثة على الخادم —
        // نترك Enter الحقيقي يمر للطرف المعتاد (الموثوق الأكثر للإدخال اليدوي)،
        // ونطبّق فقط المسح التلقائي ليكون الحقل جاهزاً للرمز التالي فوراً.
        if (!wasBurst) {
            if (fieldValue) clearSearchFieldDeferred(fieldValue);
            return;
        }

        // طفرة ماسح (كاملة أو جزئية): اجمع النص الأكمل من الحقل + المخزن المؤقت.
        let effective = buf;
        if (fieldValue) {
            if (fieldValue === buf || buf.startsWith(fieldValue)) effective = buf;
            else if (buf.length >= 1 && fieldValue.length >= 1) effective = fieldValue + buf;
            else effective = fieldValue;
        }
        if (!effective) return;

        e.preventDefault();
        e.stopPropagation();
        const parsed = tryParse(effective);
        if (parsed) {
            // A valid scale barcode (configured EAN-13 prefix) → special scale path.
            void routeToDotNet(parsed);
            return;
        }

        // A regular item barcode/code burst → commit it into the search field and
        // emit a SINGLE Enter so the existing POS handler adds the item (green flash)
        // or reports "unknown" (red flash). Exactly the purchase-invoice method.
        commitAndEnter(effective);
        return;
    }

    // Any non-single-char key breaks a scanner burst (only plain characters matter).
    if (e.key.length !== 1) { state.buffer = ''; state.swallowed = 0; state.lastMs = 0; return; }

    const now = Date.now();
    const gap = state.lastMs === 0 ? now : now - state.lastMs;

    if (gap > state.config.scanSpeedMs) {
        // Too slow to be a scanner -> human typing. Do NOT swallow; restart buffer.
        state.buffer = e.key;
        state.swallowed = 0;
        state.lastMs = now;
        return;
    }

    // Scanner-speed burst: buffer and swallow the char so the raw scan never
    // partially lands in the search field (avoids double-processing).
    state.lastMs = now;
    state.buffer += e.key;
    state.swallowed += 1;
    if (state.buffer.length > 64) state.buffer = state.buffer.slice(-64);
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

// Commit a coalesced code into the POS search field and emit a SINGLE Enter keydown,
// so the existing HandleSearchKeyDown / BarcodeParser pipeline processes it exactly
// like a real scan — the injected Enter is marked so our own capture listener never
// re-processes it (mirrors purchaseScan.js). Returns true when the field was found.
function commitAndEnter(code) {
    const selector = state.config.searchSelector || '.pos-search input';
    const input = document.querySelector(selector);
    if (!input) return false;

    // Use the native value setter so frameworks observing input events see the change.
    const valueSetter = Object.getOwnPropertyDescriptor(
        window.HTMLInputElement.prototype, 'value').set;

    // المسح المتكرر لنفس الباركود: MudBlazor Immediate يتجاهل حدث input إذا كانت
    // القيمة مطابقة لحالته الداخلية (بقايا المسح السابق) — أعد الضبط أولاً بحدث
    // input فارغ، ثم ضع الرمز؛ وكل ذلك قبل حقن Enter (ترتيب مضمون لا يضيع الإضافة).
    if (input.value === code) {
        if (valueSetter) valueSetter.call(input, '');
        else input.value = '';
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }

    if (valueSetter) valueSetter.call(input, code);
    else input.value = code;

    // Notify Blazor's @bind-Value (Immediate) that the value changed.
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));

    // Emit the terminating Enter a touch later (like a real scanner's terminator
    // which lands ~30-50ms after the last char). This guarantees the server-side
    // @bind-Value processes the `input` event BEFORE the Enter handler reads _search.
    // Guards: only fire while the field still holds THIS code and no new burst began.
    const injectedEnter = new KeyboardEvent('keydown', {
        key: 'Enter', code: 'Enter', bubbles: true, cancelable: true
    });
    Object.defineProperty(injectedEnter, '__erpScanInjected', { value: true });
    setTimeout(() => {
        const el = document.querySelector(selector);
        if (!el || el.value !== code) return;        // field was replaced by a newer scan
        if (state.swallowed !== 0 || state.buffer) return; // a new burst is in progress
        el.dispatchEvent(injectedEnter);
    }, 40);

    // مسح الحقل برمجياً بعد المسح — نفس مسار الإدخال اليدوي (clearSearchFieldDeferred):
    // MudBlazor Immediate يتجاهل حدث input القادم إذا كانت القيمة مطابقة لحالته الداخلية،
    // فيضيع التكرار لنفس الباركود؛ والمسح المؤجّل يضمن حقلاً جاهزاً للرمز التالي.
    clearSearchFieldDeferred(code);
    return true;
}

// Retained public helper: fill the search field with a canonical barcode (scale path)
// and let it flow into the regular search pipeline.
function injectSearch(code) {
    return commitAndEnter(code);
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
    if (!state.enabled) { state.buffer = ''; state.swallowed = 0; state.lastMs = 0; }
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