using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace MTConnectAgentCore
{
    public class Agent : IMachineAPI
     { 
         public IData data;
         public HttpServer hst;
         string exeFilePath;

         public Agent()
         {
            data = new Data();
            this.exeFilePath = Assembly.GetExecutingAssembly().Location;
         }
         public IData Data() { return data; }

         //ReturnValue.ERROR is
         //1) when Devices.xml could not be loaded 
         //2) No Header found in Devices.xml
         //3) No bufferSize, sender, or version in Header Element
         public virtual void Start(int ipport)
         {
             try
             {
                 DeviceEntry ldap = new DeviceEntry();
                 //DAF 2008-08-03 Added 
                 DeviceEntry.LogToFile("LDAP DeviceEntry Starting");
                 ldap.LDAPDeviceEntry();
                 DeviceEntry.LogToFile("LDAP DeviceEntry Finished");

                 if (data.loadConfig() == ReturnValue.ERROR)
                 {
                     DeviceEntry.LogToFile("Agent Start Failed.\n Problem in Devices.xml");
                     throw new AgentException("Agent Start Failed.\n Problem in Devices.xml");
                 }
                 hst = new HttpServer(data, ipport);
                 hst.Start();
             }
             catch (AgentException e)
             {
                 throw e;
             }
             catch (Exception e)
             {
                 DeviceEntry.LogToFile("Agent Start Failed.  " + e);
                 throw new AgentException("Agent Start Failed.", e);
             }
         }

         public virtual void Stop()
         {
             try
             {
                 if(hst != null)
                    hst.Stop();
             }
             catch (Exception e)
             {
                 DeviceEntry.LogToFile("Agent Stop Failed.  " + e);
                 throw new AgentException("Agent Stop Failed.", e);
             }
         }

         //- <Streams>
         //- <DeviceStream name="emc" uuid="0">
         //- <ComponentStream component="Linear" name="Y" path="/Device[@name='emc']//Axes[@name='axes']">
         //- <Samples>
         //<m:Position subType="ACTUAL" timestamp="2008-03-20T11:14:02.892" itemId="7" sequence="101">0.814353775135</m:Position> 

         //Component = "Linear"
         //ComponentName = "Y"
         //SubType = "ACTUAL"
         //name="Yact" ----> Lookup at probe, know to create Position Element
         //Value = 0.814353775135
         //partId -- optional
         //workPieceId-- optional
         public short StoreSample(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId)
         {
             return data.StoreSample(timestamp, deviceName, dataItemName, value, workPieceId, partId);
         }
         public short StoreEvent(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state)
         {
             return data.StoreEvent(timestamp, deviceName, dataItemName, value, workPieceId, partId, alarm_code, alarm_severity, alarm_nativecode, alarm_state);
         }

     }
}
