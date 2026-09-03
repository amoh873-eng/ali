import { chromium } from 'playwright';

const base = 'http://localhost:5186';
const results = [];
const errors = [];

let browser = null;
try {
  browser = await chromium.launch({ channel: 'msedge', headless: true });
} catch (e) {
  results.push('msedge channel failed: ' + e.message.split('\n')[0]);
  browser = await chromium.launch({ headless: true });
}
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

page.on('pageerror', e => errors.push('PAGEERROR: ' + e.message));
page.on('console', m => { if (m.type() === 'error') errors.push('CONSOLE: ' + m.text()); });

async function checkPage(name, url) {
  try {
    await page.goto(base + url, { waitUntil: 'networkidle', timeout: 45000 });
    await page.waitForTimeout(6000);
    const body = await page.textContent('body');
    const hasLoadError = body.includes('خطأ في تحميل البيانات');
    results.push(`[${name}] url=${url} loadError=${hasLoadError} bodyLen=${body.length}`);
    if (hasLoadError) {
      const snack = await page.textContent('.mud-snackbar, .mud-alert-message').catch(() => '');
      results.push(`   -> snackbar: ${snack.trim().slice(0, 220)}`);
    }
  } catch (e) {
    results.push(`[${name}] ERR ${url}: ${e.message.split('\n')[0]}`);
  }
}

try {
  await page.goto(base + '/login', { timeout: 45000 });
  await page.waitForSelector('input[name="Email"]', { timeout: 20000 });
  await page.fill('input[name="Email"]', 'admin@erp.com');
  await page.fill('input[name="Password"]', 'Admin@123');
  const [resp] = await Promise.all([
    page.waitForResponse(r => r.request().method() === 'POST' && r.url().includes('/login/handler'), { timeout: 30000 }),
    page.click('button[type="submit"]'),
  ]);
  results.push('[login] POST status=' + resp.status() + ' finalUrl=' + resp.url());
  const final = resp.headers()['location'];
  if (final) results.push('[login] redirect Location=' + final);
  await page.waitForTimeout(6000);
  results.push('[login] after-wait url=' + page.url());
  const bodyT = await page.textContent('body');
  results.push('[login] loadError=' + bodyT.includes('خطأ في تحميل البيانات') + ' bodyLen=' + bodyT.length);
  if (bodyT.includes('LoginFailed') || bodyT.includes('login-error')) results.push('[login] shows login failed banner');

  await checkPage('dashboard', '/');
  await checkPage('items', '/inventory/items');
  await checkPage('sales', '/sales/invoices');
  await checkPage('purchases', '/purchases/invoices');
  await checkPage('accounts', '/accounts');
  await checkPage('settings', '/settings');
  await checkPage('journal', '/journal');
  await checkPage('customers', '/sales/customers');
} catch (e) {
  results.push('FATAL: ' + e.message.split('\n')[0]);
} finally {
  console.log('=== RESULTS ===');
  console.log(results.join('\n'));
  console.log('=== JS/PAGE ERRORS (' + errors.length + ') ===');
  console.log(errors.slice(0, 25).join('\n'));
  await browser.close();
}