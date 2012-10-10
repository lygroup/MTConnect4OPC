//
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Utilities;

namespace ShdrService4Opc
{
    public class Client
    {
        //Socket mSocket;
        //public Client(Socket socket) {mSocket=socket; }

        public TcpClient clientSocket;
        public string clNo;
        public void storeClient(TcpClient inClientSocket, string clineNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clineNo;
        }
        public void Close()
        {
            clientSocket.Close();
        }
        public bool IsConnected()
        {
            return clientSocket.Connected; 
        }
        public int DoRead(ref string buffer)
        {
            int requestCount = 0;
            int nRead;
            byte[] bytesFrom = new byte[18192] ; // clientSocket.ReceiveBufferSize];
            requestCount = 0;
            requestCount = requestCount + 1;
             buffer = null;
            try
            {
                if (!clientSocket.Connected)
                    return 0; 
                
                NetworkStream networkStream = clientSocket.GetStream();
                networkStream.ReadTimeout = 100;

                if (!networkStream.DataAvailable)
                    return 0;
                nRead = networkStream.Read(bytesFrom, 0, (int)18192);
                buffer = System.Text.Encoding.ASCII.GetString(bytesFrom);
            }
            catch (Exception)
            {
                return 0;

            }
            string dataFromClient = buffer.Substring(0, 100);
            //Console.WriteLine(" >> " + "From client-" + clNo + dataFromClient);
            return nRead;
        }
        public int DoWrite(string serverResponse)
        {
            Byte[] sendBytes = null;
            try
            {
                NetworkStream networkStream = clientSocket.GetStream();
                networkStream.WriteTimeout = 10;
                sendBytes = Encoding.ASCII.GetBytes(serverResponse);
                networkStream.Write(sendBytes, 0, sendBytes.Length);
                networkStream.Flush();
  //              Console.WriteLine(" >> " + serverResponse);
                return serverResponse.Count();
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
    public class Server
    {
        Adapter _owner;
        List<Client> mClients = new List<Client>();
        public int numClients() { return mClients.Count; }
         int mPort;
        bool bListen;
        TcpListener serverSocket = null;
        Thread ctThread;
        void removeClient(Client aClient)  
        {
            Logger.LogMessage("SHDR Client removed" + Convert.ToString(aClient.clNo) + " \n", Logger.DEBUG);
            mClients.Remove(aClient); 
        }
        void addClient(Client aClient) 
        {
            mClients.Add(aClient);
            Logger.LogMessage("SHDR Client Added" + Convert.ToString(aClient.clNo) + " \n", Logger.DEBUG);
        }
 
        public void ListenForClients()
        {
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;
            while (bListen)
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                Logger.LogMessage(" >> " + "Client:" + Convert.ToString(counter) + " started!\n", Logger.DEBUG);
                Client client = new Client();
                client.storeClient(clientSocket, Convert.ToString(counter));
                _owner.sendInitialData(client);
                mClients.Add(client);
            }

            clientSocket.Close();
            serverSocket.Stop();
            clientSocket = null;
            serverSocket = null;
        }
        public Server(int aPort, Adapter adapter)
        {
            _owner = adapter;
            mPort = aPort;
            serverSocket = new TcpListener(mPort);
            bListen = true;
            serverSocket.Start();
            ctThread = new Thread(ListenForClients);
            ctThread.Start();
        }
        ~Server()
        {
            try
            {
                bListen = false;
                if (serverSocket != null)
                    ctThread.Abort();
                for (int i = 0; i < mClients.Count; i++)
                    if (mClients[i] != null)
                        mClients[i].Close();
                if (serverSocket != null)
                    serverSocket.Stop();
            }
            catch (Exception) { }

        }
        public void readFromClients()
        {
            /* Since clients can be removed, we need to iterate backwards */
            for (int i = mClients.Count - 1; i >= 0; i--)
            {
                Client client = mClients[i];
                 string buffer = null;
                int len = mClients[i].DoRead(ref buffer);
                //if (len > 0)
                 //   Console.Write("Received: " + buffer + "\n");
        
            }
        }
        public void sendToClient(Client aClient, string aString)
        {
            if (aClient.DoWrite(aString) < 0)
                removeClient(aClient);
        }
        public void sendToClients(string aString)
        {
            for (int i = mClients.Count - 1; i >= 0; i--)
                sendToClient(mClients[i], aString);
        }
    }
    public class DeviceDatum
    {
        private string mName;                   // The name of the Data Value 
        private string mValue;
        private bool mChanged;                  // A changed flag to indicated that the value has changed since last append. 
        private bool mHasValue;                 // Has this data value been initialized?    

        public DeviceDatum(string aName)
        {
            mName = aName;
            mChanged = false;
            mHasValue = false;
        }
        public bool changed() { return mChanged; }
        public void reset() { mChanged = false; }
        public bool setValue(string naValue) { mChanged = true;  mValue = naValue; return true; }
        public string getValue() { return mValue; }
        public string getName() { return mName; }
        public virtual string toString(ref string aBuffer)
        {
            aBuffer += String.Format("|{0}|{1}", mName, mValue);
            return aBuffer;
        }
        public virtual bool append(ref string aBuffer)
        {
            toString(ref aBuffer);
            mChanged = false;
            return mChanged;
        }
        public virtual bool hasInitialValue() { return mHasValue; }
        public virtual bool requiresFlush() { return false; }
    }
    public class Adapter
    {
        public Server mServer;         /* The socket server */
        string mBuffer;    /* A string buffer to hold the string we write to the streams */
        int mScanDelay;          /* How long to sleep (in ms) between scans */
        int mPort;              /* The server port we bind to */
        bool _bRunning;
        bool _bDone;
        List<DeviceDatum> mDeviceData = new List<DeviceDatum>();
        Thread adapterThread;

        public Adapter(int aPort, int aScanDelay)
        {
            mScanDelay = aScanDelay;
            mPort = aPort;
            mBuffer = String.Empty;
        }
        /* Add a data value to the list of data values */
        public void addDatum(DeviceDatum aValue ) {  mDeviceData.Add(aValue); }
        public void sleepMs(int aMs) { Thread.Sleep(aMs); }
        public virtual void sendChangedData()
        {
            for (int i = 0; i < mDeviceData.Count; i++)
            {
                DeviceDatum value = mDeviceData[i];
                if (value.changed())
                    sendDatum(value);
            }
            sendBuffer();
        }
        /* Send the initial values to a client */
        public void sendInitialData(Client aClient)
        {
            string buffer = String.Empty;
            gatherDeviceData();

            for (int i = 0; i < mDeviceData.Count; i++)
            {
                DeviceDatum value = mDeviceData[i];
                if (value.hasInitialValue())
                    value.append(ref buffer);
            }
            if (mBuffer.Count() > 0)
            {
                buffer = Utils.GetNowDateTime() + " " + mBuffer + "\n";
                Logger.LogMessage(buffer, 3);
                mServer.sendToClient(aClient, buffer);
            }
        }
        /* Start the server */
        public void startServer()
        {
            // Create the  thread.
            adapterThread = new Thread(threadServer);
            adapterThread.Start();
        }
        public void stopServer()
        {
            _bRunning = false;
            if (mServer == null)
            {
                _bDone = true;
                return;
            }
            for (int i = 0; i < 10 && _bDone != true; i++)
                sleepMs(1000);

            mServer = null;
        }
        public void threadServer()
        {
            try
            {
                // Construction done here to minimize startServer method time
                _bRunning = true;

                // Start TcpListener on mport
                // Start Server thread to detect shdr clients sockets
                mServer = new Server(mPort, this);
                if (mServer == null)
                    throw new Exception("Bad Server Construction");

                _bDone = false;
                /* Process forever... */
                while (_bRunning)
                {

                    /* Read and discard all data from the clients */
                     mServer.readFromClients();

                    /* Don't bother getting data if we don't have anyone to read it */
                    if (mServer.numClients() >= 0)
                    {
                        gatherDeviceData();
                        sendChangedData();
                        mBuffer = String.Empty;
                    }

                    if (!_bRunning)
                        break;
                    sleepMs(mScanDelay);
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage("Adapter::ThreadServer Error n" + e.Message + "\n", -1);
            }

            _bRunning = false;
            mServer = null;
            _bDone = true;
        }

        /* Send a single value to the buffer. */
        void sendDatum(DeviceDatum aValue)
        {
            if (aValue.requiresFlush())
                sendBuffer();
            aValue.append(ref mBuffer);
            if (aValue.requiresFlush())
                sendBuffer();
        }

        /* Send the buffer to the clients. Only sends if there is something in the buffer. */
        void sendBuffer()
        {
            if (mBuffer.Count() > 0)
            {
                mBuffer = Utils.GetNowDateTime() + " " + mBuffer + "\n";
                Logger.LogMessage(mBuffer,3);
                mServer.sendToClients(mBuffer);
                mBuffer = String.Empty;
            }
        }

        /* Pure virtual method to get the data from the device. */
        public virtual void gatherDeviceData() { }
    }
}