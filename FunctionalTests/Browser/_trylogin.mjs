import { chromium } from 'playwright';

const base = 'http://localhost:5186';
let browser = await chromium.launch({ channel: 'msedge', headless: true }).catch(() => chromium.launch({ headless: true }));
const ctx = await browser.newContext();
const page = await ctx.newPage();
const candidates = ['Admin@1234', 'Admin@123', 'Admin@12345', 'admin123', 'Password@123', 'Test@1234', 'Admin1234', 'P@ssw0rd'];

for (const pw of candidates) {
  try {
    await page.goto(base + '/login', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForSelector('input[name="Email"]', { timeout: 10000 });
    await page.fill('input[name="Email"]', 'admin@erp.com');
    await page.fill('input[name="Password"]', pw);
    const [resp] = await Promise.all([
      page.waitForResponse(r => r.request().method() === 'POST' && r.url().includes('/login/handler'), { timeout: 15000 }),
      page.click('button[type="submit"]'),
    ]);
    const loc = resp.headers()['location'] || '';
    console.log(`pw=${pw} status=${resp.status()} location=${loc}`);
    if (loc && !loc.includes('error')) { console.log('SUCCESS_WITH=' + pw); break; }
  } catch (e) {
    console.log(`pw=${pw} EXC ${String(e.message).split('\n')[0]}`);
  }
}
await browser.close();