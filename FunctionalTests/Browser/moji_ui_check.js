// Verify Arabic UI renders correctly after mojibake cleanup.
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

    // Scan the whole app body for leftover mojibake markers
    const bodyText = await page.evaluate(() => document.body.innerText);
    const mojiPatterns = ['Ø±', 'Ø§', 'Ù†', 'Ù„', 'â€', 'Øª', 'ØØ', 'ÙŠ', 'Ø¹', 'Ø¯', 'Ù…', 'Ø¨', 'Ùƒ'];
    const found = mojiPatterns.filter(p => bodyText.includes(p));
    log('MOJIBAKE_ON_DASHBOARD=' + JSON.stringify(found));

    // Visit accounts (tree page) — had heavy comment mojibake + label
    await page.goto(BASE + '/accounts', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const accText = await page.evaluate(() => document.body.innerText);
    log('ACCOUNTS_PAGE_TITLE_OK=' + (accText.includes('شجرة الحسابات')));
    log('ACCOUNTS_PAGE_MOJI=' + JSON.stringify(mojiPatterns.filter(p => accText.includes(p))));

    // Visit POS page — had comment mojibake + visible dash
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const posText = await page.evaluate(() => document.body.innerText);
    log('POS_PAGE_OK=' + (posText.includes('نقطة البيع') || posText.includes('الرئيسية')));
    log('POS_PAGE_HAS_MOJI=' + JSON.stringify(mojiPatterns.filter(p => posText.includes(p))));

    // Visit sales tax report page
    await page.goto(BASE + '/reports/sales-tax', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    const taxText = await page.evaluate(() => document.body.innerText);
    log('TAX_PAGE_TITLE_OK=' + (taxText.includes('تقرير ضريبة المبيعات')));
    log('TAX_PAGE_MOJI=' + JSON.stringify(mojiPatterns.filter(p => taxText.includes(p))));

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_moji_ui.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_moji_ui.txt', out.join('\n'), 'utf8');
});