# Security

> Bootstrap ile dolduruldu (2026-10-29). Ajanların her plan ve review'da uyduğu temel kurallar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Bağlantı dizesi, SMTP parolası, OIDC client secret: ortam değişkeni / .NET user-secrets (geliştirme)
  ya da sunucu secret store'u (üretim). `appsettings*.json` içinde sır bulunmaz.

## Input & output
- Tüm girdiler API sınırında doğrulanır; iş kuralı kararı yine domain'de verilir (`docs/architecture.md`).
- EF Core parametreli sorgular; ham SQL'de string birleştirme yasak.
- React varsayılan kaçışlaması kullanılır; `dangerouslySetInnerHTML` yasak.
- Hata yanıtları ProblemDetails; stack trace, SQL, iç yol ya da başka kullanıcının kişisel verisi sızmaz.
- **Baş harf maskeleme (BR-9) sunucuda** yapılır: Personel'e dönen hiçbir yanıt başkasının tam ismini,
  e-postasını ya da sicil numarasını içermez.
- **Fotoğraf yükleme:** yalnızca Admin; yalnızca JPEG/PNG/WebP, en fazla 5 MB; içerik türü dosya
  başlığından doğrulanır, görsel yeniden kodlanır (EXIF temizlenir), rastgele adla web kökü dışında saklanır.
- Teklif uç noktasında hız sınırı (ör. kullanıcı başı 10 teklif/dakika); giriş, kayıt ve parola
  sıfırlamada da hız sınırı.

## AuthN / AuthZ
- **Varsayılan: reddet.** Her uç nokta kimlik doğrulama ister; anonim olanlar (giriş, kayıt, doğrulama
  linki, parola sıfırlama, sistem durumu kontrolü) açıkça işaretlenir.
- **Sistem durumu kontrolü** (`/api/health`, spec 0001) anonimdir ve yalnızca `Healthy` / `Unhealthy`
  döner; açıklama, istisna, bağlantı bilgisi asla yanıta yazılmaz.
- **Yol A — yerel kayıt (her zaman açık):** ASP.NET Core Identity. E-posta, Admin'in yönettiği izinli
  alan listesinde olmalı (BR-12); e-posta doğrulaması zorunlu (tek kullanımlık link, 24 saat);
  parola en az 10 karakter; başarısız denemelerde geçici kilit; parola sıfırlama doğrulama linkiyle.
- **Yol B — AD ile giriş (opsiyonel):** standart OpenID Connect (Entra ID ya da AD FS — yalnızca
  yapılandırma farkı). İlk girişte kısa kayıt adımı ve KVKK onayı; aynı e-postalı hesaba bağlanır (BR-14).
- **Pasifleşme (BR-13):** 90 gün giriş yok → pasif; aktivasyon yalnızca kurumsal e-posta doğrulamasıyla.
  Admin elle pasifleştirebilir.
- **Roller** uygulama içinde tutulur (`Admin`, `Bidder`). İlk Admin yapılandırmadan seed edilir;
  sonrakileri Admin atar. AD grubu varsa ek bir kaynaktır.
- Yetki kontrolü **sunucuda, her uç noktada ve SignalR hub metodunda** yapılır; UI'da gizleme yetki değildir.
- **Oturum (spec 0002, ADR 0001):** ASP.NET Core Identity + `polsan.auth` çerezi — `HttpOnly`, `Secure`,
  `SameSite=Strict`, 8 saat hareketsizlikte biter (kayan), "beni hatırla" yok. API yönlendirme yapmaz:
  oturumsuz → 401, yetkisiz → 403.
- **CSRF:** her `POST` `X-XSRF-TOKEN` başlığında antiforgery token'ı taşır (`GET /api/auth/antiforgery`);
  token kullanıcıya bağlıdır, istemci giriş/çıkıştan sonra yeniler. Eksik/yanlış → 400 `csrf.invalid`.
- **Anonim auth uç noktaları:** `GET /api/auth/antiforgery`, `POST /api/auth/register | confirm-email |
  resend-confirmation | login`. `logout`, `me` ve `/api/admin` oturum ister; `/api/admin` `Admin` rolü ister.
- **Hesap tespitini önleme:** kayıt (yeni/var olan adres), yeniden gönderim (bilinmeyen/doğrulanmış adres) ve
  giriş (yanlış parola/bilinmeyen e-posta) aynı yanıtı verir; "doğrulanmamış" yalnızca parola doğruysa söylenir.
- **Kilit ve sınırlar:** 5 hatalı giriş → 15 dk kilit (sayaç tek bir atomik `UPDATE` ile artar, paralel
  isteklerde de); giriş kaynak başına dakikada 30; kayıt kaynak başına saatte 10; doğrulama yeniden
  gönderimi adres başına dakikada 1 (adres kayıtlı olmasa da) → 429 `rate_limited`.
- **Doğrulama bağlantısı:** Data Protection ile şifreli/imzalı, 24 saat, tek kullanımlık (`ConfirmationTokenCodec`).
- **Açık (dağıtım spec'i):** Data Protection anahtarlarının kalıcı saklanması; ters proxy arkasında gerçek
  istemci IP'si için `ForwardedHeaders`.
- Yalnızca HTTPS.
- Erişim yalnızca intranet / VPN; uygulama internete açılmaz.

## Personal data (KVKK)
- Minimum veri: ad, soyad, kurumsal e-posta, (gerekirse) sicil no, telefon (opsiyonel), teklifler.
- İlk girişte aydınlatma metni gösterilir ve onay kaydedilir (metin hukuk/İK'dan gelir).
- Loglarda tam isim/e-posta yerine kullanıcı ID'si; harici CDN, analitik ve telemetri yok.

## Audit
- Teklifler ve Admin işlemleri append-only denetim tablosunda (BR-11). Uygulamanın veritabanı
  kullanıcısının bu tabloda `UPDATE`/`DELETE` yetkisi yoktur.

## Dependencies
- Yeni NuGet/npm paketi yalnızca spec'te gerekçesiyle ve insan onayıyla eklenir; kontrol edilenler:
  lisans (ticari/belirsiz lisans yasak), bakım durumu, bilinen CVE.
- `dotnet list package --vulnerable --include-transitive` ve `npm audit --audit-level=high`
  `scripts/check` içinde çalışır.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
