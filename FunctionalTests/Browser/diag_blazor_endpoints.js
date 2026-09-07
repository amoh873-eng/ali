const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const context = await browser.newContext();
  const page = await context.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 200)));

  const interesting = [];
  page.on('response', async res => {
    const url = res.url().replace(BASE, '');
    // only blazor endpoints + any non-2xx
    if (url.includes('_blazor') || res.status() >= 400) {
      let body = '';
      try { const b = await res.body(); body = b.toString('utf8').slice(0, 80).replace(/\s+/g, ' '); }
      catch { body = '<err>'; }
      interesting.push({ url, status: res.status(), ctype: res.headers()['content-type'] || '', body });
    }
  });

  try {
    // 1) Fresh login page
    log('=== FRESH /login (no cookies) ===');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2000);
    log('PAGEERR_COUNT=' + pageErrors.length);
    for (const e of pageErrors) log('  PAGEERR: ' + e);
    log('BLZ_AND_NON2XX_RESPONSES_FRESH_LOGIN:');
    for (const r of interesting) log('  RESP[' + r.status + '][' + r.ctype + '] ' + r.url + ' B=' + r.body);

    // 2) Login, then reload /pos with full page load
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    interesting.length = 0;
    pageErrors.length = 0;

    log('=== AUTHED full-load /pos ===');
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);
    log('PAGEERR_COUNT=' + pageErrors.length);
    for (const e of pageErrors) log('  PAGEERR: ' + e);
    log('BLZ_AND_NON2XX_RESPONSES_AUTHED_POS:');
    for (const r of interesting) log('  RESP[' + r.status + '][' + r.ctype + '] ' + r.url + ' B=' + r.body);

    // 3) AUTHED full-load /settings (another full reload)
    interesting.length = 0;
    pageErrors.length = 0;
    log('=== AUTHED full-load /settings ===');
    await page.goto(BASE + '/settings', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);
    log('PAGEERR_COUNT=' + pageErrors.length);
    for (const e of pageErrors) log('  PAGEERR: ' + e);
    log('BLZ_AND_NON2XX_RESPONSES_AUTHED_SETTINGS:');
    for (const r of interesting) log('  RESP[' + r.status + '][' + r.ctype + '] ' + r.url + ' B=' + r.body);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_blz_capture.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();