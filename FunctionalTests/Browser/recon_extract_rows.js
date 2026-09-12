const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 200)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos/card-reconciliation', { waitUntil: 'domcontentloaded', timeout: 60000 });
    // تمرير داخل الجدول لتحميل كل الصفوف (MudTable قد يجلب بالتدريج) + انتظار
    await page.waitForTimeout(4000);
    for (let i = 0; i < 6; i++) {
      await page.evaluate(() => { const t = document.querySelector('.mud-table-container'); if (t) t.scrollTop = 1000000; });
      await page.waitForTimeout(500);
    }
    await page.waitForTimeout(1000);

    // اقرأ كل خلية المرجع + المبلغ + حالة كل صف من الجدول
    const rowsData = await page.evaluate(() => {
      const rows = Array.from(document.querySelectorAll('.mud-table-row'));
      return rows.map(r => {
        const tds = Array.from(r.querySelectorAll('td, .mud-td'));
        return tds.map(td => (td.textContent || '').replace(/\s+/g, ' ').trim()).slice(0, 6);
      });
    });
    const banksOnly = rowsData.filter(r => r.some(x => x.includes('غير موجود في النظام')));
    log('ROW_COUNT=' + rowsData.length + ' BANK_ONLY=' + banksOnly.length);

    // اكتب ملف بمجاميع المرجع … المبلغ لكل صف "غير موجود" للمقارنة مع قاعدة البيانات
    const lines = banksOnly.map(r => JSON.stringify(r));
    fs.writeFileSync('D:/ERPSystem/_recon_screen_rows.json', lines.join('\n'), 'utf8');
    log('WROTE_SCREEN_ROWS=' + lines.length);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    await browser.close();
  }
})();