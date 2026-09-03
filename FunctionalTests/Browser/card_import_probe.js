const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function raw(s) { out.push(typeof s === 'string' ? JSON.stringify(s) : String(s)); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => raw('PAGEERR ' + (e.message || '').slice(0, 200)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 110)); });

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 25000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 25000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/pos/card-reconciliation', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const hasInput = await page.evaluate(() => !!document.querySelector('#bankStatementFile'));
    raw('HAS_INPUT=' + hasInput);

    // CSV نظيف بمبلغ ثابت يتطابق مع فاتورة بطاقة قائمة (10.44)
    const lastRef = await page.evaluate(() => {
      const rows = Array.from(document.querySelectorAll('.mud-table-body .mud-table-row'));
      for (let i = 0; i < rows.length; i++) {
        const t = rows[i].textContent;
        // أعمدة النظام: المرجع في الأول
        const firstCell = rows[i].querySelector('td');
        return firstCell ? firstCell.textContent.trim() : '';
      }
      return '';
    });
    raw('FIRST_ROW_REF=' + lastRef);

    const csvPath = 'D:\\ERPSystem\\bank_probe.csv';
    fs.writeFileSync(csvPath, 'Reference,Amount,Date\n' + lastRef + ',10.44,2026-09-03\n', 'utf8');

    // عدّ ROWs قبل
    const before = await page.evaluate(() => document.querySelectorAll('.mud-table-body .mud-table-row').length);
    raw('ROWS_BEFORE=' + before);

    await page.setInputFiles('#bankStatementFile', csvPath);
    await page.waitForTimeout(6000);

    const snackbars = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim()));
    raw('SNACKBARS=' + JSON.stringify(snackbars));

    const after = await page.evaluate(() => document.querySelectorAll('.mud-table-body .mud-table-row').length);
    raw('ROWS_AFTER=' + after);

    const badges = await page.evaluate(() => Array.from(document.querySelectorAll('.recon-badge')).map(b => b.textContent.trim() + ':' + b.className));
    raw('BADGES=' + JSON.stringify(badges));

  } catch (e) {
    raw('PROBE_ERR ' + (e.message || '').slice(0, 400));
  } finally {
    raw('CONSOLE_ERRS=' + JSON.stringify(errs.slice(0, 6)));
    fs.writeFileSync('D:\\ERPSystem\\_card_probe_out.txt', out.join('\n'));
    await browser.close();
  }
}
main();