
#include "RemoteFunctions.h"
#include <Arduino.h>

extern ESP8266 *wifi;

void SetPinOn(int p)
{  
  Serial.print("Set Pin ON ");
  Serial.println(p);
  
  digitalWrite(p + PIN_OFFSET, 1);

  //read pin state
  int ps = digitalRead(p + PIN_OFFSET);

  char tempBuff[18] = {'\0'};
  sprintf(tempBuff, "ACK/PIN_ON/%d/%d", p, ps);
  
  wifi->send(1,(const uint8_t*)tempBuff, sizeof(tempBuff) / sizeof(char));
}

void SetPinOff(int p)
{
  Serial.print("Set Pin OFF ");
  Serial.println(p);
  
  digitalWrite(p + PIN_OFFSET, 0);

    //read pin state
  int ps = digitalRead(p + PIN_OFFSET);

  char tempBuff[18] = {'\0'};
  sprintf(tempBuff, "ACK/PIN_OFF/%d/%d", p, ps);

  wifi->send(1,(const uint8_t*)tempBuff, sizeof(tempBuff) / sizeof(char));
}

void GetDeviceState(int)
{  
  Serial.println("Get Device State");
  
  int portA = GPIOA->regs->ODR;//BSRR
  int portB = GPIOB->regs->ODR;
  int portC = GPIOC->regs->ODR;
  
  String command(DEVICE_ID);
  command += "/STATE/";
  command += portA ;
  command += "," ;
  command += portB  ;
  command += "," ;
  command += portC;

  char tempBuff[64] = {'\0'};
  command.toCharArray(tempBuff, sizeof(tempBuff) / sizeof(char));
  
  wifi->send(1,(const uint8_t*)tempBuff, sizeof(tempBuff) / sizeof(char));

  Serial.print("send: ");
  Serial.println(command.c_str());

  delay(2);
}

void Identify(int i) 
{
  if(i == 1) {
    char hello[] = { 'Online\r\n\0' };
    wifi->send(1,(const uint8_t*)hello, sizeof(hello) / sizeof(char));
  }
  else
    wifi->send(1,(const uint8_t*)DEVICE_ID, sizeof(DEVICE_ID) / sizeof(char));
}

void USBOff(int)
{
  Serial.println("USB Powering down - to reconnect send the \"USB_ON/0\" command");
  Serial.end();
}
void USBOn(int)
{
  Serial.println("USB restored"); 
  Serial.begin();
}

