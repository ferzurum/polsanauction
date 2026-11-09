# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
-->

- **B-1** — Forbidden dependency 1 checks type usage, not package/project references: an unused `PackageReference` (e.g. EF Core) in a `*.Domain.csproj` passes until the first type is used — Source: spec 0001, review round 1 (`tests/PolsanAuction.ArchitectureTests/LayerDependencyTests.cs:9-25`) — Severity: Low
  — Recommendation: add a test that scans `*.Domain.csproj` `PackageReference`/`ProjectReference` items — Deferred by: ferzurum, 2026-11-02, harm is only delayed (first type use turns the test red); add with the first spec that gives a Domain layer real types
- **B-5** — Rol (`Admin`/`Bidder`) ve izinli alan seed'i `Database:MigrateOnStartup` bayrağına bağlı; migration bundle ile kurulan ortamda roller yok (kayıt 500), alan tablosu boş (tüm kayıtlar reddedilir); `App:WebBaseUrl` yoksa sessizce `http://localhost:5173`'e düşülüyor — Source: spec 0002, review round 1, F-7 (`IdentityModule.cs:108-139`, `AccountLinks.cs:8`) — Severity: Low
  — Recommendation: rolleri migration'da `HasData` ile seed et, alan seed'ini bayraktan bağımsız çalıştır, Development dışında `App:WebBaseUrl` yoksa açılışta hata ver — Deferred by: ferzurum, 2026-11-06, üretim migration stratejisiyle birlikte dağıtım spec'inde çözülür
- **B-6** — Başarılı girişte `ResetAccessFailedCountAsync` sonucu kontrol edilmiyor; aynı hesaba eşzamanlı bir hatalı giriş ya da çıkış `concurrency_stamp`'i değiştirirse sıfırlama sessizce düşer ve hesap 5 ardışık hatadan önce kilitlenebilir — Source: spec 0002, review round 3, F-1 (`AccountSignIn.cs:44`) — Severity: Info
  — Recommendation: sıfırlamayı da tek bir `ExecuteUpdateAsync` ile yap (`AccessFailedCount = 0`, `LockoutEnd` aynı kalır) ya da sonucu kontrol et — Deferred by: ferzurum, 2026-11-07, etki yalnızca kullanıcı aleyhine erken kilit ve pencere milisaniyeler; hassas alanda bir fix-3 turunu haklı çıkarmıyor; sayacı yeniden ele alan spec'le (0004/0005) birlikte
- **B-7** — R-9'un log kısmı (parola, bağlantı, ad ya da e-posta logda görünmez) hiçbir testle assert edilmiyor; yalnızca kod okumasıyla (`IdentityLog.cs`) doğrulandı — Source: spec 0002, VERIFY, F-6 (`specs/reviews/0002-verify.md`) — Severity: Info
  — Recommendation: kayıt/giriş/doğrulama akışında yakalanan log kayıtlarında parola, bağlantı, ad ve e-posta geçmediğini assert eden bir entegrasyon testi — Deferred by: ferzurum, 2026-11-09, kod okumasıyla doğrulandı; hassas alanda son fix turu AC açıklarına ayrıldı
