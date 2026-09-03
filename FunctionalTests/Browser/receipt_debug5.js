const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);

  // تحكم: فتح إعدادات POS (يجب أن يعمل)
  await page.evaluate(() => document.querySelector('.pos-topbar-right .mud-icon-button')?.click());
  await page.waitForTimeout(1200);
  const ctrlDialogs = await page.evaluate(() => document.querySelectorAll('.mud-dialog').length);
  await page.evaluate(() => { const d = document.querySelector('.mud-dialog'); if (d) { const btns = Array.from(d.querySelectorAll('.mud-dialog-actions button')); (Array.from(d.querySelectorAll('button')).find(b => b.textContent.includes('\u0625\u0644\u063a\u0627\u0621')) || btns[0])?.click(); } });
  await page.waitForTimeout(800);
  log('CTRL_SETTINGS_DIALOGS=' + ctrlDialogs);

  // البيع
  await page.evaluate(() => document.querySelector('.pos-item')?.click());
  await page.waitForTimeout(800);
  const inputs = page.locator('.pos-cart-right input');
  await inputs.nth(0).fill('300');
  await page.waitForTimeout(400);
  await page.evaluate(() => document.querySelector('.pos-checkout')?.click());
  await page.waitForTimeout(8000);

  const res = await page.evaluate(() => {
    return {
      dialogs: document.querySelectorAll('.mud-dialog').length,
      containers: document.querySelectorAll('.mud-dialog-container').length,
      overlays: document.querySelectorAll('.mud-overlay').length,
      bodyLen: document.body.innerText.length
    };
  });
  log('AFTER_SALE ' + JSON.stringify(res));
  await browser.close();
}
main().catch(e => { log('FATAL ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_receipt_debug5.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_receipt_debug5.txt', out.join('\n'), 'ascii'); });