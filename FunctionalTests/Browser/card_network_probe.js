const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function raw(s) { out.push(typeof s === 'string' ? JSON.stringify(s) : String(s)); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => raw('PAGEERR ' + (e.message || '').slice(0, 150)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 100)); });

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 25000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 25000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    if (!(await page.evaluate(() => !!document.querySelector('.pos-root')))) { raw('NO_POS'); return; }

    await page.click('.pos-item');
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.pos-pay'));
      const card = btns.find(b => b.textContent.includes('بطاقة'));
      if (card) card.click();
    });
    await page.waitForTimeout(1200);

    // نقرة حقيقية عبر Playwright على حقل التحديد
    await page.locator('.pos-card-fields .mud-select').first().click();
    await page.waitForTimeout(1200);
    const lists = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-list-item')).map(i => i.textContent.trim()));
    raw('ITEMS_AFTER_CLICK=' + JSON.stringify(lists));

    const visaItem = page.locator('.mud-list-item', { hasText: 'Visa' }).first();
    const visaCount = await visaItem.count();
    raw('VISA_ITEM_COUNT=' + visaCount);
    if (visaCount > 0) {
      await visaItem.click();
      await page.waitForTimeout(1000);
    }

    const inputVal = await page.evaluate(() => {
      const sel = document.querySelector('.pos-card-fields .mud-select');
      return sel ? (sel.querySelector('input') || {}).value || '' : 'NO_SEL';
    });
    raw('NETWORK_VALUE=' + inputVal);

    // أكمل البيع بالمرجع
    await page.locator('.pos-card-fields input').nth(0).fill('NETW-PROBE-1');
    await page.locator('.pos-card-fields input').nth(1).fill('1234');
    await page.click('.pos-checkout');
    await page.waitForTimeout(4000);
    const receipt = await page.evaluate(() => { const r = document.querySelector('.pos-receipt'); return r ? r.textContent : ''; });
    raw('RECEIPT=' + JSON.stringify(receipt.slice(0, 300)));
  } catch (e) {
    raw('PROBE_ERR ' + (e.message || '').slice(0, 400));
  } finally {
    raw('ERRC=' + JSON.stringify(errs.slice(0, 4)));
    fs.writeFileSync('D:\\ERPSystem\\_netw_probe_out.txt', out.join('\n'));
    await browser.close();
  }
}
main();