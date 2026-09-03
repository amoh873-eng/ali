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

  // 1) ابحث بحرف واحد — يجب أن تظهر نتائج تبدأ بذلك الحرف فقط (StartsWith)
  const probe = await page.evaluate(() => Array.from(document.querySelectorAll('.pos-item .pos-item-name')).map(e => e.textContent));
  const firstChar = probe[0] ? probe[0].charAt(0) : '؟';
  const input = page.locator('.pos-search input');
  await input.fill(firstChar);
  await page.waitForTimeout(1200);
  const drift = await page.evaluate(() => {
    const opts = Array.from(document.querySelectorAll('.pos-search-option .pos-search-opt-name')).map(e => e.textContent);
    const fc = opts.length ? opts[0].charAt(0) : '';
    return { opts, allStartWith: opts.every(o => o.charAt(0) === fc) };
  });
  log('SEARCH1 firstChar=' + firstChar + ' optCount=' + drift.opts.length + ' allStartWith=' + drift.allStartWith);

  // 2) نفترض أن بعض المنتجات باركود = الكود. ابحث بالكود (يكتمل بالحرف الثاني) واضغط Enter → إضافة مباشرة
  //    اختبر عبر كتابة كود كامل ثم Enter (كمسار باركود عادي)
  const codes = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('.pos-item')).map(function (b) {
      const t = b.textContent.replace(/\s+/g, ' ');
      return t.match(/\d+([.,]\d+)?$/); // يُرجع الجزء الرقمي (السعر) — ليس باركود. نكتفي باستخدام الاسم.
    });
  });
  await input.fill('');
  // اكتب الاسم الكامل لأول منتج واضغط Enter — يجب أن يُضاف للسلة مباشرة
  await input.fill(probe[0]);
  await page.waitForTimeout(800);
  await input.press('Enter');
  await page.waitForTimeout(1500);
  const cartLinesAfter = await page.evaluate(() => document.querySelectorAll('.pos-cart-lines .pos-line').length);
  log('ENTER_ADDS cartLines=' + cartLinesAfter);

  // 3) املا السلة ثم امسحها → يجب أن تعود للأعلى
  await page.evaluate(() => {
    const clearBtn = Array.from(document.querySelectorAll('.pos-cart-header button')).find(b => b.textContent.includes('مسح') || b.textContent.includes('Clear'));
    if (clearBtn) clearBtn.click();
  });
  await page.waitForTimeout(1200);
  const scrollAfterClear = await page.evaluate(() => {
    const el = document.querySelector('.pos-cart-lines');
    return el ? { scrollTop: Math.round(el.scrollTop), lines: el.querySelectorAll('.pos-line').length } : null;
  });
  log('AFTER_CLEAR scrollTop=' + (scrollAfterClear ? scrollAfterClear.scrollTop : null) + ' lines=' + (scrollAfterClear ? scrollAfterClear.lines : null));

  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_final.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_all_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_all_e2e.txt', out.join('\n'), 'ascii'); });