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

    // Inspect the SVG geometry in detail: check the back (dark) slices are offset by Depth (dy)
    const geo = await page.evaluate(() => {
        const svg = document.querySelector('svg.erp-donut3d-svg');
        if (!svg) return 'NO_SVG';
        const paths = Array.from(svg.querySelectorAll('path'));
        // Back paths rendered first are the darker ones (fill colors)
        const info = paths.map(p => {
            const d = p.getAttribute('d') || '';
            // Extract all y-coordinates after space-commands to see offset
            return { d: d.slice(0, 90), fill: p.getAttribute('fill') };
        });
        return JSON.stringify(info);
    });
    log('SVG_PATHS=' + geo);

    // Render a full-page screenshot for visual QA
    try { await page.screenshot({ path: 'd:/donut3d_full.png', fullPage: true }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_donut3d_geo.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_donut3d_geo.txt', out.join('\n'), 'utf8');
});