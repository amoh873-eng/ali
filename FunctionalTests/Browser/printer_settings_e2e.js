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

  // فتح الإعدادات
  await page.evaluate(() => document.querySelector('.pos-topbar-right .mud-icon-button')?.click());
  await page.waitForTimeout(1200);

  // النقر على زر "إعدادات الطابعة" داخل الحوار
  const printerBtn = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    if (!dlg) return false;
    const btns = Array.from(dlg.querySelectorAll('button'));
    const b = btns.find(x => x.textContent.includes('\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u0627\u0644\u0637\u0627\u0628\u0639\u0629') || x.textContent.includes('Printer Settings'));
    if (!b) return false;
    b.click(); return true;
  });
  await page.waitForTimeout(1500);
  log('PRINTER_BTN=' + printerBtn);

  // تعبئة الحقول
  const filled = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    if (!dlg) return false;
    const inputs = dlg.querySelectorAll('input, textarea');
    const shop = inputs[0]; const phone = inputs[1]; const thanks = inputs[2];
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
    if (shop) { setter.call(shop, 'النسر الذهبي للتجارة'); shop.dispatchEvent(new Event('input', { bubbles: true })); }
    if (phone) { setter.call(phone, '0791234567'); phone.dispatchEvent(new Event('input', { bubbles: true })); }
    if (thanks) { setter.call(thanks, 'شكراً لزيارتكم'); thanks.dispatchEvent(new Event('input', { bubbles: true })); }
    return true;
  });
  // اختيار 58 في القائمة المنسدلة (اختر آخر / الأول) — نختار 58 عبر فتح القائمة
  const paperSel = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    const sel = dlg ? dlg.querySelector('.mud-select') : null;
    if (!sel) return 'NO_SELECT';
    sel.click(); return 'OPENED';
  });
  await page.waitForTimeout(900);
  const paperChosen = await page.evaluate(() => {
    const items = Array.from(document.querySelectorAll('.mud-list-item, .mud-menu-item'));
    const target = items.find(x => x.textContent.includes('58') || x.textContent.includes('58mm'));
    if (!target) return 'NO_58';
    target.click(); return 'PICKED_58';
  });
  await page.waitForTimeout(900);
  log('FILL=' + filled + ' PAPER=' + paperSel + ' PAPER58=' + paperChosen);

  // حفظ الحوار
  const saved = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    const btns = Array.from(dlg.querySelectorAll('.mud-dialog-actions button'));
    const save = btns.find(b => b.textContent.includes('\u062d\u0641\u0638') || b.textContent.includes('Save'));
    if (!save) return false;
    save.click(); return true;
  });
  await page.waitForTimeout(1200);

  // تحقق من localStorage
  const ls = await page.evaluate(() => localStorage.getItem('posPrinterSettings'));
  log('SAVED=' + saved + ' LS=' + ls);

  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_printer_settings.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_printer_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_printer_e2e.txt', out.join('\n'), 'ascii'); });