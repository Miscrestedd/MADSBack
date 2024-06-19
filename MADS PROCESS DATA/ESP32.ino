#include <WiFi.h>
#include <WiFiClient.h>
#include <WiFiAP.h>

const char* ssid = "bjH+zP8gBuW/k+MPdrRg0g==";
const char* password = "lI+lvN91s3Fxb1lq91Dd8Q==";

WiFiServer server(80);

void setup() {
  Serial.begin(9600);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Conectando ao WiFi...");
  }

  Serial.println("Conectado ao WiFi");
  Serial.println(WiFi.localIP());

  server.begin();
}

void loop() {
  WiFiClient client = server.available();

  if (client) {
    while (client.connected()) {
      if (Serial.available()) {
        String sensorValue = Serial.readStringUntil('\n');
        client.println(sensorValue);
        Serial.println(sensorValue);
      }
    }
    client.stop();
  }
}
