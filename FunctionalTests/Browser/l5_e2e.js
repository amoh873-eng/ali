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

  // Dashboard period comparison
  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const dash = await page.evaluate(() => {
    const t = document.body.innerText;
    const pcs = document.querySelectorAll('.erp-period-comp').length;
    return {
      periods: pcs,
      hasCur: t.includes('\u0647\u0630\u0627 \u0627\u0644\u0634\u0647\u0631'),      // هذا الشهر
      hasPrev: t.includes('\u0627\u0644\u0634\u0647\u0631 \u0627\u0644\u0645\u0627\u0636\u064a'), // الشهر الماضي
      hasYoy: t.includes('\u0646\u0641\u0633 \u0627\u0644\u0634\u0647\u0631 \u0639\u0627\u0645 \u0645\u0627\u0636\u064d') // نفس الشهر عام ماضٍ
    };
  });
  log('DASH comps=' + dash.periods + ' cur=' + dash.hasCur + ' prev=' + dash.hasPrev + ' yoy=' + dash.hasYoy);
  fs.writeFileSync('D:/ERPSystem/_dash_page.txt', await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ')), 'utf8');
  try { await page.screenshot({ path: 'D:/l5_dashboard.png', fullPage: true }); } catch (e) {}

  // Seasonality page
  await page.goto(BASE + '/reports/seasonality', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  const sea = await page.evaluate(() => {
    const svg = document.querySelector('svg');
    const rects = svg ? svg.querySelectorAll('rect').length : 0;
    const texts = svg ? svg.querySelectorAll('text').length : 0;
    const tables = document.querySelectorAll('.mud-table').length;
    const rows = document.querySelectorAll('.mud-table tbody tr').length;
    const t = document.body.innerText;
    return { svg: !!svg, rects, texts, tables, rows,
             hasTotal: t.includes('\u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a \u0627\u0644\u0643\u0644\u064a') };
  });
  log('SEA svg=' + sea.svg + ' rects=' + sea.rects + ' texts=' + sea.texts + ' tables=' + sea.tables + ' rows=' + sea.rows + ' grandTotal=' + sea.hasTotal);
  fs.writeFileSync('D:/ERPSystem/_sea_page.txt', await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ')), 'utf8');
  try { await page.screenshot({ path: 'D:/l5_seasonality.png', fullPage: true }); } catch (e) {}

  log('CONSOLE_ERRORS=' + errs.length);
  if (errs.length) log('ERR0=' + errs[0]);
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 400)); fs.writeFileSync('D:/ERPSystem/_l5_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_l5_e2e.txt', out.join('\n'), 'ascii'); });