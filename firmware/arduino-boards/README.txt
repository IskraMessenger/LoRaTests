Если Arduino IDE не качает https://resource.heltec.cn/download/package_heltec_esp32_index.json
(браузер при этом открывает) — это частая проблема Java/SSL IDE с доменом .cn.

Вариант A (рекомендуется): Espressif + библиотека Heltec
1. File → Preferences → Additional Board Manager URLs:
   https://espressif.github.io/arduino-esp32/package_esp32_index.json
2. Boards Manager → установить "esp32" by Espressif Systems
3. Library Manager → установить "HELTEC ESP32"
4. Tools → Board → esp32 → Heltec WiFi LoRa 32(V3)  (или V2/V4)

Вариант B: локальный JSON Heltec
1. В Additional Board Manager URLs укажи:
   file:///D:/Work/LoRaTests/firmware/arduino-boards/package_heltec_esp32_index.json
   (Arduino IDE 1.8 иногда принимает file://; IDE 2.x — не всегда)
2. Если file:// не работает — поставь простой локальный HTTP:
   cd D:\Work\LoRaTests\firmware\arduino-boards
   python -m http.server 8765
   и в URLs:
   http://127.0.0.1:8765/package_heltec_esp32_index.json

Вариант C: прокси/VPN или другой DNS — если IDE режет доступ к resource.heltec.cn.
