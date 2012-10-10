using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using MTConnectAgentCore;
using Utilities;

namespace NISTMtConnectSHDRAgent
{
    public static class Program
    {
        static public void ProgramVersioning()
        {
            Process p;

            //get the current process
            p = Process.GetCurrentProcess();

            DateTime timestamp = Form1.RetrieveLinkerTimestamp();
            Logger.LogMessage("MTC Agent Version " + timestamp, 0);
            //get all the dlls this class is using
            String[] strNames = new String[p.Modules.Count];
            int i = 0;

            foreach (ProcessModule module in p.Modules)
            {
                strNames[i] = "DLL " + module.ModuleName;
                strNames[i] += " Version:" + module.FileVersionInfo.FileVersion;
                strNames[i] += " Modified:" + File.GetLastWriteTime(module.FileName).ToShortDateString();
                i++;
            }
            Array.Sort(strNames);
            for (i = 0; i < strNames.Length; i++)
            {
                Logger.LogMessage(strNames[i], 0);

            }
            //cleanup
            p.Close();
            p = null;
        }

        //static public void OutputDebugString(string msg)
        //{
        //    StackTrace st = new StackTrace(false);
        //    string caller = st.GetFrame(1).GetMethod().Name;
        //    Debug.WriteLine(caller + ": " + msg);
        //            }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool running;
            Mutex mutex = new Mutex(true, "NISTMTConnectSHDRAgent", out running);

            if (!running)
            {
                Logger.RestartLog();
                Logger.LogMessage("Error - NISTMTConnectSHDRAgent Already Running!", 0);
                Application.Exit();
            }
            else
            {
                Application.Run(new Form1());
                mutex.ReleaseMutex(); //2
                Logger.LogMessage("Exit:" + DateTime.Now,0);
            }
        }
    }
}
