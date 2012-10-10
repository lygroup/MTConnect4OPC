using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MTConnectAgentCore
{
    class MachineAPIError
    {
        internal static int UNRECOGNIZEDDATA = 1;
        internal static int INVALIDNUMBEROFARGUMENTS = 2;
       
            
        internal static XElement createError(int _errorCode)
        {

            //<AgentError dateTime="2008-06-12T09:15:36-04:00">
	        //<Error errorCode="UNRECOGNIZEDDATA">Unrecognized data was transmitted.</Error> <AgentError>
            
            String[] errorDescription = getErrorCode(_errorCode);
            return new XElement("AgentError", new XAttribute("dateTime", Util.GetDateTime()),
                 new XElement("Error",
                    new XAttribute("errorCode", errorDescription[0]), errorDescription[1]));
        }

        //DAF 2008-07-16 Added
        internal static XElement createError(int _errorCode, string _comment)
        {
            String[] errorDescription = getErrorCode(_errorCode, _comment);
            return new XElement("AgentError", new XAttribute("dateTime", Util.GetDateTime()),
                 new XElement("Error",
                    new XAttribute("errorCode", errorDescription[0]), errorDescription[1]));
        }

        internal static String[] getErrorCode(int _errorCode, string _comment)
        {
            String[] error = getErrorCode(_errorCode);
            error[1] = error[1] + " " + _comment;
            return error;
        }

        internal static String[] getErrorCode(int _errorCode)
        {
            String[] error = new String[2];
            switch( _errorCode )
            {
                case 1:
                    error[0] = "UNRECOGNIZEDDATA";
                    error[1] = "Unrecognized data was transmitted.";
                    break;
                case 2:
                    error[0] = "INVALIDNUMBEROFARGUMENTS";
                    error[1] = "Number of argument is invalid.";
                    break;

                default:
                    error = null;
                    break;

            }
            return error;
        }
    }
}
