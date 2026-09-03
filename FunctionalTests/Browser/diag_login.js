const { chromium } = require('playwright');

const BASE = 'http://localhost:5186';

async function main() {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const page = await browser.newPage();
    const logs = [];
    page.on('console', m => logs.push('CONSOLE[' + m.type() + ']: ' + m.text().slice(0, 300)));
    page.on('pageerror', e => logs.push('PAGEERROR: ' + (e.stack || e.message).slice(0, 500)));
    page.on('requestfailed', r => logs.push('REQFAIL: ' + r.url() + ' -> ' + (r.failure() || {}).errorText));

    await page.goto(BASE + '/login', { waitUntil: 'load', timeout: 30000 }).catch(e => logs.push('GOTO: ' + e.message));
    await page.waitForTimeout(10000);

    const state = {
        url: page.url(),
        title: await page.title().catch(() => 'ERR'),
        bodyLen: await page.evaluate(() => document.body ? document.body.innerHTML.length : -1),
        bodySnippet: await page.evaluate(() => document.body ? document.body.innerText.replace(/\s+/g, ' ').trim().slice(0, 400) : 'NO-BODY'),
        hasEmailInput: await page.locator('input[name="Email"]').count().catch(() => -1),
        hasMudProgress: await page.locator('.mud-progress-circular, .mud-skeleton').count().catch(() => -1),
        scripts: await page.evaluate(() => Array.from(document.scripts).map(s => s.src).filter(Boolean)),
        blazorRoot: await page.evaluate(() => {
            const el = document.getElementById('app') || document.body.firstElementChild;
            return el ? el.outerHTML.slice(0, 500) : 'NONE';
        }),
    };
    const out = [];
    for (const [k, v] of Object.entries(state)) out.push(k + '=' + JSON.stringify(v).slice(0, 600));
    out.push('--- LOGS ---');
    out.push(...logs);
    require('fs').writeFileSync('d:/ERPSystem/_diag.txt', out.join('\n'), 'utf8');
    try { await page.screenshot({ path: 'd:/diag_login.png' }); } catch (e) {}
    await browser.close();
}
main().catch(e => { require('fs').writeFileSync('d:/ERPSystem/_diag.txt', 'FATAL: ' + e.message, 'utf8'); process.exit(1); });