using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WaterCommander
{
    public class Device
    {
        private int retryLimit = 5;
        private int waitMilliSeconds = 300;

        public string GUID { get; set; }
        public string Alias { get; set; }

        public ushort MasterPin { get; set; }
        
        public void SetAlias(string alias)
        {
            Alias = alias.Trim();
            Save();
        }

        public void SetMasterPin(ushort pin)
        {
            MasterPin = (ushort)pin;
            Save();
        }

        [System.Xml.Serialization.XmlIgnore]
        public Dictionary<ushort, bool> Pins = new Dictionary<ushort, bool>();

        public bool TurnOn(ushort pin)
        {
            retryLimit = Device.MAX_RETRY;

            bool result = false;
            if (!Pins.ContainsKey(pin)) Pins.Add(pin, false);

            

            //wait for response
            //ACK/PIN_ON/{PIN_NUMBER}/1

            UdpClient udpClient = null;

            while (retryLimit > 0)
            {
                try
                {
                    #region Send
                    //send message 
                    //{DEVICE_ID}/PIN_ON/{PIN_NUMBER}

                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                       ProtocolType.Udp);

                    sock.ReceiveTimeout = waitMilliSeconds;

                    IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, PortNumber);

                    sock.SetSocketOption(SocketOptionLevel.Socket,
                      SocketOptionName.Broadcast, 1);

                    string msg = PIN_ON.Replace("{device}", this.GUID).Replace("{pin}", pin.ToString());
                    var data = Encoding.UTF8.GetBytes(msg);

                    sock.SendTo(data, iep1);
                    sock.Close();

                    if (IsVerbose)
                    {
                        Console.WriteLine("Sent message " + msg);
                    }

                    #endregion

                    var broadcastAddress = new IPEndPoint(IPAddress.Any, PortNumber);
                    udpClient = new UdpClient();
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.
                    udpClient.Client.ReceiveTimeout = waitMilliSeconds;

                    udpClient.Client.Bind(broadcastAddress);

                    bool psResponse = ReadPinStateResponse(udpClient, broadcastAddress, pin, true);
                    if (psResponse)
                        return psResponse;
                    else
                        retryLimit--;
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    if (retryLimit == 0)
                        return false;
                    if (se.SocketErrorCode == SocketError.TimedOut)
                        retryLimit--;
                }
                finally
                {
                    if(udpClient != null)
                        udpClient.Close();
                    udpClient = null;
                }
            }

            //turn on master - TODO

            return result;
        }

        public void Reset()
        {
            #region Send

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
               ProtocolType.Udp);

            sock.ReceiveTimeout = waitMilliSeconds;

            IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, PortNumber);

            sock.SetSocketOption(SocketOptionLevel.Socket,
              SocketOptionName.Broadcast, 1);

            var data = Encoding.UTF8.GetBytes(RESET.Replace("{device}", this.GUID));

            sock.SendTo(data, iep1);
            sock.Close();

            #endregion
        }

        public bool TurnOff(ushort pin)
        {
            retryLimit = Device.MAX_RETRY;

            bool result = false;
            if (!Pins.ContainsKey(pin)) Pins.Add(pin, false);
            
            //wait for response
            //ACK/PIN_OFF/{PIN_NUMBER}/0

            UdpClient udpClient = null;

            while (retryLimit > 0)
            {
                try
                {
                    #region Send
                    //send message 
                    //{DEVICE_ID}/PIN_ON/{PIN_NUMBER}

                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                       ProtocolType.Udp);

                    sock.ReceiveTimeout = waitMilliSeconds;

                    IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, PortNumber);

                    sock.SetSocketOption(SocketOptionLevel.Socket,
                      SocketOptionName.Broadcast, 1);

                    string msg = PIN_OFF.Replace("{device}", this.GUID).Replace("{pin}", pin.ToString());
                    var data = Encoding.UTF8.GetBytes(msg);

                    sock.SendTo(data, iep1);
                    sock.Close();

                    if(IsVerbose)
                    {
                        Console.WriteLine("Sent message " + msg);
                    }

                    #endregion

                    var broadcastAddress = new IPEndPoint(IPAddress.Any, PortNumber);
                    udpClient = new UdpClient();
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.
                    udpClient.Client.ReceiveTimeout = waitMilliSeconds;

                    udpClient.Client.Bind(broadcastAddress);

                    bool psResponse = ReadPinStateResponse(udpClient, broadcastAddress, pin, false);
                    if (psResponse)//we got our positive response (pin is off)
                        return psResponse;
                    else
                        retryLimit--;//try again
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    if (retryLimit == 0)
                        return false;
                    if (se.SocketErrorCode == SocketError.TimedOut)
                        retryLimit--;
                }
                finally
                {
                    if (udpClient != null)
                        udpClient.Close();
                    udpClient = null;
                }
            }

            //turn off master - TODO

            return result;
        }

        private bool ReadPinStateResponse(UdpClient udpClient, IPEndPoint broadcastAddress, ushort pin, bool onOff)
        {
            byte[] buffer = udpClient.Receive(ref broadcastAddress);

            string resp = Encoding.UTF8.GetString(buffer);

            if(IsVerbose)
            {
                Console.WriteLine("Received message " + resp);
            }

            string ack = string.Empty;

            if (onOff)
                ack = ACK_ON;
            else
                ack = ACK_OFF;

            if (resp.Contains(ack.Replace("{pin}", pin.ToString())))
            {
                if (!Pins.ContainsKey(pin)) Pins.Add(pin, true);
                Pins[pin] = true;
                return true;
            }

            return false;
        }



        #region Static Methods

        public static bool IsVerbose { get; set; }

        private static string PIN_ON = "{device}/PIN_ON/{pin}";
        private static string PIN_OFF = "{device}/PIN_OFF/{pin}";
        private static string ACK_ON = "ACK/PIN_ON/{pin}/1";
        private static string ACK_OFF = "ACK/PIN_OFF/{pin}/0";
        private static string RESET = "{device}/RESET/RESET";
        private static string DEVICE_STATE = "/STATE/";

        private static int MAX_RETRY = 5;

        public static List<Device> Devices
        {
            get; private set;
        }
        public static string DefaultFilename { get { return ".device-dictionary.xml"; } }

        private static int PortNumber = 15000;

        static Device()
        {
            IsVerbose = false;
            Devices = new List<Device>();
            Load();
        }

        public static Device Find(string alias)
        {
            foreach(var d in Device.Devices)
            {
                if (d.GUID == alias.Trim())
                    return d;
                if (d.Alias == alias.Trim())
                    return d;
                if (d.GUID.Substring(0, 5) == alias)
                    return d;
            }

            return null;
        }

        public static bool Save()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(List<Device>));

            using (StringWriter sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    try
                    {
                        xsSubmit.Serialize(writer, Devices);
                        var xml = sww.ToString();

                        using (StreamWriter outputFile = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar  + Device.DefaultFilename))
                        {
                            outputFile.WriteLine(xml);
                            if(IsVerbose)
                            {
                                Console.WriteLine("Wrote xml device list to " + Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + Device.DefaultFilename);
                            }
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public static bool Load()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(List<Device>));

            using (StreamReader inputFile = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar  + Device.DefaultFilename))
            {
                //var xml = inputFile.ReadToEnd();

                using (XmlReader reader = XmlReader.Create(inputFile))
                {
                    try
                    {
                        var obj = xsSubmit.Deserialize(reader);

                        if (obj is List<Device>)
                            Device.Devices = obj as List<Device>;

						foreach(var d in Device.Devices)
						{
							if(d.MasterPin != 0)
								d.Pins.Add(d.MasterPin,false);//add master pin and default state to off
						}

                        if (IsVerbose)
                        {
                            Console.WriteLine("Loaded " + Device.Devices.Count + " devices from xml storage");
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public static void Add(Device device)
        {
            foreach(var d in Devices)
            {
                if (d.GUID == device.GUID)
                {
                    if (IsVerbose)
                        Console.WriteLine(d.GUID + " already exists in device list");
                    return;
                }
            }

            Devices.Add(device);

            if(IsVerbose)
            {
                Console.WriteLine(device.GUID + " has been added to device list");
            }

            Device.Save();
        }

        public static List<DeviceState> QueryState()
        {
            List<DeviceState> foundDevices = new List<DeviceState>();

            int tries = Device.MAX_RETRY;
            while (tries != 0)
            {
                tries--;//countdown

                #region Send
                //send message 
                // /STATE/

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                   ProtocolType.Udp);
                IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, PortNumber);

                sock.SetSocketOption(SocketOptionLevel.Socket,
                  SocketOptionName.Broadcast, 1);

                var data = Encoding.UTF8.GetBytes(Device.DEVICE_STATE);

                sock.SendTo(data, iep1);
                sock.Close();

                if (IsVerbose)
                {
                    Console.WriteLine("Sent message " + Device.DEVICE_STATE + " via UDP port " + PortNumber);
                }

                #endregion

                //wait for response
                //ACK/PIN_ON/{PIN_NUMBER}/1

                UdpClient udpClient = null;

                

                try
                {
                    var broadcastAddress = new IPEndPoint(IPAddress.Any, PortNumber);
                    udpClient = new UdpClient();
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.

                    udpClient.Client.Bind(broadcastAddress);

                    var localDevs = ReadStateResponse(udpClient, broadcastAddress, 1000);

                    if(localDevs != null && localDevs.Count > 0)
                    {
                        foundDevices.AddRange(localDevs);
                    }

                    if (IsVerbose)
                    {
                        Console.WriteLine("Found " + foundDevices.Count + " listening devices");
                    }

                    //return foundDevices;
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    System.Diagnostics.Debug.WriteLine(se.Message);
                }
                finally
                {
                    if (udpClient != null)
                        udpClient.Close();
                    udpClient = null;
                }

                //turn on master - TODO
            }

            //distinct

            return foundDevices.Distinct().ToList();
        }

        private static List<DeviceState> ReadStateResponse(UdpClient udpClient, IPEndPoint broadcastAddress, int waitTime = 1000)
        {
            var start = DateTime.Now;
            var results = new List<DeviceState>();

            while ((DateTime.Now) <= start.AddMilliseconds(waitTime))
            {
                udpClient.Client.ReceiveTimeout = waitTime;

                byte[] buffer = null;

                try
                {
                    buffer = udpClient.Receive(ref broadcastAddress);
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    System.Diagnostics.Debug.WriteLine(se.Message);
                }

                if (buffer != null)
                {
                    string resp = Encoding.UTF8.GetString(buffer);

                    if (IsVerbose)
                    {
                        Console.WriteLine("Received response " + resp);
                    }

                    //{DEVICE_ID}/STATE/{a},{b},{c}
                    if (resp.Contains("/STATE/") && resp.Length >= 48)
                    {
                        var state = ParseState(resp);
                        if (state != null)
                        {
                            results.Add(state);
                            if (IsVerbose)
                            {
                                Console.WriteLine("Found " + state.DeviceID);
                            }
                        }
                        else
                        {
                            if (IsVerbose)
                            {
                                Console.WriteLine("Failed to parse device response");
                            }
                        }
                    }
                    else
                    {
                        if (IsVerbose)
                        {
                            Console.WriteLine("Response does not contain /STATE/ and is not Greater Than or Equal To 48 characters " + resp.Length);
                        }
                    }
                }
                else
                {
                    if(IsVerbose)
                    {
                        Console.WriteLine("No Response");
                    }
                }
            }

            if(IsVerbose)
            {
                Console.WriteLine("Found " + results.Count + " devices");
            }

            return results;
        }

        private static DeviceState ParseState(string resp)
        {
            //{DEVICE_ID}/STATE/{a},{b},{c}
            var raw = resp.Substring(36);
            var data = raw.Replace("/STATE/", string.Empty);

            if(IsVerbose)
            {
                Console.WriteLine("Filtered Data " + data);
            }

            if (!data.Contains("STATE"))
            {
                var numbers = data.Split(',');
                uint pa = 0;
                uint pb = 0;
                uint pc = 0;

                uint.TryParse(numbers[0], out pa);
                uint.TryParse(numbers[1], out pb);
                uint.TryParse(numbers[2], out pc);

                DeviceState ds = new DeviceState();
                ds.PORTA = pa;
                ds.PORTB = pb;
                ds.PORTC = pc;
                ds.DeviceID = resp.Substring(0,36); 

                if(IsVerbose)
                {
                    Console.WriteLine("Device " + ds.DeviceID + " replied");
                }

                return ds;
            }
            else
            {
                if(IsVerbose)
                {
                    Console.WriteLine("Response does not contain STATE");
                }
            }

            return null;
        }

        #endregion
    }

    public class DeviceState
    {
        public uint PORTA { get; set; }
        public uint PORTB { get; set; }
        public uint PORTC { get; set; }
        public string DeviceID { get; set; }

        public Dictionary<int, bool> PinState
        {
            get {
                return getPinState();
            }
        }

        private Dictionary<int, bool> getPinState()
        {
            Dictionary<int, bool> blankPins = loadPins();

            //pin 1 = ??
            //blankPins[2] = IsBitSet(PORTB, 4);

            //pin 2 = PORTB, Bit 3
            blankPins[2] = IsBitSet(PORTB, 3);

            //pin 3 = PORTA, Bit 15
            blankPins[2] = IsBitSet(PORTA, 15);

            //pin 4 = PORTA, Bit 14
            blankPins[2] = IsBitSet(PORTA, 14);

            //pin 5 = PORTA, Bit 13
            blankPins[2] = IsBitSet(PORTA, 13);

            //pin 6 = PORTA, Bit 12
            blankPins[2] = IsBitSet(PORTA, 12);

            //pin 7 = PORTA, Bit 11
            blankPins[2] = IsBitSet(PORTA, 11);

            //pin 8 = PORTA, Bit 10
            blankPins[2] = IsBitSet(PORTA, 10);

            //pin 9 = PORTA, Bit 9
            blankPins[2] = IsBitSet(PORTA, 9);

            //pin 10 = PORTA, Bit 8
            blankPins[2] = IsBitSet(PORTA, 8);

            //pin 11 = PORTB, Bit 15
            blankPins[2] = IsBitSet(PORTB, 15);

            //pin 12 = PORTB, Bit 14
            blankPins[2] = IsBitSet(PORTB, 14);

            //pin 13 = PORTB, Bit 13
            blankPins[2] = IsBitSet(PORTB, 13);

            //pin 14 = PORTB, Bit 12
            blankPins[2] = IsBitSet(PORTB, 12);

            return blankPins;
        }
        private Dictionary<int, bool> loadPins()
        {
            Dictionary<int, bool> blankPins = new Dictionary<int, bool>();
            for (int i = 1; i <= 14; i++)
            {
                blankPins.Add(i, false);
            }
            return blankPins;
        }

        public override bool Equals(object obj)
        {
            var item = obj as DeviceState;

            if (item == null)
            {
                return false;
            }

            return this.DeviceID.Equals(item.DeviceID);
        }

        public override int GetHashCode()
        {
            return this.DeviceID.GetHashCode();
        }

        //0 - 7
        bool IsBitSet(uint b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
    }
}
