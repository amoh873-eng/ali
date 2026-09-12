const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 500)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 900 } });
  const context = await browser.newContext();
  const page = await context.newPage();

  const pageErrors = [];
  const consoleErrs = [];
  const negotiateCount = { login: 0, pos: 0 };
  const wsSeen = { login: [], pos: [] };
  let phase = 'login';

  page.on('response', res => {
    if (res.url().includes('_blazor/negotiate')) {
      if (phase === 'login') negotiateCount.login++;
      else negotiateCount.pos++;
    }
  });
  page.on('websocket', ws => {
    if (phase === 'login') wsSeen.login.push(ws.url().replace(BASE, ''));
    else wsSeen.pos.push(ws.url().replace(BASE, ''));
  });
  page.on('pageerror', e => pageErrors.push((e && e.message || '').slice(0, 300)));
  page.on('console', m => {
    if (m.type() === 'error') consoleErrs.push(m.text().slice(0, 300));
  });

  try {
    // ── (أ) تحميل /login من نافذة خاصة جديدة بلا جلسة ──
    log('=== STEP A: FRESH incognito /login ===');
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(3000);

    log('PAGE_ERRORS=' + JSON.stringify(pageErrors));
    log('PAGE_ERR_MATCH_JSON=' + pageErrors.some(e => /Unexpected token '<'|not valid JSON|Circuit host not initialized/.test(String(e))));
    log('NEGOCIATE_REQUESTS_LOGIN=' + negotiateCount.login + ' (EXPECT 0)');
    log('WEBSOCKETS_LOGIN=' + JSON.stringify(wsSeen.login) + ' (EXPECT [])');
    log('CONSOLE_ERRORS_LOGIN=' + JSON.stringify(consoleErrs.filter(e => /antiforgery|negotiat|circuit|Unexpected token/i.test(e))));

    // ── (ب) تأكد أن صفحة الدخول لا تزال تعمل ──
    const hasForm = await page.evaluate(() => !!document.querySelector('form[action="/login/handler"]'));
    const hasTokenInput = await page.evaluate(() => !!document.querySelector('input[name="__RequestVerificationToken"]'));
    log('LOGIN_FORM_PRESENT=' + hasForm + ' TOKEN_INPUT=' + hasTokenInput);

    await page.fill('input[name="Email"]', 'smoke@erp.com');
    await page.fill('input[name="Password"]', 'Test@1234');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(3000);
    log('LOGIN_WORKS_URL=' + page.url());

    // ── (ج) انتقل إلى /pos (صفحة تفاعلية) وتحقق أن الدائرة تُفتح وتعمل ──
    phase = 'pos';
    pageErrors.length = 0;
    consoleErrs.length = 0;
    log('=== STEP C: AUTHED /pos (interactive) ===');
    await page.goto(BASE + '/pos', { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.pos-item').length > 0, null, { timeout: 30000 }).catch(() => log('NO_ITEMS_TIMEOUT'));
    await page.waitForTimeout(2500);

    log('NEGOCIATE_REQUESTS_POS=' + negotiateCount.pos + ' (EXPECT >=1)');
    log('WEBSOCKETS_POS=' + JSON.stringify(wsSeen.pos) + ' (EXPECT non-empty — circuit open)');
    log('PAGE_ERRORS_POS=' + JSON.stringify(pageErrors));

    // تفاعل حقيقي: إضافة صنف للسلة (Blazor interactive handler)
    const cartBefore = await page.evaluate(() => document.querySelectorAll('.pos-line').length);
    await page.evaluate(() => { const b = document.querySelector('.pos-item'); if (b) b.click(); });
    await page.waitForTimeout(1200);
    const cartAfter = await page.evaluate(() => document.querySelectorAll('.pos-line').length);
    log('CART_BEFORE=' + cartBefore + ' AFTER_ADD=' + cartAfter + ' INTERACTIVE_WORKS=' + (cartAfter > cartBefore));

    // نافذة منبثقة (MudDialog) للتأكد أيضاً
    const dialogBefore = await page.evaluate(() => document.querySelectorAll('.mud-dialog').length);
    await page.evaluate(() => { const s = document.querySelector('.pos-topbar-right .mud-icon-button'); if (s) s.click(); });
    await page.waitForTimeout(800);
    const dialogAfter = await page.evaluate(() => document.querySelectorAll('.mud-dialog').length);
    log('DIALOG_BEFORE=' + dialogBefore + ' AFTER_CLICK=' + dialogAfter + ' (>=1 means interactive UI works)');
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 500));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_final_verify.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();