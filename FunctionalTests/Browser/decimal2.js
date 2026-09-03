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

  const change = async (val, label) => {
    const inputs = page.locator('.pos-cart-right input');
    await inputs.nth(0).fill(val);
    await page.waitForTimeout(500);
    const v = await inputs.nth(0).inputValue();
    const ch = await page.evaluate(() => (document.querySelector('.pos-change') || {}).textContent || '');
    return { label, v, ch };
  };

  const r1 = await change('300', 'INT');
  const r2 = await change('300.50', 'DOT');
  const r3 = await change('300,50', 'COMMA');
  const out = JSON.stringify({ r1, r2, r3 }, null, 1);
  fs.writeFileSync('D:/ERPSystem/_decimal2.txt', out, 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_decimal2.txt', 'FATAL ' + String(e.message).slice(0,200), 'utf8'); process.exit(1); });