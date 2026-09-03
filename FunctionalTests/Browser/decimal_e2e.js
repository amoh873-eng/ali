const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 90)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);

  // أضف منتجاً
  await page.evaluate(() => document.querySelector('.pos-item')?.click());
  await page.waitForTimeout(700);
  const totalTxt = await page.evaluate(() => (document.querySelector('.pos-totals .grand span:last-child') || {}).textContent || '');
  log('TOTAL_RAW=' + totalTxt);

  // املأ المبلغ المستلم بقيمة عشرية "300.50"
  const inputs = page.locator('.pos-cart-right input');
  await inputs.nth(0).fill('300.50');
  await page.waitForTimeout(600);
  const changeDot = await page.evaluate(() => (document.querySelector('.pos-change') || {}).textContent || '');

  // ثم "300,50"
  await inputs.nth(0).fill('300,50');
  await page.waitForTimeout(600);
  const changeComma = await page.evaluate(() => (document.querySelector('.pos-change') || {}).textContent || '');

  log('CHANGE_DOT=' + changeDot);
  log('CHANGE_COMMA=' + changeComma);
  log('CONSOLE_ERR=' + errs.length);
  await browser.close();
}
main().catch(e => { log('FATAL ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_decimal_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_decimal_e2e.txt', out.join('\n'), 'ascii'); });