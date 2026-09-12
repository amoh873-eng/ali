const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

// باركود حقيقية من كتالوج الأصناف (استُخرجت من القاعدة)
const SCANS = ['6210000000471', '6210000000018', '6210000000199', '6210000000463', '6210000000073'];
const RAPID_ITEM = '6210000000471'; // سنمسحه 3 مرات متتالية سريعة

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/purchases/invoices', { waitUntil: 'domcontentloaded', timeout: 60000 }).catch(async () => {
      // بعض المشاريع تستخدم مسار آخر — جرب الجذر
      await page.goto(BASE + '/purchases', { waitUntil: 'domcontentloaded', timeout: 60000 });
    });
    await page.waitForTimeout(3500);

    // زر فاتورة جديدة
    const clickNew = await page.evaluate(() => {
      const b = Array.from(document.querySelectorAll('.mud-button-root')).find(x => /فاتورة جديدة|New Invoice|New invoice/i.test(x.textContent || ''));
      if (b) { b.click(); return true; }
      return false;
    });
    log('NEW_INVOICE_BTN=' + clickNew);
    if (!clickNew) { log('NO_NEW_INVOICE_BTN'); return; }
    await page.waitForTimeout(2500);

    // تفعيل وضع الاستلام بالباركود — زر MudButton (OnClick يعمل عبر الدائرة التفاعلية)
    const toggleCount = await page.locator('#barcodeReceiveToggle').count();
    log('RECEIVE_TOGGLE_FOUND=' + toggleCount);
    if (toggleCount === 0) { log('NO_RECEIVE_TOGGLE'); return; }
    await page.locator('#barcodeReceiveToggle').click();
    await page.waitForTimeout(1500);
    const scanLoc = page.locator('input[placeholder*="امسح"]');
    log('SCAN_INPUT_VISIBLE=' + await scanLoc.count());
    if (await scanLoc.count() === 0) {
      log('TOGGLE_FAILED');
      return;
    }

    // دالة مسح (تحاكي الماسح: الحقل متمركز، أحداث مفاتيح سريعة ثم Enter — عبر ممتص الطفرة)
    async function scan(code) {
      const scanLoc2 = page.locator('input[placeholder*="امسح"]');
      await scanLoc2.fill('');
      await page.keyboard.type(code, { delay: 5 });
      await page.keyboard.press('Enter');
      await page.waitForTimeout(450);
    }

    // 1) المسح السريع المتكرر لنفس الصنف أولاً (4 مرات — يجب أن يتجمع في سطر واحد بكمية 4)
    for (let i = 0; i < 4; i++) { await scan(RAPID_ITEM); }
    // 2) مسح 3 أصناف أخرى مختلفة
    for (const c of SCANS.slice(1, 4)) { await scan(c); }
    // 3) باركود غير معروف
    await scan('9999999999999');
    await page.waitForTimeout(1200);

    // قراءة البنود المعروضة (الصنف + الكمية) من حقول القائمة
    const linesInfo = await page.evaluate(() => {
      const dialog = document.querySelector('.mud-dialog');
      if (!dialog) return { dlg: false };
      const selects = Array.from(dialog.querySelectorAll('.mud-select input'));
      const qtyInputs = Array.from(dialog.querySelectorAll('.mud-numeric input, input[inputmode="decimal"]'));
      // في MudNumericField النوع mud-input-slot
      const numerics = Array.from(dialog.querySelectorAll('input.mud-input-slot')).filter(i => i.type === 'number');
      return { dlg: true, selectCount: selects.length, numericCount: numerics.length };
    });
    log('LINES_INFO=' + JSON.stringify(linesInfo));

    // قراءة أكثر تحديداً: عدد صفوف البنود وحقول قيم الكمية (MudNumericField تنتج input)
    const rows = await page.evaluate(() => {
      const dialog = document.querySelector('.mud-dialog');
      if (!dialog) return [];
      const grids = Array.from(dialog.querySelectorAll('.mud-grid'));
      // البنود = كل MudGrid داخل DialogContent بخلاف الرأس؛ نقيس عدد قيم NumericField المرتبطة بالكمية
      return null;
    });

    // سلوك الأدق: نقرأ نص الحوار كاملاً ونستخرج أسطر الأصناف من قيم Select (display)
    const dialogHtml = await page.evaluate(() => {
      const d = document.querySelector('.mud-dialog');
      return d ? d.innerText : '';
    });
    log('DIALOG_TEXT_SNIPPET=' + dialogHtml.replace(/\s+/g, ' ').slice(0, 600));

    // رسالة آخر مسح/غير معروف
    const hasUnknownMsg = dialogHtml.includes('غير معروف');
    log('HAS_UNKNOWN_BARCODE_MSG=' + hasUnknownMsg);

    // زر الحفظ: احفظ الفاتورة (المطابقة الدقيقة لزر SaveInvoice — لا «إضافة بند»)
    const saved = await page.evaluate(() => {
      const b = Array.from(document.querySelectorAll('.mud-dialog .mud-button-root')).find(x => /حفظ الفاتورة|Save invoice|حفظ/i.test(x.textContent || '') && !/إضافة|Add/i.test(x.textContent || ''));
      if (b) { b.click(); return true; }
      return false;
    });
    log('SAVE_CLICKED=' + saved);
    await page.waitForTimeout(4000);

    const postState = await page.evaluate(() => {
      // هل أُغلق الحوار؟ هل ظهر خطأ؟
      const dlg = document.querySelector('.mud-dialog');
      const snacks = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent);
      return { dialogOpen: !!dlg, snacks };
    });
    log('POST_STATE=' + JSON.stringify(postState));

    // لو الحوار ما زال مفتوحاً، التقط النص الخام + التنبيهات
    if (postState.dialogOpen) {
      const raw = await page.evaluate(() => {
        const d = document.querySelector('.mud-dialog');
        return d ? d.innerText : 'NO_DLG';
      });
      fs.writeFileSync('D:/ERPSystem/_multi_save_raw.txt', raw, 'utf8');
      const alerts = await page.locator('.mud-dialog .mud-alert').allInnerTexts();
      log('DLG_ALERTS=' + JSON.stringify(alerts));
    }
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_br_test_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();