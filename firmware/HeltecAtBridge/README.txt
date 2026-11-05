HeltecAtBridge — свой AT-скетч TX/RX под HeltecLoRaTerminal

1) Arduino IDE
   - File → Preferences → Additional Board Manager URLs:
     https://resource.heltec.cn/download/package_heltec_esp32_index.json
   - Boards Manager: Heltec ESP32 Series Dev-boards
   - Library Manager: HELTEC ESP32

2) Tools
   - Board: WiFi LoRa 32 (V3)  [или V2 / V4 — как на плате]
   - Port: COMx
   - Region / frequency band: EU868 / 863–870 (или US915), как маркировка платы

3) Открыть HeltecAtBridge.ino → Upload
   Если не шьётся: зажать PRG, нажать RST, отпустить PRG, Upload, потом RST.

4) WinForms
   - Подключить тот же COM, 115200
   - Init Heltec → Слушать / Отправить hex

По умолчанию частота 868100000 Hz, SF7, BW 125 kHz, 14 dBm.
TX и RX по AT-командам; после TX скетч сам возвращается в RX, если слушали.
