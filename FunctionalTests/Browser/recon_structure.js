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

    await page.goto(BASE + '/pos/card-reconciliation', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(4000);

    // أخرج البنية الفعلية: هل الجدول MudTable مصدر؟ كم صفاً وهل داخل iframe/container آخر؟
    const structure = await page.evaluate(() => {
      const texts = Array.from(document.querySelectorAll('*')).slice(0, 20000);
      const tables = document.querySelectorAll('table');
      const tbodyRows = Array.from(document.querySelectorAll('tbody tr'));
      const mudRows = Array.from(document.querySelectorAll('.mud-table-row'));
      const anyRowLike = Array.from(document.querySelectorAll('tr'));
      // هل توجد نصوص "غير موجود في النظام"؟
      const bodyHtml = document.body ? document.body.innerHTML : '';
      return {
        hasMudTable: document.querySelectorAll('.mud-table').length,
        tableCount: tables.length,
        tbodyRowCount: tbodyRows.length,
        mudRowCount: mudRows.length,
        trCount: anyRowLike.length,
        hasBankOnlyText: bodyHtml.includes('غير موجود في النظام'),
        hasReconTablePaper: bodyHtml.includes('recon-table-paper'),
        mudTableRowsSample: Array.from(document.querySelectorAll('.mud-table-row')).slice(0, 3).map(r => (r.textContent || '').slice(0, 60))
      };
    });
    out.push('STRUCTURE=' + JSON.stringify(structure));
  } catch (e) {
    out.push('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_recon_structure.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();