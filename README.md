# 🎮 Universal Steam Idler

<p align="center">
  <img src="https://img.shields.io/badge/Language-C%23-blue.svg" alt="Language">
  <img src="https://img.shields.io/badge/.NET-4.5%2B%20%7C%20Windows-green.svg" alt=".NET">
  <img src="https://img.shields.io/badge/License-MIT-brightgreen.svg" alt="License">
  <img src="https://img.shields.io/badge/Size-~260%20KB-orange.svg" alt="Size">
  <img src="https://img.shields.io/badge/Status-Production%20Ready-success.svg" alt="Status">
</p>

**Universal Steam Idler** — это легкий, полностью автономный и безопасный инструмент для фонового фарма часов в любых играх Steam с удобным окном настроек, интеграцией в системный трей Windows и поддержкой одновременного фарма нескольких игр.

[English description below](#-english-description)

---

## 🇷🇺 Описание на русском

### 🔥 Почему это лучше старых решений (Idle Master, SAM, ASF)?
* **100% безопасность аккаунта:** Не требует ввода логина, пароля, Steam Guard кодов или session cookies (`sessionid`). Работает напрямую через локальный запущенный клиент Steam (`steam_api.dll`).
* **Не крашится от сетевых сбоев:** В отличие от старого Idle Master, программа не делает ненадежных HTTP-запросов к устаревшим серверам Valve и не падает с `WebException`.
* **Умный автозапуск:** Запускается в трее через 5 минут после включения ПК (настраивается), давая Windows и клиенту Steam спокойно прогрузиться.
* **Автопоиск игр на ПК:** Умеет в 1 клик сканировать библиотеки Steam (`libraryfolders.vdf` и `appmanifest_*.acf`) и находить все установленные игры.
* **Фарм нескольких игр сразу:** Можно включить одну игру или поставить галочки сразу на несколько — утилита запустит изолированные фоновые воркеры.
* **Супер-легковесный:** Весит всего ~37 КБ (с DLL суммарно ~260 КБ), потребляет 0% CPU и ~10-15 МБ RAM. Никакого Electron, Python или тяжелых фреймворков.

---

### ✨ Возможности
* 🎛 **Окно настроек:** Простой и стильный тёмный интерфейс в стиле Steam.
* 🔍 **Сканер библиотек:** Автоматический поиск установленных игр на всех ваших дисках.
* ➕ **Любая игра по AppID:** Введите название и AppID (например `2696480` для *Nothing* или `2923300` для *Banana*) и добавьте в список.
* ⭐ **Встроенные пресеты:** Быстрое добавление популярных игр для фарма (*Nothing, Banana, Spacewar, CS2, TF2, Rust, Dota 2*).
* 📥 **Системный трей:**
  * Индикатор состояния (Зеленый = активно, Желтый = ожидание/таймер, Серый = пауза).
  * Быстрое переключение игр прямо из контекстного меню трея.
  * Пауза и возобновление в один клик.
  * Переключатель автозапуска в меню.
* 🚀 **Одиночный и мульти-фарм:** Поддержка одновременного учета времени в нескольких играх.

---

### 🛠 Сборка из исходников

Для сборки **не требуется** Visual Studio или установленный SDK. Достаточно встроенного в Windows компилятора .NET:

1. Склонируйте или скачайте репозиторий:
   ```bash
   git clone https://github.com/your-username/UniversalSteamIdler.git
   cd UniversalSteamIdler
   ```
2. Запустите скрипт сборки:
   ```cmd
   build.bat
   ```
3. Готовый бинарник появится в папке `bin\UniversalSteamIdler.exe`.

*(Для разработчиков также предоставлены файлы `UniversalSteamIdler.sln` и `.csproj`)*.

---

### 🚀 Запуск и использование

1. Убедитесь, что клиент Steam запущен и авторизован.
2. Запустите `UniversalSteamIdler.exe`.
3. Отметьте нужные игры галочкой `[V]` в колонке «Фарм».
4. Нажмите **«Свернуть в трей»**.
5. Программа продолжит тихо работать в правом нижнем углу экрана возле часов.

#### Параметры командной строки:
* `UniversalSteamIdler.exe` — обычный запуск (открывает трей/окно по настройкам).
* `UniversalSteamIdler.exe --startup` — запуск с системным таймером ожидания (по умолчанию 5 мин).
* `UniversalSteamIdler.exe --settings` — принудительно открыть окно настроек при старте.
* `UniversalSteamIdler.exe --worker <appid> <parentPid>` — запуск отдельного изолированного фонового воркера.

---

<a name="-english-description"></a>
## 🇬🇧 English Description

**Universal Steam Idler** is a lightweight, zero-dependency, and safe Windows tray application designed to idle Steam hours for any game.

### 🌟 Key Advantages
1. **Zero Credentials Required:** Does not ask for your Steam username, password, or auth cookies. It uses the local `steam_api.dll` client interface directly.
2. **Crash-Proof:** No unhandled network requests or dead CDN banner downloads.
3. **Smart Autostart:** Can wait 5 minutes after Windows boot before starting the idling process.
4. **Steam Library Scanner:** Auto-detects all installed games across all Steam library drives with one click.
5. **Multi-Game Support:** Idle one game or multiple games simultaneously via headless child workers.
6. **Extremely Lightweight:** ~260 KB total size, ~10 MB memory usage, 0% CPU.

---

### 💻 Compiling from Source

Simply run `build.bat` in the repository root. It uses Windows' built-in `csc.exe` compiler (`v4.0.30319`) and compiles in under 2 seconds.

---

### 📄 License
This project is licensed under the [MIT License](LICENSE).
