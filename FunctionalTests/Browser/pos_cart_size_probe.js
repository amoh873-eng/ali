const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

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

    // add 16 items
    for (let i = 0; i < 16; i++) {
      await page.evaluate((idx) => {
        const items = document.querySelectorAll('.pos-item');
        if (items[idx]) items[idx].click();
      }, i);
      await page.waitForTimeout(250);
    }
    await page.waitForTimeout(1200);

    const m = await page.evaluate(() => {
      const lines = document.querySelector('.pos-cart-lines');
      const rs = Array.from(document.querySelectorAll('.pos-cart-row'));
      const firstRow = rs[0];
      const card = rs[0] ? rs[0].querySelector('.pos-line') : null;
      const qtyBox = rs[0] ? rs[0].querySelector('.pos-qty-box') : null;
      const rect = (el) => el ? { h: Math.round(el.getBoundingClientRect().height), w: Math.round(el.getBoundingClientRect().width) } : null;
      const scrollCss = getComputedStyle(lines);
      return {
        linesClientH: lines.clientHeight,
        linesScrollH: lines.scrollHeight,
        rowCount: rs.length,
        rowH: rect(firstRow),
        cardH: rect(card),
        qtyBox: rect(qtyBox),
        linesOverflowY: scrollCss.overflowY,
        iconBTN: rect(rs[0].querySelector('.pos-line-qty .mud-icon-button')),
        delBTN: rect(rs[0].querySelector('.pos-line-top .mud-icon-button')),
        nameFont: getComputedStyle(rs[0].querySelector('.pos-line-name')).fontSize,
        totalFont: getComputedStyle(rs[0].querySelector('.pos-line-total')).fontSize,
        rowBg0: getComputedStyle(rs[0]).backgroundColor,
        rowBg1: rs[1] ? getComputedStyle(rs[1]).backgroundColor : 'none'
      };
    });
    log('ROWS=' + m.rowCount);
    log('LINES_VIEWPORT_H=' + m.linesClientH + ' SCROLLH=' + m.linesScrollH + ' OVERFLOW=' + m.linesOverflowY);
    log('ROW_H=' + m.rowH.h + ' CARD_H=' + m.cardH.h + ' QTYBOX_H=' + m.qtyBox.h);
    log('ICON_BTN_H=' + m.iconBTN.h + ' DEL_H=' + m.delBTN.h);
    log('FONTS name=' + m.nameFont + ' total=' + m.totalFont);
    log('BG row0=' + m.rowBg0 + ' row1=' + m.rowBg1);
    // how many full rows currently visible without scrolling
    const visible = await page.evaluate(() => {
      const lines = document.querySelector('.pos-cart-lines');
      let n = 0;
      for (const r of document.querySelectorAll('.pos-cart-row')) {
        const b = r.getBoundingClientRect();
        if (b.top >= lines.getBoundingClientRect().top - 1 && b.bottom <= lines.getBoundingClientRect().bottom + 1) n++; else break;
      }
      return n;
    });
    log('FULL_ROWS_VISIBLE_NOW=' + visible + ' (= ' + visible * 2 + ' items)');
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_cart_size_probe.txt', out.join('\n'));
    await browser.close();
  }
})();