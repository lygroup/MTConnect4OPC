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
using System.Configuration;
using System.Net;
using System.Reflection;
using System.Net.Sockets;
using System.Diagnostics;
using Microsoft.Win32;
using System.Xml;
using Utilities;

using MTConnectAgentSimulator.Properties;

namespace MTConnectAgentSimulator
{
    public partial class Form1 : Form
    {
        List<SimulatedDevice> devices;
        Agent agent;


        private System.Drawing.Icon icnStarted;
        private System.Drawing.Icon icnStopped;
        System.Timers.Timer aTimer;
        int _heartbeat;
        bool state = false;
        DateTime  timestamp;

        // Agent Configuration parameters

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

            timestamp = Utils.RetrieveLinkerTimestamp();


            // notifyIcon1.Text = "MTC Multi-SHDR Agent - Release " + _assembly.ImageRuntimeVersion ;
            notifyIcon1.Text = "Autostart MTC SHDR Agent - Release " + timestamp.ToLocalTime(); ;
            notifyIcon1.Icon = icnStopped;

            Utilities.Logger.RestartLog();
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
           // System.Threading.Thread.Sleep(30000);

            MTConnectAgentCore.Configuration.defaultDirectory = Application.StartupPath + "\\";
            MTConnectAgentCore.Configuration.confDirectory = "" ;

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            Reset();

         
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


        public void Reset()
        {
            Stop();
            ReadConfigFile();

            agent = new Agent();

            Start();
 
            aTimer.Interval = 2000; //  _cycletime;
            aTimer.AutoReset = false ;
            aTimer.Enabled = true;

        }
        public void Stop()
        {
           Logger.LogMessage("Agent Stopped", -1);
            aTimer.Enabled = false;
            if (agent != null)
                agent.Stop();
            state = false;
            notifyIcon1.Icon = icnStopped;
            //if (agent == null || agent.shdrobjs == null)
            //    return;
            //for (int i = 0; i < agent.shdrobjs.Length; i++)
            //{
            //    agent.shdrobjs[i].Cancel();
            //}
        }

        public void Start()
        {
            state = false;
            try
            {
                agent.Start(ipport);
                for (int i = 0; i < devices.Count; i++)
                {
                    devices[i].Start(agent);
                }
                // http server has to be created first, only happens after start
                ////agent.hst.userCommandDelegate += new UserCommandDelegate(MyUserCommandDelegate);
                ////agent.Verify(devices, ipaddrs, ports);
                ////for (int i = 0; i < agent.shdrobjs.Length; i++)
                ////{
                ////    agent.shdrobjs[i].Start();
                ////}
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

                string szipport = System.Configuration.ConfigurationManager.AppSettings["ipport"];
                string milliseconds = System.Configuration.ConfigurationManager.AppSettings["cycletime"];
                string ReadTimeout = System.Configuration.ConfigurationManager.AppSettings["ReadTimeout"];
                string szDebug = System.Configuration.ConfigurationManager.AppSettings["debug"];
                SimulatedDevice.dTimeDivisor = Convert.ToDouble(System.Configuration.ConfigurationManager.AppSettings["TimeDivisor"]);
                SimulatedDevice.nSimCycleTime = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SimCycleTime"]);

                ipport = (szipport != null) ? Convert.ToInt32(szipport) : 80;
                _cycletime = (milliseconds != null) ? Convert.ToInt32(milliseconds) : 2000;
                _debug =  (szDebug != null) ? Convert.ToInt32(szDebug) : 0;


                ////MTConnectAgentSHDR.ShdrObj.ReadTimeout = 600000;
                ////if (ReadTimeout != null)
                ////    MTConnectAgentSHDR.ShdrObj.ReadTimeout = Convert.ToInt32(ReadTimeout);
                if (_debug > 99)
                {
                    System.Diagnostics.Debugger.Break();
                }

                // Get the current configuration file.
                System.Configuration.Configuration config =
                        ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) as System.Configuration.Configuration;

                //ConfigurationSection customSection = config.GetSection("Devices");
                //string xml = customSection.SectionInformation.GetRawXml();
                devices = (List<SimulatedDevice>)ConfigurationSettings.GetConfig("Devices");


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
            String str="";

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

                ////str = "MTC Multi SHDR Agent Release: " + timestamp.ToLocalTime() + "\r\n";
                ////str += DateTime.Now + " Heartbeat:" + _heartbeat + "\r\n";

                ////for (i = 0; i < agent.shdrobjs.Length; i++)
                ////{
                ////    str += "\r\n\r\n" + agent.shdrobjs[i].host + " ";

                ////    if (agent.shdrobjs[i].IsRunning() == 1)
                ////        str += " running\r\n";
                ////    else
                ////        str += " not running\r\n";
                ////    string msg = agent.shdrobjs[i].messages;

                ////    if (msg != null)
                ////        str += msg.Replace("\n", "\r\n");

                ////    agent.shdrobjs[i].messages = "";
                ////}
#if GUI
                SetText(str);
#endif
                Logger.LogMessage(str,2);

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




        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //if (FormWindowState.Minimized == WindowState)
            //    Hide();

        }
        private void Form1_Unload(object sender, EventArgs e)
        {
            Stop();
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

