#include <ESP8266.h>
#include <libmaple/iwdg.h>

#include "Commands.h"
#include "config.h"

//#define DEBUG
#define VERBOSE_SETUP

ESP8266 *wifi;
Commander *commander;

void setup() {
  
  // put your setup code here, to run once:
#ifdef VERBOSE_SETUP
  Serial.println("Turning off relays...");
#endif
  pinMode(D18, OUTPUT);
  pinMode(D19, OUTPUT);
  pinMode(D20, OUTPUT);
  pinMode(D21, OUTPUT);
  pinMode(D22, OUTPUT);
  pinMode(D23, OUTPUT);
  pinMode(D24, OUTPUT);
  pinMode(D25, OUTPUT);
  pinMode(D26, OUTPUT);
  pinMode(D27, OUTPUT);
  pinMode(D28, OUTPUT);
  pinMode(D29, OUTPUT);
  pinMode(D30, OUTPUT);
  pinMode(D31, OUTPUT);

  digitalWrite(D18, 0);
  digitalWrite(D19, 0);
  digitalWrite(D20, 0);
  digitalWrite(D21, 0);
  digitalWrite(D22, 0);
  digitalWrite(D23, 0);
  digitalWrite(D24, 0);
  digitalWrite(D25, 0);
  digitalWrite(D26, 0);
  digitalWrite(D27, 0);
  digitalWrite(D28, 0);
  digitalWrite(D29, 0);
  digitalWrite(D30, 0);
  digitalWrite(D31, 0);

#ifdef VERBOSE_SETUP
  Serial.println("Waiting for system to settle...");
#endif

  delay(5000);

  Serial.println("Starting Simple Relay Setup");

  Serial3.begin(9600);

#ifdef VERBOSE_SETUP
  Serial.println("Creating Wifi Device...");
#endif
  
  wifi = new ESP8266(Serial3, 9600);

  wifi->kick();
  wifi->setOprToStation();

  delay(2000);

#ifdef VERBOSE_SETUP
  Serial.println("Connecting to AP...");
#endif
  if(!wifi->joinAP(SSID,PASSWORD)) {
    Serial.println("Unable to connect to access point");
  }

  //doesnt work
  Serial.print("Local Ip: " );
  Serial.println(String(wifi->getLocalIP()));

  wifi->enableMUX();

#ifdef VERBOSE_SETUP
  Serial.println("Set to Mode to UDP");
#endif
  wifi->registerUDP( 1,HOST,PORT);


#ifdef VERBOSE_SETUP
  Serial.println("Setup as local server...");
#endif
  if(!wifi->startServer(PORT)) {
    Serial.println("Unable to start server");
  }
  else
  {
    Serial.println("Failed to start local server...");
  }

  Serial.print("ESP8266 Firmware version: " );
  Serial.println(wifi->getVersion());

#ifdef VERBOSE_SETUP
  Serial.println("Setup commander...");
#endif
  commander = new Commander(wifi);

  Serial.println("Simple Relay Driver r1 Now Ready...");

  //setup an 8 second watch dog timer
  iwdg_init(IWDG_PRE_256, 1250);

  //send hello
  Identify(1);
}

void loop() {    
  commander->update();

  //Test the wifi connection
  if(wifi->send(1, (const uint8_t*)DEVICE_ID, sizeof(DEVICE_ID) / sizeof(char)))
  {
	  //wifi is ok.	  
	  //feed the watch dog so we don't reset
	  iwdg_feed();
  }
  else {
	  reconnect();	  
  }
}

void reconnect()
{
#ifdef VERBOSE_SETUP
  Serial.println("Trying to reconnect - Send Failed");
#endif

  //try reconnect
  wifi = new ESP8266(Serial3, 9600);

  wifi->kick();
  wifi->setOprToStation();
  if(!wifi->joinAP(SSID,PASSWORD)) {
    Serial.println("Unable to connect to access point");
  }

  delay(200);
  
  //doesnt work
  Serial.print("Local Ip: " );
  Serial.println(String(wifi->getLocalIP()));

  wifi->enableMUX();
  
  wifi->registerUDP( 1,HOST,PORT);

  delay(200);

#ifdef VERBOSE_SETUP
  Serial.println("Setup as local server...");
#endif
  if(!wifi->startServer(PORT)) {
    Serial.println("Unable to start server");
  }
  else
  {
    Serial.println("Failed to start local server...");
  }

  delay(200);
}

