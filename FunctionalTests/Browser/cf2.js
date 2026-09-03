const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const EMAIL = process.env.SMOKE_EMAIL || 'smoke@erp.com';
const PASSWORD = process.env.SMOKE_PASSWORD || 'Test@1234';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?')); }
async function main() {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage();
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 15000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 15000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/reports/cash-flow', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    await page.evaluate(() => Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('?????'))?.click());
    await page.waitForTimeout(4000);
    const t = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ').slice(0, 900));
    log('BODY=' + t);
    await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('D:/ERPSystem/_cf2.txt', out.join('\n'), 'ascii'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_cf2.txt', out.join('\n'), 'ascii'); });