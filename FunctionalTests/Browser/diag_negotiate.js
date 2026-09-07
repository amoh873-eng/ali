const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const context = await browser.newContext();
  const page = await context.newPage();

  const negotiateReqs = [];
  page.on('request', req => {
    if (req.url().includes('_blazor/negotiate')) {
      negotiateReqs.push({ url: req.url().replace(BASE, ''), method: req.method(), headers: req.headers() });
    }
  });
  const negotiateResps = [];
  page.on('response', res => {
    if (res.url().includes('_blazor/negotiate')) {
      negotiateResps.push({ url: res.url().replace(BASE, ''), status: res.status(), headers: res.headers() });
    }
  });

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2000);

    // هل توجد قيمة antiforgery في الصفحة؟
    const pageHasToken = await page.evaluate(() => {
      const inputs = Array.from(document.querySelectorAll('input[name="__RequestVerificationToken"]'));
      return inputs.map(i => i.value.slice(0, 24) + '...').length;
    });
    const cookies = await context.cookies();
    log('LOGIN_PAGE_TOKEN_INPUTS=' + pageHasToken);
    log('COOKIES=' + cookies.map(c => c.name + '=' + (c.value || '').slice(0, 30)).join(', '));

    log('--- NEGOTIATE REQUESTS ---');
    for (const r of negotiateReqs) {
      log('REQ_' + r.method + ' ' + r.url);
      for (const [k, v] of Object.entries(r.headers)) {
        if (/x-request|antiforg|csrf|verification|cookie/i.test(k)) {
          log('   ' + k + ': ' + String(v).slice(0, 120));
        }
      }
    }
    log('--- NEGOTIATE RESPONSES ---');
    for (const r of negotiateResps) {
      log('RESP ' + r.status + ' ' + r.url);
      for (const [k, v] of Object.entries(r.headers)) {
        if (/content-type|set-cookie/i.test(k)) log('   ' + k + ': ' + String(v).slice(0, 120));
      }
    }
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_negotiate_capture.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();