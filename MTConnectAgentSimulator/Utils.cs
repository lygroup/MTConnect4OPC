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

using MTConnectAgentCore;

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
    }

   
    public class WallClock
    {
        #region Variables
        DateTime currentTick;
        DateTime lastTick;
        TimeSpan increment;
        int nCurrentShift;
        static TimeSpan zero = new TimeSpan(0, 0, 0);
        public static List<TimeSpan> shifts = new List<TimeSpan>() { new TimeSpan(6, 30, 0), new TimeSpan(15, 0, 0), new TimeSpan(23, 0, 0) };
        //       { new TimeSpan(0, 0, 0), new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)};
        public static WallClock time;
        #endregion
        #region Methods
        public WallClock()
        {
            currentTick = System.DateTime.Now;
            lastTick = currentTick;
            increment = new TimeSpan(0, 0, 0);
        }
        public WallClock(DateTime _start, TimeSpan _increment)
        {
            currentTick = _start;
            lastTick = currentTick;
            increment = _increment;

        }
        public DateTime GetDateTime() { return currentTick; }

        public static void SetShifts(List<TimeSpan> starts)
        {
            shifts = starts;
        }
        public static void AddShift(int hour, int min, int sec)
        {
            TimeSpan t = new TimeSpan(hour, min, sec);
            for (int i = 0; i < shifts.Count - 1; i++)
                if (t < shifts[i])
                    throw new Exception("Invalid time shift ordering\n");
            shifts.Add(t);
        }
        public static int GetShiftNumber(DateTime dt)
        {
            TimeSpan currentTime = dt.TimeOfDay;
            for (int i = 0; i < shifts.Count - 1; i++)
            {
                if (shifts[i] <= currentTime && currentTime < shifts[i + 1])
                    return i;
            }
            return shifts.Count - 1;
        }
        public int GetCurrentShift()
        {
            return GetShiftNumber(currentTick);

        }
        public int Elapsed()
        {
            TimeSpan e = currentTick - lastTick;
            return (int)e.TotalMilliseconds;
        }
        public void IncrementClock()
        {
            lastTick = currentTick;
            if (increment == zero)
            {
                currentTick = System.DateTime.Now;
            }
            else
            {
                currentTick += increment;
            }
        }
        #endregion
    }

    //    //http://ip/deviceid/probe then get return deviceid
    //    public static String getFirst(String rawUrl)
    //    {
    //        int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
    //        if (index == -1)
    //            return rawUrl.Substring(1, rawUrl.Length - 1);
    //        else
    //            return rawUrl.Substring(1, index - 1);
    //    }


    //    public static String getSecond(String rawUrl)
    //    {
    //        int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
    //        if (index == -1)
    //            return null;
    //        else
    //            return rawUrl.Substring(index + 1);
    //    }
    //    public static XElement createDeviceXST()
    //    {
    //        return createXST("MTConnectDevices");
    //    }
    //    public static XElement createErrorXST()
    //    {
    //        return createXST("MTConnectError");
    //    }

    //    public static XElement createStreamXST()
    //    {
    //        return createXST("MTConnectStreams");
    //    }
    //    private static XElement createXST(String elementName)
    //    {
    //        XElement root = null;

    //        if (elementName.Equals("MTConnectStreams"))
    //        {
    //            root = new XElement(mtStreams + elementName,
    //            new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectStreams:0.9"));
    //        }
    //        else if (elementName.Equals("MTConnectDevices"))
    //        {
    //            root = new XElement(mtDevices + elementName,
    //            new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectDevices:0.9"));
    //        }
    //        else if (elementName.Equals("MTConnectError"))
    //        {
    //            root = new XElement(mtError + elementName,
    //           new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectError:0.9"));
    //        }

    //        return root;
    //    }

    //    public static string datePatt = "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz";
    //    public static string GetDateTime()
    //    {
    //        //2008-04-29T12:34:41-07:00
    //        return DateTimeOffset.Now.ToString(datePatt);
    //    }
    //}


    public class Utils
    {
        public static string ExeName() {  return Assembly.GetExecutingAssembly().Location;}
        public static string machineName = Environment.MachineName;
        public static string exeversion = Assembly.GetExecutingAssembly().ImageRuntimeVersion;

        public static string GetDirectoryExe() 
        { 
            string exepath =  Assembly.GetExecutingAssembly().Location;
            int n  = exepath.LastIndexOfAny(new char[] {'\\'});
            exepath = exepath.Substring(0, n+1);

            return exepath;
        }
        
 
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
    public class DataTableUtils
    {
        public static string PrintRows(DataRow[] rows, string label)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\n{0}", label);
            if (rows.Length <= 0)
            {
                return sb.ToString();
            }
            foreach (DataRow r in rows)
            {
                foreach (DataColumn c in r.Table.Columns)
                {
                    sb.AppendFormat("\t {0}", r[c]);
                }
                sb.AppendFormat("\n");
            }
            return sb.ToString();
        }

        public static String DataTable2HtmlTable(DataTable table)
        {
            StringBuilder dataString = new StringBuilder();
            try
            {
                dataString.Append("<h2>" + table.TableName.ToString() + "</h2>\n");
                dataString.Append("<table>");
                dataString.Append("<tr>");
                foreach (DataColumn column in table.Columns)
                    dataString.Append("<th>" + column.ToString() + "</th>");
                dataString.Append("</tr>\n");

                foreach (DataRow row in table.Rows)
                {
                    dataString.Append("<tr>");
                    foreach (DataColumn column in table.Columns)
                    {
                        dataString.Append("<td>" + row[column].ToString() + "</td>");
                    }
                    dataString.Append("</tr>\n");
                }
                dataString.Append("</table>");
            }
            catch (Exception ex)
            {
                dataString.Append(ex.Message);
            }
            return dataString.ToString();
        }

        public static void csvDataToDataTable(DataTable csvDataTable, String[] csvData, bool isRowOneHeader)
        {
            int i = 0;
            if (isRowOneHeader)
                i = 1;
            try
            {
                //populate the DataTable
                for (; i < csvData.Length; i++)
                {
                    //create new rows
                    DataRow row = csvDataTable.NewRow();

                    for (int j = 0; j < csvDataTable.Columns.Count; j++)
                    {
                        //fill them
                        row[j] = csvData[i].Split(',')[j];
                    }

                    //add rows to over DataTable
                    csvDataTable.Rows.Add(row);
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage(String.Format("Reading Archive File Error at line {0} {1}", i,  e.Message), -1);

            }
        }

        public static DataTable csvToDataTable(DataTable csvDataTable, string file, bool isRowOneHeader)
        {

            //no try/catch - add these in yourselfs or let exception happen
            String[] csvData = File.ReadAllLines(file);

            //if no data in file ‘manually’ throw an exception
            if (csvData.Length == 0)
                return csvDataTable;

            String[] headings = csvData[0].Split(',');
            int index = (isRowOneHeader)?  1 : 0;  //so we won’t take headings as data

#if createcolumns
            DataTable csvDataTable = new DataTable();


            String[] headings = csvData[0].Split(',');
            int index = 0; //will be zero or one depending on isRowOneHeader

            if (isRowOneHeader) //if first record lists headers
            {
                index = 1; //so we won’t take headings as data

                //for each heading
                for (int i = 0; i < headings.Length; i++)
                {
                    //replace spaces with underscores for column names
                    headings[i] = headings[i].Replace(" ", "_");

                    //add a column for each heading
                    csvDataTable.Columns.Add(headings[i], typeof(string));
                }
            }
            else //if no headers just go for col1, col2 etc.
            {
                for (int i = 0; i < headings.Length; i++)
                {
                    //create arbitary column names
                    csvDataTable.Columns.Add("col" + (i + 1).ToString(), typeof(string));
                }
            }
#endif

            //populate the DataTable
            for (int i = index; i < csvData.Length; i++)
            {
                //create new rows
                DataRow row = csvDataTable.NewRow();

                for (int j = 0; j < headings.Length; j++)
                {
                    //fill them
                    row[j] = csvData[i].Split(',')[j];
                }

                //add rows to over DataTable
                csvDataTable.Rows.Add(row);
            }

            //return the CSV DataTable
            return csvDataTable;

        }
        public static void DataTable2CSV(DataTable table, string filename, string seperateChar, bool isAppended)
        {
            StreamWriter sr = null;
            try
            {
                bool bFileExists = File.Exists(filename);
                sr = new StreamWriter(filename, false);
                string str = DataTable2CSV(table, seperateChar, isAppended);
                sr.Write(str);
            }
            catch (Exception )
            {
            }
            if (sr != null)
            {
                sr.Close();
            }
        }
        public static string DataTable2CSV(DataTable table, string seperateChar, bool isAppended)
        {
            StringBuilder builder = new StringBuilder();

            string seperator = "";
            if (!isAppended)
            {
                foreach (DataColumn col in table.Columns)
                {
                    builder.Append(seperator).Append(col.ColumnName);
                    seperator = seperateChar;
                }
            }
            builder.Append(Environment.NewLine);
            foreach (DataRow row in table.Rows)
            {
                seperator = "";
                foreach (DataColumn col in table.Columns)
                {
                    builder.Append(seperator).Append(row[col.ColumnName]);
                    seperator = seperateChar;
                }
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }
        public static string DataRow2CSV(DataRow row, string seperateChar, bool bHeader)
        {
            StringBuilder builder = new StringBuilder();
            DataTable table = row.Table;

            string seperator = "";

            if (bHeader)
            {
                foreach (DataColumn col in table.Columns)
                {
                    builder.Append(seperator).Append(col.ColumnName);
                    seperator = seperateChar;
                }
                builder.Append(Environment.NewLine);
            }

            seperator = "";
            foreach (DataColumn col in table.Columns)
            {
                builder.Append(seperator).Append(row[col.ColumnName]);
                seperator = seperateChar;
            }
            builder.Append(Environment.NewLine);
            return builder.ToString();
        }

        public static string DataRow2SqlInsert(DataRow row)
        {
            StringBuilder builder = new StringBuilder();
            DataTable table = row.Table;

            string seperator = "";
            foreach (DataColumn col in table.Columns)
            {
                Type type = row[col.ColumnName].GetType();
                builder.Append(seperator);
                if (type == Type.GetType("System.DateTime"))
                    builder.Append(String.Format("#{0}#", row[col.ColumnName]));
                else if (type == Type.GetType("System.String"))
                    builder.Append(String.Format("'{0}'", row[col.ColumnName]));
                else if (type == Type.GetType("System.Int16"))
                {
                    string s = Convert.ToString(row[col.ColumnName]);
                    if (s.Length > 0)
                        builder.Append(s);
                    else
                        builder.Append(String.Format("{0}", col.DefaultValue));
                }
                else
                    builder.Append(String.Format("{0}", row[col.ColumnName]));
                seperator = ",";
            }
            return builder.ToString();
        }

        public static DataTable ParseCSV(string path)
        {
            if (!File.Exists(path))
                return null;

            string full = Path.GetFullPath(path);
            string file = Path.GetFileName(full);
            string dir = Path.GetDirectoryName(full);

            //create the "database" connection string 
            string connString = "Provider=Microsoft.Jet.OLEDB.4.0;"
              + "Data Source=\"" + dir + "\\\";"
              + "Extended Properties=\"text;HDR=No;FMT=Delimited\"";

            //create the database query 
            string query = "SELECT * FROM [" + file + "]";  // add [] in case - or other special character

            //create a DataTable to hold the query results
            DataTable dTable = new DataTable();

            //create an OleDbDataAdapter to execute the query
            OleDbDataAdapter dAdapter = new OleDbDataAdapter(query, connString);

            try
            {
                //fill the DataTable
                dAdapter.Fill(dTable);
            }
            catch (InvalidOperationException /*e*/)
            { }

            dAdapter.Dispose();

            return dTable;
        }
        public static void ArchiveCsv(Dictionary<string, string> csv, string csvline)
        {
            // Create new excel csv file to save  timestamp, shift#, machine id, total power
            // find if file exists..
            bool bFileExists = File.Exists(csv["csvfilename"]);

            System.IO.TextWriter datalogfp = new System.IO.StreamWriter(csv["csvfilename"], true);
            // No spaces between columns or column names eng up with leading _
            if (!bFileExists)
                datalogfp.WriteLine(csv["header"]);

            datalogfp.Write(csvline);
            datalogfp.Close();
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
