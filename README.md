<img src="https://github.com/user-attachments/assets/7fd4f540-66b1-4eba-9443-477704bd7b8f" alt="Главный экран" width="786"/>

# ObhodBlokirovok

**ObhodBlokirovok** — программа для удобного разделения трафика и обхода блокировок.  
Она позволяет направлять соединения либо напрямую, либо через **SOCKS5 прокси от AWGProxy**, а также использовать встроенную систему исключений (на базе Clash).  
Дополнительно доступен обход некоторых блокировок через **изменение hosts-файла**.

---

## Возможности
- Разделение трафика: прямое подключение или через SOCKS5 (AWGProxy).  
- Простая настройка исключений с использованием **Clash**.  
- Обход блокировок через изменение `hosts`.  
- Поддержка Windows.  
- Возможность сборки из исходников (dotnet).  

---

## Установка (Windows)
У вас должны быть установлены [зависимости проекта](#-зависимости).

1. [Скачайте последний релиз](https://github.com/G0Shark/ObhodBlokirovok/releases/latest)  
2. Разархивируйте архив в удобную папку  
3. Запустите `ObhodBlokirovok.exe`  

---

## Зависимости
Программа зависит от [AWGProxy](https://github.com/dima424658/awgproxy), который устанавливается через **GoLang**.  

1. [Установите GoLang](https://go.dev/dl/)  
2. Установите AWGProxy командой:  
   ```bash
   go install github.com/dima424658/awgproxy/cmd/awgproxy@latest

После этого программу можно успешно запускать.

---

## Постройка из исходников
Собрать проект можно командой:
- ```bash
   dotnet build
   dotnet run

## Скриншоты

### Настройки
<img src="https://github.com/user-attachments/assets/dcad3ba9-d220-4d88-b93c-414f81f0b2c0" alt="Настройки исключений" width="786"/>

---

### Прямой редактор конфига
<img src="https://github.com/user-attachments/assets/74cfc50b-4843-4a43-94d4-6bb3dfdbfa34" alt="Пример работы с Clash" width="786"/>

---

### Редактор исключений для Clash
<img src="https://github.com/user-attachments/assets/8aad8418-9288-4b3a-943d-eef10fc544f5" alt="Редактор hosts" width="786"/>
