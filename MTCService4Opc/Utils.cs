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
using System.ServiceProcess;
using MTCService4Opc;

namespace Utilities
{
    public class Misc
    {
        public static String GetResourceStr(string id)
       {
           // Create the resource manager. 
           Assembly _assembly = Assembly.GetExecutingAssembly();
           StreamReader _textStreamReader;
           // get a list of resource names from the manifest
           // string[] resNames = assembly.GetManifestResourceNames();

           //ResFile.Strings -> <Namespace>.<ResourceFileName i.e. Strings.resx> 
           //ResourceManager resman = new ResourceManager("StringResources.Strings", assembly); // Load the value of string value for 
           _assembly = Assembly.GetExecutingAssembly();
          // return resman.GetString(id);
           //"MyTextFile.txt"
           Stream ts = _assembly.GetManifestResourceStream( id);
           _textStreamReader = new StreamReader(ts);
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
    }
    public class Logger
    {
        public const int FATAL =-1;
        public const int ERROR =0;
        public const int WARNING =1;
        public const int INFORMATION =3;
        public const int DEBUG = 5;
  
        public static int debuglevel = 0;
        public static string debugfile;
        public static StreamWriter sw = new StreamWriter(Stream.Null);
        public static StreamWriter dumpsw = new StreamWriter(Stream.Null);
        static public void SetLogStream(StreamWriter _sw) { sw = _sw; }
        static public void SetDebugLevel(int level) { debuglevel = level; }
        static public void Fatal(string msg) { LogMessage(msg, FATAL); }
        static public void Warning(string msg) { LogMessage(msg, WARNING); }
        static public void Error(string msg) { LogMessage(msg, ERROR); }
        static public void Debug(string msg) { LogMessage(msg, DEBUG); }
        static public void LogMessage(string msg, int level)
        {
            if (sw == null)
                return;
            if (((int) level) > debuglevel)
                return;
            sw.WriteLine(msg);
            sw.Flush();
        }
        public static void RestartLog()
        {
            RestartLog(false);
        }
        public static void RestartLog(bool append)
        {
            // create a writer and open the file
            debugfile = Utils.GetDirectoryExe() + "debug.txt"; ;
            sw = new StreamWriter(debugfile, append);
           // string dumpfile;
            //dumpfile = Utils.GetDirectoryExe() + "dump.txt"; ;
            //dumpsw = new StreamWriter(dumpfile);

            // write a line of text to the file
            sw.WriteLine(DateTime.Now);

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
}
