# VERIFY — spec 0002

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1, A-2, A-3)
- HEAD: `771c1dc` on `feature/0002-identity-signup-login`
- Review turları: `0002-review-round-1.md`, `-round-2.md`, `-round-3.md`
- QA: bağımsız oturum (agent), 2026-11-09. Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| F-1 | Low | real | fix-3: kayıt sınırını üretim yapılandırmasıyla (10/saat) sabitleyen test |
| F-2 | Low | real | fix-3: doğrulanmamış ilk Admin adresi yeniden açılışta Admin olmaz — test |
| F-3 | Info | real | fix-3: rol yenileme testi aralığı sabit 1 dk ile yazar |
| F-4 | Low | real | fix-3: `LoginPage` 429 → "Çok fazla deneme…" mesajı + test |
| F-5 | Info | real | fix-3: test adı yaptığı şeyi söyler (klavye kanıtı manuel kalır, A-2 d) |
| F-6 | Info | defer | `specs/backlog.md` B-7 |
| F-7 | Info | real (yalnızca not) | SHIP: PR'da CI full koşusu son SHA ile doğrulanır |

fix-3, R-06 öncesi izin verilen son tur. Sonrası: dar REVIEW (round 4), etkilenen satırlar için yeniden VERIFY.

AC-10 ekran görüntüleri ve klavye gözlemi: — (insan, SHIP öncesi)

Triaged by: ferzurum · on: 2026-11-09

---

## Report

# VERIFY — spec 0002 (Kimlik: kayıt, e-posta doğrulama, giriş) · dal `feature/0002-identity-signup-login` · HEAD `771c1dc`

## Sonuç
Otomatik testle kanıtlanabilen her şey kanıtlandı. AC-1…AC-9 **Pass**. AC-10 **Needs human**: ekran görüntüleri ve klavye gözlemi yapılmadan kapanamaz.
- 30 mutasyon yaptım. 27'si en az bir testi kırmızıya düşürdü.
- 3 mutasyonda hiçbir test kırmızıya düşmedi. Bunlar aşağıda bulgu olarak listeleniyor (F-1, F-2, F-3).

## Check kanıtı
- `git diff --stat f5ba4ef..771c1dc` → yalnızca `specs/backlog.md` (+2) ve `specs/reviews/0002-review-round-3.md` (+101). Kod farkı yok.
- Developer'ın f5ba4ef için gösterdiği satırı ben göremedim, yalnızca brief'teki aktarımı gördüm. Bu yüzden full check'i HEAD üzerinde kendim koştum:
  - `./scripts/check` → **`CHECK GREEN (full, 11 steps, 142309 s)`**, EXIT 0.
  - Test toplamları: .NET fast 76/76, entegrasyon 46/46, web 39/39 (6 dosya). Developer'ın sayılarıyla aynı.
  - Süre anlamsız: makine koşu sırasında iki kez uykuya geçti. Mimari testler 9 sa 29 dk, web typecheck 108031 sn göründü. Sonuç geçerli, süre değil.
- Docs-only fark nedeniyle f5ba4ef'in yeşili de mantıken HEAD'i kapsar. Ama asıl kanıt yukarıdaki koşudur.
- CI: HEAD için bir CI koşusu görmedim. Review turları `gh` için 404 bildirmişti; ben ayrıca denemedim.

## Criterion ↔ evidence

Tüm testler HEAD üzerindeki full koşuda yeşil; tekrar komutu `./scripts/check`. Mutasyon kodları (M-…) bir sonraki tabloda.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 Geçerli kayıt, normalizasyon, bekleyen hesap, e-posta, hash | `EmailAddressTests.TryCreate_SurroundingSpacesAndCase_…_AC1`, `RegistrationTests(unit).Create_ValidInput_…_AC1`, `RegisterUserTests.HandleAsync_NewAddress_…_AC1`, `RegistrationTests.Register_AllowedDomain_CreatesPendingBidderAndSendsConfirmation_AC1`, `…Register_PasswordIsNotStoredInPlainText_AC1`. Bozuldu: M-AC1 | Pass |
| AC-2 İzinsiz / alt alan adı reddi | `EmailAddressTests.IsOnAllowedDomain_*_AC2`, `RegistrationTests.Create_DomainNotAllowed_…_AC2`, `RegistrationTests.Register_DomainNotAllowed_IsRejectedWithoutAccountOrEmail_AC2` (gmail + mail.polsanteknoloji…). Ekrandaki mesaj: `RegisterPage.test.tsx` "shows each field error…". Bozuldu: M-AC2 | Pass |
| AC-3 Kayıtlı e-posta: aynı sonuç, tek hesap, "zaten hesabınız var" | `RegistrationTests.Register_ExistingEmail_…_AC3`, `…Register_SameEmailInParallel_…_AC3`, `RegisterUserTests.*_AC3`. Bozuldu: M-AC3 | Pass |
| AC-4 Alan bazında Türkçe hata, hesap yok | `RegistrationTests(unit).Create_NameBoundaries_100AcceptedAnd101Rejected_AC4`, `…Create_PasswordBoundaries_10To128Accepted_AC4`, `…Create_EverythingWrong_…_AC4`, `EmailAddressTests.TryCreate_*_AC4`, `RegistrationTests.Register_InvalidInput_…_AC4`, `…Register_InvalidEmail_…_AC4`. Türkçe metin: `RegisterPage.test.tsx`. Bozuldu: M-AC4a, M-AC4b | Pass |
| AC-5 Doğrulama bağlantısı (24 sa, tek kullanım, bozuk, yeniden gönderim) | `ConfirmationTokenCodecTests.*_AC5`, `EmailConfirmationTests.*_AC5` (6 test). UI: `ConfirmEmailPage.test.tsx`. Bozuldu: M-AC5a, M-AC5b, W-3 | Pass |
| AC-6 Giriş, genel hata, doğrulanmamış hesap, hız sınırları | `LoginTests.*_AC6`, `LoginTests.Login_WithoutAntiforgeryToken_IsRejected`, `RateLimitTests.*`, `ResendThrottleTests.*`. UI: `LoginPage.test.tsx`, `RegisterPage.test.tsx` (429). Bozuldu: M-AC6a, M-AC6b, M-AC6c, W-4. **Kırmızıya düşmedi: M-AC6d → F-1** | Pass (F-1 açığıyla) |
| AC-7 5 hata → 15 dk kilit, açılış saati, sonra giriş | `LockoutTests.FourFailures_…_AC7`, `…FiveFailures_LockFor15Minutes_…_AC7`, `…ParallelFailures_StillLockTheAccount_AC7`. UI: `LoginPage.test.tsx` "shows until when … locked". Bozuldu: M-AC7a–d, W-2 | Pass |
| AC-8 Çıkış, 8 sa hareketsizlik, oturumsuz → giriş | `SessionTests.*_AC8` (5 test). UI: `AdminPage.test.tsx` "sends a signed-out visitor…", `App.test.tsx` "signs out…". Bozuldu: M-AC8a–c, W-5 | Pass |
| AC-9 Personel varsayılan, ilk Admin, sunucu tarafı kontrol | `RoleTests.*_AC9` (6 test). UI: `AdminPage.test.tsx`. Bozuldu: M-AC9a, M-AC9c, W-6. **Kırmızıya düşmedi: M-AC9b → F-2; M-AC9d → F-3** | Pass (F-2 açığıyla) |
| AC-10 Tüm UX durumları Türkçe, klavye, ekran okuyucu | Bileşen testleri yeşil (39). `getByLabelText`, `role="alert"`, `toHaveAccessibleDescription`. Bozuldu: W-1…W-6. **Ekran görüntüleri ve klavye gözlemi yok** | **Needs human** |

## Yaptığım mutasyonlar

Mutasyonları repo dışındaki geçici kopyada yaptım (`git archive 771c1dc`). Her biri ayrı ayrı uygulandı ve geri alındı; hiçbir şey commit'lenmedi.

| # | Dosya | Değişiklik | Kırmızıya düşen test | Kırmızı çıktı |
|---|---|---|---|---|
| M-AC1 | `EmailAddress.cs` | `Trim().ToLowerInvariant()` → `Trim()` | `TryCreate_SurroundingSpacesAndCase_…_AC1` (2 vaka) | `Expected: "ayse.yilmaz@polsanteknoloji.com.tr" Actual: "AYSE.YILMAZ@POLSANTEKNOLOJI.COM.TR"` |
| M-AC2 | `EmailAddress.cs` | `string.Equals` → `Domain.EndsWith(allowed)` | `IsOnAllowedDomain_OtherOrSubDomain_IsRejected_AC2` (mail.…, xpolsan…) | `Assert.False() Failure Expected: False Actual: True` |
| M-AC3 | `RegisterUser.cs` | Var olan adreste `email.taken` alan hatası döndür | `Register_ExistingEmail_GivesTheSameResult…_AC3` | `Expected: Accepted Actual: BadRequest` |
| M-AC4a | `PersonName.cs` | `> MaxLength` → `>= MaxLength` | `Create_NameBoundaries_100AcceptedAnd101Rejected_AC4` | `Assert.Empty() Failure: Collection was not empty` |
| M-AC4b | `PasswordPolicy.cs` | `> MaxLength` → `>=` (128 sınırı) | `Create_PasswordBoundaries_10To128Accepted_AC4` | `Assert.Empty() Failure` |
| M-AC5a | `UserAccounts.cs` | `user.EmailConfirmed` kontrolü silindi (bağlantı tekrar kullanılabilir) | `Confirm_LinkUsedBefore_IsRejected_AC5` | `Expected: BadRequest` |
| M-AC5b | `ConfirmationTokenProvider.cs` | Ömür +1 saat | `Confirm_LinkOlderThan24Hours_DoesNotActivate_AC5` | `Expected: BadRequest` |
| M-AC6a | `AccountSignIn.cs` | Doğrulama kontrolü parola kontrolünden önceye alındı | `Login_UnconfirmedAccountWithWrongPassword_RevealsNothing_AC6` | `Expected: Unauthorized` |
| M-AC6b | `AccountSignIn.cs` | Bilinmeyen e-posta → `EmailNotConfirmed` | `Login_WrongPasswordAndUnknownEmail_GetTheSameGenericError_AC6` | `Expected: Unauthorized` |
| M-AC6c | `ResendThrottle.cs` | `>= interval` → `> interval` (60 sn sınırı) | `Resend_TwiceWithinAMinute_IsLimited_ThenAllowedAgain_AC6` | `Expected: Accepted` |
| **M-AC6d** | `appsettings.json` | `RegisterAttemptsPerHour` 10 → 100 | **Hiçbiri (0/5 kırmızı)** | → F-1 |
| M-AC7a | `AccountSignIn.cs` | `maxFailures + 1` (6. hatada kilit) | `FiveFailures_LockFor15Minutes_…_AC7` | `Expected: Forbidden` |
| M-AC7b (eşzamanlılık) | `AccountSignIn.cs` | Atomik `ExecuteUpdateAsync` → Identity `AccessFailedAsync` | `ParallelFailures_StillLockTheAccount_AC7` | `Expected: Forbidden` |
| M-AC7c | `AccountSignIn.cs` | `ResetAccessFailedCountAsync` silindi | `FourFailures_DoNotLock_AndASuccessResetsTheCount_AC7` | `Expected: 0` |
| M-AC7d | `IdentityModule.cs` | Kilit 15 → 14 dk | `FiveFailures_LockFor15Minutes_…_AC7` | `Assert.InRange() Failure: Value not in range` |
| M-AC8a | `AccountSignIn.cs` | Çıkışta güvenlik damgası yenilenmiyor | `Logout_InvalidatesACopiedSessionCookie_AC8` | `Expected: Unauthorized` |
| M-AC8b | `IdentityModule.cs` | Hareketsizlik 8 → 9 sa | `Session_IdleForMoreThan8Hours_…_AC8`, `Session_WithActivity_…_AC8` | `Expected: Unauthorized` |
| M-AC8c | `IdentityModule.cs` | `SlidingExpiration = false` | `Session_WithActivity_StaysValidPast8Hours_…_AC8` | `Expected: OK` |
| M-AC9a (yetki) | `Program.cs` | Admin politikası → `RequireAuthenticatedUser()` | `NewAccount_IsABidder_AndCannotOpenTheAdminArea_AC9`, `RoleGrantedDuringASession_…_AC9` | `Expected: Forbidden` |
| **M-AC9b** | `InitialAdminSeeder.cs` | `!user.EmailConfirmed` koşulu silindi | **Hiçbiri (0/6 kırmızı)** | → F-2 |
| M-AC9c | `UserAccounts.cs` | Yeni hesaba `Admin` rolü | 4 RoleTests (`NewAccount_IsABidder…`, `InitialAdminAddress_NotYetConfirmed_IsNotAdmin_AC9`, …) | `Collections differ at index 0` / `Assert.False() Failure` |
| **M-AC9d** | `IdentityModule.cs` | Damga kontrol aralığı 1 → 10 dk | **Hiçbiri (0/6 kırmızı)** | → F-3 |
| W-1 | `TextField.tsx` | `aria-describedby` kaldırıldı | RegisterPage "…tied to it for screen readers" | `expect(element).toHaveAccessibleDescription() … Received:` (boş) |
| W-2 | `LoginPage.tsx` | Kilit mesajı yerine genel hata | LoginPage "shows until when the account is locked…" | `Expected element to have text content` |
| W-3 | `ConfirmEmailPage.tsx` | StrictMode ref koruması `if (true)` yapıldı | ConfirmEmailPage "sends a single-use link only once…" | `1 failed` |
| W-4 | `LoginPage.tsx` | `ResendConfirmation` kaldırıldı | LoginPage "offers to resend…" | `1 failed` |
| W-5 | `AdminPage.tsx` | Oturumsuz kullanıcı yönlendirmesi kaldırıldı | AdminPage "sends a signed-out visitor to the sign-in page" | `1 failed` |
| W-6 | `AdminPage.tsx` | Sunucu 403 dese de rol listesinde Admin varsa panel göster | AdminPage "follows the server, not the role list" | `1 failed` |

Not: ilk mutasyon koşum geçersizdi. Geri yükleme dosyanın eski mtime'ını getirdiği için MSBuild önceki mutasyonun DLL'ini güncel saydı ve bir mutasyonun izi sonrakine taşındı. Düzeltip kopyayı temiz derledim ve kaynakların HEAD ile birebir aynı olduğunu `diff -r` ile doğruladım. Tüm entegrasyon mutasyonlarını yeniden koştum; tablodaki sonuçlar o temiz koşudan.

## Bulgular (açıklar)
- **F-1 (Low, AC-6):** "Kayıt saatte en fazla 10" değeri hiçbir testle sabitlenmiyor.
  - `RateLimitTests.Register_EleventhAttemptWithinAnHour_IsLimited_AC6` sınırı kendisi `"10"` olarak veriyor.
  - Üretim değeri (`appsettings.json` + `IdentitySettings` varsayılanı) 100 yapılınca test yeşil kalıyor.
  - Ölçülen şey mekanizma, spec'teki sayı değil. Yeniden gönderim aralığı (1 dk) ise üretim yapılandırmasıyla test ediliyor; o sorunsuz.
- **F-2 (Low, AC-9, hassas alan):** `InitialAdminAddress_NotYetConfirmed_IsNotAdmin_AC9` kırmızıya düşemez.
  - Seeder yalnızca açılışta (hesap henüz yokken) ve doğrulama anında çalışıyor; test hesabı açılıştan sonra kaydediyor.
  - `!user.EmailConfirmed` koşulu silinince test yeşil kalıyor (M-AC9b).
  - Kapsanmayan senaryo: ilk Admin adresi kayıtlı ama doğrulanmamış, uygulama yeniden başlıyor. Bu senaryoda koşul silinirse adresin sahibi doğrulamadan Admin olur.
  - Önerim: yeniden açılışta doğrulanmamış hesabın Admin olmadığını assert eden bir test.
- **F-3 (Info, test tasarımı):** `RoleGrantedDuringASession_…_AC9` saati üretim sabiti `IdentityModule.SecurityStampValidationInterval` kadar ilerletiyor. Sabit 10 dk yapılınca test yeşil kalıyor (M-AC9d).
  - Plan haritası ve `docs/security.md` "1 dakikada yansır" diyor ama bu test bunu sabitlemiyor.
  - Çıkış tarafında `Logout_InvalidatesACopiedSessionCookie_AC8` 1 dk'yı sabit yazıyor; orası korunuyor.
- **F-4 (Low, UX):** Girişte 429 (kaynak başına giriş sınırı) `LoginPage`'de "Bir hata oluştu…" gösteriyor, "Çok fazla deneme…" değil. `switch`'te `rate_limited` dalı yok.
  - AC-6 bu mesajı yalnızca kayıt ve yeniden gönderim için istiyor. Bu yüzden kriter ihlali değil, tutarsız UX.
- **F-5 (Info, AC-10):** `RegisterPage` "can be completed with the keyboard" testi `fireEvent.submit` kullanıyor. Enter tuşu, Tab sırası ya da odak kanıtlanmıyor; test adı olduğundan fazlasını vaat ediyor.
  - A-2(d) gereği klavye kanıtı zaten manuel; bu test o gözlemin yerini tutmaz.
- **F-6 (Info):** R-9'un log kısmı (parola, bağlantı, isim ya da e-posta logda görünmez) ve R-10 hiçbir AC'ye bağlı değil, dolayısıyla AC kanıtı yok.
  - R-10'u Development dışında başlatmayı reddetme yönünden `NotificationModuleTests` kısmen kapsıyor.
  - Log içeriğini assert eden test yok; yalnızca kod okumasıyla (`IdentityLog.cs`) doğruladım.
- **F-7 (Info, süreç):** HEAD için CI koşusu görülmedi (review turlarında `gh` 404). PR'da CI full koşusu `771c1dc` SHA'sıyla doğrulanmalı.

## Gerçek kanıtı olmayan kriterler
- **AC-10:** ekran görüntüleri ve manuel klavye / ekran okuyucu gözlemi yok. Bileşen testleri durumları ve aria bağlarını kanıtlıyor, ama spec ve test kuralı (Evidence for UI criteria) ekran görüntüsü istiyor.
- AC-6 içindeki "10/saat" sayısı (F-1) ve AC-9 içindeki "doğrulanmamış ilk Admin, açılışta Admin olmaz" (F-2): mekanizma testli, ama spec'teki sayıyı ve senaryoyu sabitleyen kanıt yok.

## Davranış yerine uygulama ayrıntısını assert eden testler
- `RegisterUserTests.HandleAsync_ExistingAddress_StillSpendsAPasswordHash_SoTimingRevealsNothing` ve `…HandleAsync_NewAddress_HashesOnlyOnce_…`: zamanlamayı ölçmüyor, sahte port'ta `HashPasswordForTimingOnly` çağrısını sayıyor.
- `LockoutTests.FourFailures_DoNotLock_AndASuccessResetsTheCount_AC7`: sıfırlamayı DB kolonuyla (`AccessFailedCount == 0`) assert ediyor, davranışla değil.
  - Davranışsal hali: başarıdan sonra 4 hata daha kilitlemez.
  - Mutasyon yine de kırmızıya düştü, yani kanıt değeri var.
- `RoleTests.RoleGrantedDuringASession_…`: üretim sabitine bağlı (F-3).
- `Register_PasswordIsNotStoredInPlainText_AC1` hash kolonunu okuyor. R-9 için kabul edilebilir.

## Backlog kontrolü
- R1 F-7 → `specs/backlog.md` **B-5** mevcut. Source, Severity, Recommendation ve Deferred by (ferzurum, 2026-11-06) tam.
- R3 F-1 → **B-6** mevcut, alanlar tam (ferzurum, 2026-11-07).
- R2 F-3 (yerel DB sıfırlama notu): yalnızca not olarak işaretlenmiş. Sıfırlama satırı PR açıklamasına girmeli: `docker compose down -v` ardından `docker compose up -d --wait`.

## İnsanın yapması gereken (AC-10, PR'a eklenecek)
Hazırlık:
- DB zaten sıfırlandı.
- README adım 2–4: API `dotnet run --project src/Api/PolsanAuction.Api`, web `npm --prefix web run dev`.
- Doğrulama bağlantısı API konsolunda çıkar.

Ekran görüntüleri:
1. Kayıt: boş form; alan hataları (her alanın altında Türkçe, ör. izinsiz alan "Yalnızca kurumsal e-posta adresleriyle kayıt olunabilir", kısa parola, eşleşmeyen tekrar); "Gönderiliyor…"; başarılı "E-postanızı kontrol edin"; sunucu hatası (ör. API durdurulmuşken); 11. denemede "Çok fazla deneme…" (opsiyonel).
2. Doğrulama: "doğrulanıyor…"; başarılı "E-postanız doğrulandı, giriş yapabilirsiniz"; geçersiz / kullanılmış bağlantı ve yeniden gönderme formu (aynı bağlantıyı ikinci kez açmak yeterli); yeniden gönderildi mesajı.
3. Giriş: boş form; "Gönderiliyor…"; hatalı bilgi (genel mesaj); doğrulanmamış hesap ve yeniden gönder seçeneği; hesap kilitli ve açılış saati (5 yanlış parola); sunucu hatası.
4. Üst çubuk: Personel (ad soyad, "(Personel)", Çıkış, Yönetim linki yok); Admin (`fatih.erzurum@…` doğrulandıktan sonra "(Admin)" ve "Yönetim" linki).
5. Yönetim: Admin için "Yönetim paneli"; Personel için "Bu sayfaya erişim yetkiniz yok"; oturumsuzken `/yonetim` → giriş ekranı.

Manuel klavye gözlemi (A-2 d):
- Kayıt, giriş ve yeniden gönderim formlarını yalnızca Tab / Shift+Tab / Enter ile tamamlayın; odak görünür olmalı.
- Hata sonrası ekran okuyucunun (NVDA / Narrator) alan hatasını ve `role="alert"` mesajlarını duyurduğunu not edin.
- Parola alanlarında tarayıcının otomatik doldurma önerdiğini gözleyin.
- Sonucu PR açıklamasına yazın.

## Temizlik
- Mutasyon kopyasını silme (`rm -rf …/scratchpad/qa-copy` ve `…/scratchpad/head-ref`) izin sisteminde **reddedildi**; etrafından dolanmadım.
  - Kopya repo dışında, oturumun scratchpad'inde duruyor: `C:\Users\FATIH~1.ERZ\AppData\Local\Temp\claude\C--Polsan-Edu-polsanauction\918d067d-bf89-4b9f-96eb-60f883153e29\scratchpad\qa-copy` (ve `...\scratchpad\head-ref`).
  - Lütfen elle silin. İçindeki kaynaklar HEAD ile aynı; bütün mutasyonlar geri alındı.
- Testcontainers konteyneri kalmadı. Çalışan tek konteyner `polsanauction-postgres-1`, yani yerel dev DB.

## Son `git status --short` (repo: `C:/Polsan Edu/polsanauction`, HEAD `771c1dc`)
```
--- git status --short:
--- (end; nothing between the markers = clean)
```
Çıktı boş: repo dokunulmamış.
