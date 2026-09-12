const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

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

    // go to POS and measure load
    const t0 = Date.now();
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);
    const loadMs = Date.now() - t0;

    const info = await page.evaluate(() => {
      const buttons = document.querySelectorAll('.pos-item').length;
      const cats = document.querySelectorAll('.pos-cat').length;
      const search = !!document.querySelector('.pos-search input');
      return { buttons, cats, search };
    });
    log('POS_LOAD_MS=' + loadMs);
    log('POS_ITEM_BUTTONS(' + info.buttons + ')  CATS(' + info.cats + ')  SEARCH=' + info.search);
    log('GRID_CAPPED_AT_240=' + (info.buttons <= 240));

    // اكتب في البحث (يجب أن يصفي فوراً من ذاكرة التخزين، لا DB)
    await page.fill('.pos-search input', 'لحم');
    await page.waitForTimeout(1200);
    const afterSearch = await page.evaluate(() => document.querySelectorAll('.pos-item').length);
    log('AFTER_SEARCH_BUTTONS=' + afterSearch);

    // مسح البحث وعرض قائمة أولية
    await page.fill('.pos-search input', '');
    await page.waitForTimeout(1200);

    log('PAGE_ERROR_COUNT=' + pageErrors.length);
    for (const e of pageErrors.slice(0, 5)) log('PAGE_ERR=' + e);

    // جرّب إضافة صنف + إمساك عملية (اختبار سريع للتفاعل بعد الإصلاح)
    await page.evaluate(() => {
      const btn = document.querySelector('.pos-item');
      if (btn) btn.click();
    });
    await page.waitForTimeout(800);
    const cartLines = await page.evaluate(() => document.querySelectorAll('.pos-line').length);
    log('CART_LINES_AFTER_CLICK=' + cartLines);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_check.txt', out.join('\n'));
    await browser.close();
  }
})();