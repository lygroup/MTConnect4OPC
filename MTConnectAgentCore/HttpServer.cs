using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Web;


namespace MTConnectAgentCore
{
    internal delegate void delReceiveWebRequest(HttpListenerContext Context);
    public delegate short UserCommandDelegate(String deviceId, HttpListenerRequest Context, StreamWriter writer);

     public class HttpServer
    {
        private HttpListener Listener;
        private bool IsStarted = false;
        internal event delReceiveWebRequest ReceiveWebRequest;
        private IData sharedData;
        private long _portnumber;
        public UserCommandDelegate userCommandDelegate;

        // C# Default parameter specifiers are not permitted! 
         internal HttpServer(IData _shared)
        {
            sharedData = _shared;
           _portnumber= 80;
           userCommandDelegate = null;
        }
         internal HttpServer(IData _shared, long portnumber)
        {
            sharedData = _shared;
            _portnumber = portnumber;
        }

        internal void Start()
        {

            if (this.IsStarted)
                return;

            if (this.Listener == null)
            {
                this.Listener = new HttpListener();
            }
            String str = String.Format("http://+:{0}/", Convert.ToString(_portnumber));
            this.Listener.Prefixes.Add(str);

            this.IsStarted = true;
            try
            {
                this.Listener.Start();
            }
            catch (System.Net.HttpListenerException)
            {
                throw new AgentException("Agent Start Failed.  Please make sure port 80 is available for Agent.", AgentException.E_HTTPFAIL);
            }

            IAsyncResult result = this.Listener.BeginGetContext(new AsyncCallback(WebRequestCallback), this.Listener);

        }

        internal virtual void Stop()
        {
            if (Listener != null)
            {
                this.Listener.Close();
                this.Listener = null;
                this.IsStarted = false;

            }

        }

        private void WebRequestCallback(IAsyncResult result)
        {

            if (this.Listener == null)
                return;

            try
            {
                // Get out the context object
                HttpListenerContext context = this.Listener.EndGetContext(result);
                //setup a new context for the next request
                this.Listener.BeginGetContext(new AsyncCallback(WebRequestCallback), this.Listener);

                if (this.ReceiveWebRequest != null)
                    this.ReceiveWebRequest(context);

                this.ProcessRequest(context);

            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;

            }

            catch (Exception e)
            {
                if (this.Listener == null) //it seems like Stop is called this.listener.EndGetContext will throw NullPointException
                    return;
                else
                {
                    // Obtain a response object.
                    HttpListenerResponse response = this.Listener.GetContext().Response;
                    response.StatusCode = 500;
                    throw e;
                }
            }

        }

        private void ProcessRequest(System.Net.HttpListenerContext Context)
        {
            HttpListenerRequest request = Context.Request;
            String rawUrl;
            if (!(rawUrl = request.RawUrl).Equals("/"))
            {
                HttpListenerResponse response = Context.Response;
                //front support returning xml to Http client
                response.ContentType = "text/xml";
                //Construct a response
                response.StatusCode = 200;
                StreamWriter writer = new StreamWriter(response.OutputStream, System.Text.UTF8Encoding.UTF8);
                try
                {
                    short rv = process2(request, writer);
                    if (rv == ReturnValue.ERROR)
                    {
                        response.StatusCode = 500;
                    }

                }
                catch (System.Xml.XPath.XPathException e)  
                {
                    //when XDocument.XPathSelectElements() throws XPathException but not when a client send path to query
                    Error.createError(sharedData, Error.INTERNAL_ERROR, e.Message).Save(writer);
                }
                writer.Close();

            }
        }
        //http://127.0.0.1/storeEvent?timestamp=2008-04-30T15:26:17-04:00&deviceName=Company+IMTS+MTConnect+Demonstration&dataItemName=power&value=ON
        //http://127.0.0.1/storeEvent?timestamp=2008-04-30T15:26:17-04:00&deviceName=Company+IMTS+MTConnect+Demonstration&dataItemName=alarm&value=ON&partId=partId&workPieceId=workPieceId&code=code&severity=severity&nativecode=nativecode&state=state
        //http://127.0.0.1/storeSample?timestamp=2008-04-30T15:26:17-04:00&deviceName=Company+IMTS+MTConnect+Demonstration&dataItemName=Srpm&value=0.8256

        //error http://127.0.0.1/storeSample?timestamp=2008-04-30T15:26:17-04:00&deviceName=Company+IMTS+MTConnect+Demonstration&dataItemName=Srpm&value=0.8256&partId=partId&workPieceId=workPieceId&code=code&severity=severity&nativecode=nativecode&state=state


        private short createInvalidRequestError(StreamWriter writer, String extra)
        {
            if (extra == null)
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
            else
                Error.createError(sharedData, Error.INVALID_REQUEST, extra).Save(writer);
            return ReturnValue.ERROR;
        }

        private short process2(HttpListenerRequest request, StreamWriter writer)
        {

            //segument[0] = / segumens[1] = sample, for http:/127.0.0.1/sample?path= is 
            String[] seguments = request.Url.Segments; // {/,devicename,sample

            if (seguments[0].StartsWith("/") == false)
                return createInvalidRequestError(writer, null);

            if (seguments.Length == 2) //http://<IP>/current or http://<IP>/current?path=...
            {
                String[] keys = request.QueryString.AllKeys;
                String path = seguments[1];
                if (keys.Length == 0)
                {
                    switch (path)//http://<IP>/current
                    {
                        case "probe":
                            return sharedData.getProbe(writer);
                        case "current":
                            return sharedData.getCurrent(writer);
                        case "sample":
                            return sharedData.getStream(writer);
                        case "debug":
                            return sharedData.getDebug(writer);
                        //DAF 2008-07-31 Added 
                        case "version":
                            return sharedData.getVersion(writer);
                        case "log":
                            return sharedData.getLog(writer);
                        case "config":
                            return sharedData.getConfig(writer);
                        default:
                            {
                                if (userCommandDelegate != null)
                                    if (ReturnValue.SUCCESS == userCommandDelegate(null, request, writer))
                                        return ReturnValue.SUCCESS;
                                return createInvalidRequestError(writer, null);
                            }
                    }
                }
                else
                {
                    switch (path) //http://<IP>/current?path=...
                    {
                        case "storeSample":
                            return handleStoreSample(request.QueryString, writer);
                        case "storeEvent":
                            return handleStoreEvent(request.QueryString, writer);
                        case "current":
                            return handleCurrent(request.QueryString, null, writer);
                        case "sample":
                            return handleSample(request.QueryString, null, writer);
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
            }
            else if (seguments.Length == 3) //http://<IP>/deviceName/current? => s[0] = "/" s[1] = "deviceName/", s[2] = current
            {
                String deviceName = null;
                if (!seguments[1].EndsWith("/"))
                    return createInvalidRequestError(writer, null);
                else
                    deviceName = seguments[1].Substring(0, seguments[1].Length - 1);

                String[] keys = request.QueryString.AllKeys;
                String path = seguments[2];
                if (keys.Length == 0)
                {
                    switch (path)
                    {
                        case "probe":
                            return sharedData.getProbeDevice(deviceName, writer);
                        case "current":
                            return sharedData.getCurrentDevice(deviceName, writer);
                        case "sample":
                            return sharedData.getStreamDevice(deviceName, writer);
                        default:
                            {
                                if (userCommandDelegate != null)
                                    if (ReturnValue.SUCCESS == userCommandDelegate(deviceName, request, writer))
                                        return ReturnValue.SUCCESS;
                                return createInvalidRequestError(writer, null);
                            }
                    }
                }
                else
                {
                    switch (path)
                    {
                        case "current":
                            return handleCurrent(request.QueryString, deviceName, writer);
                        case "sample":
                            return handleSample(request.QueryString, deviceName, writer);
                        default:
                            return createInvalidRequestError(writer, null);
                    }
                }
            }
            else
            {
                return createInvalidRequestError(writer, null);
            }

        }

        private short handleSample(System.Collections.Specialized.NameValueCollection queryString, String deviceName, StreamWriter writer)
        {
            String path, from, count;
            bool success = handleUrlRequest(queryString, out path, out from, out count);
            if (success == false)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
                return ReturnValue.ERROR;
            }
            else
                if (deviceName == null)
                    return sharedData.getStream(path, from, count, writer);
                else
                    return sharedData.getStreamDevice(deviceName, path, from, count, writer);

        }
        private short handleCurrent(System.Collections.Specialized.NameValueCollection queryString, String deviceName, StreamWriter writer)
        {
            String path, from, count;
            bool success = handleUrlRequest(queryString, out path, out from, out count);
            if (success == false)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST).Save(writer);
                return ReturnValue.ERROR;
            }
            else if (from != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"from\" parameter can not be Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
            else if (count != null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"count\" parameter can not be in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
            else if (path == null)
            {
                Error.createError(sharedData, Error.INVALID_REQUEST, "\"path\" parameter is not specified in the Current Request.").Save(writer);
                return ReturnValue.ERROR;
            }
            else
            {
                if (deviceName == null)
                    return sharedData.getCurrent(path, writer);
                else
                    return sharedData.getCurrentDevice(deviceName, path, writer);
            }

        }
        //return error XElement
        //return null for success
        private bool handleUrlRequest(System.Collections.Specialized.NameValueCollection queryString, out String path, out String from, out String count)
        {
            //set default
            path = null;
            from = null;
            count = null;

            String[] keys = queryString.AllKeys;
            if (keys.Length == 0)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                String[] values = queryString.GetValues(keys[i]);
                switch (keys[i])
                {
                    case "path":
                        path = values[0];
                        break;
                    case "from":
                        from = values[0];
                        break;
                    case "count":
                        count = values[0];
                        break;
                }
            }
            return true;
        }

        private XElement handleStoreSampleStoreEventCommon(System.Collections.Specialized.NameValueCollection queryString,
            out String timestamp, out String deviceName, out String dataItemName, out String value, out String workPieceId, out String partId, out String code, out String severity, out String nativecode, out String state)
        {
            timestamp = null;
            deviceName = null;
            dataItemName = null;
            value = null;
            workPieceId = null;
            partId = null;
            code = null;
            severity = null;
            nativecode = null;
            state = null;


            //// Get names of all keys into a string array.
            String[] keys = queryString.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                String[] values = queryString.GetValues(keys[i]);
                switch (keys[i])
                {
                    case "timestamp":
                        timestamp = values[0];
                        break;
                    case "deviceName":
                        deviceName = values[0];
                        break;
                    case "dataItemName":
                        dataItemName = values[0];
                        break;
                    case "value":
                        value = values[0];
                        break;
                    case "workPieceId":
                        workPieceId = values[0];
                        break;
                    case "partId":
                        partId = values[0];
                        break;
                    case "code":
                        code = values[0];
                        break;
                    case "severity":
                        severity = values[0];
                        break;
                    case "nativecode":
                        nativecode = values[0];
                        break;
                    case "state":
                        state = values[0];
                        break;
                }



            }

            if (timestamp == null)
                return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The timestamp is missing.");
            else if (deviceName == null)
                return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The deviceName is missing.");
            else if (dataItemName == null)
                return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The dataItemName is missing.");
            else if (value == null)
                return MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The value is missing.");
            else
                return null; //success

        }
        //short StoreSample(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId);
        private short handleStoreSample(System.Collections.Specialized.NameValueCollection queryString, StreamWriter writer)
        {
            short storeSampleReturn = ReturnValue.ERROR; //default
            XElement returnElement;
            String timestamp, deviceName, dataItemName, value, workPieceId, partId, code, severity, nativecode, state;
            if ((returnElement = handleStoreSampleStoreEventCommon(queryString, out timestamp, out deviceName, out dataItemName, out value, out workPieceId, out partId, out code, out severity, out nativecode, out state)) == null)
            {
                if (code != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not defined \"code\".");
                else if (severity != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not defined \"severity\".");
                else if (state != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not defined \"state\".");
                else if (nativecode != null)
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, "The storeSample should not defined \"nativecode\".");
                else
                {
                    try
                    {
                        storeSampleReturn = sharedData.StoreSample(timestamp, deviceName, dataItemName, value, workPieceId, partId);

                        if (storeSampleReturn == ReturnValue.SUCCESS)
                            returnElement = new XElement("Acknowledge", new XAttribute("dateTime", Util.GetDateTime()));
                        else
                            returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA);
                    }
                    catch (AgentException e)
                    {
                        returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, e.Message);
                    }
                }
            }
            returnElement.Save(writer);
            return storeSampleReturn;

        }
        // short StoreEvent(String timestamp, String deviceName, String dataItemName, String value, String workPieceId, String partId, String alarm_code, String alarm_severity, String alarm_nativecode, String alarm_state);
        private short handleStoreEvent(System.Collections.Specialized.NameValueCollection queryString, StreamWriter writer)
        {

            short storeEventReturn = ReturnValue.ERROR; //default
            XElement returnElement;
            String timestamp, deviceName, dataItemName, value, workPieceId, partId, code, severity, nativecode, state;
            if ((returnElement = handleStoreSampleStoreEventCommon(queryString, out timestamp, out deviceName, out dataItemName, out value, out workPieceId, out partId, out code, out severity, out nativecode, out state)) == null)
            {
                try
                {
                    storeEventReturn = sharedData.StoreEvent(timestamp, deviceName, dataItemName, value, workPieceId, partId, code, severity, nativecode, state);

                    if (storeEventReturn == ReturnValue.SUCCESS)
                        returnElement = new XElement("Acknowledge", new XAttribute("dateTime", Util.GetDateTime()));
                    else
                        returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA);
                }
                catch (AgentException e)
                {
                    returnElement = MachineAPIError.createError(MachineAPIError.UNRECOGNIZEDDATA, e.Message);
                }
            }
            returnElement.Save(writer);
            return storeEventReturn;

        }
    }
}
