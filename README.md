# Basit Pomodoro Uygulaması

.NET 10 Minimal API ve sade bir HTML/Tailwind arayüzü ile hazırlanmış, harici kütüphane bağımlılığı olmayan pratik bir Pomodoro sayacı.

### Neler Var?
* **Ön Tanımlı Modlar:** 25/5 Klasik, 50/10 Derin Çalışma ve 40/20 modları arasında hızlı geçiş.
* **Döngü Takibi:** 4 çalışma periyodunda bir otomatik uzun mola.
* **Sesli Uyarı:** Süre bittiğinde tarayıcı üzerinden çalışan uyarı sesi.
* **Arka Plan Servisi:** Süre takibi backend tarafında `PeriodicTimer` ve `BackgroundService` ile asenkron yürütülür.

### Nasıl Çalıştırılır?
Projeyi klonladıktan sonra proje klasöründe terminali açıp çalıştırmanız yeterlidir:

```bash
dotnet run
