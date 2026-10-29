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
- Frontend: Node 22+ LTS (yerelde 26 kurulu), React + TypeScript (strict), Vite.
- Veritabanı: PostgreSQL (sürüm iskelet spec'inde sabitlenir; testlerde Testcontainers ile aynı imaj).
- Paket sürümleri sabitlenir (NuGet Central Package Management, `package-lock.json` commit'lenir).
- Yeni bağımlılık eklemek için spec'te gerekçe gerekir; ticari ya da lisansı belirsiz paket
  (ör. AutoMapper/MediatR yeni sürümleri, FluentAssertions 8+) onaysız eklenmez (`docs/security.md`).

## Naming
- C#: standart .NET konvansiyonları — tipler/metotlar `PascalCase`, private alanlar `_camelCase`,
  async metotlar `…Async`. `.editorconfig` ile zorlanır.
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

## Enforced by tooling
- Derleyici: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, .NET analyzers.
- `dotnet format --verify-no-changes` (`.editorconfig`), ESLint + Prettier, `tsc --noEmit` (strict).
- Mimari testler (`docs/architecture.md`, Forbidden dependencies).
- Hepsi `scripts/check` adımlarıdır (`scripts/check.conf`).
