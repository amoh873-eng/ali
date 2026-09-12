const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const context = await browser.newContext();
  const page = await context.newPage();

  const negotiateResp = [];
  page.on('response', res => {
    if (res.url().includes('_blazor/negotiate')) {
      negotiateResp.push({ url: res.url().replace(BASE, ''), status: res.status() });
    }
  });

  try {
    // AUTHED POS: هل negotiate فعلاً 200؟
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    negotiateResp.length = 0;
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);
    log('POS_NEGOTIATE_STATUSES=' + JSON.stringify(negotiateResp.map(r => r.status)));

    // تجربة fetch مباشرة بـ fetch بدون رمز وبالرمز على /login (anon context جديد؟ لا - هذا authed)
    // الآن context جديد anon:
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_negotiate_status.txt', out.join('\n'), 'utf8');
    await browser.close();
  }

  // ANON context
  const browser2 = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page2 = await browser2.newPage();
  const out2 = [];
  try {
    await page2.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page2.waitForTimeout(1500);
    const probe = await page2.evaluate(async () => {
      // خذ الرمز من الصفحة
      const t = document.querySelector('input[name="__RequestVerificationToken"]');
      const token = t ? t.value : null;
      const tryReq = async (label, opts) => {
        try {
          const r = await fetch('/_blazor/negotiate?negotiateVersion=1', opts);
          return label + ' => ' + r.status;
        } catch (e) { return label + ' => ERR ' + e.message; }
      };
      const a = await tryReq('PLAIN', { method: 'POST', headers: { 'x-requested-with': 'XMLHttpRequest' } });
      const b = token ? await tryReq('WITH_HEADER', { method: 'POST', headers: { 'x-requested-with': 'XMLHttpRequest', 'RequestVerificationToken': token } }) : 'NO_TOKEN';
      const c = token ? await tryReq('WITH_FORM', { method: 'POST', headers: { 'content-type': 'application/x-www-form-urlencoded' }, body: '__RequestVerificationToken=' + encodeURIComponent(token) }) : 'NO_TOKEN';
      return JSON.stringify({ a, b, c, hasToken: !!token });
    });
    out2.push('ANON_NEGOTIATE_PROBE=' + probe);
  } catch (e) {
    out2.push('FATAL2=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_negotiate_probe.txt', out2.join('\n'), 'utf8');
    await browser2.close();
  }
})();