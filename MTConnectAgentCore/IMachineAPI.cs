using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MTConnectAgentCore
{
    interface IMachineAPI
    {
        short StoreSample(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId);
        short StoreEvent(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
    }
}
