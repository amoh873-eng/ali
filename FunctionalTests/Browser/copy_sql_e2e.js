const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5199';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 200)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 200)));
  try {
    // Login
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);
    log('LOGIN_URL=' + page.url());

    // جولة: الأصناف (يعرض بيانات من القاعدة الجديدة)
    await page.goto(BASE + '/inventory/items', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(5000);
    const itemsInfo = await page.evaluate(() => {
      const rows = document.querySelectorAll('.mud-table-row');
      const txt = (document.body ? document.body.innerText : '').slice(0, 200);
      return { rows: rows.length, txt: txt.replace(/\s+/g, ' ').slice(0, 150) };
    });
    log('ITEMS_ROWS=' + itemsInfo.rows);

    // جولة: ملصقات الباركود (الميزة التي أضفناها)
    await page.goto(BASE + '/inventory/barcode-labels', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(4000);
    const labelInfo = await page.evaluate(() => {
      const wrap = !!document.querySelector('.bl-wrap');
      const checks = document.querySelectorAll('.bl-table .mud-checkbox').length;
      return { wrap, checks };
    });
    log('LABELS_PAGE=' + labelInfo.wrap + ' ROWS=' + labelInfo.checks);
  } catch (e) {
    log('FATAL: ' + (e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_copy_e2e.txt', out.join('\n'));
    await browser.close();
  }
})();