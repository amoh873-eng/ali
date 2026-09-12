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
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 110)); });

  try {
    // ── Login (admin) ──
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 25000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 25000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);

    // ── زر الاستيراد الجماعي في صفحة الأصناف ──
    await page.goto(BASE + '/inventory/items', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const bulkBtnVisible = await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      return btns.some(b => b.textContent.includes('الاستيراد الجماعي'));
    });
    log('BULK_BUTTON_VISIBLE=' + bulkBtnVisible);
    if (!bulkBtnVisible) { log('SKIP_NO_BUTTON'); return; }

    // ── ملف صغير (12 صفاً مع حالات مختلفة) ──
    const rows = [];
    for (let i = 1; i <= 7; i++) rows.push(`صنف اختبار ${i},BULK-SM-${i},${i}.5,${i + 2}.0,مواد خام,قطعة`);
    rows.push(',BULK-SM-8,3,4,مواد خام,قطعة');                        // اسم مفقود
    rows.push('صنف بدون باركود,,1,2,مواد خام,قطعة');                  // باركود مفقود
    rows.push('مكرر الباركود,BULK-SM-1,5,6,مواد خام,قطعة');           // مكرر داخل الملف
    rows.push('سعر سيئ,BULK-SM-11,abc,6,مواد خام,قطعة');              // سعر شراء غير صالح
    rows.push('فئة غير معروفة,BULK-SM-12,7,9,فئة خيالية,قطعة');        // فئة غير معروفة → ملاحظة
    const csv = 'اسم الصنف,الباركود,سعر الشراء,سعر البيع,الفئة,الوحدة\n' + rows.join('\n') + '\n';
    const csvPath = 'D:\\ERPSystem\\bulk_small.csv';
    fs.writeFileSync(csvPath, csv, 'utf8');
    log('CSV_ROWS=' + rows.length);

    // ── الصفحة: رفع ──
    await page.goto(BASE + '/inventory/bulk-import', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    await page.setInputFiles('input[type=file]', csvPath);
    await page.waitForTimeout(3500);
    const parsedOk = await page.evaluate(() => {
      const alerts = Array.from(document.querySelectorAll('.mud-alert'));
      return alerts.some(a => a.textContent.includes('تم تحليل'));
    });
    log('PARSED_OK=' + parsedOk);

    // ── الخطوة 2: تعيين الأعمدة ──
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const next = btns.find(b => b.textContent.includes('التالي'));
      if (next) next.click();
    });
    await page.waitForTimeout(1500);
    const mapInfo = await page.evaluate(() => {
      const selects = Array.from(document.querySelectorAll('.bulk-map-fields .mud-select'));
      const labels = selects.map(s => (s.querySelector('label') || {}).textContent || '');
      const values = selects.map(s => {
        const input = s.querySelector('input');
        return input ? input.value : '';
      });
      return { labels, values };
    });
    log('MAP_SUGGESTED=' + JSON.stringify(mapInfo));
    // â”€â”€ Ø§Ù„Ø®Ø·ÙˆØ© 3: Ø§Ù„Ù…Ø¹Ø§ÙŠÙ†Ø© Ø«Ù… Ø§Ù„ØªÙ†ÙÙŠØ° â”€â”€
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const next = btns.find(b => b.textContent.includes('التالي'));
      if (next) next.click();
    });
    await page.waitForTimeout(2500);

    const summary = await page.evaluate(() => {
      const stats = Array.from(document.querySelectorAll('.bulk-stat'));
      const text = stats.map(s => s.textContent.trim());
      const rows = Array.from(document.querySelectorAll('.bulk-table .mud-table-body .mud-table-row'));
      return { stats: text, sampleRows: rows.length };
    });
    log('PREVIEW_STATS=' + JSON.stringify(summary.stats));
    log('PREVIEW_SAMPLE_ROWS=' + summary.sampleRows);

    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const confirm = btns.find(b => b.textContent.includes('تأكيد الاستيراد'));
      if (confirm) confirm.click();
    });
    await page.waitForTimeout(8000);

    const result = await page.evaluate(() => {
      const stats = Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim());
      const doneHint = document.querySelector('.bulk-card .bulk-muted');
      const failRows = Array.from(document.querySelectorAll('.bulk-table .mud-table-body .mud-table-row'));
      return { stats, doneHint: doneHint ? doneHint.textContent : '', failCount: failRows.length };
    });
    log('RESULT_STATS=' + JSON.stringify(result.stats));
    log('RESULT_DONE_HINT=' + String(result.doneHint || '').replace(/[^\x00-\x7F]/g, '?'));
    log('RESULT_FAILURE_ROWS=' + result.failCount);

    // â”€â”€ Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„Ø§Ø³ØªÙŠØ±Ø§Ø¯: Ø§Ù„ØµÙÙˆÙ Ø³ØªØµØ¨Ø­ "Ù…ÙˆØ¬ÙˆØ¯Ø© ÙÙŠ Ø§Ù„Ù†Ø¸Ø§Ù…" â”€â”€
    await page.goto(BASE + '/inventory/bulk-import', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    await page.setInputFiles('input[type=file]', csvPath);
    await page.waitForTimeout(3500);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const next = btns.find(b => b.textContent.includes('التالي'));
      if (next) next.click();
    });
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const next = btns.find(b => b.textContent.includes('التالي'));
      if (next) next.click();
    });
    await page.waitForTimeout(2500);
    const preview2 = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('PREVIEW2_STATS=' + JSON.stringify(preview2));
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const confirm = btns.find(b => b.textContent.includes('تأكيد الاستيراد'));
      if (confirm) confirm.click();
    });
    await page.waitForTimeout(8000);
    const result2 = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('RESULT2_STATS=' + JSON.stringify(result2));
  } catch (e) {
    log('E2E_ERR ' + (e.message || '').slice(0, 400));
  } finally {
    log('CONSOLE_ERRS=' + JSON.stringify(errs.slice(0, 6)));
    fs.writeFileSync('D:\\ERPSystem\\_bulk_small_out.txt', out.join('\n'));
    await browser.close();
  }
}
main();