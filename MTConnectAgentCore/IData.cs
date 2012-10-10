using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MTConnectAgentCore
{
    
    public interface IData
    {
        
        short loadConfig();

        String getSender();
        String getVersion();
        
        //http://xxx/probe
        short getProbe(StreamWriter writer);
        //http://xxx/deviceId/probe
        short getProbeDevice(String deviceId, StreamWriter writer);

        //http://xxx/current
        short getCurrent(StreamWriter writer);
        //http://xxx/current?path=....
        short getCurrent(String xpath, StreamWriter writer);
        //http://xxx/deiceId/current
        short getCurrentDevice(String deviceId, StreamWriter writer);
        //http://xxx/deiceId/current?path=
        short getCurrentDevice(String deviceId, String xpath, StreamWriter writer);

        String[] getDevices();
        bool isEvent(String _name, String _device); //used by AgentSHDR
        bool isAlarm(String _name, String _device); //used by AgentSHDR

        short getStream(StreamWriter writer); //http://xxx/sample
        short getStream(String xpath, String from, String count, StreamWriter writer);
        short getStreamDevice(String deviceId, StreamWriter writer);
        short getStreamDevice(String deviceId, String xpath, String from, String count, StreamWriter writer);

        short StoreSample(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId);
        short StoreEvent(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
        short getDebug(StreamWriter writer);

        //DAF 2008-07-31 Added 
        short getVersion(StreamWriter writer);
        short getLog(StreamWriter writer);
        short getConfig(StreamWriter writer);

        // jlm added Sat 10/31/09 10:33:29 PM maybe connection event instead
       // short doDeviceReset(String deviceId, StreamWriter writer);

        void ClearDataBuffer(string devicename);

     }
}
