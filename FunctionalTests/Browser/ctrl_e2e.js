const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 160)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 140)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  await page.goto(BASE + '/reports/financial-ratios', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  const gen = await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (!b) return false; b.click(); return true; });
  await page.waitForTimeout(4000);
  const probe = await page.evaluate(() => {
    const tables = document.querySelectorAll('.mud-table').length;
    const alerts = document.querySelectorAll('.mud-alert').length;
    const progress = document.querySelectorAll('.mud-progress-circular').length;
    const bodyText = document.body.innerText.replace(/\s+/g, ' ');
    return { tables, alerts, progress, bodyLen: bodyText.length };
  });
  log('CTRL financial-ratios: gen=' + gen + ' tables=' + probe.tables + ' alerts=' + probe.alerts + ' progress=' + probe.progress + ' bodyLen=' + probe.bodyLen);
  log('CONSOLE_ERRORS=' + errs.length);
  if (errs.length) { log('ERR0=' + errs[0]); log('ERR1=' + (errs[1] || '')); }
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 400)); fs.writeFileSync('D:/ERPSystem/_ctrl_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_ctrl_e2e.txt', out.join('\n'), 'ascii'); });