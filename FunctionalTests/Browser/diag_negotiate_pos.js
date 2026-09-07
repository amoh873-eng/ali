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

  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    negotiateReqs.length = 0;
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);

    const cookies = await context.cookies();
    log('POS_AUTHED_COOKIES=' + cookies.filter(c => /Antiforgery|.AspNetCore.Identity/i.test(c.name)).map(c => c.name).join(', '));

    log('--- POS NEGOTIATE REQUEST (authed) ---');
    for (const r of negotiateReqs) {
      log('REQ_' + r.method + ' ' + r.url);
      for (const [k, v] of Object.entries(r.headers)) {
        log('   ' + k + ': ' + String(v).slice(0, 100));
      }
    }
    log('NEGOTIATE_COUNT=' + negotiateReqs.length);

    // هل صفحة pos تحتوي مدخل antiforgery؟
    const posToken = await page.evaluate(() => {
      const t = document.querySelector('input[name="__RequestVerificationToken"]');
      return t ? t.value.slice(0, 20) + '...' : '(none)';
    });
    log('POS_PAGE_TOKEN_INPUT=' + posToken);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_negotiate_pos.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();