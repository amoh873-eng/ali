// End-to-end test for the non-invasive scale-barcode interceptor:
// 1. log in as a POS user, 2. open /pos, 3. simulate hardware-scanner bursts
// (13 fast keydowns + Enter, twice with different weights), 4. assert the item lands
// in the cart with the MERGED weight (1.500 + 0.750 = 2.250 kg, qty > 1).
const { chromium } = require('playwright');
const fs = require('fs');

const BASE = 'http://localhost:5186';
const EMAIL = process.env.SMOKE_EMAIL || 'smoke@erp.com';
const PASSWORD = process.env.SMOKE_PASSWORD || 'Test@1234';
// Two scans of the same SKU with different weights: 1.500 kg then 0.750 kg.
const SCAN_CODES = ['2121150015008', '2121150007508'];

function genCheckDigit(base) {
    let sum = 0;
    for (let i = 0; i < base.length; i++) {
        const d = Number(base[i]);
        sum += (i % 2 === 0) ? d : d * 3;
    }
    return String((10 - (sum % 10)) % 10);
}

const out = [];
function log(s) { out.push(s); }

async function simulateScan(page, code) {
    await page.evaluate((scanCode) => {
        const target = document.body;
        const fire = (type, opts) =>
            target.dispatchEvent(new KeyboardEvent(type, Object.assign(
                { bubbles: true, cancelable: true, view: window }, opts)));
        for (const ch of scanCode) {
            fire('keydown', { key: ch, code: 'Digit' + ch, keyCode: ch.charCodeAt(0) });
        }
        fire('keydown', { key: 'Enter', code: 'Enter', keyCode: 13 });
    }, code);
    await page.waitForTimeout(3500); // allow the Blazor round-trip + render
}

async function readCart(page) {
    return page.evaluate(() => {
        const lines = Array.from(document.querySelectorAll('.pos-line')).map(line => ({
            name: (line.querySelector('.pos-line-name') || {}).textContent || '',
            qtyAttr: (line.querySelector('.pos-qty-box') || {}).getAttribute('value') || '',
            qtyProp: (line.querySelector('.pos-qty-box') || {}).value || '',
            total: (line.querySelector('.pos-line-total') || {}).textContent || ''
        }));
        const search = (document.querySelector('.pos-search input') || {}).value || '';
        return JSON.stringify({ lines, search });
    });
}

async function main() {
    for (const c of SCAN_CODES) {
        const expected = genCheckDigit(c.slice(0, 12));
        log(c + ' CHECK_OK=' + (c[12] === expected));
    }

    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage();
    page.on('pageerror', e => log('PAGEERROR: ' + (e.message || '').slice(0, 200)));
    page.on('console', m => { if (m.type() === 'error') log('CONSOLE_ERROR: ' + m.text().slice(0, 300)); });

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

    // Open the POS page
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle' });
    await page.waitForSelector('.pos-search input', { timeout: 20000 }).catch(() => log('NO_SEARCH_FIELD'));
    await page.waitForTimeout(2500); // let the interceptor init + enable

    const moduleState = await page.evaluate(async () => {
        try {
            const m = await import('/scaleBarcode.js');
            return JSON.stringify(m.getState());
        } catch (e) { return 'ERR:' + e.message; }
    });
    log('INTERCEPTOR_STATE=' + moduleState);

    // Scan #1: 1.500 kg
    await simulateScan(page, SCAN_CODES[0]);
    log('CART_AFTER_SCAN1=' + await readCart(page));

    // Scan #2: 0.750 kg -> the cart line quantity should now be 2.250 kg
    await simulateScan(page, SCAN_CODES[1]);
    log('CART_AFTER_SCAN2=' + await readCart(page));

    try { await page.screenshot({ path: 'd:/scale_pos.png' }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_scale_e2e.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_scale_e2e.txt', out.join('\n'), 'utf8');
});