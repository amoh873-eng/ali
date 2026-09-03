// E2E verify the three new reports render on the live app.
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
    log('LOGGED_IN=' + page.url());

    // 1) AR Aging
    await page.goto(BASE + '/reports/ar-aging', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'));
        b?.click();
    });
    await page.waitForTimeout(3000);
    const arText = await page.evaluate(() => document.body.innerText);
    log('AR_AGING_HAS_TABLE=' + (arText.includes('0-30') && arText.includes('90+')));
    log('AR_AGING_HAS_CONC=' + arText.includes('مخاطر التركيز'));

    // 2) AP Aging
    await page.goto(BASE + '/reports/ap-aging', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'));
        b?.click();
    });
    await page.waitForTimeout(3000);
    const apText = await page.evaluate(() => document.body.innerText);
    log('AP_AGING_HAS_TABLE=' + (apText.includes('0-30') && apText.includes('90+')));

    // 3) Financial Ratios
    await page.goto(BASE + '/reports/financial-ratios', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('توليد التقرير'));
        b?.click();
    });
    await page.waitForTimeout(4000);
    const fr = await page.evaluate(() => {
        const t = document.body.innerText;
        return JSON.stringify({
            hasLiquidity: t.includes('السيولة'),
            hasProfit: t.includes('الربحية'),
            hasEfficiency: t.includes('الكفاءة التشغيلية'),
            hasCurrentRatio: t.includes('النسبة المتداولة'),
            hasPrevPeriod: t.includes('الفترة السابقة')
        });
    });
    log('RATIOS=' + fr);

    try { await page.screenshot({ path: 'd:/ratios.png', fullPage: true }); } catch (e) {}
    await browser.close();
}

main().catch(e => {
    log('FATAL: ' + (e.stack || e.message));
    fs.writeFileSync('d:/ERPSystem/_ratios_e2e.txt', out.join('\n'), 'utf8');
    process.exit(1);
}).then(() => {
    fs.writeFileSync('d:/ERPSystem/_ratios_e2e.txt', out.join('\n'), 'utf8');
});