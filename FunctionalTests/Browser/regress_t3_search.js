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

    // لكل شاشة: البحث بجزء الاسم ثم عدّ النتائج
    const screens = [
      { url: '/inventory/items', term: 'لحم', label: 'ITEMS' },
      { url: '/sales/customers', term: 'عمر', label: 'CUSTOMERS' },
      { url: '/purchases/suppliers', term: 'مورد', label: 'SUPPLIERS' }
    ];

    for (const s of screens) {
      await page.goto(BASE + s.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(3500);

      const before = await page.evaluate(() => {
        return document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr').length;
      });

      // ابحث عن حقل البحث (قد يكون هناك حقول تصفية متعددة — نستخدم الأول الأقرب للأعلى)
      const searchInput = page.locator('input.mud-input-slot').first();
      await searchInput.fill(s.term);
      await page.waitForTimeout(2200);

      const after = await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr'));
        const firstRowText = rows[0] ? (rows[0].innerText || '').replace(/\s+/g, ' ').trim().slice(0, 120) : '(no rows)';
        return { count: rows.length, firstRow: firstRowText };
      });

      log('T3_' + s.label + '_BEFORE_ROWS=' + before + ' TERM=' + s.term);
      log('T3_' + s.label + '_AFTER_ROWS=' + after.count + ' FIRST_ROW=' + after.firstRow);

      await searchInput.fill('');
      await page.waitForTimeout(1000);
    }
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t3_search_result.txt', out.join('\n'));
    await browser.close();
  }
})();