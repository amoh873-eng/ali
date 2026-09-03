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

  const res = await page.evaluate(() => {
    const methods = Array.from(document.querySelectorAll('.pos-pay')).map(b => b.textContent.replace(/\s+/g, ' ').trim());
    const hasCredit = document.body.innerText.includes('\u0622\u062c\u0644'); // آجل
    const hasNewInvoice = methods.some(m => m.includes('\u0641\u0627\u062a\u0648\u0631\u0629 \u062c\u062f\u064a\u062f\u0629') || m.includes('New Invoice'));
    const cashBtn = methods.some(m => m.startsWith('\u0646\u0642\u062f\u064a'));
    const cardBtn = methods.some(m => m.startsWith('\u0628\u0637\u0627\u0642\u0629'));
    const incomingField = !!document.querySelector('.pos-cart-right input[type="text"], .pos-cart-right .mud-input');
    // عدد أزرار طرق الدفع في الصف
    const payMethods = document.querySelectorAll('.pos-pay-methods .pos-pay').length;
    return { methods, hasCredit, hasNewInvoice, cashBtn, cardBtn, incomingField, payMethods };
  });
  log('RESULT ' + JSON.stringify(res));
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_no_credit.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_nocredit.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_nocredit.txt', out.join('\n'), 'ascii'); });