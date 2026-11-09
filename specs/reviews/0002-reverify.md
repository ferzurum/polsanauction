# VERIFY (dar, fix-3 sonrası) — spec 0002

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1, A-2, A-3)
- HEAD: `9cf5cef` on `feature/0002-identity-signup-login`
- Önceki: `0002-verify.md` (F-1…F-7 + triage), `0002-review-round-4.md` (clean)
- QA: bağımsız oturum (agent), 2026-11-09. Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| G-1 | Info | defer | `specs/backlog.md` B-8; fix-3 R-06 öncesi son turdu, risk düşük |

VERIFY F-1…F-5: kapandı (yukarıdaki kanıtla). Next: SHIP.

AC-10 ekran görüntüleri ve klavye gözlemi: — (insan, PR'a eklenecek)

Triaged by: ferzurum · on: 2026-11-09

---

## Report

# VERIFY (dar, fix-3 sonrası) — spec 0002 · dal `feature/0002-identity-signup-login` · HEAD `9cf5cef` (= origin)

## Sonuç
Kapsamdaki satırlarda AC-6 ve AC-9 **Pass**. AC-10 **Needs human** (ekran görüntüleri ve klavye / ekran okuyucu gözlemi).
- F-1…F-5'in beşi de kanıtla **kapandı**.
- 9 mutasyon yaptım (4'ü istenen, 5'i ek). 8'i kırmızıya düştü.
- 1 mutasyon (M-AC6f) hiçbir testi kırmızıya düşürmedi. Bu açık fix-3'ten önce de vardı; aşağıda G-1 olarak yazdım (Info).

## Check kanıtı
- `./scripts/check`'i repo kökünde HEAD `9cf5cef` üzerinde kendim koştum. Çıktıyı scratchpad'deki log dosyasına aldım, exit kodunu `rc=$?` ile kaydettim:
  - **`CHECK GREEN (full, 11 steps, 101 s)`**, **`rc=0`**
- Test toplamları:

  | Grup | Toplam | Başarısız |
  |---|---|---|
  | .NET fast (unit+architecture) | 76 | 0 |
  | Entegrasyon | 47 | 0 |
  | Web (6 dosya) | 40 | 0 |

  Geliştiricinin `81a146d` için bildirdiği 76 / 47 / 40 ile aynı. `81a146d..9cf5cef` yalnızca review dosyasını ekliyor.
- Log: `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\check-9cf5cef.log`
- CI: HEAD için CI koşusu görmedim (F-7, SHIP'te).

## Criterion ↔ evidence (yalnızca kapsamdaki satırlar)
Tekrar komutu: `./scripts/check` (yukarıdaki full koşuda hepsi yeşil). Kapsamdaki sınıfları kopyada ayrıca koştum: `RoleTests` + `RateLimitTests` 12/12, `LoginPage` + `RegisterPage` 15/15.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-6 Giriş, genel hata, doğrulanmamış hesap, hız sınırları (kayıt 10/saat, "Çok fazla deneme…") | `RateLimitTests.Register_EleventhAttemptWithinAnHour_IsLimited_AC6`: teslim edilen `appsettings.json` değerini okuyor ve `Assert.Equal(10, shipped)` ile sabitliyor; 10 deneme Accepted, 11.'si 429 + `rate_limited`. UI: `LoginPage.test.tsx` "shows the too-many-attempts message when sign-ins from this source are limited". Önceki VERIFY'daki öteki AC-6 testleri de full koşuda yeşil. Bozduğum mutasyonlar: M-AC6d, M-AC6e, W-7, W-8. **Kırmızıya düşmeyen: M-AC6f (1 saatlik pencere) → G-1** | Pass |
| AC-9 Personel varsayılan; ilk Admin doğrulamada ya da açılışta; sunucu tarafı kontrol | Yeni test `RoleTests.InitialAdminAddress_RegisteredButNotConfirmed_IsNotAdminAfterARestart_AC9`. `RoleTests.RoleGrantedDuringASession_…_AC9` artık sabit 1 dk + 1 sn ilerletiyor. `…AlreadyConfirmedBeforeTheSetting_BecomesAdminAtStartup_AC9` de yeşil. Bozduğum mutasyonlar: M-AC9b, M-AC9d, M-AC9e, M-AC9f | Pass |
| AC-10 Tüm UX durumları Türkçe; klavye; ekran okuyucu | Web testleri 40/40. Yeni giriş "limited" durumu `role="alert"` ile Türkçe metni gösteriyor (W-7 ve W-8 kırmızı). RegisterPage testinin adı artık gerçekten yaptığı şeyi söylüyor (F-5). **Ekran görüntüleri ve manuel klavye / ekran okuyucu gözlemi yok** | **Needs human** |

## Mutasyon tablosu
Her mutasyonu repo dışında yaptım: `git archive 9cf5cef` ile açılmış `qa-copy-2` kopyasında. Her biri tek başına uygulandı, ilgili testler koşuldu, sonra geri alındı.
- Geri alırken dosyayı HEAD referans kopyasından (`head-ref-2`) aldım ve `touch` ile mtime'ı tazeledim (eski DLL sorunu tekrarlanmasın diye). Her geri alma `diff -q` ile doğrulandı.
- Sonunda `diff -r` (bin/obj/node_modules hariç) yalnızca test çıktısı olan `TestResults` klasörünü farklı gösteriyor; kaynak farkı yok. Temel koşu tekrar yeşil (12/12 ve 15/15).

| # | Dosya | Değişiklik | Kırmızıya düşen test | Kırmızı çıktı |
|---|---|---|---|---|
| M-AC6d | `src/Api/PolsanAuction.Api/appsettings.json` | `RegisterAttemptsPerHour` 10 → 100 | `RateLimitTests.Register_EleventhAttemptWithinAnHour_IsLimited_AC6` (1/5 kırmızı) | `Assert.Equal() Failure: Values differ Expected: 10 Actual: 100` |
| M-AC6e (ek) | `Auth/RateLimits.cs` | Kayıt `PermitLimit` + 1 (11. deneme geçer) | Aynı test (1/5) | `Expected: TooManyRequests Actual: Accepted` |
| **M-AC6f (ek)** | `Auth/RateLimits.cs` | Kayıt penceresi `FromHours(1)` → `FromMinutes(1)` | **Hiçbiri (0/5 kırmızı, 5/5 başarılı)** | → G-1 |
| M-AC9b | `InitialAdminSeeder.cs` | `\|\| !user.EmailConfirmed` satırı silindi | `RoleTests.InitialAdminAddress_RegisteredButNotConfirmed_IsNotAdminAfterARestart_AC9` (1/7) | `Assert.False() Failure Expected: False Actual: True` |
| M-AC9d | `IdentityModule.cs` | `SecurityStampValidationInterval` 1 → 10 dk | `RoleTests.RoleGrantedDuringASession_ReachesItWithinTheCheckInterval_WithoutSigningInAgain_AC9` (1/7) | `Expected: OK Actual: Forbidden` |
| M-AC9e (ek) | `IdentityModule.cs:138` | Açılıştaki ilk Admin kontrolü (`InitialAdminSeeder.EnsureAsync`) devre dışı | `RoleTests.InitialAdminAddress_AlreadyConfirmedBeforeTheSetting_BecomesAdminAtStartup_AC9` (1/7) | `Assert.Contains() Failure: Item not found in collection` |
| M-AC9f (ek, sınır) | `IdentityModule.cs` | Aralık 1 dk → 62 sn | `RoleGrantedDuringASession_…_AC9` | `Expected: OK Actual: Forbidden` |
| W-7 | `web/src/pages/LoginPage.tsx` | `isRateLimited(...) ? 'limited' : 'error'` → her zaman `'error'` (genel hata) | LoginPage "shows the too-many-attempts message when sign-ins from this source are limited" (1/8) | `Expected element to have text content: Çok fazla deneme…` · `Tests 1 failed \| 7 passed (8)` |
| W-8 (ek) | `web/src/pages/LoginPage.tsx` | "limited" mesajından `role="alert"` kaldırıldı (ekran okuyucu duyurmaz) | Aynı test (1/8) | `TestingLibraryElementError: Unable to find role="alert"` |

Ek mutasyonların gerekçesi:
- **M-AC9e:** yeni restart testi yalnızca "Admin değil" diyor. Açılış yolu hiç çalışmasa da bu test yeşil kalırdı. M-AC9b'nin kırmızısı açılış yolunun gerçekten çalıştığını gösteriyor. M-AC9e de karşı yönü, yani açılışta doğrulanmış adresin Admin olmasını koruyan testin canlı olduğunu kanıtlıyor.
- **M-AC9f:** testin 1 dakikalık sınırı sıkı tuttuğunu gösteriyor.
- **M-AC6e ve M-AC6f:** F-1 sayıyı sabitliyor; mekanizmanın sayıyı ve pencereyi uygulayıp uygulamadığını ayrıca denedim.

## F-1…F-5 kapanış durumu
- **F-1 Kapandı.** Test teslim edilen `appsettings.json` değerini sabitliyor (M-AC6d kırmızı). Değer limitleyiciye gerçekten uygulanıyor (M-AC6e kırmızı).
- **F-2 Kapandı.** Yeni restart testi var. M-AC9b, 0/6'dan 1/7 kırmızıya geçti.
- **F-3 Kapandı.** Test üretim sabitine değil, belgelenen 1 dk sınırına bağlı. M-AC9d (10 dk) ve M-AC9f (62 sn) kırmızı.
- **F-4 Kapandı.** `LoginPage` 429 ya da `rate_limited` yanıtında `role="alert"` ile "Çok fazla deneme, lütfen sonra tekrar deneyin." gösteriyor. W-7 ve W-8 kırmızı. Kilit (`auth.locked_out`) dalı `switch`'te önce geliyor, akışı bozulmadı.
- **F-5 Kapandı.** `RegisterPage.test.tsx`'te yalnızca test adı değişti ve A-2(d)'ye işaret eden bir yorum eklendi. Assert'lere dokunulmadı. Klavye kanıtı manuel kalıyor (AC-10).

## Yeni bulgular
- **G-1 (Info, AC-6, fix-3 öncesinden kalan):** Kayıt sınırındaki "saatte" kısmı hiçbir testle sabitlenmiyor.
  - Pencere 1 saatten 1 dakikaya indirilince (M-AC6f) 5 `RateLimitTests` testinin hepsi yeşil kalıyor.
  - Test 11 denemeyi hemen arka arkaya yaptığı için yalnızca sayıyı ölçüyor. `RateLimits.cs`'teki `FixedWindowRateLimiter` `TimeProvider` değil duvar saati kullanıyor, bu yüzden test saati ilerletemiyor.
  - Önerim: **defer** (`specs/backlog.md`). Çözüm, pencereyi adlı bir sabite ya da ayara taşıyıp bir birim testiyle 1 saate sabitlemek, ya da limitleyiciyi `TimeProvider` ile kurup entegrasyon testinde 59 dk / 61 dk sınırını denemek.
  - Gerekçe: Değer koddaki tek bir literal ve review'dan geçti; risk düşük. R-06 sınırındayız, bu yüzden yeni bir fix turu açmak orantısız.

## İnsanın yapması gereken
1. **AC-10 (SHIP öncesi, PR'a):** önceki VERIFY'daki ekran görüntüsü ve manuel klavye / ekran okuyucu listesi (`specs/reviews/0002-verify.md`, "İnsanın yapması gereken") olduğu gibi geçerli. Ek olarak:
   - Giriş ekranında kaynak başına sınır aşıldığında (dakikada 30'dan fazla deneme) "Çok fazla deneme, lütfen sonra tekrar deneyin." mesajının ekran görüntüsü.
   - Bu mesajın ekran okuyucuda duyurulduğunun gözlemi.
2. **CI (F-7):** PR'da CI full koşusunun son SHA ile (şu an `9cf5cef`) yeşil olduğunu doğrulayın.
3. **G-1 triage.**

## Temizlik
Scratch kopyalarını silmeyi denemedim; elle silinmeli. İçlerindeki kaynaklar HEAD ile aynı, bütün mutasyonlar geri alındı.
- `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\qa-copy-2`
- `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\head-ref-2`
- `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\lp.bak`
- `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\mut.sh`
- `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\check-9cf5cef.log`
- Önceki turdan kalanlar: `...\scratchpad\qa-copy` ve `...\scratchpad\head-ref`

Konteynerler: kalıcı olan yalnızca `polsanauction-postgres-1` (yerel dev DB). Testcontainers'ın `testcontainers-ryuk-…` konteyneri son kontrolde hâlâ çalışıyordu; Ryuk bunu kendiliğinden kaldırır.

## Son `git status --short` (repo `C:/Polsan Edu/polsanauction`, HEAD `9cf5cef`)
```
--- git status --short:
--- (end)
```
Çıktı boş: repoya hiçbir şekilde dokunulmadı.

Next: insan G-1'i triage eder (önerim: defer → backlog), AC-10 ekran görüntülerini ve klavye / ekran okuyucu gözlemini PR'a ekler, PR'da son SHA'nın CI full koşusunu doğrular, sonra SHIP.
