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

  const recvInputs = await page.evaluate(() => Array.from(document.querySelectorAll('.pos-cart-right input')).map(i => ({ cls: i.className, ph: i.placeholder, val: i.value })));
  log('RECV_INPUTS ' + JSON.stringify(recvInputs));
  const inputs = page.locator('.pos-cart-right input');
  await inputs.nth(0).fill('1000');
  await page.waitForTimeout(500);
  const val = await inputs.nth(0).inputValue();
  log('RECV_AFTER_FILL=' + val);

  const totalTxt = await page.evaluate(() => {
    const t = document.querySelector('.pos-totals .grand');
    return t ? t.textContent.replace(/\s+/g, ' ') : 'no-total';
  });
  log('TOTAL=' + totalTxt);

  await inputs.nth(0).press('Enter');
  await page.waitForTimeout(800);

  const checkout = await page.evaluate(() => {
    const b = document.querySelector('.pos-checkout');
    if (!b) { return 'NO_BTN'; }
    b.click();
    return 'CLICKED_BTN ' + b.disabled;
  });
  log('CHECKOUT=' + checkout);
  await page.waitForTimeout(4000);

  const after = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    const snack = document.querySelector('.mud-snackbar');
    const cartLines = document.querySelectorAll('.pos-cart-lines .pos-line').length;
    return {
      dialogText: dlg ? dlg.innerText.replace(/\s+/g, ' ').slice(0, 200) : null,
      snackText: snack ? snack.innerText.replace(/\s+/g, ' ').slice(0, 120) : null,
      hasReceipt: !!document.querySelector('.pos-receipt'),
      cartLines
    };
  });
  log('AFTER ' + JSON.stringify(after));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_receipt_debug.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_receipt_debug.txt', out.join('\n'), 'ascii'); });