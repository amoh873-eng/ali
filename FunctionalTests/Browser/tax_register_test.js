// End-to-end test for the new "سجل ضريبة المبيعات" (Sales Tax Register) report.
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
    page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 200)));

    // Login
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 15000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 15000 });
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
        page.click('button.login-btn'),
    ]);
    await page.waitForTimeout(2000);
    log('URL_AFTER_LOGIN=' + page.url());

    // Open the reports center
    await page.goto(BASE + '/reports', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    // Find and click the new report button (part of the built-in catalog)
    const buttons = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('button')).map(b => b.textContent.trim()).filter(t => t.length > 0 && t.length < 60);
    });
    log('BUTTONS=' + JSON.stringify(buttons.filter(t => t.includes('ضريبة') || t.includes('Tax'))));

    // Locate the button by text
    const found = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const target = btns.find(b => b.textContent.includes('سجل ضريبة المبيعات'));
        if (target) { target.click(); return true; }
        return false;
    });
    log('CLICKED_VAT_REGISTER=' + found);
    await page.waitForTimeout(1500);

    // Click the توليد (Generate) button
    const gen = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const target = btns.find(b => b.textContent.includes('توليد'));
        if (target) { target.click(); return true; }
        return false;
    });
    log('CLICKED_GENERATE=' + gen);
    await page.waitForTimeout(4000);

    // Capture the popup report table
    const report = await page.evaluate(() => {
        const headerEl = document.querySelector('.report-header');
        const table = document.querySelector('.report-table');
        return JSON.stringify({
            header: headerEl ? headerEl.textContent.replace(/\s+/g, ' ').trim() : 'NO-HEADER',
            tableHtml: table ? table.outerHTML.slice(0, 4000) : 'NO-TABLE'
        });
    });
    log('REPORT=' + report);

    try { await page.screenshot({ path: 'd:/tax_report.png' }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_tax_e2e.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_tax_e2e.txt', out.join('\n'), 'utf8');
});