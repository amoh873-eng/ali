// End-to-end test: POS barcode scanning through the SAME method as the Purchase invoice.
// 1) Scan 5 different items -> each fast scan adds one cart line (green flash).
// 2) Re-scan the same barcode 3x -> stays a SINGLE line with quantity 4 (not 3 lines).
// 3) Scan an unknown barcode -> red "unknown" message, nothing added.
// The scanner-burst interceptor (scaleBarcode.js, same pattern as purchaseScan.js)
// swallows the rapid keystrokes and emits a single Enter into the POS search field.
// Scanning is simulated the REAL-cashier way: all barcode keystrokes + Enter are fired
// synchronously in one JS task (hardware scanners emit ~0ms inter-key gaps), exactly
// like the existing scale_scan_test.js.
const { chromium } = require('playwright');
const fs = require('fs');

const BASE = 'http://localhost:5186';
const EMAIL = process.env.SMOKE_EMAIL || 'smoke@erp.com';
const PASSWORD = process.env.SMOKE_PASSWORD || 'Test@1234';

const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

// 5 distinct item barcodes from the catalog (verified via DB).
const SCANS = ['6210000000471', '6210000000018', '6210000000199', '6210000000463', '6210000000073'];
const REPEAT = '6210000000471'; // scanned 3 extra times -> that line should reach qty 4
const UNKNOWN = '9999999999999'; // not in the catalog

// Fire a full scanner burst synchronously (13 chars + Enter) — hardware-accurate.
async function scanOnce(page, code) {
  await page.evaluate((scanCode) => {
    const target = document.body;
    const fire = (type, opts) =>
      target.dispatchEvent(new KeyboardEvent(type, Object.assign(
        { bubbles: true, cancelable: true, view: window }, opts)));
    for (const ch of scanCode) {
      fire('keydown', { key: ch, code: 'Digit' + ch, keyCode: ch.charCodeAt(0) });
    }
    fire('keydown', { key: 'Enter', code: 'Enter', keyCode: 13 });
  }, code);
  await page.waitForTimeout(600); // Blazor round-trip + re-render
}

async function readCart(page) {
  return page.evaluate(() => {
    const lines = [];
    document.querySelectorAll('.pos-line').forEach(line => {
      const nameEl = line.querySelector('.pos-line-name');
      const qtyEl = line.querySelector('.pos-qty-box');
      const totalEl = line.querySelector('.pos-line-total');
      lines.push({
        name: nameEl ? (nameEl.textContent || '').trim() : '',
        qty: qtyEl ? qtyEl.value : '',
        total: totalEl ? (totalEl.textContent || '').trim() : ''
      });
    });
    const search = (document.querySelector('.pos-search input') || {}).value || '';
    return { lines, search };
  });
}

async function readSnacks(page) {
  return page.evaluate(() =>
    Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim())
  );
}
(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 30000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 30000 });
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.click('button.login-btn')
    ]);
    await page.waitForTimeout(2500);
    log('URL_AFTER_LOGIN=' + page.url());

    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForSelector('.pos-search input', { timeout: 20000 }).catch(() => log('NO_SEARCH_FIELD'));
    await page.waitForTimeout(2500); // interceptor init + item load

    const interceptorState = await page.evaluate(async () => {
      try { const m = await import('/scaleBarcode.js'); return JSON.stringify(m.getState()); }
      catch (e) { return 'ERR:' + e.message; }
    });
    log('INTERCEPTOR_STATE=' + interceptorState);

    // 1) Scan 5 different items — each adds a line (green flash)
    for (const c of SCANS) { await scanOnce(page, c); }
    const cart1 = await readCart(page);
    log('CART_AFTER_5_SCANS_COUNT=' + cart1.lines.length);
    log('SEARCH_CLEARED_1=' + (cart1.search === ''));
    fs.writeFileSync('D:/ERPSystem/_pos_scan_cart_raw.json', JSON.stringify(cart1, null, 2), 'utf8');

    // 2) Re-scan same barcode 3x — single line qty 4 (not 3 new lines)
    for (let i = 0; i < 3; i++) { await scanOnce(page, REPEAT); }
    const cart2 = await readCart(page);
    log('CART_AFTER_REPEAT_3_COUNT=' + cart2.lines.length);
    log('HAS_QTY_4_LINE=' + cart2.lines.some(l => Math.abs(Number(l.qty) - 4) < 0.001));
    log('SEARCH_CLEARED_2=' + (cart2.search === ''));
    fs.writeFileSync('D:/ERPSystem/_pos_scan_cart2_raw.json', JSON.stringify(cart2, null, 2), 'utf8');

    // 3) Unknown barcode — red message, nothing added
    const snacksBefore = await readSnacks(page);
    await scanOnce(page, UNKNOWN);
    // MudSnackbar تعرض 5 ظاهراً كحد أقصى والباقي يُطابَر حتى يحل دورها —
    // فانتظر ظهور اللمحة الحمراء (حتى بعد انقضاء اللمحات الخضراء).
    await page.waitForFunction(() =>
      Array.from(document.querySelectorAll('.mud-snackbar'))
        .some(s => (s.textContent || '').indexOf('غير معروف') >= 0),
      null, { timeout: 9000 }).catch(() => log('RED_TIMEOUT'));
    const cart3 = await readCart(page);
    const snacksAfter = await readSnacks(page);
    const newSnacks = snacksAfter.filter(s => snacksBefore.indexOf(s) < 0);
    log('CART_AFTER_UNKNOWN_COUNT=' + cart3.lines.length);
    log('UNKNOWN_SNACKS=' + JSON.stringify(newSnacks));
    log('UNKNOWN_RED_SEEN=' + newSnacks.some(s => s.indexOf('غير معروف') >= 0));
    log('SEARCH_CLEARED_3=' + (cart3.search === ''));
    fs.writeFileSync('D:/ERPSystem/_pos_scan_unknown_raw.json',
      JSON.stringify({ cart: cart3, snacksBefore, snacksAfter }, null, 2), 'utf8');

    const allSnacks = await readSnacks(page);
    log('GREEN_FLASH_SEEN=' + allSnacks.some(s => s.indexOf('تمت إضافة') >= 0));
    log('AFTER_UNKNOWN_SNACKS=' + JSON.stringify(allSnacks));
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_scan_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();