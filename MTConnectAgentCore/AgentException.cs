using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MTConnectAgentCore
{
    public class AgentException: Exception
    {
        public const int S_OK = 0x00000000;
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int E_BADCATEGORY = unchecked((int)0x80070058);
        public const int E_BADALARMDATA = unchecked((int)0x80070059);
        public const int E_HTTPFAIL = unchecked((int)0x80070060);

        public AgentException(String msg) : base(msg) { HResult = E_FAIL; }
        public AgentException(String msg, Exception innerException) : base(msg, innerException) { HResult = E_FAIL; }
        public AgentException(String msg, int errorcode) : base(msg){ HResult=errorcode; }
      
    }
}
