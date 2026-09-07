const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

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
    await page.goto(BASE + '/purchases/invoices', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(4000);
    await page.evaluate(() => {
      const b = Array.from(document.querySelectorAll('.mud-button-root')).find(x => /فاتورة جديدة/i.test(x.textContent || ''));
      if (b) b.click();
    });
    await page.waitForTimeout(3000);

    await page.locator('#barcodeReceiveToggle').click();
    await page.waitForTimeout(1500);
    const scanLoc = page.locator('input[placeholder*="امسح"]');

    // محاكاة ماسح حقيقي: الحقل متمركز، كتابة الأحرف بسرعة ثم Enter، 5 مرات لنفس الصنف
    const CODE = '6210000000073'; // فولدر ملفات - جديد
    const scanLoc0 = page.locator('input[placeholder*="امسح"]');
    await scanLoc0.click();
    for (let i = 0; i < 5; i++) {
      await scanLoc0.fill('');
      await page.keyboard.type(CODE, { delay: 5 });
      await page.keyboard.press('Enter');
      await page.waitForTimeout(450);
    }
    await page.waitForTimeout(1200);

    // احفظ
    await page.evaluate(() => {
      const b = Array.from(document.querySelectorAll('.mud-dialog .mud-button-root')).find(x => /حفظ الفاتورة|Save invoice|حفظ/i.test(x.textContent || '') && !/إضافة|Add/i.test(x.textContent || ''));
      if (b) b.click();
    });
    await page.waitForTimeout(3500);
    const dlgOpen = await page.locator('.mud-dialog').count();
    log('DLG_OPEN_AFTER_SAVE=' + dlgOpen);
    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));

    // إن وُجد خطأ في الحوار
    if (dlgOpen > 0) {
      const raw = await page.evaluate(() => { const d = document.querySelector('.mud-dialog'); return d ? d.innerText : 'NO_DLG'; });
      fs.writeFileSync('D:/ERPSystem/_rapid_save_raw.txt', raw, 'utf8');
      const alerts = await page.locator('.mud-dialog .mud-alert').allInnerTexts();
      log('DLG_ALERTS=' + JSON.stringify(alerts));
    }
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_rapid_result.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();