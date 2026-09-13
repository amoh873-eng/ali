const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);
async function selectByLabel(page, labelText) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.click({ timeout: 8000 });
  await page.waitForTimeout(800);
  const items = page.locator('.mud-menu-item, .mud-list-item, [role="option"], [role="menuitem"]');
  const n = await items.count();
  if (n === 0) throw new Error('NO_ITEMS for ' + labelText);
  await items.first().click({ timeout: 8000 });
  await page.waitForTimeout(600);
}
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.fill(value, { timeout: 6000 });
}
async function clickByText(page, text) {
  await page.locator('button', { hasText: text }).first().click({ timeout: 12000 });
}
async function clickDialogSave(page) {
  const acts = page.locator('.mud-dialog-actions').first().locator('button');
  const n = await acts.count();
  if (n > 0) await acts.nth(n - 1).click({ timeout: 10000 });
}

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3000);

  // تسجيل شيك ثانٍ ثم ارتداده
  await page.goto(BASE + '/finance/checks', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  await clickByText(page, 'تسجيل شيك جديد');
  await page.waitForTimeout(2000);
  await selectByLabel(page, 'العميل');
  await fillByLabel(page, 'رقم الشيك', 'E2E-BOUNCE-2');
  await fillByLabel(page, 'البنك', 'BankTest3');
  await fillByLabel(page, 'المبلغ', '25.75');
  await clickDialogSave(page);
  await page.waitForTimeout(4000);
  const t1 = await page.evaluate(() => document.body.innerText);
  log('REG2_VISIBLE=' + t1.includes('E2E-BOUNCE-2'));
  fs.writeFileSync('d:/ERPSystem/_chk_list1.txt', t1.slice(0, 3000), 'utf8');

  await page.locator('#chk-bounce').first().click({ timeout: 10000 });
  await page.waitForTimeout(1500);
  await clickDialogSave(page);
  await page.waitForTimeout(4000);
  const t2 = await page.evaluate(() => document.body.innerText);
  log('BOUNCED_VISIBLE=' + t2.includes('E2E-BOUNCE-2'));
  log('BOUNCED_STATE_VISIBLE=' + (t2.includes('مرتد')));
  fs.writeFileSync('d:/ERPSystem/_chk_list2.txt', t2.slice(0, 3000), 'utf8');

  // التقرير
  await page.goto(BASE + '/finance/checks-report', { waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  const rt = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_chk_report.txt', rt.slice(0, 8000), 'utf8');
  log('REPORT_INCOMING_LABEL=' + rt.includes('مستحق علينا'));
  log('REPORT_MONTHLY_LABEL=' + rt.includes('التوقع الشهري'));
  log('DONE');
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('d:/ERPSystem/_chk_e2e5.txt', out.join('\n'), 'utf8'); process.exit(1); })
  .then(() => fs.writeFileSync('d:/ERPSystem/_chk_e2e5.txt', out.join('\n'), 'utf8'));