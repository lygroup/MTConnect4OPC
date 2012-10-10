using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MTConnectAgentCore;
using System.Net.NetworkInformation;

namespace MTConnectAgentSHDR
{
    public class AgentSHDR : Agent
    {
        public ShdrObj[] shdrobjs;
        public class ShdrObj
        {
            public int index;
            public int port;
            public String host;
            public String device;
            public int running;
            public IPHostEntry entry;
            public IPAddress addr;
            public IPEndPoint ipep;
            public int attempts;

            public Socket server = null;
            public NetworkStream ns = null;
            public StreamReader sr = null;
            public StreamWriter sw = null;
            public string messages;
            public int IsRunning() { return running; }

        };
        public Threadme threads;


        public String messages;
        private bool stopped;


        public AgentSHDR()
        {
            stopped = true;
        }


        bool FindString(string[] strArray, string findThisString)
        {
            int strNumber;
            int strIndex = 0;
            findThisString.Trim();
            for (strNumber = 0; strNumber < strArray.Length; strNumber++)
            {
                strIndex = strArray[strNumber].IndexOf(findThisString);
                if (strIndex >= 0)
                    return true;
            }
            return false;
        }

        public void Verify(String[] _devices, String[] _hosts, int[] _ports)
        {
            try
            {
                // This has to be started to parse the devices.xml file

                String[] xmldevices = data.getDevices();
                shdrobjs = new ShdrObj[_devices.Length];
                for (int i = 0; i < _devices.Length; i++)
                {

                    if (_hosts[i] == null || _hosts[i].Length < 1)
                        throw new AgentException("Illegal Host Name");
                    if (_devices[i] == null || _devices[i].Length < 1)
                        throw new AgentException("Illegal Device Name" + _devices[i]);
                    if (!FindString(xmldevices, _devices[i].Trim()))
                        throw new AgentException("Illegal Device Name" + _devices[i]);

                    ShdrObj shdrobj = new ShdrObj();
                    shdrobj.index = i;
                    shdrobj.port = _ports[i];
                    shdrobj.device = _devices[i].Trim();
                    shdrobj.host = _hosts[i].Trim();
                    shdrobj.running = 0;
                    shdrobj.ns = null;
                    shdrobj.sr = null;
                    shdrobj.sw = null;
                    shdrobj.attempts = 0;



                    shdrobj.entry = Dns.GetHostEntry(shdrobj.host);
                    shdrobj.addr = shdrobj.entry.AddressList[0];



                    shdrobjs[i] = shdrobj;

                }
            }
            catch (AgentException e)
            {
                throw e;
            }
        }

        public void Cycle(int k)
        {
            ShdrObj shdrobj = shdrobjs[k];
            String device = shdrobjs[k].device;
            String line = null;
           // shdrobj.messages = "";
            // this a reference 
            try
            {
                if (shdrobj.attempts > 0)
                {
                    shdrobj.attempts--;
                    Thread.Sleep(100);
                    return;

                }

                //if (!TestSHDRConnection(shdrobj.host))
                //{

                //    messages += "Ping failed for " + shdrobj.host + "\n";
                //    throw new Exception("Ping failed for " + shdrobj.host);

                //}

                if (shdrobj.ipep == null)
                {
                    shdrobj.ipep = new IPEndPoint(shdrobj.addr, shdrobj.port);
                }

                //bool bConnected = TestSocketConnection(ipep,  port, 100);
                //if (!bConnected)
                if (shdrobj.server == null)
                {
                    shdrobj.attempts = 30;

                    shdrobj.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    shdrobj.server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                    shdrobj.server.Connect(shdrobj.ipep);
                    shdrobj.attempts = 0;
                }

                if (shdrobj.ns == null)
                    shdrobj.ns = new NetworkStream(shdrobj.server);
                if (shdrobj.sr == null)
                    shdrobj.sr = new StreamReader(shdrobj.ns);
                if (shdrobj.sw == null)
                    shdrobj.sw = new StreamWriter(shdrobj.ns);

                shdrobj.ns.ReadTimeout = 200;
                shdrobj.running = 1;

                if (!shdrobj.ns.DataAvailable)
                {
                    shdrobj.messages += "No data: for " + shdrobj.host + "\n";
                    return;
                }

                try
                {
                    line = shdrobj.sr.ReadLine();
                }
                catch (TimeoutException e)
                {
                    shdrobj.messages += "TimeoutException: " + e.Message + " for " + shdrobj.host + "\n";
                    return;
                }


                String[] values = line.Split('|');
                String timestamp = values[0];
                String item = values[1];

                // Check if this is an Alarm. Alarms must be on one line.
                if (data.isAlarm(item, device) && values.Length >= 7)
                {
                    String code = values[2];
                    String nativeCode = values[3];
                    String severity = values[4];
                    String state = values[5];
                    String text = values[6];
                    //short StoreEvent(String timestamp, String deviceName, String name, String value, String partId, String workPieceId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
                    data.StoreEvent(timestamp, device, item, text, "", "", code, severity, nativeCode, state);
                }
                else
                {
                    String value;
                    for (int i = 1; i < values.Length; i += 2)
                    {
                        item = values[i];
                        value = values[i + 1];

                        try
                        {
                            if (data.isEvent(item, device))
                                data.StoreEvent(timestamp, device, item, value, "", "", null, null, null, null);
                            else
                                data.StoreSample(timestamp, device, item, value, "", "");
                        }
                        catch (AgentException e)
                        {
                            shdrobj.messages += item + "|" + value + " is ignored. " + e.Message + "\n";
                        }
                    }
                    //short StoreEvent(String timestamp, String deviceName, String name, String value, String partId, String workPieceId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
                }
                return;
            }



            catch (ThreadAbortException e)
            {
                shdrobj.messages += "ThreadAbortException: " + e.Message + " for " + shdrobj.host + "\n";

            }
            catch (SocketException e)
            {
                shdrobj.messages += "SocketException: " + e.Message + " for " + shdrobj.host + "\n";

            }
            catch (IOException e)
            {
                shdrobj.messages += "IOException: " + e.Message + " for " + shdrobj.host + "\n";
            }
            catch (Exception e)
            {
                shdrobj.messages += "Exception: " + e.Message + " for " + shdrobj.host + "\n";
            }

            /////////// agent stopped or exception incurred
            {
                if (shdrobj.server != null && shdrobj.server.Connected)
                {
                    shdrobj.server.Shutdown(SocketShutdown.Both);
                    shdrobj.server.Close();
                }
                shdrobj.server = null;

                if (shdrobj.sr != null)
                    shdrobj.sr.Close();
                if (shdrobj.sw != null)
                    shdrobj.sw.Close();
                if (shdrobj.ns != null)
                    shdrobj.ns.Close();
                shdrobj.sr = null;
                shdrobj.sw = null;
                shdrobj.ns = null;

                shdrobj.running = 0;
            }



        }

        public override void Start(int ipport)
        {
            try
            {
                base.Start(ipport);
            }
            catch (AgentException e)
            {
                throw e;
            }

        }
        public override void Stop()
        {
            base.Stop();

        }
        public static bool TestSocketConnection(IPEndPoint endPoint, int port, int timeoutMs)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // do not block - do not want to be forced to wait on (too- long) timeout 
                socket.Blocking = false;

                // initiate connection - will cause exception because we're not  blocking 
                socket.Connect(endPoint);
                return false;
            }
            catch (SocketException socketException)
            {
                // check if this exception is for the expected 'Would Block' error 
                if (socketException.ErrorCode != 10035)
                {
                    socket.Close();
                    // the error is not 'Would Block', so propogate the exception 
                    return false;
                }
                // if we get here, the error is 'Would Block' so just continue  execution   } 
                // wait until connected or we timeout 
                int timeoutMicroseconds = timeoutMs * 1000;
                if (socket.Poll(timeoutMicroseconds, SelectMode.SelectRead) == false)
                {
                    // timed out 
                    socket.Close();
                    return false;
                }

                // *** AT THIS POINT socket.Connected SHOULD EQUAL TRUE BUT  IS FALSE!  ARGH! 
                // set socket back to blocking 
                socket.Blocking = true;
                socket.Close();
                return true;
            }

        }

        // test an ip connection
        private bool TestSHDRConnection(string ipaddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingreply = ping.Send(ipaddress, 500);
                if (pingreply.Status != IPStatus.Success)
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }

    }
}
