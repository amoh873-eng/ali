const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  await ctrl.locator('input').first().fill(value, { timeout: 5000 });
}
async function clickByText(page, text) {
  await page.locator('button', { hasText: text }).first().click({ timeout: 10000 });
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

  await page.goto(BASE + '/pos/shift-start', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2200);
  await fillByLabel(page, 'رصيد الفكة الافتتاحي', '150');
  await page.waitForTimeout(400);
  await clickByText(page, 'بدء الوردية');
  await page.waitForTimeout(2200);

  await page.goto(BASE + '/pos/shift-end', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  const expText = await page.evaluate(() => {
    const tr = Array.from(document.querySelectorAll('.shift-summary tr')).find(r => {
      const th = r.querySelector('th');
      return th && (th.textContent || '').includes('الرصيد المتوق');
    });
    const td = tr ? tr.querySelectorAll('td') : null;
    return td && td.length > 0 ? Array.from(td).map(c => (c.textContent || '').trim()).join('|') : '';
  });
  const expVal = expText.split('|').pop().replace(/[^\d.,]/, '');
  log('ZERO_EXPECTED_RAW=' + expText + ' CLEAN=' + expVal);

  await fillByLabel(page, 'المبلغ المعدود فعلياً', expVal.replace(',', '.'));
  await page.waitForTimeout(900);
  await clickByText(page, 'تأكيد الإغلاق وتسوية الفرق');
  await page.waitForTimeout(2600);
  const t2 = await page.evaluate(() => document.body.innerText);
  log('ZERO_CLOSED=' + (t2.includes('أُغلقت الوردية') || t2.includes('Shift closed')));
  fs.writeFileSync('D:\\ERPSystem\\_shift_zero_out.txt', out.join('\n'), 'utf8');
  console.log(out.join('\n'));
  await browser.close();
}
main().catch(e => { out.push('ERR=' + e); fs.writeFileSync('D:\\ERPSystem\\_shift_zero_out.txt', out.join('\n'), 'utf8'); console.log(out.join('\n')); process.exit(1); });