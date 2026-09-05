const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 300)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const errs = [];
  page.on('pageerror', e => errs.push((e && e.message || '').slice(0, 200)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);
    log('T0_AFTER_LOAD_ERRORS=' + (errs.length ? errs.join(' | ') : 'none'));
    errs.length = 0;

    await page.evaluate(() => { const i = document.querySelector('.pos-item'); if (i) i.click(); });
    await page.waitForTimeout(1500);
    log('T1_AFTER_ADD1_ERRORS=' + (errs.length ? errs.join(' | ') : 'none'));
    errs.length = 0;

    // حرّك زر الكمية (Add) في السلة للتأكد أن أزرار السلة تعمل
    const addResult = await page.evaluate(() => {
      const addBtn = document.querySelector('.pos-line-qty .mud-icon-button');
      if (addBtn) { addBtn.click(); return true; }
      return false;
    });
    await page.waitForTimeout(800);
    log('QTY_ADD_CLICK=' + addResult + ' QTY_BOX_MUD_TOTAL=' + await page.evaluate(() => document.querySelector('.pos-line-total') ? document.querySelector('.pos-line-total').textContent : 'none'));
    log('T2_AFTER_QTY_ERRORS=' + (errs.length ? errs.join(' | ') : 'none'));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_cart_2col_probe.txt', out.join('\n'));
    await browser.close();
  }
})();