using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Xml;
using System.Data;
using System.Reflection;

using MTConnectAgentCore;
using Utilities;

namespace MTConnectAgentSimulator
{
    class SimulatedDevice
    {
        #region ThreadVariables
        int running;
        private Thread worker;  /// <summary>Holds the main thread to do the work.</summary>
        public bool _isDone; /// <summary>Just is the thread processing, not the final result. </summary>
        private int _Result; 
        #endregion


        #region ClassVariables
        public int index;
        public string deviceId;
        public string filter;
        public DataTable csvDataTable;
        public System.Diagnostics.Stopwatch oStopWatch = new System.Diagnostics.Stopwatch();
        public string szNCFilename;
        public MTConnectAgentCore.Agent agent;
        public Dictionary<string, string> dataitems=new Dictionary<string, string>();
        public Dictionary<string, string> mappings = new Dictionary<string, string>();
        public String[] headings;
        #endregion


        public int heartbeat;
        public static double dTimeDivisor;
        public static int nSimCycleTime;
        public SimulatedDevice()
        {
            worker = new Thread(doWork);
            heartbeat = 0;
        }
        public void doWork()
        {

            try
            {
                heartbeat = 0;
                _isDone = false; // Simply informs of processing state, not status.
                while (true)
                {
                    _Result = 0; // This is the state of the processing.
                    Cycle();
                    Thread.Sleep(10);
                    _Result = 1; // This is the state of the processing.

                }
            }

            // Catch this if the parent calls thread.abort()
            // So we handle a cancel gracefully.
            catch (ThreadAbortException e)
            {

                // We have handled the exception and will simply return without data.
                // Otherwise the abort will be rethrown out of this block.
                string msg = "ThreadAbortException: " + e.Message + "\n";
                Logger.LogMessage(msg, 0);
                Thread.ResetAbort();
                _Result = -1; // This is the state of the processing.
            }
            finally
            {
                _isDone = true; // Simply informs of processing state, not status.
            }
        }
        public void Start(MTConnectAgentCore.Agent _agent)
        {
            agent = _agent;
            ParseDevice();
            Init(Utils.GetDirectoryExe() + this.szNCFilename);
            worker.Start();

        }
        public void ParseDevice()
        {

            dataitems.Clear();

            XmlReader reader = XmlReader.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Devices.xml");
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(reader);

            // parse <Device sampleRate="10.0" name="Mazak1
            XmlElement root = xmldoc.DocumentElement;
            XmlNode anode = root.SelectSingleNode("//Device[@name='" + deviceId + "']");

            // <DataItem category="SAMPLE" id="id20122" name="Slod_percent
            XmlNodeList nodes = anode.SelectNodes("//DataItem");
            foreach (XmlElement elem in nodes)
            {
                if (elem.Attributes["category"] == null)
                    continue;
                if (elem.Attributes["name"] == null)
                    continue;
                if (elem.Attributes["category"].InnerText.ToLower() == "event")
                {
                    //type="ALARM" 
                    if (elem.Attributes["type"] != null && elem.Attributes["type"].InnerText.ToLower() == "alarm")
                        dataitems[elem.Attributes["name"].InnerText] = "Alarm";
                    else
                        dataitems[elem.Attributes["name"].InnerText] = "Event";

                }
                else
                {
                    dataitems[elem.Attributes["name"].InnerText] = "Sample";
                }

                // <DataItem category="SAMPLE"  *OR*
                // <DataItem category="EVENT" 
                // or  <DataItem category="EVENT" id="id20215" name="alarm" type="ALARM" /> 

                //category

            }

            //StringStream ss = new StringStream();
            //StreamWriter writer = new StreamWriter(ss);

        }

        public void Cancel()
        {
            worker.Abort();
        }

        public void Cycle()
        {
            oStopWatch.Reset();
            oStopWatch.Start();
            try
            {
                while (true)
                {
                    Thread.Sleep(nSimCycleTime);
                    if ((index + 1) > csvDataTable.Rows.Count)
                    {
                        index = 0;
                    }
                    index++;
                    DateTime t1 = Convert.ToDateTime(csvDataTable.Rows[index][0]);
                    DateTime t2 = Convert.ToDateTime(csvDataTable.Rows[index + 1][0]);
                    TimeSpan ts = t2 - t1;
                    if ((ts.TotalMilliseconds / dTimeDivisor) > oStopWatch.ElapsedMilliseconds)
                        continue;

                    System.Diagnostics.Debug.Print(DataTableUtils.DataRow2CSV(csvDataTable.Rows[index], ",", false) +"\n");

                    oStopWatch.Reset();
                    oStopWatch.Start();
                    String nowtimestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
                    //  and see if column name mapping exists
                    
                    //store sample or event - must lookup type in devices.xml
                    for (int i = 1; i < csvDataTable.Columns.Count; i++)
                    {
                        string colname = Convert.ToString(csvDataTable.Columns[i]);
                        if (!this.dataitems.ContainsKey(colname))
                            continue;
                        if (dataitems[colname] == "Sample")
                        {
                            agent.StoreSample(nowtimestamp, deviceId, colname, Convert.ToString(csvDataTable.Rows[index][i]), "", "");
                        }
                        else if (dataitems[colname] == "Event")
                        {
                            agent.StoreEvent(nowtimestamp, deviceId, colname, Convert.ToString(csvDataTable.Rows[index][i]), "", "", null, null, null, null);
                        }
                        else if (dataitems[colname] == "Alarm")
                        {
                            string[] items = Convert.ToString(csvDataTable.Rows[index][i]).Split(':');
                            // Problem need severity and state with alarm...
                            if (items.Count() < 3)
                                continue;
                            agent.StoreEvent(nowtimestamp, deviceId, colname, items[0], "", "", "", items[1], "", items[2]);
                        }

                    }
                }
                   

            }
            catch (Exception e)
            {
                string msg = "Exception: " + e.Message + "\n";
                Logger.LogMessage(msg, 3);
            }


        }
        public bool IsDone()
        {
            return _isDone;
        }


        public DataTable csvExToDataTable( string file, bool isAlreadyDefined)
        {
            DataTable csvDataTable = new DataTable(); ;
            //no try/catch - add these in yourselfs or let exception happen
            String[] csvData = File.ReadAllLines(file);

            //if no data in file ‘manually’ throw an exception
            if (csvData.Length == 0)
                return csvDataTable;

            index = 0;  //so we won’t take headings as data
            headings = csvData[0].Split(',');

            if (!isAlreadyDefined)
            {
                String[] types = csvData[1].Split(',');
               

                //for each heading
                for (int i = 0; i < headings.Length; i++)
                {
                    //replace spaces with underscores for column names
                    headings[i] = headings[i].Replace(" ", "_").Trim();

                    if (mappings.ContainsKey(headings[i]))
                        headings[i] = mappings[headings[i]];
                    else
                        headings[i] = headings[i].ToLower();

                    //add a column for each heading
                    if (types[i] == "")
                        types[i] = "String";
                    //csvDataTable.Columns.Add(headings[i], Type.GetType("System." + types[i]));
                    csvDataTable.Columns.Add(headings[i], Type.GetType("System.String"));
                }
            }
            //populate the DataTable
            for (int i = 1; i < csvData.Length; i++)
            {
                //create new rows
                DataRow row = csvDataTable.NewRow();
                string[] fields = csvData[i].Split(',');

                for (int j = 0; j < fields.Length; j++)
                    row[j] = fields[j];
                for (int j = fields.Length; j < headings.Length; j++)
                    row[j] = "";

                //add rows to over DataTable
                csvDataTable.Rows.Add(row);
            }

            csvDataTable.Columns["Alarm"].ColumnName = "AlarmText";
            csvDataTable.Columns.Add("alarm").Expression = "AlarmText + ':' +  AlarmSeverity  + ':' + AlarmState";

            //return the CSV DataTable
            return csvDataTable;

        }
        public void Init(string szNCFilename)
        {
            if (!File.Exists(szNCFilename))
                throw new Exception("File Does not exist");

            FileStream file = new FileStream(szNCFilename, FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite);

            // Create a new stream to read from a file
            StreamReader sr = new StreamReader(file);
            // return sr.ReadToEnd();
            csvDataTable = csvExToDataTable(szNCFilename, false);
            //DataRow[] rows = csvDataTable.Select(filter);
            System.Diagnostics.Debug.Print(DataTableUtils.DataTable2CSV(csvDataTable, ",", true));
            var data = csvDataTable.Select(filter);
            csvDataTable = data.CopyToDataTable<DataRow>();
            oStopWatch.Reset();
        }

    }
    /*
     * 
     * In the app.config file:
     * 
       <configSections>
       <section name="Devices" type="MTConnectAgentSimulator.BasicConfigurator, MTConnectAgentSimulator" />
        </configSections>
        <Devices>
        <!-->1) ===================================================================== </!-->
        <Device name="Mazak1"
                CsvFile="M25751-12-03-09.csv"
                model="VRX730"
         />
        </Devices> 
     * 
     *   //Get the current configuration file.
     *   System.Configuration.Configuration config =
     *   ConfigurationManager.OpenExeConfiguration(
     *   ConfigurationUserLevel.None) as Configuration;

     *   ConfigurationSection customSection = config.GetSection("Devices");
     *   string xml = customSection.SectionInformation.GetRawXml();
     *  xmlhandler.Setup(xml); 
  * */

    public sealed class BasicConfigurator : System.Configuration.IConfigurationSectionHandler
    {
         public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            List<SimulatedDevice> devices = null;
            string[] mapping;

            if (section == null) { return devices; }

            devices = new List<SimulatedDevice>();

            XmlNodeList xmldevices = section.SelectNodes("//Device");

            foreach (XmlElement elem in xmldevices)
            {
                try
                {
                    SimulatedDevice device = new SimulatedDevice();
                    device.deviceId = elem.GetAttribute("name");
                    device.szNCFilename = elem.GetAttribute("CsvFile");
                    device.filter = elem.GetAttribute("filter");

                    mapping = elem.GetAttribute("mapping").Split(',');
                    for (int i = 0; i < mapping.Count(); i++)
                    {
                       string[] dict = mapping[i].Split('=');
                       if (dict.Count() < 2)
                           continue;
                       device.mappings[dict[0].Trim()] = dict[1].Trim();
                    }

                    devices.Add(device);
                }
                catch (Exception e) 
                { 
                    Logger.LogMessage("Device Configuration Error " + e.Message, 2); 
                }
            }

            return devices;

        }
    }
}
