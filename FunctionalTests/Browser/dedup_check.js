// Verify the Reports dropdown no longer contains the duplicate /reports/custom link.
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

    // Collect all report-related links in the nav
    const links = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('a'))
            .map(a => ({ href: (a.getAttribute('href') || ''), text: a.textContent.replace(/\s+/g, ' ').trim() }))
            .filter(l => l.href.includes('reports'));
    });
    log('REPORTS_LINKS=' + JSON.stringify(links));

    const customCount = links.filter(l => l.href === '/reports/custom').length;
    const dashCount = links.filter(l => l.href === '/reports').length;
    log('DUPLICATE_CUSTOM_LINK=' + customCount);
    log('DASHBOARD_LINK=' + dashCount);

    // Verify /reports still works
    await page.goto(BASE + '/reports', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    const text = await page.evaluate(() => document.body.innerText);
    log('REPORTS_CENTER_OK=' + (text.includes('مركز التقارير') || text.includes('التقارير الجاهزة') || text.includes('قائمة الدخل')));

    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_dedup_check.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_dedup_check.txt', out.join('\n'), 'utf8');
});