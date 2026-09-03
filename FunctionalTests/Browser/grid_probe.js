const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4500);

  const res = await page.evaluate(() => {
    const grid = document.querySelector('.mud-grid');
    if (!grid) return { noGrid: true };
    const items = Array.from(grid.querySelectorAll(':scope > div')); // direct children = MudItem divs
    return items.slice(0, 6).map((el, i) => {
      const r = el.getBoundingClientRect();
      return { i, cls: (el.className || '').toString(), top: Math.round(r.top), left: Math.round(r.left), w: Math.round(r.width), bg: (el.querySelector('.mud-paper') ? getComputedStyle(el.querySelector('.mud-paper')).backgroundColor : 'none') };
    });
  });
  log('ITEMS=' + JSON.stringify(res, null, 1));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.message || '').slice(0, 200)); fs.writeFileSync('D:/ERPSystem/_grid_probe.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_grid_probe.txt', out.join('\n'), 'ascii'); });