# 🚀 AtlasWeb Projesi — Güncel Durum ve Geliştirme Raporu

**Son Güncelleme:** 23.03.2026

---

## 1. Sistem Altyapısı

Proje, Çok Kiracılı (SaaS / Multi-Tenant) bir .NET 9 Web API uygulamasıdır. PostgreSQL veritabanı ve Entity Framework Core 9 üzerinde çalışır.

- **Global Veri İzolasyonu:** `ITenantEntity` + `ApplyGlobalFilters` → Her kullanıcı yalnızca kendi şirket verisini görür.
- **Otomatik Denetim (Audit):** `SaveChangesAsync` override'ı ile kayıt/güncelleme/silme tarihleri ve kullanıcı bilgileri otomatik işlenir.
- **Yüksek Performanslı ID:** UUID V7 (zaman damgalı sıralı GUID) ile veritabanı indeks performansı optimize edilmiştir.

---

## 2. Güvenlik Katmanları

| Katman | Açıklama |
|--------|----------|
| **BCrypt** | Kullanıcı şifreleri kırılamaz hash algoritmasıyla saklanır |
| **JWT (15 dk)** | Kısa ömürlü Access Token |
| **Refresh Token (7 gün)** | BCrypt hashlenmiş, veritabanında saklı |
| **Rate Limiting** | Login endpoint'i 5 dk'da max 10 denemeyle sınırlı |
| **Adaptive Throttling** | Brute-force'a karşı katlanarak artan gecikme |
| **Circuit Breaker** | Dağıtık önbellekte tutulan kırılma deseni |
| **Exception Middleware** | Tüm hatalar veritabanına loglanır, kullanıcıya Türkçe yanıt |
| **register-admin Koruması** | `[Authorize(Roles="Admin")]` — Sadece mevcut Admin yeni admin oluşturabilir |

---

## 3. Geliştirilen Modüller

### Auth
- `register-admin` → Sadece adminin çağırabileceği admin kayıt rotası
- `register-user` → Şirket ID'si zorunlu kullanıcı kayıt rotası
- `login / logout / refresh-token` → JWT + Refresh Token akışı
- Tüm istekler **FluentValidation** doğrulamasından geçer

### Müşteri (Şirket)
- GET / Soft Delete / Hard Delete endpointleri mevcut
- Admin, silinenler dahil tüm kayıtları görebilir (`IgnoreQueryFilters`)

### Ölçü Birimleri (Birim)
- Global tablo (şirketten bağımsız), tüm firmalar paylaşır
- Sadece Admin ekleyip silebilir
- Soft Delete korumalı (`AktifMi` manuel filtreli)

### Stok Yönetimi
- Şirket bazlı izole tablo (`BaseEntity` miras)
- Otomatik sıralı stok kodu üretimi (`00001`, `00002` vb.)
- **Race Condition koruması:** `Serializable` transaction seviyesi
- **Sayfalama (Pagination):** `?sayfa=1&sayfaBoyutu=20` (max 100)
- Birim JOIN ile döner (BirimAdi, BirimSembolu)

### Admin Paneli
- `GET /api/Admin/aktiviteler` → Son 50 Login/Logout aktivitesi
- `GET /api/Admin/sistem-hatalari` → Son 100 sistem hatası (StackTrace dahil)

---

## 4. Teknik İyileştirmeler (Bu Oturumda Düzeltilen Sorunlar)

| # | Sorun | Durum |
|---|-------|-------|
| ~~1~~ | `register-admin` endpoint açıktı | ✅ Düzeltildi — `[Authorize(Roles="Admin")]` eklendi |
| ~~2~~ | Stok kodu Race Condition | ✅ Düzeltildi — `IsolationLevel.Serializable` transaction |
| ~~3~~ | JWT key `appsettings.json`'da düz metin | ✅ İyileştirildi — Env variable fallback eklendi |
| ~~4~~ | `Birimler` SoftDelete filtresiz | ✅ Düzeltildi — Manual `AktifMi` filtresi eklendi |
| ~~5~~ | Rate Limiter politika tanımsızdı | ✅ Düzeltildi — `AddRateLimiter("LoginPolicy")` eklendi |
| ~~6~~ | CORS yapılandırması yoktu | ✅ Düzeltildi — `AddCors("AtlasWebCors")` eklendi |
| ~~7~~ | `Fatura.cs` CS0108 uyarısı | ✅ Düzeltildi — Çakışan `int Id` alanı kaldırıldı |
| ~~8~~ | Pagination eksikti | ✅ Eklendi — Stok listesi sayfalama destekli |

---

## 5. CORS Desteklenen Frontend Portları

| Port | Framework |
|------|-----------|
| `localhost:3000` | React / Next.js |
| `localhost:5173` | Vite |
| `localhost:4200` | Angular |
| `127.0.0.1:5500` | VS Code Live Server |

> Prodüksiyon ortamında `WithOrigins(...)` listesi gerçek domain adresiyle güncellenmelidir.

---

## 6. Önerilen Sonraki Adımlar

1. `appsettings.json` içindeki JWT key ve DB şifresi ortam değişkenlerine (`Environment Variables`) taşınmalı, dosya `.gitignore`'a eklenmeli
2. EF Core Migration stratejisi netleştirilmeli
3. `AuthController` içindeki iş mantığı bir `AuthService`'e taşınması önerilir (Clean Architecture)
