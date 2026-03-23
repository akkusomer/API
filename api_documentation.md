# AtlasWeb API Dokümantasyonu (UI Ekibi İçin)

Bu dokümantasyon, UI/Front-end ekibinin AtlasWeb API'sini entegre edebilmesi için hazırlanmıştır.

> [!NOTE]
> Tüm yetki gerektiren istekler, HTTP Headers'a `Authorization: Bearer <AccessToken>` eklenerek gönderilmelidir.
> HTTP **401** = Token geçersiz/süresi dolmuş | **429** = Çok fazla istek gönderildi (Bloke edildiniz) | **409** = Çakışma (Stok kodu tekrar deneyin)

---

## 🔐 1. Kimlik Doğrulama (Auth)

### 1.1 Admin Kaydı
**Sadece mevcut Admin kullanıcılar çağırabilir.**

- **URL:** `POST /api/Auth/register-admin`
- **Auth:** `[Authorize(Roles = "Admin")]`
- **Body:**
```json
{ "ad": "Ali", "soyad": "Veli", "ePosta": "ali@atlas.com", "sifre": "Gizli123!" }
```
- **Başarılı (200):** `{ "mesaj": "Admin yetkili kullanıcı kaydı başarıyla oluşturulmuştur." }`

---

### 1.2 Şirket Kullanıcısı Kaydı
Bir şirkete bağlı (`musteriId` gerekli) kullanıcı oluşturur. **Sadece Admin'ler çağırabilir.**

- **URL:** `POST /api/Auth/register-user`
- **Auth:** `[Authorize(Roles = "Admin")]`
- **Body:**
```json
{ "musteriId": "SIRKET-GUID", "ad": "Ayşe", "soyad": "Yılmaz", "ePosta": "ayse@firma.com", "sifre": "Gizli123!" }
```
- **Başarılı (200):** `{ "mesaj": "Kullanıcı kaydı ilgili şirkete bağlı olarak başarıyla oluşturulmuştur." }`

---

### 1.3 Giriş (Login)
Rate Limit: 5 dakikada maksimum 10 deneme.

- **URL:** `POST /api/Auth/login`
- **Auth:** Hayır
- **Body:**
```json
{ "ePosta": "ali@atlas.com", "sifre": "Gizli123!" }
```
- **Başarılı (200):**
```json
{ "accessToken": "eyJhbGciOiJIUz...", "refreshToken": "uG1+Axyz..." }
```
- **Hata (401):** `{ "hata": "Girdiğiniz şifre hatalıdır." }` / `{ "hata": "Kullanıcı hesabı aktif değildir..." }`

---

### 1.4 Token Yenileme

- **URL:** `POST /api/Auth/refresh-token`
- **Body:**
```json
{ "accessToken": "SURESI_DOLMUS_TOKEN", "refreshToken": "GECERLI_REFRESH_TOKEN" }
```
- **Başarılı (200):** Yeni `accessToken` + `refreshToken` döner.

---

### 1.5 Çıkış (Logout)

- **URL:** `POST /api/Auth/logout`
- **Auth:** Evet
- **Başarılı (200):** `{ "mesaj": "Güvenli çıkış işlemi başarıyla gerçekleştirildi." }`

---

## 👥 2. Müşteri (Şirket)

### 2.1 Aktif Müşterileri Listele

- **URL:** `GET /api/Musteri`
- **Auth:** Evet
- **Dönen Alan:** `id, musteriKodu, unvan, vergiNo, kimlikTuru (VKN|TCKN), paketTipi, aktifMi`

### 2.2 Tüm Müşteriler (Silinenler Dahil) — *Admin*

- **URL:** `GET /api/Musteri/silinenler-dahil`
- **Auth:** `[Admin]`

### 2.3 Müşteri Sil (Soft Delete)

- **URL:** `DELETE /api/Musteri/{id}`
- **Auth:** Evet
- **Başarılı (200):** `{ "mesaj": "Müşteri kaydı... işaretlendi." }`

### 2.4 Müşteri Kalıcı Sil (Hard Delete) — *Admin*

- **URL:** `DELETE /api/Musteri/{id}/hard`
- **Auth:** `[Admin]`
- **Başarılı (200):** `{ "mesaj": "Müşteri kaydı veritabanından kalıcı olarak (Hard Delete) silindi." }`

---

## 👑 3. Admin Paneli

### 3.1 Kullanıcı Aktivite Logları

- **URL:** `GET /api/Admin/aktiviteler`
- **Auth:** `[Admin]`
- **Başarılı (200):**
```json
[{ "id": "...", "action": "Login", "userId": "...", "timestamp": "2026-03-23T..." }]
```

### 3.2 Sistem Hata Logları

- **URL:** `GET /api/Admin/sistem-hatalari`
- **Auth:** `[Admin]`
- **Başarılı (200):**
```json
[{ "id": 1, "hataMesaji": "...", "hataDetayi": "Stack Trace...", "istekYolu": "/api/Auth/login", "kullaniciId": "...", "tarih": "..." }]
```

---

## 📦 4. Ölçü Birimleri (Birim)

### 4.1 Birimleri Listele (Sadece aktif olanlar)

- **URL:** `GET /api/Birim`
- **Auth:** Evet
- **Başarılı (200):**
```json
[{ "id": "...", "ad": "Kilogram", "sembol": "Kg", "aktifMi": true }]
```

### 4.2 Yeni Birim Ekle — *Admin*

- **URL:** `POST /api/Birim`
- **Auth:** `[Admin]`
- **Body:** `{ "ad": "Adet", "sembol": "Ad" }`
- **Başarılı (200):** `{ "mesaj": "Ölçü birimi başarıyla eklendi." }`

### 4.3 Birim Sil (Soft Delete) — *Admin*

- **URL:** `DELETE /api/Birim/{id}`
- **Auth:** `[Admin]`
- **Başarılı (200):** `{ "mesaj": "Ölçü birimi sistemden başarıyla silindi (pasife çekildi)." }`

---

## 🏷️ 5. Stok Yönetimi

### 5.1 Stokları Listele (Sayfalama destekli)
Sadece giriş yapan kullanıcının şirketine ait aktif stokları döner.

- **URL:** `GET /api/Stok?sayfa=1&sayfaBoyutu=20`
- **Auth:** Evet
- **Query Params:** `sayfa` (default: 1), `sayfaBoyutu` (default: 20, max: 100)
- **Başarılı (200):**
```json
{
  "veriler": [
    { "id": "...", "stokKodu": "00002", "stokAdi": "Gaming Kasa", "yedekAdi": "PC Kasa", "birimAdi": "Adet", "birimSembolu": "Ad" }
  ],
  "toplamKayit": 42,
  "mevcutSayfa": 1,
  "sayfaBoyutu": 20,
  "toplamSayfa": 3
}
```

### 5.2 Yeni Stok Kartı Aç
Stok kodu (`00001` vb.) sistem tarafından otomatik üretilir.

- **URL:** `POST /api/Stok`
- **Auth:** Evet
- **Body:**
```json
{ "stokAdi": "Gaming Kasa", "yedekAdi": "PC Kasa Siyah", "birimId": "BIRIM-GUID" }
```
- **Başarılı (200):** `{ "mesaj": "Stok kartı başarıyla açıldı.", "stokKodu": "00001" }`
- **Hata (409):** `{ "hata": "Stok kodu üretiminde çakışma meydana geldi. Lütfen tekrar deneyiniz." }`

---

## 🛡️ Global Hata Yanıtları

| HTTP Status | Açıklama |
|-------------|----------|
| 400 | Geçersiz istek (validasyon hatası, eksik alan vb.) |
| 401 | Token yok / geçersiz / kullanıcı pasif |
| 403 | Yetki yok (Güvenlik ihlali) |
| 409 | Çakışma (Stok kodu çakışması gibi) |
| 429 | Çok fazla istek gönderildi (Rate Limit) |
| 500 | Sunucu hatası |

**Tüm hata yanıtları JSON formatındadır:**
```json
{ "hata": "Türkçe açıklama mesajı." }
```
