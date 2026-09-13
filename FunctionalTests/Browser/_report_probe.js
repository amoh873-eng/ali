const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/finance/checks-report', { waitUntil: 'networkidle' });
  await page.waitForTimeout(6000);
  const rt = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_chk_report.txt', rt.slice(0, 9000), 'utf8');
  log('INCOMING_LABEL=' + rt.includes('مستحق علينا'));
  log('MONTHLY_LABEL=' + rt.includes('التوقع الشهري'));
  log('DONE');
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('d:/ERPSystem/_chk_e2e6.txt', out.join('\n'), 'utf8'); process.exit(1); })
  .then(() => fs.writeFileSync('d:/ERPSystem/_chk_e2e6.txt', out.join('\n'), 'utf8'));