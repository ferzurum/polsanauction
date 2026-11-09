# Review — spec 0002, round 2

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1, A-2, A-3)
- Diff: fix-1, `f14095a..bfbdb84` on `feature/0002-identity-signup-login` (40 dosya)
- Round 1: `specs/reviews/0002-review-round-1.md`
- Reviewer: bağımsız read-only subagent, 2026-11-06
- Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| F-1 | Low | real | fix-2: damga tek bir atomik `ExecuteUpdateAsync` ile yenilenir; Known traps satırı `UpdateSecurityStampAsync`'i de anar |
| F-2 | Info | real | fix-2: rolün oturuma yansıması entegrasyon testiyle kanıtlanır, `docs/security.md` düzeltilir; çıkışın hesabın tüm oturumlarını bitirmesi bilinçli olarak kabul edildi |
| F-3 | Info | real | yalnızca not: yerel DB sıfırlama satırı VERIFY handoff'una ve PR açıklamasına |

Fix-2 sonrası yeniden review (hassas alan, `workflows/feature-development.md` adım 8).

Triaged by: ferzurum · on: 2026-11-06

---

## Report

Review round 2 · fix rounds so far: 1

**Kapsam:** fix-1 diff'i `git -c safe.directory='*' diff f14095a..HEAD` (49ff3d1..bfbdb84, 40 dosya, hepsi `[plan 0002/fix-1]`). Spec 0002, plan 0002 ve onaylı A-1, A-2, A-3 esas alındı. Round-1 raporu ve triage `specs/reviews/0002-review-round-1.md` dosyasından okundu. Dal origin ile senkron: `origin/feature/0002-identity-signup-login` = `bfbdb84`.

**CHECK:** Developer'ın HEAD `bfbdb84` için gösterdiği full GREEN'i alıntılıyorum (11 adım, 176 sn; 76 .NET fast + 45 entegrasyon + 39 web). `docs/testing.md` Two speeds kural 6 gereği yeniden koşmadım.

**CI:** Doğrulayamadım. `gh run list -R ferzurum/polsanauction --branch feature/0002-identity-signup-login` yine HTTP 404 döndü. Triage, HEAD için CI full koşusunu görmeden kapanmamalı.

### Round-1 bulgularının durumu

| R1 | Triage | Durum | Kanıt |
|---|---|---|---|
| F-1 | real | Düzeltildi | `AccountSignIn.cs:84-105` sayacı tek bir atomik `ExecuteUpdateAsync` ile artırıyor. `RateLimits.cs:31-38` + `AuthEndpoints.cs:60` girişe kaynak başına sınır koyuyor. `LockoutTests.ParallelFailures_StillLockTheAccount_AC7` var: eski kodda 20 isteğin hepsi aynı damgayı okuyup tek artış yapacağı için bu test kırmızı olurdu. |
| F-2 | real | Düzeltildi | `NotificationModule.cs:15-24` Development dışında açılışı durduruyor. `NotificationModuleTests` Production ve Staging için sınıyor. |
| F-3 | real | Düzeltildi (bkz. yeni F-1, F-2) | `IdentityModule.cs:84` `OnValidatePrincipal` bağlı. `:90-97` TwoFactorRememberMe şeması kayıtlı. `:100-104` aralık 1 dk ve `TimeProvider` ayarlı. Çıkışta damga yenileniyor (`AccountSignIn.cs:55-63`). `SessionTests.Logout_InvalidatesACopiedSessionCookie_AC8` bunu sınıyor. `Session_WithActivity_StaysValidPast8Hours…` da geçerli oturumun doğrulayıcıdan geçtiğini kanıtlıyor; yani gerileme yok. |
| F-4 | real | Düzeltildi | `web/src/api/client.ts:30-48`: 401'de token unutuluyor; `csrf.invalid`'de yeni token alınıp istek bir kez yeniden deneniyor. `client.test.ts` üç testle sınıyor. |
| F-5 | real | Düzeltildi | `ConfirmEmailPage.tsx:16-31` istek `useRef` ile korunuyor. `renderAt(…, { strict: true })` testi tek istek gittiğini assert ediyor. |
| F-6 | real | Düzeltildi | `UserAccounts.cs:35-46,69-80` 23505'i istisna zincirinde yakalıyor. `RegisterUser.cs:34-40` "var olan adres" yoluna düşüyor. `Register_SameEmailInParallel_…_AC3` 8 paralel istekle 8×202 ve tek hesap olduğunu sınıyor. |
| F-7 | defer | Doğru ertelendi | `specs/backlog.md` B-5 kaydında Source, Severity, Recommendation ve Deferred by var. |
| F-8 | real | Düzeltildi | Girişte bilinmeyen e-postada sahte hash doğrulanıyor (`AccountSignIn.cs:24-28`). Kayıtta var olan adres için hash harcanıyor (`RegisterUser.cs:27-30`). Birim testleri var. |
| F-9 | real | Düzeltildi | `ResendThrottle.cs:11-54` 256 çağrıda bir süresi geçmiş kayıtları siliyor; değer eşleşmesine bağlı `Remove` kullanıyor. `/resend-confirmation` için kaynak başına sınır var. Testler: `ResendThrottleTests`, `RateLimitTests.Resend_FromOneSource…`. |
| F-10 | real | Düzeltildi | `Header.tsx:4-5,33` rol etiketini gösteriyor. `App.test.tsx` "(Personel)" ve "(Admin)" için sınıyor. |
| F-11 | real | Düzeltildi (bkz. yeni F-3) | `IdentityDbContext.cs:20-39`. Migration snake_case adlarla yeniden üretildi. Benzersiz indeksler korunuyor (`20261106113830_InitialIdentity.cs:204-240`, `unique: true`). |
| F-12 | real | Düzeltildi | A-2 (a)–(e) fix-1 dosyalarını da kapsıyor. |
| F-13 | real | Düzeltildi | Harita gerçek test adlarıyla güncellendi. E-posta testi hata kodunu assert ediyor (`RegistrationTests.cs:128-140`). Klavye kanıtı A-2(d) gereği manuel gözlemle sağlanacak. |
| F-14 | real | Düzeltildi | Mesaj eksiz kuruluyor (`messages.ts:53`). Kabul edilen riskler `docs/security.md` ve ADR 0001'e yazıldı. |
| A-3 | — | Yapıldı | `docs/conventions.md:63-67`'de beş Known traps satırı var. |

### Bulgular (önem sırasına göre)

#### F-1 — Low — Security / Edge cases — Çıkıştaki `UpdateSecurityStampAsync` sonucu kontrol edilmiyor
- **Evidence:**
  - `src/Modules/Identity/PolsanAuction.Identity.Infrastructure/AccountSignIn.cs:57-60`: `await users.UpdateSecurityStampAsync(user);` dönen `IdentityResult` yok sayılıyor.
  - Bu çağrı `UpdateAsync` üzerinden `concurrency_stamp` ile iyimser eşzamanlılık kullanıyor. Çakışmada istisna atmıyor, `ConcurrencyFailure` döndürüyor. Bu tuzak branch'in kendi Known traps satırında yazılı (`docs/conventions.md:63`).
  - Fix-1'in kendisi de `concurrency_stamp`'i değiştiren bir yol ekledi: `AccountSignIn.cs:97`, her hatalı girişte damgayı yeniliyor.
  - Dolayısıyla `GetUserAsync` (satır 57) ile güncelleme arasında aynı hesaba gelen bir hatalı giriş, güvenlik damgası yenilemesini sessizce düşürüyor. Bu durumda çıkış yine 204 dönüyor ama kopyalanmış çerez geçerli kalıyor; R1 F-3'ün amacı tam da buydu.
- **Impact:** Pencere dar: milisaniyeler ve aynı hesaba eşzamanlı istek gerekiyor. Ama hata sessiz ve kimlik hassas alanında.
- **Recommended action:**
  - Sonucu kontrol et. `ConcurrencyFailure` gelirse kullanıcıyı yeniden okuyup sınırlı sayıda tekrar dene. Alternatif: `security_stamp` ile `concurrency_stamp`'i `RecordFailureAsync`'teki gibi tek bir `ExecuteUpdateAsync` ile yenile.
  - Başarısızlık kalırsa logla. Log, `docs/security.md` R-9 gereği e-posta içermemeli.
  - Test zorunlu değil, çünkü yarışı deterministik kurmak zor. Bir birim/entegrasyon testi ancak sahte bir `ConcurrencyFailure` ile mümkün.

#### F-2 — Info — Maintainability (dokümantasyon) — `docs/security.md` "Oturum geçersizleştirme" maddesi bir noktada yanlış, bir noktada eksik
- **Evidence:** `docs/security.md:41-43`.
- **Yanlış olan kısım: "rol değişikliği (damgayı yeniler)".**
  - Rol veren iki çağrı da damgayı yenilemiyor: `InitialAdminSeeder.cs:29` ve `UserAccounts.cs:59` `AddToRoleAsync` kullanıyor. Identity'nin `UserManager.AddToRoleAsync`'i yalnızca `UpdateUserAsync` çağırıyor, `UpdateSecurityStamp` çağırmıyor.
  - Rolün oturuma ulaşmasının gerçek nedeni şu: `SecurityStampValidator`, damga geçerli olduğunda her aralıkta principal'ı yeniden üretiyor (`ReplacePrincipal`). Bu yüzden rol değişikliği yeniden giriş istemeden en geç 1 dakikada oturuma yansıyor. `IdentityModule.cs:31` yorumu bunu doğru anlatıyor.
  - Bu çıkarım Identity kaynak davranışına dayanıyor; çalıştırarak doğrulamadım.
- **Eksik olan kısım:** Çıkış, hesap genelindeki damgayı yeniliyor. Bu yüzden kullanıcının diğer cihaz ve tarayıcılardaki oturumları da en geç 1 dakikada bitiyor. Bu davranış belgede açıkça yazmıyor. Spec R-7 bunu ne istiyor ne yasaklıyor.
- **Impact:** 0003 (rol atama, pasifleştirme) ve 0005 bu satıra dayanırsa yanlış bir varsayım taşır. Örneğin pasifleştirmenin damgayı açıkça yenilemesi gerektiği gözden kaçabilir.
- **Recommended action:** Maddeyi şöyle düzelt: "rol değişikliği en geç 1 dakikada oturuma yansır (principal yenilenir); oturumu hemen bitirmek gereken işlemler (pasifleştirme) damgayı açıkça yeniler; çıkış hesabın tüm oturumlarını bitirir". Davranışın bilinçli olarak kabul edildiğini triage'da teyit et.

#### F-3 — Info — Edge cases (geliştirme ortamı) — Yeniden üretilen ilk migration, eski migration'ı uygulamış yerel veritabanında açılışı kırar
- **Evidence:**
  - Eski migration `20261104153226_InitialIdentity` silindi. Yerine farklı kimlikli `20261106113830_InitialIdentity` geldi.
  - İki migration da `identity.allowed_email_domains` tablosunu oluşturuyor: `git -c safe.directory='*' show f14095a:src/Modules/Identity/PolsanAuction.Identity.Infrastructure/Migrations/20261104153226_InitialIdentity.cs` satır 19 ve yeni dosyanın satır 18-19.
  - `compose.yaml:12-13,21-22` veriyi kalıcı `pgdata` volume'unda tutuyor.
  - Dolayısıyla f14095a'da çalıştırılmış yerel bir DB'de `MigrateOnStartup=true` ile açılış 42P07 ("relation already exists") ile düşer. Eski `AspNet*` tabloları da sahipsiz kalır.
  - Bunu çalıştırarak göstermedim, çünkü DB'ye yazmak gerekiyor. Reprodüksiyon: f14095a'da API'yi bir kez başlat, HEAD'e geç, yeniden başlat.
  - Testler etkilenmiyor; her koşu kendi geçici container'ını kullanıyor.
- **Impact:** Triage "veri yok" diyerek yeniden üretimi kabul etti. Ancak gereken tek seferlik sıfırlama adımı hiçbir yerde yazılı değil. AC-10'un manuel klavye gözlemi ve ekran görüntüleri tam da bu yerel ortamda alınacak.
- **Recommended action:** PR açıklamasına ve VERIFY handoff'una tek satır ekle: "Yerel DB'yi bir kez sıfırlayın: `docker compose down -v` ardından `docker compose up -d --wait`". Kalıcı doküman değişikliği gerekmiyor; bu durum yalnızca bu dalın ara commit'lerini çalıştıranları etkiliyor.

### Boyutlar
- **Correctness:**
  - AC-1…AC-9, fix-1 sonrasında da testlerle karşılanıyor.
  - AC-8'in tarayıcı akışındaki iki açık (R1 F-3, F-4) kapandı.
  - AC-10: rol etiketi eklendi. StrictMode'daki "geçersiz" yanıtı giderildi. Kilit mesajı düzeltildi. Erişilebilirlik özellikleri (`role="alert"`, etiketler) değişmedi. Klavye kanıtı A-2(d) gereği manuel.
  - F-1 ve F-2 dışında bulgu yok.
- **Plan conformance:** Clean. Değişen her dosya plan listesinde, A-1'de, A-2'de (fix-1 dosyaları A-2(e)'de) ya da A-3'te. `docs/security.md` ve ADR 0001 planın doküman listesinde. Plan dışı dosya yok.
- **Security:** F-1. Bunun dışında:
  - Yeni sır yok; `appsettings.json` yalnızca sınır değerleri içeriyor.
  - Yeni loglarda e-posta ya da bağlantı yok.
  - Konsol gönderimi yalnızca Development'ta.
  - Giriş ve yeniden gönderim hız sınırları da `RemoteIpAddress`'e bağlı. Ters proxy arkasında tek IP sorunu zaten `docs/security.md`'de "Açık (dağıtım spec'i)" olarak kayıtlı; yeni bir bulgu değil.
- **Edge cases:** F-1, F-3. Bunun dışında:
  - Atomik sayaç kilitte sayacı 0'a çekiyor; bu, Identity'nin `AccessFailedAsync` davranışıyla aynı. Kilitten sonra paralel gelen hatalar kilidi yalnızca yeniden uzatıyor.
  - Benzersiz kısıt yolu entity'yi detach edip işlemi geri alıyor.
  - Pruning'deki `ICollection.Remove(KeyValuePair)` yalnızca değer eşleşirse siliyor; böylece aradaki yenilemeler korunuyor.
- **Performance:** Clean. Doğrulayıcı dakikada bir kullanıcı okuması yapıyor. Hatalı giriş başına bir `UPDATE` ve bir `SELECT` çalışıyor. Pruning 256 çağrıda bir O(n). Hepsi ihmal edilebilir; ölçüm gerektiren bir iddiam yok.
- **Tests:** Clean. Yeni testler davranışı assert ediyor: kilitli yanıt ve DB'deki `LockoutEnd`, tek hesap sayısı, header'daki token sırası, istek sayısı, hata kodu. Her biri eski kodda kırmızı olurdu:
  - Paralel kilit: yalnızca tek artış yapılırdı.
  - Kopyalanmış çerez: 200 dönerdi.
  - StrictMode: 2 istek giderdi.
  - Bayat token: yeniden deneme yapılmazdı.
  - Throttle testleri: yeni API olmadığı için derlenmezdi.

  Bu çıkarımları kod okumasıyla yaptım; testleri eski koda karşı çalıştırmadım (read-only). Kriter ↔ test haritası tam.
- **Maintainability:** F-2. Katman ihlali yok:
  - `IUserAccounts.HashPasswordForTimingOnly` port'ta Identity tipi taşımıyor.
  - `ExecuteUpdateAsync` Infrastructure'da.
  - `TimeProvider.System`'in doğrudan kullanımı (`AccountSignIn.cs:87,104`) A-1'e dayanıyor ve yorumla gerekçelendirilmiş.
  - Dead code yok.

Önerilen Known traps satırı yok. F-1'in tuzağı zaten `docs/conventions.md:63`'te yazılı; düzeltmeden sonra o satıra "`UpdateSecurityStampAsync` dahil" eklenebilir.

Real bulgular yalnızca Low/Info düzeyinde. `workflows/feature-development.md` adım 8'e göre bir fix-2 turu yeniden REVIEW gerektirmez, VERIFY'a geçer.

Bu tek geçişlik (one-pass) bir review değil; QA tablosu yok.

Next: TRIAGE — human (real / noise / defer / investigate); then Developer fix rounds, then VERIFY
