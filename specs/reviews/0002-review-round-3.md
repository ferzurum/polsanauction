# Review — spec 0002, round 3

- Spec: `specs/active/0002-identity-signup-login.md` · Plan: `specs/plans/0002-plan.md` (A-1, A-2, A-3)
- Diff: fix-2, `bfbdb84..f5ba4ef` on `feature/0002-identity-signup-login` (6 dosya)
- Önceki turlar: `specs/reviews/0002-review-round-1.md`, `specs/reviews/0002-review-round-2.md`
- Reviewer: bağımsız read-only subagent, 2026-11-07
- Rapor aşağıda değiştirilmeden yer alıyor.

## Triage (human)

Karar: real (fix) / noise (gerekçe yaz) / defer (`specs/backlog.md` kaydı) / investigate (→ QA, R-05).

| Finding | Severity | Decision | Note / Amendment |
|---|---|---|---|
| F-1 | Info | defer | `specs/backlog.md` B-6; fix-3 açılmaz (hassas alanda son round hakkı korunur) |

Next: VERIFY.

Triaged by: ferzurum · on: 2026-11-07

---

## Report

Review round 3 · fix rounds so far: 2

**Kapsam:** fix-2 diff'i: `git -c safe.directory='*' diff bfbdb84..HEAD`. Commit'ler 167e6c0, f6c4f58 ve f5ba4ef; üçü de `[plan 0002/fix-2]` etiketli. Diff 6 dosyayı değiştiriyor:
- `AccountSignIn.cs`
- `RoleTests.cs`
- `docs/security.md`
- `docs/conventions.md`
- `specs/plans/0002-plan.md`
- `specs/reviews/0002-review-round-2.md`

Esas aldığım belgeler: spec 0002, plan 0002 (A-1, A-2, A-3) ve `specs/reviews/0002-review-round-2.md` içindeki round-2 triage'ı. Dal origin ile senkron: `origin/feature/0002-identity-signup-login` = `f5ba4ef` = HEAD. Çalışma ağacı temiz.

**CHECK:** Developer'ın HEAD `f5ba4ef` için gösterdiği full GREEN'i alıntılıyorum: 11 adım, 137 sn; 76 .NET fast, 46 entegrasyon ve 39 web testi. `docs/testing.md` Two speeds kural 6 gereği yeniden koşmadım. Entegrasyon testi sayısı 45'ten 46'ya çıktı; bu, eklenen tek yeni testle tutarlı.

**CI:** Doğrulayamadım. Önceki turlarda `gh` 404 döndü; bu turda da bir CI koşusu göremedim. Triage, HEAD için CI full koşusunu görmeden kapanmamalı.

### Round-2 bulgularının durumu

| R2 | Triage | Durum | Kanıt |
|---|---|---|---|
| F-1 | real | Düzeltildi | `AccountSignIn.cs:60-72`: `UpdateSecurityStampAsync` artık yok. `security_stamp` ve `concurrency_stamp` tek bir koşulsuz `ExecuteUpdateAsync` ile yenileniyor. İyimser eşzamanlılık kontrolü olmadığı için paralel bir hatalı giriş yenilemeyi düşüremez. Paralel çalışan izlenen (tracked) bir yazma güvenlik damgasını da ezemez: değişen `concurrency_stamp` o yazmayı başarısız kılar. Oturumsuz çağrıda `GetUserId` null döner, `TryParse` false olur ve güncelleme atlanır; eski davranışla aynı. Triage'ın istediği Known traps satırı `docs/conventions.md:63`'te güncellenmiş. Regresyon testi `SessionTests.Logout_InvalidatesACopiedSessionCookie_AC8` değişmedi ve yeşil. |
| F-2 | real | Düzeltildi | `docs/security.md:41-46` artık şunları doğru anlatıyor: çıkış hesabın tüm oturumlarını bitirir (bilinçli kabul olarak işaretli); rol değişikliği damgayı yenilemez, rol en geç 1 dakikada oturuma yansır; pasifleştirme ve rol geri alma damgayı açıkça yenilemeli. Yeni test: `RoleTests.RoleGrantedDuringASession_ReachesItWithinTheCheckInterval_WithoutSigningInAgain_AC9` (`RoleTests.cs:98-119`). Bu test, round 2'de "çalıştırarak doğrulamadım" dediğim çıkarımı artık yürütülebilir kanıta çeviriyor. Kriter ↔ test haritasının AC-9 satırı güncellenmiş (`0002-plan.md:75`). |
| F-3 | real (yalnızca not) | Henüz uygulanacak bir yer yok | Triage kod değişikliği istemedi; diff'te de yok, bu doğru. Sıfırlama satırının (`docker compose down -v` ardından `docker compose up -d --wait`) VERIFY handoff'unda ve PR açıklamasında yer alıp almadığı ancak o aşamada denetlenebilir. SHIP'te DoD kontrolüne bir madde olarak eklenmeli. |

### Bulgular (önem sırasına göre)

#### F-1 — Info — Edge cases — `AccountSignIn.cs:44` başarılı girişte `ResetAccessFailedCountAsync` sonucunu kontrol etmiyor (fix-2'den önce de vardı, fix-2 kapsamı dışında)
- **Evidence:**
  - `src/Modules/Identity/PolsanAuction.Identity.Infrastructure/AccountSignIn.cs:44`: `await users.ResetAccessFailedCountAsync(user);` dönen `IdentityResult` yok sayılıyor.
  - Bu çağrı, sayaç 0 değilse `UpdateAsync` üzerinden `concurrency_stamp` kontrolüyle yazıyor. `concurrency_stamp`'i değiştiren iki atomik yol var: `RecordFailureAsync` (`:106`) ve fix-2 ile eklenen çıkış (`:68`).
  - `user` satır 22'de okunuyor. Satır 22 ile 44 arasında aynı hesaba paralel bir hatalı giriş ya da başka bir cihazdan çıkış gelirse sıfırlama sessizce `ConcurrencyFailure` ile düşer.
  - Sonuç: önceki ardışık hataların sayacı kalır ve sonraki hatalarda hesap 5 ardışık hatadan önce kilitlenebilir. AC-7'nin "başarılı giriş sayacı sıfırlar" kısmına dokunuyor.
  - Branch'in kendi Known traps satırı (`docs/conventions.md:63`) tam bu kalıbı tarif ediyor.
  - Çalıştırarak göstermedim; çıkarım Identity'nin `UserManager.ResetAccessFailedCountAsync` → `UpdateUserAsync` davranışına dayanıyor.
- **Impact:** Pencere çok dar: aynı hesaba milisaniyeler içinde eşzamanlı istek gerekiyor. Etki de güvenlik açığı yönünde değil, kullanıcı aleyhine erken kilit yönünde. Fix-2 bunu üretmedi; sayacı değiştiren yollara çıkışı ekleyerek pencereyi yalnızca marjinal biçimde genişletti.
- **Recommended action:** Önerim `specs/backlog.md`'ye **defer**. Gerekçe: Low eşiğinin altında, kimlik güvenliğini zayıflatmıyor ve bir fix-3 turunu haklı çıkarmıyor. Düzeltme ileride yapılırsa yol şu: sıfırlamayı da tek bir `ExecuteUpdateAsync` ile yap (`AccessFailedCount = 0`; `LockoutEnd` aynı kalır) ya da sonucu kontrol et. Ekip "noise" derse bu da savunulabilir.

### Boyutlar
- **Correctness:** Clean.
  - AC-8: çıkış artık yarışa dayanıklı biçimde damgayı yeniliyor. Kopyalanmış çerez testi değişmeden yeşil.
  - AC-9: yeni test, oturum sırasında verilen rolün 1 dakika içinde yeniden giriş istemeden yetkiye yansıdığını kanıtlıyor.
  - fix-2'de UI değişikliği yok; AC-10 durumu round 2'deki gibi.
- **Plan conformance:** Clean.
  - `AccountSignIn.cs`: A-2(b).
  - `RoleTests.cs`: planın test listesi (`0002-plan.md:31`).
  - `docs/security.md` ve `docs/conventions.md`: planın doküman listesi (`:41`). Known traps değişikliği round-2 triage'ında onaylı.
  - `specs/reviews/0002-review-round-2.md`: A-2(c).
  - Plan haritası güncellemesi: A-2(d).
  - Plan dışı dosya yok.
- **Security:** Clean.
  - Yeni damga `Guid.NewGuid()` ile üretiliyor: v4, 122 bit rastgele. Doğrulayıcı yalnızca eşitlik karşılaştırdığı için Identity'nin kendi biçiminden farklı olması sorun değil.
  - Damgayı kullanan doğrulama token'ları (A-1) yalnızca doğrulanmamış hesapları ilgilendiriyor. Çıkış yapabilen hesap zaten doğrulanmış, dolayısıyla etkilenmiyor.
  - Kimlik hesaptaki principal'dan alınıyor; istekten gelen bir id kullanılmıyor, bu yüzden başka bir hesabın damgası yenilenemez.
  - Yeni log, sır veya veri sızıntısı yok.
- **Edge cases:** F-1 (Info, fix-2 kapsamı dışında). Ek olarak:
  - Oturumsuz çağrı ve id'nin çözülemediği durum güvenli; güncelleme atlanıyor.
  - Silinmiş bir hesapta güncelleme 0 satır etkiliyor ve çıkış yine çalışıyor.
- **Performance:** Clean. Çıkışta artık bir `SELECT` ve bir `UPDATE` yerine tek bir `UPDATE` çalışıyor.
- **Tests:** Clean. Yeni test davranışı assert ediyor; yalnızca durum koduna bakmıyor:
  - Aralık içinde 403, aralık sonrasında 200 bekliyor. Yetki çerezdeki principal'dan geldiği için 200, principal'ın gerçekten yenilendiğini kanıtlıyor. Principal yenilenmeseydi ikinci assert kırmızı olurdu.
  - Saat `FakeTimeProvider` ile ilerletiliyor, gerçek beklemeye dayanmıyor; bu yüzden test deterministik.
  - `/me` rol assert'i DB'den okuduğu için tek başına kanıt değil, ancak ek doğrulama olarak zararsız.
  - R2 F-1 için yeni test yok. Round 2'de testi zorunlu tutmadım ve triage da istemedi; bu kabul edilebilir.
  - Kriter ↔ test haritası tam.
- **Maintainability:** Clean.
  - `ExecuteUpdateAsync` Infrastructure katmanında, katman ihlali yok.
  - Doc-comment değişikliğin gerekçesini round numarasıyla birlikte veriyor.
  - Dead code yok.
  - Yalnızca bir üslup notu, bulgu değil: `docs/conventions.md:63`'teki "sonuç kontrol edilmezse paralel isteklerde kilit sayacı kaybolur" ifadesi artık damga yenilemesini de kapsıyor ama yalnızca sayacı anıyor. Anlam yine açık.

Önerilen yeni Known traps satırı yok. F-1'in tuzağı `docs/conventions.md:63`'te zaten yazılı.

fix-2, R2'deki üç "real" bulguyu triage'da kararlaştırıldığı gibi ele alıyor ve regresyon getirmiyor. Tek yeni bulgu Info düzeyinde ve fix-2'den önce de vardı. `workflows/feature-development.md` adım 8'e göre Medium veya üstü bulgu olmadığından yeni bir fix turu ve yeniden REVIEW gerekmiyor. F-1 için önerim defer.

Bu tek geçişlik (one-pass) bir review değil; QA tablosu yok.

Next: TRIAGE — human (real / noise / defer / investigate); then Developer fix rounds, then VERIFY
