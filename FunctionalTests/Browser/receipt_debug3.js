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

  await page.evaluate(() => document.querySelector('.pos-item')?.click());
  await page.waitForTimeout(900);
  const inputs = page.locator('.pos-cart-right input');
  await inputs.nth(0).fill('500');
  await page.waitForTimeout(500);
  await page.evaluate(() => document.querySelector('.pos-checkout')?.click());
  await page.waitForTimeout(5000);

  const after = await page.evaluate(() => {
    const snackbars = Array.from(document.querySelectorAll('.mud-snackbar, .mud-alert')).map(e => e.innerText.replace(/\s+/g, ' ').slice(0, 120));
    const dialogsOfInterest = Array.from(document.querySelectorAll('*')).filter(e => e.className && typeof e.className === 'string' && (e.className.includes('mud-dialog') || e.className.includes('mud-popover')));
    const bodyHasReceipt = document.body.innerText.includes('\u0625\u0634\u062a\u0631\u0627\u0621') || document.body.innerText.includes('Receipt');
    return {
      snacks: snackbars,
      dialogCount: document.querySelectorAll('.mud-dialog').length,
      popoverCount: document.querySelectorAll('.mud-popover').length,
      bodyHasReceipt
    };
  });
  log('AFTER ' + JSON.stringify(after));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_receipt_debug3.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_receipt_debug3.txt', out.join('\n'), 'ascii'); });