# HANDOFF — ERP System Checkpoint

**Checkpoint date:** 2026-09-12
**Branch:** `localization-cleanup` (upstream `origin/localization-cleanup`)
**Last verified commit:** `7508577` — clean working tree (only `HANDOFF.md` untracked)
**Previous checkpoint:** `d217ac5` (barcode labels perf + one-click launcher + Blazor login fix)

> This file is a working-tree note (untracked, not committed) so the next session can
> resume cold. Re-read it fully before doing anything else.

---

## ⚡ Session 2026-09-12 — POS Sales Return + Cash Transfer (Safe ↔ Till) + till posting redirect

### A) Feature 1 — Sales return directly from POS (`مردود من نقطة البيع`)
- **«إرجاع / Return»** button on the POS screen opens `PosReturnDialog` (`src/Web/Components/Pages/Pos/`).
  Lookup the original invoice by **invoice number** (fastest), **customer name/code**, or **date**
  (server-side `ISalesInvoiceService.SearchPostableInvoicesAsync` — posted invoices only).
- Shows the invoice lines with **max = remaining returnable** (invoice sold − prior partial returns,
  new `ISalesReturnService.GetRemainingReturnableAsync`), **full return** (one tap) or **partial** per line.
- **Reuses the existing `SalesReturnService.CreateAsync`** — no new journal/stock logic.
  Refund method = **cash from the till** (store credit not supported in the system → out of scope).
- **Role gate:** completing a return requires `Sales`/`Admin`/`SuperAdmin`; a cashier with only `Pos`
  sees a lock warning + disabled save.
- Evidence: `FunctionalTests/Browser/pos_return_e2e.js` — sold 3 × item, partial-returned 1 via POS,
  success snackbar, stock `+1.00` (SalesReturnIn), reversing entries correct.

### B) Feature 2 — Cash transfer between the Main Safe and the Till Drawer
- **New account 1105 «نقدية - درج الكاش» / "Cash - Till Drawer"** (Asset, Debit, parent = الأصول),
  separate from **1100 «الصندوق»** (the only pre-existing cash account — there was NO «الخزنة»).
  Seeded via migration `20260912063912_AddCashDrawerTransactions` + runtime `SeedSalesAccounts`.
- Entity `CashDrawerTransaction` (`Id, Type FloatIn/CashOut, Amount, Timestamp, CashierUserId, Notes, JournalEntryId`)
  + `CashDrawerService` + `ICashDrawerService` + EF config. Each transfer posts a **balanced**
  `JournalEntryType.CashTransfer` entry **atomically**:
  - **Float-In** (Safe→Till): Debit 1105 / Credit 1100
  - **Cash-Out** (Till→Safe): Debit 1100 / Credit 1105
- UI: **«تحويل نقد / Cash Transfer»** button on POS → `CashTransferDialog` (direction/amount/note);
  log page `/pos/cash-transfer` (`CashTransfers.razor`) filterable by date / direction / cashier.
- Evidence: `FunctionalTests/Browser/cash_transfer_e2e.js` — Float-In 100 + Cash-Out 50; DB shows
  `JE-...-0001 Debit 1105/ Credit 1100`, `JE-...-0003 Debit 1100 / Credit 1105`.

### C) POS cash posting redirect (user-confirmed decision — IMPORTANT)
- POS **cash sales** now post to **1105 (till)** (`SalesInvoiceService.Create.cs` — receivable account picks
  till for `IsPos && PaymentMethod=Cash`). POS-origin **cash-invoice returns** credit **1105** too
  (`SalesReturnService.Create.cs`). **Regular-module** cash sales/returns stay on **1100**.
- Verified: sale `SI-...-0010` → `JE` Debit 1105 132.48; its return `SR-...-0002` → Credit 1105 44.16.
- Future shift-reconciliation math (NOT built): `Expected till = Sum(Float-Ins) + cash sales − cash refunds − Sum(Cash-Outs)`.

### D) Also committed in `7508577`
- The **pre-existing uncommitted Stock Write-Off work** (entity/service/pages, WO numbering, COGS/inventory
  entry, batch-aware, `tools/DataCleanup/` + `tools/WriteOffHarness/`) was sitting in the tree at the last
  checkpoint — committed together because shared files (AppDbContext, DependencyInjection, model snapshot)
  are entangled. Verify it if you pick it up next.

### E) DbContext concurrency hardening (`c921906`)
- **Symptom:** «A second operation was started on this context instance» — Blazor Server shares one
  **scoped DbContext per circuit**, so two overlapping DB ops (rapid row clicks, search fired while a
  query is in flight, double-click Save) collide.
- **Fix:** re-entry guards (`_busy`/`_loading`, set synchronously **before** the first await, reset in
  `finally`) on: `PosReturnDialog` (search/row-select/submit), `CashTransferDialog` submit,
  `CashTransfers.LoadAsync`, and the inventory `WriteOffs.LoadAsync` + `WriteOffFormDialog` submit.
- **Verified:** `FunctionalTests/Browser/dbcontext_race_stress.js` (concurrent search + row clicks +
  triple-click Save) → zero DbContext errors in the server log and exactly **one** persisted transfer.

### F) POS F1–F7 keyboard shortcuts (`f50d3cc`)
- **Mechanism:** new `src/Web/wwwroot/posShortcuts.js` (window **bubble** keydown listener, a single
  `SHORTCUT_KEYS = Set['F1'..'F7']` lookup — add F8..F12 there + in the C# table later) that only acts on
  those keys (suppresses browser defaults: F1 help / F5 refresh) and forwards to `[JSInvokable]
  Pos.HandleShortcut`. Page-scoped lifecycle on `Pos.razor` (`@implements IAsyncDisposable`): the ES module
  is imported in `OnAfterRenderAsync(firstRender)` and detached in `DisposeAsync` — active only while the
  POS is mounted, no cross-session leak.
- **C# lookup table** in `Pos.razor` maps each key to the **exact existing button method**:
  F1→NewInvoice, F2→CompleteSale, F3→Cash, F4→Card, F5→HoldSaleAsync, F6→OpenReturnAsync, F7→OpenCashTransferAsync.
  `HandleShortcut` calls `StateHasChanged()` after (a `[JSInvokable]` doesn't auto-re-render like `@onclick`).
- **Disabled-state respected by construction** (methods guard `_busy`/empty cart like the buttons); **F3/F4
  additionally no-op during `_busy`** (`GuardedNotBusy`) so a mid-checkout Card press can't flip an
  in-flight cash sale to card.
- **Tooltips:** `title` on the 7 buttons via new resx keys `PosShortcutF1..F7` (e.g. "New Invoice (F1)").
  **Visible badges (`11256d6`):** each button also shows a small keyboard-style `F1..F7` badge (`.pos-kbd` +
  resx `PosShortcutKeyF1..F7`) so the cashier sees the key without hovering.
- **Tests:** `pos_shortcuts_e2e.js` (all 7 keys, F5-no-refresh, disabled states) + `pos_shortcuts_busy_e2e.js`
  (fresh page per scenario; F6/F7/F3/F4 during `_busy` are harmless no-ops, sales still post as cash).
  **Barcode regression:** `pos_shortcuts_e2e.js` + `pos_scan_barcode_test.js` pass with the listener active
  (the listener only sees F-keys; `scaleBarcode.js` capture handler ignores them). Note `scale_scan_test.js`
  currently no-ops because the test DB has no item matching scale SKU `11500` (WeightBarcodeRule is set but
  the catalog lacks the item) — a pre-existing data gap, not a regression.

### G) Reports Dashboard landing page (`b0c9c66`)
- **`/reports`** now shows a professional Reports Dashboard (`ReportsDashboard.razor` + scoped
  `.razor.css`): hero header with a **reports logo** (inline SVG), **6 live KPI cards** (cash on hand
  1100+1105, AR 1200, AP 2200, inventory value, low-stock items, month-to-date sales), and **12 report
  cards** grouped into Financial statements / Business analytics / Tax & custom — each linking to the
  existing page.
- All DB loads run inside `ScopeFactory.CreateScope()` (same DbContext-isolation pattern as Home).
- **Reports center stays** at `/reports/custom` (+`/reports/center`); removed the duplicate `/reports`
  mapping from `CustomReports.razor`. Added missing `ReportsDashboard` resx key (also fixes raw-key text
  in TopNav) + `RepDash*`/`ReportsCenter`/footer keys (ar+en, `&` escaped).
- Verified: `/reports` = 6 KPIs / 12 cards / logo with zero page errors; `/reports/custom` +
  `/reports/center` work; `dedup_check.js` green; full solution build 0 errors.

---

### A) شاشة طباعة ملصقات الباركود — إصلاح «اللود الضخم» و تعارض DbContext
- **المشكلة:** فتح `/inventory/barcode-labels` كان يحمّل **كل الأصناف** في الذاكرة ويرسمها
  كلها (تجميد الجهاز) + خطأ `A second operation was started on this context instance`
  (مشاركة نفس DbContext بين استعلامات الصفحة ومكونات الدائرة).
- **الحل:**
  - الجدول تحوّل إلى **`MudTable ServerData`** (خادمي مقسّم — يجلب أول 50 فقط،
    `LoadServerData(TableState, CancellationToken)` بالتوقيع الصحيح).
  - خدمات جديدة في `ItemService.Labels.cs`: `SearchPageAsync` (ILike + فئة + ترقيم
    COUNT/SKIP/TAKE مع إلغاء)، `GetByIdsAsync` (المحدّد فقط)، `GetIdsByCategoryAsync`
    و `GetLastBulkImportIdsAsync` (أكواد فقط مع حاجز 20,000) — DTO `ItemPageDto`.
  - **كل استعلام داخل نطاق `CreateAsyncScope()`** → نسخة AppDbContext مستقلة لكل طلب
    (يمنع تعارض DbContext). Debounce 300ms للبحث مع `CancellationTokenSource` يُلغى في `Dispose()`.
- **مميزات إضافية:** معاينة PDF في iframe + توليد/تنزيل + عدد النسخ لكل صنف أو موحّد.
- **الأدلة:** `barcode_labels_e2e.js` (فتح/تحديد 3/معاينة/تنزيل ✅)،
  `barcode_labels_perf_e2e.js` — **توليد 120 ملصقاً في 0.64 ثانية**،
  سجل الخادم بلا أي خطأ DbContext.

### B) تشغيل الموقع بضغطة واحدة (أداة جديدة للمستخدم)
- **`_start_now.ps1`** في جذر المستودع: يبدأ PostgreSQL، يوقف نسخة قديمة من الموقع،
  يشغّله على `http://localhost:5186`، ينتظر الجاهزية، ويفتح المتصفح (وضع `-Quiet` للتحقق الصامت).
  ترميز **UTF-8 مع BOM** (مهم: PowerShell يقرأ ANSI افتراضياً فينكسر العربي).
  السكربت **مختبَر فعلياً** (4 مراحل كاملة بلا أخطاء، الموقع يصل للاستماع).
- **ملف سطح المكتب:** `C:\Users\User az\Desktop\تشغيل نظام ERP.bat` (نقرة مزدوجة = تشغيل كامل).
- المسارات: dotnet = `C:\Users\User az\.dotnet10\dotnet.exe`، PostgreSQL = `D:\PostgreSQL`.

### C) ملاحظات للتشغيل اليدوي
1. `D:\PostgreSQL\pgsql\bin\pg_ctl.exe -D D:\PostgreSQL\data -l D:\PostgreSQL\pglog.log start`
2. `& 'C:\Users\User az\.dotnet10\dotnet.exe' run --project 'D:\ERPSystem\src\Web\ERPSystem.Web.csproj'`
3. المتصفح: `http://localhost:5186` — الدخول: `smoke@erp.com` / `Test@1234`.

---

## 1. What is confirmed working & verified (with evidence)

### Regression suite (5 tests, PASSED at commit `9185f96` and still green)
1. **Login** — no antiforgery issues, no console errors.
2. **Journal-sale** — one sale produces two journal entries, correct numbering
   (e.g. JE-…-0005/0006 for SI-…-0018; 0007–0012 for SI-…-0019/20/21).
3. **Case-insensitive search** — Items 2, Customers 3, Suppliers 2/4.
4. **Bulk import** — 500 rows in ~0.5s (475 created / 15 skipped / 10 failed), re-import idempotent.
5. **POS under load** — grid limit 20→20 and 300→300; 3 rapid consecutive sales with
   **zero** "A second operation was started" errors.

### Blazor login circuit fix (shipped in `d217ac5`)
- **Root cause fixed:** `FallbackPolicy = RequireAuthenticatedUser` redirected Blazor's
  `/_blazor/initializers` to the login HTML → "Unexpected token '<' ... not valid JSON".
- **Fix:** `Program.cs` middleware (anonymous → `[]` JSON), `Login.razor`
  `[ExcludeFromInteractiveRouting]`, `App.razor` conditional render mode via
  `HttpContext.AcceptsInteractiveRouting()`.
- **Verified:** `/login` fresh → 0 negotiate, 0 WebSocket, 0 console errors;
  `/pos` → circuit opens (WebSocket `ws://…/_blazor?id=…`), adding items works.

### pg_trgm indexing (in `d217ac5`)
- Migration `20260905120000_AddPgTrgmIndexes` (+ Designer): `CREATE EXTENSION pg_trgm;`
  + 10 GIN indexes (Items/Customers/Suppliers on Code/NameAr/NameEn/Barcode).
- Applied at boot; migration row + extension + indexes present.
- **Honest note:** at the current tiny data volume, plans stay `Seq Scan`; a long-pattern
  EXPLAIN provably uses `Bitmap Index Scan on idx_items_namear_trgm`. Expected to help as
  data grows. (Rule observed: Program.cs/App.razor/Login.razor/POS fixes untouched.)

### ItemBatch expiry tracking (in `d217ac5`, independent entity per requirement)
- New `ItemBatch` + `Item.TracksBatches` (default false). Did **not** touch legacy
  `StockBatch`/`TracksExpiry`. DTOs/interfaces/service/helper wired.
- `PurchaseInvoiceService` enforces ExpiryDate for `TracksBatches` items and creates/merges
  batches (merge only when same batch number + expiry + item + warehouse).
- `SalesInvoiceService.Create` allocates FEFO (usable = exp ≥ today, nearest first) and
  **blocks selling expired**; consistency guard: `SUM(ItemBatches.Quantity) ==
  SUM(StockMovements.Quantity)` in same transaction.
- Page `/inventory/expiring-batches` + TopNav link + POS near-expiry orange indicator.
- **Verified:** seeded batches A(exp tomorrow,12)/B(+30d,20)/EXP(expired,5); sale of 15 →
  A 12→0, B 20→17, EXP untouched; `SUM_BATCHES=22==SUM_MOVEMENTS=22`; sale of 18 (only 17
  usable) rejected «المتاح غير المنتهي: 17، المطلوب: 18.»; after zeroing usable, sale of 1
  rejected «لا توجد دُفعات صالحة غير منتهية هذا الصنف…»; invoice `SI-20260906-0001`;
  final consistency 5==5.
### (B) Rare scan-loss edge case (purchase barcode receiving) — mitigation approach pending
- During rapid, *varied-item* scanning under artificially fast automated tests, an
  occasional scan is lost (or, rarely, duplicates a line) because Blazor's re-render can
  swap the scan-field node mid-burst.
- **Not observed with a physical scanner** (hardware bursts push one scan at a time with
  human-paced pauses). Same-barcode rapid repeats are rock-solid (5/5 every run); a wrong
  quantity has never been produced — only a missing/duplicate line the operator can re-scan.
- `purchaseScan.js` already re-`refreshTarget()`s on disconnected nodes, but the residual
  race remains. **Pending:** queue-based fix vs. a visual "scanned vs added" counter
  safeguard. Do not silently pick one.

---

## 4. Deferred — explicitly NOT started (do not assume half-done)

- **Recipe/BOM item costing**
- **Table management + bill splitting**
- **Kitchen display / ticket printing**
- **Shelf-restocking mobile app** (incl. its unresolved online/offline architecture decision)
- **Two-factor authentication for admin accounts**

None of these have any code, migrations, or partial work in the tree.

---

## 5. Environment setup (cold resume)

### Starting the stack
1. **PostgreSQL first** (manual; no Windows service):
   `D:\PostgreSQL\pgsql\bin\pg_ctl.exe -D D:\PostgreSQL\data start`
   (status check: `…pg_ctl.exe status -D D:\PostgreSQL\data`)
2. **App** (from `D:\ERPSystem`):
   `C:\Users\User az\.dotnet10\dotnet.exe run --project src/Web/ERPSystem.Web.csproj --urls http://localhost:5186`
3. App listens on **http://localhost:5186**; migrations run automatically at boot
   (incl. the pg_trgm + ItemBatch ones — they are idempotent/no-op if already applied).

### Credentials
- Login: `smoke@erp.com` / `Test@1234` (dev smoke account).
- **No secrets in the repo.** DB password was removed in `b949bef`; app reads
  user-secrets / env var. For ad-hoc `psql` use, the password is referenced in existing
  root-level gitignored scratch scripts (e.g., `_run_*.ps1` with `$env:PGPASSWORD=…`);
  otherwise ask the user. Convention: `psql -h localhost -p 5432 -U erp_app -d ERPSystemDb`.

### Tests (reusable Playwright scripts — all in `FunctionalTests/Browser/`)
- `pos_scan_barcode_test.js` — POS barcode UX (green/red/auto-clear/increments).
- `barcode_receive_test.js`, `rapid_scan_test.js` — purchase barcode receiving incl.
  the 5× repeat-accumulate case.
- `batch_sale_check.js`, `batch_expired_buy.js`, `batch_expired_only.js` — ItemBatch FEFO.
- `pg_trgm_sql_capture.js` — index plan capture; `diag_*.js` — login/negotiate diagnostics.
- `pos_auto_clear_probe.js` — diagnostic probe (auto-clear timing).
- Run: `node FunctionalTests/Browser/<file>.js` from `D:\ERPSystem` (needs the app up).

### Scratch hygiene notice
- The repo root holds many gitignored scratch files (`/_*`, `/*.log`, `run_*.txt`…) from
  prior sessions. They are **ignored on purpose** and must NOT be committed. The
  card-reconciliation backup CSV is one of them — treat it as precious until decision (A).
- Designer migration files are **full-schema EF snapshots** (auto-generated) — they
  legitimately contain many table/column names; not secrets.