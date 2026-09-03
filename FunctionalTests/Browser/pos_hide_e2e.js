const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 150)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 110)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  // تعليم ما إذا كنا على /pos أم رفض وصول
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  let url = page.url();
  const hasPosLayout = await page.evaluate(() => !!document.querySelector('.pos-root'));
  log('POS_URL=' + url + ' HAS_POS=' + hasPosLayout);

  if (!hasPosLayout) {
    log('SKIP: no pos access with smoke@erp.com; trying admin@erp.com');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'admin@erp.com', { timeout: 20000 });
    await page.fill('input[name="Password"]', 'Admin@1234', { timeout: 20000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    url = page.url();
  }

  const before = await page.evaluate(() => {
    const items = document.querySelector('.pos-items');
    const cart = document.querySelector('.pos-cart');
    return {
      hasPos: !!items,
      itemsDisplay: items ? getComputedStyle(items).display : 'none',
      cartWidth: cart ? Math.round(cart.getBoundingClientRect().width) : -1,
      cartRight: cart ? Math.round(cart.querySelector('.pos-cart-right').getBoundingClientRect().width) : -1
    };
  });
  log('BEFORE itemsDisplay=' + before.itemsDisplay + ' cartW=' + before.cartWidth + ' cartRightW=' + before.cartRight);

  // فتح الإعدادات
  const gearClicked = await page.evaluate(() => {
    const b = document.querySelector('.pos-topbar-right .mud-icon-button');
    if (!b) return false;
    b.click(); return true;
  });
  await page.waitForTimeout(1500);
  log('GEAR=' + gearClicked);

  const switchFound = await page.evaluate(() => {
    const labels = Array.from(document.querySelectorAll('.mud-switch,.mud-switch-label')).length;
    const txt = document.querySelector('.mud-dialog') ? document.querySelector('.mud-dialog').innerText : '';
    return { switches: labels, hasText: txt.includes('\u0625\u062e\u0641\u0627\u0621 \u0627\u0644\u0645\u0646\u062a\u062c\u0627\u062a') || txt.includes('Hide products') };
  });
  log('SWITCH labels=' + switchFound.switches + ' hasHideText=' + switchFound.hasText);

  // تفعيل المفتاح (آخر مفتاح في الحوار) ثم حفظ
  const toggled = await page.evaluate(() => {
    const sw = document.querySelectorAll('.mud-switch');
    if (!sw.length) return 'NO_SWITCH';
    const last = sw[sw.length - 1];
    last.click();
    return 'CLICKED';
  });
  await page.waitForTimeout(800);
  log('TOGGLE=' + toggled);

  // زر الحفظ (نص "حفظ"/"Save") — أزرار الحوار
  const saved = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    if (!dlg) return false;
    const btns = Array.from(dlg.querySelectorAll('button'));
    const save = btns.find(b => b.innerText.includes('\u062d\u0641\u0638') || b.innerText.includes('Save'));
    if (!save) return false;
    save.click(); return true;
  });
  await page.waitForTimeout(2500);
  log('SAVE=' + saved);

  const after = await page.evaluate(() => {
    const items = document.querySelector('.pos-items');
    const cart = document.querySelector('.pos-cart');
    return {
      posRootClass: document.querySelector('.pos-root') ? document.querySelector('.pos-root').className : '',
      itemsDisplay: items ? getComputedStyle(items).display : 'none',
      cartWidth: cart ? Math.round(cart.getBoundingClientRect().width) : -1,
      cartRight: cart ? Math.round(cart.querySelector('.pos-cart-right').getBoundingClientRect().width) : -1
    };
  });
  log('AFTER rootClass=' + after.posRootClass + ' itemsDisplay=' + after.itemsDisplay + ' cartW=' + after.cartWidth + ' cartRightW=' + after.cartRight);
  log('CONSOLE_ERR=' + errs.length + (errs[0] ? (' FIRST=' + errs[0]) : ''));
  try { await page.screenshot({ path: 'D:/pos_hide_test.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_e2e.txt', out.join('\n'), 'ascii'); });