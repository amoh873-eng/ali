const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 160)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 120)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  // 1) Budget entry page
  await page.goto(BASE + '/reports/budget-entry', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  // Load budget (click first filled button)
  let loaded = await page.evaluate(() => {
    const bts = Array.from(document.querySelectorAll('button'));
    const load = bts.find(b => b.textContent.includes('??????') || b.textContent.includes('\u062a\u062d\u0645\u064a\u0644')); // تحميل
    if (load) load.click();
    return !!load;
  });
  await page.waitForTimeout(3500);
  const entryProbe = await page.evaluate(() => {
    const t = document.querySelector('.mud-table');
    const rows = t ? t.querySelectorAll('tbody tr').length : 0;
    const cols = t ? t.querySelectorAll('tbody tr:first-child td').length : 0;
    return { rows, cols, hasInputs: t ? t.querySelectorAll('input').length : 0 };
  });
  log('BUDGET_ENTRY loadBtn=' + loaded + ' rows=' + entryProbe.rows + ' cols=' + entryProbe.cols + ' inputs=' + entryProbe.hasInputs);

  // 2) Budget vs Actual
  await page.goto(BASE + '/reports/budget-vs-actual', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  const gen = await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) { b.click(); return true; } return false; });
  await page.waitForTimeout(3500);
  const bvaProbe = await page.evaluate(() => {
    const t = document.querySelector('.mud-table');
    const alerts = document.querySelectorAll('.mud-alert').length;
    return { table: !!t, rows: t ? t.querySelectorAll('tbody tr').length : 0, alerts };
  });
  log('BUDGET_VS_ACTUAL gen=' + gen + ' table=' + bvaProbe.table + ' rows=' + bvaProbe.rows + ' alerts=' + bvaProbe.alerts);
  log('CONSOLE_ERR=' + errs.length + (errs[0] ? (' FIRST=' + errs[0]) : ''));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_l6_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_l6_e2e.txt', out.join('\n'), 'ascii'); });