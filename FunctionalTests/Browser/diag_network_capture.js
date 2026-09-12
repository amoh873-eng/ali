const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];

function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 700)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();

  const requestLog = [];
  page.on('request', req => {
    if (req.resourceType() === 'document' || req.resourceType() === 'xhr' || req.resourceType() === 'fetch' || req.resourceType() === 'websocket') {
      requestLog.push({ url: req.url().replace(BASE, ''), method: req.method(), type: req.resourceType() });
    }
  });

  const responseLog = [];
  page.on('response', async res => {
    const url = res.url().replace(BASE, '');
    if (res.request().resourceType() === 'xhr' || res.request().resourceType() === 'fetch' || res.request().resourceType() === 'document') {
      let body = '';
      try {
        const buf = await res.body();
        body = buf.toString('utf8').slice(0, 60).replace(/\s+/g, ' ');
      } catch { body = '<no-body-read>'; }
      responseLog.push({
        url,
        status: res.status(),
        ctype: res.headers()['content-type'] || '',
        bodyStart: body
      });
    }
  });

  const pageErrors = [];
  page.on('pageerror', e => {
    pageErrors.push({ message: (e && e.message || '').slice(0, 300), stack: (e && e.stack || '').split('\n').slice(0, 8).join(' || ').slice(0, 800) });
  });

  const consoleMsgs = [];
  page.on('console', m => {
    if (m.type() === 'error' || m.type() === 'warning') {
      consoleMsgs.push({ type: m.type(), text: m.text().slice(0, 400) });
    }
  });

  try {
    // ─── LOAD LOGIN PAGE ───
    log('============= LOADING /login =============');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);

    log('--- PAGE ERRORS (login) ---');
    for (const e of pageErrors) log('PAGEERR=' + e.message + ' | STACK: ' + e.stack);
    log('--- CONSOLE MSGS (login) ---');
    for (const c of consoleMsgs) log('CONSOLE_' + c.type + '=' + c.text);

    log('--- RESPONSES (login) — status != 200 OR html content OR json expected ---');
    for (const r of responseLog) {
      if (r.status >= 400 || (r.ctype && r.ctype.includes('text/html'))) {
        log('RESP[' + r.status + '][' + r.ctype + '] ' + r.url + ' BODY_START=' + r.bodyStart);
      }
    }
    log('ALL_RESPONSES_COUNT=' + responseLog.length);

    const errCountLogin = pageErrors.length;
    pageErrors.length = 0;
    consoleMsgs.length = 0;
    requestLog.length = 0;
    responseLog.length = 0;

    // ─── LOGIN THEN LOAD POS ───
    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    log('============= LOADING /pos =============');
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(3000);

    log('--- PAGE ERRORS (pos) ---');
    for (const e of pageErrors) log('PAGEERR=' + e.message + ' | STACK: ' + e.stack);
    log('--- CONSOLE MSGS (pos) ---');
    for (const c of consoleMsgs) log('CONSOLE_' + c.type + '=' + c.text);

    log('--- NON-2XX / HTML RESPONSES (pos) ---');
    for (const r of responseLog) {
      if (r.status >= 400 || (r.ctype && r.ctype.includes('text/html'))) {
        log('RESP[' + r.status + '][' + r.ctype + '] ' + r.url + ' BODY_START=' + r.bodyStart);
      }
    }
    log('ALL_RESPONSES_COUNT=' + responseLog.length);

    log('ERR_COUNT_LOGIN=' + errCountLogin + ' ERR_COUNT_POS=' + pageErrors.length);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_net_capture.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();