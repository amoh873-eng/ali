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

    await page.goto(BASE + '/pos/card-reconciliation', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(4500);

    // اقرأ كل إحصائيات الملخص
    const stats = await page.evaluate(() => {
      const statEls = Array.from(document.querySelectorAll('.recon-stat'));
      return statEls.map(s => ({
        val: (s.querySelector('.recon-stat-val') || {}).textContent || '',
        lbl: (s.querySelector('.recon-stat-lbl') || {}).textContent || ''
      }));
    });
    log('RECON_STATS=' + JSON.stringify(stats));

    // عدد صفوف الجدول + نص أول عدة صفوف من نوع "غير موجود في النظام"
    const tableInfo = await page.evaluate(() => {
      const rows = Array.from(document.querySelectorAll('.mud-table-row'));
      const bankOnly = rows.filter(r => (r.innerText || '').includes('غير موجود في النظام'));
      const sample = bankOnly.slice(0, 3).map(r => (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 130));
      return { totalRows: rows.length, bankOnlyRows: bankOnly.length, sample };
    });
    log('TABLE_BANK_ONLY_ROWS=' + tableInfo.bankOnlyRows + ' TOTAL_ROWS=' + tableInfo.totalRows + ' SAMPLE=' + JSON.stringify(tableInfo.sample));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_recon_screen_read.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();