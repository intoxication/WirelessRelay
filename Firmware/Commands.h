
#ifndef _COMMANDS_H_
#define _COMMANDS_H_

//#include <String.h>
#include "RemoteFunctions.h"
#include "config.h"

//set station, on/off
//read station
//read version
//set scheudle, {schedule] {station number, duration}

extern void SetPinOn(int);
extern void SetPinOff(int);
extern void GetDeviceState(int);
extern void Identify(int);

struct command {
        char* functionName;
        void (*function)(int);
};

struct command commands[] = {
  {"PIN_ON", SetPinOn},
  {"PIN_OFF", SetPinOff},
  {"STATE", GetDeviceState},
  {"IDENTIFY", Identify},
  {"USB_ON", USBOn},
  {"USB_OFF", USBOff},
};

class Commander{
  public:
    Commander(ESP8266 *wifi)
    {
      this->mWifi = wifi;
      this->last = 0;
    }
    
    void update()
    {
      unsigned long current = millis();
      
      if(current - last >= 60000)
      {
        last = current;
        mWifi->send(1, (const uint8_t*)DEVICE_ID, sizeof(DEVICE_ID) / sizeof(char));
      }

      uint8_t ubuff[64] = {'\0'};
      mWifi->recv(1,ubuff,sizeof(ubuff) / sizeof(uint8_t), 200);
     
      String msg = String((const char*)ubuff);
      this->parse(msg);

#ifdef VERBOSE_SETUP
      Serial.print("duration in millis ");
      Serial.println(millis() - current);
#endif
    }
  private:
    void parse(String stz) 
    {

      if(stz.length() == 0)
      {
        return;
      }

      if(stz.indexOf("/STATE/") != -1)
      {
        //send state
        GetDeviceState(0);
        return;
      }

       if(stz.indexOf("/IDENTIFY/") != -1)
      {
        //send state
        Identify(0);
        return;
      }

      //check if message is for this device
      if(stz.indexOf(DEVICE_ID) == -1)
      {
#ifdef DEBUG
        Serial.print("Message not for this device");
#endif
        return;
      }
      
      Serial.print("Received message: ");
      Serial.println(stz);
       
      //this->execute(stz);   
      this->executeV2(stz);  
    }

    void executeV2(String str)
    {
      //  "/{DEVICE ID}/<CMD>/<param1>

      Serial.print("Command: ");
      Serial.println(str);

      char *pch;
      pch = strtok ((char*)str.c_str(),"/");
      uint8_t splitPos = 0;

      char *cmd;
      char *device;
      char *param;
      
      while (pch != NULL)
      {
        //device         
        if(splitPos == 0) {
          device = pch;
        }
        //command
        if(splitPos == 1) {
          cmd = pch;
        }
        //parameter
        if(splitPos == 2) {
         param = pch; 
        }

        pch = strtok (NULL, "/");
        splitPos++;
      }

      //don't care about device here its already validated;

      struct command *scan;
      
      for(scan=commands; scan->function; scan++)
      {
        if(!strcmp(cmd, scan->functionName))
        {
          scan->function(atoi(param));
          break;
        }
      }
    }
    
    void execute(String cmd) 
    {
      //  "/{DEVICE ID}/<CMD>/<param1>

      Serial.print("Command: ");
      Serial.println(cmd);

      String ourCommand = "";

      int idx = cmd.indexOf('/');
      int idxE = cmd.lastIndexOf('/');
      
      if(idx + 1 <= cmd.length() && idxE<= cmd.length() && idx > -1 && idxE > -1)
      {
        ourCommand = cmd.substring(idx+1, idxE);

        idx = ourCommand.indexOf('/');
        if(idx + 1 <= ourCommand.length() && idx > -1)
        {
          ourCommand = ourCommand.substring(idx+1);
        }
      }
      
      String param1 = "";
      if(idxE > -1 && idxE + 1 <= cmd.length())
      {
        param1 = cmd.substring(idxE+1);
      }
      
      if(ourCommand.length() <= 0)
      {
        Serial.println("Command not found");
        return;
      }
      
      Serial.print("command: ");
      Serial.print(ourCommand);

      if(param1.length() > 0)
      {
        Serial.print("\t");
        Serial.print("parameter: ");
        Serial.println(param1);
      }
      
      struct command *scan;
      
      for(scan=commands; scan->function; scan++)
      {
        if(!strcmp(ourCommand.c_str(), scan->functionName))
        {
          scan->function(param1.toInt());
          break;
        }
      }
    }
  
    ESP8266 *mWifi;   
    unsigned long last;
};

#endif //_COMMANDS_H_
