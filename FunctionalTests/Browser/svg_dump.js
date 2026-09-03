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
  const svgOuter = await page.evaluate(() => {
    const svg = document.querySelector('svg');
    return svg ? svg.outerHTML : 'NO_SVG';
  });
  fs.writeFileSync('D:/ERPSystem/_svg_dump.txt', svgOuter, 'utf8');
  log('SVG_BYTES=' + Buffer.byteLength(svgOuter, 'utf8'));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_svg_dump.txt', 'FATAL', 'utf8'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_svg_probe.txt', out.join('\n'), 'ascii'); });