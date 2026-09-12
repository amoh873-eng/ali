const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

(async () => {
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

    // توليد ملف 500 صف مع حالات مقصودة
    const rows = [];
    // 450 صنفاً جديداً سليماً
    for (let i = 1; i <= 450; i++) {
      rows.push(`صنف تراجع شامل ${i},REGRESS-${String(i).padStart(5, '0')},${(i * 0.5).toFixed(2)},${(i * 0.75).toFixed(2)},مواد خام,قطعة`);
    }
    // 20 صفاً بباركود مكرر داخل الملف (10 قيم مكررة × 2)
    for (let k = 1; k <= 10; k++) {
      rows.push(`مكرر الباركود أ ${k},REGRESS-DUP-${k},3,4,مواد خام,قطعة`);
      rows.push(`مكرر الباركود ب ${k},REGRESS-DUP-${k},5,6,مواد خام,قطعة`);
    }
    // 15 صفاً بلا سعر (سعر الشراء والبيع فارغان)
    for (let k = 1; k <= 15; k++) {
      rows.push(`صنف بدون سعر ${k},REGRESS-NOPRICE-${k},,,مواد خام,قطعة`);
    }
    // 15 صفاً بباركود موجود مسبقاً في النظام (ينبغي عدّه "موجود")
    for (let k = 1; k <= 15; k++) {
      rows.push(`تحديث صنف ${k},BULK5K-${String(k).padStart(5, '0')},${(k * 1.5).toFixed(2)},${(k * 2.5).toFixed(2)},مواد خام,قطعة`);
    }

    const csv = 'اسم الصنف,الباركود,سعر الشراء,سعر البيع,الفئة,الوحدة\n' + rows.join('\n') + '\n';
    const csvPath = 'D:\\ERPSystem\\regress500.csv';
    fs.writeFileSync(csvPath, csv, 'utf8');
    log('T4_FILE_ROWS=' + rows.length);

    await page.goto(BASE + '/inventory/bulk-import', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    const tUpload0 = Date.now();
    await page.setInputFiles('input[type=file]', csvPath);
    await page.waitForFunction(() => {
      const alerts = Array.from(document.querySelectorAll('.mud-alert'));
      return alerts.some(a => a.textContent.includes('تم تحليل'));
    }, null, { timeout: 60000 });
    const tUpload1 = Date.now();
    log('T4_UPLOAD_PARSE_SECONDS=' + ((tUpload1 - tUpload0) / 1000).toFixed(1));

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
    await page.waitForFunction(() => document.querySelectorAll('.bulk-stat').length >= 4, null, { timeout: 60000 });
    const previewStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('T4_PREVIEW_STATS=' + JSON.stringify(previewStats));

    const tExec0 = Date.now();
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const confirm = btns.find(b => b.textContent.includes('تأكيد الاستيراد'));
      if (confirm) confirm.click();
    });
    // انتظار وصول الخطوة 5 (التقرير) — قد يطول مع 500 صف
    await page.waitForFunction(() => {
      const chips = Array.from(document.querySelectorAll('.bulk-step'));
      return chips.length === 5 && chips[4].className.includes('active');
    }, null, { timeout: 240000 }).catch(() => log('T4_TIMEOUT_WAITING_REPORT'));
    const tExec1 = Date.now();
    log('T4_EXEC_SECONDS=' + ((tExec1 - tExec0) / 1000).toFixed(1));

    await page.waitForTimeout(1500);
    const resultStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    const doneHint = await page.evaluate(() => {
      const el = document.querySelector('.bulk-card .bulk-muted');
      return el ? el.textContent : '';
    });
    log('T4_RESULT_STATS=' + JSON.stringify(resultStats));
    log('T4_DONE_HINT=' + String(doneHint || '').replace(/[^\x00-\x7F]/g, '?'));

    // قراءة جدول الأسباب إن وُجد
    const reasons = await page.evaluate(() => {
      const rows2 = Array.from(document.querySelectorAll('.bulk-table .mud-table-body .mud-table-row'));
      return rows2.slice(0, 15).map(r => (r.innerText || '').replace(/\s+/g, ' ').trim());
    });
    log('T4_FAILURE_SAMPLE=' + JSON.stringify(reasons));

    await page.screenshot({ path: 'D:/ERPSystem/_t4_bulk_report.png' });
  } catch (e) {
    log('T4_ERR ' + (e.message || '').slice(0, 400));
  } finally {
    log('T4_CONSOLE_ERRS=' + JSON.stringify(errs.slice(0, 4)));
    fs.writeFileSync('D:\\ERPSystem\\_t4_bulk_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();