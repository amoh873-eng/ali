const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  page.on('pageerror', e => log('PAGEERR: ' + (e.message || '').slice(0, 120)));
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push((m.text() || '').slice(0, 100)); });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);

  // 1) هيكل السلة: أول عنصر بعد header يجب أن يكون pos-cart-lines مباشرةً
  const cartProbe = await page.evaluate(() => {
    const left = document.querySelector('.pos-cart-left');
    if (!left) return null;
    const kids = Array.from(left.children).map(c => c.className);
    return { kids, secondIsLines: kids[1] === 'pos-cart-lines' };
  });
  log('CART kids=' + JSON.stringify(cartProbe ? cartProbe.kids : null) + ' secondIsLines=' + (cartProbe ? cartProbe.secondIsLines : null));

  // 2) العميل والخصم مخفيان افتراضياً
  const hidden = await page.evaluate(() => {
    const hasAutocomplete = !!document.querySelector('.pos-cart-left .mud-autocomplete');
    const hasDiscount = !!document.querySelector('.pos-cart-left .pos-discount');
    return { hasCustomer: hasAutocomplete, hasDiscount };
  });
  log('HIDDEN customer=' + hidden.hasCustomer + ' discount=' + hidden.hasDiscount);

  // 3) البحث: اكتب حرفاً واحداً → تظهر القائمة المنسدلة
  const input = page.locator('.pos-search input');
  await input.click();
  await input.fill('ل'); // أول حرف
  await page.waitForTimeout(1200);
  const dropdown = await page.evaluate(() => {
    const d = document.querySelector('.pos-search-dropdown');
    return { visible: !!d, options: d ? d.querySelectorAll('.pos-search-option').length : 0 };
  });
  log('DROPDOWN after 1 char visible=' + dropdown.visible + ' options=' + dropdown.options);

  // انقر أول خيار → يُضاف للسلة
  const clicked = await page.evaluate(() => {
    const opt = document.querySelector('.pos-search-option');
    if (!opt) return false;
    opt.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    return true;
  });
  await page.waitForTimeout(1500);
  const cartLines = await page.evaluate(() => document.querySelectorAll('.pos-cart-lines .pos-line').length);
  log('ADDED cartLines=' + cartLines + ' click=' + clicked);

  // 4) الإعدادات: 3 مفاتيح (إخفاء المنتجات/العميل/الخصم)
  await page.evaluate(() => {
    const b = document.querySelector('.pos-topbar-right .mud-icon-button');
    if (b) b.click();
  });
  await page.waitForTimeout(1200);
  const settings = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    const switches = dlg ? dlg.querySelectorAll('.mud-switch').length : 0;
    const txt = dlg ? dlg.innerText : '';
    return { switches, hasCustomer: txt.includes('\u0625\u062e\u0641\u0627\u0621 \u062d\u0642\u0644 \u0627\u0644\u0639\u0645\u064a\u0644'), hasDiscount: txt.includes('\u0625\u062e\u0641\u0627\u0621 \u062d\u0642\u0644 \u0627\u0644\u062e\u0635\u0645') };
  });
  log('SETTINGS switches=' + settings.switches + ' hasCustomerSwitch=' + settings.hasCustomer + ' hasDiscountSwitch=' + settings.hasDiscount);

  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_cart_fix.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_cart_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_cart_e2e.txt', out.join('\n'), 'ascii'); });