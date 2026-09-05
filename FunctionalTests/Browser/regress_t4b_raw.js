const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
function log(s) { out.push(String(s)); }
const out = [];

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);

    await page.goto(BASE + '/inventory/bulk-import', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    await page.setInputFiles('input[type=file]', 'D:\\ERPSystem\\regress500.csv');
    await page.waitForFunction(() => {
      const alerts = Array.from(document.querySelectorAll('.mud-alert'));
      return alerts.some(a => a.textContent.includes('تم تحليل'));
    }, null, { timeout: 60000 });
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('التالي'));
      if (b) b.click();
    });
    await page.waitForTimeout(1500);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('التالي'));
      if (b) b.click();
    });
    await page.waitForFunction(() => document.querySelectorAll('.bulk-stat').length >= 4, null, { timeout: 60000 });
    const previewStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    log('PREVIEW=' + JSON.stringify(previewStats));

    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const confirm = btns.find(b => b.textContent.includes('تأكيد الاستيراد'));
      if (confirm) confirm.click();
    });
    await page.waitForFunction(() => {
      const chips = Array.from(document.querySelectorAll('.bulk-step'));
      return chips.length === 5 && chips[4].className.includes('active');
    }, null, { timeout: 240000 }).catch(() => log('TIMEOUT'));
    await page.waitForTimeout(1500);
    const resultStats = await page.evaluate(() => Array.from(document.querySelectorAll('.bulk-stat')).map(s => s.textContent.trim()));
    const doneHint = await page.evaluate(() => {
      const el = document.querySelector('.bulk-card .bulk-muted');
      return el ? el.textContent : '';
    });
    log('RESULT=' + JSON.stringify(resultStats));
    log('DONE_HINT=' + (doneHint || ''));
  } catch (e) {
    log('ERR=' + (e && e.message || ''));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_t4_raw_ar.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();