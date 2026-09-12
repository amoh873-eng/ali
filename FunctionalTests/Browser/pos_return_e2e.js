// End-to-end test (Feature 1): Sales return directly from POS.
// 1) Make a cash POS sale of 3 units of an item.
// 2) Open the POS Return dialog, look up the invoice by its number.
// 3) Partial-return 1 unit -> stock restored + reversing journal entry posted by the existing service.
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
    log('LOGIN_URL=' + page.url());

    // ── 1) بيع 3 وحدات نقداً من نقطة البيع ──
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2500);

    await page.fill('.pos-search input', '6210000000471');
    await page.waitForTimeout(600);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(900);

    await page.evaluate(() => {
      const box = document.querySelector('.pos-line .pos-qty-box');
      if (!box) return;
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
      setter.call(box, '3');
      box.dispatchEvent(new Event('input', { bubbles: true }));
      box.dispatchEvent(new Event('change', { bubbles: true }));
    });
    await page.waitForTimeout(900);

    await page.evaluate(() => {
      const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
      if (cash) {
        cash.value = '99999';
        cash.dispatchEvent(new Event('input', { bubbles: true }));
        cash.dispatchEvent(new Event('change', { bubbles: true }));
      }
    });
    await page.waitForTimeout(500);
    await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
    await page.waitForTimeout(5000);

    const receiptText = await page.evaluate(() => {
      const dlg = document.querySelector('.mud-dialog-content .pos-receipt');
      return dlg ? dlg.innerText : '';
    });
    const m = receiptText.match(/SI-[\w-]+/);
    const invoiceNumber = m ? m[0] : '';
    log('SALE_INVOICE=' + invoiceNumber);
    log('SALE_RECEIPT_SHOWN=' + (receiptText.length > 0));
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-dialog-actions button'));
      const cancel = btns.find(b => !b.classList.contains('erp-print-btn'));
      if (cancel) cancel.click();
    });
    // انتظر حتى يُعاد تمكين أزرار الأكشن (ينتهي _busy بعد إغلاق حوار الإيصال + LoadAsync)
    await page.waitForFunction(() => {
      const b = document.querySelector('.pos-action-return');
      return b && !b.disabled && !document.querySelector('.mud-dialog');
    }, null, { timeout: 20000 }).catch(() => log('RETURN_BTN_ENABLE_TIMEOUT'));

    // ── 2) فتح حوار المردود + البحث برقم الفاتورة ──
    await page.evaluate(() => { const b = document.querySelector('.pos-action-return'); if (b) b.scrollIntoView({ block: 'center' }); });
    await page.waitForTimeout(600);
    await page.click('.pos-action-return', { timeout: 15000 });
    await page.waitForSelector('.pos-return-inv input', { timeout: 15000 });
    log('RETURN_DIALOG_OPENED=true');
    await page.waitForSelector('.pos-return-inv input', { timeout: 15000 });
    log('RETURN_DIALOG_OPENED=' + true);

    await page.fill('.pos-return-inv input', invoiceNumber);
    await page.waitForTimeout(1000);
    const invInputVal = await page.inputValue('.pos-return-inv input').catch(() => '');
    log('INV_INPUT_VALUE=' + invInputVal);
    await page.click('.pos-return-search-btn');
    await page.waitForTimeout(3000);

    // اختيار صف الفاتورة المطابقة (أي صف يحتوي رقم الفاتورة)
    const rowLoc = '.pos-return-results .mud-table-row:has-text("' + invoiceNumber + '")';
    const rowCount = await page.$$(rowLoc).then(r => r.length).catch(() => 0);
    log('RETURN_MATCHING_ROWS=' + rowCount);
    if (rowCount > 0) {
      await page.click(rowLoc);
      await page.waitForTimeout(3000);

      const linesRows = await page.$$('.pos-return-lines tbody .mud-table-row, .pos-return-lines .mud-table-row').then(r => r.length).catch(() => 0);
      log('RETURN_LINES_ROWS=' + linesRows);
      const maxQtyShown = await page.evaluate(() => {
        const rows = document.querySelectorAll('.pos-return-lines .mud-table-row');
        const cells = rows.length > 0 ? rows[0].querySelectorAll('td') : [];
        return cells.length > 1 ? cells[1].textContent.trim() : '';
      });
      log('RETURN_SOLD_QTY_COL=' + maxQtyShown);

      await page.evaluate(() => {
        const num = document.querySelector('.pos-return-lines input.mud-input-slot');
        if (!num) return;
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        setter.call(num, '1');
        num.dispatchEvent(new Event('input', { bubbles: true }));
        num.dispatchEvent(new Event('change', { bubbles: true }));
      });
      await page.waitForTimeout(1000);

      const saveBtn = await page.$('.pos-return-save');
      const disabled = saveBtn ? await saveBtn.isDisabled() : true;
      log('RETURN_SAVE_ENABLED=' + (!disabled));
      if (!disabled) {
        await saveBtn.click();
        await page.waitForTimeout(5000);
      }
    }

    const snacks = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim()));
    log('SNACKS=' + JSON.stringify(snacks));
    log('RETURN_SAVED_SNACK=' + snacks.some(s => s.includes('تم ترحيل المردود')));
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_return_e2e.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();
