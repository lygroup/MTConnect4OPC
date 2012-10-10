using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections;
using System.Xml.XPath;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace MTConnectAgentCore
{

    public static class DataUtil
    {
        public static XElement getDevice(XElement probe, String deviceId, StreamWriter writer, IData data)
        {
            
            XElement devices = probe.XPathSelectElement("//Devices");
            if (devices != null)
            {
                XElement device = devices.XPathSelectElement("//Device[@name='" + deviceId + "']");
                if (device == null)
                {
                    Error.createError(data, Error.NO_DEVICE).Save(writer);
                    return null;
                }
                else
                    return device;
            }
            else //Devices not found from Devices.xml load at initialization
            {
                Error.createError(data, Error.INTERNAL_ERROR).Save(writer);
                return null;
            }
           

        }
       
      
        internal static String[] return_INVALID_URI_Error(IData data, StreamWriter writer, String extra_info)
        {
            Error.createError(data, Error.INVALID_URI, extra_info).Save(writer);
            return null;
        }
        //Remove ConponentStream if its Samples or Events has no child elements
        internal static XElement trimStream(XElement _s)
        {
            IEnumerable<XElement> componentStreams = _s.XPathSelectElements("//ComponentStream");
            int s_counter = 0;
            int e_counter = 0;
           // Debug.Print(_s.ToString());

            for ( int i = 0; i < componentStreams.Count(); i++ )
            {
                e_counter=s_counter = 0;
                XElement cs = componentStreams.ElementAt(i);
                XElement samples = cs.Element("Samples");
                if (samples != null)
                {
                    IEnumerable<XElement> elements = samples.Elements();
                    if (elements == null)
                    {
                        s_counter = 0;

                    }
                    else if (elements.Count() == 0)
                    {
                        s_counter = 0;
                    }
                    else
                        s_counter = samples.Elements().Count();
                }
                XElement events = cs.Element("Events");
                if (events != null)
                {
                    IEnumerable<XElement> elements = events.Elements();
                    if (elements == null)
                    {
                        e_counter = 0;

                    }
                    else if (elements.Count() == 0)
                    {
                        e_counter = 0;
                    }
                    else 
                        e_counter = events.Elements().Count();
                }

                if (s_counter == 0 && e_counter == 0)
                {
                    cs.Remove();
                    i--;
                }
            }

            //Debug.Print(_s.ToString());
         
            IEnumerable <XElement> devices = _s.XPathSelectElements("//DeviceStream");
            for ( int i = 0; i < devices.Count(); i++ )
            {
                XElement device = devices.ElementAt(i);
                if (!device.HasElements)
                {
                    device.Remove();
                    i--;
                }
            }


           // Debug.Print(_s.ToString());

            return _s;
        }


        //internal static XElement getNewest(IEnumerable<XElement> _data)
        //{
        //    //Compares two DateTimeOffset objects and indicates whether the first is earlier than the second, equal to the second, or later than the second.
        //    if (_data.Count() == 0)
        //        return null;
        //    else
        //    {
        //        XElement maxElement = _data.ElementAt(0);
        //        String maxtimespan = maxElement.Attribute("timestamp").Value;
        //        DateTimeOffset max = (DateTimeOffset)Util.ConvertToDateTime(maxtimespan);
        //        for (int i = 1; i < _data.Count(); i++)
        //        {
        //            String temptimespan = _data.ElementAt(i).Attribute("timestamp").Value;
        //            DateTimeOffset temp = (DateTimeOffset) Util.ConvertToDateTime(temptimespan);
        //            if (max.CompareTo(temp) < 0) //first is earlier than second
        //            {
        //                max = temp;//reset
        //                maxElement = _data.ElementAt(i);
        //            }
        //        }

        //        return maxElement;
        //    }
        //}
        internal static String getDataElementName(String _type)
        {
            //POSITION_XXX to Position_Xxx
            String name = "";
            String[] parts = _type.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                name += (parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1).ToLower());
            }
            return name;
        }
        internal static XElement getComponentFromDevice(XElement _probe, String _devicename, String _component, String _componentName)
        {
            IEnumerable<XElement> devices = _probe.XPathSelectElements("//Devices/Device[@name='" + _devicename + "']");
            if (devices == null)
                return null;
            else
            {
                return getComponent(devices.ElementAt(0), _component, _componentName);
            }

        }
        // find component based on component and componentname recursively
        //first _ele must be Probe Device
        internal static XElement getComponent(XElement _ele, String _component, String _componentName)
        {
            IEnumerable<XElement> componentsList = _ele.XPathSelectElements("Components");
            foreach (XElement componentsElement in componentsList)
            {
                foreach (XElement ctemp in componentsElement.Elements())
                {
                    String ename = ctemp.Name.LocalName;
                    if (!ename.Equals("DataItems"))
                    {
                        if (ename.Equals(_component) && ctemp.Attribute("name").Value.Equals(_componentName))
                            return ctemp;
                        else
                        { 
                            XElement r;
                            if ((r = getComponent(ctemp, _component, _componentName)) != null)
                                return r;
                        }
                            
                    }
                }
            }
            return null;
        }
      
        internal static XElement getDataItemFromComponent(XElement _ele, String _name)
        {
            IEnumerable<XElement> dataitems = _ele.XPathSelectElements("DataItems");
            if (dataitems == null)
                return null;
            else
            {
                IEnumerable<XElement>detaitemList = dataitems.ElementAt(0).Elements();
                foreach (XElement d in detaitemList)
                {
                   String dataitemname = d.Attribute("name").Value;
                   if (dataitemname.Equals(_name))
                       return d;
                }
            }
            return null;
        }
        //Find DataItem for name = _name from entire probe - assume name is unique in entire probe.
        internal static XElement getDataItemFromName(XElement _probe, String _name, String _devicename)
        {
            IEnumerable<XElement> detaitemList = _probe.XPathSelectElements("//DataItem[@name='" + _name + "']"); //get all DataItems//title[@lang='eng']
            
            foreach (XElement d in detaitemList)
            {
                XElement temp = d.Parent;
               //recursively find Device
                while (!temp.Name.LocalName.Equals("Device"))
                {
                    temp = temp.Parent;
                }
                if (temp.Attribute("name").Value.Equals(_devicename))
                    return d;
//                else
  //                  return null;
            }
            return null;
        }
        internal static XElement createDataStorage(XElement _probe)
        {
            XElement devices = new XElement(_probe.Element("Devices"));
            return devices;
        }
        
        internal static XElement createStreams(XElement _probe, XmlNamespaceManager namespaceManager)
        {
            XElement streams = new XElement("Streams");
            IEnumerable<XElement> devices = _probe.Element("Devices").Elements("Device");
            foreach (XElement d in devices)
            {
                String devicename = d.Attribute("name").Value;
                String uuid = ""; //uuid is optional as of 7/1/08
                XAttribute temp;
                if ((temp = d.Attribute("uuid")) != null)
                    uuid = temp.Value;

                XElement devicestream = new XElement("DeviceStream", new XAttribute("name", devicename), new XAttribute("uuid", uuid));
                handleComponents(ref devicestream, d, namespaceManager);
                streams.Add(devicestream);

            }
            return streams;
        }
        //create <ComponentStream> if <Components>'s child element is not <DataItems>
        internal static void handleComponents(ref XElement _devicestream, XElement device, XmlNamespaceManager namespaceManager)
        {
            IEnumerable<XElement> componentsList = device.Elements("Components");
            foreach (XElement componentsElement in componentsList)
            {
                foreach (XElement ctemp in componentsElement.Elements())
                {
                    String ename = ctemp.Name.LocalName;
                    if (!ename.Equals("DataItems") && !ename.Equals("Description"))
                    {
                        _devicestream.Add(DataUtil.CreateComponentStream(ctemp)); //Power, Axes, Controller
                        handleComponents(ref _devicestream, ctemp, namespaceManager);
                    }
                }
            }
        }
      
      
        /*
         * 1st upper and lest lower
         */
        internal static String modifyString1(String _s)
        {
            return _s.Substring(0, 1).ToUpper() + _s.Substring(1).ToLower();
        }


        internal static XElement CreateComponentStream(XElement _ele)
        {
            XElement cs = new XElement("ComponentStream", new XAttribute("component", _ele.Name), new XAttribute("name", _ele.Attribute("name").Value), new XAttribute("componentId", _ele.Attribute("id").Value));
            ArrayList categories = GetDataItemCategories(_ele);
            foreach (String s in categories)
            {
                cs.Add(new XElement(s)); //<Samples> or <Events> in <ComponentStream>
            }
            return cs;
        }
        internal static String getCategoryElementName(String _category)
        {
            String c_lower = _category.ToLower();
            if (c_lower.Equals("sample"))
                return "Samples";
            else if (c_lower.Equals("event"))
                return "Events";
            else
                return null;
        }
        internal static ArrayList getDataItemTypeAndCategory(IEnumerable<XElement> cdataitems, String _nameToMatch)
        {
            ArrayList list = new ArrayList();
            foreach (XElement di in cdataitems)
            {
                String dname = di.Attribute("name").Value;
                if (dname.Equals(_nameToMatch))
                {
                    list.Add(di.Attribute("type").Value); // to be element name
                    list.Add(di.Attribute("category").Value); // to put under
                }

            }
            return list;
        }

        internal static ArrayList GetDataItemCategories(XElement _ele)
        {
            ArrayList list = new ArrayList();
            XElement dataitemsEle = _ele.Element("DataItems");
            if (dataitemsEle != null)
            {
                IEnumerable<XElement> dataitems = dataitemsEle.Elements("DataItem");
                foreach (XElement di in dataitems)
                {
                    String category = di.Attribute("category").Value;
                    if (category.Equals("SAMPLE") && !list.Contains("Samples"))
                        list.Add("Samples");
                    else if (category.Equals("EVENT") && !list.Contains("Events"))
                        list.Add("Events");
                }
            }
            return list;
        }
        
        internal static XElement createEventData(String _timestamp, String _partId, String _workPieceId, String _sequence, String _alarm_code, String _alarm_severity, String _alarm_nativecode, String _alarm_state, String _value)
        {
            XElement re = new XElement("Data", new XAttribute("timestamp", _timestamp), new XAttribute("sequence", _sequence));
            if (_partId != null )
                re.Add(new XAttribute("partId", _partId));
            if (_workPieceId != null )
                re.Add(new XAttribute("workPieceId", _workPieceId));
            if (_alarm_state != null )
                re.Add(new XAttribute("state", _alarm_state));
            if (_alarm_code != null )
                re.Add(new XAttribute("code", _alarm_code));
            if (_alarm_severity != null )
                re.Add(new XAttribute("severity", _alarm_severity));
            if (_alarm_nativecode != null )
                re.Add(new XAttribute("nativecode", _alarm_nativecode));
            re.SetValue(_value);
            return re;
        }
        
        internal static XElement createSampleData(String _timestamp, String _value, String _partId, String _workPieceId, String _sequence)
        {
            XElement re = new XElement("Data", new XAttribute("timestamp", _timestamp), new XAttribute("sequence", _sequence));
            if (_partId != null )
                re.Add(new XAttribute("partId", _partId));
            if (_workPieceId != null)
                re.Add(new XAttribute("workPieceId", _workPieceId));
            re.SetValue(_value);
            return re;
        }
        internal static XElement createData(String _elename, String _dataitem_name, String _dataitem_subType, String _dataItem_id, XElement data)
        {
            String timestamp = data.Attribute("timestamp").Value;
            String sequence = data.Attribute("sequence").Value;
            XElement re = new XElement(MTConnectNameSpace.mtStreams + _elename, new XAttribute("name", _dataitem_name), new XAttribute("timestamp", timestamp), new XAttribute("sequence", sequence));
            re.Add(new XAttribute("dataItemId", _dataItem_id));
            if (_dataitem_subType != null)
                re.Add(new XAttribute("subType", _dataitem_subType));
            XAttribute temp = data.Attribute("partId");
            if ( temp != null )
                re.Add(new XAttribute("partId",temp.Value));
            temp = data.Attribute("workPieceId");
            if ( temp != null )
                re.Add(new XAttribute("workPieceId", temp.Value));
            temp = data.Attribute("state");
            if (temp != null)
                re.Add(new XAttribute("state", temp.Value));
            temp = data.Attribute("code");
            if (temp != null)
                re.Add(new XAttribute("code", temp.Value));
            temp = data.Attribute("severity");
            if (temp != null)
                re.Add(new XAttribute("severity", temp.Value));
            temp = data.Attribute("nativeCode");
            if (temp != null)
                re.Add(new XAttribute("nativeCode", temp.Value));

            re.SetValue(data.Value);
            return re;
        }
       
         
        // get Device element's name value recursively
        internal static String getDeviceName(XElement _ele)
        {
            //TODO: assumption Device's name is required
            XElement p = _ele.Parent;
            if (p.Name.LocalName.Equals("Device"))
                return p.Attribute("name").Value;
            else
                return getDeviceName(p);
        }





    }
}
