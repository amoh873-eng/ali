const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

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
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2500);

    await page.fill('.pos-search input', 'BT-EXP');
    await page.waitForTimeout(600);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(900);

    // الكمية 1 — السبيل الوحيد المتاح دُفعة منتهية (5) لا تصلح
    await page.evaluate(() => {
      const box = document.querySelector('.pos-line .pos-qty-box');
      if (!box) return;
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
      setter.call(box, '1');
      box.dispatchEvent(new Event('input', { bubbles: true }));
      box.dispatchEvent(new Event('change', { bubbles: true }));
    });
    await page.waitForTimeout(900);

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
    for (const s of snacks) log('SNACK=' + s);
    fs.writeFileSync('D:/ERPSystem/_t_final_snacks.json', JSON.stringify(snacks), 'utf8');
    log('SALE_COMPLETED=' + snacks.some(s => s.includes('تم إتمام')));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t_final_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();