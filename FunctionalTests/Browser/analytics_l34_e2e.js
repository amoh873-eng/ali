const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
const PAGES = [
  { url: '/reports/cash-flow', name: 'CASHFLOW' },
  { url: '/reports/item-profitability', name: 'ITEM_PROFIT' },
  { url: '/reports/slow-moving-stock', name: 'SLOW_MOVING' },
  { url: '/reports/abc-analysis', name: 'ABC' }
];
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 160)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push(m.text().slice(0, 120)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  for (const p of PAGES) {
    const pre = (new Date()).getTime();
    await page.goto(BASE + p.url, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    const gen = await page.evaluate(() => {
      const btn = document.querySelector('button.mud-button-filled');
      if (!btn) return false;
      btn.click();
      return true;
    });
    await page.waitForTimeout(4000);
    const probe = await page.evaluate(() => {
      const tables = document.querySelectorAll('.mud-table').length;
      const alerts = document.querySelectorAll('.mud-alert').length;
      const papers = document.querySelectorAll('.mud-paper').length;
      const progress = document.querySelectorAll('.mud-progress-circular').length;
      const bodyText = document.body.innerText.replace(/\s+/g, ' ');
      return { tables, alerts, papers, progress, bodyLen: bodyText.length };
    });
    const renderOk = !probe.progress && (probe.tables > 0 || probe.alerts > 0);
    log(`${p.name}: url=${p.url} gen=${gen} tables=${probe.tables} alerts=${probe.alerts} papers=${probe.papers} progress=${probe.progress} bodyLen=${probe.bodyLen} RENDER_OK=${renderOk}`);
    try { await page.screenshot({ path: 'D:/l34_' + p.name.toLowerCase() + '.png', fullPage: true }); } catch (e) {}
  }
  log('CONSOLE_ERRORS=' + errs.length);
  if (errs.length) log('CONSOLE_FIRST=' + errs[0]);
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 400)); fs.writeFileSync('D:/ERPSystem/_l34_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_l34_e2e.txt', out.join('\n'), 'ascii'); });