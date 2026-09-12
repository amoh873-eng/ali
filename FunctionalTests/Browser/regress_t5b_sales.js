const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function b64(s) { return Buffer.from(String(s || ''), 'utf8').toString('base64'); }
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 300)));

  const invoiceNumbers = [];
  const snacksRaw = [];

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // 3 بيعات متتابعة سريعة
    for (let sale = 1; sale <= 3; sale++) {
      await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
      await page.waitForTimeout(2000);
      pageErrors.length = 0; // أصفّر أخطاء الصفحة قبل كل عملية

      // أضف أول صنف متاح (سلعة عادية)
      const added = await page.evaluate(() => {
        const items = document.querySelectorAll('.pos-item');
        // اختر صنفاً ليس نافداً إن أمكن
        let el = null;
        for (const it of items) { if (!it.className.includes('pos-item--out')) { el = it; break; } }
        if (!el) el = items[0];
        if (!el) return false;
        el.click();
        return true;
      });
      await page.waitForTimeout(600);

      // المبلغ المستلم كبير (نقدي)
      await page.evaluate(() => {
        const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
        if (cash) {
          cash.value = '99999';
          cash.dispatchEvent(new Event('input', { bubbles: true }));
          cash.dispatchEvent(new Event('change', { bubbles: true }));
        }
      });
      await page.waitForTimeout(400);
      await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
      await page.waitForTimeout(6000);

      // التقاط رقم الفاتورة من Snackbar
      const snackTexts = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim()));
      log('SALE_' + sale + '_SNACKS=' + JSON.stringify(snackTexts.map(b64)));
      const rawSnacks = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent));
      snacksRaw.push(rawSnacks.join(' | '));

      const m = rawSnacks.join(' ').match(/SI-\d{8}-\d{4}/);
      if (m) invoiceNumbers.push(m[0]);

      const secondOpLocal = pageErrors.some(e => /second operation/i.test(String(e)));
      log('SALE_' + sale + '_SECOND_OP=' + secondOpLocal + ' PAGE_ERRORS=' + pageErrors.length);

      // أعد فتح صفحة جديدة لبدء بيع جديد
      await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(1500);
    }

    log('INVOICE_NUMBERS=' + JSON.stringify(invoiceNumbers));
    log('TOTAL_SALES_SNIPPET=' + snacksRaw.map(s => b64(s)).join(' ### '));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    log('ALL_PAGE_ERRORS=' + JSON.stringify(pageErrors));
    fs.writeFileSync('D:/ERPSystem/_t5_part2.txt', out.join('\n'));
    fs.writeFileSync('D:/ERPSystem/_t5_snacks_raw.txt', snacksRaw.join('\n'), 'utf8');
    await browser.close();
  }
})();