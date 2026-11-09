# Review — spec 0002, round 4

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1, A-2, A-3)
- Diff: fix-3, `285bdf4..81a146d` on `feature/0002-identity-signup-login` (6 dosya)
- Kaynak: `specs/reviews/0002-verify.md` (VERIFY F-1…F-7 + triage); önceki turlar round 1–3
- Reviewer: bağımsız read-only subagent, 2026-11-09
- Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| — | — | — | Bulgu yok (clean) |

Next: VERIFY (dar: AC-6, AC-9, AC-10 giriş sayfası satırı; F-1…F-5 kapanışı), sonra SHIP.

Triaged by: ferzurum · on: 2026-11-09

---

## Report

Review round 4 · fix rounds so far: 3

Kapsam: `git diff 285bdf4..81a146d`. Dalda 6 commit var, hepsi `[plan 0002/fix-3]` etiketli. HEAD `81a146d`, origin ile aynı. Diff 6 dosyaya dokunuyor: +53 / −9.
Referanslar: `specs/active/0002-identity-signup-login.md`, `specs/plans/0002-plan.md` (A-1…A-3) ve `specs/reviews/0002-verify.md` (F-1…F-7 ve triage).

**Karar: clean.** Medium ya da üstü bulgu yok, Low bulgu da yok. Gerçek bulgu çıkmadığı için R-06 (dördüncü fix turu) tetiklenmiyor.

## Kanıt
- **CHECK GREEN** (full, 11 adım, 162 sn): .NET fast 76, integration 47, web 40. Bu sonuç geliştiricinin `81a146d` için verdiği kanıttır. prompts/review.md kuralı gereği burada aktarıldı, yeniden koşulmadı.
- **Mutasyonlar:** Geliştirici üç mutasyonun artık kırmızıya düştüğünü bildiriyor:
  - M-AC6d: "Expected: 10 Actual: 100"
  - M-AC9b: yeni restart testi kırmızı
  - M-AC9d: "Expected: OK Actual: Forbidden"
  Bunları yeniden koşmadım. Kod okumam her üçünün kırmızıya düşmesi gerektiğiyle tutarlı (aşağıda).
- **CI:** HEAD için CI full koşusunu göremedim (önceki turlarda `gh` 404 veriyordu). Triage, PR'da SHA `81a146d` ile CI full koşusunu görmeyi beklemeli (VERIFY F-7).

## Boyutlar

**Correctness: clean.**
- F-1: `tests/PolsanAuction.Api.IntegrationTests/RateLimitTests.cs:58-65` değeri test çıktı dizinindeki `appsettings.json`'dan okuyor ve `Assert.Equal(10, shipped)` ile sabitliyor.
  - Okunan dosya Api projesinin kopyası. `tests/PolsanAuction.Api.IntegrationTests/bin/Release/net10.0/appsettings.json:15` = 10.
  - Test projesinin çakışan kendi `appsettings.json`'ı yok.
  - Anahtar silinirse `GetValue<int>` 0 döner ve test yine kırmızıya düşer.
  - Okunan değer `AuthApp`'teki 1000 varsayılanını ezerek uygulamaya veriliyor. Böylece hem sayı hem mekanizma test ediliyor.
- F-2: `RoleTests.cs:80-102`.
  - Adres, ilk Admin ayarı boşken (`AuthApp.cs:108`) kaydediliyor.
  - Sonra aynı veritabanında `AdminIs(adminEmail)` ile ikinci bir uygulama başlıyor. `CreateApiClientAsync` host'u başlattığı için açılış kontrolü (`IdentityModule.cs:138`) mevcut, doğrulanmamış hesabı buluyor. VERIFY'da kapsanmadığı söylenen senaryo tam olarak bu.
  - Bir not: açılış kontrolü bir istisnada sessizce atlanıyor (`IdentityModule.cs:142`, `InitialAdminCheckSkipped`). Bu yüzden testi tek başına "seeder koştu" kanıtı saymak eksik olur. Geliştiricinin bildirdiği M-AC9b kırmızısı, bu yolun gerçekten çalıştığını gösteriyor. Bulgu değil.
- F-3: `RoleTests.cs:139-140` artık üretim sabitini kullanmıyor, `docs/security.md:44` içindeki "en geç 1 dakika" sınırını (1 dk + 1 sn) kullanıyor. Sabit 10 dk yapılırsa test kırmızıya düşer.
- F-4: `web/src/pages/LoginPage.tsx:10,42,72` 429 ya da `rate_limited` yanıtında `messages.tooManyAttempts` gösteriyor.
  - Bunu `role="alert"` ile, öteki durumlarla aynı desende yapıyor.
  - `isRateLimited` kayıt sayfası ve yeniden gönderimle ortak (`messages.ts:56`).
  - Sunucunun gönderdiği kod (`RateLimits.cs:17`, `"rate_limited"`) ile uyumlu.
  - Kilit (403/locked) dalından önce gelmiyor, yani o akışı bozmuyor.
- F-5: `RegisterPage.test.tsx:100-101`'de yalnızca testin adı değişti ve bir yorum eklendi. Assert'lere dokunulmamış.

**Plan conformance: clean.**
- Altı dosyanın hepsi plan listesinde ya da A-2'de var. Diff'te plan dışı dosya yok.
- `specs/plans/0002-plan.md:72,75,76` kriter ↔ test haritasını F-1, F-2 ve F-4 kanıtlarıyla güncelliyor.

**Security: clean.**
- Diff üretimde yalnızca `LoginPage.tsx`'e dokunuyor ve yalnızca gösterilen mesajı değiştiriyor.
- Yeni metin bilgi sızdırmıyor: hesabın varlığını ya da durumunu açığa çıkarmıyor.
- Hassas alan (AC-9) için koruyucu test eklendi.

**Edge cases: clean.**
- Kayıt sınırı 10. denemede geçiyor, 11. denemede 429 veriyor. Bu sınır değeri artık teslim edilen yapılandırmayla test ediliyor.

**Performance: clean.** Yalnızca test ve UI dalı değişti.

**Tests: clean.**
- Hiçbir assert zayıflatılmadı ya da silinmedi; atlanan test yok. Kural 5'e uygun.
- Yeni testler davranışı assert ediyor: veritabanındaki rol üyeliği, ekranda görünen alert metni, 11. denemenin sonucu.
- Restart testindeki `WithDbAsync(async … Task.FromResult(...))` deseni mevcut `InitialAdminAddress_NotYetConfirmed_IsNotAdmin_AC9` testinden kopyalanmış. Biçim olarak gereksiz ama tutarlı, bulgu yazmıyorum.

**Maintainability: clean.** Yorumlar her değişikliği VERIFY bulgusuna bağlıyor ve adlandırmalar testlerin kanıtladığı şeyle örtüşüyor.

## Açık kalanlar (bu turun kapsamı dışında, bilgi için)
- **AC-10:** ekran görüntüleri ve manuel klavye / ekran okuyucu gözlemi hâlâ insanda. VERIFY'da "Needs human" olarak kaldı.
- **F-6:** `specs/backlog.md` B-7'ye ertelendi.
- **F-7:** CI SHA doğrulaması SHIP aşamasında yapılacak.

Next: TRIAGE — human (real / noise / defer / investigate); bu turun bulgusu olmadığı için VERIFY'a (ya da VERIFY F-1…F-5 kapanışının onayına) geçilir, ardından SHIP; CI full koşusu `81a146d` için görülmeli.
