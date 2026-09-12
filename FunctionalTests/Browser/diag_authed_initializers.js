const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 300)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    const res = await page.evaluate(async () => {
      const r = await fetch('/_blazor/initializers', { headers: { 'Accept': 'application/json' } });
      const text = await r.text();
      return { status: r.status, ctype: r.headers.get('content-type'), body: text.slice(0, 120) };
    });
    log('AUTHED_INITIALIZERS_RESP=' + JSON.stringify(res));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_blz_authed_body.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();