# Finstance — proje mantığı ve UI

## Veri akışı

1. `POST /upload`, `file` adlı multipart alanında en fazla 20 MB PDF alır.
2. `StatementService`, belgedeki banka adına göre Yapı Kredi veya QNB parser'ını seçer.
3. Parser kesim tarihini, işlem tarihini, iş yeri metnini, tutarı ve taksit işaretini çıkarır.
4. `DataService`, kullanıcı ve kesim tarihine göre tekrar kontrolü yapar. Kayıtlar tek veritabanı transaction'ı içinde saklanır.
5. `LocationPipeline` önce metin, sonra bulanık eşleştirme ile mevcut iş yerini arar. Yeni iş yerlerinde `CategoryPipeline` anahtar kelimelerden kategori bulur; eşleşmeyenler `Diger` olur.
6. `GET /getMonthlyReport?date=YYYY-MM-01`, **ekstre kesim tarihinin ayına** göre kayıtları döndürür. Önceki ayda yapılan bir işlem de bu raporda bulunabilir.

## Next.js arayüzü

- App Router ve TypeScript; `src/components/dashboard.tsx` etkileşimli paneli sağlar.
- `src/lib/report.ts`: saf toplama, kategori, Türkçe arama, tarih ve CSV işlevleri. Pozitif harcamalar özetlenir; negatif işlemler ayrıca gösterilir.
- `/api/report` ve `/api/upload` aynı origin üzerinden .NET API'ye gider. `FINSTANCE_API_URL` yalnızca sunucuda okunur; `X-User-Name` iletilir. Yanıtlar önbelleğe alınmaz.
- Grafik, özet ve filtreli liste aynı rapor verisini kullanır. Günlük grafik, işlem tarihlerini kullanır; rapordaki önceki ay işlemlerini dışlamaz. Veri bulunan tarihler eşit aralıklarla gösterilir.
- Ekstreler ekranı rapordaki `bankStatementId` alanından türetilir. Backend ayrı bir ekstre listesi sağlamadığından, işlemsiz ekstreler burada görünmez. Banka adı rapor DTO'sunda bulunmadığı için listede banka adı uydurulmaz.
- Örnek veri modu yalnızca istemcide oluşturulur ve açıkça etiketlenir; veritabanına yazılmaz. PDF yüklemek gerçek veriye geçiş yapar.
- Profil seçimi oturum boyunca tutulur; sayfa yenilenince `guest` kullanılır. Bu bir giriş/auth sistemi değildir.

## Geliştirme ortamı

Kök `.devcontainer/compose.yaml`, Node.js 24, .NET 10 API ve PostgreSQL 18 servislerini çalıştırır. UI kodu bind mount, Linux `node_modules` ve `.next` dosyaları ayrı named volume kullanır. Windows dosya değişimleri için Webpack polling açıktır.

Geliştirme veritabanı `finstance-dev` Compose projesinin ayrı volume'unda tutulur. PostgreSQL 18 için volume `/var/lib/postgresql` altına bağlanır. API, yalnızca `Development` ortamında ve `Database__Initialize=true` verilirse `EnsureCreatedAsync` ile boş veritabanını kurar ve iş yeri seed'lerini ekler. Bu migration mekanizması değildir; mevcut şema değişikliklerini uygulamaz.

## Mevcut backend sınırları

- `X-User-Name` güvenilir kimlik doğrulama sağlamaz. İnternete açılmadan önce gerçek auth ve kullanıcı yetkilendirmesi gerekir.
- Tekrar kontrolü banka ayrımı yapmadan kullanıcı + kesim tarihini kullanır. Aynı gün kesilen iki farklı banka ekstresi çakışabilir.
- Kullanıcı oluşturma ve tekrar kontrolü veritabanında unique constraint ile korunmuyor; eşzamanlı istekler için backend düzenlemesi gerekir.
- Parser'lar tarih biçimlerine ve PDF metin yerleşimine bağımlı. Taranmış görüntü PDF'leri için OCR yok.
- Taksit bilgisi kaydediliyor ama rapor DTO'sunda sunulmuyor; UI taksit bilgisi göstermez.
- Bankalara göre ödeme satırlarının işaretleri parser içinde farklı yorumlanıyor; mevcut parser davranışı korunmuştur.
- Depoda EF migration dosyası bulunmuyor. Üretim veritabanı için migration stratejisi ayrıca hazırlanmalıdır.

Framework kurulum referansı: [Next.js App Router](https://nextjs.org/docs/app/getting-started/installation).
