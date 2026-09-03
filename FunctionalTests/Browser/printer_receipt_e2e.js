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

  // أضف أول منتج من الشبكة
  const added = await page.evaluate(() => {
    const btn = document.querySelector('.pos-item');
    if (!btn) return false;
    btn.click(); return true;
  });
  await page.waitForTimeout(1000);

  // املأ المبلغ المستلم
  const recv = page.locator('.pos-cart-right input').nth(0);
  await recv.fill('1000');
  await page.waitForTimeout(800);

  // اضغط إتمام البيع
  const checkout = await page.evaluate(() => {
    const b = document.querySelector('.pos-checkout');
    if (!b) return false;
    b.click(); return true;
  });
  await page.waitForTimeout(3500);

  // اقرأ الإيصال
  const receipt = await page.evaluate(() => {
    const r = document.querySelector('.pos-receipt');
    if (!r) return { noReceipt: true };
    const txt = r.innerText.replace(/\s+/g, ' ');
    return {
      noReceipt: false,
      text: txt.slice(0, 400),
      hasShop: txt.includes('\u0627\u0644\u0646\u0633\u0631 \u0627\u0644\u0630\u0647\u0628\u064a'),
      hasPhone: txt.includes('0791234567'),
      hasThanks: txt.includes('\u0634\u0643\u0631\u0627\u064b \u0644\u0632\u064a\u0627\u0631\u062a\u0643\u0645'),
      is58: r.className.includes('pos-receipt-58')
    };
  });
  log('CHECKOUT=' + checkout + ' RECEIPT=' + JSON.stringify(receipt));
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_receipt_printer.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_printer_receipt.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_printer_receipt.txt', out.join('\n'), 'ascii'); });