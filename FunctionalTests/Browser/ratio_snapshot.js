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
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 15000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 15000 });
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
        page.click('button.login-btn'),
    ]);
    await page.waitForTimeout(2500);

    // AR aging snapshot
    await page.goto(BASE + '/reports/ar-aging', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.evaluate(() => Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'))?.click());
    await page.waitForTimeout(3500);
    const arBody = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ').slice(0, 700));
    log('AR_BODY=' + arBody);

    // Ratios snapshot
    await page.goto(BASE + '/reports/financial-ratios', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.evaluate(() => Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'))?.click());
    await page.waitForTimeout(4000);
    const ratioBody = await page.evaluate(() => document.body.innerText.replace(/\s+/g, ' ').slice(0, 900));
    log('RATIO_BODY=' + ratioBody);

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_ratio_snap.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_ratio_snap.txt', out.join('\n'), 'utf8');
});