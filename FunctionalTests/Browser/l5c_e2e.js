const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/reports/seasonality', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  const res = await page.evaluate(() => {
    const svg = document.querySelector('svg[viewBox="0 0 620 250"]');
    if (!svg) return { found: false };
    const rects = svg.querySelectorAll('rect').length;
    const texts = svg.querySelectorAll('text').length;
    return { found: true, rects, texts, htmlLen: svg.innerHTML.length };
  });
  log('CHART ' + JSON.stringify(res));
  try { await page.screenshot({ path: 'D:/l5_seasonality3.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_l5c.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_l5c.txt', out.join('\n'), 'ascii'); });