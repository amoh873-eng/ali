// Verify DonutChart3D renders on the dashboard.
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
    await page.waitForTimeout(2500);
    log('URL_AFTER_LOGIN=' + page.url());

    // Look for the 3D donut SVG on the dashboard
    const svgInfo = await page.evaluate(() => {
        const svgs = Array.from(document.querySelectorAll('svg.erp-donut3d-svg'));
        return JSON.stringify({
            count: svgs.length,
            aria: svgs.map(s => s.getAttribute('aria-label')),
            pathCount: svgs.map(s => s.querySelectorAll('path').length),
            textCount: svgs.map(s => s.querySelectorAll('text').length),
            hasCssClass: document.querySelector('.erp-donut3d') !== null
        });
    });
    log('DONUT3D=' + svgInfo);

    // Verify the legend
    const legend = await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.erp-donut3d-legend-item'));
        return JSON.stringify(items.map(i => i.textContent.replace(/\s+/g, ' ').trim()));
    });
    log('LEGEND=' + legend);

    try { await page.screenshot({ path: 'd:/donut3d_dashboard.png', fullPage: true }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_donut3d_test.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_donut3d_test.txt', out.join('\n'), 'utf8');
});