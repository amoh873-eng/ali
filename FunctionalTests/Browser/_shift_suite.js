const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);

function num(s) {
  if (s === null || s === undefined) return NaN;
  let t = String(s).replace(/[\u066B\u066C\u0020\u00A0\u200F\u200E\u200B]/g, m =>
    m === '\u066B' ? '.' : m === '\u066C' ? '' : '');
  t = t.replace(/,/g, '');
  return parseFloat(t);
}
function fmt(v) { return v.toFixed(2); }
async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  await ctrl.locator('input').first().fill(value, { timeout: 5000 });
}
async function clickByText(page, text) {
  await page.locator('button', { hasText: text }).first().click({ timeout: 10000 });
}
async function currentExpected(page) {
  await page.goto(BASE + '/pos/shift-end', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  const val = await page.evaluate(() => {
    const tr = Array.from(document.querySelectorAll('.shift-summary tr')).find(r => {
      const th = r.querySelector('th');
      return th && (th.textContent || '').includes('الرصيد المتوق');
    });
    const tds = tr ? Array.from(tr.querySelectorAll('td')).map(c => (c.textContent || '').trim()) : [];
    return tds.length > 0 ? tds[tds.length - 1] : '';
  });
  return val;
}
async function openShift(page, amount) {
  await page.goto(BASE + '/pos/shift-start', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2200);
  await fillByLabel(page, 'رصيد الفكة الافتتاحي', amount);
  await page.waitForTimeout(400);
  await clickByText(page, 'بدء الوردية');
  await page.waitForTimeout(2200);
}
async function closeWithCounted(page, counted) {
  await fillByLabel(page, 'المبلغ المعدود فعلياً', counted);
  await page.waitForTimeout(900);
  await clickByText(page, 'تأكيد الإغلاق وتسوية الفرق');
  await page.waitForTimeout(2600);
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

  // ── السيناريو 1: زيادة +2.50 (بيع نقدي حقيقي يغذي بند المبيعات) ──
  await openShift(page, '500');
  await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(2200);
  await page.locator('.pos-item').first().click();
  await page.waitForTimeout(1800);
  try { await fillByLabel(page, 'المبلغ المستلم', '99999'); } catch (e) {}
  await page.waitForTimeout(600);
  await page.locator('button.pos-checkout, .pos-checkout').first().click({ timeout: 9000 }).catch(() => {});
  await page.waitForTimeout(4500);
  const e1 = await currentExpected(page);
  const c1 = fmt(num(e1) + 2.50);
  log('S1_EXPECTED=' + e1 + ' COUNTED=' + c1 + ' TARGET_VAR=+2.50');
  await closeWithCounted(page, c1);

  // ── السيناريو 2: عجز −0.02 ──
  await openShift(page, '50');
  const e2 = await currentExpected(page);
  const c2 = fmt(num(e2) - 0.02);
  log('S2_EXPECTED=' + e2 + ' COUNTED=' + c2 + ' TARGET_VAR=-0.02');
  await closeWithCounted(page, c2);

  // ── السيناريو 3: صفر (معدود = المتوقّع) ──
  await openShift(page, '70');
  const e3 = await currentExpected(page);
  log('S3_EXPECTED=' + e3 + ' TARGET_VAR=0.00');
  await closeWithCounted(page, e3.replace(/\u066B/g, '.').replace(/,/g, ''));

  log('SUITE_DONE');
  fs.writeFileSync('D:\\ERPSystem\\_shift_suite_out.txt', out.join('\n'), 'utf8');
  console.log(out.join('\n'));
  await browser.close();
}
main().catch(e => { out.push('ERR=' + e); fs.writeFileSync('D:\\ERPSystem\\_shift_suite_out.txt', out.join('\n'), 'utf8'); console.log(out.join('\n')); process.exit(1); });