const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

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
    await page.waitForTimeout(6000);

    // أضف 16 صنفاً
    for (let i = 0; i < 16; i++) {
      await page.evaluate((idx) => {
        const items = document.querySelectorAll('.pos-item');
        if (items[idx]) items[idx].click();
      }, i);
      await page.waitForTimeout(220);
    }
    await page.waitForTimeout(1200);
    await page.screenshot({ path: 'D:/ERPSystem/_pos_cart_16.png' });

    // بنية البطاقة: name, price, qty box, total داخل أول بطاقة
    const card = await page.evaluate(() => {
      const line = document.querySelector('.pos-line');
      const r = (el, prop) => { if (!el) return null; const s = getComputedStyle(el); return prop === 'h' ? Math.round(el.getBoundingClientRect().height) : s[prop]; };
      return {
        hasName: !!line.querySelector('.pos-line-name'),
        hasPrice: !!line.querySelector('.pos-line-price'),
        hasQty: !!line.querySelector('.pos-qty-box'),
        hasTotal: !!line.querySelector('.pos-line-total'),
        cardBg: r(line, 'backgroundColor'),
        cardH: r(line, 'h'),
        altCardBg: (() => { const c = document.querySelectorAll('.pos-cart-row--alt .pos-line')[0]; return c ? getComputedStyle(c).backgroundColor : 'none'; })()
      };
    });
    log('CARD_NAME=' + card.hasName + ' PRICE=' + card.hasPrice + ' QTY=' + card.hasQty + ' TOTAL=' + card.hasTotal);
    log('CARD_H=' + card.cardH + ' WHITE_CARD_BG=' + card.cardBg + ' ALT_CARD_BG=' + card.altCardBg);

    // زر + يعمل: اضغط Add على أول سطر وتحقق أن الكمية تصبح 2
    await page.evaluate(() => {
      const addBtn = document.querySelector('.pos-line-qty .mud-icon-button');
      if (addBtn) addBtn.click();
    });
    await page.waitForTimeout(700);
    const qtyAfter = await page.evaluate(() => {
      const box = document.querySelector('.pos-qty-box');
      return box ? box.value : 'none';
    });
    log('QTY_AFTER_PLUS=' + qtyAfter + ' EXPECTED=2');

    // عدد الأصناف المرئية دون تمرير
    const vis = await page.evaluate(() => {
      const lines = document.querySelector('.pos-cart-lines');
      const linesRect = lines.getBoundingClientRect();
      let full = 0, partial = 0;
      for (const r of document.querySelectorAll('.pos-cart-row')) {
        const b = r.getBoundingClientRect();
        if (b.top >= linesRect.top - 1 && b.bottom <= linesRect.bottom + 1) full++;
        else if (b.top < linesRect.bottom && b.bottom > linesRect.top) partial++;
      }
      return JSON.stringify({ full: full * 2, partial: partial * 2 });
    });
    log('VISIBLE_ITEMS=' + vis);

    const stripes = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.pos-cart-row')).slice(0, 4).map(r => {
        const line = r.querySelector('.pos-line');
        return {
          rowBg: getComputedStyle(r).backgroundColor,
          cardBg: line ? getComputedStyle(line).backgroundColor : 'none'
        };
      });
    });
    log('STRIPES=' + JSON.stringify(stripes));
    log('SCREENSHOT_16=' + fs.existsSync('D:/ERPSystem/_pos_cart_16.png'));
    log('PAGE_ERRORS=' + (pageErrors.length ? pageErrors.join(' | ') : 'none'));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_cart_16_check.txt', out.join('\n'));
    await browser.close();
  }
})();