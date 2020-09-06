using BeatlesBlog.SimConnect;
using System;
using UCLBFlightTracker.Hubs;

namespace UCLBFlightTracker
{
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
                await simHub.SendPositionObject(position);
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


}
