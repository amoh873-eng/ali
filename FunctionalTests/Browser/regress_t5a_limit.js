const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function b64(s) { return Buffer.from(String(s || ''), 'utf8').toString('base64'); }
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 300)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // دالة ضبط حد الشبكة من /settings
    async function setGridLimit(value) {
      await page.goto(BASE + '/settings', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(3000);
      const ok = await page.evaluate((v) => {
        const numeric = Array.from(document.querySelectorAll('input.mud-input-slot')).filter(i => i.type === 'number');
        const el = numeric[numeric.length - 1];
        if (!el) return false;
        el.value = String(v);
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
      }, value);
      await page.waitForTimeout(600);
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.mud-button-root'));
        const saveBtn = btns.find(b => /حفظ حد الشبكة|Save grid limit/.test(b.innerText));
        if (saveBtn) saveBtn.click();
      });
      await page.waitForTimeout(1200);
      return ok;
    }

    async function countPosButtons() {
      await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
      // انتظر حتى تظهر الأزرار
      await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
      await page.waitForTimeout(2500);
      return await page.evaluate(() => document.querySelectorAll('.pos-item').length);
    }

    // 1) ضبط إلى 20
    let set = await setGridLimit(20);
    log('SET_LIMIT_20_SAVED=' + set);
    let n = await countPosButtons();
    log('LIMIT_20_BUTTONS=' + n + ' EXPECTED=20');

    // 2) ضبط إلى 300
    set = await setGridLimit(300);
    log('SET_LIMIT_300_SAVED=' + set);
    n = await countPosButtons();
    log('LIMIT_300_BUTTONS=' + n + ' EXPECTED=300');
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    log('PAGE_ERRORS_SO_FAR=' + JSON.stringify(pageErrors));
    fs.writeFileSync('D:/ERPSystem/_t5_part1.txt', out.join('\n'));
    await browser.close();
  }
})();