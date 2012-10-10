/* 
 * Project:    	MT Connect - AgentCore
 * Company:    	Georgia Tech - MARC - FIS
 * Author:     	Douglas A. Furbush
 * Date:     	July, August 2008
 */
using System;
using System.Xml.Serialization;
using System.IO;

namespace MTConnectAgentCore
{
	[XmlRoot("Configuration")]
	public class Configuration
	{
        //static public string SERVICEVAR = "MTConnectAgentCore";

		private const int DEFAULT_WAIT_TIME=150;        /*default wait time in msecs*/
        //private string serverName = "fis-banyan";
        public string serverURL = "ldap.mtconnect.org";
        public string portId = "389";
        public string deviceName = "GT4MTC";
        public string adminDN = "dc=mtconnect,dc=org";
        public string container = "ou=Devices,dc=mtconnect,dc=org";
        public string company = "GeorgiaTech";
        public string description = "GT MTConnect Devices";
        public string deviceURI = @"http://banyan.marc.gatech.edu";
        public string serialno = "444555";
        public string loginDN = "cn=Agent,ou=accounts,dc=mtconnect,dc=org";
        public string passwd = "mtc0nnect";
        //private int waitTime=DEFAULT_WAIT_TIME;
        //private int waitTimeMsg=0;
//        private DBConfiguration MySQLConfiguration = new DBConfiguration();
        public const string DEFAULT_FILE_NAME = "mtcagent.ini";
        //static public string defaultDirectory = Environment.GetEnvironmentVariable(SERVICEVAR);
        //static public string confDirectory = "c:";
        static public string defaultDirectory = "";
        static public string confDirectory = "";	
		static public string logDirectory="log\\";
        public bool createNewLogfile = false;
        public bool useLDAP = true;
        public bool useLogFile = true;
		
        //private bool mySQLEnabled=false;
		
		public Configuration(Configuration source)
		{
            this.UseLDAP = source.UseLDAP;
            //this.serverName = source.serverName;
			this.serverURL=source.serverURL;
            this.portId = source.portId;
            this.deviceName = source.deviceName;

            this.adminDN=source.adminDN;
			this.container=source.container;
			this.company=source.company;
            this.description=source.description;
			this.deviceURI=source.deviceURI;
			this.serialno=source.serialno;
            this.loginDN=source.loginDN;
			this.passwd=source.passwd;

            //this.WaitTime=source.WaitTime;
            //this.WaitTimeMsg=source.WaitTimeMsg;
			this.DefaultDirectory=source.DefaultDirectory;
			this.CreateNewLogfile=source.CreateNewLogfile;
            this.UseLogFile = source.UseLogFile;
		}
		
		private void CloneConfiguration(Configuration source)
		{

            this.UseLDAP = source.UseLDAP;
            //this.serverName = source.serverName;
			this.serverURL=source.serverURL;
			this.portId=source.portId;
			this.deviceName=source.deviceName;
            this.adminDN = source.adminDN;
            this.container = source.container;
            this.company = source.company;
            this.description = source.description;
            this.deviceURI = source.deviceURI;
            this.serialno = source.serialno;
            this.loginDN = source.loginDN;
            this.passwd = source.passwd;

            //this.WaitTime=source.WaitTime;
            //this.WaitTimeMsg=source.WaitTimeMsg;
			this.DefaultDirectory=source.DefaultDirectory;
			this.CreateNewLogfile=source.CreateNewLogfile;
			this.UseLogFile=source.UseLogFile;
		}

        //public void SetLogFile(DBConfiguration.WriteToLogFile LogFile)
        //{
        //    this.MySQLConfiguration.logfile = LogFile;
        //    this.SQLServerConfiguration.logfile = LogFile;
        //}

        public Configuration()
        {
            //MySQLDBType = DBConfiguration.DBTypes.MYSQL;
            //SQLServerDBType = DBConfiguration.DBTypes.SQLSERVER;
        }

		public Configuration(bool LoadDefault)
		{
            if (LoadDefault)
			{
				TextReader xmlIn=null;
			
				try
				{
                    //DAF Display
                    String dirStr = defaultDirectory + confDirectory + DEFAULT_FILE_NAME;
                    System.Console.Out.WriteLine("log Directory = " + dirStr);

                    //MySQLDBType = DBConfiguration.DBTypes.MYSQL;
					xmlIn=new StreamReader(defaultDirectory+confDirectory+DEFAULT_FILE_NAME);
					XmlSerializer s = new XmlSerializer( typeof( Configuration ) );
					CloneConfiguration((Configuration) s.Deserialize( xmlIn ));
				}
				catch(Exception e)
				{
                    DeviceEntry.LogToFile("********* Configuration:Error:" + e.Message);
                    DeviceEntry.LogToFile("Agent Start Failed.\n Problem with " + DEFAULT_FILE_NAME);
                    throw new AgentException("Agent Start Failed.\n Problem with " + DEFAULT_FILE_NAME);
				}
				finally
				{
					if (xmlIn!=null)
						xmlIn.Close();
				}
			}
        }

        public StreamReader GetConfiguration()
        {
            //StreamWriter sw = new StreamWriter(file);
            //StreamReader sr = new StreamReader(file);

            //TextReader xmlIn = null;
            StreamReader xmlIn = null;

            try
            {
                //DAF Display
                String dirStr = defaultDirectory + confDirectory + DEFAULT_FILE_NAME;
                System.Console.Out.WriteLine("log Directory = " + dirStr);

                xmlIn = new StreamReader(defaultDirectory + confDirectory + DEFAULT_FILE_NAME);
                XmlSerializer s = new XmlSerializer(typeof(Configuration));
                CloneConfiguration((Configuration)s.Deserialize(xmlIn));
            }
            catch (Exception e)
            {
                Console.WriteLine("GetConfiguration:Error:" + e.Message);
                DeviceEntry.LogToFile("AGetConfiguration:Error: " + e.Message);
            }

            //sw.Close();


            return xmlIn;
        }

        #region MTConnectAgentCore Connection Configuration
        //properties 
		
		[XmlElement("UseLDAP")]
        public bool UseLDAP
		{
			set
			{
				useLDAP=value;
			}
			get
			{
				return useLDAP;
			}
		}

        //[XmlElement("ServerName")]
        //public string ServerName
        //{
        //    set
        //    {
        //        serverName=value;
        //    }
        //    get
        //    {
        //        return serverName;
        //    }
        //}
		
		[XmlElement("ServerURL")]
		public string ServerURL
		{
			set
			{
				serverURL=value;
			}
			get
			{
				return serverURL;
			}
		}
		
		[XmlElement("PortId")]
		public string PortId
		{
			set
			{
				portId=value;
			}
			get
			{
				return portId;
			}
		}

        [XmlElement("DeviceName")]
        public string DeviceName
        {
            set
            {
                deviceName = value;
            }
            get
            {
                return deviceName;
            }
        }

        [XmlElement("AdminDN")]
        public string AdminDN
        {
            set
            {
                adminDN = value;
            }
            get
            {
                return adminDN;
            }
        }

        [XmlElement("Container")]
        public string Container
        {
            set
            {
                container = value;
            }
            get
            {
                return container;
            }
        }
        		
        [XmlElement("Company")]
        public string Company
        {
            set
            {
                company = value;
            }
            get
            {
                return company;
            }
        }

        [XmlElement("Description")]
        public string Description
        {
            set
            {
                description = value;
            }
            get
            {
                return description;
            }
        }

        [XmlElement("DeviceURI")]
        public string DeviceURI
        {
            set
            {
                deviceURI = value;
            }
            get
            {
                return deviceURI;
            }
        }

        [XmlElement("Serialno")]
        public string Serialno
        {
            set
            {
                serialno = value;
            }
            get
            {
                return serialno;
            }
        }

        [XmlElement("LoginDN")]
        public string LoginDN
        {
            set
            {
                loginDN = value;
            }
            get
            {
                return loginDN;
            }
        }

        [XmlElement("Passwd")]
        public string Passwd
        {
            set
            {
                passwd = value;
            }
            get
            {
                return passwd;
            }
        }

        #endregion
		
		#region Internal Configuration
		
        //[XmlElement("WaitTime")]
        //public int WaitTime
        //{
        //    set
        //    {
        //        waitTime=value;
        //    }
        //    get
        //    {
        //        return waitTime;
        //    }
        //}
		
        //[XmlElement("WaitTimeMsg")]
        //public int WaitTimeMsg
        //{
        //    set
        //    {
        //        waitTimeMsg=value;
        //    }
        //    get
        //    {
        //        return waitTimeMsg;
        //    }
        //}
		
		[XmlElement("DefaultDirectory")]
		public string DefaultDirectory
		{
			set
			{
				defaultDirectory=value;
			}
			get
			{
				return defaultDirectory;
			}
		}

		[XmlElement("CreateNewLogfile")]
		public bool CreateNewLogfile
		{
			set
			{
				createNewLogfile=value;
			}
			get
			{
				return  createNewLogfile;
			}
		}
								
		[XmlElement("UseLogFile")]
		public bool UseLogFile
		{
			set{useLogFile=value;}
			get{return useLogFile;}
		}
				
        //[XmlElement("MySQLEnabled")]
        //public bool MySQLEnabled
        //{
        //    set{mySQLEnabled=value;}
        //    get{return mySQLEnabled;}
        //}
        //[XmlElement("SQLServerEnabled")]
        //public bool SQLServerEnabled
        //{
        //    set{sqlServerEnabled=value;}
        //    get{return sqlServerEnabled;}
        //}

        #endregion

    }
}