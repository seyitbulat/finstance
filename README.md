# Finstance

Kredi kartı PDF ekstrelerini kategorili harcama raporlarına dönüştüren .NET API ve Next.js arayüzü.

## Dev Container ile başlat

Gereksinimler: Docker Desktop (Linux containers), VS Code ve Dev Containers eklentisi.

1. Proje kökünü VS Code'da aç.
2. Komut paletinden **Dev Containers: Reopen in Container** seç.
3. İlk kurulumda bağımlılıklar kurulur; UI, API ve ayrı geliştirme veritabanı açılır.
4. [Arayüzü aç](http://localhost:3000).

İlk ekranda gerçek veriler gösterilir. Kayıt yoksa PDF yükleyebilir veya **Örnek verilerle keşfet** düğmesiyle tasarımı deneyebilirsin.

### Editör olmadan aynı geliştirme ortamı

Proje kökünde:

```powershell
docker compose -f .devcontainer/compose.yaml up -d --build
docker compose -f .devcontainer/compose.yaml exec -T ui npm ci
docker compose -f .devcontainer/compose.yaml exec -T ui bash /workspace/.devcontainer/start-ui.sh
```

Arayüz: http://localhost:3000 · API: http://localhost:5257

UI kaynak değişiklikleri otomatik yansır. API değişikliklerini uygulamak için:

```powershell
docker compose -f .devcontainer/compose.yaml up -d --build api
```

Servisleri durdurmak için (veritabanı korunur):

```powershell
docker compose -f .devcontainer/compose.yaml stop
```

UI logları: `docker compose -f .devcontainer/compose.yaml exec -T ui cat /tmp/finstance-ui.log`

## Arayüz

- Türkçe, masaüstü ve mobil uyumlu harcama paneli.
- Aylık toplam, işlem sayısı, işlem başına ortalama ve öne çıkan kategori.
- Günlük harcama grafiği ve kategori dağılımı.
- İş yeri arama, kategori filtresi, sıralama, sayfalama ve CSV dışa aktarma.
- Seçili ayın ekstre kartları.
- Sürükle-bırak veya dosya seçerek PDF yükleme; dosya sınırı, hata ve tekrar yükleme bildirimleri.
- API hatası, yükleniyor ve boş veri durumları. Örnek veriler gerçek kayıtlardan ayrı tutulur.
- Fontlar uygulamadan sunulur; tarayıcı Google Fonts'a istek göndermez.

## İş mantığı

**PDF → banka tespiti → işlem çıkarma → iş yeri eşleştirme → kategori → PostgreSQL → aylık rapor.**

Desteklenen bankalar: Yapı Kredi ve QNB. Aylık rapor **işlem ayına değil, ekstre kesim ayına** göre hazırlanır. Yerel profil `X-User-Name` başlığına karşılık gelir; bu kimlik doğrulama değildir. Varsayılan profil `guest`.

Detaylı mimari, veri akışı ve mevcut sınırlar: [docs/architecture.md](docs/architecture.md).

## Dizinler

- `api/`: .NET 10 Minimal API, EF Core, PdfPig, Tabula.
- `ui/finstance/`: Next.js App Router, React, TypeScript, CSS, Lucide.
- `.devcontainer/`: Node.js 24, API ve PostgreSQL 18 geliştirme ortamı.
- `ui/.devcontainer/`: yalnızca `ui` klasöründen açıldığında aynı ortamı kullanır.

## Doğrulama

Geliştirme konteynerinde:

```powershell
docker compose -f .devcontainer/compose.yaml exec -T ui npm run lint
docker compose -f .devcontainer/compose.yaml exec -T ui npm run typecheck
docker compose -f .devcontainer/compose.yaml exec -T ui npm test
docker compose -f .devcontainer/compose.yaml exec -T ui npm run build
```

Birim testleri; boş rapor, Türkçe arama, kategori filtreleri, yıl geçişi, ekstre ayındaki önceki ay işlemleri, iade/ödeme ayrımı ve CSV hücre kaçışını kapsar.

UI geliştirme sunucusu açıkken `docker compose -f .devcontainer/compose.yaml exec -T ui npm run test:integration` komutu yapay QNB PDF'iyle gerçek API ve veritabanını test eder. Test kayıtları benzersiz `ui-test-*` geliştirme profiline eklenir; `guest` verilerine dokunulmaz.

## API

| İşlem | Endpoint |
| --- | --- |
| PDF yükle | `POST /upload` — multipart `file`, en fazla 20 MB |
| Aylık rapor | `GET /getMonthlyReport?date=2026-09-01` |

UI bu uçlara `/api/upload` ve `/api/report?month=2026-09` üzerinden erişir. Sunucu ortam değişkeni `FINSTANCE_API_URL`; konteynerde `http://api:5257`, yerelde `http://localhost:5257`.

## Veritabanı ve üretim

Geliştirme ortamı kendi `finstance-dev` veritabanı volume'unu kullanır. `Database__Initialize=true` yalnızca Development ortamında boş veritabanını oluşturur ve iş yeri seed'lerini yükler. Şema değişikliklerini uygulayan migration dosyaları henüz yoktur.

Kökteki mevcut `docker-compose.yaml`, üretim tipi API/UI imajlarını derleyebilir. Gerçek kullanım öncesinde veritabanı şeması, kimlik doğrulama, gizli bilgiler ve PostgreSQL volume geçişi ayrıca hazırlanmalıdır. UI için çok aşamalı `ui/finstance/Dockerfile` standalone Next.js çıktısı üretir.
