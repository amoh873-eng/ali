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

    // Click the report + Generate
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.includes('سجل ضريبة المبيعات'))?.click();
    });
    await page.waitForTimeout(1500);
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.includes('توليد'))?.click();
    });
    await page.waitForTimeout(4000);

    // Extract the totals row (tfoot)
    const totals = await page.evaluate(() => {
        const table = document.querySelector('.report-table');
        if (!table) return 'NO-TABLE';
        const tfoot = table.querySelector('tfoot');
        return tfoot ? tfoot.textContent.replace(/\s+/g, ' ').trim() : 'NO-TOTALS';
    });
    log('TOTALS_ROW=' + totals);

    // Count invoice rows
    const rowCount = await page.evaluate(() => {
        const table = document.querySelector('.report-table');
        if (!table) return -1;
        return table.querySelectorAll('tbody tr').length;
    });
    log('ROW_COUNT=' + rowCount);

    // Verify Excel export works (button exists in popup)
    const excelBtn = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        return btns.some(b => b.textContent.trim() === 'Excel');
    });
    log('EXCEL_BTN=' + excelBtn);

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_tax_totals.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_tax_totals.txt', out.join('\n'), 'utf8');
});