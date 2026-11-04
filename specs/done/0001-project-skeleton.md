# Spec 0001 — Proje iskeleti
<!-- Small behavior changes use TEMPLATE-mini.md — brevity is sanctioned, discipline is not. -->

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0001-plan.md`

## Intent
Geliştirici (tek kişilik ekip), ilk iş özelliğine başlamadan önce bootstrap'ta kararlaştırılan
mimariyi çalışır hâlde görmek istiyor: .NET 10 backend, modül sınırları, React web arayüzü,
PostgreSQL ve hepsini tek komutla doğrulayan `scripts/check`. Başarı: `./scripts/check` gerçek
adımlarla (derleme, format, birim/mimari/entegrasyon testleri, web lint/tip/test/derleme, güvenlik
taraması) yeşil döner, geride hiçbir süreç bırakmaz ve modül sınırları testle korunur. Kullanıcıya
dönük tek davranış, uygulamanın ve veritabanının ayakta olup olmadığını gösteren bir sistem durumu
kontrolüdür. Hiçbir iş özelliği (ürün, ihale, teklif, giriş/kayıt) bu spec'te yapılmaz.

## Requirements
- R-1: Backend, `docs/architecture.md`'deki beş modülün (Catalog, Auction, Identity, Notification,
  Audit) her biri için Domain / Application / Infrastructure katmanlarını ve tek bir uygulama
  barındırıcısını (host) içerir — 15 katman projesinin hepsi şimdi oluşturulur. Modüller bu
  spec'te iş davranışı içermez.
- R-2: Sistem bir **durum kontrolü** sunar: uygulama ve veritabanı erişilebilirse "sağlıklı",
  veritabanına erişilemiyorsa "sağlıksız" der. Yanıt makine okunur bir durum değeridir ve iç ayrıntı (bağlantı dizesi, hata metni,
  stack trace) içermez. Durum kontrolü kimlik doğrulama gerektirmez (`docs/security.md`'de açıkça
  anonim işaretlenir).
- R-3: Web arayüzü Türkçe bir ana sayfa gösterir: uygulama adı ("Polsan Auction") ve sistem
  durumu (yükleniyor / sağlıklı / sağlıksız-hata).
- R-4: `docs/architecture.md` "Forbidden dependencies" maddeleri 1–3, 5 ve 6 otomatik mimari
  testlerle korunur; bir ihlal eklendiğinde test kırmızıya düşer. (Madde 4 — denetim tablosu — ve
  7 — harici CDN — ilgili özelliklerle gelir.)
- R-5: `scripts/check.conf` adımları etkinleştirilir; full ve `--fast` profilleri gerçek
  adımlarla çalışır, test adımları seçiciyle (Category) bölünür.
- R-6: Check hiçbir süreç ve veri bırakmaz (`docs/testing.md`, Resource hygiene); residue testi
  her iki profilde temizdir.
- R-7: CI tanımı (`.github/workflows/check.yml`) check'in full profilini çalıştırabilecek
  araçları (.NET 10 SDK, Node LTS, Docker) kurar.
- R-8: Geliştirici yerelde veritabanını tek komutla ayağa kaldırıp uygulamayı çalıştırabilir;
  nasıl yapılacağı README'de yazılıdır.

## User experience
- Flow: kullanıcı ana sayfayı açar → uygulama adı ve sistem durumu görünür.
- States: yükleniyor (durum sorgulanıyor) · sağlıklı · sağlıksız/hata (sorgu başarısız ya da
  veritabanı erişilemez). Boş durum ve yetki-yok durumu bu sayfada yoktur (veri ve giriş yok).
- Design reference: none — the flow above is the design.
- Accessibility: durum metinle de ifade edilir (yalnızca renkle değil); sayfa klavyeyle gezilebilir;
  `lang="tr"`.

## Constraints & out of scope
- Yığın ve sürümler `docs/conventions.md`'ye uyar; PostgreSQL **18** (geliştirme ve test aynı imaj).
- Yerel geliştirme veritabanı yalnızca bir compose dosyasıyla; check onu kullanmaz, testler kendi
  geçici konteynerini açar (her koşu taze DB).
- AC-5 kanıtı: verify'da kod geçici olarak bozulur; ihlal commit'lenmez.
- AC-9: uzak depo SHIP'e kadar bağlanmazsa kriter `specs/backlog.md`'ye ertelenir.
- Split: tek yetenek (çalışır iskelet), 10 kriter — bölünmedi (insan onayı, 2026-10-29).
- Out of scope: tüm iş özellikleri (ürün, ihale, teklif, bildirim içeriği), giriş/kayıt/roller,
  SignalR hub'ı, e-posta gönderimi, UI bileşen kütüphanesi seçimi, dağıtım (deployment), e2e testleri.
- Yeni bağımlılıklar yalnızca `docs/conventions.md` / `docs/security.md`'de onaylı listeden
  (EF Core + Npgsql, xUnit, Testcontainers, NetArchTest, React, Vite, Vitest, Testing Library,
  ESLint, Prettier); listede olmayan her paket planda gerekçelendirilir.

## Acceptance criteria
- [x] AC-1 — `./scripts/check` (full) gerçek adımlarla çalışır ve `CHECK GREEN (full, …)` verir. — Kanıt: CI [run 41477725539](https://github.com/ferzurum/polsanauction/actions/runs/41477725539) (SHA 8c8045b) `CHECK GREEN (full, 11 steps, 77 s)`; QA worktree `CHECK GREEN (full, 11 steps, 179 s)`.
- [x] AC-2 — `./scripts/check --fast` yalnızca `[fast]` adımlarını çalıştırır ve yeşil döner. — Kanıt: QA `CHECK GREEN (fast, 7 steps, 69 s)`, `skip` yalnızca full-only adımlar; CI residue job'ında `CHECK GREEN (fast, 7 steps, 21 s)`.
- [x] AC-3 — Veritabanı erişilebilirken durum kontrolü "sağlıklı" döner (gerçek PostgreSQL ile entegrasyon testi). — Kanıt: `HealthEndpointTests.Health_DatabaseReachable_ReturnsHealthy` (full); kasıtlı `Degraded` → kırmızı (VERIFY).
- [x] AC-4 — Veritabanı erişilemezken durum kontrolü "sağlıksız" döner ve yanıtta iç ayrıntı bulunmaz. — Kanıt: `HealthEndpointUnreachableDatabaseTests.Health_DatabaseUnreachable_ReturnsUnhealthyWithoutDetails` (full), `HealthResponseTests.*` (fast); `detail` yazdırma ve catch→Healthy bozmaları → kırmızı (VERIFY).
- [x] AC-5 — Mimari testler R-4 maddelerini doğrular; her madde için geçici bir ihlal eklendiğinde ilgili test kırmızıya düşer. — Kanıt: `LayerDependencyTests.*`, `ModuleBoundaryTests.*`, `MoneyTypeTests.*` (fast); madde 1, 2, 3, 6 kasıtlı ihlal → ilgili test kırmızı, madde 5 → RS0030 derleme hatası (VERIFY). Paket referansı kontrolü B-1'de.
- [x] AC-6 — Ana sayfa yükleniyor, sağlıklı ve hata durumlarını Türkçe metinle gösterir (bileşen testleri). — Kanıt: `web/src/App.test.tsx` 6 test (fast), G-1 dahil; durum eşlemesini ters çevirme → kırmızı (VERIFY).
- [x] AC-7 — `./scripts/residue` ve `./scripts/residue --fast` temiz (survivor yok); baseline ve iki ölçüm raporlanır. — Kanıt: CI [run 41477725539](https://github.com/ferzurum/polsanauction/actions/runs/41477725539) residue job: full baseline 39 süreç → 2 koşuda 0 survivor (−13/−21 MB) `RESIDUE CLEAN (full, 2 runs)`; fast baseline 36 → 0 survivor (−157/−141 MB) `RESIDUE CLEAN (fast, 2 runs)`. Yerel Windows ölçümleri boşta olmayan makinede yalnızca check dışı survivor gösterdi (A-5).
- [x] AC-8 — Test bölme doğru: fast ve full-only test adımlarının sayıları toplamı tüm testlerin sayısına eşittir ve seçiciler eklenmeden önceki sayıyla aynıdır. — Kanıt: `dotnet test -c Release --no-build --list-tests`: seçicisiz 23 = `--filter-not-trait "Category=Integration"` 21 + `--filter-trait "Category=Integration"` 2; vitest 6 (QA).
- [x] AC-9 — CI tanımı .NET 10 SDK ve Node LTS kurar ve `./scripts/check` (full) çalıştırır; kanıt ilk push'taki yeşil CI koşusudur. — Kanıt: CI [run 41477725539](https://github.com/ferzurum/polsanauction/actions/runs/41477725539) `check` job yeşil, SHA 8c8045b. Not: ilk push kırmızıydı (script exec biti, `Permission denied`) — fix-3 / A-6 sonrası yeşil.
- [x] AC-10 — README'deki yerel çalıştırma adımları izlendiğinde uygulama açılır ve ana sayfa "sağlıklı" gösterir. — Kanıt: [PR #1 yorumu](https://github.com/ferzurum/polsanauction/pull/1#issuecomment-6583387068) — bağımsız QA, README adımları: `/api/health` doğrudan ve Vite proxy'si üzerinden 200 Healthy, ana sayfa `lang="tr"` + başlık, DB durdurulunca 503 Unhealthy (A-7).

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 1 (clarify cevapları onayda işlendi) |
| Fix rounds | 3 (fix-1: review tur 1 · fix-2: VERIFY F-1/G-1 · fix-3: CI exec biti) |
| Review findings: real / noise / deferred | 9 / 1 / 4 (B-1, B-4 açık; B-2, B-3 bu PR'da CI ile kapandı) |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 (henüz dağıtım yok) |
| Plan amendments | 7 (A-1 … A-7) |
