# Domain

> Bootstrap ile dolduruldu (2026-10-29). Bir terim burada yoksa ajan kendi anlamını uydurur — önce buraya ekleyin.

**Ürün hedefi:** Polsan'da kurum içinde kullanılmış teknolojik demirbaşlar (bilgisayar, monitör,
yazıcı vb.) hurdaya çıkarılmak yerine, ürün bilgileri paylaşılarak belirli bir süre boyunca açık
artırmaya çıkarılır; süre sonunda en yüksek teklifi veren Polsan çalışanı ürünü satın alır.
Kurum içi (intranet), responsive bir web uygulamasıdır. Ödeme ve teslim sistem dışında yapılır;
sistem kazananı belirler, bildirir ve teslimi işaretler.

## Ubiquitous language

Kod İngilizce, UI ve dokümanlar Türkçedir; bu tablo TR ↔ EN eşlemesidir.

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Ürün (`Item`) | Satışa çıkarılan demirbaş: demirbaş no, kategori, marka/model, teknik özellikler, kondisyon, fotoğraflar | Ürün ≠ ihale; bir ürün satılmazsa yeni bir ihaleye çıkabilir |
| İhale / Açık artırma / Müzayede (`Auction`) | Bir ürünün başlangıç–bitiş zamanı arasında teklife açılması. Tek ürün = tek ihale | Durumlar: `Draft → Scheduled → Open → Closed → Sold / Unsold / Cancelled` |
| Teklif (`Bid`) | Bir personelin bir ihale için verdiği tutar | Geri çekilemez, silinemez, güncellenemez |
| Başlangıç fiyatı (`StartingPrice`) | Kabul edilen ilk teklifin alt sınırı | |
| Minimum artış (`MinimumIncrement`) | Yeni teklifin mevcut en yüksek teklifi en az geçmesi gereken tutar | İhale başına Admin belirler |
| Süre uzatma (`Extension`) | Son 2 dakikada gelen teklifin bitişi 2 dakika ötelemesi | "anti-sniping" |
| Kazanan (`Winner`) | Kapanışta en yüksek geçerli teklifi vermiş personel | Devir sonrası 2./3. kişi de kazanan olabilir |
| Devir (`Offer` / `Handover`) | Kazanan vazgeçince ürünün sıradaki farklı teklif sahibine teklif edilmesi | Bkz. BR-7 |
| Admin (`Admin`) | Ürün, ihale, kullanıcı ve izinli e-posta alanlarını yöneten yetkili | Rol uygulama içinde tutulur |
| Personel (`Bidder` / `Employee`) | Teklif veren Polsan / grup şirketi çalışanı | |
| İzinli alan (`AllowedEmailDomain`) | Kayıt ve aktivasyona izin verilen kurumsal e-posta uzantısı | Admin panelinden yönetilir |
| Pasif kullanıcı (`Inactive`) | 90 gün giriş yapmamış ya da Admin'in pasifleştirdiği kullanıcı | Yeniden aktivasyon = e-posta doğrulaması |
| Kuruş (`AmountKurus`) | Tüm tutarların saklandığı birim (tam sayı) | Float yasak |

## Business rules

- **BR-1 — Roller:** Ürünü yalnızca Admin ekler ve ihaleye çıkarır; Personel görüntüler ve teklif verir.
- **BR-2 — Teklif geçerliliği:** İlk teklif ≥ başlangıç fiyatı; sonraki her teklif ≥ güncel en yüksek
  teklif + minimum artış. Teklif yalnızca ihale `Open` iken ve teklif veren aktif, e-postası
  doğrulanmış bir kullanıcıysa kabul edilir. Teklif geri çekilemez.
- **BR-3 — Eşzamanlılık:** Aynı anda gelen iki tekliften sunucunun önce kaydettiği geçerli sayılır;
  diğeri "fiyat değişti" gerekçesiyle reddedilir. Eşit en yüksek teklif oluşamaz.
- **BR-4 — Süre uzatma:** Bitişe 2 dakikadan az kala kabul edilen her teklif bitişi, teklif
  anından itibaren 2 dakika sonrasına öteler.
- **BR-5 — Kendi kendine teklif:** Güncel en yüksek teklifin sahibi yeni teklif veremez.
- **BR-6 — Kişi başı sınır:** Yoktur (MVP).
- **BR-7 — Kazanan vazgeçerse:** Admin devri başlatır; ürün sırayla 2. ve 3. en yüksek teklif
  sahibine teklif edilir — yalnızca farklı kişiler sayılır (aynı kişinin düşük teklifleri atlanır).
  Kişiye e-posta gider; 48 saat içinde sistemden kabul/ret verir, yanıtsızlık = ret. Tutar, o kişinin
  kendi en yüksek teklifidir. 3. kişi de almazsa daha alttaki tekliflere bakılmaz: ihale `Unsold`
  kapanır ve yeniden müzayede planlanır.
- **BR-8 — Teklifsiz ihale:** `Unsold` kapanır; Admin yeniden ihaleye açar ya da hurdaya ayırır.
- **BR-9 — Görünürlük:** Güncel en yüksek teklif ve teklif sayısı herkese görünür. Teklif sahipleri
  diğer kullanıcılara yalnızca ad ve soyad baş harfiyle ("F. E.") görünür; tam isim yalnızca
  Admin'e. Maskeleme sunucuda yapılır.
- **BR-10 — Para:** TL, tam sayı kuruş olarak saklanır; gösterim tr-TR (`₺1.250,00`).
- **BR-11 — Denetim:** Her teklif ve her Admin işlemi zaman damgasıyla, silinemez (append-only)
  kayıt altına alınır.
- **BR-12 — Kayıt:** Kayıt yalnızca izinli alanlardaki e-posta adresleriyle yapılır; e-posta
  doğrulanmadan teklif verilemez. İzinli alan listesini Admin yönetir (grup şirketleri eklenebilir).
  Listeden çıkarılan alan mevcut hesapları silmez; o alanla yeni kayıt ve yeniden aktivasyon yapılamaz.
- **BR-13 — Pasifleşme:** 90 gün giriş yapmayan kullanıcı (yerel ya da AD) otomatik pasif olur;
  yeniden aktivasyon kurumsal e-postaya gelen doğrulama linkiyle yapılır. Admin bir hesabı elle de
  pasifleştirebilir. Pasif kullanıcının mevcut teklifleri geçerli kalır, yeni teklif veremez;
  kazanırsa bildirimi gider ve BR-7 işler.
- **BR-14 — AD ile giriş:** AD/OIDC girişi opsiyoneldir (yapılandırmayla açılır). AD kullanıcısı da
  ilk girişte kısa kayıt adımından geçer (KVKK onayı, gerekirse sicil no). Aynı e-postalı yerel hesap
  varsa AD girişi o hesaba bağlanır; mükerrer hesap açılmaz.

## Key domain invariants

- Bir ihalenin en fazla bir kazananı vardır; kapanmış ihaleye teklif kabul edilmez.
- Kabul edilmiş bir teklif asla silinmez, değiştirilmez; denetim kaydı asla silinmez.
- En yüksek teklif zaman içinde yalnızca artar.
- Tutarlar hiçbir katmanda kayan noktalı sayı ile tutulmaz.
- Personel hiçbir API yanıtında başka bir personelin tam ismini almaz.

## Sensitive areas

- **Bidding & closing** — teklif kabulü (BR-2…BR-5), süre uzatma, ihale kapanışı, kazanan belirleme
  ve devir (BR-7). Hata = para ve adalet sorunu.
- **Identity & personal data (KVKK)** — kayıt, giriş, roller, pasifleşme, baş harf maskeleme
  (BR-9, BR-12…BR-14), kişisel veri saklama.
