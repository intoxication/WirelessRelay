using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterCommander
{
    class Program
    {
        //alias -> 217BB

		//Notes -> add VERBOSE settings
		//i.e print network packets received,
		//print how many timeouts we had/retries
		//print raw device states
		
		//FIX ME - calling -s with no other args crashes
        static void Main(string[] args)
        {
            string device = string.Empty;

            string master = string.Empty;
            string deviceId = string.Empty;
            string setAlias = string.Empty;
            string pin = string.Empty;
            string pinOff = string.Empty;

            bool help = false;
            bool reset = false;
			bool showState = false;
			
            var optionSet = new OptionSet() {
                { "a|add=", "Add a new device GUID", v => { device = v; } },
                { "m|master=", "Set device master pin", v => { master = v; } },
                { "d|device=", "Indicate which device you wish to set", v => { deviceId = v; } },
                { "s|alias=", "Set device alias", v=> { setAlias = v; } },
                { "l|list", "List all devices", v => { ListAllDevices(); } },
                { "h|help", "Display this help message", v=> { help = true; } },
                { "p|pin=", "Turn on device pin", v => { pin = v; } },
                { "x|pinoff=", "Turn off device pin", v => { pinOff = v; } },
                { "r|reset", "Reset device", v => { reset = true; } },
				{ "v|version", "Display "+" version", v => { PrintVersion(); } },
				{ "q|state", "Show devices current state", v => { showState = true; } },
                { "verbose", "Show extra verbose messages", v => { Device.IsVerbose = true; } },
            };

            //List<string> extra = 
            try
            {
                optionSet.Parse(args);
            }
            catch (NDesk.Options.OptionException ex)
            {
                Console.WriteLine("Error " + ex.Message);
                return;
            }

            AddDevice(device);

            Device dev = Device.Find(deviceId);
           
            SetAlias(setAlias, dev);
            SetMaster(master, dev);
            SendPinOn(pin, dev);
            SendPinOff(pinOff, dev);

            if(help)
            {
                PrintHelp(optionSet);
            } 
			
            if(reset && dev != null)
            {
                dev.Reset();
            }    
			else if(reset && dev == null)
			{
				Console.WriteLine("Please provide device or alias (-d\t--device [device alias] or [device id])");
			}
			
			if(showState)
			{
				PrintDeviceState();
			}
        }

        private static void SendPinOn(string pin, Device device)
        {
            if (String.IsNullOrEmpty(pin))
                return;

            if (device == null)
            {
                Console.WriteLine("Please provide device or alias (-d\t--device [device alias] or [device id])");
                return;
            }

            ushort upin = 0;
            if(!ushort.TryParse(pin, out upin))
            {
                Console.WriteLine("Invalid pin");
                return;
            }

            if(device.TurnOn(upin))
            {
                Console.WriteLine("pin " + upin.ToString() + " is on for " + device.Alias);
            }
            else
            {
                Console.WriteLine("pin " + upin.ToString() + " couldn't be turned on for " + device.Alias);
            }

            //turn on master after secondary pins
			if(device.Pins.ContainsKey(device.MasterPin))
			{
				if (device.Pins[device.MasterPin])
					return;
				else
				{
					if (device.TurnOn(device.MasterPin))
					{
						Console.WriteLine("master pin " + device.MasterPin + " is on for " + device.Alias);
					}
					else
					{
						Console.WriteLine("master pin " + device.MasterPin + " couldn't be turned on for " + device.Alias);
					}
				}    
			}	
			else			
			{
                if(Device.IsVerbose)
				    Console.WriteLine("warning no master pin set for " + device.GUID);
			}
        }

        private static void SendPinOff(string pin, Device device)
        {
            if (String.IsNullOrEmpty(pin))
                return;

            if (device == null)
            {
                Console.WriteLine("Please provide device or alias (-d\t--device [device alias] or [device id])");
                return;
            }

            ushort upin = 0;
            if (!ushort.TryParse(pin, out upin))
            {
                Console.WriteLine("Invalid pin");
                return;
            }

            if (device.TurnOff(upin))
            {
                Console.WriteLine("pin " + upin.ToString() + " is off for " + device.Alias);
            }
            else
            {
                Console.WriteLine("pin " + upin.ToString() + " couldn't be turned off for " + device.Alias);
            }

            //turn on master after secondary pins
			if(device.Pins.ContainsKey(device.MasterPin))
			{
				if (!device.Pins[device.MasterPin])
					return;
				else
				{
					if (device.TurnOff(device.MasterPin))
					{
						Console.WriteLine("master pin " + device.MasterPin + " is on for " + device.Alias);
					}
					else
					{
						Console.WriteLine("master pin " + device.MasterPin + " couldn't be turned on for " + device.Alias);
					}
				}
			}	
			else			
			{
                if (Device.IsVerbose)
                    Console.WriteLine("warning no master pin set for " + device.GUID);
			}
        }

        private static void PrintHelp(OptionSet p)
        {          
            Console.WriteLine("Usage: wc [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);            
        }

        private static void ListAllDevices()
        {
            Console.WriteLine("GUID\t\t\t\t\tAlias\t\tMaster\tState");
            Console.WriteLine("-------------------------------------------------------------------------------");
            foreach (var d in Device.Devices)
            {
                Console.WriteLine(d.GUID + "\t" + d.Alias + "\t" + d.MasterPin + "\t" + d.Pins.Any(a => a.Value).ToString());
            }
        }

        private static void SetAlias(string alias, Device device)
        {
            if (String.IsNullOrEmpty(alias))
                return;

            if(device == null)
            {
                Console.WriteLine("Please provide device or alias (-d\t--device [device alias] or [device id])");
                return;
            }

            device.SetAlias(alias.Trim());

            Console.WriteLine("Added alias \"" + device.Alias + "\" to device " + device.GUID);
        }

        private static void SetMaster(string master, Device device)
        {
            if (String.IsNullOrEmpty(master))
                return;

            if (device == null)
            {
                Console.WriteLine("Please provide device or alias (-d\t--device [device alias] or [device id])");
                return;
            }

            ushort pin = 0;
            if(!ushort.TryParse(master, out pin))
            {
                Console.Write("Invalid pin, enter valid positive integer (1-255)");
                return;
            }

            device.SetMasterPin(pin);
        }

        private static void AddDevice(string device)
        {
            if (!String.IsNullOrEmpty(device))
            {
                Console.WriteLine("Added device " + device);
                Device dev = new Device() { GUID = device.Trim() };
                Device.Add(dev);
            }
        }
		
		private static void PrintVersion()
		{
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            Console.WriteLine(assembly.GetName() + " v" + version);
		}
		
		private static void PrintDeviceState(bool autoAdd = false)
		{
            var devs = Device.QueryState();

            if(devs == null || devs.Count == 0)
            {
                Console.WriteLine("No devices responded");
                return;
            }

            foreach(var d in devs)
            {
                string hexstateA = String.Format("{0:X}", d.PORTA);
                string hexstateB = String.Format("{0:X}", d.PORTB);
                Console.WriteLine("Device " + d.DeviceID + " responded with states A:" + hexstateA + " B:" + hexstateB);

                for(int i = 1; i <= 14; i++)
                {
                    if (d.PinState[i])
                        Console.WriteLine(i + " is on"); ;
                }

                if (Device.Find(d.DeviceID) == null)
                {
                    Device.Add(new Device() { GUID = d.DeviceID });
                }
            }
		}
    }
}
