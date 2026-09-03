const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
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
  const res = await page.evaluate(() => {
    const methods = Array.from(document.querySelectorAll('.pos-pay-methods .pos-pay')).map(b => b.textContent.replace(/\s+/g, ' ').trim());
    const cashBtn = methods.some(m => m.includes('\u0646\u0642\u062f\u064a'));
    const newBtn = methods.some(m => m.includes('\u0641\u0627\u062a\u0648\u0631\u0629'));
    const creditBtn = methods.some(m => m.includes('\u0622\u062c\u0644'));
    return { methods, cashBtn, newBtn, creditBtn };
  });
  fs.writeFileSync('D:/ERPSystem/_pos_methods_utf8.txt', JSON.stringify(res, null, 2), 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_pos_methods_utf8.txt', 'FATAL ' + String(e.message).slice(0,150), 'utf8'); process.exit(1); });