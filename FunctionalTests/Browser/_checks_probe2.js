const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);
async function selectByLabel(page, labelText) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.click({ timeout: 6000 });
  await page.waitForTimeout(800);
  const items = page.locator('.mud-menu-item, .mud-list-item, [role="option"], [role="menuitem"]');
  const n = await items.count();
  log('ITEMS_' + labelText + '=' + n);
  if (n > 0) { await items.first().click({ timeout: 6000 }); }
  await page.waitForTimeout(500);
}
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  log('FILL_' + labelText + '=' + (await input.fill(value, { timeout: 5000 }).then(() => 'ok').catch(e => 'ERR:' + e.message)));
}
async function typeAmount(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.click({ timeout: 5000 });
  await input.pressSequentially(value, { delay: 40 });
  await input.press('Tab');
  await page.waitForTimeout(700);
}
async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(2500);
  await page.goto(BASE + '/finance/checks', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  await page.locator('button', { hasText: 'تسجيل شيك جديد' }).first().click({ timeout: 10000 });
  await page.waitForTimeout(1800);
  // farms: dump initial labels
  const labels = await page.evaluate(() => Array.from(document.querySelectorAll('label, legend')).map(l => l.textContent.trim()).filter(t => t.length > 0).slice(0, 25));
  log('LABELS=' + JSON.stringify(labels));
  await selectByLabel(page, 'العميل');
  await fillByLabel(page, 'رقم الشيك', 'E2E-RECV-1');
  await fillByLabel(page, 'البنك', 'BankTest1');
  await fillByLabel(page, 'المبلغ', '110.50');
  const diag = await page.evaluate(() => {
    const inputs = Array.from(document.querySelectorAll('.mud-dialog input'));
    return inputs.map(i => ({ type: i.getAttribute('type'), aria: i.getAttribute('aria-invalid'), val: (i.value || '').slice(0, 40), id: i.id }));
  });
  log('DIAG=' + JSON.stringify(diag));
  const acts = page.locator('.mud-dialog-actions').first().locator('button');
  log('ACT_BTNS=' + await acts.count());
  await acts.nth(await acts.count() - 1).click({ timeout: 8000 });
  await page.waitForTimeout(3500);
  const body = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_chk_after_save.txt', body.slice(0, 4500), 'utf8');
  log('AFTERSAVE_LEN=' + body.length);
  log('HAS_ALERT=' + (await page.locator('.mud-alert').count()));
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('d:/ERPSystem/_chk_probe2.txt', out.join('\n'), 'utf8'); process.exit(1); }).then(() => fs.writeFileSync('d:/ERPSystem/_chk_probe2.txt', out.join('\n'), 'utf8'));