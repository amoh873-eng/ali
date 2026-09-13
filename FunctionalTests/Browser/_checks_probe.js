const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(s); }

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
    page.click('button.login-btn'),
  ]);
  await page.waitForTimeout(2500);

  await page.goto(BASE + '/finance/checks', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const homeText = await page.evaluate(() => document.body.innerText);
  log('CHECKS_PAGE_HEADER=' + (homeText.includes('الشيكات البنكية') || homeText.includes('Bank Checks')));
  log('CHECKS_REGISTER_BTN=' + (homeText.includes('تسجيل شيك جديد') || homeText.includes('Register New Check')));

  // افتح حوار التسجيل
  const opened = await page.evaluate(() => {
    const btn = Array.from(document.querySelectorAll('button')).find(b => (b.textContent || '').includes('تسجيل شيك جديد') || (b.textContent || '').includes('Register New Check'));
    if (btn) { btn.click(); return true; }
    return false;
  });
  await page.waitForTimeout(2500);
  const dialogHtml = await page.evaluate(() => {
    const dlg = Array.from(document.querySelectorAll('.mud-dialog, .mdc-dialog, [role="dialog"]')).map(d => d.outerHTML).join('\n---\n');
    return dlg ? dlg.slice(0, 14000) : '(no dialog @ ' + document.body.innerText.slice(0, 1200) + ')';
  });
  fs.writeFileSync('d:/ERPSystem/_chk_dom.txt', dialogHtml, 'utf8');
  log('OPENED=' + opened);
  await browser.close();
}
main().catch(e => {
  log('FATAL: ' + (e.stack || e.message));
  fs.writeFileSync('d:/ERPSystem/_chk_e2e1.txt', out.join('\n'), 'utf8');
  process.exit(1);
}).then(() => {
  fs.writeFileSync('d:/ERPSystem/_chk_e2e1.txt', out.join('\n'), 'utf8');
});