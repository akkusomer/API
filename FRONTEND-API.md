# AtlasWeb API — UI / Frontend entegrasyon rehberi

Bu doküman, web veya mobil arayüz geliştiren ekip için **AtlasWeb** backend API’sinin nasıl çağrılacağını özetler. Kaynak: mevcut controller ve DTO yapıları; üretim adresi ve ortam değişkenleri projeye göre değişir.

---

## 1. Temel bilgiler

| Öğe | Açıklama |
|-----|-----------|
| **Taban URL** | Örnek yerel: `https://localhost:7xxx` veya yayın URL’si (HTTPS tercih edilir). Tüm route’lar kökten itibaren birleştirilir. |
| **İçerik tipi** | İstek gövdeleri için `Content-Type: application/json` kullanın. |
| **Swagger** | Uygulama çalışırken genellikle kök dizinde: `{BASE_URL}/` (Swagger UI), şema: `{BASE_URL}/swagger/v1/swagger.json`. Canlı ortamda Swagger’ı kapatmak yaygın bir güvenlik tercihidir. |
| **CORS** | Sunucu şu an **tüm kökenlere** izin verecek şekilde yapılandırılmıştır (`AllowAnyOrigin`). Üretimde yalnızca kendi frontend alan adınızın listelenmesi önerilir (backend ekibi). |
| **Zaman** | API tarafında yaygın olarak **UTC** kullanılır; tarih alanlarını kullanıcıya gösterirken istemci saat dilimine çevirin. |

---

## 2. Kimlik doğrulama (JWT)

### 2.1 Genel akış

1. **`POST /api/Auth/login`** ile e-posta ve şifre gönderilir; cevapta **access token** ve **refresh token** alınır.
2. Korumalı tüm isteklerde header:

   ```http
   Authorization: Bearer <accessToken>
   ```

3. Access token süresi yaklaşık **8 saat** (sunucu yapılandırmasına bağlı).
4. Refresh token sunucuda saklanır; süresi yaklaşık **7 gün**. **`POST /api/Auth/refresh-token`** ile yeni çift token alınır.
5. **`POST /api/Auth/logout`** (Bearer gerekli) çağrıldığında sunucudaki refresh token temizlenir; istemcide de token’ları silin.

### 2.2 Giriş (login)

**İstek**

```http
POST /api/Auth/login
Content-Type: application/json
```

```json
{
  "ePosta": "kullanici@ornek.com",
  "sifre": "********"
}
```

**Başarılı yanıt — `200 OK`**

```json
{
  "accessToken": "<JWT>",
  "refreshToken": "<base64 benzeri dize>"
}
```

**Hata örnekleri**

| HTTP | Gövde (örnek) |
|------|----------------|
| `401 Unauthorized` | `{ "hata": "E-posta veya şifre hatalı." }` |
| `400` | FluentValidation hataları (aşağıda “Doğrulama hataları”) |
| `429 Too Many Requests` | Giriş uçunda oran sınırı: **5 dakikada en fazla 10** deneme (yapılandırmaya göre değişebilir). Gövde boş olabilir; `Retry-After` üzerinden anlaşma backend’e bağlı. |

### 2.3 Kayıt (register)

**İstek**

```http
POST /api/Auth/register
Content-Type: application/json
```

```json
{
  "musteriId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ad": "Ahmet",
  "soyad": "Yılmaz",
  "ePosta": "yeni@ornek.com",
  "sifre": "********"
}
```

- **`musteriId`**: Sistemde **var olan** bir müşteri (şirket / kiracı) kaydının `id` değeri olmalıdır. Bu API ile **yeni şirket oluşturulmaz**; şirket kaydı yoksa `400` döner: `"Geçersiz müşteri (şirket) bilgisi."`
- Oluşturulan kullanıcı rolü **User** olur.

**Başarılı yanıt — `200 OK`**

```json
{ "mesaj": "Kayıt tamamlandı." }
```

**Not:** İlk kurulumda admin kullanıcı veritabanı seed ile oluşabilir; son kullanıcı kaydı için UI’da “hangi `musteriId` ile kayıt” akışını ürün/operasyon ekibiyle netleştirin.

### 2.4 Token yenileme

**İstek**

```http
POST /api/Auth/refresh-token
Content-Type: application/json
```

```json
{
  "accessToken": "",
  "refreshToken": "<önceki refresh token>"
}
```

- Sunucu kodu **`refreshToken`** alanını kullanır; `accessToken` boş bırakılabilir.

**Başarılı — `200 OK`**

```json
{
  "accessToken": "<yeni JWT>",
  "refreshToken": "<yeni refresh>"
}
```

**Hata — `401`**

```json
{ "hata": "Geçersiz veya süresi dolmuş oturum." }
```

### 2.5 Çıkış

```http
POST /api/Auth/logout
Authorization: Bearer <accessToken>
```

**Başarılı — `200 OK`**

```json
{ "mesaj": "Çıkış yapıldı." }
```

### 2.6 JWT içindeki iddia (claim) bilgisi

Access token çözümlendiğinde (yalnızca doğrulama için; imza doğrulaması sunucuda yapılmalıdır) tipik iddialar:

| Claim | Anlamı |
|-------|--------|
| `sub` / `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | Kullanıcı `Guid` (string) |
| `email` | E-posta |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | `Admin` veya `User` |
| `MusteriId` | Bağlı olunan şirketin GUID’i; admin seed kullanıcıda `00000000-0000-0000-0000-000000000000` olabilir |

UI’da rol bazlı menü: rol claim değeri **`Admin`** / **`User`** ile kontrol edin.

### 2.7 Korumalı uçlarda yetkisiz erişim

Geçersiz veya eksik Bearer token durumunda ASP.NET Core genelde **`401 Unauthorized`** döner (gövde standart olmayabilir).

---

## 3. Kiracı (multi-tenant) ve roller

| Rol | Davranış özeti |
|-----|----------------|
| **Admin** | Tüm müşteri listesini görebilir; birim ekle/sil; müşteri kalıcı silme; pasif müşteriler dahil liste; admin panel uçları. |
| **User** | Yalnızca kendi **`MusteriId`** kapsamındaki veriler (filtrelenmiş listeler). Stok eklemek için token’da **`MusteriId` dolu ve sıfır GUID olmayan** olmalı. |

Stok oluşturma (`POST /api/Stok`) için token’da `MusteriId` yoksa **`401`**:

```json
{ "hata": "Stok açabilmek için öncelikle bir şirkete bağlı olmanız gerekmektedir (Müşteri ID bulunamadı)." }
```

---

## 4. Endpoint referansı

Aşağıda yollar **`{BASE_URL}`** ile birleştirilir (ör. `https://api.sirketiniz.com`).

### 4.1 Müşteri (`Musteri`)

Tüm uçlar: **`Authorization: Bearer`** gerekir.

| Metod | Yol | Yetki | Açıklama |
|-------|-----|--------|----------|
| `GET` | `/api/Musteri` | Giriş yapmış | **Admin:** tüm şirketler. **User:** yalnızca kendi şirket kaydı (çoğunlukla tek elemanlı dizi). |
| `GET` | `/api/Musteri/silinenler-dahil` | **Admin** | Global filtre yok; pasif kayıtlar dahil tüm müşteriler. |
| `DELETE` | `/api/Musteri/{id}` | Giriş yapmış | **Soft delete:** kayıt pasifleştirilir. |
| `DELETE` | `/api/Musteri/{id}/hard` | **Admin** | Kalıcı silme (geri alınamaz). |

**`GET /api/Musteri` yanıt gövdesi:** `Musteri` nesnelerinin dizisi (JSON property isimleri **camelCase**).

Örnek alanlar:

| Alan | Tip | Not |
|------|-----|-----|
| `id` | string (UUID) | |
| `musteriKodu` | string | |
| `unvan` | string | |
| `vergiNo`, `vergiDairesi` | string | |
| `kimlikTuru` | number | `0`: VKN, `1`: TCKN (varsayılan JSON enum serileştirme) |
| `gsmNo`, `ePosta` | string | |
| `il`, `ilce`, `adresDetay` | string | |
| `paketTipi` | number | `0`: Standart, `1`: Premium, `2`: Kurumsal |
| `aktifMi` | boolean | |
| `kayitTarihi` | string (ISO 8601) | |

**Soft delete başarı — `200 OK`**

```json
{ "mesaj": "Müşteri kaydı başarıyla silinmiş (pasife alınmış) olarak işaretlendi." }
```

**Bulunamadı — `404`**

```json
{ "hata": "Belirtilen müşteri bulunamadı." }
```

---

### 4.2 Ölçü birimi (`Birim`)

| Metod | Yol | Yetki | Açıklama |
|-------|-----|--------|----------|
| `GET` | `/api/Birim` | Giriş yapmış | Aktif birimler, `ad`’a göre sıralı. |
| `POST` | `/api/Birim` | **Admin** | Yeni birim. |
| `DELETE` | `/api/Birim/{id}` | **Admin** | Soft delete (pasifleştirme). |

**`POST /api/Birim` gövde**

```json
{
  "ad": "Kutu",
  "sembol": "KUT"
}
```

**Başarı — `200 OK`**

```json
{ "mesaj": "Ölçü birimi başarıyla eklendi." }
```

**Çakışma — `400`**

```json
{ "hata": "Bu ölçü birimi veya sembol zaten mevcut." }
```

**`GET` yanıtı:** `Birim` nesne listesi (ör. `id`, `ad`, `sembol`, `aktifMi`, `kayitTarihi`, audit alanları vb.).

---

### 4.3 Stok (`Stok`)

| Metod | Yol | Yetki | Açıklama |
|-------|-----|--------|----------|
| `GET` | `/api/Stok?sayfa=1&sayfaBoyutu=20` | Giriş yapmış | Sayfalı liste; sunucu `sayfaBoyutu` üst sınırını **100** ile sınırlar. |
| `POST` | `/api/Stok` | Giriş yapmış | Yeni stok; `MusteriId` token’da olmalı. |

**`GET /api/Stok` — başarı `200 OK`**

```json
{
  "veriler": [
    {
      "id": "...",
      "stokKodu": "00001",
      "stokAdi": "...",
      "yedekAdi": null,
      "birimAdi": "...",
      "birimSembolu": "..."
    }
  ],
  "toplamKayit": 42,
  "mevcutSayfa": 1,
  "sayfaBoyutu": 20,
  "toplamSayfa": 3
}
```

**`POST /api/Stok` gövde**

```json
{
  "stokAdi": "Ürün adı",
  "yedekAdi": "İsteğe bağlı",
  "birimId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

**Başarı — `200 OK`**

```json
{
  "mesaj": "Stok kartı başarıyla açıldı.",
  "stokKodu": "00042"
}
```

**Hatalar**

| Durum | Gövde |
|-------|--------|
| `401` | `MusteriId` yok / boş |
| `400` | `{ "hata": "Seçilen ölçü birimi sistemde bulunamadı." }` |
| `409 Conflict` | Eşzamanlı kod üretiminde çakışma: `{ "hata": "Stok kodu üretiminde çakışma meydana geldi. Lütfen tekrar deneyiniz." }` — istemci yeniden deneyebilir. |

---

### 4.4 Admin paneli (`Admin`)

Tüm uçlar: **`Authorization: Bearer`** ve rol **`Admin`** gerekir.

| Metod | Yol | Açıklama |
|-------|-----|----------|
| `GET` | `/api/Admin/aktiviteler` | Son 50 login/logout tipi denetim kaydı (özet proje). |
| `GET` | `/api/Admin/sistem-hatalari` | Son 100 sistem hata log kaydı. |

**`aktiviteler` örnek eleman:** `id`, `action`, `userId`, `timestamp`.

**`sistem-hatalari`:** `ErrorLog` model alanları (ör. `id`, `hataMesaji`, `hataDetayi`, `istekYolu`, `kullaniciId`, `tarih` — camelCase).

---

## 5. Hata biçimleri (özet)

API tek tip bir “RFC 7807” şeması kullanmaz; aşağıdaki kalıplar yaygındır:

| Durum | Örnek gövde |
|-------|-------------|
| İş kuralı / controller | `{ "hata": "Açıklayıcı mesaj" }` |
| Başarı mesajı | `{ "mesaj": "..." }` |
| Yetkisiz kiracı (middleware) | **403** `{ "hata": "Sisteme erişim yetkiniz bulunmamaktadır." }` |
| Beklenmeyen sunucu hatası | **500** `{ "hata": "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz." }` |
| FluentValidation (otomatik) | Genelde **400**; ASP.NET Core **`ValidationProblemDetails`**: `errors` sözlüğü, `title`, `status` vb. — UI’da alan bazlı göstermek için `errors` kullanılabilir. |

---

## 6. UI uygulama önerileri

1. **Token depolama:** XSS riskine göre `httpOnly` cookie veya bellek içi + kısa ömürlü access token tercihleri değerlendirilsin; refresh token’ı mümkünse güvenli saklama.
2. **401 / 403:** Oturumu sonlandırıp login sayfasına yönlendirme veya refresh denemesi (refresh de 401 verirse çıkış).
3. **429 (login):** Kullanıcıya “çok fazla deneme” mesajı; geri sayım veya bekleme.
4. **Çakışma (409 stok):** Aynı isteği güvenli şekilde **bir kez** yeniden deneme (idempotent değilse dikkat).
5. **Enum gösterimi:** API şu an enum’ları sayı olarak döndürebilir; etiketleri UI’da sabit bir eşleme tablosuyla gösterin (VKN/TCKN, paket tipi).
6. **OpenAPI:** Mümkünse `swagger.json` ile TypeScript client veya mock üretimi için CI’da senkron tutun.

---

## 7. Örnek: fetch ile login ve korumalı istek

```javascript
const BASE = 'https://api.ornek.com';

async function login(email, password) {
  const res = await fetch(`${BASE}/api/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ePosta: email, sifre: password }),
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json(); // { accessToken, refreshToken }
}

async function getStoklar(token, sayfa = 1, sayfaBoyutu = 20) {
  const q = new URLSearchParams({ sayfa: String(sayfa), sayfaBoyutu: String(sayfaBoyutu) });
  const res = await fetch(`${BASE}/api/Stok?${q}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

---

## 8. Sürüm ve iletişim

- API sürümü OpenAPI `info.version` alanından takip edilebilir (`v1`).
- Yeni alan veya kırıcı değişiklikler için backend ile **sözleşme (changelog)** üzerinden anlaşma önerilir.

---

*Bu dosya AtlasWeb repository’sindeki mevcut kodla uyumlu olacak şekilde hazırlanmıştır. ASP.NET projesi `AtlasWeb/AtlasWeb/` altındadır. Üretim URL’si, rate limit sayıları ve JWT süreleri sunucu yapılandırmasına göre güncellenebilir.*
