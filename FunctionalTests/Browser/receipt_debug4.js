const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const errs = [];
  page.on('pageerror', e => { errs.push('PAGEERR ' + ((e && (e.stack || e.message)) || '?').slice(0, 250)); });
  page.on('console', m => { if (m.type() === 'error') errs.push('CON ' + (m.text() || '').slice(0, 200)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  errs.length = 0; // تجاهل أخطاء التحميل المسبق
  log('CLEARED_PRELOAD');
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  errs.length = 0;

  await page.evaluate(() => document.querySelector('.pos-item')?.click());
  await page.waitForTimeout(900);
  const inputs = page.locator('.pos-cart-right input');
  await inputs.nth(0).fill('500');
  await page.waitForTimeout(400);
  await page.evaluate(() => document.querySelector('.pos-checkout')?.click());
  await page.waitForTimeout(5000);

  log('ERRS=' + errs.length);
  errs.slice(0, 6).forEach(e => log('E ' + e));
  const after = await page.evaluate(() => ({ dialogs: document.querySelectorAll('.mud-dialog').length }));
  log('DIALOGS=' + after.dialogs);
  await browser.close();
}
main().catch(e => { log('FATAL ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_receipt_debug4.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_receipt_debug4.txt', out.join('\n'), 'ascii'); });