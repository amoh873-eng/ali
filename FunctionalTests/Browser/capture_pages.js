const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  await page.goto(BASE + '/reports/cash-flow', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) b.click(); });
  await page.waitForTimeout(4000);
  const cf = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' '));
  fs.writeFileSync('D:/ERPSystem/_cf_page.txt', cf, 'utf8');

  await page.goto(BASE + '/reports/abc-analysis', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) b.click(); });
  await page.waitForTimeout(4000);
  const abc = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' '));
  fs.writeFileSync('D:/ERPSystem/_abc_page.txt', abc, 'utf8');

  await page.goto(BASE + '/reports/item-profitability', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) b.click(); });
  await page.waitForTimeout(4000);
  const ip = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' '));
  fs.writeFileSync('D:/ERPSystem/_ip_page.txt', ip, 'utf8');

  await page.goto(BASE + '/reports/slow-moving-stock', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) b.click(); });
  await page.waitForTimeout(4000);
  const sm = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' '));
  fs.writeFileSync('D:/ERPSystem/_sm_page.txt', sm, 'utf8');

  fs.writeFileSync('D:/ERPSystem/_all_done.txt', 'OK', 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_all_done.txt', 'FATAL ' + String(e.message).slice(0, 200), 'utf8'); process.exit(1); });