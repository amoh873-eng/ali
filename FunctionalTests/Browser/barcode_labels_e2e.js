const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e && e.message || '').slice(0, 180)));

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    await page.goto(BASE + '/inventory/barcode-labels', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const hasPage = await page.evaluate(() => !!document.querySelector('.bl-wrap'));
    log('PAGE_LOADED=' + hasPage);
    if (!hasPage) { log('SKIP'); return; }

    // حدد أول 3 أصناف عبر جدول
    await page.evaluate(() => {
      const checks = document.querySelectorAll('.bl-table .mud-checkbox');
      for (let i = 0; i < checks.length && i < 3; i++) { const cb = checks[i]; if (cb) { cb.click(); } }
    });
    await page.waitForTimeout(1000);
    const selText = await page.evaluate(() => {
      const el = document.querySelector('.bl-head .bl-muted');
      return el ? el.textContent : '';
    });
    log('SELECTED_STR=' + String(selText).replace(/[^\x00-\x7F]/g, '?'));

    // معاينة الورقة الأولى
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('معاينة'));
      if (b) b.click();
    });
    await page.waitForTimeout(8000);
    const iframeSrc = await page.evaluate(() => {
      const f = document.getElementById('erpLabelPreview');
      return f ? f.getAttribute('src') || '' : '';
    });
    const busyGone = await page.evaluate(() => !document.querySelector('.bl-busy'));
    log('PREVIEW_SRC_PRESENT=' + (iframeSrc.length > 10));
    log('BUSY_CLEARED=' + busyGone);

    // توليد PDF وتنزيل
    const downloadPromise = page.waitForEvent('download', { timeout: 30000 }).catch(() => null);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('توليد'));
      if (b) b.click();
    });
    const download = await downloadPromise;
    if (download) { log('DOWNLOAD_NAME=' + download.suggestedFilename()); }
    else { log('DOWNLOAD_NONE'); }
    await page.waitForTimeout(6000);
  } catch (e) {
    log('E2E_ERR ' + (e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_label_e2e.txt', out.join('\n'));
    await browser.close();
  }
}
main();