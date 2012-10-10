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
using System.Windows.Forms;
using System.Data;
using System.Reflection;

namespace Utilities
{

    public class Logger
    {
        public static int tracelevel = 3;
        public static int debuglevel = 0;
        public static string debugfile = Utils.GetDirectoryExe() + "debug.txt";
        public static StreamWriter sw = new StreamWriter(Stream.Null);
        public static StreamWriter dumpsw = new StreamWriter(Stream.Null);
        static public void SetLogStream(StreamWriter _sw) { sw = _sw; }
        static public void SetDebugLevel(int level) { debuglevel = level; }
        static public void LogToFile(string msg)
        {
            LogMessage(msg, 0);
        }
        static public void LogMessage(string msg, int level)
        {
            if (level > tracelevel)
                System.Diagnostics.Trace.WriteLine(msg);
            if (sw == null)
                return;
            if (level > debuglevel)
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
            FileMode nFileAccess =  append ? FileMode.Append : FileMode.Open;
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
        public static string GetLogContents()
        {
            try
            {
                FileStream file = new FileStream(debugfile, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);

                // Create a new stream to read from a file
                StreamReader sr = new StreamReader(file);
                return sr.ReadToEnd();
            }
            catch (Exception)
            {

            }
            return "Sorry, Get Log File Failed";
        }

    }
}