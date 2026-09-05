const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);

    await page.evaluate(() => { const o = document.querySelector('.pos-item--out'); if (o) o.click(); });
    await page.waitForTimeout(1200);

    await page.evaluate(() => {
      const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
      if (cash) {
        cash.value = '999999';
        cash.dispatchEvent(new Event('input', { bubbles: true }));
        cash.dispatchEvent(new Event('change', { bubbles: true }));
      }
    });
    await page.waitForTimeout(600);
    await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
    await page.waitForTimeout(4500);

    const raw = await page.evaluate(() => {
      const lineName = (() => { const l = document.querySelector('.pos-line--no-stock .pos-line-name'); return l ? l.textContent || '' : ''; })();
      const snacks = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent || '');
      const flashName = (() => { const f = document.querySelector('.pos-item--flash .pos-item-name'); return f ? f.textContent || '' : ''; })();
      const lineNamesAll = Array.from(document.querySelectorAll('.pos-line-name')).map(n => n.textContent || '');
      return JSON.stringify({ lineName, snacks, flashName, lineNamesAll, hasNoStockLine: !!document.querySelector('.pos-line--no-stock'), hasFlash: !!document.querySelector('.pos-item--flash') });
    });
    fs.writeFileSync('D:/ERPSystem/_pos_raw_snack.json', raw, 'utf8');
    console.log('WROTE_RAW');
  } catch (e) {
    fs.writeFileSync('D:/ERPSystem/_pos_raw_snack.err.txt', (e && e.message || String(e)).slice(0, 1000), 'utf8');
  } finally {
    await browser.close();
  }
})();