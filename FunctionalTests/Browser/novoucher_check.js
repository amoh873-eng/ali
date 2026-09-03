// Verify the "القيود/مرجع المستند" column is removed from the Sales Tax Report.
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

    await page.goto(BASE + '/reports/sales-tax', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);

    // Generate
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.includes('توليد التقرير'))?.click();
    });
    await page.waitForTimeout(4000);

    // Check table headers and body
    const result = await page.evaluate(() => {
        const headers = Array.from(document.querySelectorAll('.mud-table-head .mud-th')).map(t => t.textContent.trim());
        const bodyText = document.body.innerText;
        return JSON.stringify({
            headers,
            hasVoucherHeader: headers.some(h => h.includes('القيود') || h.includes('مرجع')),
            hasVoucherBody: /JE-\d{8}/.test(bodyText),
            bodySnippet: document.body.innerText.replace(/\s+/g, ' ').slice(0, 400)
        });
    });
    log('RESULT=' + result);

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_novoucher_check.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_novoucher_check.txt', out.join('\n'), 'utf8');
});