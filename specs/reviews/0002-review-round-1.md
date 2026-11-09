# Review — spec 0002, round 1

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1)
- Diff: `feature/0002-identity-signup-login` vs `main`, HEAD `f14095a` (99 dosya)
- Reviewer: bağımsız read-only subagent, 2026-11-06 (önceki round-1 raporu kaybolduğu için yeniden koşuldu)
- Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| F-1 | High | real | fix-1 |
| F-2 | Medium | real | fix-1 |
| F-3 | Medium | real | fix-1 |
| F-4 | Medium | real | fix-1 |
| F-5 | Medium | real | fix-1 |
| F-6 | Low | real | fix-1 |
| F-7 | Low | defer | `specs/backlog.md` B-5 (dağıtım spec'i) |
| F-8 | Low | real | fix-1 |
| F-9 | Low | real | fix-1 |
| F-10 | Low | real | fix-1 |
| F-11 | Low | real | fix-1 (veri yok; migration yeniden üretilir) |
| F-12 | Low | real | A-2; B-4 bu dalda kapanır |
| F-13 | Info | real | harita A-2'de; klavye kanıtı AC-10 manuel gözlem, yeni paket yok |
| F-14 | Info | real | eksiz mesaj; kilit DoS + hesap tespiti kabul edilen risk (`docs/security.md`, ADR 0001) |

A-3: F-1, F-3, F-4, F-5, F-11 için `docs/conventions.md` "Known traps" satırları.

Triaged by: ferzurum · on: 2026-11-06

---

## Report

Review round 1 · fix rounds so far: 0

**Kapsam:** `feature/0002-identity-signup-login` ile `main` karşılaştırıldı (HEAD `f14095a`, 99 dosya). Spec 0002, plan 0002 ve onaylı A-1 esas alındı. Dal origin ile senkron.

**CHECK:** Builder'ın handoff'taki full GREEN sonucunu alıntılıyorum (11 adım, 245 sn; 68 fast + 40 entegrasyon + 35 web; HEAD `f14095a`). `docs/testing.md` Two speeds kural 6'ya göre yeniden koşmadım. CI durumunu doğrulayamadım: `gh run list -R ferzurum/polsanauction --branch feature/0002-identity-signup-login` HTTP 404 döndü (yetki ya da görünürlük sorunu). Triage, HEAD için CI full koşusunu görmeden kapanmamalı.

### Bulgular (önem sırasına göre)

#### F-1 — High — Security / Edge cases — Paralel isteklerle kilit (AC-7) aşılabiliyor; giriş uç noktasında hız sınırı yok
- **Evidence:**
  - `src/Modules/Identity/PolsanAuction.Identity.Infrastructure/AccountSignIn.cs:28-33`: `await users.AccessFailedAsync(user);` sonucu kontrol edilmiyor.
  - ASP.NET Core Identity'de `UserManager.AccessFailedAsync`, sayacı bellekteki nesnede artırıp `UpdateAsync` çağırıyor. `UserStore.UpdateAsync` `concurrency_stamp` ile iyimser eşzamanlılık kullanıyor (snapshot: `IdentityDbContextModelSnapshot.cs:223` `IsConcurrencyToken`). Çakışmada `DbUpdateConcurrencyException`'ı yakalayıp `IdentityResult.Failed(ConcurrencyFailure)` döndürüyor, istisna atmıyor.
  - Aynı hesaba N paralel yanlış parola isteği gelirse hepsi aynı damgayı okuyor (PBKDF2 yaklaşık 100 ms sürüyor). Yalnızca biri sayacı artırıyor, kalan N-1 artış sessizce kayboluyor. Böylece 15 dakikada 5 yerine 5×N tahmin yapılabiliyor.
  - Bunu sınırlayacak bir şey de yok: `AuthEndpoints.cs:134-147` `/login` uç noktasında `RequireRateLimiting` yok. `docs/security.md` ise şunu istiyor: "giriş, kayıt ve parola sıfırlamada da hız sınırı".
  - `LockoutTests` yalnızca sıralı istekleri sınıyor.
- **Impact:** Kimlik hassas alanında kaba kuvvet korumasının ana kontrolü paralellikle etkisizleşiyor. AC-7 sıralı senaryoda sağlanıyor ama amacına ulaşmıyor.
- **Recommended action:**
  1. `AccessFailedAsync` sonucu `ConcurrencyFailure` olursa kullanıcıyı yeniden okuyup tekrar dene (sınırlı döngü). Alternatif: sayacı atomik SQL ile artır (`UPDATE … SET access_failed_count = access_failed_count + 1 … RETURNING`).
  2. `/login` için kaynak başına bir hız sınırı politikası ekle (docs/security.md).
  3. Bir entegrasyon testi ekle: aynı hesaba ör. 20 paralel yanlış parola gönderildiğinde hesap kilitlenmeli.
  - Bu bir eşzamanlılık kriteri. Triage isterse önce QA minimal reprodüksiyonu yapılabilir (investigate, R-05). Bulgu Identity kaynak kodunun davranışından çıkarıldı, çalıştırılarak gösterilmedi.
- **Known traps önerisi:** "Identity `UserManager.AccessFailedAsync`/`UpdateAsync` eşzamanlılık çakışmasını istisna değil `IdentityResult` (ConcurrencyFailure) olarak döndürür. Sonuç kontrol edilmezse paralel isteklerde kilit sayacı kaybolur."

#### F-2 — Medium — Security — `ConsoleEmailSender` her ortamda kayıtlı; üretimde doğrulama bağlantıları stdout'a düşer
- **Evidence:** `src/Modules/Notification/PolsanAuction.Notification.Infrastructure/NotificationModule.cs:13-14`: `AddSingleton<IEmailSender>(_ => new ConsoleEmailSender(Console.Out))` ortamdan bağımsız. `ConsoleEmailSender.cs:13-21` alıcı adresi ve bağlantıyı tam olarak yazıyor.
- **Impact:** R-10'a göre içerik "yalnızca geliştirme ortamının konsoluna" düşmeli. R-9'a göre doğrulama bağlantısı ve e-posta hiçbir logda görünmemeli. Development dışında stdout genellikle log toplayıcıya gider. Loglara erişen biri bağlantıyla başkasının hesabını etkinleştirebilir ve e-posta adresleri loga düşer.
- **Recommended action:** Konsol gönderimini yalnızca `IsDevelopment()` (ya da açık bir `Email:Delivery=Console` ayarı) ile kaydet. Diğer ortamlarda açılışta net bir hatayla dur ya da gönderimi reddet; SMTP kararı gelene kadar böyle kalsın. Bunu bir testle sabitle.

#### F-3 — Medium — Security / Correctness (AC-8) — Çıkış oturumu sunucu tarafında geçersiz kılmıyor; `SecurityStampValidator` bağlı değil
- **Evidence:**
  - `IdentityModule.cs:66-79`: çerez `AddIdentityCore` ile birlikte elle `AddCookie` üzerinden kuruluyor ve `Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync` atanmıyor (`AddIdentity` bunu kendisi yapar). Repo'da `OnValidatePrincipal` geçmiyor (`grep -rn OnValidatePrincipal src` boş).
  - `AccountSignIn.cs:45` `SignOutAsync` yalnızca çerezi siliyor, güvenlik damgasını güncellemiyor.
  - `SessionTests.Logout_EndsTheSession_AC8` çerezi silinmiş aynı istemciyle sınıyor. Çıkıştan önce kopyalanmış bir çerezin tekrar kullanımını sınamıyor.
- **Impact:**
  - Çalınmış ya da kopyalanmış bir çerez çıkıştan sonra da 8 saate kadar (kayan sürede daha uzun) geçerli kalıyor.
  - Rol talepleri hiç yenilenmiyor. Örnek: oturumu açıkken açılışta Admin yapılan kullanıcıda `/me` (DB'den okuyor) "Admin" gösteriyor, dolayısıyla "Yönetim" bağlantısı görünüyor, ama `/api/admin` (çerezdeki rol) 403 dönüyor.
  - 0003 (rol atama, pasifleştirme) ve 0005 (pasifleşme) bu mekanizmaya dayanacak.
- **Recommended action:**
  - `OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync` bağla. Kısa bir `ValidationInterval` ver ve `TimeProvider`'ın kullanıldığını doğrula.
  - Çıkışta `UpdateSecurityStampAsync` çağır.
  - Test ekle: çıkıştan önce saklanan çerez çıkıştan sonra 401 almalı.
  - Triage ertelemeyi seçerse `specs/backlog.md`'ye 0003'e bağlı bir kayıt aç.
- **Known traps önerisi:** "`AddIdentityCore` + elle `AddCookie` kurulumunda güvenlik damgası doğrulanmaz. `OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync` elle bağlanır."

#### F-4 — Medium — Edge cases / Correctness (AC-8 akışı) — Oturum düştükten sonra önbellekteki antiforgery token'ı girişi kırıyor
- **Evidence:**
  - `web/src/api/client.ts:13-27`: token modül düzeyinde bir kez alınıp önbelleğe konuyor ve yalnızca `login`/`logout` sonrasında sıfırlanıyor (`api/auth.ts:28-38`).
  - Token kullanıcıya bağlı (`docs/security.md`, CSRF).
  - 8 saat hareketsizlikten sonra istek anonim oluyor, ama önbellekteki token eski kullanıcıya ait.
  - Senaryo: `AdminPage.tsx:41-43` 401'de `/giris`'e yönlendiriyor ama token'ı sıfırlamıyor. Ardından `POST /api/auth/login` `AntiforgeryFilter.cs:70-77` üzerinden 400 `csrf.invalid` dönüyor. `LoginPage.tsx:46-47` bunu "Bir hata oluştu" olarak gösteriyor. Kullanıcı sayfayı yenilemeden giriş yapamıyor.
  - Aynı durum çok sekmede de oluşuyor: bir sekmede giriş yapılırsa diğer sekmenin anonim token'ı bayatlıyor.
  - Web testleri bu yolu sınamıyor.
- **Recommended action:**
  - `client.ts`'de 400 `csrf.invalid` gelirse token'ı sıfırlayıp isteği bir kez yeniden dene.
  - Herhangi bir 401'de de token'ı sıfırla.
  - `AuthProvider`'ın hesabı 401'de `null` yapmasını değerlendir.
  - Bileşen testi ekle: bayat token → `csrf.invalid` → yeniden deneme başarılı.
- **Known traps önerisi:** "Antiforgery token kullanıcıya bağlıdır. Oturum sunucuda düştüğünde (süre/çıkış/başka sekme) SPA'nın önbellekteki token'ı `csrf.invalid` alır. `csrf.invalid`/401'de token yenilenir."

#### F-5 — Medium — Correctness (AC-5, AC-10, geliştirme ortamı) — StrictMode'da doğrulama isteği iki kez gidiyor; kullanıcı "geçersiz" görebiliyor
- **Evidence:**
  - `web/src/main.tsx:8` `<StrictMode>` kullanıyor. React 19, geliştirmede efektleri kur → temizle → kur sırasıyla çalıştırır.
  - `ConfirmEmailPage.tsx:17-36`: efekt tek kullanımlık, idempotent olmayan `POST /confirm-email` çağrısı yapıyor. Temizlik ilk isteğin sonucunu yok sayıyor (`active=false`), ikinci istek ise hesap zaten doğrulandığı için (`UserAccounts.cs:70` `user.EmailConfirmed` olduğundan) ya da eşzamanlılık çakışması yüzünden `confirmation.invalid` alıyor.
  - Sonuç: README 5. adımdaki geliştirme akışında hesap doğrulanıyor ama ekranda "Bu bağlantı geçersiz…" görünüyor.
  - Testler `MemoryRouter`'ı StrictMode olmadan render ettiği için (`web/src/test/api.tsx:57-63`) bu görünmüyor.
  - Üretim build'i etkilenmiyor, ama AC-10 ekran görüntüleri ve manuel QA geliştirmede alınacak.
- **Recommended action:**
  - Çağrıyı `useRef` ile `(accountId, token)` başına bir kez yap; ref'ler StrictMode'un yeniden bağlamasında korunur. Sonucu yok sayma mantığını buna göre düzenle.
  - StrictMode altında render eden bir bileşen testi ekle.
  - Triage isterse QA, `npm run dev` ile reprodüksiyon yapabilir.
- **Known traps önerisi:** "React StrictMode geliştirmede efektleri iki kez çalıştırır. `useEffect` içinde tek kullanımlık POST (doğrulama bağlantısı vb.) ref ile korunur."

#### F-6 — Low — Edge cases — Eşzamanlı aynı e-postayla kayıt 500 dönüyor; port sözleşmesi ve birim testi gerçek adaptörle uyuşmuyor
- **Evidence:**
  - `IUserAccounts.cs:405` "returns null when the e-mail was taken concurrently" diyor.
  - `UserAccounts.cs:34-41` yalnızca `DuplicateEmail`/`DuplicateUserName` doğrulayıcı hatalarını null'a çeviriyor.
  - Gerçek bir yarışta iki istek de doğrulayıcıdan geçiyor ve `UserNameIndex` benzersiz indeksi (`Migrations/20261104153226_InitialIdentity.cs:236-240`) `DbUpdateException` (23505) fırlatıyor. `UserStore.CreateAsync` bunu yakalamıyor, istek 500 dönüyor.
  - `RegisterUserTests.HandleAsync_AddressTakenConcurrently_AnswersAsForAnExistingAccount_AC3` sahte bir adaptörle, gerçek adaptörün vermediği bir davranışı kanıtlıyor.
- **Impact:** Nadir bir yarış durumu. R-3'ün "aynı sonuç" garantisi bu durumda bozuluyor: 202 yerine 500 dönüyor.
- **Recommended action:** `CreatePendingAsync` içinde benzersiz kısıt ihlalini (`PostgresException.SqlState == "23505"`, istisna zincirinde) yakalayıp `null` döndür. Bunu iki paralel kaydı sınayan bir entegrasyon testiyle kanıtla.

#### F-7 — Low — Correctness (dağıtım) — Rol ve izinli alan seed'i `Database:MigrateOnStartup` bayrağına bağlı; `App:WebBaseUrl` sessizce localhost'a düşüyor
- **Evidence:**
  - `IdentityModule.cs:108-111,123-139`: `Admin`/`Bidder` rolleri ve `InitialAllowedDomains` yalnızca bayrak true iken seed ediliyor. `appsettings.json` varsayılanı `false`.
  - Bir migration bundle ile kurulan ortamda roller yok: `UserAccounts.cs:46-50` `AddToRoleAsync(Bidder)` istisna atıyor ve her kayıt 500 dönüyor. İzinli alan tablosu boş: tüm kayıtlar reddediliyor (R-1 "ilk değeri yapılandırmadan gelir").
  - `AccountLinks.cs:8` `App:WebBaseUrl` yoksa `http://localhost:5173`'e düşüyor ve bu anahtar `appsettings.json`'da yok.
- **Recommended action:**
  - Rolleri migration'da `HasData` ile seed et.
  - İzinli alan seed'ini bayraktan bağımsız çalıştır.
  - Development dışında `App:WebBaseUrl` yoksa açılışta hata ver.
  - Ya da bunları dağıtım spec'i için `specs/backlog.md`'ye ertele.

#### F-8 — Low — Security — Zamanlamadan hesap tespiti (giriş ve kayıt)
- **Evidence:**
  - `AccountSignIn.cs:17-21`: bilinmeyen e-postada parola hash'lenmeden hemen dönülüyor. Kayıtlı e-postada `CheckPasswordAsync` (PBKDF2) çalışıyor.
  - `RegisterUser.cs:463-471`: var olan adreste hash yok, yeni adreste `CreateAsync` hash'liyor.
  - Yanıt gövdeleri aynı (`LoginTests:41-53`, `RegistrationTests:70-87`) ama süreleri ölçülebilir biçimde farklı.
- **Impact:** R-3/R-5'in amacı kısmen delinebiliyor. Etkisi sınırlı: intranet ortamı, kayıtta hız sınırı var, F-9'daki kilit kanalı da zaten var.
- **Recommended action:** Bilinmeyen e-postada sahte bir hash doğrulaması (`PasswordHasher.VerifyHashedPassword` ile sabit bir hash) çalıştır. Ya da kabul edilen risk olarak ADR 0001'e ve `docs/security.md`'ye yaz.

#### F-9 — Low — Edge cases / Performance — `ResendThrottle` sözlüğü hiç temizlenmiyor
- **Evidence:** `src/Modules/Identity/PolsanAuction.Identity.Application/ResendThrottle.cs:12-30`. Her farklı (rastgele) adres kalıcı bir kayıt bırakıyor ve uç noktada kaynak başına sınır yok (`AuthEndpoints.cs:130-132`).
- **Impact:** Anonim istemci uygulamanın belleğini sınırsızca büyütebiliyor. Intranet olduğu için düşük risk.
- **Recommended action:** Aralığı geçmiş kayıtları fırsatçı biçimde sil (ör. `TryAcquire` içinde N çağrıda bir), ya da `IMemoryCache` ile mutlak sona erme kullan. `/resend-confirmation`'a kaynak başına bir hız sınırı da eklenebilir.

#### F-10 — Low — Correctness (User experience) — Üst çubukta rol gösterilmiyor
- **Evidence:** Spec, User experience: "üst çubukta ad soyad, rol (Admin ise "Yönetim" bağlantısı) ve "Çıkış"". `web/src/components/Header.tsx:25-35` ad soyad, Admin için bağlantı ve Çıkış gösteriyor ama rol etiketini ("Personel"/"Admin") göstermiyor. `App.test.tsx:54-66` de rolü sınamıyor.
- **Recommended action:** Rol etiketini ekle (domain dilinde: `Bidder` → "Personel"). Spec metninin "rol" ifadesi yalnızca bağlantıyı kastediyorsa bunu triage'da noise olarak kapat.

#### F-11 — Low — Maintainability — Identity tablo ve indeks adları snake_case değil
- **Evidence:** `Migrations/20261104153226_InitialIdentity.cs:186,206-240`: `"AspNetUsers"`, `"AspNetRoles"`, `"UserNameIndex"`, `"EmailIndex"`, `"RoleNameIndex"`. `UseSnakeCaseNamingConvention`, Identity'nin açıkça verdiği `ToTable` adlarını değiştirmiyor. `docs/conventions.md` (Naming): "Veritabanı: `snake_case`".
- **Recommended action:** Veri yokken `OnModelCreating` içinde `ToTable("users")` vb. ile adlandır ve migration'ı yeniden üret. Ya da istisnayı `docs/conventions.md`'ye yaz.
- **Known traps önerisi:** "EFCore.NamingConventions, Identity'nin `ToTable("AspNetUsers")` adlarını değiştirmez. Tablolar `OnModelCreating`'de açıkça adlandırılır."

#### F-12 — Low — Plan conformance (R-07) — Plan ve A-1 dışında kalan dosyalar
Aşağıdaki her dosya plan listesinde de A-1(c)'de de yok. Builder R-07 ile bildirmemiş:
- **Kapsam dışı (B-4 düzeltmesi):** `compose.yaml` (healthcheck), `specs/backlog.md` (B-4 silindi), `README.md` adım 1'deki `--wait`. B-4 kaydı "spec 0002 ile çözülür" diyordu, ama plan bunu içermiyor.
- **Plan dışı doküman:** `docs/testing.md`. Planın doküman listesinde security/architecture/conventions/README var, testing.md yok.
- **Api:** `src/Api/PolsanAuction.Api/Auth/AntiforgeryFilter.cs`, `Auth/RateLimits.cs`. `Auth/AccountLinks.cs`, A-1'deki `IAccountLinks` port'unun uygulaması olarak kabul edilebilir.
- **Identity.Application:** `IAccountSignIn.cs`, `ResendThrottle.cs`. Plandaki `IdentityOptions.cs` (Application) yerine `IdentitySettings.cs` Infrastructure'da.
- **Identity.Infrastructure:** `AccountSignIn.cs`, `IdentityLog.cs`, `IdentitySettings.cs`.
- **Testler:** `tests/PolsanAuction.Api.IntegrationTests/HealthEndpointTests.cs` (collection fixture'a geçişin doğal sonucu). Birim testlerde plandaki `PersonNameTests.cs` ve `PasswordPolicyTests.cs` yok; yerine `RegistrationTests.cs` (birim) var.
- **Web:** `web/src/auth/context.ts`, `components/Header.tsx`, `components/ResendConfirmation.tsx`, `components/TextField.tsx`, `index.css`, `messages.ts`, `test/api.tsx`.
- **Örtük ve kabul edilebilir:** `.csproj` değişiklikleri, `PolsanAuction.slnx` (yeni test projesi).
- **Recommended action:** Bunları tek bir Amendment (A-2) ile plana işle; dosyaların hepsi makul ayrıştırmalar. B-4'ün bu dalda kapanmasını triage'da açıkça onayla ya da ayrı bir değişikliğe taşı.

#### F-13 — Info — Tests — Kriter ↔ test haritası güncel değil; birkaç zayıf assert
- **Evidence:**
  - Harita (`specs/plans/0002-plan.md`, Criterion ↔ test map) şu adları anıyor: `PersonNameTests.*`, `PasswordPolicyTests.*`, `RegistrationTests.Register_AllowedDomain_CreatesPendingAccountAndSendsEmail`, `Register_DomainNotAllowed_IsRejected`, `Register_ExistingEmail_ShowsSameResultAndSendsNotice`, `LockoutTests.FiveFailures_LocksFor15Minutes_ThenAllowsLogin`. Gerçek adlar farklı; kapsam birim testindeki `RegistrationTests` ile karşılanıyor. A-1'de `ConfirmationTokenCodecTests` var.
  - `tests/PolsanAuction.Api.IntegrationTests/RegistrationTests.cs` içindeki `Register_InvalidEmail_ReturnsEmailFieldError_AC4` yalnızca tek bir hata olduğunu sınıyor, hata kodunu sınamıyor.
  - `web/src/pages/RegisterPage.test.tsx:100-104`'teki "klavye" testi `fireEvent.submit` kullanıyor, gerçek klavye etkileşimi yok.
- **Recommended action:**
  - VERIFY'den önce haritayı gerçek test adlarıyla güncelle (A-2 içinde).
  - E-posta testinde kodu assert et (`email.invalid` / `required`).
  - Klavye kanıtını `@testing-library/user-event` (yeni paket, onay gerekir) ya da manuel bir gözlemle (AC-10 ekran görüntüleri) sağla.

#### F-14 — Info — Security (spec kaynaklı) — Kilit mesajı ve Türkçe ek
- **Evidence:**
  - `AuthEndpoints.cs:141-144`: kilit yanıtı (`lockedUntil`) yanlış parolayla da dönüyor. 5 deneme sonunda "kilitli" yanıtı yalnızca kayıtlı adreslerde geliyor; bu, R-6 ile R-5'in doğal gerilimi. Ayrıca herkes başkasının hesabını 15 dakika kilitleyebiliyor.
  - `web/src/messages.ts:46-52` `"…${time}'e kadar kilitli."` ifadesinde ek sabit: "14:30'e" ve "14:40'e" yanlış ("'a" olmalı).
- **Recommended action:**
  - Hesap tespiti ve kilitleme yoluyla DoS'u kabul edilen risk olarak ADR 0001 / `docs/security.md`'ye yaz.
  - Mesajı eksiz kur: "Hesabınız geçici olarak kilitlendi. Kilit açılış saati: 14:35."

### Boyut özeti
- **Correctness:**
  - AC-1…AC-7 ve AC-9 sunucu tarafında karşılanıyor ve testlerle kanıtlı.
  - AC-8 tarayıcı akışında F-3 ve F-4'ten etkileniyor.
  - AC-10'un durumları uygulanmış (kayıt: boş/alan hatası/gönderiliyor/başarılı/sunucu hatası/çok fazla deneme; doğrulama: doğrulanıyor/başarılı/geçersiz + yeniden gönder; giriş: tüm durumlar; yönetim: yer tutucu/yetkiniz yok/yönlendirme).
  - Erişilebilirlik: görünür etiketler, `aria-describedby` ve `role="alert"` var; parola alanlarında `autoComplete` var.
  - Açık noktalar: F-5 (geliştirme ortamında doğrulama ekranı), F-10 (rol etiketi).
- **Plan conformance:** F-12.
- **Security:** F-1, F-2, F-3, F-8, F-14. Sır yok: `appsettings.json` yalnızca e-posta/alan/sınır içeriyor. ProblemDetails iç ayrıntı sızdırmıyor. Admin kontrolü sunucuda (`AdminEndpoints.cs:17-19`).
- **Edge cases:** F-1, F-4, F-6, F-9.
- **Performance:** Ciddi sorun yok. N+1 ya da gereksiz materyalizasyon görmedim; kayıt başına izinli alan listesi okuması önemsiz. Yalnızca F-9.
- **Tests:** Testler davranışı assert ediyor (DB durumu, e-posta içeriği, yanıt gövdesi eşitliği, rol listesi), yalnızca durum kodlarını değil. Eksikler: paralel kilit testi (F-1), eski çerezin tekrar kullanımı (F-3), bayat token (F-4), StrictMode (F-5), gerçek yarış (F-6). Harita: F-13.
- **Maintainability:** Katman ihlali görmedim. Domain'de altyapı yok; mimari testi `Microsoft.Extensions.Identity`'yi de yasaklıyor. Dead code yok. F-11.

Bu tek geçişlik (one-pass) bir review değil; QA tablosu yok.

Next: TRIAGE — human (real / noise / defer / investigate); then Developer fix rounds, then VERIFY
