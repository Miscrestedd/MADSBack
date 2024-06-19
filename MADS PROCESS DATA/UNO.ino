const int mq135Pin = A0;

void setup() {
  Serial.begin(9600);
}

void loop() {
  int sensorValue = analogRead(mq135Pin);
  Serial.println(sensorValue);
  delay(1000);
}
