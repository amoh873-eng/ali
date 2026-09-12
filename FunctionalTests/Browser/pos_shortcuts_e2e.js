// End-to-end test: F1–F7 keyboard shortcuts on the POS screen.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

async function login(page) {
  await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
    page.click('button.login-btn')
  ]);
  await page.waitForTimeout(2500);
}

async function goPos(page) {
  await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(2500);
}

async function cartCount(page) {
  return page.evaluate(() => document.querySelectorAll('.pos-line').length);
}
async function snacks(page) {
  return page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim()));
}
async function currentPaymentMethod(page) {
  return page.evaluate(() => {
    const btns = Array.from(document.querySelectorAll('.pos-pay'));
    const active = btns.find(b => b.classList.contains('active'));
    return active ? (active.textContent || '').trim() : '';
  });
}

async function addItem(page, code) {
  await page.evaluate(() => {
    const s = document.querySelector('.pos-search input');
    if (s) { const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set; if (set) set.call(s, ''); s.dispatchEvent(new Event('input', { bubbles: true })); }
  });
  await page.waitForTimeout(300);
  await page.fill('.pos-search input', code);
  await page.waitForTimeout(400);
  await page.keyboard.press('Enter');
  await page.waitForTimeout(1300);
  const c = await cartCount(page);
  if (c === 0) {
    await page.fill('.pos-search input', code);
    await page.waitForTimeout(400);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(1500);
  }
  return cartCount(page);
}

async function setCash(page, val) {
  await page.evaluate((v) => {
    const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
    if (cash) { cash.value = v; cash.dispatchEvent(new Event('input', { bubbles: true })); cash.dispatchEvent(new Event('change', { bubbles: true })); }
  }, val);
  await page.waitForTimeout(500);
}

async function closeDialog(page) {
  await page.evaluate(() => {
    const btns = Array.from(document.querySelectorAll('.mud-dialog-actions button'));
    const cancel = btns.find(b => !/print|pos-return-save|pos-transfer-save/.test(b.className));
    if (cancel) cancel.click();
  });
  await page.waitForTimeout(1200);
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));

  try {
    await login(page);
    await goPos(page);

    // ── F3 / F4 ──
    await page.keyboard.press('F4'); await page.waitForTimeout(800);
    log('F4_PAYMENT=' + await currentPaymentMethod(page));
    await page.keyboard.press('F3'); await page.waitForTimeout(800);
    log('F3_PAYMENT=' + await currentPaymentMethod(page));

    // ── F1 ──
    const before = await addItem(page, '10010');
    await page.keyboard.press('F1'); await page.waitForTimeout(900);
    log('F1_CART_BEFORE=' + before + ' AFTER=' + await cartCount(page));
    log('F1_CLEARED=' + ((await cartCount(page)) === 0));

    // ── F2 ──
    await addItem(page, '10010');
    await setCash(page, '99999');
    await page.keyboard.press('F2'); await page.waitForTimeout(5000);
    const receiptShown = await page.evaluate(() => !!document.querySelector('.pos-receipt'));
    log('F2_SALE_RECEIPT=' + receiptShown);
    log('F2_SNACKS=' + JSON.stringify(await snacks(page)));
    await closeDialog(page);
    await page.waitForFunction(() => { const b = document.querySelector('.pos-action-return'); return b && !b.disabled; }, null, { timeout: 20000 }).catch(() => {});

    // ── F5 ──
    await addItem(page, '10010');
    await page.keyboard.press('F5'); await page.waitForTimeout(2500);
    log('F5_HELD_SNACK=' + (await snacks(page)).some(s => /إيقاف|Held|موقوف/i.test(s)));
    log('F5_CART_CLEARED=' + ((await cartCount(page)) === 0));

    // ── F6 / F7 ──
    await page.keyboard.press('F6');
    await page.waitForSelector('.pos-return-inv input', { timeout: 15000 }).then(() => log('F6_RETURN_DIALOG=true')).catch(() => log('F6_RETURN_DIALOG=false'));
    await closeDialog(page);
    await page.keyboard.press('F7');
    await page.waitForSelector('.pos-transfer-amt input', { timeout: 15000 }).then(() => log('F7_TRANSFER_DIALOG=true')).catch(() => log('F7_TRANSFER_DIALOG=false'));
    await closeDialog(page);

    // ── F5 لا يحدّث الصفحة ──
    await page.evaluate(() => { window.__posMarker = 'alive'; });
    await page.keyboard.press('F5'); await page.waitForTimeout(1500);
    const urlAfterF5 = page.url();
    const markerAlive = await page.evaluate(() => window.__posMarker === 'alive');
    log('F5_NO_REFRESH_OK=' + (urlAfterF5.includes('/pos') && markerAlive));

    // ── حالات التعطيل ──
    await page.keyboard.press('F2'); await page.waitForTimeout(1500);
    log('F2_EMPTY_SNACK=' + (await snacks(page)).some(s => /فارغة|empty/i.test(s)));
    await page.keyboard.press('F5'); await page.waitForTimeout(1500);
    log('F5_EMPTY_NOOP=' + ((await cartCount(page)) === 0));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_shortcuts_e2e.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();
