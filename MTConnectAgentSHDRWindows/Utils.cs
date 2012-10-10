// This software was developed by U.S. Government employees as part of
// their official duties and is not subject to copyright. No warranty implied 
// or intended.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Data;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;
using System.Data.OleDb;
using System.Xml.XPath;
using System.Net;
using System.Net.Sockets;


namespace Utilities
{

    public class Util
    {
        public static string datePatt = "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz";
        public static string GetDateTime()
        {
            //2008-04-29T12:34:41-07:00
            return DateTimeOffset.Now.ToString(datePatt);
        }
        public static string GetDirectoryExe()
        {
            string exepath = Assembly.GetExecutingAssembly().Location;
            int n = exepath.LastIndexOfAny(new char[] { '\\' });
            exepath = exepath.Substring(0, n + 1);

            return exepath;
        }
        public static string machineName = Environment.MachineName;
        public static string exeversion = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
        public static String GetResourceStr(string id)
        {
            // DONT FORGET TO EMBED RESOURCE
            // Create the resource manager. 
            Assembly _assembly = Assembly.GetExecutingAssembly();
            // get a list of resource names from the manifest
            // string[] resNames = assembly.GetManifestResourceNames();

            //ResFile.Strings -> <Namespace>.<ResourceFileName i.e. Strings.resx> 
            //ResourceManager resman = new ResourceManager("StringResources.Strings", assembly); // Load the value of string value for 
            _assembly = Assembly.GetExecutingAssembly();
            // return resman.GetString(id);
            //"MyTextFile.txt"
            Stream ts = _assembly.GetManifestResourceStream(id);
            StreamReader _textStreamReader = new StreamReader(ts);
            return _textStreamReader.ReadToEnd();

        }
        public static void WriteHtmlFile(string filename, string contents)
        {
            try
            {
                System.IO.TextWriter tw = new System.IO.StreamWriter(filename);
                tw.Write(contents);
                tw.Close();
            }
            catch (Exception) { }
        }
        public static String VersionNumber()
        {
            // return  "Version:" + Assembly.GetExecutingAssembly().ImageRuntimeVersion ;;
            return "Version:" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        public Stream CreateRWFile(string path, bool append)
        {
            return new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000, FileOptions.SequentialScan);
        }

        public static bool UrlIsValid(string smtpHost)
        {
            bool br = false;
            try
            {
                IPHostEntry ipHost = Dns.Resolve(smtpHost);
                br = true;
            }
            catch (SocketException se)
            {
                br = false;
            }
            return br;
        }
    }


    public class Utils
    {
        public static string GetDirectoryExe() 
        { 
            string exepath =  Assembly.GetExecutingAssembly().Location;
            int n  = exepath.LastIndexOfAny(new char[] {'\\'});
            exepath = exepath.Substring(0, n+1);

            return exepath;
        }
        public static string machineName = Environment.MachineName;
        public static string exeversion =  Assembly.GetExecutingAssembly().ImageRuntimeVersion ;
        
 
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

        static public string ProgramVersioning()
        {
            Process p;

            //get the current process
            p = Process.GetCurrentProcess();

            DateTime timestamp = RetrieveLinkerTimestamp();
            string msg =  "MTC Agent Version " + timestamp;
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
                msg+=strNames[i];

            }
            //cleanup
            p.Close();
            p = null;
            return msg;
        }
    }
  
    public class IntervalSleep
    {
        public System.Diagnostics.Stopwatch oStopWatch = new System.Diagnostics.Stopwatch();
        public void Reset()
        {
            oStopWatch.Reset();
            oStopWatch.Start();
        }
        public void Sleep(int nRefreshDataInterval)
        {
            oStopWatch.Stop();
            // This is bad in that it will lose time = need exact interval
            long nWaitMilliseconds = nRefreshDataInterval - oStopWatch.ElapsedMilliseconds;
            if (nWaitMilliseconds > 0)
                Thread.Sleep((int)nWaitMilliseconds);
            oStopWatch.Reset();
            oStopWatch.Start();
        }

    }
}
