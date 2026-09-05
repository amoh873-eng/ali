const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 300)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 200)));
  page.on('console', m => { if (m.type() === 'error') log('CONSOLEERR: ' + (m.text() || '').slice(0, 150)); });
  page.on('response', r => { if (r.status() >= 400) log('HTTP ' + r.status() + ' ' + r.url().slice(0, 120)); });
  try {
    log('1 goto login');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    log('2 login page url=' + page.url());
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);
    log('3 after login url=' + page.url());
    log('4 goto labels');
    await page.goto(BASE + '/inventory/barcode-labels', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);
    log('5 url=' + page.url());
    const info = await page.evaluate(() => {
      const text = (document.body ? document.body.innerText : '').slice(0, 400);
      const hasWrap = !!document.querySelector('.bl-wrap');
      const title = document.title;
      return { hasWrap, title, text };
    });
    log('HAS_WRAP=' + info.hasWrap);
    log('TITLE=' + info.title);
    log('BODYTEXT=' + info.text.replace(/\s+/g, ' ').slice(0, 400));
  } catch (e) {
    log('FATAL: ' + (e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_label_probe.txt', out.join('\n'));
    await browser.close();
  }
})();