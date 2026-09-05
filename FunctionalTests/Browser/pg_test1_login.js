const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const context = await browser.newContext(); // جلسة جديدة نظيفة تماماً
  const page = await context.newPage();
  const consoleErrors = [];
  const pageErrors = [];
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', e => pageErrors.push((e && e.message) || ''));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    const loginTitle = await page.title();
    log('LOGIN_TITLE=' + loginTitle);

    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.click('button.login-btn')
    ]);
    await page.waitForTimeout(3500);
    const url = page.url();
    log('AFTER_LOGIN_URL=' + url);
    // فحص ظهور أي نص خطأ antiforgery/form على الصفحة
    const bodyText = await page.evaluate(() => (document.body ? document.body.innerText : ''));
    const antiforgeryHit = /antiforgery|validation|مطلوب|خطأ|Incorrect|failed/i.test(bodyText) && url.includes('login');
    log('LOGIN_LANDED_ON_DASHBOARD=' + (url === BASE + '/' || url === BASE + '/index' || url === BASE));
    log('ANTIFORGERY_ERROR_ON_PAGE=' + antiforgeryHit);
    log('CONSOLE_ERROR_COUNT=' + consoleErrors.length);
    for (const e of consoleErrors.slice(0, 5)) log('CONSOLE_ERR=' + e);
    log('PAGE_ERROR_COUNT=' + pageErrors.length);
    for (const e of pageErrors.slice(0, 5)) log('PAGE_ERR=' + e);

    // تحقق أن الجلسة فعلاً معمّدة: افتح صفحة محمية
    await page.goto(BASE + '/inventory/items', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);
    const itemsLoaded = await page.evaluate(() => document.querySelectorAll('.mud-table-row').length);
    log('PROTECTED_PAGE_ROWS=' + itemsLoaded);
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pg_test1_login.txt', out.join('\n'));
    await browser.close();
  }
})();