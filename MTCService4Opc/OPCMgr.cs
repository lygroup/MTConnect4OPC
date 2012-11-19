//
// OPCMgr.cs
//

// This software was developed by U.S. Government employees as part of
// their official duties and is not subject to copyright. No warranty implied 
// or intended.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;

using MTCService4Opc;
using Utilities;
using MTConnectAgentCore;

namespace OpcLibrary
{
    /// <summary>
    /// This OPC client interface only pulls data from the OPC Server. 
    /// No callbacks for shutdown or data change are implemented.
    /// OPC shutdown is not supported for the siemens 840d OPC server anyway. It uses the regie com component.
    /// The pull-only interface simplies connection to devices not on a domain.
    /// </summary>
    public class OPCMgr
    {
         public class Symbol
        {
            public string name;
            public string opcalias;
            public string type;  // sample, event, alarm
            public string value;
            public int nKey;
            public bool bEnum;
        }
        #region Variables

        // MT Connect parent variables
        MTConnectAgentCore.Agent agent;
        public String device = "Siemens840D";

        //public Symbol[] symbols= new Symbol[1000];
        public System.Collections.Generic.List<Symbol> symbols = new System.Collections.Generic.List<Symbol>();

        // ConfigurationSettings AppSettings variables. In file: App.config in the exe path
        public string sIpAddress;                       // connection prefix: e.g., opca://localhost/
        public bool bSynchronous = true;               // use synchronous OPC communication (ReadGroup)
        public bool bAutoConnect;                      // attempt to connect automatically, even after disconnected?
        public string sOPCProgId;                      // OPC Server Program ID
        public int nPriority;                          // windows process priority
        public int nServerRetryPeriod = 5000;          // in milliseconds 
        public int nServerUpdatePeriod = 1000;
        public string sCNCProcessName;                 // exe name on remote cnc pc machine 
        public int nMTConnectPort;
        public string sUser;
        public string sPassword;
        public string sDomain;
        public string sOpcClsid;                        // clsid of OPC Server
        public AlarmMsgs alarms = new AlarmMsgs();

        // OPC Client-Server declarations
        public Hashtable updateditems;
        System.Guid clsid;
        private OpcServer _opcserver;
        private OpcGroup opcgroup;
        private OPCItemDef[] tags;
        private int[] handlesSrv = new int[2] { 0, 0 };
        string[] itemvalues;
        int[] aE;

        private string _status;
        private static int nElapsed = 0;           // timer elapsed for query server  period

        public IDictionary serveritems;             // OPC items in App.config file, server Group. 
        #endregion
        public OPCMgr(MTConnectAgentCore.Agent _agent, string _device, string ipaddress)
        {
            agent = _agent;
            device = _device;
            sIpAddress = ipaddress;
        }
        public enum LogLevel  { FATAL=-1, ERROR = 0, WARNING = 1,  INFORMATION = 3, DEBUG = 5 } ;
        public void LogMessage(string errmsg, LogLevel level)
        {
            Logger.LogMessage(errmsg, (int) level);
        }
        public int FindTagIndex(string name)
        {
            for(int i=0; i< symbols.Count; i++)
            {
              if(symbols[i].name == name)
                return i;
            }
            return -1;
        }
        public string FindTagValue(string name)
        {
            for(int i=0; i< symbols.Count; i++)
            {
              if(symbols[i].name == name)
                  return symbols[i].value;
            }
            return "";
        }
        /// <summary>
        /// Initialize Potential OPC Tag names to canonical ids (ints).
        /// </summary>
        public void Init()
        {
            // declarations-  not a savvy enough C# programmer not to just hard code array sizes
            updateditems = new Hashtable();
            itemvalues = new string[1000];
            tags = new OPCItemDef[1000];

            _status = "Reading OPC configuration";

            // Read App.config file for  OPC and Exe options
            try
            {
                // MTC4OPC Settings
                bSynchronous = Convert.ToBoolean(ConfigurationManager.AppSettings["Synchronous"]);
                nPriority = Convert.ToInt32(ConfigurationManager.AppSettings["ProcessPriority"]);
                bAutoConnect = Convert.ToBoolean(ConfigurationManager.AppSettings["AutoConnect"]);
                nServerUpdatePeriod = Convert.ToInt32(ConfigurationManager.AppSettings["ServerUpdatePeriod"]);
                nServerRetryPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["ServerRetryPeriod"]);
                

                // OPC Settings
                sOPCProgId = ConfigurationManager.AppSettings["OPCServer"];

                //n This works is obsolete by why risk it???
                serveritems = (System.Collections.IDictionary)ConfigurationSettings.GetConfig(sOPCProgId);

                sCNCProcessName = (string)serveritems["CNCProcessName"];
                sOpcClsid = (string)serveritems["OpcServerClsid"];
                clsid = new System.Guid(sOpcClsid);
                //myurl = new Opc.URL("opcda://" + sOPCMachine + "/" + sOPCProgId);

                 nMTConnectPort = Convert.ToInt32(ConfigurationManager.AppSettings["MTConnectPort"]);

                sUser = ConfigurationManager.AppSettings["User"];
                sPassword = ConfigurationManager.AppSettings["Password"];
                sDomain = ConfigurationManager.AppSettings["Domain"];

                string sRpcAuthzSrv = ConfigurationManager.AppSettings["RpcAuthzSrv"];
                string sRpcAuthnLevel = ConfigurationManager.AppSettings["RpcAuthnLevel"];
                string sRpcImpersLevel = ConfigurationManager.AppSettings["RpcImpersLevel"];
                // MyEnum oMyEnum = (MyEnum)Enum.Parse(typeof(MyEnum), "stringValue");
                OpcServer.eRpcAuthzSrv = (RpcAuthnSrv)Enum.Parse(typeof(RpcAuthnSrv), sRpcAuthzSrv);
                OpcServer.eRpcAuthnLevel = (RpcAuthnLevel)Enum.Parse(typeof(RpcAuthnLevel), sRpcAuthnLevel);
                OpcServer.eRpcImpersLevel = (RpcImpersLevel)Enum.Parse(typeof(RpcImpersLevel), sRpcImpersLevel);
                OpcServer.sUser = sUser;
                OpcServer.sPassword = sPassword;
                OpcServer.sDomain = sDomain;
                OpcServer.nSimpleOPCActivate = Convert.ToInt32(ConfigurationManager.AppSettings["SimpleOPCActivate"]);

                // Map OPC Server data names into canonical names/ids

                int i = 0;
                foreach (DictionaryEntry de in serveritems)
                {
                    Symbol symbol = new Symbol();
                    string id = (string)de.Key;
                    if (!id.StartsWith("Tag."))
                        continue;
                    id = id.Substring(4);

                    symbol.bEnum = false;
                    if (id.StartsWith("Enum."))
                    {
                        symbol.bEnum = true;
                        id = id.Substring(5);
                    }
                    if (id.StartsWith("Event."))
                    {
                        symbol.type = "Event";
                        id = id.Substring(6);
                    }
                    if (id.StartsWith("Sample."))
                    {
                        symbol.type = "Sample";
                        id = id.Substring(7);
                    }
                    if (id.StartsWith("Data."))
                    {
                        symbol.type = "Data";
                        id = id.Substring(5);
                    }
                    symbol.name = (string)id;
                    symbol.opcalias = (string)de.Value;
                    symbol.nKey = i;
                    // symbols[i] = symbol;
                    symbols.Add(symbol);

                    tags[i] = new OPCItemDef(symbol.opcalias, true, i, VarEnum.VT_EMPTY);


                    i++;
                }
                ChangeProcessPriority(nPriority);
                Array.Resize<OPCItemDef>(ref tags, i);
                Array.Resize<int>(ref handlesSrv, i);
                Array.Resize<string>(ref itemvalues, i);

                _opcserver = new OpcServer();

            }
            catch (Exception e)
            {
                LogMessage("App.config Initialization Error: " + e.Message, LogLevel.FATAL);
                throw e;
            };
        }
        /// <summary>
        /// OPC shutdown handler, although Siemens 840D does not issue on. Uses regie instead to notify
        /// shutdown.
        /// </summary>
        private void opcshutdown_handler(string reason)
        {
            Disconnect();
        }

        public bool IsConnected()
        {
            if (_opcserver == null)
                return false;
            return _opcserver.IsConnected();
        }
        public string Update()
        {
            string str = ReadStatus();
            if (bSynchronous)
                ReadGroup();
            return str;
        }
        // this was added 
        public void StoreEvent(string timestamp, string deviceName, string dataItemName, string value, string workPieceId, string partId, string alarm_code, string alarm_severity, string alarm_nativecode, string alarm_state)
        {
            try
            {
                agent.StoreEvent(timestamp, deviceName, dataItemName, value, workPieceId, partId, alarm_code, alarm_severity, alarm_nativecode, alarm_state);
            }
            catch (Exception e)
            {
                LogMessage("StoreEvent Exception: " + e.Message, LogLevel.INFORMATION);

                // If its bad MTConnect device data or similar break gracefully
                int hr = Marshal.GetHRForException(e);
                if (hr == AgentException.E_INVALIDARG || hr == AgentException.E_BADCATEGORY || hr == AgentException.E_BADALARMDATA)
                    return;
                throw e;
            }

        }
        public void StoreSample(string timestamp, string deviceName, string dataItemName, string value, string workPieceId, string partId)
        {
            try
            {
                agent.StoreSample(timestamp, deviceName, dataItemName, value, workPieceId, partId);
            }
            catch (Exception e)
            {
                LogMessage("StoreSample Exception: " + e.Message, LogLevel.INFORMATION);

                // If its bad MTConnect device data or similar break gracefully
                int hr = Marshal.GetHRForException(e);
                if (hr == AgentException.E_INVALIDARG || hr == AgentException.E_BADCATEGORY || hr == AgentException.E_BADALARMDATA)
                    return;
                throw e;
            }
        }

        public void Cycle()
        {
            try
            {
                if (agent == null)
                    throw new Exception("MTC Cycle no agent");

                if (IsConnected())
                {
                    String nowtimestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
                    agent.StoreEvent(DateTime.Now.ToString("s"), device, "power", "ON", null, null, null, null, null, null);
                   

                    string str = Update();
                    MtUpdate();
                    // update heartbeat to confirm socket not zombie
                    agent.StoreEvent(nowtimestamp, device, "heartbeat", nowtimestamp, "", "", null, null, null, null);
                    updateditems.Clear();
                    _status = str;
                    return;
                }
                agent.Data().ClearDataBuffer(device);
                StoreEvent(DateTime.Now.ToString("s"), device, "power", "OFF", null, null, null, null, null, null);
                nElapsed = nElapsed + nServerUpdatePeriod;
                if (nElapsed < nServerRetryPeriod)
                {
                    return;
                }

                // Connect to OPC Server and add MT Connect Agent
                if (bAutoConnect && nElapsed >= nServerRetryPeriod)
                {
                    _status = "Attempting to connect";
                    nElapsed = 0;
                    Connect();

                }
            }
            catch (Exception ex)
            {
                Disconnect();
                throw ex;

            }
        }
        public string Status()
        {
            return _status;
        }

        private void MtUpdate()
        {
            int i;
            String s = DateTime.Now.ToString("s");
            if (agent == null)
                throw new Exception("MTC Update no agent");
            
            // Update symbol values, then write to Agent
            foreach (DictionaryEntry de in updateditems)
            {
                try
                {
                    int key = (int)de.Key;
                    i = key;
                    String value = (String)itemvalues[key];
                    symbols[i].value = value;
                }
                catch (Exception e)
                {
                    LogMessage("MT Connect Update Value Save Error: " + e.Message, LogLevel.ERROR);
                }
            }

            foreach (DictionaryEntry de in updateditems)
            {
                try
                {
                    String datum;
                    int key = (int)de.Key;
                    i = key;

                    string timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
                    StoreEvent(timestamp, device, "heartbeat", timestamp, "", "", null, null, null, null);
                    
                    if (i >= symbols.Count())
                        continue;
                    //if (i >= symbols.GetLength(0))
                    //    continue;

                    LogMessage("MtUpdate Symbol " + symbols[i].name + " = " + symbols[i].value, LogLevel.INFORMATION );

                    // Lookup MT-Connect string equivalent to Canonical OPC Tag Id
                    String value = (String)itemvalues[key];

                    if (symbols[i].bEnum)
                    {
                        string tag = "Enum." + symbols[i].name + "." + value;
                        try
                        {
                            value = serveritems[tag].ToString();
                        }
                        catch (Exception )
                        {
                            value = (String)itemvalues[key];
                        }
                    }
                    symbols[i].value = value;

                    // Determine which type of MT Connect item the opc data is : event or sample
                    if (symbols[i].name == "alarm")
                    {
                        if (value == "0")
                        {
                            StoreEvent(s, device, "alarm", "", "", "", "OTHER", "CRITICAL", "0", "CLEARED");
                        }
                        else
                        {
                            string alarmNative = FindTagValue("alarmNative");
                            string sAlarmMessage="";
                            int lNativeCode;
                            // <add key="Tag.Event.alarmNative" value="/Nck/LastAlarm/textIndex" />
                            
                            // Look up native code
                            if (alarms.alarm.Keys.Contains(alarmNative))
                                sAlarmMessage = alarms.alarm[alarmNative];

                            bool flag = Int32.TryParse(alarmNative, out lNativeCode);
                            for (int j = 0; flag && j < 4; j++)
                            {
                                string fillText = FindTagValue("alarmField" + Convert.ToString(j + 1));
                                if (fillText.Length == 0 )
                                    continue;
                                if (fillText[0] == 'S' || fillText[0] == 'K')
                                    fillText = fillText.Substring(1);
                                sAlarmMessage = sAlarmMessage.Replace(Convert.ToString(j + 1)+ "%", fillText);
                            }
                            if (sAlarmMessage.Length > 0)
                                value = sAlarmMessage;
                            
                            StoreEvent(s, device, "alarm", value, "", "", "OTHER", "CRITICAL", "-1", "ACTIVE");
                        }

                    }
                    else if (symbols[i].type == "Event")
                    {

                        datum = (String)symbols[i].name;
                        StoreEvent(s, device, datum, value, null, null, null, null, null, null);

                    }
                    else if (symbols[i].type == "Sample")
                    {
                        datum = (String)symbols[i].name;
                        StoreSample(s, device, datum, value, "", "");

                    }
                }
                catch (Exception e)
                {
                    LogMessage("MT Connect Update Error: " + e.Message, LogLevel.ERROR);
                }
            }
            // clear tags update.
            updateditems.Clear();
        }
        /// <summary>
        /// This removes the OPC group iutem, opc group and disconnects and nulls the OPC server connection. 
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_opcserver == null)
                    return;
                if (!IsConnected())
                    return;

                if (opcgroup != null)
                {
                    opcgroup.RemoveItems(handlesSrv, out aE);
                    opcgroup.Remove(false);
                }
                _opcserver.Disconnect();
                opcgroup = null;
                _opcserver = null;
            }
            catch (Exception e)
            {
                LogMessage("OPC Disconnect Error: " + e.Message, LogLevel.ERROR);
                _opcserver = null;
            }
        }


        private static Array RemoveAt(Array source, int index)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (0 > index || index >= source.Length)
                throw new ArgumentOutOfRangeException("index", index, "index is outside the bounds of source array");
            Array dest = Array.CreateInstance(source.GetType().GetElementType(), source.Length - 1);
            Array.Copy(source, 0, dest, 0, index);
            Array.Copy(source, index + 1, dest, index, source.Length - index - 1);
            return dest;
        }

        public void Connect()
        {
            int i;
            try
            {

  
#if CREDENTIALS
                 LogMessage("Test if CNC Exe IsExecuting()", 1);
                if ((sCNCProcessName.Length > 0) && !IsExecuting(sOPCMachine, sCNCProcessName))
                {
                    throw new Exception("Cannot start OPC Server" + sCNCProcessName + " not running");

                }


                LogMessage("Connect to CNC", 1);

                // This is used as credentials to logon ont remote pc to see if CNC running
                System.Net.NetworkCredential credential;
                if (sDomain.Length > 0)
                    credential = new System.Net.NetworkCredential(sUser, sPassword, sDomain);
                else
                    credential = new System.Net.NetworkCredential(sUser, sPassword); // , sDomain);
#endif
                _opcserver = new OpcServer();

                LogMessage("Attempt OPC Server Connection", LogLevel.INFORMATION);
                // _opcserver will be null if failed...
                _opcserver.Connect(this.clsid, this.sIpAddress);

                LogMessage("Create OPC Group", LogLevel.DEBUG);
                opcgroup = _opcserver.AddGroup("OPCCSharp-Group", false, 900);

                if (opcgroup == null)
                    throw new Exception("Connect - AddGroup failed");

                // FIXME: this only works if OPC item exists.
                // Add items to OPC Group
                LogMessage("Add OPC Items", LogLevel.DEBUG);
                OPCItemResult[] itemresult;
                opcgroup.AddItems(tags, out itemresult);

                if (itemresult == null)
                    throw new Exception("Connect - OPC AddItems failed");

                LogMessage("Check OPC items for errors.", LogLevel.DEBUG);
                for (i = 0; i < itemresult.Length; i++)
                {
                    // If the OPC item failed - remove it from the tags to be updated.
                    if (HRESULTS.Failed(itemresult[i].Error))
                    {
                        LogMessage("OPC AddItems Error: " + tags[i].ItemID, LogLevel.DEBUG);
                        itemresult = (OPCItemResult[])RemoveAt(itemresult, i);
                        tags = (OPCItemDef[])RemoveAt(tags, i);
                        handlesSrv = (int[])RemoveAt(handlesSrv, i);
                        continue;

                    }
                    handlesSrv[i] = itemresult[i].HandleServer;
                }

                // read group
                LogMessage("OPC ReadStatus", LogLevel.DEBUG);
                ReadStatus();
                LogMessage("OPC ReadGroup", LogLevel.DEBUG);
                ReadGroup();
            }
            catch (Exception e)
            {
                LogMessage("OPC Connect Error: " + e.Message, LogLevel.FATAL);
                Disconnect();

            }
        }
        static private string[] opcStatus = 
        {   
         "",
        "OPC_STATUS_RUNNING",
        "OPC_STATUS_FAILED",
        "OPC_STATUS_NOCONFIG",
        "OPC_STATUS_SUSPENDED",
        "OPC_STATUS_TEST"
                                  };
        public string GetOpcStatusString(int n)
        {

            if (n < 0 || n >= opcStatus.Length)
                return "Error";
            return opcStatus[(int)n];
        }
        public string ReadStatus()
        {
            String str = String.Empty;
            if (_opcserver == null)
                return "OPC Server Not Connected";

            try
            {
                // FIXME : status is a memory leak this memory must be freed!
               SERVERSTATUS  status = _opcserver.GetServerStatus();

               LogMessage(sOPCProgId + "\n" + status.ToString(), LogLevel.DEBUG);
                //pStatus = System.Runtime.InteropServices.(status, typeof(SERVERSTATUS));
                //Marshal.DestroyStructure(pStatus, typeof(SERVERSTATUS));
                //Marshal.FreeCoTaskMem(pStatus);
            }
            catch (Exception e)
            {
                str = "ReadStatus Error: " + e.Message;
                throw new Exception(str);
            }
            finally
            {

            }
            return str;
        }

        public void ReadGroup()
        {
            int i;
            OPCItemState[] arrStat;
            if (!IsConnected())
                return;

            try
            {
                // Read opc group data
                opcgroup.Read(OPCDATASOURCE.OPC_DS_DEVICE, handlesSrv, out arrStat);

                updateditems.Clear(); // clear updated opc items list.
                for (i = 0; i < handlesSrv.Length; i = i + 1)
                {
                    updateditems.Add((int)tags[i].HandleClient, "");
                    itemvalues[(int)tags[i].HandleClient] = arrStat[i].DataValue.ToString();
                    LogMessage("Updated: Tag[" + tags[i].ItemID + "] = " + itemvalues[i], LogLevel.DEBUG);
                }
            }
            catch (Exception e)
            {
                Disconnect();
                throw new Exception("ReadGroup Error: " + e.Message);
            }
        }

        private void ChangeProcessPriority(int n)
        {
            if (n != (int)ProcessPriorityClass.AboveNormal &&
                n != (int)ProcessPriorityClass.BelowNormal &&
                  n != (int)ProcessPriorityClass.High &&
                  n != (int)ProcessPriorityClass.Idle &&
                   n != (int)ProcessPriorityClass.Normal)
                return;

            Process thisProc = Process.GetCurrentProcess();
            thisProc.PriorityClass = (System.Diagnostics.ProcessPriorityClass)n; // ProcessPriorityClass.High;

        }

        public bool IsExecuting(string remMachine, string procname)
        {
            if (procname == "")
                return true;
            return true;
            // This can work but is unnecessary complexity
#if CREDENTIALS
            ConnectionOptions co = new ConnectionOptions();
            if (sUser.Length > 0)
            {
                co.Username = remMachine + "\\" + sUser;
                co.Password = sPassword;
                co.Impersonation = ImpersonationLevel.Impersonate;
                co.EnablePrivileges = true;
            }

            // Beware! the account used to connect must have remote WMI privileges on the remote server. 
            // No domain, means its hard to authenticate properly
            ///   co.Username = "userid"; //any account with appropriate privileges 
            //    co.Password = "xxxx";
            ManagementPath p = new ManagementPath(@"\\" + remMachine + @"\root\cimv2");
            ManagementScope scope = new ManagementScope(p, co);
            scope.Connect();

            //SelectQuery selectQuery = new SelectQuery("SELECT * from Win32_Process");
            WqlObjectQuery selectQuery = new WqlObjectQuery("select * from Win32_Process where Name='" + procname + "'");

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, selectQuery))
            {
                foreach (ManagementObject proc in searcher.Get())
                {
                    //Console.WriteLine("{0}", proc["Name"].ToString());
                    if (string.Equals(procname, proc["Name"].ToString(), StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
#endif
        }
    }
}
