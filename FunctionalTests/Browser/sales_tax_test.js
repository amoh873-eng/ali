// E2E test for the dedicated Sales Tax Report page (/reports/sales-tax).
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

    // Direct nav to the report page
    await page.goto(BASE + '/reports/sales-tax', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    log('PAGE_URL=' + page.url());

    // Verify filter section exists
    const hasFrom = await page.evaluate(() => document.body.innerText.includes('تاريخ البداية') || document.body.innerText.includes('From'));
    const hasGen = await page.evaluate(() => Array.from(document.querySelectorAll('button')).some(b => b.textContent.includes('توليد التقرير')));
    log('HAS_FROM_DATE=' + hasFrom + ' HAS_GENERATE_BTN=' + hasGen);

    // Click Generate Report
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        btns.find(b => b.textContent.includes('توليد التقرير'))?.click();
    });
    await page.waitForTimeout(5000);

    // Read the report table on the page
    const result = await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.mud-table-body .mud-table-row')).map(tr =>
            tr.textContent.replace(/\s+/g, ' ').trim());
        const chipRow = Array.from(document.querySelectorAll('.mud-table-body tr')).map(tr =>
            tr.textContent.replace(/\s+/g, ' ').trim());
        // Gather chips (tax rates)
        const chips = Array.from(document.querySelectorAll('.mud-chip')).map(c => c.textContent.trim()).filter(t => /%\s*$/.test(t));
        // Totals area
        const totalsArea = document.body.innerText.match(/عدد الفواتير[\s\S]{0,200}?إجمالي المبيعات/)?.[0] || '';
        return JSON.stringify({
            bodySnippet: document.body.innerText.replace(/\s+/g,' ').slice(0, 600),
            chipCount: chips.length,
            chips: chips.slice(0, 10),
            hasBalanced: document.body.innerText.includes('متوازن')
        });
    });
    log('PAGE_RESULT=' + result);

    try { await page.screenshot({ path: 'd:/sales_tax_report.png', fullPage: true }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_salestax_e2e.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_salestax_e2e.txt', out.join('\n'), 'utf8');
});