const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/inventory/barcode-labels', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(5000);
    await page.evaluate(() => {
      const checks = document.querySelectorAll('.bl-table .mud-checkbox');
      for (let i = 0; i < checks.length && i < 3; i++) checks[i].click();
    });
    await page.waitForTimeout(800);
    log('click preview');
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('معاينة'));
      if (b) b.click();
    });
    await page.waitForTimeout(8000);
    const res = await page.evaluate(() => {
      const f = document.getElementById('erpLabelPreview');
      const snack = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim());
      return { exists: !!f, srcProp: f ? (f.src || '').slice(0, 60) : '', snack: snack };
    });
    log('IFRAME_EXISTS=' + res.exists);
    log('SRC_PROP=' + res.srcProp);
    log('SNACK=' + res.snack.join(';'));
  } catch (e) {
    log('FATAL: ' + (e.message || '').slice(0, 200));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_label_preview.txt', out.join('\n'));
    await browser.close();
  }
})();