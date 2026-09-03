const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);
  await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const fns = await page.evaluate(() => ({
    scrollBottom: typeof window.erpScrollPosCartToBottom,
    scrollTop: typeof window.erpScrollPosCartToTop,
    loadPrinter: typeof window.erpLoadPrinterSettings,
    savePrinter: typeof window.erpSavePrinterSettings,
    html: (document.body.innerHTML.match(/<script[\s\S]*?<\/script>/g) || []).slice(0,2).map(s => s.replace(/[\x00-\x7F]/g,'?').slice(0,80))
  }));
  fs.writeFileSync('D:/ERPSystem/_script_probe.txt', JSON.stringify(fns, null, 2), 'utf8');
  await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_script_probe.txt', 'FATAL ' + String(e.message).slice(0,150), 'utf8'); process.exit(1); });