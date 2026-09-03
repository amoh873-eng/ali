import { chromium } from 'playwright';

const base = 'http://localhost:5186';
let browser = await chromium.launch({ channel: 'msedge', headless: true }).catch(() => chromium.launch({ headless: true }));
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
let loadErrors = 0;
let failedReq = [];
page.on('pageerror', e => { if (!e.message.includes('is not valid JSON')) loadErrors++; });
page.on('response', r => { if (r.status() >= 400 && !r.url().includes('favicon')) failedReq.push(r.status() + ' ' + r.url()); });

// Login once, then loop dashboard loads (each is a fresh navigation = fresh prerender+circuit, the race window we fixed).
await page.goto(base + '/login', { waitUntil: 'domcontentloaded' });
await page.waitForSelector('input[name="Email"]');
await page.fill('input[name="Email"]', 'admin@erp.com');
await page.fill('input[name="Password"]', 'Admin@123');
await Promise.all([
  page.waitForURL(u => !u.pathname.startsWith('/login'), { timeout: 45000 }),
  page.click('button[type="submit"]'),
]);
console.log('logged in: ' + page.url());

for (let i = 0; i < 6; i++) {
  try {
    await page.goto(base + '/', { waitUntil: 'domcontentloaded', timeout: 45000 });
    await page.waitForTimeout(4000 + Math.random() * 3000);
    const body = await page.textContent('body');
    const has = body.includes('خطأ في تحميل البيانات');
    if (has) loadErrors++;
    console.log(`loop#${i} url=${page.url()} loadError=${has}`);
  } catch (e) {
    console.log(`loop#${i} EXC ${String(e.message).split('\n')[0]}`);
  }
}
console.log('TOTAL_LOAD_ERRORS=' + loadErrors);
console.log('FAILED_REQS=' + JSON.stringify(failedReq.slice(0, 8)));
await browser.close();