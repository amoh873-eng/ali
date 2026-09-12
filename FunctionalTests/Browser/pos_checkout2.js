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

    await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(5000);

    await page.evaluate(() => { const b = document.querySelector('.pos-item'); if (b) b.click(); });
    await page.waitForTimeout(500);

    const total = await page.evaluate(() => {
      const el = document.querySelector('.pos-total-row.grand span:last-child');
      return el ? el.textContent.trim() : '';
    });
    log('GRAND_TOTAL_B64=' + b64(total));

    // العثور على حقل المبلغ المستلم (input داخل pos-payment، ليس حقل البحث)
    const receivedInfo = await page.evaluate(() => {
      const inputs = Array.from(document.querySelectorAll('.pos-payment input.mud-input-slot'));
      return inputs.map((i, idx) => ({ idx, placeholder: i.getAttribute('placeholder') || '', val: i.value }));
    });
    log('PAYMENT_INPUTS=' + JSON.stringify(receivedInfo));

    if (receivedInfo.length > 0) {
      await page.fill('.pos-payment input.mud-input-slot >> nth=0', '1000');
      await page.waitForTimeout(600);
    }

    await page.evaluate(() => { const c = document.querySelector('.pos-checkout'); if (c) c.click(); });
    await page.waitForTimeout(6000);

    const post = await page.evaluate(() => {
      const snacks = Array.from(document.querySelectorAll('.mud-snackbar')).map(s => s.textContent.trim());
      const dialogs = document.querySelectorAll('.mud-dialog').length;
      return { snacks, dialogs };
    });
    log('DIALOG_COUNT=' + post.dialogs);
    log('SNACK0_B64=' + b64(post.snacks[0] || ''));
    log('SNACKS_ALL_B64=' + JSON.stringify(post.snacks.map(b64)));

    log('HAS_SECOND_OP=' + pageErrors.some(e => /second operation/i.test(String(e))));
    for (const e of pageErrors.slice(0, 5)) log('PAGE_ERR_B64=' + b64(e));
  } catch (e) {
    log('FATAL_B64=' + b64(e && e.message || ''));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_pos_checkout2.txt', out.join('\n'));
    await browser.close();
  }
})();