const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function b64(s) { return Buffer.from(String(s || ''), 'utf8').toString('base64'); }
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 300)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(5000);

    // أضف 3 أصناف مختلفة لضمان توليد إيراد + تكلفة بحجم أكبر
    for (let i = 0; i < 3; i++) {
      await page.evaluate((idx) => {
        const items = document.querySelectorAll('.pos-item');
        if (items[idx]) items[idx].click();
      }, i);
      await page.waitForTimeout(400);
    }
    await page.waitForTimeout(800);

    const cart = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.pos-line')).map(l => ({
        name: (l.querySelector('.pos-line-name') || {}).textContent || '',
        qty: (l.querySelector('.pos-qty-box') || {}).value || '',
        total: (l.querySelector('.pos-line-total') || {}).textContent || ''
      }));
    });
    log('CART_LINES=' + cart.length);

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

    const post = await page.evaluate(() => {
      const snacks = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim());
      const dialogs = document.querySelectorAll('.mud-dialog').length;
      return { snacks, dialogs };
    });
    log('DIALOG_COUNT=' + post.dialogs);
    log('SNACKS_B64=' + JSON.stringify(post.snacks.map(b64)));
    log('HAS_SECOND_OP=' + pageErrors.some(e => /second operation/i.test(String(e))));

    // سجّل كل نص snack خام
    const rawSnacks = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent));
    fs.writeFileSync('D:/ERPSystem/_t2_snacks.txt', JSON.stringify(rawSnacks), 'utf8');
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t2_pos_sale.txt', out.join('\n'));
    await browser.close();
  }
})();