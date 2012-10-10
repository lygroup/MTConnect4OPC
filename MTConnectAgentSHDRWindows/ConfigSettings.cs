using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Configuration;
using System.Reflection;

namespace NISTMtConnectSHDRAgent
{

    //public class ConfigSettings
    //{
    //    private ConfigSettings() { }

    //    public static string ReadSetting(string key)
    //    {
    //        return ConfigurationSettings.AppSettings[key];
    //    }

    //    public static void WriteSetting(string key, string value)
    //    {
    //        // load config document for current assembly
    //        XmlDocument doc = loadConfigDocument();

    //        // retrieve appSettings node
    //        XmlNode node = doc.SelectSingleNode("//appSettings");

    //        if (node == null)
    //            throw new InvalidOperationException("appSettings section not found in config file.");

    //        try
    //        {
    //            // select the 'add' element that contains the key
    //            XmlElement elem = (XmlElement)node.SelectSingleNode(string.Format("//add[@key='{0}']", key));

    //            if (elem != null)
    //            {
    //                // add value for key
    //                elem.SetAttribute("value", value);
    //            }
    //            else
    //            {
    //                // key was not found so create the 'add' element 
    //                // and set it's key/value attributes 
    //                elem = doc.CreateElement("add");
    //                elem.SetAttribute("key", key);
    //                elem.SetAttribute("value", value);
    //                node.AppendChild(elem);
    //            }
    //            doc.Save(getConfigFilePath());
    //        }
    //        catch
    //        {
    //            throw;
    //        }
    //    }

    //    public static void RemoveSetting(string key)
    //    {
    //        // load config document for current assembly
    //        XmlDocument doc = loadConfigDocument();

    //        // retrieve appSettings node
    //        XmlNode node = doc.SelectSingleNode("//appSettings");

    //        try
    //        {
    //            if (node == null)
    //                throw new InvalidOperationException("appSettings section not found in config file.");
    //            else
    //            {
    //                // remove 'add' element with coresponding key
    //                node.RemoveChild(node.SelectSingleNode(string.Format("//add[@key='{0}']", key)));
    //                doc.Save(getConfigFilePath());
    //            }
    //        }
    //        catch (NullReferenceException e)
    //        {
    //            throw new Exception(string.Format("The key {0} does not exist.", key), e);
    //        }
    //    }

    //    private static XmlDocument loadConfigDocument()
    //    {
    //        XmlDocument doc = null;
    //        try
    //        {
    //            doc = new XmlDocument();
    //            doc.Load(getConfigFilePath());
    //            return doc;
    //        }
    //        catch (System.IO.FileNotFoundException e)
    //        {
    //            throw new Exception("No configuration file found.", e);
    //        }
    //    }

    //    private static string getConfigFilePath()
    //    {
    //        return Assembly.GetExecutingAssembly().Location + ".config";
    //    }
    //}
}