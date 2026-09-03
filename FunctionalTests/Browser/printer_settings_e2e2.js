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

  // فتح الإعدادات ثم إعدادات الطابعة
  await page.evaluate(() => document.querySelector('.pos-topbar-right .mud-icon-button')?.click());
  await page.waitForTimeout(1200);
  const printerBtn = await page.evaluate(() => {
    const dlg = document.querySelector('.mud-dialog');
    if (!dlg) return false;
    const b = Array.from(dlg.querySelectorAll('button')).find(x => x.textContent.includes('\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u0627\u0644\u0637\u0627\u0628\u0639\u0629'));
    if (!b) return false;
    b.click(); return true;
  });
  await page.waitForTimeout(1500);
  log('PRINTER_BTN=' + printerBtn);

  // حوار الطابعة = آخر حوار في الصفحة (الأعمق)
  const filled = await page.evaluate(() => {
    const dialogs = document.querySelectorAll('.mud-dialog');
    const dlg = dialogs[dialogs.length - 1]; // الأخير = نافذة الطابعة
    if (!dlg) return 'NO_DLG';
    const inputs = dlg.querySelectorAll('input');
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
    if (inputs[0]) { setter.call(inputs[0], 'النسر الذهبي للتجارة'); inputs[0].dispatchEvent(new Event('input', { bubbles: true })); }
    if (inputs[1]) { setter.call(inputs[1], '0791234567'); inputs[1].dispatchEvent(new Event('input', { bubbles: true })); }
    if (inputs[2]) { setter.call(inputs[2], 'شكراً لزيارتكم'); inputs[2].dispatchEvent(new Event('input', { bubbles: true })); }
    return 'FILLED inputs=' + inputs.length;
  });
  log('FILL=' + filled);

  // فتح قائمة المقاس واختيار 58
  const paperOpen = await page.evaluate(() => {
    const dialogs = document.querySelectorAll('.mud-dialog');
    const dlg = dialogs[dialogs.length - 1];
    const sel = dlg ? dlg.querySelector('.mud-select, .mud-input-control') : null;
    if (!sel) return 'NO_SEL';
    sel.querySelector('.mud-input')?.click(); // يفتح القائمة
    return 'OPENED';
  });
  await page.waitForTimeout(900);
  const paperPick = await page.evaluate(() => {
    const items = Array.from(document.querySelectorAll('.mud-list-item, .mud-menu-item, [class*=mud-list-item]'));
    const target = items.find(x => x.textContent.includes('58'));
    if (!target) return 'NO_58 items=' + items.length;
    target.click();
    return 'PICKED_58';
  });
  await page.waitForTimeout(900);
  log('PAPER open=' + paperOpen + ' pick=' + paperPick);

  // حفظ: زر حفظ في حوار الطابعة (الأخير)
  const saved = await page.evaluate(() => {
    const dialogs = document.querySelectorAll('.mud-dialog');
    const dlg = dialogs[dialogs.length - 1];
    if (!dlg) return 'NO_DLG2';
    const btns = Array.from(dlg.querySelectorAll('.mud-dialog-actions button'));
    const save = btns.find(b => b.textContent.includes('\u062d\u0641\u0638') || b.textContent.includes('Save'));
    if (!save) return 'NO_SAVE_BTN';
    save.click(); return 'SAVED';
  });
  await page.waitForTimeout(1500);
  const ls = await page.evaluate(() => localStorage.getItem('posPrinterSettings'));
  log('SAVE=' + saved + ' LS=' + ls);
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_printer2.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_printer2.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_printer2.txt', out.join('\n'), 'ascii'); });