const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 150)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/inventory/barcode-labels', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    if (!(await page.evaluate(() => !!document.querySelector('.bl-wrap')))) { log('NO_PAGE'); return; }

    // حدد أول 12 صنفاً
    await page.evaluate(() => {
      const checks = document.querySelectorAll('.bl-table .mud-checkbox');
      for (let i = 0; i < checks.length && i < 12; i++) checks[i].click();
    });
    await page.waitForTimeout(800);

    // KO: فعّل "نسخة واحدة للجميع" واضبطها إلى 10
    await page.evaluate(() => {
      const sw = document.querySelectorAll('.bl-options .mud-switch');
      if (sw[0]) sw[0].click();
    });
    await page.waitForTimeout(500);
    await page.evaluate(() => {
      const inputs = document.querySelectorAll('.bl-options input');
      for (const i of inputs) { if (i.type === 'number') { i.value = '10'; i.dispatchEvent(new Event('change')); } }
    });
    await page.waitForTimeout(800);

    const t0 = Date.now();
    const dl = page.waitForEvent('download', { timeout: 60000 }).catch(() => null);
    await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('.mud-button-root'));
      const b = btns.find(x => x.textContent.includes('توليد'));
      if (b) b.click();
    });
    const download = await dl;
    const ms = Date.now() - t0;
    log('GEN_MS=' + ms + ' SECONDS=' + (ms / 1000).toFixed(2));
    log('DOWNLOAD=' + (download ? download.suggestedFilename() : 'NONE'));
    log('LABELS_EXPECTED=120 (12 items x 10 copies)');

    // التحقق من الحجم للبدء على الدمغ
    const cheerioLike = await page.evaluate(() => {
      const busy = !!document.querySelector('.bl-busy');
      const snack = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim());
      return { busy, snack: snack.map(x => String(x).replace(/[^\x00-\x7F]/g, '?')) };
    });
    log('AFTER=' + JSON.stringify(cheerioLike));
  } catch (e) {
    log('PERF_ERR ' + (e.message || '').slice(0, 200));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_label_perf.txt', out.join('\n'));
    await browser.close();
  }
}
main();