# Doğrulama notları

## Tamamlanan kontroller

- Node.js 24 Dev Container içinde bağımlılık kurulumu; lock dosyasıyla tekrar üretilebilir kurulum.
- `npm run lint` ve `npm run typecheck`: başarılı.
- `npm test`: 6 test başarılı.
- `npm run test:integration`: gerçek geliştirme API'sine yapay QNB PDF yükleme, PostgreSQL kaydı, önceki ay işlem tarihinin kesim ayı raporunda bulunması, tekrar yüklemede 409, geçersiz ay/dosya ve profil ayrımı başarılı.
- .NET API Docker derlemesi ve boş geliştirme veritabanı kurulumu başarılı. Mevcut backend nullable uyarıları devam ediyor.
- `docker build -t finstance-ui:local ui/finstance`: standalone üretim imajı başarılı.
- Tarayıcı: örnek/gerçek veri ayrımı, boş durum, Türkçe iş yeri araması, kategori filtresi, sayfalama, ekstre kartları, ay geçişi ve yükleme penceresi kontrol edildi. Konsol hata kaydı yok.
- 390 px mobil görünüm: sayfa yatay taşmıyor; işlem tutarı, iş yeri, tarih ve kategori kaydırmadan görülebiliyor. Mobil profil düğmesi ve yükleme penceresi çalışıyor.

## Görsel değerlendirme

Refactoring UI hızlı değerlendirmesi: **9/10 (7/8 ölçüt)**. Başlık/değer hiyerarşisi, renkten bağımsız okunabilirlik, boşluklar, ikincil etiketler, metin genişliği, ölçekli yerleşim ve gölge kullanımı kontrol edildi. Tam kontrast uygunluğu doğrulanmadı; 10/10 için küçük yardımcı metinler ve renkli iş yeri harfleri üzerinde bütün renk çiftlerinin ölçülüp gerekirse koyulaştırılması gerekir. Bu değerlendirme bir WCAG sertifikası değildir.

## Sınırlar

PDF entegrasyon kontrolü yapay QNB belgesiyle yapıldı. Gerçek Yapı Kredi/QNB ekstre örnekleri sağlanmadığı için banka PDF biçimlerinin tamamı doğrulanmış sayılmaz. API'de kimlik doğrulama bulunmadığından yerel profil, güvenli çok kullanıcılı oturum yerine geçmez.

Geçici test kayıtları `ui-test-*` geliştirme profillerindedir. Örnek ekran verileri veritabanına yazılmaz.
