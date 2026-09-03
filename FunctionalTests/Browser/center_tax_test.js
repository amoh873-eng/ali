// Verify the Sales Tax Report button appears in the reports center catalog.
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
    await page.waitForTimeout(2000);

    await page.goto(BASE + '/reports', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    const catalog = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button')).map(b => b.textContent.trim()).filter(t => t.includes('ضريبة'));
        return JSON.stringify(btns);
    });
    log('CATALOG_TAX_BUTTONS=' + catalog);

    // Open the report from the center and generate
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.includes('تقرير ضريبة المبيعات'))?.click();
    });
    await page.waitForTimeout(1500);
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.trim() === 'توليد')?.click();
    });
    await page.waitForTimeout(4000);

    const popupInfo = await page.evaluate(() => {
        const header = document.querySelector('.report-header');
        const table = document.querySelector('.report-table');
        const tfoot = table ? table.querySelector('tfoot') : null;
        return JSON.stringify({
            header: header ? header.textContent.replace(/\s+/g,' ').trim().slice(0,150) : 'NO_HEADER',
            tfoot: tfoot ? tfoot.textContent.replace(/\s+/g,' ').trim() : 'NO_TFOOT'
        });
    });
    log('POPUP=' + popupInfo);

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_center_check.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_center_check.txt', out.join('\n'), 'utf8');
});