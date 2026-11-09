# Spec 0002 — Kimlik: kayıt, e-posta doğrulama, giriş

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0002-plan.md`

## Intent
Polsan çalışanları sisteme kurumsal e-posta adresleriyle kendileri kayıt olabilmeli ve giriş
yapabilmeli; böylece ihalelere yalnızca e-postası doğrulanmış Polsan / grup şirketi çalışanları
erişir. Yöneticiler (Admin) ürün ve ihale yönetimi için ayrıca tanınmalı. Başarı: izinli alan
adına sahip bir çalışan kayıt olur, e-postasını doğrular, giriş yapar; izinsiz alanla kayıt
mümkün değildir; doğrulanmamış hesap giremez; ilk Admin, parola ya da varsayılan hesap olmadan
yapılandırmadan tanınır. Bilinçli olarak yapılmayanlar: AD/OIDC girişi (0007), parola sıfırlama
(0004), 90 günlük pasifleşme (0005), Admin paneli — izinli alan yönetimi, rol atama,
pasifleştirme (0003), KVKK onay ekranı (0006).

## Requirements
- R-1 — Kayıt: ad, soyad, kurumsal e-posta, parola ve parola tekrarı ile yapılır. E-posta adresinin
  alan adı izinli alanlar listesinde olmalıdır (BR-12). Başlangıç listesi `polsanteknoloji.com.tr`;
  liste kalıcı olarak saklanır, ilk değeri yapılandırmadan gelir.
- R-2 — Kayıt sonrası hesap "doğrulama bekliyor" durumundadır ve e-posta adresine tek kullanımlık,
  24 saat geçerli bir doğrulama bağlantısı gönderilir. Bağlantı kullanılınca hesap aktif olur.
- R-3 — Kayıt ekranı, e-posta zaten kayıtlı olsa da olmasa da aynı sonucu gösterir ("E-postanızı
  kontrol edin"). Zaten kayıtlı adrese yeni hesap açılmaz; o adrese "zaten bir hesabınız var"
  bilgilendirmesi gönderilir (hesap tespiti önlenir).
- R-4 — Doğrulanmamış hesap giriş yapamaz; kullanıcı doğrulama e-postasının yeniden gönderilmesini
  isteyebilir.
- R-5 — Giriş: doğrulanmış hesap, doğru e-posta ve parola ile giriş yapar. Yanlış parola ve kayıtlı
  olmayan e-posta aynı genel hatayı alır.
- R-6 — Üst üste 5 başarısız giriş denemesinden sonra hesap 15 dakika kilitlenir; kilit süresince
  doğru parola da reddedilir ve kullanıcıya kilidin ne zaman açılacağı söylenir.
- R-7 — Oturum, 8 saat hareketsizlikten sonra sona erer; "beni hatırla" yoktur. Kullanıcı istediği
  an çıkış yapabilir. Giriş gerektiren bir sayfaya oturumsuz gelen kullanıcı giriş ekranına yönlenir.
- R-8 — Roller: her yeni kullanıcı Personel'dir. Yapılandırmada tanımlı ilk Admin e-posta adresiyle
  kayıt olup e-postasını doğrulayan kullanıcı Admin olur. Yönetim alanı yalnızca Admin'e açıktır.
- R-9 — Parolalar geri döndürülemez biçimde saklanır; parola ve doğrulama bağlantısı hiçbir yanıtta,
  logda ya da hata mesajında görünmez. Loglarda kişi kullanıcı kimliğiyle anılır (isim/e-posta değil).
- R-10 — Geliştirme ve testte gerçek e-posta gönderilmez; gönderilecek içerik (doğrulama bağlantısı
  dahil) yalnızca geliştirme ortamının konsoluna düşer. Gerçek gönderim yapılandırmayla açılır.

## User experience
- Flow (kayıt): Ana sayfa → "Kayıt ol" → form → "E-postanızı kontrol edin" → e-postadaki bağlantı →
  "E-postanız doğrulandı, giriş yapabilirsiniz" → giriş ekranı.
- Flow (giriş): "Giriş yap" → e-posta + parola → ana sayfa; üst çubukta ad soyad, rol (Admin ise
  "Yönetim" bağlantısı) ve "Çıkış".
- States:
  - Kayıt formu: boş · alan hatası (her alanın altında Türkçe mesaj) · gönderiliyor · başarılı
    ("E-postanızı kontrol edin") · sunucu hatası.
  - Doğrulama sayfası: doğrulanıyor · başarılı · bağlantı geçersiz/süresi dolmuş/kullanılmış (yeniden
    gönderme seçeneğiyle).
  - Giriş formu: boş · gönderiliyor · hatalı bilgi (genel mesaj) · e-posta doğrulanmamış (yeniden
    gönder seçeneği) · hesap kilitli (açılış saati) · sunucu hatası.
  - Yönetim sayfası: Admin için "Yönetim paneli" yer tutucusu · Personel için "Bu sayfaya erişim
    yetkiniz yok" · oturumsuz → giriş ekranı.
- Design reference: none — the flow above is the design.
- Accessibility: her form alanının görünür etiketi var; hatalar alanla ilişkilendirilir ve ekran
  okuyucuya duyurulur; tüm akış klavyeyle tamamlanabilir; parola alanları otomatik doldurmayı destekler.

## Constraints & out of scope
- Sensitive area: Identity & personal data (KVKK) — `docs/domain.md`.
- Kararlar (clarify, 2026-11-02): ilk Admin e-postası `fatih.erzurum@polsanteknoloji.com.tr`
  (yapılandırma değeri); başlangıç izinli alanı `polsanteknoloji.com.tr`; parola 10–128 karakter, karakter türü zorunluluğu yok
  (`docs/security.md`); kayıtta sicil no ve telefon alınmaz (veri minimizasyonu).
- Açık karar: kurumsal SMTP sunucusu/relay bilinmiyor — bu spec onsuz tamamlanır (R-10).
- Out of scope: AD/OIDC (0007), parola sıfırlama (0004), 90 gün pasifleşme (0005), Admin paneli
  (0003), KVKK onayı (0006), doğrulanmamış hesapların otomatik silinmesi.
- Self-critique a–f folded in (2026-11-02): e-posta normalizasyonu ve tam alan eşleşmesi, kötüye
  kullanım sınırları, ad/soyad 1–100, parola 10–128, ilk Admin açılışta da uygulanır.
- Split: proposed 0002–0007 (clarify 1); this spec is 0002 — not split further (10 criteria, one capability).

## Acceptance criteria
- [ ] AC-1 — İzinli alanlı e-posta ile geçerli kayıt (e-posta baştaki/sondaki boşluklar atılarak ve büyük/küçük harf duyarsız ele alınır): hesap "doğrulama bekliyor" olarak oluşur, doğrulama e-postası gönderilir, ekran "E-postanızı kontrol edin" der; parola düz metin olarak saklanmaz.
- [ ] AC-2 — İzinli olmayan alan adıyla kayıt reddedilir (alan adı tam eşleşmelidir; `mail.polsanteknoloji.com.tr` gibi alt alan adı izinli değildir), hesap oluşmaz, kullanıcı "Yalnızca kurumsal e-posta adresleriyle kayıt olunabilir" mesajını görür.
- [ ] AC-3 — Zaten kayıtlı e-postayla kayıt: ekran AC-1 ile aynı sonucu gösterir, yeni hesap açılmaz, mevcut adrese "zaten bir hesabınız var" e-postası gider.
- [ ] AC-4 — Geçersiz form girdileri (boş ya da 100 karakterden uzun ad/soyad — boşluklar kırpılır, Türkçe harf ve iç boşluk geçerlidir; geçersiz e-posta; 10 karakterden kısa ya da 128 karakterden uzun parola; eşleşmeyen parola tekrarı) alan bazında Türkçe hatayla reddedilir ve hesap oluşmaz.
- [ ] AC-5 — Doğrulama bağlantısı: 24 saat içinde ilk kullanımda hesabı aktifleştirir; süresi dolmuş, daha önce kullanılmış ya da bozuk bağlantı hesabı aktifleştirmez ve yeniden gönderme seçeneği sunar.
- [ ] AC-6 — Giriş: doğrulanmış hesap doğru bilgilerle girer; yanlış parola ve kayıtlı olmayan e-posta aynı genel hatayı alır; doğrulanmamış hesap giremez ve yeniden gönderme seçeneği görür. Aynı adrese yeniden gönderim dakikada en fazla 1 kez, aynı kaynaktan kayıt denemesi saatte en fazla 10 kez yapılabilir; aşımda "Çok fazla deneme, lütfen sonra tekrar deneyin" gösterilir.
- [ ] AC-7 — 5 başarısız denemeden sonra hesap 15 dakika kilitlenir (doğru parola dahil reddedilir, açılış saati gösterilir); 15 dakika dolunca doğru parola ile giriş yapılır.
- [ ] AC-8 — Çıkış oturumu bitirir; 8 saat hareketsizlikten sonra oturum geçersizdir; oturumsuz kullanıcı giriş gerektiren sayfadan giriş ekranına yönlenir.
- [ ] AC-9 — Yeni kullanıcı Personel'dir; yapılandırılmış ilk Admin adresi doğrulanınca — ya da uygulama açılışında bu adresle doğrulanmış hesap zaten varsa — Admin olur; Personel yönetim alanına erişemez ("erişim yetkiniz yok"), Admin erişir — kontrol sunucuda yapılır.
- [ ] AC-10 — Kayıt, doğrulama, giriş ve yönetim ekranlarının User experience'ta listelenen tüm durumları Türkçe metinle gösterilir; formlar klavyeyle tamamlanır ve hatalar ekran okuyucuya duyurulur.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
