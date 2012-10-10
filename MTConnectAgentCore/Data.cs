using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Xml.Schema;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace MTConnectAgentCore 
{
    public class Data : IData
    {

        const int samplesize = 50;
        int buffersize;
        String sender;
        String instanceId;
        String version;
        XElement probe; //initialized from Devices.xml
        XElement streams;
        XElement datastorage;
        private long sequence;
        int minIndex;
        int bufferSizeCounter;
        XmlNamespaceManager namespaceManager;

        public Data()
        {
            sequence = 1;
            minIndex = 1;
            bufferSizeCounter = 0;
            
        }
        public String getSender()
        {
            return this.sender;
        }
        public String getVersion()
        {
            return this.version;
        }

        public short loadConfig()
        {
            try {
                //creating xml from file
                XmlReader reader = XmlReader.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Devices.xml");
                probe = XElement.Load(reader);
                XmlNameTable nameTable = reader.NameTable;
                namespaceManager = new XmlNamespaceManager(nameTable);
                namespaceManager.AddNamespace(MTConnectNameSpace.mtConnectPrefix, MTConnectNameSpace.mtConnectUriDevices);
                namespaceManager.AddNamespace(MTConnectNameSpace.mtConnectPrefix, MTConnectNameSpace.mtConnectUriStreams);
            }
            catch (Exception e)
            {
                throw new AgentException("Loading Devices.xml Failed.", e);
            }
            //validation of Devices.xml against MTConnectDevices.xsd
            Stream schemaPath = Assembly.GetExecutingAssembly().GetManifestResourceStream("MTConnectAgentCore.MTConnectDevices.xsd");
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(MTConnectNameSpace.mtConnectUriDevices, XmlReader.Create(new StreamReader(schemaPath)));
            
            XDocument doc1 = new XDocument(probe);
            bool errors = false;
            doc1.Validate(schemas, (sender, e) =>
            {
                errors = true;
                throw new AgentException("Validating Devices.xml Failed.", e.Exception);
            }, true);
            if (errors)
                return ReturnValue.ERROR; //validation failed

            XElement header = probe.Element("Header");
            try
            {
                this.buffersize = Int32.Parse(header.Attribute("bufferSize").Value);
            }
            catch (Exception e)
            {
                throw new AgentException("Devices.xml's Header bufferSize value can not be converted into an integer.", e);
            }

            this.sender = header.Attribute("sender").Value;
            this.version = header.Attribute("version").Value;
            this.instanceId = header.Attribute("instanceId").Value;
                 

            //create stream template based on probe
            streams = DataUtil.createStreams(probe, namespaceManager);
            datastorage = DataUtil.createDataStorage(probe); //clone of probe without its name space

            //StoreSample("2008-04-30T15:25:17-04:00", "Company IMTS MTConnect Demonstration", "Srpm", "value", "workPieceId", "partId");
            //StoreEvent("2008-04-30T15:25:17-04:00", "Company IMTS MTConnect Demonstration", "block", "value", "workPieceId", "partId", null, null, null,null);

            //datastorage.Save(@"C:\temp\data.xml");
            return ReturnValue.SUCCESS;
        
        }

        private void createStreamFromDataStorage(IEnumerable<XElement> _data, long count, long from, ref XElement _streamclone, Boolean current, String _deviceIdLookingFor )
        {
            lock (this)
            {
                if (_data.Count() > 0)
                {
                    //_data is DataItem elements
                    if (_data.ElementAt(0).Name.LocalName.Equals("DataItem"))
                        handleDataItems(_data, count, from, ref _streamclone, current, _deviceIdLookingFor);
                    else
                    {
                        foreach (XElement d in _data)
                        {
                            IEnumerable<XElement> dataItems = d.XPathSelectElements(".//DataItem"); //all <DataItem> elements one or more levels deep in the current context
                            handleDataItems(dataItems, count, from, ref _streamclone, current, _deviceIdLookingFor);
                        }
                    }
                }
            }
        }
         
        private void createStreamFromDataStorage(ref XElement _streamclone, Boolean current, String _deviceIdLookingFor)
        {
            //query from datastorage
            IEnumerable<XElement> dataItems = this.datastorage.XPathSelectElements("//DataItem");
            handleDataItems(dataItems, 100, 0, ref _streamclone, current, _deviceIdLookingFor);
        }
        private void handleDataItems(IEnumerable<XElement> dataItems, long count, long from, ref XElement _streamclone, Boolean current, String _deviceIdLookingFor)
        {
            
            foreach (XElement di in dataItems)
            {
                //name, id, type, and category are required
                String dataitem_type = di.Attribute("type").Value;
                String eleName = DataUtil.getDataElementName(dataitem_type); //change POSITION to Position
                String dataitem_name = di.Attribute("name").Value;
                String dataItem_id = di.Attribute("id").Value;
                String dataItem_category = di.Attribute("category").Value;

                XAttribute temp = null;
                String dataItem_subType =  null;
                if ( (temp = di.Attribute("subType")) != null)
                    dataItem_subType = temp.Value; //ACTUAL, etc..
                
                //for each data
                IEnumerable<XElement> dataElements = di.Elements("Data");
                if (dataElements.Count() != 0)
                {
                   
                    //prepare to add data from _streamclone
                    String deviceName = DataUtil.getDeviceName(di);
                    if (_deviceIdLookingFor != null && (_deviceIdLookingFor.Equals(deviceName) == false))
                        continue;
                    XElement deviceStream = _streamclone.XPathSelectElement("DeviceStream[@name='" + deviceName + "']");
                    XElement dataItemNameElement = di.Parent.Parent;
                    if (dataItemNameElement.Name.LocalName.Equals("Device"))
                        continue;
                    XElement componentStream = deviceStream.XPathSelectElement("ComponentStream[@component='" + dataItemNameElement.Name + "' and @name='" + dataItemNameElement.Attribute("name").Value + "']");
                    //query category Samples or Events to place data
                   
                    XElement samplesOrEvents = componentStream.Element(DataUtil.modifyString1(dataItem_category) + "s"); //get Samples or Events from SAMPLE or EVENT
                    if (current)
                    {
                        //Assume last one is most newest data
                        XElement d = dataElements.Last();
                        samplesOrEvents.Add(DataUtil.createData(eleName, dataitem_name, dataItem_subType, dataItem_id, d));
                    }
                    else //sample
                    {
                        if (from == 0)
                        {   //then from = first sequence number in the buffer
                            lock (this)
                            {
                                from = minIndex;
                            }
                        }
                        long to = from + count - 1;   
                        foreach (XElement d in dataElements)
                        {
                            if (count != -1 && from != -1) //no count and from provided 
                            {
                                XAttribute s = d.Attribute("sequence");
                                long sequence = Convert.ToInt64(s.Value);
                                if (sequence >= from && sequence <= to)
                                {
                                    //assume timestamp must exisit in data
                                    samplesOrEvents.Add(DataUtil.createData(eleName, dataitem_name, dataItem_subType, dataItem_id, d));
                                }
                            }
                            else
                            {
                                //added to _streamclone
                                samplesOrEvents.Add(DataUtil.createData(eleName,dataitem_name, dataItem_subType, dataItem_id, d));
                            }
                        }
                    }
                }
            }
          //  Debug.Print(_streamclone.ToString());

            //else
            //return _streamclone;

        }
      

      
        public short getCurrent(StreamWriter writer)
        {
            XElement streamclone = new XElement(streams);
            createStreamFromDataStorage(ref streamclone, true, null);
            return addHeaderAndXSTToSteram(streamclone, writer);
        }
        //http://127.0.0.1/devicename/sample
        public short getCurrentDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this);
            if (device == null) //check if device exist
                return ReturnValue.ERROR;

            XElement streamclone = new XElement(streams);
            createStreamFromDataStorage(ref streamclone, true, deviceId);
            return addHeaderAndXSTToSteram(streamclone, writer);
        }
        public short getCurrent(String xpath, StreamWriter writer)
        {
            return getCurrentDevice(null, xpath, writer);
        }
        //xpath starts with stream....
        public short getCurrentDevice(String deviceId, String xpath, StreamWriter writer)
        {
            if (deviceId != null) //check if device exist
            {
                XElement device = DataUtil.getDevice(probe, deviceId, writer, this);
                if (device == null)
                    return ReturnValue.ERROR;
            }
            return getCurrentOrStreamDevice(deviceId, xpath, -1, -1, writer, true);
        }
        
       
        //all stream
        public short getStream(StreamWriter writer)
        {
            XElement streamclone = new XElement(streams);
            createStreamFromDataStorage(ref streamclone, false, null);
            return addHeaderAndXSTToSteram(streamclone, writer);
        }
        public short getStreamDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this);
            if (device == null) //check if device exist
                return ReturnValue.ERROR;

            XElement streamclone = new XElement(streams);
            createStreamFromDataStorage(ref streamclone, false, deviceId);
            return addHeaderAndXSTToSteram(streamclone, writer);

        }
        public short getStream(String xpath, String from, String count, StreamWriter writer)
        {
            return getStreamDevice(null, xpath, from, count, writer);
        }
          //xpath starts with stream....
        public short getStreamDevice(String deviceId, String xpath, String from, String count, StreamWriter writer)
        {
            if (deviceId != null) //check if device exist
            {
                XElement device = DataUtil.getDevice(probe, deviceId, writer, this);
                if (device == null)
                    return ReturnValue.ERROR;
            }
            long fromNum, countNum; //Signed 64-bit integer
            if (from == null)
                fromNum = 0;
            else if (from.Trim().Equals(""))
                fromNum = 0;
            else
            {
                fromNum = Convert.ToInt64(from);
                if (fromNum < 0)
                {
                    Error.createError(this, Error.INVALID_PATH, "\"from\" in Sample Request is negative.").Save(writer);
                    return ReturnValue.ERROR;
                }
            }

            if (count == null)
                countNum = 100;
            else if ( count.Trim().Equals(""))
                countNum = 100;
            else
            {
                countNum = Convert.ToInt64(count);
                if (countNum < 0)
                {
                    Error.createError(this, Error.INVALID_PATH, "\"count\" in Sample Request is negative.").Save(writer);
                    return ReturnValue.ERROR;
                }
            }


            return getCurrentOrStreamDevice(deviceId, xpath, fromNum, countNum, writer, false);

        }

        private short getCurrentOrStreamDevice(String deviceId, String path, long from, long count, StreamWriter writer, Boolean current )
        {

            IEnumerable<XElement> xpathResults;
            XDocument doc = new XDocument(this.datastorage);
            if (path != null)
            {   ////http://ip/deviceId/sample?path=.... or //http://ip/deviceId/current?path=....

                try
                {
                    xpathResults = doc.XPathSelectElements(path, namespaceManager);
                }
                catch (System.Xml.XPath.XPathException e)
                {
                    Error.createError(this, Error.INVALID_PATH, e.Message).Save(writer);
                    return ReturnValue.ERROR;
                }
             }
            else  //http://ip/deviceId/sample?from=2&count=100 //then all datItem
            {
                xpathResults = doc.Document.XPathSelectElements("//DataItem", namespaceManager);
            }

            if (xpathResults.Count() == 0)
            {
                XElement streamclone = new XElement(streams);
                addHeaderAndXSTToSteram(streamclone, writer);
                return ReturnValue.SUCCESS;
            }
            else
            {
                XElement streamclone = new XElement(streams);
                createStreamFromDataStorage(xpathResults, count, from, ref streamclone, current, deviceId);
                addHeaderAndXSTToSteram(streamclone, writer);
                return ReturnValue.SUCCESS;
            }
        }
    
        private short addHeaderAndXSTToSteram(XElement streamclone, StreamWriter writer)
        {
            streamclone = DataUtil.trimStream(streamclone);
 //           Debug.Print(streamclone.ToString());

            XElement header = getHeader(getNextSequence(streamclone));
            XElement mtxst = Util.createStreamXST();
            mtxst.Add(header);
            mtxst.Add(streamclone);
            mtxst.Save(writer);
            return ReturnValue.SUCCESS;
        }
       
        public short getDebug(StreamWriter writer)
        {
            datastorage.Save(writer);
            return ReturnValue.SUCCESS;
        }

        //DAF 2008-07-31 Added 
        public short getVersion(StreamWriter writer)
        { 
            string version2 = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //string version = Application.ProductVersion.ToString();
            //string productid = Application.ProductName.ToString();
            //string companyna = Application.CompanyName.ToString();

            XmlTextWriter xw = new XmlTextWriter(writer);
            xw.Formatting = Formatting.Indented; // optional
            xw.WriteStartElement("MTConnectAgent");
            xw.WriteAttributeString("version", version2);
            //xw.WriteElementString("Product", productid);
            //xw.WriteElementString("Company", companyna);
            xw.WriteEndElement();
            return ReturnValue.SUCCESS;
        }

        public short getLog(StreamWriter writer)
        {
            string loginp = "DeviceEntry2.log";
            try
            {
                //System.Diagnostics.Process.Start(@"copy DeviceEntry.log DeviceEntry2.log /Y");   
                System.Diagnostics.Process.Start(@"CopyLog.bat");    //works
            }
            catch (Exception ex)        /*if an error ocurrs, show a warning*/
            {
                string st = ex.Message.ToString();
                DeviceEntry.LogToFile("getLog: copy failed " + ex);
            }

            try
            {
                FileStream file = new FileStream(loginp, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                
                // Create a new stream to read from a file
                StreamReader sr = new StreamReader(file);

                // Read contents of file into a string
                string loghead = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><MTConnectAgent>";
                string logdata = sr.ReadToEnd();
                string logends = "</MTConnectAgent>";
                DeviceEntry.LogToFile("getLog: Log file data  " + logdata);
                writer.Write(loghead + logdata + logends);
            }
            catch (Exception e)
            {
                Console.WriteLine("getLog:Error:" + e.Message);
                DeviceEntry.LogToFile("getLog:Error: " + e.Message);
                return ReturnValue.ERROR;
            }
            return ReturnValue.SUCCESS;
        }

        public short getConfig(StreamWriter writer)
        {
            string configini = "mtcagent.ini";
            try
            {
                FileStream file = new FileStream(configini, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                
                // Create a new stream to read from a file
                StreamReader sr = new StreamReader(file);

                // Read contents of file into a string
                string logdata = sr.ReadToEnd();
                //DeviceEntry.LogToFile("getConfig: Config file data  " + logdata);
                writer.Write(logdata);
            }
            catch (Exception e)
            {
                Console.WriteLine("getConfig:Error:" + e.Message);
                DeviceEntry.LogToFile("getConfig:Error: " + e.Message);
                return ReturnValue.ERROR;
            }
            return ReturnValue.SUCCESS;
        }
        //DAF 2008-07-31 End 

        //all probe
        public short getProbe(StreamWriter writer)
        {
            resetProbeHeader();
            probe.Save(writer);
            return ReturnValue.SUCCESS;
        }
        //xpath startsWtih probe....       
        public short getProbeDevice(String deviceId, StreamWriter writer)
        {
            XElement device = DataUtil.getDevice(probe, deviceId, writer, this);
            if (device != null)
            {
                //create new XElment Devices and put add device
                XElement newDevices = new XElement("Devices", device);
                XElement header = getProbeHeader();
                XElement mtxst = Util.createDeviceXST();
                mtxst.Add(header);
                mtxst.Add(newDevices);
                mtxst.Save(writer);
                return ReturnValue.SUCCESS;
            }
            else
                return ReturnValue.ERROR;
        }

        public String[] getDevices()
        {
            IEnumerable<XElement> devices = probe.XPathSelectElements("//Devices/Device");
            String[] names = new String[devices.Count()];
            int i = 0;
            foreach (XElement d in devices)
            {
                names[i++] = d.Attribute("name").Value;
            }
            return names;
        }

        private void resetProbeHeader()
        {
            probe.Element("Header").SetAttributeValue("creationTime", Util.GetDateTime());
            probe.Element("Header").SetAttributeValue("instanceId", instanceId);
        }

        private XElement getProbeHeader()
        {
            XElement header =
                new XElement("Header",
                    new XAttribute("creationTime", Util.GetDateTime()),
                    new XAttribute("instanceId", instanceId),
                    new XAttribute("sender", sender),
                    new XAttribute("bufferSize", buffersize),
                    new XAttribute("version", version)
                    );
            return header;
        }

        private int getNextSequence(XElement ele)
        {
            int nextSequence = 0;
            int temp;
            IEnumerable<XElement> sampledata = ele.XPathSelectElements("//Samples").Elements();
            foreach (XElement d in sampledata)
            {
                temp = Convert.ToInt32(d.Attribute("sequence").Value);
                if (temp > nextSequence)
                    nextSequence = temp;
            }

            IEnumerable<XElement> eventdata = ele.XPathSelectElements("//Events").Elements();
            foreach( XElement d in eventdata)
            {
                temp = Convert.ToInt32(d.Attribute("sequence").Value);
                if (temp > nextSequence)
                    nextSequence = temp;
            }
            nextSequence++; //increment one
            return nextSequence;

        }

        private XElement getHeader(int nextSequence)
        {
            XElement header =
                new XElement("Header",
                    new XAttribute("creationTime", Util.GetDateTime()),
                    new XAttribute("instanceId", instanceId),
                    new XAttribute("nextSequence", nextSequence.ToString()),
                    new XAttribute("sender", sender),
                    new XAttribute("bufferSize", buffersize),
                    new XAttribute("version", version)
                    );
            return header;
        }
        public bool isAlarm(String _name, String _deviceName)
        {
            XElement item = DataUtil.getDataItemFromName(datastorage, _name, _deviceName);
            if (item == null)
                return false;
            XAttribute attr = item.Attribute("type");
            if (attr != null && attr.Value.Equals("ALARM"))
            {
                return true;
            }
            return false;
        }

        public bool isEvent(String _name, String _deviceName)
        {
            XElement item = DataUtil.getDataItemFromName(datastorage, _name, _deviceName);
            if (item == null)
                return false;
            XAttribute attr = item.Attribute("category");
            if (attr != null && attr.Value.Equals("EVENT"))
            {
                return true;
            }
            return false;
        }

        //Machine API in directory called
        public short StoreEvent(String _timestamp, String _deviceName, String _dataItemName, String _value, String _workPieceId,  String _partId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state)
        {
            //TODO: timestamp must exist
            lock (this) {
                XElement datastorage_dataitem = StoreSampleEventCommon(_deviceName, _dataItemName, "EVENT");
                if (datastorage_dataitem.Attribute("type").Value.ToUpper().Equals("ALARM"))
                {
                    if (alarm_code == null)
                        throw new AgentException( "DataItem type is ALARM but alarm_code is not defined." );
                    if ( alarm_severity == null )
                        throw new AgentException( "DataItem type is ALARM but alarm_severity is not defined." );
                    if ( alarm_state == null )
                        throw new AgentException( "DataItem type is ALARM but alarm_state is not defined." );
                    if( alarm_nativecode == null)
                        throw new AgentException("DataItem type is ALARM but alarm_state is not defined.");

                    if (datastorage_dataitem.Elements().Count() > samplesize)
                    {
                        datastorage_dataitem.Elements().First().Remove();
                    }
                    
                    //all four alarm required attributes are populated
                    datastorage_dataitem.Add(DataUtil.createEventData(_timestamp, _partId, _workPieceId, sequence + "", alarm_code, alarm_severity, alarm_nativecode, alarm_state, _value));
                }
                else //not alarm 
                {
                   // alarm_code = Util.convertEmptyStringToNull(alarm_code);
                    if (alarm_code != null )
                        throw new AgentException("DataItem type is not ALARM but alarm_code is populated.", AgentException.E_BADALARMDATA);
                    //alarm_severity = Util.convertEmptyStringToNull(alarm_severity);
                    if (alarm_severity != null)
                        throw new AgentException("DataItem type is not ALARM but alarm_severity is populated.", AgentException.E_BADALARMDATA);
                    //alarm_state = Util.convertEmptyStringToNull(alarm_state);
                    if (alarm_state != null)
                        throw new AgentException("DataItem type is not ALARM but alarm_state is populated.", AgentException.E_BADALARMDATA);
                    //alarm_nativecode = Util.convertEmptyStringToNull(alarm_nativecode);
                    if (alarm_nativecode != null)
                        throw new AgentException("DataItem type is not ALARM but alarm_nativecode is populated.", AgentException.E_BADALARMDATA);

                    if (datastorage_dataitem.Elements().Count() > samplesize)
                    {
                        datastorage_dataitem.Elements().First().Remove();
                    }
                    datastorage_dataitem.Add(DataUtil.createEventData(_timestamp, _partId, _workPieceId, sequence + "", alarm_code, alarm_severity, alarm_nativecode, alarm_state, _value));
                    
                }
                sequence++;
                bufferSizeCounter++;
            }
            return ReturnValue.SUCCESS;
        }

        public short StoreSample(String _timestamp, String _deviceName, String _dataItemName, String _value, String _workPieceId, String _partId)
        {
            ////TODO: timestamp must exist
            lock (this)
            {
                XElement datastorage_dataitem = StoreSampleEventCommon(_deviceName, _dataItemName, "SAMPLE");

                if (datastorage_dataitem.Elements().Count() > samplesize)
                {
                    datastorage_dataitem.Elements().First().Remove();
                }

                datastorage_dataitem.Add(DataUtil.createSampleData(_timestamp, _value, _partId, _workPieceId, sequence + ""));

                sequence++;
                bufferSizeCounter++;
            }
            return ReturnValue.SUCCESS;
      
        }
        private XElement StoreSampleEventCommon( String _deviceName, String _dataItemName, String categoryType )
        {
            //CheckBufferSize();  // Tue 10/27/09 12:36:08 PM Removed since we truncate buffer sizes on insertion
            XElement datastorage_dataitem = DataUtil.getDataItemFromName(datastorage, _dataItemName, _deviceName);
            if (datastorage_dataitem == null)
                throw new AgentException("DataItem (name = \"" + _dataItemName + "\") for Device (name = \"" + _deviceName + "\") not found.", AgentException.E_INVALIDARG);

            String category = datastorage_dataitem.Attribute("category").Value;
            if (!category.Equals(categoryType))
            {
                if (category.Equals("SAMPLE"))
                    throw new AgentException("Use storeSample for DataItem (name = \"" + _dataItemName + "\") for Device (name = \"" + _deviceName + "\").  The DataItem category is " + category + ".", AgentException.E_BADCATEGORY);
                else if (category.Equals("EVENT"))
                    throw new AgentException("Use storeEvent for DataItem (name = \"" + _dataItemName + "\") for Device (name = \"" + _deviceName + "\").  The DataItem category is " + category + ".", AgentException.E_BADCATEGORY);
            }
            return datastorage_dataitem;    
        }
    

        //private void CheckBufferSize()
        //{
        //    if (bufferSizeCounter >= buffersize)
        //    {
        //        IEnumerable<XElement> alldata = datastorage.XPathSelectElements("//Data");
        //        var remove = from d in alldata where d.Attribute("sequence").Value == minIndex + "" select d;
                       
        //        foreach (var thedata in remove) //only one match
        //        {
        //            thedata.Remove();
        //            bufferSizeCounter--;
        //            minIndex++;
        //        }
        //    }

        //}

        public void ClearDataBuffer(string devicename)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("//Device[@name='{0}']//Data", devicename);
            // IEnumerable<XElement> alldata = datastorage.XPathSelectElements("//Data");
            IEnumerable<XElement> alldata = datastorage.XPathSelectElements(sb.ToString());
            alldata.Remove();
        }
    
        //DAF 2008-07-17 Added LogToFile
        static public void LogToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                "MTConnectAgentCore.log");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

    }
}
