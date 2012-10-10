//
// Logger.cs
//

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

namespace Utilities
{

    public class Logger
    {
        public static int FATAL = -1;
        public static int ERROR = 0;
        public static int WARNING = 1;
        public static int INFORMATION = 2;
        public static int DEBUG = 3;
        public static int HEAVYDEBUG = 4;

        public static int Fatal(string msg) { return LogMessage(msg, FATAL); }
        public static int Error(string msg) { return LogMessage(msg, ERROR); }
        public static int Warning(string msg) { return LogMessage(msg, WARNING); }
        public static int Info(string msg) { return LogMessage(msg, INFORMATION); }
       
        
        public static string GetDirectoryExe()
        {
            string exepath = Assembly.GetExecutingAssembly().Location;
            int n = exepath.LastIndexOfAny(new char[] { '\\' });
            exepath = exepath.Substring(0, n + 1);

            return exepath;
        }
        
        public static int tracelevel = 3;
        public static int debuglevel = 0;
        public static string debugfile = GetDirectoryExe() + "debug.txt";
        public static StreamWriter sw = new StreamWriter(Stream.Null);
        public static StreamWriter dumpsw = new StreamWriter(Stream.Null);
        static public void SetLogStream(StreamWriter _sw) { sw = _sw; }
        static public void SetDebugLevel(int level) { debuglevel = level; }
        static public void LogToFile(string msg)
        {
            LogMessage(msg, 0);
        }
        static public int LogMessage(string msg, int level)
        {
            if (level < tracelevel)
                System.Diagnostics.Trace.Write(msg);
            if (sw == null)
                return level;
            if (level > debuglevel)
                return level;
            sw.WriteLine(msg);
            sw.Flush();
            return level;
        }
        public static void RestartLog()
        {
            RestartLog(false);
        }
        public static void RestartLog(bool append)
        {
            FileMode nFileAccess = append ? FileMode.Append : FileMode.Create;
            if (!File.Exists(debugfile))
                nFileAccess = FileMode.Create;

            FileStream file = new FileStream(debugfile,
                 nFileAccess,
               FileAccess.Write,
               FileShare.ReadWrite);

            // Create a new stream to read from a file
            sw = new StreamWriter(file);
            // string dumpfile;
            //dumpfile = Utils.GetDirectoryExe() + "dump.txt"; ;
            //dumpsw = new StreamWriter(dumpfile);

            // write a line of text to the file
            sw.WriteLine(DateTime.Now);

        }
    }
}