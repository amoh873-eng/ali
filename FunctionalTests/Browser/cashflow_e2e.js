const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const EMAIL = process.env.SMOKE_EMAIL || 'smoke@erp.com';
const PASSWORD = process.env.SMOKE_PASSWORD || 'Test@1234';
const out = [];
function log(s) { out.push(s); }
async function main() {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage();
    page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 150)));
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 15000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 15000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);
    await page.goto(BASE + '/reports/cash-flow', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    await page.evaluate(() => Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'))?.click());
    await page.waitForTimeout(4000);
    const t = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' '));
    log('CF_HAS_OPERATING=' + t.includes('التدفق التشغيلي'));
    log('CF_HAS_INVESTING=' + t.includes('التدفق الاستثماري'));
    log('CF_HAS_FINANCING=' + t.includes('التدفق التمويلي'));
    log('CF_HAS_BEGINNING=' + t.includes('رصيد النقدية الافتتاحي'));
    log('CF_HAS_ENDING=' + t.includes('رصيد النقدية الختامي'));
    log('CF_HAS_BALANCED=' + (t.includes('متوازن')));
    log('CF_SNIPPET=' + t.slice(0, 500));
    try { await page.screenshot({ path: 'D:/cashflow.png', fullPage: true }); } catch (e) {}
    await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('D:/ERPSystem/_cf_e2e.txt', out.join('\n'), 'utf8'); process.exit(1); })
.then(() => { fs.writeFileSync('D:/ERPSystem/_cf_e2e.txt', out.join('\n'), 'utf8'); });