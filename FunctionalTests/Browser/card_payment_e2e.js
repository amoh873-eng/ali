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
    // ── Login ──
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 25000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 25000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // ── POS ──
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const hasPos = await page.evaluate(() => !!document.querySelector('.pos-root'));
    log('HAS_POS=' + hasPos);
    if (hasPos) {
      const firstItem = await page.evaluate(() => {
        const b = document.querySelector('.pos-item .pos-item-name');
        const p = document.querySelector('.pos-item .pos-item-price');
        return { name: b ? b.textContent : '', price: p ? parseFloat(p.textContent.replace(/[^0-9.]/g, '')) : 0 };
      });
      log('FIRST_ITEM=' + JSON.stringify(firstItem));
      await page.click('.pos-item');
      await page.waitForTimeout(1200);

      // الدفع بالبطاقة
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.pos-pay'));
        const card = btns.find(b => b.textContent.includes('بطاقة'));
        if (card) card.click();
      });
      await page.waitForTimeout(1200);
      const cardFieldsVisible = await page.evaluate(() => !!document.querySelector('.pos-card-fields'));
      log('CARD_FIELDS_VISIBLE=' + cardFieldsVisible);

      // إتمام بدون رقم موافقة → تحذير فقط
      await page.click('.pos-checkout');
      await page.waitForTimeout(1500);
      const noRefSnack = await page.evaluate(() => !!document.querySelector('.mud-snackbar'));
      const cartLines = await page.evaluate(() => document.querySelectorAll('.pos-line').length);
      log('NO_REF_BLOCKED=' + noRefSnack + ' cartLines=' + cartLines);

      // إدخال رقم الموافقة + آخر 4 أرقام
      const ref = 'CARD-TEST-' + Date.now().toString().slice(-6);
      const inputs = page.locator('.pos-card-fields input');
      await inputs.nth(0).fill(ref);
      await inputs.nth(1).fill('4242');
      await page.waitForTimeout(400);

      // اختيار الشبكة Visa
      await page.evaluate(() => {
        const sel = document.querySelector('.pos-card-fields .mud-select');
        if (sel) sel.querySelector('.mud-select-input').click();
      });
      await page.waitForTimeout(1000);
      const listCount = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-list')).length);
      const listTexts = await page.evaluate(() => Array.from(document.querySelectorAll('.mud-list-item')).map(i => i.textContent.trim()));
      log('MUD_LIST_COUNT=' + listCount + ' ITEMS=' + JSON.stringify(listTexts).slice(0, 120));
      await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.mud-list-item'));
        const visa = items.find(i => i.textContent.includes('Visa'));
        if (visa) visa.click();
      });
      await page.waitForTimeout(1000);
      const networkInputVal = await page.evaluate(() => {
        const sel = document.querySelector('.pos-card-fields .mud-select');
        return sel ? (sel.querySelector('input') || {}).value || '' : 'NO_SEL';
      });
      log('NETWORK_INPUT_VAL=' + networkInputVal);

      // إتمام البيع
      await page.click('.pos-checkout');
      await page.waitForTimeout(4000);
      const receiptShown = await page.evaluate(() => !!document.querySelector('.pos-receipt'));
      const receiptText = await page.evaluate(() => {
        const r = document.querySelector('.pos-receipt');
        return r ? r.textContent : '';
      });
      log('RECEIPT_SHOWN=' + receiptShown);
      log('RECEIPT_HAS_REF=' + receiptText.includes(ref));
      const totalVal = await page.evaluate(() => {
        const spans = Array.from(document.querySelectorAll('.rc-grand span'));
        return spans.length >= 2 ? spans[spans.length - 1].textContent.trim() : '';
      });
      log('GRAND_TOTAL=' + totalVal);
      log('SALE_REF=' + ref);

      // إغلاق الإيصال
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.mud-dialog button'));
        const ok = btns.find(b => b.textContent.includes('Cancel') || b.textContent.includes('إلغاء') || b.textContent.includes('رجوع'));
        if (ok) ok.click();
      });
      await page.waitForTimeout(1500);

      // ── صفحة التسوية: العملية تظهر بانتظار تأكيد البنك ──
      await page.goto(BASE + '/pos/card-reconciliation', { waitUntil: 'networkidle' });
      await page.waitForTimeout(4000);
      const hasRecon = await page.evaluate(() => !!document.querySelector('.recon-page'));
      log('HAS_RECON=' + hasRecon);
      const pendingBadges = await page.evaluate(() => Array.from(document.querySelectorAll('.recon-badge.warn')).map(b => b.textContent.trim()));
      log('PENDING_BADGES=' + JSON.stringify(pendingBadges));
      const rowBeforeImport = await page.evaluate((r) => {
        const rows = Array.from(document.querySelectorAll('.mud-table-body .mud-table-row'));
        const hit = rows.find(x => x.textContent.includes(r));
        return hit ? hit.textContent : '';
      }, ref);
      log('ROW_BEFORE_IMPORT=' + rowBeforeImport.slice(0, 200));

      // ── استيراد كشف البنك (CSV) بنفس المرجع والمبلغ — بتاريخ اليوم ضمن نطاق الفترة ──
      // ملاحظة: الإجمالي قد يظهر بفاصل عربي (10٫44) — نُعيد تحويله لنقطة ASCII
      const amount = totalVal.replace(/٫/g, '.').replace(/[^\d.]/g, '').trim();
      const dateToday = new Date().toISOString().slice(0, 10);
      const csv = 'Reference,Amount,Date\n' + ref + ',' + amount + ',' + dateToday + '\n';
      const csvPath = 'D:\\ERPSystem\\bank_card_test.csv';
      fs.writeFileSync(csvPath, csv, 'utf8');
      log('CSV_CONTENT={' + csv.trim().replace(/\n/g, '|') + '}');

      await page.setInputFiles('#bankStatementFile', csvPath);
      await page.waitForTimeout(4000);

      const matchedBadges = await page.evaluate(() => Array.from(document.querySelectorAll('.recon-badge.ok')).map(b => b.textContent.trim()));
      const warnAfter = await page.evaluate(() => Array.from(document.querySelectorAll('.recon-badge.warn')).map(b => b.textContent.trim()));
      const rowAfterImport = await page.evaluate((r) => {
        const rows = Array.from(document.querySelectorAll('.mud-table-body .mud-table-row'));
        const hit = rows.find(x => x.textContent.includes(r));
        return hit ? hit.textContent : '';
      }, ref);
      log('AFTER_IMPORT_MATCHED=' + JSON.stringify(matchedBadges));
      log('AFTER_IMPORT_WARN=' + JSON.stringify(warnAfter));
      log('ROW_AFTER_IMPORT=' + rowAfterImport.slice(0, 200));
    }
  } catch (e) {
    log('E2E_ERR ' + (e.message || '').slice(0, 400));
  } finally {
    log('CONSOLE_ERRS=' + JSON.stringify(errs.slice(0, 8)));
    fs.writeFileSync('D:\\ERPSystem\\_card_e2e_out.txt', out.join('\n'));
    await browser.close();
  }
}
main();