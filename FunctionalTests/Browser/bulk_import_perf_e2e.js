const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 200)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 100)); });

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);

    // ── توليد ملف 5000 صف ──
    const N = 5000;
    const rows = [];
    for (let i = 1; i <= N; i++) {
      rows.push(`صنف اختبار كبير ${i},BULK5K-${String(i).padStart(5, '0')},${(i * 0.5).toFixed(2)},${(i * 0.75).toFixed(2)},مواد خام,قطعة`);
    }
    const csv = 'اسم الصنف,الباركود,سعر الشراء,سعر البيع,الفئة,الوحدة\n' + rows.join('\n') + '\n';
    const csvPath = 'D:\\ERPSystem\\bulk5k.csv';
    fs.writeFileSync(csvPath, csv, 'utf8');
    log('PERF_FILE_ROWS=' + N + ' size=' + csv.length);

    await page.goto(BASE + '/inventory/bulk-import', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    const tUpload0 = Date.now();
    await page.setInputFiles('input[type=file]', csvPath);
    // انتظر ظهور رسالة التحليل
    await page.waitForFunction(() => {
      const alerts = Array.from(document.querySelectorAll('.mud-alert'));
      return alerts.some(a => a.textContent.includes('تم تحليل'));
    }, null, { timeout: 60000 });
    const tUpload1 = Date.now();
    log('UPLOAD_PARSE_SECONDS=' + ((tUpload1 - tUpload0) / 1000).toFixed(1));

    async function clickByText(part) {
      await page.evaluate((p) => {
        const btns = Array.from(document.querySelectorAll('.mud-button-root'));
        const b = btns.find(x => x.textContent.includes(p));
        if (b) b.click();
      }, part);
    }

    await clickByText('التالي');   // → mapping
    await page.waitForTimeout(1500);
    await clickByText('التالي');   // → preview
    // انتظار المعاينة
    await page.waitForFunction(() => document.querySelectorAll('.bulk-stat').length >= 4, null, { timeout: 60000 });
    const previewStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('PREVIEW_STATS=' + JSON.stringify(previewStats));

    const tExec0 = Date.now();
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const confirm = btns.find(b => b.textContent.includes('تأكيد الاستيراد'));
      if (confirm) confirm.click();
    });
    // انتظار الوصول للخطوة 5 (التقرير)
    await page.waitForFunction(() => {
      const chips = Array.from(document.querySelectorAll('.bulk-step'));
      return chips.length === 5 && chips[4].className.includes('active');
    }, null, { timeout: 240000 });
    const tExec1 = Date.now();
    log('CLIENT_EXEC_SECONDS=' + ((tExec1 - tExec0) / 1000).toFixed(1));

    const doneHint = await page.evaluate(() => {
      const el = document.querySelector('.bulk-card .bulk-muted');
      return el ? el.textContent : '';
    });
    const resultStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('DONE_HINT=' + String(doneHint || '').replace(/[^\x00-\x7F]/g, '?'));
    log('RESULT_STATS=' + JSON.stringify(resultStats));
  } catch (e) {
    log('PERF_ERR ' + (e.message || '').slice(0, 300));
  } finally {
    log('CONSOLE_ERRS=' + JSON.stringify(errs.slice(0, 4)));
    fs.writeFileSync('D:\\ERPSystem\\_bulk5k_out.txt', out.join('\n'));
    await browser.close();
  }
}
main();