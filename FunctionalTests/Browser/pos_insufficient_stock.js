const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 600)); }

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

    // أضف أول صنف نافد إلى السلة (بالمؤشر — الأسماء عربية)
    const clicked = await page.evaluate(() => {
      const outs = document.querySelectorAll('.pos-item--out');
      if (!outs[0]) return false;
      outs[0].click();
      return true;
    });
    log('CLICKED_OUT_ITEM=' + clicked);
    await page.waitForTimeout(1200);

    // السطر يجب أن يميّز فوراً (رفع -no-stock + شارة نفد)
    const lineState = await page.evaluate(() => {
      const nl = document.querySelectorAll('.pos-line--no-stock');
      return {
        noStockCount: nl.length,
        badgeCount: document.querySelectorAll('.pos-line-stock-badge').length,
        names: Array.from(nl).map(l => (l.querySelector('.pos-line-name') || {}).textContent || ''),
        cartLines: document.querySelectorAll('.pos-line').length
      };
    });
    log('CART_LINE_NO_STOCK=' + JSON.stringify(lineState));

    // إتمام البيع — المبلغ المستلم كبير
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
// بعد الفشل: الرسالة تحمل اسم الصنف + السطر مميز + زر الشبكة يومض
    const after = await page.evaluate(() => {
      const lineName = (() => { const l = document.querySelector('.pos-line--no-stock .pos-line-name'); return l ? l.textContent || '' : ''; })();
      const snacks = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent || '');
      const flashName = (() => { const f = document.querySelector('.pos-item--flash .pos-item-name'); return f ? f.textContent || '' : ''; })();
      return {
        lineName,
        snacksText: snacks.join(' | '),
        snackCount: snacks.length,
        snackHasName: lineName ? snacks.some(s => s.includes(lineName)) : false,
        flashNameMatches: !!lineName && flashName === lineName,
        noStockAfter: document.querySelectorAll('.pos-line--no-stock').length
      };
    });
    log('AFTER_FAILED_CHECKOUT=' + JSON.stringify(after));
    log('SNACK_HAS_ITEM_NAME=' + after.snackHasName);
    log('GRID_FLASH_MATCHES=' + after.flashNameMatches);
    log('CART_LINE_STILL_MARKED=' + after.noStockAfter);

    await page.screenshot({ path: 'D:/ERPSystem/_pos_insufficient_stock.png' });
    log('SCREENSHOT_SAVED=' + fs.existsSync('D:/ERPSystem/_pos_insufficient_stock.png'));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_insufficient_check.txt', out.join('\n'));
    await browser.close();
  }
})();