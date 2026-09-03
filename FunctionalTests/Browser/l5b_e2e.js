const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 160)));

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  await page.goto(BASE + '/reports/seasonality', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  const sea = await page.evaluate(() => {
    const svg = document.querySelector('svg');
    const tbl = document.querySelector('.mud-table');
    const txt = (tbl ? tbl.innerText : '').replace(/\s+/g, ' ');
    return {
      svgHtmlLen: svg ? svg.innerHTML.length : -1,
      rectInHtml: svg ? svg.innerHTML.includes('<rect') || svg.querySelectorAll('rect').length : -1,
      headerOk: txt.indexOf('1 2 3 4 5') >= 0,
      bodyLen: document.body.innerText.length
    };
  });
  log('SEA svgHtmlLen=' + sea.svgHtmlLen + ' rects=' + sea.rectInHtml + ' headerOk=' + sea.headerOk + ' bodyLen=' + sea.bodyLen);
  fs.writeFileSync('D:/ERPSystem/_sea2.txt', await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ')), 'utf8');
  try { await page.screenshot({ path: 'D:/l5_seasonality2.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_l5b.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_l5b.txt', out.join('\n'), 'ascii'); });