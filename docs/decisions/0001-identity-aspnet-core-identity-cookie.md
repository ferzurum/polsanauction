# ADR 0001 — Kimlik: ASP.NET Core Identity + oturum çerezi

- Status: Accepted
- Date: 2026-11-04

## Context
Spec 0002 (kayıt, e-posta doğrulama, giriş) için parola saklama, kilitlenme, doğrulama token'ı ve oturum
gerekiyor; hepsi hassas alan (kimlik/KVKK, `docs/domain.md`). Uygulama tek bir kurum içi SPA + API
(aynı köken, Vite proxy'si / ileride aynı host). İleride AD/OIDC girişi (spec 0007) aynı hesaplara bağlanacak.

## Decision
Hesaplar, parola hash'i, roller ve kilitlenme **ASP.NET Core Identity** (EF Core + PostgreSQL, `identity`
şeması) ile; oturum **HttpOnly, Secure, SameSite=Strict bir çerez** (8 saat kayan) ile; CSRF koruması
ASP.NET antiforgery token'ı (`X-XSRF-TOKEN` başlığı) ile yapılır. JWT kullanılmaz.

## Consequences
- **Kazanç:** parola hash'i (PBKDF2), kilitlenme, güvenlik damgası, rol yönetimi ve OIDC'ye bağlanma
  hazır ve bakımlı; kendi kripto kodumuz en aza iner. Çerez JavaScript'ten okunamaz (XSS'te token çalınmaz);
  çıkış ve oturum sonu sunucuda kesin.
- **Maliyet:** Identity'nin doğrulama token'ı ve kilidi `TimeProvider` kullanmıyor. Token için kendi
  `ConfirmationTokenCodec`'imiz yazıldı (Data Protection ile; plan 0002, A-1); kilit testi bitiş zamanını
  veritabanında geçmişe çekerek yapılıyor.
- **Maliyet:** Çerez + SPA = CSRF koruması zorunlu; istemci giriş/çıkıştan sonra antiforgery token'ını yeniler.
- **Maliyet:** Data Protection anahtarlarının üretimde kalıcı bir yerde saklanması gerekir; aksi hâlde yeniden
  başlatmada oturumlar ve bekleyen doğrulama bağlantıları geçersizleşir — dağıtım spec'inin konusu.
- `PasswordSignInAsync` kullanılmaz: doğrulanmamış hesabı parola doğru olmasa da açığa vurur;
  `AccountSignIn` sırayı (kilit → parola → doğrulama) kendisi kurar.

## Alternatives considered
- **JWT (Bearer) + localStorage:** XSS'te token çalınabilir, çıkış/iptal için ek mekanizma gerekir; aynı kökenli
  bir SPA'da kazancı yok.
- **Kendi hesap/parola tablomuz:** hash, kilit, damga ve OIDC bağlamayı yeniden yazmak; hassas alanda gereksiz risk.
- **Yalnızca AD/OIDC:** kullanıcı yerel kaydı da istedi (bootstrap, güvenlik konusu); OIDC 0007'de eklenir.

## Revisit triggers
- API'yi başka kökenden (mobil uygulama, üçüncü taraf) çağırma ihtiyacı.
- Birden fazla API örneği (Data Protection anahtar paylaşımı, `ResendThrottle`'ın bellek içi durumu).
- Identity'nin token sağlayıcısı veya kilidi `TimeProvider` desteği kazanırsa kendi codec'imiz kaldırılabilir.
