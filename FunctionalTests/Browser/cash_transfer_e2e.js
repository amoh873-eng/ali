// End-to-end test (Feature 2): Cash transfer between the Main Safe (1100) and Till Drawer (1105).
// 1) Float-In of 100 (Safe -> Till) via the POS Cash Transfer dialog.
// 2) Cash-Out of 50 (Till -> Safe).
// 3) The transfer log at /pos/cash-transfer shows both rows.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.click('button.login-btn')
    ]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2500);

    // ── 1) Float-In 100 ──
    await page.click('.pos-action-cash');
    await page.waitForSelector('.pos-transfer-amt input', { timeout: 15000 });
    log('TRANSFER_DIALOG_OPENED=' + true);
    await page.fill('.pos-transfer-amt input', '100');
    await page.waitForTimeout(500);
    await page.click('.pos-transfer-save');
    await page.waitForTimeout(4000);
    const snacks1 = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim()));
    log('FLOATIN_SNACK=' + snacks1.some(s => s.includes('تم تنفيذ التحويل')));

    // ── 2) Cash-Out 50 (تغيير الاتجاه إلى سحب نقد) ──
    await page.click('.pos-action-cash');
    await page.waitForSelector('.pos-transfer-amt input', { timeout: 15000 });
    log('TRANSFER_DIALOG_OPENED_2=' + true);
    // فتح قائمة الاتجاه واختيار "سحب نقد (إلى الخزنة)"
    await page.click('.pos-transfer-dir');
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('.mud-list-item'));
      const target = items.find(i => (i.textContent || '').includes('سحب نقد'));
      if (target) target.click();
    });
    await page.waitForTimeout(1200);
    const dirText = await page.evaluate(() =>
      (document.querySelector('.pos-transfer-dir') || {}).textContent || '');
    log('CASHOUT_DIR_SELECTED=' + dirText.includes('سحب نقد'));
    await page.fill('.pos-transfer-amt input', '50');
    await page.waitForTimeout(500);
    await page.click('.pos-transfer-save');
    await page.waitForTimeout(4000);

    // ── 3) سجل التحويلات ──
    await page.goto(BASE + '/pos/cash-transfer', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);
    const logText = await page.evaluate(() => (document.body ? document.body.innerText : ''));
    const rows = await page.evaluate(() => document.querySelectorAll('.mud-table-row').length);
    log('LOG_ROWS=' + rows);
    log('LOG_HAS_100=' + logText.includes('100.00'));
    log('LOG_HAS_50=' + logText.includes('50.00'));
    log('LOG_HAS_FLOATIN=' + logText.includes('تمويل درج الكاش'));
    log('LOG_HAS_CASHOUT=' + logText.includes('سحب نقد إلى الخزنة'));
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_cash_transfer_e2e.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();
