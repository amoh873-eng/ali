const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 200)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);
    // انتقل لصفحة الأصناف لفتح القائمة العلوية للمخزون
    await page.goto(BASE + '/inventory/items', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);
    const res = await page.evaluate(() => {
      const links = Array.from(document.querySelectorAll('nav a'));
      const inv = links.find(a => a.href && a.href.includes('/inventory/items'));
      const labels = links.find(a => a.href && a.href.includes('/inventory/barcode-labels'));
      return {
        total: links.length,
        hasInventory: !!inv,
        hasBarcodeLabels: !!labels,
        labelsText: labels ? labels.textContent.trim() : ''
      };
    });
    log('HAS_INVENTORY_LINK=' + res.hasInventory);
    log('HAS_BARCODE_LABELS_LINK=' + res.hasBarcodeLabels);
    log('LABELS_TEXT=' + res.labelsText);

    // تحقق أن النقر على الرابط يفتح الشاشة
    await page.evaluate(() => {
      const links = Array.from(document.querySelectorAll('nav a'));
      const labels = links.find(a => a.href && a.href.includes('/inventory/barcode-labels'));
      if (labels) labels.click();
    });
    await page.waitForTimeout(4000);
    const hasWrap = await page.evaluate(() => !!document.querySelector('.bl-wrap'));
    log('PAGE_OPENED_AFTER_CLICK=' + hasWrap);
    log('FINAL_URL=' + page.url());
  } catch (e) {
    log('FATAL: ' + (e.message || '').slice(0, 250));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_nav_check.txt', out.join('\n'));
    await browser.close();
  }
})();