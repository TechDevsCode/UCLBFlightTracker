using BeatlesBlog.SimConnect;
using BlazorApp1.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BlazorApp1.Data
{
    public class WeatherForecastService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
        {
            var rng = new Random();
            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray());
        }
    }



    public class UCLBClient : SimConnectClient
    {
        public Position currentPosition;
        private readonly SimHub simHub;

        public UCLBClient(SimHub simHub) : base("UCLB Client")
        {
            Client.OnRecvException += OnRecvException;
            Client.OnRecvEvent += OnRecvEvent;
            Client.OnRecvEventObjectAddremove += OnRecvEventObjectAddremove;
            Client.OnRecvSimobjectData += OnRecvSimobjectData;
            Client.OnRecvSimobjectDataBytype += OnRecvSimobjectDataBytype;
            this.simHub = simHub;
        }

        protected override void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            // call parent class for default behavior
            base.OnRecvOpen(sender, data);

            string simIdent = "MSFS";

            if (data.szApplicationName.Contains("Flight Simulator X"))
            {
                simIdent = "FSX";
            }
            else if (data.szApplicationName.Contains("ESP"))
            {
                simIdent = "ESP";
            }
            else if (data.szApplicationName.Contains("Prepar3D"))
            {
                simIdent = "P3D";
            }

            //ffUdp = ForeFlightUdp.Instance;
            //ffUdp.SetSimulator(simIdent);

            Client.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, typeof(Position));
            Client.RequestDataOnSimObjectType(Requests.TrafficEnumerate, 200000, SIMCONNECT_SIMOBJECT_TYPE.AIRCRAFT & SIMCONNECT_SIMOBJECT_TYPE.HELICOPTER, typeof(TrafficInfo));
            Client.SubscribeToSystemEvent(Events.ObjectAdded, "ObjectAdded");
            Client.SubscribeToSystemEvent(Events.SixHz, "6Hz");
        }

        private void OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            //Console.WriteLine($"OnRecvSimobjectDataBytype: {JsonConvert.SerializeObject(data, Formatting.Indented)}");
        }

        private async void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            try
            {
                var position = (Position)data.dwData;
                currentPosition = position;
                //simHub.Clients.All.SendAsync("Position", position.Latitude.ToString());
                await simHub.SendPosition(position.Latitude.ToString(), position.Longitude.ToString());
                await simHub.SendPositionObject(position);
                //simHub.SendMessage("New Position " + currentPosition.Latitude.ToString());
                //Console.WriteLine($"OnRecvSimobjectData: {JsonConvert.SerializeObject(data.dwData, Formatting.None)}");
            }
            catch (Exception)
            {
            }
        }

        private void OnRecvEventObjectAddremove(SimConnect sender, SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE data)
        {
            //Console.WriteLine($"OnRecvEventObjectAddremove: {JsonConvert.SerializeObject(data, Formatting.Indented)}");
        }

        private void OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            //Console.WriteLine($"OnRecvEvent: {JsonConvert.SerializeObject(data, Formatting.Indented)}");
        }

        private void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            //Console.WriteLine($"OnRecvException: {JsonConvert.SerializeObject(data, Formatting.Indented)}");
        }
    }


    public abstract class SimConnectClient : ObservableObject
    {
        protected readonly string ApplicationName;
        protected SimConnect Client;

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            private set { SetField(ref _connected, value); }
        }

        private string _simName;
        public string SimulatorName
        {
            get { return _simName; }
            private set { SetField(ref _simName, value); }
        }

        public SimConnectClient(string ApplicationName)
        {
            this.ApplicationName = ApplicationName;
            Client = new SimConnect();
            Client.OnRecvOpen += OnRecvOpen;
            Client.OnRecvQuit += OnRecvQuit;
        }

        ~SimConnectClient()
        {
            Uninitialize();
        }

        //-----------------------------------------------------------------------------

        public void Initialize()
        {
            PreInitialize();
            OpenConnection();
            PostInitialize();
        }

        public void Uninitialize()
        {
            PreUninitialize();
            CloseConnection();
            PostUninitialize();
        }

        //-----------------------------------------------------------------------------

        protected virtual void PreInitialize() { }
        protected virtual void PostInitialize() { }

        protected virtual void PreUninitialize() { }
        protected virtual void PostUninitialize() { }

        //-----------------------------------------------------------------------------

        private static int GetSimConnectXmlPort(string path, string protocol)
        {
            var doc = XDocument.Load(path);
            var comms = doc.XPathSelectElements("/SimBase.Document/SimConnect.Comm");
            foreach (var comm in comms)
            {
                if (comm.Element("Protocol").Value == protocol)
                {
                    return int.Parse(comm.Element("Port").Value);
                }
            }
            return 0;
        }

        private static bool IsLocalRunning
        {
            get { return LookupDefaultPortNumber("SimConnect_Port_IPv4") != 0 || LookupDefaultPortNumber("SimConnect_Port_IPv6") != 0; }
        }

        private static int LookupDefaultPortNumber(string ValueName)
        {
            string[] simulators = {
                @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator",
                @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft ESP",
                @"HKEY_CURRENT_USER\Software\LockheedMartin\Prepar3D",
                @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v2",
                @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v3",
                @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v4",
                @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator - Steam Edition"
            };

            foreach (var sim in simulators)
            {
                var value = (string)Microsoft.Win32.Registry.GetValue(sim, ValueName, null);
                if (!string.IsNullOrEmpty(value))
                {
                    var port = int.Parse(value);
                    if (port != 0) { return port; }
                }
            }

            string[] paths = {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\SimConnect.xml"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft Flight Simulator\SimConnect.xml"
                )
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                var protocol = ValueName.Substring(ValueName.Length - 4);
                var port = GetSimConnectXmlPort(path, protocol);
                if (port != 0) { return port; }
            }

            return 0;
        }

        //-----------------------------------------------------------------------------

        private void OpenConnection()
        {
            if (IsLocalRunning)
            {
                try
                {
                    Log.Instance.Info("Attempting SimConnect connection.");
                    Client.Open(ApplicationName);
                }
                catch (SimConnect.SimConnectException ex)
                {
                    Log.Instance.Warning(string.Format("Local connection failed.\r\n{0}", ex.ToString()));
                    try
                    {
                        bool ipv6support = System.Net.Sockets.Socket.OSSupportsIPv6;
                        Log.Instance.Info($"Attempting SimConnect connection ({(ipv6support ? "IPv6" : "IPv4")}).");
                        int scPort = LookupDefaultPortNumber(ipv6support ? "SimConnect_Port_IPv6" : "SimConnect_Port_IPv4");
                        if (scPort == 0) { throw new SimConnect.SimConnectException("Invalid port."); }
                        Client.Open(ApplicationName, null, scPort, ipv6support);
                    }
                    catch (SimConnect.SimConnectException innerEx)
                    {
                        Log.Instance.Error(string.Format("Local connection failed.\r\n{0}", innerEx.ToString()));
                    }
                }
            }
            else
            {
                Log.Instance.Warning("Flight Simulator must be running in order to connect to SimConnect.");
            }
        }

        private void CloseConnection()
        {
            Client.Close();
        }

        //-----------------------------------------------------------------------------

        protected virtual void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Log.Instance.Info(JsonConvert.SerializeObject(data, Formatting.Indented));
            Connected = true;
            SimulatorName = data.szApplicationName;
            var simVersion = $"{data.dwApplicationVersionMajor}.{data.dwApplicationVersionMinor}.{data.dwApplicationBuildMajor}.{data.dwApplicationBuildMinor}";
            var scVersion = $"{data.dwSimConnectVersionMajor}.{data.dwSimConnectVersionMinor}.{data.dwSimConnectBuildMajor}.{data.dwSimConnectBuildMinor}";
            Log.Instance.Info($"UCLB Connected to {data.szApplicationName}\r\n    Simulator Version:\t{simVersion}\r\n    SimConnect Version:\t{scVersion}");
        }

        protected virtual void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Connected = false;
            Log.Instance.Info("Flight Simulator disconnected.");
        }

        //-----------------------------------------------------------------------------

        // https://www.fsdeveloper.com/forum/threads/converting-adf-frequency.21264/post-444535
        // https://www.fsdeveloper.com/wiki/index.php?title=C:_Decimal_to_BCD
        protected ulong ToBCD(ulong input)
        {
            ulong a = 0UL;
            ulong result = 0UL;
            for (a = 0; input != 0; a++)
            {
                result |= (input % 10) << (int)(a * 4);
                input /= 10;
            }
            return result;
        }

        // https://www.fsdeveloper.com/wiki/index.php?title=C:_Decimal_to_BCO
        protected uint ToBCO(int xpndr)
        {
            return (uint)((xpndr % 10) + (((xpndr / 10) % 10) * 16) + (((xpndr / 100) % 10) * 256) + ((xpndr / 1000) * 4096));
        }

        // https://www.fsdeveloper.com/forum/threads/get-xpndr-code-in-external-app.8759/post-57481
        protected int FromBCO(uint xpndr)
        {
            return (int)((((xpndr & 0xf000) >> 12) * 1000) + (((xpndr & 0x0f00) >> 8) * 100) + (((xpndr & 0x00f0) >> 4) * 10) + (xpndr & 0x000f));
        }

        //-----------------------------------------------------------------------------
    }


    // https://rachel53461.wordpress.com/2011/05/08/simplemvvmexample/
    // https://stackoverflow.com/a/1316417
    // https://stackoverflow.com/a/35582811
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            VerifyPropertyName(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public virtual void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }
    }


}
