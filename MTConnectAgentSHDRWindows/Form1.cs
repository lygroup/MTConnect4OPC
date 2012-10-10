// 
// Form1.cs
//

#define GUI
#define RESTARTABLE

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MTConnectAgentCore;
using MTConnectAgentSHDR;
using System.Configuration;
using System.Net;
using System.Reflection;
using NISTMtConnectSHDRAgent.Properties;
using System.Net.Sockets;
using System.Diagnostics;
using Microsoft.Win32;
using System.Xml;
using Utilities;

namespace NISTMtConnectSHDRAgent
{
    public partial class Form1 : Form
    {
        private System.Drawing.Icon icnStarted;
        private System.Drawing.Icon icnStopped;
        AgentSHDR agent;
        System.Timers.Timer aTimer;
        int _heartbeat;
        bool state = false;

        // Configuration change monitoring
        DateTime timestamp;
        DateTime _configmodified;
        string _configfilename;

        // Configuration parameters
        string[] ipaddrs;
        string[] devices;
        string[] szportnumbers;
        int[] ports;
        int ipport=80;
        int _cycletime;
        int _dumpdata=0;
        int _debug;

        // This delegate enables asynchronous calls for setting
        // the richtext  on the form.
        delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
             //InvokeRequired required compares the thread ID of the
             //calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.Text = text;
            }
        }
        public Form1()
        {

            _heartbeat = 0;
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            icnStopped = Resources.Icon1;
            icnStarted = Resources.Icon2;

            timestamp = RetrieveLinkerTimestamp();
            Assembly _assembly;
            _assembly = Assembly.GetExecutingAssembly();


            // notifyIcon1.Text = "MTC Multi-SHDR Agent - Release " + _assembly.ImageRuntimeVersion ;
            notifyIcon1.Text = "MTC Agent Simulator - Release " + timestamp.ToLocalTime(); ;
            notifyIcon1.Icon = icnStopped;

            Logger.RestartLog();
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
           // System.Threading.Thread.Sleep(30000);

            MTConnectAgentCore.Configuration.defaultDirectory = Application.StartupPath + "\\";
            MTConnectAgentCore.Configuration.confDirectory = "" ;

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;

         
        }
        private void Form1_Load(object sender, EventArgs e)
        {
#if GUI
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
#endif
            // Make sure this event is handled in the Form so this is called!
            this.WindowState = FormWindowState.Minimized;

            //Hide();  // hide the window in the taskbar
            Reset();
        }

        void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionEndReasons.Logoff:
                    Logger.LogMessage("Log-off", -1); 
                    break;
                case SessionEndReasons.SystemShutdown:
                    Logger.LogMessage("Shutdown", -1); 
                    break;
            }
            Stop();
        }
        public short MyUserCommandDelegate(String deviceId, HttpListenerRequest request, StreamWriter writer)
        {
            try
            {
                string[] segments = request.Url.Segments; // {/,devicename,sample
                string command = "";
                if (segments.Length >1) //http://<IP>/reset or 
                {
                    if (segments.Length == 2 && segments[1] == "reset")
                    {
                        for (int i = 0; i < agent.shdrobjs.Length; i++)
                        {
                            agent.shdrobjs[i].Cancel();
                            agent.shdrobjs[i].Start();
                        }
                        command = "reset";
                    }
                    //http://<IP>/device/reset 
                    if (segments.Length == 3 && segments[2] == "reset" && deviceId != null)
                    {
                        ShdrObj shdrobj = agent.FindDevice(deviceId);

                        if (shdrobj != null)
                        {
                            shdrobj.Cancel();
                            shdrobj.Start();
                        }

                        command = "reset";
                    }
                    if (command == "reset")
                    {

                        XmlTextWriter xw = new XmlTextWriter(writer);
                        xw.Formatting = Formatting.Indented; // optional
                        xw.WriteStartElement("MTConnectAgent");
                        xw.WriteAttributeString("state", "resetting");
                        xw.WriteEndElement();
                        return ReturnValue.SUCCESS;
                    }
                }
            }
            catch (Exception e)
            {

            }

            return ReturnValue.ERROR;
        }

        public void Reset()
        {
            Stop();
            ReadConfigFile();

            agent = new AgentSHDR();
            agent.SetLogStream(Logger.sw);
            agent.SetDebugLevel(Logger.debuglevel);

            Start();
 
            aTimer.Interval = 2000; //  _cycletime;
            aTimer.AutoReset = false ;
            aTimer.Enabled = true;

        }
        public void Stop()
        {
            Logger.LogMessage("Agent Stopped", -1);
            aTimer.Enabled = false;
            if (agent == null || agent.shdrobjs == null)
                return;
            for (int i = 0; i < agent.shdrobjs.Length; i++)
            {
                agent.shdrobjs[i].Cancel();
            } 
            if (agent != null)
                agent.Stop();
            state = false;
            notifyIcon1.Icon = icnStopped;

        }

        public void Start()
        {
            state = false;
            try
            {
                agent.Start(ipport);
                // http server has to be created first, only happens after start
                agent.hst.userCommandDelegate += new UserCommandDelegate(MyUserCommandDelegate);
                agent.Verify(devices, ipaddrs, ports);
                for (int i = 0; i < agent.shdrobjs.Length; i++)
                {
                    agent.shdrobjs[i].Start();
                }
            }
            catch (AgentException exp)
            {
                String msg = exp.Message;
                if (exp.InnerException != null)
                    msg = msg + "\n" + exp.InnerException.Message;
                Logger.LogMessage(msg, 0);
                Stop();
                Application.Exit();
                Process.GetCurrentProcess().Kill();
            }
            state = true;
            notifyIcon1.Icon = icnStarted;

        }
        public static String VersionNumber()
        {
            String tmp;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            tmp = String.Format("Version: {0}", version);
            return tmp;
        }

        void ReadConfigFile()
        {
            // Application configuration settings
            try
            {

                _configfilename = Application.StartupPath + "\\NISTMtConnectSHDRAgent.exe.config"; ;
                FileInfo _configfileinfo = new FileInfo(_configfilename);
                _configmodified = _configfileinfo.LastWriteTime;

                // Use 
                System.Configuration.Configuration other = ConfigurationManager.OpenExeConfiguration(Application.StartupPath + "\\NISTMtConnectSHDRAgent.exe");

                devices = other.AppSettings.Settings["devices"].Value.Split(',');
                for (int i = 0; i < devices.Count(); i++) devices[i] = devices[i].Trim();
                ipaddrs = other.AppSettings.Settings["ipaddrs"].Value.Split(',');
                for (int i = 0; i < ipaddrs.Count(); i++) ipaddrs[i] = ipaddrs[i].Trim();
                szportnumbers = other.AppSettings.Settings["ports"].Value.Split(',');
                for (int i = 0; i < szportnumbers.Count(); i++) szportnumbers[i] = szportnumbers[i].Trim();


                string szipport = other.AppSettings.Settings["ipport"].Value;
                string milliseconds = other.AppSettings.Settings["cycletime"].Value;
                string dumpdata = other.AppSettings.Settings["dumpdata"].Value;
                string ReadTimeout = other.AppSettings.Settings["ReadTimeout"].Value;
                string szDebug = other.AppSettings.Settings["debug"].Value;

                ports = new int[szportnumbers.Length];
                for (int i = 0; i < szportnumbers.Length; i++)
                {
                    ports[i] = Convert.ToInt32(szportnumbers[i]);
                }

                ipport = 80;
                if (szipport != null)
                    ipport = Convert.ToInt32(szipport);

                if (dumpdata != null)
                    _dumpdata = Convert.ToInt32(dumpdata);

                _cycletime = 2000;
                if (milliseconds != null)
                    _cycletime = Convert.ToInt32(milliseconds);

                MTConnectAgentSHDR.ShdrObj.ReadTimeout = 600000;
                if (ReadTimeout != null)
                    MTConnectAgentSHDR.ShdrObj.ReadTimeout = Convert.ToInt32(ReadTimeout);

                if (szDebug != null)
                {
                    _debug = Convert.ToInt32(szDebug);
                    if (_debug > 0)
                        Logger.debuglevel = _debug;

                }
                if (_debug > 99)
                {
                    System.Diagnostics.Debugger.Break();
                }
                //NISTMtConnectSHDRAgent.Program.ProgramVersioning();
            }
            catch (Exception ex)
            {
                string msg = " Error " + ex.Message;
                Logger.LogMessage(msg, 0);
                Application.Exit();
                return;

            }

        }
 

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            String str;

            int i;
            try
            {
                _heartbeat++;
#if RESTARTABLE
                FileInfo fi = new FileInfo(_configfilename);
                if (fi.LastWriteTime != _configmodified)
               {
                   Stop(); //  timer1.Enabled = false;
                   Reset();
                   return;

                }
#endif

                str = "MTC Multi SHDR Agent Release: " + timestamp.ToLocalTime() + "\r\n";
                str += DateTime.Now + " Heartbeat:" + _heartbeat + "\r\n";

                for (i = 0; i < agent.shdrobjs.Length; i++)
                {
                    str += "\r\n\r\n" + agent.shdrobjs[i].host + " ";

                    if (agent.shdrobjs[i].IsRunning() == 1)
                        str += " running\r\n";
                    else
                        str += " not running\r\n";
                    string msg = agent.shdrobjs[i].messages;

                    if (msg != null)
                        str += msg.Replace("\n", "\r\n");

                    agent.shdrobjs[i].messages = "";
                }
#if GUI
                SetText(str);
#endif
                Logger.LogMessage(str, 6);


                // Dump current contents
                if (_dumpdata > 0)
                {
                    _dumpdata = 0;
                    agent.Data().getDebug(Logger.dumpsw);
                }
            }
            catch (Exception)
            {

            }
            aTimer.Start();
        }

        //private void contextMenuStrip1_MouseClick(object sender, MouseEventArgs e)
        //{
        //    this.WindowState = FormWindowState.Normal;
        //    Show();  // hide the window in the taskbar

        //}


        public static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }


        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //if (FormWindowState.Minimized == WindowState)
            //    Hide();

        }
        private void Form1_Unload(object sender, EventArgs e)
        {
            Stop();
            Application.Exit();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Start();

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stop();
            Application.Exit();

        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stop();

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

            currentToolStripMenuItem.Enabled = state;
            startToolStripMenuItem.Enabled = !state;
            stopToolStripMenuItem.Enabled = state;
            exitToolStripMenuItem.Enabled = true;

        }

        private void currentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string targetURL = @"http://127.0.0.1/current";
            System.Diagnostics.Process.Start(targetURL);
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Minimized;

            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Show();  // show the window in the taskbar

        }

    }
}
