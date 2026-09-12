// Probe: verify the Reports Dashboard at /reports (KPIs + logo + cards) and that
// /reports/custom + /reports/center still work, with no page errors.
const { chromium } = require('playwright');
const fs = require('fs');
const BASE = 'http://localhost:5186';
const out = [];
function log(s) { out.push(String(s).replace(/[^\x00-\x7F]/g, '?').slice(0, 400)); }

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true, viewport: { width: 1440, height: 1000 } });
  const page = await browser.newPage();
  const errs = [];
  page.on('pageerror', e => errs.push((e && e.message || '').slice(0, 200)));
  try {
    await page.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 60000 });
    await page.fill('input[name="Email"]', 'smoke@erp.com', { timeout: 30000 });
    await page.fill('input[name="Password"]', 'Test@1234', { timeout: 30000 });
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}), page.click('button.login-btn')]);
    await page.waitForTimeout(2500);

    // ── /reports (اللوحة) ──
    await page.goto(BASE + '/reports', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForFunction(() => document.querySelectorAll('.repdash-kpi').length > 0, null, { timeout: 20000 }).catch(() => log('KPI_TIMEOUT'));
    await page.waitForTimeout(3000);
    const res = await page.evaluate(() => ({
      kpiCards: document.querySelectorAll('.repdash-kpi').length,
      reportCards: document.querySelectorAll('.repdash-card').length,
      hasLogo: !!document.querySelector('.repdash-logo svg'),
      hasHero: !!document.querySelector('.repdash-hero h1'),
      heroTitle: (document.querySelector('.repdash-hero h1') || {}).textContent || '',
      cash: (document.querySelector('.repdash-kpi-value') || {}).textContent || '',
      sections: Array.from(document.querySelectorAll('.repdash-section-head h2')).map(h => h.textContent.trim()),
      firstCardHref: (document.querySelector('.repdash-card') || {}).getAttribute('href') || ''
    }));
    log('DASH_KPI_CARDS=' + res.kpiCards);
    log('DASH_REPORT_CARDS=' + res.reportCards);
    log('DASH_HAS_LOGO=' + res.hasLogo);
    log('DASH_HAS_HERO=' + res.hasHero);
    log('DASH_HERO_TITLE=' + res.heroTitle);
    log('DASH_FIRST_CARD_HREF=' + res.firstCardHref);
    log('DASH_SECTIONS=' + JSON.stringify(res.sections));
    await page.screenshot({ path: 'D:/ERPSystem/_reports_dashboard.png', fullPage: true });

    // ── /reports/custom (مركز التقارير) ──
    await page.goto(BASE + '/reports/custom', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(2500);
    const customText = await page.evaluate(() => (document.body ? document.body.innerText : ''));
    log('CUSTOM_CENTER_TEXT=' + customText.slice(0, 80));

    // ── /reports/center ──
    await page.goto(BASE + '/reports/center', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(2000);
    const centerUrl = page.url();
    log('CENTER_URL=' + centerUrl);

    log('PAGE_ERRORS=' + JSON.stringify(errs));
  } catch (e) {
    log('FATAL=' + (e && e.message || '').slice(0, 300));
  } finally {
    fs.writeFileSync('D:/ERPSystem/_reports_dash_probe.txt', out.join('\n'), 'utf8');
    await browser.close();
  }
})();