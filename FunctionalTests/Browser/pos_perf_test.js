const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

async function loadPosButtons(page) {
  await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
  const t0 = Date.now();
  await page.waitForTimeout(6000);
  const n = await page.evaluate(() => document.querySelectorAll('.pos-item').length);
  return { n, ms: Date.now() - t0 };
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2000);

    const r30 = await loadPosButtons(page);
    log('LIMIT_30_BUTTONS=' + r30.n + ' LOAD_MS=' + r30.ms);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_perf_test.txt', out.join('\n'));
    await browser.close();
  }
})();