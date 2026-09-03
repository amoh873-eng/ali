const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 150)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 110)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  await page.goto(BASE + '/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4500);

  const res = await page.evaluate(() => {
    const lg = Array.from(document.querySelectorAll('.mud-item-lg-3'));
    const tops = lg.length ? lg.map(e => Math.round(e.getBoundingClientRect().top)) : [];
    const sameRow = new Set(tops).size === 1;
    const papers = lg.map(i => i.querySelector('.mud-paper'));
    const bgs = papers.map(p => (p ? getComputedStyle(p).backgroundColor : 'none'));
    const t = document.body.innerText;
    return {
      count: lg.length,
      sameRow,
      tops: tops.slice(0, 5),
      bgs,
      hasSales: t.includes('\u0635\u0627\u0641\u064a \u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a'),
      hasPurchases: t.includes('\u0635\u0627\u0641\u064a \u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a'),
      hasIncome: t.includes('\u0635\u0627\u0641\u064a \u0627\u0644\u062f\u062e\u0644'),
      hasInventory: t.includes('\u0642\u064a\u0645\u0629 \u0627\u0644\u0645\u062e\u0632\u0648\u0646'),
      periods: document.querySelectorAll('.erp-period-comp').length
    };
  });

  log('COUNT=' + res.count + ' SAME_ROW=' + res.sameRow + ' TOPS=' + JSON.stringify(res.tops));
  log('LABELS sales=' + res.hasSales + ' purchases=' + res.hasPurchases + ' income=' + res.hasIncome + ' inventory=' + res.hasInventory + ' periodComps=' + res.periods);
  log('BGS=' + JSON.stringify(res.bgs));
  log('CONSOLE_ERR=' + errs.length + (errs[0] ? (' FIRST=' + errs[0]) : ''));
  try { await page.screenshot({ path: 'D:/dash_kpi.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_dash_check.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_dash_check.txt', out.join('\n'), 'ascii'); });