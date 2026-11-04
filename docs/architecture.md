# Architecture

> Bootstrap ile dolduruldu (2026-10-29). Karar verilmiş mimariyi anlatır; ajanlar plan yapmadan önce okur.

## System overview

Polsan Auction, kurum içi (intranet) çalışan tek bir **modüler monolittir**: bir ASP.NET Core
(.NET 10) backend, bir React + TypeScript (Vite) SPA ve bir PostgreSQL veritabanı. Canlı teklif
güncellemeleri SignalR ile, bildirimler e-posta ile gider; ihale kapanışı ve 90 günlük pasifleşme
arka plan servisleriyle (`BackgroundService`) zamanında tetiklenir. **Gerekçe:** kullanıcı sayısı
kurum içiyle sınırlı (yüzler mertebesi), tek geliştirici var; mikroservis yalnızca operasyon yükü
getirir. Modül sınırları baştan çizildiği için ileride parçalamak mümkündür.

Hedef klasör yapısı (ilk iskelet spec'inde kurulur):

```
src/
  Api/                         ASP.NET Core host (endpoints, SignalR hub, DI, auth)
  Modules/<Module>/Domain      saf C#: entity, value object, kurallar
  Modules/<Module>/Application use case'ler, arayüzler
  Modules/<Module>/Infrastructure EF Core, e-posta vb.
web/                           React + TypeScript (Vite)
tests/                         PolsanAuction.<Module|Api>.UnitTests / .IntegrationTests, PolsanAuction.ArchitectureTests
```

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| Catalog | Ürün kaydı, teknik özellikler, kondisyon, fotoğraflar | Ürünler, fotoğraf dosyaları |
| Auction | İhale yaşam döngüsü, teklif kuralları, süre uzatma, kapanış, devir (BR-2…BR-8) | İhaleler, teklifler, devir teklifleri |
| Identity | Kayıt, e-posta doğrulama, yerel + AD/OIDC giriş, roller, izinli alanlar, pasifleşme, baş harf maskeleme | Kullanıcılar, roller, izinli e-posta alanları, KVKK onayları |
| Notification | E-posta gönderimi (kazanan, devir, doğrulama) | Gönderim kuyruğu / şablonlar |
| Audit | Silinemez işlem kaydı (BR-11) | Denetim kayıtları (append-only) |
| Web (SPA) | Arayüz; gösterim ve kullanıcı etkileşimi | Hiçbir iş verisi |

## Communication rules

- Modüller birbirine yalnızca diğer modülün **public arayüzü** (Application katmanındaki
  sözleşmeler) ya da **domain olayları** üzerinden erişir.
- **Doğrudan çağrı:** okuma amaçlı ve senkron ihtiyaçta (Auction → Catalog: ürün özeti).
- **Olay:** yan etkiler için (Auction `AuctionClosed`, `BidPlaced` → Notification, Audit, SignalR).
- **Yasak:** bir modülün diğerinin tablolarına, `DbContext`'ine ya da `internal` tiplerine erişmesi.
- İş kuralı kararları yalnızca Auction (ve Identity) **Domain** katmanında verilir; API ve UI
  bu kuralları tekrar etmez, çağırır. UI'daki doğrulamalar yalnızca gösterim amaçlıdır.

## Forbidden dependencies (make them testable)

Mimari testlerle (ArchUnitNET) zorlanır — `tests/PolsanAuction.ArchitectureTests`; madde 5
derleme zamanında BannedApiAnalyzers ile; madde 4 ve 7 ilgili özelliklerle eklenir:

1. `*.Domain` projeleri EF Core, ASP.NET Core, SignalR, e-posta veya başka altyapı paketlerine
   referans vermez.
2. Bir modül başka bir modülün `Domain`/`Infrastructure` tiplerine ya da `DbContext`'ine erişmez;
   yalnızca `Application` sözleşmelerine.
3. `Catalog` → `Auction` bağımlılığı yoktur (yalnızca Auction → Catalog).
4. `Audit` modülü dışında hiçbir kod denetim tablosuna yazmaz; hiçbir kod onu güncellemez/silmez.
5. Kodda `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.Now` kullanılmaz — `TimeProvider`.
6. Para alanlarında `float` / `double` kullanılmaz.
7. Frontend harici CDN, analitik ya da 3. parti telemetri yüklemez.

## Deliberately out of scope

- Sistem içi ödeme (ödeme ve teslim sistem dışında; sistem yalnızca "teslim edildi" işaretler).
- Mobil uygulama (responsive web yeterli).
- İnternete açık erişim (yalnızca intranet/VPN).
- Hollanda usulü, kapalı zarf vb. diğer açık artırma türleri.
- Kişi başı kazanma sınırı (BR-6), otomatik devir (devri Admin başlatır).
- Uçtan uca (e2e) tarayıcı testleri (bkz. `docs/testing.md`).
