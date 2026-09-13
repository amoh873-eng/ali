const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);

async function openSelect(page, labelText) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const vis = await ctrl.locator('.mud-input-slot:not([style*="display:none"]):not([type="hidden"]), input:not([type="hidden"])').all();
  if (vis.length) { await vis[0].click({ timeout: 10000 }); return; }
  const inp = ctrl.locator('input').first();
  await inp.click({ timeout: 10000 });
}
async function selectByLabel(page, labelText, pickFirst = true) {
  await openSelect(page, labelText);
  await page.waitForTimeout(800);
  const items = page.locator('.mud-menu-item, .mud-list-item, [role="option"], [role="menuitem"]');
  const n = await items.count();
  if (n === 0) throw new Error('NO_ITEMS for ' + labelText);
  await (pickFirst ? items.first() : items.nth(n - 1)).click({ timeout: 10000 });
  await page.waitForTimeout(600);
}
async function selectByLabelOpt(page, labelText, optText) {
  await openSelect(page, labelText);
  await page.waitForTimeout(800);
  const item = page.locator('.mud-menu-item, .mud-list-item, [role="option"], [role="menuitem"]', { hasText: optText }).first();
  await item.click({ timeout: 10000 });
  await page.waitForTimeout(600);
}
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.fill(value, { timeout: 8000 });
}
async function clickByText(page, text) {
  const btns = page.locator('button', { hasText: text });
  const btnsList = await btns.all();
  if (!btnsList.length) throw new Error('NO_BUTTON ' + text);
  // اختر أول زر دقيق (ليس جزءاً من حوار مغلق)
  await btnsList[0].click({ timeout: 15000 });
}
async function clickDialogSave(page) {
  const acts = page.locator('.mud-dialog-actions').first().locator('button');
  const n = await acts.count();
  if (n > 0) await acts.nth(n - 1).click({ timeout: 12000 });
}
async function login(page) {
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 20000 });
  await page.fill('input[name="Password"]', 'Test@1234', { timeout: 20000 });
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button.login-btn')]);
  await page.waitForTimeout(3500);
}

async function main() {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage();
  await login(page);

  // ══ الاختبار 1: شيك مصدر لمورد + صرفه ══
  await page.goto(BASE + '/finance/checks', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  await clickByText(page, 'تسجيل شيك جديد');
  await page.waitForTimeout(2200);
  await selectByLabelOpt(page, 'الاتجاه', 'مصدر لمورد');
  await selectByLabel(page, 'المورد');
  await fillByLabel(page, 'رقم الشيك', 'E2E-ISS-1');
  await fillByLabel(page, 'البنك', 'BankSup1');
  await fillByLabel(page, 'المبلغ', '333.33');
  await clickDialogSave(page);
  await page.waitForTimeout(4000);
  const t1 = await page.evaluate(() => document.body.innerText);
  log('ISS_REG_VISIBLE=' + t1.includes('E2E-ISS-1'));
  // شيك المورد مسجّل ← صرفه
  await page.locator('#chk-clear').first().click({ timeout: 12000 });
  await page.waitForTimeout(1800);
  await clickDialogSave(page);
  await page.waitForTimeout(4000);
  const t2 = await page.evaluate(() => document.body.innerText);
  log('ISS_CLEARED_ROW=' + t2.includes('E2E-ISS-1'));
  log('ISS_CLEARED_STATE=' + t2.includes('محصَّل/صُرف'));
  fs.writeFileSync('d:/ERPSystem/_list3.txt', t2.slice(0, 2800), 'utf8');

  // ══ الاختبار 2: فاتورة مبيعات بشيك (نموذج الشيك داخل نفس التدفق) ══
  await page.goto(BASE + '/sales/invoices', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  await clickByText(page, 'فاتورة جديدة');
  await page.waitForTimeout(2200);
  await selectByLabelOpt(page, 'طريقة الدفع', 'شيك');
  await fillByLabel(page, 'رقم الشيك', 'E2E-SINV-1');
  await fillByLabel(page, 'البنك', 'BankSale1');
  await selectByLabel(page, 'الصنف');
  await page.waitForTimeout(800);
  await clickDialogSave(page);
  await page.waitForTimeout(4500);
  const t3 = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_sale_body.txt', t3.slice(0, 3000), 'utf8');
  log('SINV_SAVED=' + (t3.includes('E2E-SINV-1') || /SI-\d{8}-\d{4}/.test(t3)));
  const m3 = t3.match(/SI-\d{8}-\d{4}/);
  log('SINV_NUMBER=' + (m3 ? m3[0] : 'NA'));

  // ══ الاختبار 3: فاتورة مشتريات بشيك ══
  await page.goto(BASE + '/purchases/invoices', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  await clickByText(page, 'فاتورة جديدة');
  await page.waitForTimeout(2200);
  await selectByLabelOpt(page, 'طريقة الدفع', 'شيك');
  await fillByLabel(page, 'رقم الشيك', 'E2E-PINV-1');
  await fillByLabel(page, 'البنك', 'BankPur1');
  await selectByLabel(page, 'الصنف');
  await page.waitForTimeout(800);
  await clickDialogSave(page);
  await page.waitForTimeout(4500);
  const t4 = await page.evaluate(() => document.body.innerText);
  fs.writeFileSync('d:/ERPSystem/_pur_body.txt', t4.slice(0, 3000), 'utf8');
  log('PINV_SAVED=' + (t4.includes('E2E-PINV-1') || /PI-\d{8}-\d{4}/.test(t4)));
  const m4 = t4.match(/PI-\d{8}-\d{4}/);
  log('PINV_NUMBER=' + (m4 ? m4[0] : 'NA'));

  log('ALL_DONE');
  await browser.close();
}
main().catch(e => { log('FATAL: ' + (e.stack || e.message)); fs.writeFileSync('d:/ERPSystem/_e2e3out.txt', out.join('\n'), 'utf8'); process.exit(1); })
  .then(() => fs.writeFileSync('d:/ERPSystem/_e2e3out.txt', out.join('\n'), 'utf8'));