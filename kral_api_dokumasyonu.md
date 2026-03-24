# 🚀 AtlasWeb API Dokümantasyonu (Güncel ve Detaylı)

Bu dokümantasyon, AtlasWeb projesinin tüm API endpoint'lerini kapsamlı şekilde açıklar. Proje, .NET 9 tabanlı bir SaaS (Çok Kiracılı) Web API uygulamasıdır.

> [!IMPORTANT]
> - Tüm yetki gerektiren istekler için HTTP Headers'a `Authorization: Bearer <AccessToken>` eklenmelidir
> - Access Token süresi: **15 dakika**
> - Refresh Token süresi: **7 gün**
> - Rate Limit: Login endpoint'i için 5 dakikada maksimum 10 deneme

---

## 📊 Genel Bilgiler

### Teknik Altyapı
- **Framework:** .NET 9 Web API
- **Veritabanı:** PostgreSQL
- **ORM:** Entity Framework Core 9
- **Kimlik Doğrulama:** JWT + Refresh Token
- **Şifreleme:** BCrypt
- **Mimari:** Multi-Tenant (SaaS)

### Global HTTP Status Kodları
| Kod | Açıklama |
|-----|----------|
| 200 | Başarılı |
| 400 | Geçersiz istek (validasyon hatası) |
| 401 | Token yok/geçersiz/kullanıcı pasif |
| 403 | Yetki yok |
| 404 | Kayıt bulunamadı |
| 409 | Çakışma (stok kodu tekrarı vb.) |
| 429 | Çok fazla istek (Rate Limit) |
| 500 | Sunucu hatası |

---

## 🔐 1. Kimlik Doğrulama (Authentication)

### 1.1 Admin Kaydı
**Sadece mevcut Admin kullanıcılar yeni Admin kaydedebilir.**

```http
POST /api/Auth/register-admin
Authorization: Bearer <AdminToken>
Content-Type: application/json

{
  "ad": "Ali",
  "soyad": "Veli", 
  "ePosta": "ali@atlas.com",
  "sifre": "Gizli123!"
}
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Admin yetkili kullanıcı kaydı başarıyla oluşturulmuştur."
}
```

**Hata Yanıtları:**
- `400`: `"Bu e-posta adresi sistemde kayıtlıdır."`
- `401`: Yetkiniz yok
- `403`: Admin rolü gereklidir

---

### 1.2 Şirket Kullanıcısı Kaydı
**Sadece Admin'ler şirkete bağlı kullanıcı oluşturabilir.**

```http
POST /api/Auth/register-user
Authorization: Bearer <AdminToken>
Content-Type: application/json

{
  "musteriId": "SIRKET-GUID",
  "ad": "Ayşe",
  "soyad": "Yılmaz",
  "ePosta": "ayse@firma.com", 
  "sifre": "Gizli123!"
}
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Kullanıcı kaydı ilgili şirkete bağlı olarak başarıyla oluşturulmuştur."
}
```

**Hata Yanıtları:**
- `400`: `"Bu e-posta adresi sistemde kayıtlıdır."`
- `400`: `"Belirtilen Şirket/Müşteri ID bulunamadı."`

---

### 1.3 Giriş (Login)
**Rate Limit: 5 dakikada maksimum 10 deneme**

```http
POST /api/Auth/login
Content-Type: application/json

{
  "ePosta": "ali@atlas.com",
  "sifre": "Gizli123!"
}
```

**Başarılı Yanıt (200):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "uG1+Axyz..."
}
```

**Hata Yanıtları:**
- `401`: `"Belirtilen e-posta adresine ait kullanıcı kaydı bulunamadı."`
- `401`: `"Kullanıcı hesabı aktif değildir. Lütfen sistem yöneticinizle iletişime geçiniz."`
- `401`: `"Girdiğiniz şifre hatalıdır."`
- `429`: Rate limit aşıldı

---

### 1.4 Token Yenileme

```http
POST /api/Auth/refresh-token
Content-Type: application/json

{
  "accessToken": "SURESI_DOLMUS_TOKEN",
  "refreshToken": "GECERLI_REFRESH_TOKEN"
}
```

**Başarılı Yanıt (200):**
```json
{
  "accessToken": "YENI_ACCESS_TOKEN",
  "refreshToken": "YENI_REFRESH_TOKEN"
}
```

**Hata Yanıtları:**
- `400`: `"Belirtilen kullanıcı kaydı bulunamadı."`
- `401`: `"Kullanıcı hesabı aktif değildir..."`
- `400`: `"Oturum bilgileri doğrulanamadı..."`
- `400`: `"Oturum süreniz sona ermiştir..."`

---

### 1.5 Çıkış (Logout)

```http
POST /api/Auth/logout
Authorization: Bearer <AccessToken>
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Güvenli çıkış işlemi başarıyla gerçekleştirildi."
}
```

---

## 👥 2. Müşteri (Şirket) Yönetimi

### 2.1 Aktif Müşterileri Listele
**Sadece giriş yapan kullanıcının şirketine ait müşteriler**

```http
GET /api/Musteri
Authorization: Bearer <AccessToken>
```

**Başarılı Yanıt (200):**
```json
[
  {
    "id": "GUID",
    "musteriKodu": "M0001",
    "unvan": "ABC Teknoloji A.Ş.",
    "vergiNo": "1234567890",
    "kimlikTuru": "VKN",
    "paketTipi": "Premium",
    "aktifMi": true,
    "kayitTarihi": "2026-03-23T10:30:00Z"
  }
]
```

---

### 2.2 Tüm Müşteriler (Silinenler Dahil) - Admin Only

```http
GET /api/Musteri/silinenler-dahil
Authorization: Bearer <AdminToken>
```

---

### 2.3 Müşteri Sil (Soft Delete)

```http
DELETE /api/Musteri/{id}
Authorization: Bearer <AccessToken>
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Müşteri kaydı başarıyla silinmiş (pasife alınmış) olarak işaretlendi."
}
```

---

### 2.4 Müşteri Kalıcı Sil (Hard Delete) - Admin Only

```http
DELETE /api/Musteri/{id}/hard
Authorization: Bearer <AdminToken>
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Müşteri kaydı veritabanından kalıcı olarak (Hard Delete) silindi."
}
```

---

## 👑 3. Admin Paneli

### 3.1 Kullanıcı Aktivite Logları

```http
GET /api/Admin/aktiviteler
Authorization: Bearer <AdminToken>
```

**Başarılı Yanıt (200):**
```json
[
  {
    "id": "GUID",
    "entityName": "Auth",
    "entityId": "USER_GUID",
    "action": "Login",
    "userId": "USER_GUID",
    "timestamp": "2026-03-23T14:30:00Z",
    "changes": null
  }
]
```

---

### 3.2 Sistem Hata Logları

```http
GET /api/Admin/sistem-hatalari
Authorization: Bearer <AdminToken>
```

**Başarılı Yanıt (200):**
```json
[
  {
    "id": 1,
    "hataMesaji": "Object reference not set to an instance of an object.",
    "hataDetayi": "Stack Trace...",
    "istekYolu": "/api/Auth/login",
    "kullaniciId": "USER_GUID",
    "tarih": "2026-03-23T15:45:00Z"
  }
]
```

---

## 📦 4. Ölçü Birimleri (Birim)

### 4.1 Birimleri Listele (Sadece aktif olanlar)

```http
GET /api/Birim
Authorization: Bearer <AccessToken>
```

**Başarılı Yanıt (200):**
```json
[
  {
    "id": "GUID",
    "ad": "Kilogram",
    "sembol": "Kg",
    "aktifMi": true
  },
  {
    "id": "GUID", 
    "ad": "Adet",
    "sembol": "Ad",
    "aktifMi": true
  }
]
```

---

### 4.2 Yeni Birim Ekle - Admin Only

```http
POST /api/Birim
Authorization: Bearer <AdminToken>
Content-Type: application/json

{
  "ad": "Metre",
  "sembol": "m"
}
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Ölçü birimi başarıyla eklendi."
}
```

---

### 4.3 Birim Sil (Soft Delete) - Admin Only

```http
DELETE /api/Birim/{id}
Authorization: Bearer <AdminToken>
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Ölçü birimi sistemden başarıyla silindi (pasife çekildi)."
}
```

---

## 🏷️ 5. Stok Yönetimi

### 5.1 Stokları Listele (Sayfalama destekli)
**Sadece giriş yapan kullanıcının şirketine ait aktif stoklar**

```http
GET /api/Stok?sayfa=1&sayfaBoyutu=20
Authorization: Bearer <AccessToken>
```

**Query Parametreleri:**
- `sayfa`: Sayfa numarası (default: 1)
- `sayfaBoyutu`: Sayfa boyutu (default: 20, max: 100)

**Başarılı Yanıt (200):**
```json
{
  "veriler": [
    {
      "id": "GUID",
      "stokKodu": "00001",
      "stokAdi": "Gaming Kasa",
      "yedekAdi": "PC Kasa Siyah",
      "birimAdi": "Adet",
      "birimSembolu": "Ad",
      "aktifMi": true,
      "kayitTarihi": "2026-03-23T10:00:00Z"
    }
  ],
  "toplamKayit": 42,
  "mevcutSayfa": 1,
  "sayfaBoyutu": 20,
  "toplamSayfa": 3
}
```

---

### 5.2 Yeni Stok Kartı Aç
**Stok kodu sistem tarafından otomatik üretilir (00001, 00002...)**

```http
POST /api/Stok
Authorization: Bearer <AccessToken>
Content-Type: application/json

{
  "stokAdi": "Gaming Kasa",
  "yedekAdi": "PC Kasa Siyah RGB",
  "birimId": "BIRIM-GUID"
}
```

**Başarılı Yanıt (200):**
```json
{
  "mesaj": "Stok kartı başarıyla açıldı.",
  "stokKodu": "00001"
}
```

**Hata Yanıtları:**
- `409`: `"Stok kodu üretiminde çakışma meydana geldi. Lütfen tekrar deneyiniz."`

---

## 🔒 Güvenlik Özellikleri

### Rate Limiting
- **Login endpoint:** 5 dakikada maksimum 10 deneme
- **Adaptive Throttling:** Başarısız denemelerde artan gecikme

### Token Güvenliği
- Access Token: 15 dakika
- Refresh Token: 7 gün (BCrypt hashlenmiş)
- JWT Key: Environment Variable'da saklanmalı

### Veri İzolasyonu
- Multi-Tenant mimari
- Her kullanıcı sadece kendi şirket verisini görebilir
- Global filtreleme ile veri güvenliği

### Audit Logging
- Tüm kritik işlemler loglanır
- Login/Logout aktiviteleri takip edilir
- Sistem hataları detaylı kaydedilir

---

## 🚀 Deployment Bilgileri

### Desteklenen Frontend Portları (CORS)
| Port | Framework |
|------|-----------|
| localhost:3000 | React/Next.js |
| localhost:5173 | Vite |
| localhost:4200 | Angular |
| 127.0.0.1:5500 | VS Code Live Server |

### Environment Variables
```bash
JWT_KEY=your-secret-key-here
DB_CONNECTION_STRING=your-postgres-connection
```

---

## 📝 Örnek Kullanım Senaryoları

### Senaryo 1: Yeni Şirket Kullanıcısı Oluşturma
1. Admin giriş yapar
2. `POST /api/Auth/register-user` ile yeni kullanıcı oluşturur
3. Yeni kullanıcı kendi hesabıyla giriş yapar

### Senaryo 2: Stok Yönetimi
1. Kullanıcı giriş yapar
2. `GET /api/Birim` ile ölçü birimlerini listeler
3. `POST /api/Stok` ile yeni stok kartı açar
4. `GET /api/Stok` ile stoklarını listeler

### Senaryo 3: Admin Monitoring
1. Admin giriş yapar
2. `GET /api/Admin/aktiviteler` ile kullanıcı aktivitelerini izler
3. `GET /api/Admin/sistem-hatalari` ile sistem hatalarını kontrol eder

---

## 🐛 Hata Ayıklama İpuçları

### Common Issues
1. **401 Unauthorized**: Token geçersiz veya süresi dolmuş
2. **403 Forbidden**: Yetersiz yetki
3. **409 Conflict**: Stok kodu çakışması - tekrar deneyin
4. **429 Too Many Requests**: Rate limit aşıldı - bekleyin

### Debug Steps
1. Token'ın geçerliliğini kontrol edin
2. Kullanıcı rolünü doğrulayın
3. Request body formatını kontrol edin
4. Rate limit durumunu gözlemleyin

---

*Dokümantasyon son güncelleme: 24.03.2026*
