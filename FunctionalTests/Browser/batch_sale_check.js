const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 250)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2500);

    // أضف الصنف التجريبي عبر البحث (كود + Enter — المسار المضمون)
    await page.fill('.pos-search input', 'BT-EXP');
    await page.waitForTimeout(600);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(900);

    const inCart = await page.evaluate(() => {
      const lines = Array.from(document.querySelectorAll('.pos-line'));
      return lines.map(l => (l.querySelector('.pos-line-name') || {}).textContent || '');
    });
    log('IN_CART=' + JSON.stringify(inCart));
    if (!inCart.some(n => n.includes('دُفعات'))) { log('FAILED_TO_ADD_VIA_ENTER'); return; }

    // اضبط الكمية إلى 15
    const qtySet = await page.evaluate(() => {
      const box = document.querySelector('.pos-line .pos-qty-box');
      if (!box) return false;
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
      setter.call(box, '15');
      box.dispatchEvent(new Event('input', { bubbles: true }));
      box.dispatchEvent(new Event('change', { bubbles: true }));
      return true;
    });
    log('SET_QTY_15=' + qtySet);
    await page.waitForTimeout(900);

    const lineState = await page.evaluate(() => {
      const lines = Array.from(document.querySelectorAll('.pos-line'));
      return lines.map(l => ({
        name: (l.querySelector('.pos-line-name') || {}).textContent || '',
        qty: (l.querySelector('.pos-qty-box') || {}).value || '',
        total: (l.querySelector('.pos-line-total') || {}).textContent || ''
      }));
    });
    log('CART=' + JSON.stringify(lineState));

    // المبلغ المستلم كبير وتنفيذ البيع
    await page.evaluate(() => {
      const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
      if (cash) {
        cash.value = '99999';
        cash.dispatchEvent(new Event('input', { bubbles: true }));
        cash.dispatchEvent(new Event('change', { bubbles: true }));
      }
    });
    await page.waitForTimeout(500);
    await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
    await page.waitForTimeout(6000);

    const snacks = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent));
    fs.writeFileSync('D:/ERPSystem/_t_sale_snacks.json', JSON.stringify(snacks), 'utf8');
    log('SALE_SNACK_COUNT=' + snacks.length);
    for (const s of snacks) log('SNACK=' + s);
    log('SECOND_OP=' + pageErrors.some(e => /second operation/i.test(String(e))));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t_sale_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();