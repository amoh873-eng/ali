// Dedicated test: F-key shortcuts pressed DURING an in-progress checkout (_busy == true).
// Each scenario runs on a FRESH login + page so no cross-scenario state can leak in.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

async function freshPage(browser) {
  const page = await browser.newPage();
  const errs = [];
  page.on('pageerror', e => errs.push((e && e.message || '').slice(0, 200)));
  await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
    page.click('button.login-btn')
  ]);
  await page.waitForTimeout(2500);
  await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(2500);
  return { page, errs };
}

async function cartCount(page) { return page.evaluate(() => document.querySelectorAll('.pos-line').length); }
async function snacks(page) { return page.evaluate(() => Array.from(document.querySelectorAll('.mud-snackbar')).map(s => (s.textContent || '').trim())); }

async function addItem(page, code) {
  await page.evaluate(() => {
    const s = document.querySelector('.pos-search input');
    if (s) { const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set; if (set) set.call(s, ''); s.dispatchEvent(new Event('input', { bubbles: true })); }
  });
  await page.waitForTimeout(300);
  await page.fill('.pos-search input', code);
  await page.waitForTimeout(400);
  await page.keyboard.press('Enter');
  await page.waitForTimeout(1300);
  if ((await cartCount(page)) === 0) {
    await page.fill('.pos-search input', code); await page.waitForTimeout(400); await page.keyboard.press('Enter'); await page.waitForTimeout(1500);
  }
  return cartCount(page);
}

async function setCash(page, val) {
  await page.evaluate((v) => {
    const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
    if (cash) { cash.value = v; cash.dispatchEvent(new Event('input', { bubbles: true })); cash.dispatchEvent(new Event('change', { bubbles: true })); }
  }, val);
  await page.waitForTimeout(500);
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  try {
    // ── A) F6 + F7 أثناء _busy → لا يُفتح حوار، والبيع يكتمل ──
    {
      const { page, errs } = await freshPage(browser);
      await addItem(page, '10010');
      await setCash(page, '99999');
      await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
      await page.keyboard.press('F6');
      await page.keyboard.press('F7');
      await page.waitForTimeout(4500);
      const ret = await page.evaluate(() => !!document.querySelector('.pos-return-inv input'));
      const trn = await page.evaluate(() => !!document.querySelector('.pos-transfer-amt input'));
      const recA = await page.evaluate(() => !!document.querySelector('.pos-receipt'));
      log('BUSY_F6_NO_DIALOG=' + (!ret));
      log('BUSY_F7_NO_DIALOG=' + (!trn));
      log('BUSY_F6F7_SALE_DONE=' + recA);
      log('BUSY_A_ERRS=' + JSON.stringify(errs));
      await page.close();
    }
    // ── B) F4 أثناء _busy → بلا أثر، والبيع يبقى نقدياً وسليماً ──
    {
      const { page, errs } = await freshPage(browser);
      await addItem(page, '10010');
      await setCash(page, '99999');
      await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
      await page.waitForFunction(() => { const b = document.querySelector('.pos-checkout'); return b && b.disabled; }, null, { timeout: 8000 }).catch(() => log('BUSY_B_WINDOW_TIMEOUT'));
      await page.keyboard.press('F4'); // بطاقة أثناء إتمام بيع نقدي — يجب أن تُهمَل
      await page.waitForTimeout(5000);
      const recB = await page.evaluate(() => !!document.querySelector('.pos-receipt'));
      const recText = await page.evaluate(() => { const dlg = document.querySelector('.pos-receipt'); return dlg ? dlg.innerText : ''; });
      log('BUSY_F4_SALE_DONE=' + recB);
      log('BUSY_F4_SALE_IS_CASH=' + /نقدي|Cash/.test(recText));
      log('BUSY_F4_SNACKS=' + JSON.stringify(await snacks(page)));
      log('BUSY_B_ERRS=' + JSON.stringify(errs));
      await page.close();
    }
    // ── C) F3 أثناء _busy → بلا أثر، والبيع يكتمل ──
    {
      const { page, errs } = await freshPage(browser);
      await addItem(page, '10010');
      await setCash(page, '99999');
      await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
      await page.waitForFunction(() => { const b = document.querySelector('.pos-checkout'); return b && b.disabled; }, null, { timeout: 8000 }).catch(() => log('BUSY_C_WINDOW_TIMEOUT'));
      await page.keyboard.press('F3'); // نقدي أثناء الإتمام — يجب أن تُهمَل
      await page.waitForTimeout(5000);
      const recC = await page.evaluate(() => !!document.querySelector('.pos-receipt'));
      log('BUSY_F3_SALE_DONE=' + recC);
      log('BUSY_F3_SNACKS=' + JSON.stringify(await snacks(page)));
      log('BUSY_C_ERRS=' + JSON.stringify(errs));
      await page.close();
    }
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_shortcuts_busy_e2e.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();
