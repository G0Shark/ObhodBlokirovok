# ObhodBlokirovok
Программа для удобного разделения траффика, и пропуска их через прямое подключение, либо через SOCKS5 прокси от AWGProxy. В программе встроенна удобная система установки исключений, работающих через Clash. Так-же программа позволяет установить обход некоторых блокировок через изменение hosts файла.

Программа использует [AWGProxy](https://github.com/dima424658/awgproxy) и [Clash](https://github.com/clashdownload/Clash_for_Windows).
## Установка (Windows)
У вас должны быть установлены [зависимости проекта](https://github.com/G0Shark/ObhodBlokirovok#зависимости)

1. [Скачайте последний релиз](https://github.com/G0Shark/ObhodBlokirovok/releases/latest)

2. Разархивируйте его в любую удобную папку

3. Запустите ObhodBlokirovok.exe

## Зависимости
Программа зависит от AWGProxy, что устанавливается через Golang. [Установите GoLang](https://go.dev/dl/), после установите [AWGProxy](https://github.com/dima424658/awgproxy) командой ниже

```go install github.com/pufferffish/awgproxy/cmd/awgproxy@v1.0.9 # or @latest```

После этого программу можно успешно запускать.

## Постройка
```dotnet build```
