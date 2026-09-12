const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/inventory/items', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(4000);

    const bodyText = () => page.evaluate(() => {
      const b = document.querySelector('.mud-table-body, .mud-table');
      return b ? b.innerText.replace(/\s+/g, ' ').trim().slice(0, 260) : '(no-table)';
    });

    log('BEFORE=' + (await bodyText()));

    const input = page.locator('input.mud-input-slot').first();
    await input.fill('itm-20260904-501');
    await page.waitForTimeout(1800);
    log('SEARCH_LOWERCASE_CODE_RESULT=' + (await bodyText()));

    await input.fill('');
    await page.waitForTimeout(1200);
    await input.fill('لحم');
    await page.waitForTimeout(1800);
    log('SEARCH_ARABIC_PARTIAL_RESULT=' + (await bodyText()));

    await input.fill('');
    await page.waitForTimeout(1200);
    await input.fill('zzznomatch999');
    await page.waitForTimeout(1800);
    log('SEARCH_NOMATCH_RESULT=' + (await bodyText()));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pg_test3_search.txt', out.join('\n'));
    await browser.close();
  }
})();