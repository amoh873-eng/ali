const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const errs = [];
  page.on('pageerror', e => errs.push((e.message || '').slice(0,150)));
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  await page.evaluate(() => document.querySelector('.pos-topbar-right .mud-icon-button')?.click());
  await page.waitForTimeout(1200);
  await page.locator('.mud-dialog button').filter({ hasText: '\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u0627\u0644\u0637\u0627\u0628\u0639\u0629' }).first().click();
  await page.waitForTimeout(2500);
  const res = await page.evaluate(() => {
    const dlg = document.querySelectorAll('.mud-dialog');
    const receipt = document.querySelector('.pos-receipt');
    return {
      dlgCount: dlg.length,
      hasReceipt: !!receipt,
      receiptText: receipt ? receipt.innerText.replace(/\s+/g,' ').slice(0,250) : null
    };
  });
  const out = JSON.stringify({ res, errs: errs.slice(0,3) }, null, 1);
  fs.writeFileSync('D:/ERPSystem/_receipt_isolated.txt', out, 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_receipt_isolated.txt', 'FATAL ' + String(e.message).slice(0,200), 'utf8'); process.exit(1); });