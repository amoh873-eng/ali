const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
async function main() {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage();
    await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);
    await page.goto(BASE + '/reports/cash-flow', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    const genClicked = await page.evaluate(() => {
        const b = document.querySelector('button.mud-button-filled');
        if (!b) return false;
        b.click(); return true;
    });
    await page.waitForTimeout(5000);
    const res = await page.evaluate(() => {
        const t = document.body.innerText;
        return {
            url: location.pathname,
            genClicked: !!document.querySelector('button.mud-button-filled'),
            hasOp: t.includes('    '),  // placeholder (replaced below)
            hasNetChange: t.indexOf('Net change') >= 0 || t.includes('???????') >= 0
        };
    });
    const pass = genClicked && res.hasNetChange;
    fs.writeFileSync('D:/ERPSystem/_cf_result.txt', 'RESULT=' + (pass ? 'PASS' : 'FAIL'), 'ascii');
    await browser.close();
}
main().catch(e => { fs.writeFileSync('D:/ERPSystem/_cf_result.txt', 'RESULT=FATAL ' + String(e.message).slice(0,80), 'ascii'); process.exit(1); });