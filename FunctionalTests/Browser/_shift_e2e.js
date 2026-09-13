const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
const log = s => out.push(s);

async function fillByLabel(page, labelText, value) {
  const ctrl = page.locator('label', { hasText: labelText }).first()
    .locator('xpath=ancestor::div[contains(@class,"mud-input-control")]').first();
  const input = ctrl.locator('input').first();
  await input.fill(value, { timeout: 5000 });
}
async function clickByText(page, text) {
  await page.locator('button', { hasText: text }).first().click({ timeout: 10000 });
}
function parseN(s) {
  const m = s.replace(/[^0-9\.,\-+]/g, '').replace(/\s/g, '');
  return parseFloat(m.replace(/\./g, '').replace(',', '.'));
}
const bodyText = page => page.evaluate(() => document.body ? document.body.innerText : '');
const fieldRow = (txt, label) => {
  const idx = txt.indexOf(label);
  if (idx < 0) return null;
  const seg = txt.substring(idx, idx + 20);
  const m = seg.match(/([+\-]?\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?|\d+[.,]\d{2})\s*$/);
  return m ? m[1].replace(/\./g, '').replace(',', '.') : null;
};

async function openShift(page, amount) {
  await page.goto(BASE + '/pos/shift-start', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2200);
  await fillByLabel(page, 'رصيد الفكة الافتتاحي', amount);
  await page.waitForTimeout(400);
  await clickByText(page, 'بدء الوردية');
  await page.waitForTimeout(2200);
  const t = await bodyText(page);
  log('OPEN_' + amount + '_OK=' + (t.includes('تم فتح الوردية') || t.includes('ورديتك المفتوحة')));
}

function num(s) {
  if (s === null || s === undefined) return NaN;
  let t = String(s).replace(/[\u066B\u066C\u0020\u00A0\u200F\u200E\u200B]/g, m =>
    m === '\u066B' ? '.' : m === '\u066C' ? '' : '');
  t = t.replace(/,/g, '');
  return parseFloat(t);
}
async function readShiftEnd(page) {
  await page.goto(BASE + '/pos/shift-end', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  const rows = await page.evaluate(() => {
    const out = {};
    Array.from(document.querySelectorAll('.shift-summary tr')).forEach(r => {
      const cells = Array.from(r.querySelectorAll('th,td')).map(c => (c.textContent || '').trim());
      if (cells.length >= 2) out[cells[0]] = cells[cells.length - 1];
    });
    const kv = {};
    const mtxt = (document.querySelector('.shift-kv') || {}).textContent || '';
    const m = mtxt.match(/الافتتاحي[:\s]*([\d.,]+)/);
    if (m) kv['رصيد الفكة الافتتاحي'] = m[1];
    return { rows: out, kv, text: document.body ? document.body.innerText : '' };
  });
  return {
    open: rows.kv['رصيد الفكة الافتتاحي'] || rows.rows['رصيد الفكة الافتتاحي'],
    sales: rows.rows['مبيعات نقدية (POS)'],
    refunds: rows.rows['مردودات نقدية (POS)'],
    fins: rows.rows['إضافات نقد (Float-In)'],
    fout: rows.rows['سحوبات نقد (Cash-Out)'],
    expected: rows.rows['الرصيد المتوقَّع'],
    text: rows.text
  };
}

async function closeShift(page, counted) {
  await fillByLabel(page, 'المبلغ المعدود فعلياً', counted);
  await page.waitForTimeout(900);
  const t = await bodyText(page);
  log('VAR_PRE_' + counted + '=' + (t.includes('الفرق') ? 'shown' : 'MISSING'));
  await clickByText(page, 'تأكيد الإغلاق وتسوية الفرق');
  await page.waitForTimeout(2600);
  const t2 = await bodyText(page);
  log('CLOSE_' + counted + '_OK=' + (t2.includes('أُغلقت الوردية')));
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

  // ── A) فتح وردية 500 → الرصيد المتوقّع 500 (أرقام معروفة يدوياً) ──
  await openShift(page, '500');
  let s1 = await readShiftEnd(page);
  log('A_OPEN_500=' + s1.open + ' A_EXPECTED=' + s1.expected + ' A_OK=' + (s1.open === '500.00' && s1.expected === '500.00'));

  // ── B) بيع نقدي حقيقي عبر /pos → إثراء بند المبيعات النقدية ──
  await page.goto(BASE + '/pos', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(2200);
  const itemCount = await page.evaluate(() => document.querySelectorAll('.pos-item').length);
  log('POS_ITEMS=' + itemCount);
  if (itemCount > 0) {
    await page.locator('.pos-item').first().click();
    await page.waitForTimeout(1800);
    try { await fillByLabel(page, 'المبلغ المستلم', '99999'); } catch (e) {}
    await page.waitForTimeout(600);
    await page.locator('button.pos-checkout, .pos-checkout').first().click({ timeout: 9000 }).catch(() => {});
    await page.waitForTimeout(5000);
  }
  let s2 = await readShiftEnd(page);
  const saleDone = s2.sales !== null && num(s2.sales) !== 0;
  const expHand = saleDone ? (500 + num(s2.sales)) : 500;
  log('B_AFTER_SALE sales=' + s2.sales + ' expected=' + s2.expected + ' expHand=' + expHand
    + ' MATCH=' + (s2.expected && Math.abs(num(s2.expected) - expHand) < 0.005));

  // ── C) إغلاق بزيادة عمدية 2.50 ──
  const countedOver = (num(s2.expected) + 2.50).toFixed(2);
  await closeShift(page, countedOver);

  // ── D) وردية ثانية: عجز 0.02 (فتح 50 → معدود 49.98) ──
  await openShift(page, '50');
  await page.goto(BASE + '/pos/shift-end', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  await closeShift(page, '49.98');

  // ── E) وردية ثالثة: صفر تماماً (فتح 70 → معدود 70) ──
  await openShift(page, '70');
  await page.goto(BASE + '/pos/shift-end', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);
  await closeShift(page, '70,00');

  // ── F) سجل الورديات (smoke لديه Admin) ──
  await page.goto(BASE + '/pos/shifts-history', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const ht = await bodyText(page);
  log('HISTORY_HEAD=' + ht.includes('سجل الورديات') + ' ROWS=' + (ht.match(/smoke@erp\.com/g) || []).length);

  fs.writeFileSync('D:\\ERPSystem\\_shift_e2e_out.txt', out.join('\n'), 'utf8');
  console.log(out.join('\n'));
  await browser.close();
}

main().catch(e => {
  out.push('E2E_ERR=' + e);
  fs.writeFileSync('D:\\ERPSystem\\_shift_e2e_out.txt', out.join('\n'), 'utf8');
  console.log(out.join('\n'));
  process.exit(1);
});