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
  const printerBtn = await page.locator('.mud-dialog button').filter({ hasText: '\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u0627\u0644\u0637\u0627\u0628\u0639\u0629' }).first().click().then(() => true).catch(() => false);
  await page.waitForTimeout(1500);
  log('PRINTER_BTN=' + printerBtn);

  // الحوار الأخير = نافذة الطابعة — املأ حقوله بـ Playwright fill (أحداث صحيحة)
  const dialogs = page.locator('.mud-dialog');
  const countD = await dialogs.count();
  const printerDlg = dialogs.nth(countD - 1);
  const inputs = printerDlg.locator('input');
  const n = await inputs.count();
  if (n >= 3) {
    await inputs.nth(0).fill('\u0627\u0644\u0646\u0633\u0631 \u0627\u0644\u0630\u0647\u0628\u064a \u0644\u0644\u062a\u062c\u0627\u0631\u0629');
    await inputs.nth(1).fill('0791234567');
    await inputs.nth(2).fill('\u0634\u0643\u0631\u0627\u064b \u0644\u0632\u064a\u0627\u0631\u062a\u0643\u0645');
    log('FILLED inputs=' + n);
  } else log('FILL_NONE inputs=' + n);

  // فتح قائمة المقاس (آخر MudSelect في الحوار) — نضغط عليه
  const selects = printerDlg.locator('.mud-select');
  const sCount = await selects.count();
  if (sCount > 0) { await selects.nth(sCount - 1).click(); } else log('NO_SELECT');
  await page.waitForTimeout(900);

  // ابحث عن عنصر القائمة الذي يحتوي 58 (في كل الصفحة لأن MudBlazor popover)
  const item58 = page.locator('.mud-list-item, .mud-menu-item').filter({ hasText: '58' }).first();
  const picked = await item58.click().then(() => 'PICKED_58').catch(() => {
    // حاول مجدداً بفتح مختلفة
    return 'NO_58';
  });
  await page.waitForTimeout(900);
  log('PAPER=' + picked);

  // اضغط زر الحفظ في حوار الطابعة (الأخير)
  const actions = printerDlg.locator('.mud-dialog-actions button');
  const aCount = await actions.count();
  let saveRes = 'NO_SAVE';
  for (let i = 0; i < aCount; i++) {
    const txt = (await actions.nth(i).innerText()) || '';
    if (txt.includes('\u062d\u0641\u0638') || txt.includes('Save')) { await actions.nth(i).click(); saveRes = 'SAVED'; break; }
  }
  await page.waitForTimeout(1500);
  log('SAVE=' + saveRes);
  const ls = await page.evaluate(() => localStorage.getItem('posPrinterSettings'));
  log('LS=' + ls);
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_printer3.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_printer3.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_printer3.txt', out.join('\n'), 'ascii'); });