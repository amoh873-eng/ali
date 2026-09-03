const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s)); }
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  page.on('response', async (resp) => {
    if (resp.status() === 500) {
      try {
        let body = await resp.text();
        if (body && body.length > 0) {
          log('500_RESP_URL=' + resp.url().slice(0, 120));
          log('500_RESP_BODY=' + body.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F]/g, ' ').slice(0, 1800));
        }
      } catch (e) { log('500_RESP_ERR=' + (e.message || '').slice(0, 120)); }
    }
  });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  await page.goto(BASE + '/reports/financial-ratios', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.evaluate(() => { const b = document.querySelector('button.mud-button-filled'); if (b) b.click(); });
  await page.waitForTimeout(5000);
  log('DONE');
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message).slice(0, 400)); fs.writeFileSync('D:/ERPSystem/_resp500.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_resp500.txt', out.join('\n'), 'ascii'); });