/*
 * HeltecAtBridge — AT-мост TX/RX для WiFi LoRa 32 (V2/V3/V4)
 * Совместим с HeltecLoRaTerminal (WinForms).
 *
 * Arduino IDE:
 *   Board: Heltec ESP32 Series → WiFi LoRa 32 (V3) / V2 / V4
 *   Library: HELTEC ESP32 (HelTecAutomation)
 *   Serial: 115200
 *
 * Команды (как у Heltec CubeCell AT, CR/LF необязательны):
 *   AT+XXX              — wake / ping → +OK
 *   AT+AutoLPM=0|1
 *   AT+LORAWAN=0        — P2P mode (единственный режим этого скетча)
 *   AT+PrintMode=0|1    — 0=string, 1=hex (по умолчанию 1)
 *   AT+FREQ=<Hz>
 *   AT+LoraSet=freq,pwr,SF,BW,CR,preamble,CRC,IQ,save
 *   AT+RX=0             — слушать непрерывно (пока не TX / новая команда)
 *   AT+RX=<ms>          — слушать с таймаутом
 *   AT+SendHex=<hex>    — передать бинарь (до 64 байт), затем снова RX если был listen
 *   AT+Busy=?
 *   AT+ChipID=?
 */

#include "LoRaWan_APP.h"
#include "Arduino.h"
#include <ctype.h>
#include <stdlib.h>
#include <string.h>

#ifndef RF_FREQUENCY
#define RF_FREQUENCY 868100000UL
#endif

#define TX_OUTPUT_POWER_DEFAULT 14
#define LORA_BANDWIDTH_DEFAULT 0
#define LORA_SPREADING_FACTOR_DEFAULT 7
#define LORA_CODINGRATE_DEFAULT 1
#define LORA_PREAMBLE_LENGTH_DEFAULT 8
#define LORA_FIX_LENGTH_PAYLOAD_ON false
#define MAX_PAYLOAD 64
#define CMD_IDLE_MS 40
#define CMD_BUF_SIZE 256

enum class RadioMode : uint8_t { Idle, Rx, Tx };

static uint32_t g_freq = RF_FREQUENCY;
static int8_t g_power = TX_OUTPUT_POWER_DEFAULT;
static uint8_t g_bw = LORA_BANDWIDTH_DEFAULT;
static uint8_t g_sf = LORA_SPREADING_FACTOR_DEFAULT;
static uint8_t g_cr = LORA_CODINGRATE_DEFAULT;
static uint16_t g_preamble = LORA_PREAMBLE_LENGTH_DEFAULT;
static bool g_crc = true;
static bool g_iqInvert = false;
static bool g_printHex = true;
static bool g_autoLpm = false;

static RadioMode g_mode = RadioMode::Idle;
static bool g_wantListen = false;
static uint32_t g_rxTimeoutMs = 0; // 0 = continuous
static bool g_busy = false;

static RadioEvents_t RadioEvents;
static uint8_t g_rxBuf[MAX_PAYLOAD];
static char g_cmdBuf[CMD_BUF_SIZE];
static size_t g_cmdLen = 0;
static uint32_t g_lastByteMs = 0;

static void applyRadioConfig();
static void enterRx();
static void stopRadio();
static void handleCommand(char *cmd);
static void processSerial();
static bool parseHexPayload(const char *hex, uint8_t *out, size_t *outLen);
static void printOk();
static void printError(const char *msg);

static void OnTxDone(void)
{
  Serial.println("TX done");
  g_busy = false;
  g_mode = RadioMode::Idle;
  if (g_wantListen) {
    enterRx();
  } else {
    Radio.Sleep();
  }
}

static void OnTxTimeout(void)
{
  Serial.println("TX timeout");
  g_busy = false;
  g_mode = RadioMode::Idle;
  Radio.Sleep();
  if (g_wantListen) {
    enterRx();
  }
}

static void OnRxDone(uint8_t *payload, uint16_t size, int16_t rssi, int8_t snr)
{
  if (size > MAX_PAYLOAD) {
    size = MAX_PAYLOAD;
  }
  memcpy(g_rxBuf, payload, size);

  if (g_printHex) {
    Serial.print("Received Hex:");
    for (uint16_t i = 0; i < size; i++) {
      if (g_rxBuf[i] < 0x10) {
        Serial.print('0');
      }
      Serial.print(g_rxBuf[i], HEX);
    }
    Serial.println();
  } else {
    Serial.print("Received String:");
    for (uint16_t i = 0; i < size; i++) {
      Serial.write(g_rxBuf[i]);
    }
    Serial.println();
  }
  Serial.printf("RSSI:%d, SNR:%d, Size:%d\r\n", rssi, snr, size);

  g_busy = false;
  g_mode = RadioMode::Idle;
  Radio.Sleep();

  // После пакета снова слушаем, если режим RX активен.
  if (g_wantListen) {
    enterRx();
  }
}

static void OnRxTimeout(void)
{
  Serial.println("RX Timeout");
  g_busy = false;
  g_mode = RadioMode::Idle;
  Radio.Sleep();
  if (g_wantListen && g_rxTimeoutMs == 0) {
    enterRx();
  } else {
    g_wantListen = false;
  }
}

static void OnRxError(void)
{
  Serial.println("Rx error");
  g_busy = false;
  g_mode = RadioMode::Idle;
  Radio.Sleep();
  if (g_wantListen) {
    enterRx();
  }
}

void setup()
{
  Serial.begin(115200);
  Mcu.begin(HELTEC_BOARD, SLOW_CLK_TPYE);

  RadioEvents.TxDone = OnTxDone;
  RadioEvents.TxTimeout = OnTxTimeout;
  RadioEvents.RxDone = OnRxDone;
  RadioEvents.RxTimeout = OnRxTimeout;
  RadioEvents.RxError = OnRxError;

  Radio.Init(&RadioEvents);
  applyRadioConfig();
  Radio.Sleep();

  Serial.println();
  Serial.println("HeltecAtBridge ready");
  Serial.println("+OK");
  Serial.printf("+FREQ=%lu\r\n", (unsigned long)g_freq);
  Serial.printf("+SF=%u\r\n", g_sf);
  Serial.printf("+TxPower=%d\r\n", g_power);
}

void loop()
{
  processSerial();
  Radio.IrqProcess();
}

static void applyRadioConfig()
{
  Radio.SetChannel(g_freq);
  Radio.SetTxConfig(
      MODEM_LORA, g_power, 0, g_bw, g_sf, g_cr, g_preamble,
      LORA_FIX_LENGTH_PAYLOAD_ON, g_crc, 0, 0, g_iqInvert, 3000);
  Radio.SetRxConfig(
      MODEM_LORA, g_bw, g_sf, g_cr, 0, g_preamble, 0,
      LORA_FIX_LENGTH_PAYLOAD_ON, 0, g_crc, 0, 0, g_iqInvert, true);
}

static void stopRadio()
{
  g_wantListen = false;
  g_busy = false;
  g_mode = RadioMode::Idle;
  Radio.Sleep();
}

static void enterRx()
{
  applyRadioConfig();
  g_wantListen = true;
  g_busy = true;
  g_mode = RadioMode::Rx;
  // 0 = continuous listen in Heltec Radio API
  uint32_t t = (g_rxTimeoutMs == 0) ? 0 : g_rxTimeoutMs;
  Radio.Rx(t);
}

static void printOk()
{
  Serial.println("+OK");
}

static void printError(const char *msg)
{
  Serial.print("+ERROR: ");
  Serial.println(msg);
}

static void processSerial()
{
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    g_lastByteMs = millis();

    if (c == '\r' || c == '\n') {
      if (g_cmdLen > 0) {
        g_cmdBuf[g_cmdLen] = '\0';
        handleCommand(g_cmdBuf);
        g_cmdLen = 0;
      }
      continue;
    }

    if (g_cmdLen + 1 < CMD_BUF_SIZE) {
      g_cmdBuf[g_cmdLen++] = c;
    } else {
      g_cmdLen = 0;
      printError("command too long");
    }
  }

  // Heltec AT без терминатора: закрываем команду по паузе.
  if (g_cmdLen > 0 && (millis() - g_lastByteMs) >= CMD_IDLE_MS) {
    g_cmdBuf[g_cmdLen] = '\0';
    handleCommand(g_cmdBuf);
    g_cmdLen = 0;
  }
}

static bool startsWith(const char *s, const char *prefix)
{
  return strncmp(s, prefix, strlen(prefix)) == 0;
}

static bool parseHexPayload(const char *hex, uint8_t *out, size_t *outLen)
{
  size_t n = 0;
  const char *p = hex;
  while (*p) {
    while (*p == ' ' || *p == ',' || *p == '-' || *p == ':') {
      p++;
    }
    if (!*p) {
      break;
    }
    if (!isxdigit((unsigned char)p[0]) || !isxdigit((unsigned char)p[1])) {
      return false;
    }
    if (n >= MAX_PAYLOAD) {
      return false;
    }
    char tmp[3] = { p[0], p[1], 0 };
    out[n++] = (uint8_t)strtoul(tmp, nullptr, 16);
    p += 2;
  }
  *outLen = n;
  return n > 0;
}

static void handleCommand(char *cmd)
{
  // trim leading spaces
  while (*cmd == ' ' || *cmd == '\t') {
    cmd++;
  }
  if (*cmd == '\0') {
    return;
  }

  // Any AT... wakes / acknowledges
  if (strcmp(cmd, "AT+XXX") == 0 || strcmp(cmd, "AT") == 0) {
    printOk();
    return;
  }

  if (startsWith(cmd, "AT+AutoLPM=")) {
    g_autoLpm = atoi(cmd + 11) != 0;
    printOk();
    Serial.printf("+AutoLPM=%d\r\n", g_autoLpm ? 1 : 0);
    return;
  }

  if (strcmp(cmd, "AT+LORAWAN=0") == 0 || strcmp(cmd, "AT+LORAWAN=?") == 0) {
    printOk();
    Serial.println("+LORAWAN=0");
    return;
  }
  if (startsWith(cmd, "AT+LORAWAN=") && cmd[11] == '1') {
    printError("LoRaWAN not supported in HeltecAtBridge");
    return;
  }

  if (startsWith(cmd, "AT+PrintMode=")) {
    if (cmd[13] == '?') {
      printOk();
      Serial.printf("+PrintMode=%d\r\n", g_printHex ? 1 : 0);
      return;
    }
    g_printHex = atoi(cmd + 13) != 0;
    printOk();
    Serial.printf("+PrintMode=%d\r\n", g_printHex ? 1 : 0);
    return;
  }

  if (startsWith(cmd, "AT+FREQ=")) {
    if (cmd[8] == '?') {
      printOk();
      Serial.printf("+FREQ=%lu\r\n", (unsigned long)g_freq);
      return;
    }
    uint32_t f = strtoul(cmd + 8, nullptr, 10);
    if (f < 150000000UL || f > 960000000UL) {
      printError("FREQ out of range");
      return;
    }
    g_freq = f;
    bool keepListen = g_wantListen;
    stopRadio();
    applyRadioConfig();
    printOk();
    Serial.printf("+FREQ=%lu\r\n", (unsigned long)g_freq);
    if (keepListen) {
      enterRx();
    }
    return;
  }

  if (startsWith(cmd, "AT+LoraSet=")) {
    // freq,pwr,SF,BW,CR,preamble,CRC,IQ,save
    if (cmd[11] == '?') {
      printOk();
      Serial.printf("+LoraPara:%lu,%d,%u,%u,%u,%u,%d,%d,0\r\n",
                    (unsigned long)g_freq, g_power, g_sf, g_bw, g_cr, g_preamble,
                    g_crc ? 1 : 0, g_iqInvert ? 1 : 0);
      return;
    }

    unsigned long freq = g_freq;
    int power = g_power;
    int sf = g_sf, bw = g_bw, cr = g_cr, preamble = g_preamble;
    int crc = g_crc ? 1 : 0, iq = g_iqInvert ? 1 : 0, save = 0;
    int n = sscanf(cmd + 11, "%lu,%d,%d,%d,%d,%d,%d,%d,%d",
                   &freq, &power, &sf, &bw, &cr, &preamble, &crc, &iq, &save);
    if (n < 8) {
      printError("LoraSet format");
      return;
    }
    if (sf < 5 || sf > 12 || bw < 0 || bw > 2 || cr < 1 || cr > 4) {
      printError("LoraSet params");
      return;
    }
    g_freq = (uint32_t)freq;
    g_power = (int8_t)power;
    g_sf = (uint8_t)sf;
    g_bw = (uint8_t)bw;
    g_cr = (uint8_t)cr;
    g_preamble = (uint16_t)preamble;
    g_crc = crc != 0;
    g_iqInvert = iq != 0;
    (void)save;

    bool keepListen = g_wantListen;
    stopRadio();
    applyRadioConfig();
    printOk();
    Serial.printf("+LoraPara:%lu,%d,%u,%u,%u,%u,%d,%d,0\r\n",
                  (unsigned long)g_freq, g_power, g_sf, g_bw, g_cr, g_preamble,
                  g_crc ? 1 : 0, g_iqInvert ? 1 : 0);
    if (keepListen) {
      enterRx();
    }
    return;
  }

  if (startsWith(cmd, "AT+RX=")) {
    long t = atol(cmd + 6);
    if (t < 0) {
      printError("RX timeout");
      return;
    }
    g_rxTimeoutMs = (uint32_t)t;
    enterRx();
    printOk();
    Serial.printf("+RX=%lu\r\n", (unsigned long)g_rxTimeoutMs);
    return;
  }

  if (startsWith(cmd, "AT+SendHex=")) {
    uint8_t payload[MAX_PAYLOAD];
    size_t len = 0;
    if (!parseHexPayload(cmd + 11, payload, &len)) {
      printError("bad hex / empty / >64");
      return;
    }
    if (g_mode == RadioMode::Tx) {
      printError("busy");
      return;
    }

    bool resumeListen = g_wantListen;
    g_wantListen = false;
    Radio.Sleep();
    applyRadioConfig();
    g_busy = true;
    g_mode = RadioMode::Tx;
    g_wantListen = resumeListen; // restore after OnTxDone

    Serial.print("+Send Hex Data:");
    for (size_t i = 0; i < len; i++) {
      if (payload[i] < 0x10) {
        Serial.print('0');
      }
      Serial.print(payload[i], HEX);
    }
    Serial.println();
    printOk();
    Radio.Send(payload, (uint8_t)len);
    return;
  }

  if (strcmp(cmd, "AT+Busy=?") == 0) {
    printOk();
    Serial.printf("+Busy=%d\r\n", g_busy ? 1 : 0);
    return;
  }

  if (strcmp(cmd, "AT+ChipID=?") == 0) {
    uint64_t chipid = ESP.getEfuseMac();
    printOk();
    Serial.printf("+ChipID:%04X%08X\r\n",
                  (uint16_t)(chipid >> 32), (uint32_t)chipid);
    return;
  }

  // unknown AT — still ack wake style for short noise
  if (startsWith(cmd, "AT")) {
    printError("unknown command");
    return;
  }
}
