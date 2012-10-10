using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MTConnectAgentCore
{
    public class Util
    {
        //http://ip/deviceid/probe then get return deviceid
        public static String getFirst(String rawUrl)
        {
            int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
            if  (index == -1 )
                return rawUrl.Substring(1, rawUrl.Length - 1);
            else
                return rawUrl.Substring(1, index - 1);
        }

        
        public static String getSecond(String rawUrl)
        {
            int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
            if (index == -1)
                return null;
            else
                return rawUrl.Substring(index + 1);
        }
        public static XElement createDeviceXST()
        {
            return createXST("MTConnectDevices");
        }
        public static XElement createErrorXST()
        {
            return createXST("MTConnectError");
        }

        public static XElement createStreamXST()
        {
            return createXST("MTConnectStreams");
        }
        private static XElement createXST(String elementName)
        {
            XElement root = null;

            if (elementName.Equals("MTConnectStreams"))
            {
                root = new XElement(MTConnectNameSpace.mtStreams + elementName,
                new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectStreams:0.9"));
            }
            else if (elementName.Equals("MTConnectDevices"))
            {
                root = new XElement(MTConnectNameSpace.mtDevices + elementName,
                new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectDevices:0.9"));
            }
            else if (elementName.Equals("MTConnectError"))
            {
                root = new XElement(MTConnectNameSpace.mtError + elementName,
               new XAttribute(XNamespace.Xmlns + "mt", "urn:mtconnect.com:MTConnectError:0.9"));
            }
            
            return root;
        }
       
        //public static String[] trimAndreaplaceStringNullWithNull(String[] original)
        //{
        //    for (int i = 0; i < original.Length; i++)
        //    {
        //        if (original[i] != null)
        //        {
        //            original[i] = original[i].Trim();
        //            if (original[i].ToLower().Equals("null"))
        //                original[i] = null;
        //        }
        //    }
        //    return original;
        //}
        //public static String convertEmptyStringToNull(String original)
        //{
        //    if (original != null && original.Trim().Equals(""))
        //        return null;
        //    else
        //        return original;
        //}
     
        //check for format      
        //public static Object ConvertToDateTime(String _s)
        //{
        //    try
        //    {
        //        String yearmonthday = _s.Substring(0, 10);
        //        String[] split = yearmonthday.Split('-');
        //        int year = int.Parse(split[0]);
        //        int month = int.Parse(split[1]);
        //        int day = int.Parse(split[2]);

        //        String time = _s.Substring(11, 8);
        //        String[] split2 = time.Split(':');
        //        int hour = int.Parse(split2[0]);
        //        int minite = int.Parse(split2[1]);
        //        int second = int.Parse(split2[2]);

        //        String offset = _s.Substring(19, 3);

        //        return new DateTimeOffset(year, month, day, hour, minite, second, new TimeSpan(int.Parse(offset), 0, 0));
        //    }
        //    catch (ArgumentOutOfRangeException)
        //    {
        //        return null;
        //    }

        //}


        public static string datePatt = "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz"; 
        public static string GetDateTime()
        {
            //2008-04-29T12:34:41-07:00
            return DateTimeOffset.Now.ToString(datePatt);
        }
    }
}
