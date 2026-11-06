# Conventions

> Bootstrap ile dolduruldu (2026-10-29). Yalnızca gerçek kurallar: ya araçla zorlanır ya review'da kontrol edilir.

## Language
- Chat language (interviews, sessions): Turkish (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Turkish (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: English. UI metinleri Türkçe.
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- Backend: .NET 10 (LTS), C# (SDK `global.json` ile sabitlenir), ASP.NET Core, EF Core + Npgsql, SignalR.
- Testler: xUnit v3, **Microsoft.Testing.Platform** çalıştırıcısıyla (`global.json` → `test.runner`);
  VSTest modu .NET 10 + xunit.v3 4.x'te desteklenmez (plan 0001, A-1).
- Frontend: Node 22+ LTS (CI 24), React 19 + TypeScript 7 (strict), Vite 8, Vitest 5.
- Veritabanı: **PostgreSQL 18** (`postgres:18-alpine`; geliştirme `compose.yaml` ve testlerde aynı imaj).
- Paket sürümleri sabitlenir (NuGet Central Package Management, `package-lock.json` commit'lenir).
- Yeni bağımlılık eklemek için spec'te gerekçe gerekir; ticari ya da lisansı belirsiz paket
  (ör. AutoMapper/MediatR yeni sürümleri, FluentAssertions 8+) onaysız eklenmez (`docs/security.md`).

## Naming
- C#: standart .NET konvansiyonları — tipler/metotlar `PascalCase`, private alanlar `_camelCase`,
  async metotlar `…Async`. `.editorconfig` ile zorlanır.
- C# sabitleri ve `static readonly` alanlar `PascalCase` (`.editorconfig`).
- TS/React: değişken/fonksiyon `camelCase`, bileşenler ve dosyaları `PascalCase.tsx`, hook'lar `useX`.
- Veritabanı: `snake_case` (Npgsql naming convention ile otomatik).
- API: `/api/<kaynak-çoğul>/{id}/<alt-kaynak>`, ör. `POST /api/auctions/{id}/bids`.
- Testler: `<Metot/Davranış>_<Koşul>_<Beklenen>`; iş kuralı testleri adında BR numarası taşır,
  ör. `PlaceBid_BelowMinimumIncrement_IsRejected_BR2`.
- Kullanıcıya gösterilen ihale numarası: `IHL-<yıl>-<sıra>` (ör. `IHL-2026-0042`).

## Error handling
- İş kuralı ihlalleri exception değil, `Result` tipiyle döner (ör. `BidRejected(BelowMinimumIncrement)`).
  Exception yalnızca beklenmeyen durumlar içindir.
- API hataları RFC 9457 **ProblemDetails** formatında, makine okunur `code` alanıyla döner
  (`bid.below_minimum`, `auction.closed`, `bid.price_changed`).
- Kullanıcı mesajları Türkçedir ve frontend'de `code` üzerinden üretilir. Stack trace, SQL, iç detay
  kullanıcıya asla gösterilmez.
- Loglar Serilog ile yapılandırılmıştır; kişisel veri (tam isim, e-posta) yerine kullanıcı ID'si loglanır.

## Data rules
- **Para:** `long` kuruş (`AmountKurus`); DB `bigint`; frontend tam sayı kuruş. `float`/`double` yasak.
  Gösterim yalnızca sunum katmanında, tr-TR.
- **Zaman:** her yerde UTC (`DateTimeOffset`, DB `timestamptz`); kullanıcıya Europe/Istanbul gösterilir.
  Şimdiki zaman yalnızca `TimeProvider` ile alınır.
- **Kimlikler:** `Guid` v7 (`Guid.CreateVersion7()`); kullanıcıya ayrıca okunabilir numara gösterilir.
- **Eşzamanlılık:** teklif kaydında optimistic concurrency (versiyon kolonu / `xmin`); çakışmada
  `bid.price_changed` döner (BR-3).
- **Silme yok:** ürün ve ihaleler durum değiştirir; teklifler ve denetim kayıtları asla silinmez ya
  da güncellenmez.

## Known traps
- Npgsql `timestamptz` alanına `Kind=Unspecified/Local` `DateTime` yazılırsa hata verir — `DateTimeOffset`/UTC kullan — from bootstrap.
- SignalR bağlantısı koptuğunda UI eski fiyatı gösterebilir — yeniden bağlanınca güncel durumu API'den yeniden çek — from bootstrap.
- İhale kapanışını bir isteğe bağlamak kapanışı geciktirir — kapanış `BackgroundService` + `TimeProvider` ile zamanlanır — from bootstrap.
- Windows'ta `core.autocrlf=true` + `* text=auto` checkout'u CRLF yapar, Prettier ve `dotnet format` kırmızıya düşer — `.gitattributes`'taki `* text=auto eol=lf` korunur — from spec 0001, VERIFY F-1 (A-4).
- ASP.NET Core Identity'nin doğrulama token'ı ve kilidi `TimeProvider` kullanmaz — süre testleri için token `ConfirmationTokenCodec` ile üretilir; kilit testi bitiş zamanını veritabanında geçmişe çeker — from spec 0002, A-1.
- EF Core sağlayıcının `DbException`'ını `InvalidOperationException` ("transient failure") içine sarar — "DB erişilemez" yakalarken istisna zincirine bakılır — from spec 0002, adım 6.
- Kimlik çerezleri `Secure` — entegrasyon testlerinde istemci `https://localhost` taban adresiyle açılır (`AuthApp.CreateApiClientAsync`) — from spec 0002, adım 5.
- Identity `UserManager.AccessFailedAsync`/`UpdateSecurityStampAsync`/`UpdateAsync` eşzamanlılık çakışmasını istisna değil `IdentityResult` (`ConcurrencyFailure`) olarak döndürür; sonuç kontrol edilmezse paralel isteklerde kilit sayacı kaybolur — sayaç ve çıkıştaki damga yenilemesi tek bir atomik `ExecuteUpdateAsync` ile yapılır (`AccountSignIn`) — from spec 0002, review round 1 F-1 (A-3) ve round 2 F-1.
- `AddIdentityCore` + elle `AddCookie` kurulumunda güvenlik damgası doğrulanmaz — `OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync` elle bağlanır; reddedince `Identity.TwoFactorRememberMe` şemasından da çıkış yapıldığı için o şema da kayıtlı olmalı (yoksa 500) — from spec 0002, review round 1 F-3 (A-3).
- Antiforgery token kullanıcıya bağlıdır; oturum sunucuda düştüğünde (süre, çıkış, başka sekmede giriş) SPA'nın önbellekteki token'ı `csrf.invalid` alır — `web/src/api/client.ts` 401'de token'ı unutur, `csrf.invalid`'de yenisini alıp isteği bir kez yeniden dener — from spec 0002, review round 1 F-4 (A-3).
- React StrictMode geliştirmede efektleri iki kez çalıştırır — `useEffect` içindeki tek kullanımlık POST (doğrulama bağlantısı vb.) `useRef` ile bir kez gönderilir; bileşen testi `renderAt(…, { strict: true })` ile sınar — from spec 0002, review round 1 F-5 (A-3).
- EFCore.NamingConventions, Identity'nin açıkça verdiği `ToTable("AspNetUsers")` ve indeks adlarını (`UserNameIndex`) değiştirmez — tablolar ve indeksler `IdentityDbContext.OnModelCreating`'de açıkça snake_case adlandırılır — from spec 0002, review round 1 F-11 (A-3).
- Windows'tan commit'lenen script çalıştırılabilir bitini kaybeder (`100644`), Linux CI `Permission denied` verir — yeni script'te `git update-index --chmod=+x -- <yol>` ve `git ls-files -s` ile `100755` doğrulanır — from spec 0001, CI (A-6).

## Database migrations
- Şema değişikliği yalnızca migration ile: `dotnet tool restore`, ardından
  `dotnet ef migrations add <Ad> --project src/Modules/<Modül>/PolsanAuction.<Modül>.Infrastructure --output-dir Migrations`.
- Üretilen dosyalar elle düzenlenmez (`.editorconfig`: generated code). `MigrationTests` modelde migration'a
  girmemiş değişiklik kalırsa kırmızıya düşer.
- Açılışta migration yalnızca `Database:MigrateOnStartup=true` iken (yerelde `launchSettings`, testlerde `AuthApp`);
  üretim stratejisi dağıtım spec'inde.

## Enforced by tooling
- Derleyici: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, .NET analyzers.
- `dotnet format --verify-no-changes` (`.editorconfig`), oxlint + Prettier (`npm run lint`),
  `tsc -b` (strict). ESLint değil: typescript-eslint TS 7'yi desteklemiyor (plan 0001, A-2).
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt`: `DateTime.Now/UtcNow/Today` ve
  `DateTimeOffset.Now/UtcNow` derleme hatasıdır (RS0030) — `TimeProvider` kullanılır.
- Mimari testler — ArchUnitNET (`docs/architecture.md`, Forbidden dependencies).
- Hepsi `scripts/check` adımlarıdır (`scripts/check.conf`).
