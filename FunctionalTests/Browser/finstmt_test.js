// Verify /reports/financial-statements renders (no "content does not exist" error).
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

    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', EMAIL, { timeout: 15000 });
    await page.fill('input[name="Password"]', PASSWORD, { timeout: 15000 });
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
        page.click('button.login-btn'),
    ]);
    await page.waitForTimeout(2000);

    // Try the page route directly
    await page.goto(BASE + '/reports/financial-statements', { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const bodyText = await page.evaluate(() => document.body.innerText);
    const hasError = bodyText.includes('Sorry, the content you are looking for does not exist')
        || bodyText.includes('content you are looking');
    log('URL=' + page.url());
    log('HAS_NOT_FOUND_ERROR=' + hasError);
    log('SHOWS_FINANCIAL=' + (bodyText.includes('القوائم المالية') || bodyText.includes('قائمة الدخل')));
    log('BODY_SNIP=' + bodyText.slice(0, 300).replace(/\s+/g, ' '));

    // Also test the redirect from the nav link style
    await page.evaluate(() => window.location.href = '/reports');
    await page.waitForTimeout(2500);
    const centerText = await page.evaluate(() => document.body.innerText);
    log('CENTER_OK=' + (centerText.includes('مركز التقارير') || centerText.includes('التقارير الجاهزة') || centerText.includes('قائمة الدخل')));

    try { await page.screenshot({ path: 'd:/financial_statements.png', fullPage: true }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_finstmt_e2e.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_finstmt_e2e.txt', out.join('\n'), 'utf8');
});