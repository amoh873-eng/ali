// Stress test: deliberately fire concurrent event-handler invocations that share the
// scoped Blazor DbContext, to verify the re-entry guards prevent the
// "A second operation was started on this context instance" error.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.click('button.login-btn')
    ]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2500);

    // ── 1) حوار المردود: بحث + نقر صفوف متزامن ──
    await page.click('.pos-action-return');
    await page.waitForSelector('.pos-return-inv input', { timeout: 15000 });
    await page.click('.pos-return-search-btn'); // بحث بلا مرشّحات → أحدث الفواتير المرحّلة
    await page.waitForTimeout(2500);

    // نقر بحث متزامن 3 مرات
    await Promise.all([
      page.click('.pos-return-search-btn').catch(() => {}),
      page.click('.pos-return-search-btn').catch(() => {}),
      page.click('.pos-return-search-btn').catch(() => {})
    ]);
    await page.waitForTimeout(1500);

    // نقر أول صف بيانات 3 مرات متزامنة (يكشف أي تعارض DbContext)
    const rowLoc = '.pos-return-results tbody .mud-table-row';
    const rowCount = await page.$$(rowLoc).then(r => r.length).catch(() => 0);
    log('STRESS_ROWS=' + rowCount);
    if (rowCount > 0) {
      const row = await page.$(rowLoc);
      await Promise.all([
        row.click().catch(() => {}),
        row.click().catch(() => {}),
        row.click().catch(() => {})
      ]);
      await page.waitForTimeout(4000);
    }

    // هل ما زال الحوار يعمل (بدون خطأ أحمر)؟
    const errAlert = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.mud-alert')).some(a => (a.textContent || '').includes('ثانٍ') || (a.textContent || '').includes('A second operation')));
    const snacks = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim()));
    log('STRESS_ERR_ALERT=' + errAlert);
    log('STRESS_SNACKS=' + JSON.stringify(snacks));
    // أغلق الحوار
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-dialog-actions button'));
      const cancel = btns.find(b => !b.classList.contains('pos-return-save'));
      if (cancel) cancel.click();
    });
    await page.waitForTimeout(1500);

    // ── 2) حوار التحويل: نقرة حفظ مزدوجة متزامنة ──
    await page.click('.pos-action-cash');
    await page.waitForSelector('.pos-transfer-amt input', { timeout: 15000 });
    await page.fill('.pos-transfer-amt input', '7');
    await page.waitForTimeout(800);
    const save = await page.$('.pos-transfer-save');
    await Promise.all([
      save.click().catch(() => {}),
      save.click().catch(() => {}),
      save.click().catch(() => {})
    ]);
    await page.waitForTimeout(4000);

    // عدد عمليات التحويل بقيمة 7 في السجل (يجب أن تكون 1 وليس 2-3)
    await page.goto(BASE + '/pos/cash-transfer', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);
    const body = await page.evaluate(() => (document.body ? document.body.innerText : ''));
    const amt7Count = (body.match(/7\.00/g) || []).length;
    log('STRESS_AMT7_ROWS=' + amt7Count);
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_dbcontext_stress.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();
