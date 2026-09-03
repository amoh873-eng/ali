const { chromium } = require('playwright');

let EMAIL = process.env.SMOKE_EMAIL || 'admin@erp.com';
let PASSWORD = process.env.SMOKE_PASSWORD || 'Admin@1234';

const BASE = 'http://localhost:5186';

async function login(page, email, password) {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', email, { timeout: 15000 });
    await page.fill('input[name="Password"]', password, { timeout: 15000 });
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
        page.click('button.login-btn'),
    ]);
    await page.waitForTimeout(2500);
}

async function main() {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const ctx = await browser.newContext();
    const page = await ctx.newPage();
    const errors = [];
    page.on('console', m => { if (m.type() === 'error') errors.push('CONSOLE: ' + m.text()); });
    page.on('pageerror', e => errors.push('PAGEERROR: ' + e.message));

    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    const demoText = await page.locator('.login-demo').textContent().catch(() => 'NO-DEMO');
    console.log('DEMO_TEXT=' + (demoText || '').trim());

    await login(page, EMAIL, PASSWORD);
    await page.waitForTimeout(2000);
    console.log('URL_AFTER_LOGIN=' + page.url());
    const bodyText = (await page.locator('body').innerText()).replace(/\s+/g, ' ').trim();
    const hasErr = /which form is being submitted|An unhandled error|Oops/i.test(bodyText);
    console.log('HAS_ERR=' + hasErr);
    console.log('BODY_SNIP=' + bodyText.slice(0, 300));
    await page.screenshot({ path: 'd:/ft_dashboard.png', fullPage: false });

    console.log('ERRORS=' + JSON.stringify(errors));
    await browser.close();
}
main().catch(e => { console.error('FATAL', e); process.exit(1); });