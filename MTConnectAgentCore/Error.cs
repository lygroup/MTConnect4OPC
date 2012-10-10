using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MTConnectAgentCore
{
    class Error
    {
        internal static int UNAUTHORIZED = 1;
        internal static int NO_DEVICE  = 2; 
        internal static int OUT_OF_RANGE  = 3;
        internal static int TOO_MANY = 4; 
        internal static int INVALID_URI = 5; 
        internal static int INVALID_REQUEST  = 6; 
        internal static int INTERNAL_ERROR  = 7; 
        internal static int INVALID_PATH  = 8 ;
            
        internal static XElement createError(IData _data, int _errorCode)
        {

            XElement mtxst = Util.createErrorXST();
            String[] errorDescription = getErrorCode(_errorCode);
            XElement error =
                new XElement("Error",
                    new XAttribute("errorCode", errorDescription[0]), errorDescription[1]);
            mtxst.Add(error);
            return mtxst;
        }
        internal static XElement createError(IData _data, int _errorCode, String _extra)
        {

            XElement mtxst = Util.createErrorXST();
            String[] errorDescription = getErrorCode(_errorCode);
            XElement error =
                new XElement("Error",
                    new XAttribute("errorCode", errorDescription[0]), errorDescription[1] + " " + _extra);
            mtxst.Add(error);
            return mtxst;
        }

        internal static String[] getErrorCode( int _errorCode)
        {
            String[] error = new String[2];
            switch( _errorCode )
            {
                case 1: 
                    error[0] = "UNAUTHORIZED";
                    error[1] = "The request did not have sufficient permissions to perform the request.";
                    break;
                case 2: 
                    error[0] = "NO_DEVICE";
                    error[1] = "The device specified in the URI could not be found.";
                    break;
                case 3: 
                    error[0] = "OUT_OF_RANGE";
                    error[1] = "The sequence number was beyond the end of the buffer.";
                    break;
                case 4: 
                    error[0] = "TOO_MANY";
                    error[1] = "The sequence number was beyond the end of the buffer.";
                    break;
                case 5: 
                    error[0] = "INVALID_URI";
                    error[1] = "The URI provided was incorrect.";
                    break;
                case 6:
                    error[0] = "INVALID_REQUEST";
                    error[1] = "The request was not one of the specified requests.";
                    break;
                 case 7:
                    error[0] = "INTERNAL_ERROR";
                    error[1] = "Contact the software provider, the Agent did not behave correctly.";
                    break;
                 case 8:
                    error[0] = "INVALID_PATH";
                    error[1] = "The xpath could not be parsed. Invalid syntax.";
                    break;
                default:
                    error = null;
                    break;

 



            }
            return error;
        }
    }
}
