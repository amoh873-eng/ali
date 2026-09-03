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

  // إضافة عدد كبير من الأصناف عبر البحث الفوري (القائمة المنسدلة) لإمتلاء السلة
  const bulk = await page.evaluate(async () => {
    const delay = (ms) => new Promise(r => setTimeout(r, ms));
    const input = document.querySelector('.pos-search input');
    if (!input) return { added: 0 };
    // نقرأ أسماء المنتجات من شبكة المنتجات للبحث بها
    const names = Array.from(document.querySelectorAll('.pos-item .pos-item-name')).map(e => e.textContent).slice(0, 25);
    let added = 0;
    for (const name of names) {
      input.focus();
      // اكتب الاسم واضغط Enter (الذي يضيف أول نتيجة)
      input.value = '';
      input.dispatchEvent(new Event('input', { bubbles: true }));
      await delay(60);
      // MudBlazor Immediate يحتاج حدث input — نكتب عبر native setter
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
      setter.call(input, name.slice(0, 3));
      input.dispatchEvent(new Event('input', { bubbles: true }));
      await delay(120);
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      await delay(160);
      added++;
    }
    return { added, lines: document.querySelectorAll('.pos-cart-lines .pos-line').length };
  });
  log('BULK ' + JSON.stringify(bulk));

  // تحقق: هل القائمة مرّرت لأسفل؟ نقرأ scrollTop وscrollHeight والخط الأخير المرئي
  const scroll = await page.evaluate(() => {
    const el = document.querySelector('.pos-cart-lines');
    if (!el) return null;
    const lines = Array.from(el.querySelectorAll('.pos-line'));
    const lastLine = lines[lines.length - 1];
    const viewBottom = el.getBoundingClientRect().bottom;
    const lastBottom = lastLine ? lastLine.getBoundingClientRect().bottom : 0;
    return {
      scrollTop: Math.round(el.scrollTop),
      scrollHeight: el.scrollHeight,
      clientHeight: el.clientHeight,
      atBottom: Math.abs(el.scrollHeight - el.clientHeight - el.scrollTop) < 4,
      lastLineVisible: lastBottom <= viewBottom + 2,
      lastLineText: lines.length ? lastLine.textContent.replace(/\s+/g, ' ').slice(0, 50) : ''
    };
  });
  log('SCROLL ' + JSON.stringify(scroll));
  log('CONSOLE_ERR=' + errs.length);
  try { await page.screenshot({ path: 'D:/pos_scroll.png', fullPage: true }); } catch (e) {}
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 300)); fs.writeFileSync('D:/ERPSystem/_pos_scroll_e2e.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_pos_scroll_e2e.txt', out.join('\n'), 'ascii'); });