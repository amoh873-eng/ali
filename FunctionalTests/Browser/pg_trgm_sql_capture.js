const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // بحث في شاشة الأصناف بمصطلح لحم (كما في التقرير السابق)
    await page.goto(BASE + '/inventory/items', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3500);
    const input = page.locator('input.mud-input-slot').first();
    await input.fill('لحم');
    await page.waitForTimeout(2200);

    // سجّل عدد النتائج
    const rows = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr')).length;
    });
    out.push('ITEMS_SEARCH_لحم_ROWS=' + rows);

    // سجّل حجم السجل قبل (لمعرفة موضع SQL الجديد)
    const beforeLen = fs.statSync('D:/ERPSystem/fg_out.log').size;
    out.push('LOG_BEFORE_BYTES=' + beforeLen);

    // اقرأ SQL الصادر فعلياً من السجل (كل الأسطر بعد النقطة السابقة تحتوي ILIKE)
    await page.waitForTimeout(1500);
    const afterLen = fs.statSync('D:/ERPSystem/fg_out.log').size;
    out.push('LOG_AFTER_BYTES=' + afterLen);

    const logAll = fs.readFileSync('D:/ERPSystem/fg_out.log', 'utf8');
    const tail = logAll.slice(Math.max(0, afterLen - 6000), afterLen);
    const ilikeLines = tail.split('\n').filter(l => /ILIKE|ILike|WHERE|FROM "Items"|FROM "Customers"|FROM "Suppliers"/i.test(l));
    out.push('ILIKELINES_COUNT=' + ilikeLines.length);
    // خذ الـ SELECT كاملاً (من أسطر Executed DbCommand حول ILIKE)
    const idx = tail.indexOf('SELECT');
    if (idx >= 0) {
      out.push('RAW_SQL=' + tail.slice(idx, tail.indexOf('\n', idx + 5000)).trim().slice(0, 4000).replace(/\s+/g, ' '));
    }
  } catch (e) {
    out.push('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t1_sql_capture.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();