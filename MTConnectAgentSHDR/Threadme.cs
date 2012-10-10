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
  

    public class ShdrObj
    {
        #region Variables
        public int index;
        public int port;
        public String host;
        public String device;
        public int running;
        public IPHostEntry entry;
        public IPAddress addr;
        public IPEndPoint ipep;
        public int attempts;
        public static int ReadTimeout = 600000;

        public Socket server = null;
        public NetworkStream ns = null;
        public StreamReader sr = null;
        public StreamWriter sw = null;
        public string messages;
        public int IsRunning() { return running; }
        public AgentSHDR agent;
        System.IO.StreamWriter rpmsw;


        /// <summary>Holds the main thread to do the work.</summary>
        private Thread worker;

        /// <summary>Just is the thread processing, not the final result.</summary>
        public bool _isDone;

        private int _Result;
        public int Result
        {
            get { return _Result; }
            protected set { _Result = value; }
        }
        #endregion

         /// <remarks>A proper dispose was not implemented???</remarks>

        public ShdrObj()
        {
            // Create a worker thread for the work.
            worker = new Thread(doWork);
 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Background is set so not to not prevent the
            // mainprocess from terminating it someone closes it.
            // This is vital.
            worker.IsBackground = true;
         }

        /// <summary>The work to be done on the thread.</summary>
        /// <remarks>Handles the thread abort exception if it is aborted!</remarks>
        public void doWork()
        {

            try
            {
                _isDone = false; // Simply informs of processing state, not status.
                while (true)
                {
                    //try
                    //{
                        _Result = 0; // This is the state of the processing.
                        Cycle();
                        Thread.Sleep(1000);
                        _Result = 1; // This is the state of the processing.
                    //}
                    //catch (Exception e)
                    //{
                    //    messages += "Thread Exception: " + e.Message + " for " + host + "\n";
                    //}
                }
            }

            // Catch this if the parent calls thread.abort()
            // So we handle a cancel gracefully.
            catch (ThreadAbortException e)
            {

                // We have handled the exception and will simply return without data.
                // Otherwise the abort will be rethrown out of this block.
                string msg = "ThreadAbortException: " + e.Message + " for " + host + "\n";
                messages += msg;
                AgentSHDR.LogMessage(msg,0);
                Thread.ResetAbort();
                _Result = -1; // This is the state of the processing.
            }
            finally
            {
                _isDone = true; // Simply informs of processing state, not status.
            }
        }
        public void Start()
        {
            worker = new Thread(doWork);
            worker.Start();

        }
        public void Cancel()
        {
            worker.Abort();
        }

        public void Cycle()
        {
            ShdrObj shdrobj = this;
            String device = this.device;
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

                if (shdrobj.ipep == null)
                {
                    shdrobj.ipep = new IPEndPoint(shdrobj.addr, shdrobj.port);
                }

                if (shdrobj.server == null)
                {
                    shdrobj.attempts = 30;

                    shdrobj.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //shdrobj.server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                    shdrobj.server.Connect(shdrobj.ipep);
                    shdrobj.attempts = 0;
                }

                if (shdrobj.ns == null)
                    shdrobj.ns = new NetworkStream(shdrobj.server);
                if (shdrobj.sr == null)
                    shdrobj.sr = new StreamReader(shdrobj.ns);
                if (shdrobj.sw == null)
                    shdrobj.sw = new StreamWriter(shdrobj.ns);
                
                //The default value, System.Threading.Timeout.Infinite, specifies that the read operation does not time out
                shdrobj.ns.ReadTimeout = ReadTimeout; // System.Threading.Timeout.Infinite=-1
                shdrobj.running = 1;

                if (!shdrobj.server.Connected)
                {
                    shdrobj.messages += "Server disconnected: for " + shdrobj.host + "\n";
                    throw new Exception("Server disconnected");

                }

                // Manually turn Power
               //String nowtimestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
                String nowtimestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
                agent.Data().StoreEvent(nowtimestamp, shdrobj.device, "power", "ON", "", "", null, null, null, null);

                while (true)
                {
                    AgentSHDR.LogMessage("Enter Readline" + shdrobj.host + "\n", 3);
                    line = shdrobj.sr.ReadLine();
                    AgentSHDR.LogMessage("Exit Readline" + shdrobj.host + "\n", 3);
                    nowtimestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);


                    if (line == null)
                    {
                        throw new Exception(shdrobj.host + ": Readline failed\n");
                    }


                    String[] values = line.Split('|');
                    String timestamp = values[0];
                    String item = values[1];

                    // update heartbeat to confirm socket not zombie
                    agent.Data().StoreEvent(nowtimestamp, shdrobj.device, "heartbeat", nowtimestamp, "", "", null, null, null, null);

                    // Check if this is an Alarm. Alarms must be on one line.
                    if (agent.Data().isAlarm(item, device) && values.Length >= 7)
                    {
                        String code = values[2];
                        String nativeCode = values[3];
                        String severity = values[4];
                        String state = values[5];
                        String text = values[6];
                        //short StoreEvent(String timestamp, String deviceName, String name, String value, String partId, String workPieceId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
                        agent.Data().StoreEvent(timestamp, device, item, text, "", "", code, severity, nativeCode, state);
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
                                if (agent.Data().isEvent(item, device))
                                {
                                    agent.Data().StoreEvent(timestamp, device, item, value, "", "", null, null, null, null);
                                }
                                else
                                {
                                    //if (item == "spindle_speed")
                                    //{
                                    //    sw.WriteLine("\n================================================================================");
                                    //    agent.Data().getCurrentDevice(null, "//Axes//DataItem[@type=\"SPINDLE_SPEED\"and@subType=\"ACTUAL\"]", rpmsw);
                                    //    sw.WriteLine("\n-----------------------------------------------");
                                    //    agent.Data().getStreamDevice(null,
                                    //        "//Axes//DataItem[@type=\"SPINDLE_SPEED\"and@subType=\"ACTUAL\"]",
                                    //        "0", "100", rpmsw);
                                    //    rpmsw.Flush();

                                    //    //System.Diagnostics.Debugger.Break();
                                    //}
                                    agent.Data().StoreSample(timestamp, device, item, value, "", "");
                                }
                            }
                            catch (AgentException e)
                            {
                                string msg = item + "|" + value + " is ignored. " + e.Message + "\n";
                                AgentSHDR.LogMessage(msg, 1);
                                shdrobj.messages += msg;
                            }
                        }
                        //short StoreEvent(String timestamp, String deviceName, String name, String value, String partId, String workPieceId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
                    }
                    // We need other threads to run
                    Thread.Sleep(50);
                }
                return;
            }


            catch (TimeoutException e)
            {
                string msg = "TimeoutException: " + e.Message + " for " + shdrobj.host + "\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,-1);
               return;
            }

            catch (ThreadAbortException e)
            {
                string msg = "ThreadAbortException: " + e.Message + " for " + shdrobj.host + "\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,-1);
                throw e;

            }
            catch (SocketException e)
            {
                string msg = "SocketException: " + e.Message + " for " + shdrobj.host + "\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,1);

            }
            catch (IOException e)
            {
                string msg =  "IOException: " + e.Message + " for " + shdrobj.host + "\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,1);
            }
            catch (Exception e)
            {
                string msg = "Exception: " + e.Message + " for " + shdrobj.host + "\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,1);
            }

            /////////// agent stopped or exception incurred
            try
            {
                if (shdrobj.server != null && shdrobj.server.Connected)
                    shdrobj.server.Shutdown(SocketShutdown.Both);

                if (shdrobj.server != null)
                    shdrobj.server.Close();

                if (shdrobj.server.Connected)
                {
                    AgentSHDR.LogMessage("Winsock shutdown close error: " + Convert.ToString(System.Runtime.InteropServices.Marshal.GetLastWin32Error()),1);
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
                string timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);

                agent.Data().ClearDataBuffer(shdrobj.device);
                agent.Data().StoreEvent(timestamp, shdrobj.device, "power", "OFF", "", "", null, null, null, null);

            }
            catch (Exception e)
            {
                string msg = "Exception: " + e.Message + " exception  while resetting MTC Agent" + shdrobj.host + "SHDR socket connection\n";
                shdrobj.messages += msg;
                AgentSHDR.LogMessage(msg,1);

            }
        }

        /// <summary>Show the operation messages generated.</summary>
        public List<string> Messages
        {
            get
            {
                List<string> retMessages = new List<string>();
                lock (locker)
                {
                    if (_myQueue.Count > 0)
                        retMessages.AddRange(_myQueue);

                    _myQueue.Clear();
                }

                return retMessages;
            }
        }



        #region Public Properties / Operations
        /// <summary>Wait for the thread to finish.</summary>
        /// <returns>True if finished normally, false if timeout</returns>
        public bool WaitForFinish()
        {
            return worker.Join(5000);
        }

        /// <summary>Optional method to check for operations done in an
        /// aschronous mode.</summary>
        /// <returns>True if thread is done or false if it is not.</returns>
        /// <remarks>Check value of the result if it is -1, then the operation
        /// was canceled.</remarks>
        public bool IsDone()
        {
            return _isDone;
        }


        /// <summary>Cancel thread operations here return -1.</summary>

        /// <summary>Access the queue for the operational messages</summary>
        public Queue<string> TheQueue
        {
            get
            {
                lock (locker)
                    return _myQueue;
            }

        }
        /// <summary>Acquire result value here, check <see cref="IsDone"/> to 
        /// verify if work is done during asynchrous access.</summary>
        /// <remarks>-1 signals no operational work complete.</remarks>


        /// <summary>Push messages onto the queue.</summary>
        /// <param name="msg">Message to push</param>
        public void SetMessages(string msg)
        {
            TheQueue.Enqueue(msg);
        }
        #endregion

        #region Variables

        /// <summary> This will lock the queue for safety. </summary>
        private object locker = new object();



        private Queue<string> _myQueue = new Queue<string>();
        #endregion

    }


}
