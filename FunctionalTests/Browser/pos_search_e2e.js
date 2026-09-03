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
  await page.waitForTimeout(2500);

  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const hasPos = await page.evaluate(() => !!document.querySelector('.pos-root'));
  log('HAS_POS=' + hasPos);
  if (!hasPos) { log('SKIP'); await browser.close(); return; }

  // 1) اسم النافذة — عدد أزرار المنتجات قبل
  const countBefore = await page.evaluate(() => document.querySelectorAll('.pos-item').length);

  // 2) اكتب جزءاً من اسم منتج (لنفرض "لابتوب" أو "ITM") — نقرأ النص الفعلي للأزرار
  const firstBtn = await page.evaluate(() => {
    const b = document.querySelector('.pos-item .pos-item-name');
    return b ? b.textContent : '';
  }).catch(() => ({}));

  // نفترض: اكتب العبارة العامة "صنف" التي تظهر في أسماء كثيرة، أو "لابتوب" إن وُجد
  const probe = await page.evaluate(() => {
    const names = Array.from(document.querySelectorAll('.pos-item .pos-item-name')).map(e => e.textContent).slice(0, 8);
    return names;
  });
  log('ITEM_NAMES=' + JSON.stringify(probe));

  let searchTerm = 'لابتوب';
  if (!probe.some(n => n.includes('لابتوب'))) searchTerm = probe[0] ? probe[0].slice(0, 4) : 'لابتوب';
  log('USING_SEARCH=' + searchTerm);

  // اكتب في حقل البحث (Immediate)
  const input = page.locator('.pos-search input');
  await input.fill(searchTerm);
  await page.waitForTimeout(1500);
  const countDuring = await page.evaluate(() => document.querySelectorAll('.pos-item').length);
  const inputVal = await input.inputValue();
  log('DURING inputVal=' + inputVal + ' items=' + countDuring + ' (before=' + countBefore + ')');

  // 3) اضغط Enter — يجب أن يُضاف منتج واحد للسلة
  await input.press('Enter');
  await page.waitForTimeout(1500);
  const cartLines = await page.evaluate(() => document.querySelectorAll('.pos-line').length);
  log('AFTER_ENTER cartLines=' + cartLines);

  // 4) اكتب كود صنف (من الأزرار) واضغط Enter
  const codes = await page.evaluate(() => Array.from(document.querySelectorAll('.pos-item')).map(b => b.textContent).slice(0, 5));
  log('BUTTON_TEXTS=' + JSON.stringify(codes));

  const empty = await page.evaluate(() => document.querySelectorAll('.pos-cart-lines .pos-line').length);

  // التقط نص "NoItems" إن صار البحث بلا نتائج
  await page.fill('.pos-search input', 'zzzz_nonexistent');
  await page.waitForTimeout(1200);
  const noItemsText = await page.evaluate(() => {
    const t = document.body.innerText;
    return t.includes('لا توجد أصناف') || t.includes('No items');
  });
  log('NO_ITEMS_EMPTY_STATE=' + noItemsText);
  await page.fill('.pos-search input', '');

  log('RESULT searchLiveWorks=' + (countDuring < countBefore) + ' enterAdds=' + (cartLines > empty) + ' consErrors=' + errs.length);
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_search_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_search_e2e.txt', out.join('\n'), 'ascii'); });