const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 120)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 100)); });

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
    const dlg = document.querySelector('.mud-dialog');
    const snack = document.querySelector('.mud-snackbar');
    const receipt = document.querySelector('.pos-receipt');
    return {
      dialogText: dlg ? dlg.innerText.replace(/\s+/g, ' ').slice(0, 300) : null,
      snackText: snack ? snack.innerText.replace(/\s+/g, ' ').slice(0, 120) : null,
      hasReceipt: !!receipt,
      receiptText: receipt ? receipt.innerText.replace(/\s+/g, ' ').slice(0, 300) : null
    };
  });
  log('AFTER ' + JSON.stringify(after));
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/receipt_final.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_receipt_debug2.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_receipt_debug2.txt', out.join('\n'), 'ascii'); });