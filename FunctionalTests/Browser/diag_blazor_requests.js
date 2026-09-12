const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const context = await browser.newContext();
  const page = await context.newPage();

  const reqs = [];
  page.on('request', req => {
    const u = req.url().replace(BASE, '');
    if (u.includes('_blazor') || u.includes('/login')) {
      reqs.push({ url: u, method: req.method(), headers: req.headers() });
    }
  });

  try {
    log('=== FRESH /login requests involving _blazor ===');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2000);
    for (const r of reqs) {
      const accept = (r.headers['accept'] || '').slice(0, 60);
      log('REQ[' + r.method + '] ' + r.url + (accept ? ' ACCEPT=' + accept : ''));
    }
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_blz_requests.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();