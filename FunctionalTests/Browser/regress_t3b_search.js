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

    // العملاء: مصطلحات مضمونة الوجود: "فحص" (عميل فحص 8) و "cUS-002" (حالة أحرف مختلفة عن cus-002)
    await page.goto(BASE + '/sales/customers', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3500);
    const before = await page.evaluate(() => document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr').length);
    log('CUSTOMERS_BEFORE=' + before);

    const searchInput = page.locator('input.mud-input-slot').first();
    await searchInput.fill('فحص');
    await page.waitForTimeout(2200);
    let rows = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr')).map(r => (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 80));
    });
    log('CUSTOMERS_ARABIC_فحص_ROWS=' + rows.length + ' FIRST=' + (rows[0] || '(none)'));

    await searchInput.fill('');
    await page.waitForTimeout(1000);
    await searchInput.fill('cus-002');
    await page.waitForTimeout(2200);
    rows = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr')).map(r => (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 80));
    });
    log('CUSTOMERS_LOWERCASE_cus002_ROWS=' + rows.length + ' FIRST=' + (rows[0] || '(none)'));

    // الموردون بمصطلح مضمون
    await page.goto(BASE + '/purchases/suppliers', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3500);
    const supBefore = await page.evaluate(() => document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr').length);
    log('SUPPLIERS_BEFORE=' + supBefore);
    const supInput = page.locator('input.mud-input-slot').first();
    await supInput.fill('qa-sup');
    await page.waitForTimeout(2200);
    rows = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.mud-table-body .mud-table-row, tbody tr')).map(r => (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 80));
    });
    log('SUPPLIERS_LOWERCASE_qasup_ROWS=' + rows.length + ' FIRST=' + (rows[0] || '(none)'));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t3_search_result2.txt', out.join('\n'));
    await browser.close();
  }
})();