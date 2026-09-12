// End-to-end probe: POS search-field auto-clear after Enter (typed lookups).
// Mirrors global-POS behaviour — after pressing Enter (typed name/code, or a slow
// hand-typed barcode), the search field empties itself and keeps focus so the cashier
// can immediately scan/type the next item without manually clearing.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const rows = [];

async function snap2(page, label) {
  const d = await page.evaluate(() => {
    const input = document.querySelector('.pos-search input');
    return {
      fieldValue: input ? input.value : ''
    };
  });
  rows.push('SNAP2[' + label + ']= ' + JSON.stringify(d));
}

async function snap(page, label) {
  await page.waitForTimeout(350);
  const d = await page.evaluate(() => {
    const input = document.querySelector('.pos-search input');
    return {
      fieldValue: input ? input.value : '',
      focusedInput: input ? (document.activeElement === input) : false,
      lines: Array.from(document.querySelectorAll('.pos-line')).map(l => ({
        name: (l.querySelector('.pos-line-name') || {}).textContent || '',
        qty: (l.querySelector('.pos-qty-box') || {}).value || ''
      })),
      snacks: Array.from(document.querySelectorAll('.mud-snackbar')).slice(-2).map(s => (s.textContent || '').trim())
    };
  });
  rows.push('SNAP[' + label + ']= ' + JSON.stringify(d));
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => rows.push('PAGEERROR ' + (e && e.message || '')));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2000);
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForSelector('.pos-search input', { timeout: 20000 });
    await page.waitForTimeout(2500);

    const search = page.locator('.pos-search input').first();
    await search.click();

    // 1) Typed ITEM NAME (human speed, gaps > interceptor burst threshold) then Enter
    await page.keyboard.type('كيبورد', { delay: 120 });
    await page.keyboard.press('Enter');
    await snap(page, 'afterTypedName');

    // 2) Typed BARCODE slowly (human speed) then Enter — field must auto-clear
    await page.keyboard.type('6210000000199', { delay: 100 });
    await page.keyboard.press('Enter');
    await snap(page, 'afterTypedBarcode');

    // 3) Immediately type the next code (no manual clearing needed) → still works
    await page.keyboard.type('6210000000463', { delay: 100 });
    await page.keyboard.press('Enter');
    // عيّنات توقيتية لمراقبة دورة حياة القيمة بعد Enter
    await page.waitForTimeout(120);
    await snap2(page, 't120');
    await page.waitForTimeout(180);
    await snap2(page, 't300');
    await page.waitForTimeout(300);
    await snap2(page, 't600');
    await snap(page, 'final');

    // 4) A fast full scanner burst — the classic path, must still clear too
    await page.evaluate(() => {
      const t = document.body;
      const fire = (ty, o) => t.dispatchEvent(new KeyboardEvent(ty, Object.assign({ bubbles: true, cancelable: true, view: window }, o)));
      const code = '6210000000073';
      for (const ch of code) fire('keydown', { key: ch, code: 'Digit' + ch, keyCode: ch.charCodeAt(0) });
      fire('keydown', { key: 'Enter', code: 'Enter', keyCode: 13 });
    });
    await snap(page, 'afterScannerBurst');

    // 5) Unknown typed code → red message + field auto-cleared
    await page.keyboard.type('1111111111111', { delay: 100 });
    await page.keyboard.press('Enter');
    await page.waitForTimeout(600);
    await snap(page, 'afterUnknown');
  } catch (e) {
    rows.push('FATAL ' + (e && e.stack || e.message || String(e)).slice(0, 600));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_autoclear.json', JSON.stringify(rows, null, 2), 'utf8');
    await browser.close();
  }
})();