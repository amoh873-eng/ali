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
  await page.evaluate(() => document.querySelector('.pos-item')?.click());
  await page.waitForTimeout(700);

  const input = page.locator('.pos-cart-right input').nth(0);
  // مسح الحقل ثم كتابة كاملة عبر fill (يضبط القيمة كلها دفعة واحدة)
  await input.click();
  await page.waitForTimeout(200);
  await input.fill('');
  await page.waitForTimeout(200);
  await input.fill('300.50');
  await page.waitForTimeout(700);
  const rawVal = await input.inputValue();
  const ch = await page.evaluate(() => (document.querySelector('.pos-change') || {}).textContent || '');

  const out = JSON.stringify({ rawVal, change: ch }, null, 1);
  fs.writeFileSync('D:/ERPSystem/_decimal5.txt', out, 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_decimal5.txt', 'FATAL ' + String(e.message).slice(0,200), 'utf8'); process.exit(1); });