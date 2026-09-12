const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 250)));
  try {
    // Login
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // ── 1) تحقق: عدد أزرار POS الافتراضي = 30 ──
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);
    const countButtons = () => page.evaluate(() => document.querySelectorAll('.pos-item').length);
    const initial = await countButtons();
    log('STEP1_INITIAL_BUTTONS=' + initial + ' (expected 30)');

    // ── 2) غيّر الإعداد إلى 15 من صفحة الإعدادات ثم أعد فتح POS ──
    await page.goto(BASE + '/settings', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);

    // ابحث عن الحقل الذي قيمته 30 (MudNumericField) واضبطه إلى 15
    const setGrid = async (value) => {
      await page.evaluate((v) => {
        const inputs = Array.from(document.querySelectorAll('input.mud-input-slot'));
        const target = inputs.find(i => i.type === 'number' && i.value === '30' || i.value === String(v));
        // الأكثر أماناً: آخر حقل رقمي هو PosGridLimit
        const numeric = inputs.filter(i => i.type === 'number');
        const el = numeric[numeric.length - 1];
        if (!el) return false;
        el.value = String(v);
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
      }, value);
      // انتظر الالتقاط ثم احفظ
      await page.waitForTimeout(600);
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.mud-button-root'));
        const saveBtn = btns.find(b => /حفظ حد الشبكة|Save grid limit/.test(b.innerText));
        if (saveBtn) saveBtn.click();
      });
      await page.waitForTimeout(1500);
    };

    await setGrid('15');
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(6000);
    const fifteen = await countButtons();
    log('STEP2_AFTER_SET_15_BUTTONS=' + fifteen + ' (expected 15) — no rebuild needed');

    // ── 3) غيّره إلى كبير (500) وتحقق من الأداء التدريجي وعدم كسر الأنماط ──
    await page.goto(BASE + '/settings', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(3000);
    await setGrid('500');
    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    const t500 = Date.now();
    await page.waitForTimeout(8000);
    const big = await countButtons();
    const ms500 = Date.now() - t500;
    log('STEP3_AFTER_SET_500_BUTTONS=' + big + ' (expected >=500) loadMs=' + ms500);
    log('STEP3_GRID_CACHED_RESULT_OK=' + (big >= 500));

    // ── 4) فحص التزامن: إتمام عملية بيع سريعاً (يؤكد _busy يعمل ولا خطأ DbContext) ──
    await page.evaluate(() => { const b = document.querySelector('.pos-item'); if (b) b.click(); });
    await page.waitForTimeout(600);
    await page.evaluate(() => {
      const cash = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot')).find(i => i.type !== 'hidden');
      if (cash) { cash.value = '9999'; cash.dispatchEvent(new Event('input', { bubbles: true })); }
    });
    await page.waitForTimeout(400);
    await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
    await page.waitForTimeout(5000);
    log('STEP4_DIALOGS_OR_DONE=' + await page.evaluate(() => document.querySelectorAll('.mud-dialog').length > 0));
    log('STEP4_HAS_SECOND_OP_ERROR=' + pageErrors.some(e => /second operation/i.test(String(e))));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 400));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_limit_test.txt', out.join('\n'));
    await browser.close();
  }
})();