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
    // Login
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);

    // أضف 7 أصناف (يجب أن تظهر 7 أسطر داخل 4 صفوف، شطرنجية)
    const countItems = await page.evaluate(() => document.querySelectorAll('.pos-item').length);
    log('POS_ITEMS_MENU=' + countItems);
    for (let i = 0; i < 7; i++) {
      await page.evaluate((idx) => {
        const items = document.querySelectorAll('.pos-item');
        if (items[idx]) items[idx].click();
      }, i);
      await page.waitForTimeout(350);
    }
    await page.waitForTimeout(1200);
    await page.screenshot({ path: 'D:/ERPSystem/_pos_cart_2col.png' });

    const probe = await page.evaluate(() => {
      const rows = Array.from(document.querySelectorAll('.pos-cart-row')).map((r, i) => {
        const lines = Array.from(r.querySelectorAll('.pos-line'));
        const bg = getComputedStyle(r).backgroundColor;
        const boxes = lines.map(l => {
          const b = l.getBoundingClientRect();
          return { x: Math.round(b.x), y: Math.round(b.y), w: Math.round(b.width), name: (l.querySelector('.pos-line-name') || {}).textContent || '', total: (l.querySelector('.pos-line-total') || {}).textContent || '' };
        });
        return { i, bg, lines: boxes };
      });
      const grid = getComputedStyle(document.querySelector('.pos-cart-lines'));
      return { rows, gridTemplate: grid.gridTemplateColumns };
    });

    log('ROWS=' + probe.rows.length);
    log('LINES_TOTAL=' + probe.rows.reduce((a, r) => a + r.lines.length, 0));
    probe.rows.forEach(r => {
      log('ROW_' + r.i + '_BG=' + r.bg + ' _LINES=' + r.lines.length + (r.lines[1] ? ' _TWO_COLS_dx=' + (r.lines[1].x - r.lines[0].x) + ' _W1=' + r.lines[0].w + ' _W2=' + r.lines[1].w : ' _SINGLE') + (r.lines[0] ? ' _N1=' + r.lines[0].name + ' _T1=' + r.lines[0].total : '') + (r.lines[1] ? ' _N2=' + r.lines[1].name + ' _T2=' + r.lines[1].total : ''));
    });

    const nameOverflow = await page.evaluate(() => {
      const names = Array.from(document.querySelectorAll('.pos-line-name'));
      return names.map(n => n.scrollWidth > n.clientWidth ? 'TRUNCATED' : 'FULL').length > 0 ? 'ok' : 'none';
    });
    log('NAME_ELLIPSIS_FOUND=' + nameOverflow);
    log('PAGE_ERRORS=' + (pageErrors.length ? pageErrors.join(' | ') : 'none'));
    log('SCREENSHOT_SAVED=' + fs.existsSync('D:/ERPSystem/_pos_cart_2col.png'));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_cart_2col_check.txt', out.join('\n'));
    await browser.close();
  }
})();