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
  await page.waitForTimeout(700);
  const items = page.locator('.mud-menu-item, .mud-list-item, [role="option"], [role="menuitem"]');
  const n = await items.count();
  if (n === 0) throw new Error('NO_ITEMS for ' + labelText);
  await items.first().click({ timeout: 6000 });
  await page.waitForTimeout(500);
}
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.fill(value, { timeout: 5000 });
}
async function clickByText(page, text) {
  await page.locator('button', { hasText: text }).first().click({ timeout: 10000 });
}
async function clickDialogSave(page) {
  const acts = page.locator('.mud-dialog-actions').first();
  const btns = acts.locator('button');
  const n = await btns.count();
  if (n > 0) { await btns.nth(n - 1).click({ timeout: 8000 }); return true; }
  return false;
}
async function clickIcon(page, id) {
  await page.locator('#' + id).first().click({ timeout: 8000 });
  await page.waitForTimeout(1500);
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
  await clickByText(page, 'تسجيل شيك جديد');
  await page.waitForTimeout(1800);
  await selectByLabel(page, 'العميل');
  await fillByLabel(page, 'رقم الشيك', 'E2E-RECV-1');
  await fillByLabel(page, 'البنك', 'BankTest1');
  await fillByLabel(page, 'المبلغ', '110.50');
  await clickDialogSave(page);
  await page.waitForTimeout(2500);
  log('RECV1_VISIBLE=' + (await page.evaluate(() => document.body.innerText)).includes('E2E-RECV-1'));

  await clickIcon(page, 'chk-clear');
  await clickDialogSave(page);
  await page.waitForTimeout(2500);
  log('CLEAR_SAVED=' + (await page.evaluate(() => document.body.innerText)).includes('E2E-RECV-1'));

  await clickByText(page, 'تسجيل شيك جديد');
  await page.waitForTimeout(1800);
  await fillByLabel(page, 'رقم الشيك', 'E2E-BOUNCE-1');
  await fillByLabel(page, 'البنك', 'BankTest2');
  await fillByLabel(page, 'المبلغ', '25.75');
  await clickDialogSave(page);
  await page.waitForTimeout(2500);
  await clickIcon(page, 'chk-bounce');
  await clickDialogSave(page);
  await page.waitForTimeout(2500);

  await page.goto(BASE + '/finance/checks-report', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);
  const rt = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_chk_report.txt', rt.slice(0, 6500), 'utf8');
  log('REPORT_INCOMING_LABEL=' + (rt.includes('مستحق علينا')));
  log('REPORT_MONTHLY_LABEL=' + (rt.includes('التوقع الشهري')));
  log('FINISHED');
  await browser.close();
}

main().catch(e => {
  log('FATAL: ' + (e.stack || e.message));
  fs.writeFileSync('d:/ERPSystem/_chk_e2e4.txt', out.join('\n'), 'utf8');
  process.exit(1);
}).then(() => {
  fs.writeFileSync('d:/ERPSystem/_chk_e2e4.txt', out.join('\n'), 'utf8');
});