/******************************************************************************
* The MIT License
* Copyright (c) 2003 Novell Inc.  www.novell.com
* 
* Permission is hereby granted, free of charge, to any person obtaining  a copy
* of this software and associated documentation files (the Software), to deal
* in the Software without restriction, including  without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
* copies of the Software, and to  permit persons to whom the Software is 
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in 
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*******************************************************************************/
//
// Samples.AddEntry.cs
//
// Author:
//   Sunil Kumar (Sunilk@novell.com)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
//
using System;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace MTConnectAgentCore
{

    class DeviceEntry
    {
        private static bool LOAD_DEFAULT_CONFIGURATION = true;
        static public Configuration config = new Configuration(LOAD_DEFAULT_CONFIGURATION);
        //static private System.Diagnostics.EventLog eventLog1;	//true to load default.ini	
        public string serverURI = "ldap.mtconnect.org";
        public string portId = "389";
        public string loginDN = "cn=Agent,ou=accounts,dc=mtconnect,dc=org";
        public string passwd = "mtc0nnect";
        public string searchbase = "dc=mtconnect,dc=org";
        public string deviceURI = @"http://banyan.marc.gatech.edu";
        public string container = "ou=Devices,dc=mtconnect,dc=org";
        public string company = "GeorgiaTech";
        public string descript = "GT MTC Devices";
        public string deviceName = "myMTCAgent";
        public string serialno = "777777";
        public int  waitTime = 100;
        public int  waitTimeMsg = 100;
        public bool useLDAP = false;

        public DeviceEntry() { }

        public void LDAPDeviceEntry()
        {
            try
            {
                DeviceEntry.LogToFile("********** Starting LDAP DeviceEntry *************");
                deviceURI = @config.DeviceURI;
                serverURI = config.ServerURL;
                useLDAP = config.UseLDAP;
                portId = config.PortId;
                if (!useLDAP)
                {
                    DeviceEntry.LogToFile("DeviceEntry:Use of LDAP not selected (useLDAP is false)");
                }
                else
                {
                    DeviceEntry.LogToFile("DeviceEntry:PortId = :" + config.PortId);
                    loginDN = config.LoginDN;
                    passwd = config.Passwd;
                    searchbase = config.AdminDN;
                    container = config.Container;
                    company = config.Company;
                    deviceName = config.DeviceName;
                    descript = config.Description;
                    serialno = config.Serialno;
                    DeviceEntry.LogToFile("DeviceEntry:descript = :" + config.Description);
                    string newDN = "(cn=" + deviceName + ",ou=" + company + "," + container + ")";    // Works to check for a device presence
                    //LDAPSearch:cn=LinuxEMC1,ou=NIST,ou=Devices,dc=mtconnect,dc=org
                    string ret1 = DeviceEntry.SearchLDAP(serverURI, portId, loginDN, passwd, searchbase, newDN);
                    DeviceEntry.LogToFile("DeviceEntry::Return = :" + ret1);
                    if (ret1.Equals("OK"))
                    {
                        SleepFor(waitTime);
                        string ret2 = AddRootLDAP(serverURI, portId, loginDN, passwd, container, company, descript);
                        DeviceEntry.LogToFile("DeviceEntry:AddRoot:Return = :" + ret2);
                        SleepFor(waitTimeMsg);
                        string ret3 = AddEntryLDAP(serverURI, portId, loginDN, passwd, container, deviceName,
                            company, deviceURI, descript, serialno);
                        DeviceEntry.LogToFile("DeviceEntry:AddEntry:Return = :" + ret3);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                LogToFile("DeviceEntry:Exception Loading mtcagent.ini failed. " + e.Message);
                throw new AgentException("Error:Loading mtcagent.ini failed. ", e);
                //return "Error";
            }
        }

        public static string SearchLDAP(string ldapHost, string port,
                string loginDN, string password,
                string searchBase, string searchFilter)
        {
            int ldapPort = System.Convert.ToInt32(port);
            LogToFile("************ DeviceEntry started: ldapHost = " + ldapHost + " ************");

            try
            {
                LdapConnection conn = new LdapConnection();
                Console.WriteLine("Connecting to:" + ldapHost);
                LogToFile("DeviceEntry: Connecting to:" + ldapHost);
                conn.Connect(ldapHost, ldapPort);
                conn.Bind(loginDN, password);
                LdapSearchResults lsc = conn.Search(searchBase,
                                LdapConnection.SCOPE_SUB,
                                searchFilter,
                                null,
                                false);

                while (lsc.hasMore())
                {
                    Console.WriteLine("DAF:lsc.hasMore\n");
                    LogToFile("\n");
                    LdapEntry nextEntry = null;
                    try
                    {
                        nextEntry = lsc.next();
                    }
                    catch (LdapException e)
                    {
                        Console.WriteLine("LdapException: " + e.LdapErrorMessage);
                        LogToFile("DeviceEntry:LdapInfo " + e.LdapErrorMessage);
                        // Exception is thrown, go for next entry
                        continue;
                    }
                    Console.WriteLine("*********************************\n" + nextEntry.DN);
                    LogToFile("DeviceEntry:" + nextEntry.DN);
                    LdapAttributeSet attributeSet = nextEntry.getAttributeSet();
                    System.Collections.IEnumerator ienum = attributeSet.GetEnumerator();
                    while (ienum.MoveNext())
                    {
                        LdapAttribute attribute = (LdapAttribute)ienum.Current;
                        string attributeName = attribute.Name;
                        string attributeVal = attribute.StringValue;
                        if (!Base64.isLDIFSafe(attributeVal))
                        {
                            byte[] tbyte = SupportClass.ToByteArray(attributeVal);
                            attributeVal = Base64.encode(SupportClass.ToSByteArray(tbyte));
                        }
                        Console.WriteLine(attributeName + "  value = :" + attributeVal);
                        LogToFile("DeviceEntry: Attribute:" + attributeName + ":  value = :" + attributeVal);
                    }
                }
                conn.Disconnect();
            }
            catch (LdapException e)
            {
                Console.WriteLine("Error:" + e.LdapErrorMessage);
                LogToFile("DeviceEntry:LdapInfo:" + e.LdapErrorMessage);
                return "Error";
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                LogToFile("DeviceEntry:Exception Error:" + e.Message);
                return "Error";
            }
            LogToFile("\n");
            return "OK";
        }

        public static string AddRootLDAP(string ldapHost, string port, string loginDN, string password,
                    string containerName, string company, string descript)
        {
            int ldapPort = System.Convert.ToInt32(port);
            LogToFile("************ LDAPAddEntry started: ldapHost = " + ldapHost + " ************");
            try
            {
                /****************************** ADDing a Main Company ********************************/
                LdapAttributeSet attributeSet = new LdapAttributeSet();
                attributeSet.Add(new LdapAttribute("objectclass", "organizationalUnit"));
                attributeSet.Add(new LdapAttribute("objectclass", "top"));
                attributeSet.Add(new LdapAttribute("description", descript));
                attributeSet.Add(new LdapAttribute("ou", company));
                string dn = "ou=" + company + ",ou=Devices,dc=mtconnect,dc=org";
                LogToFile("AddRootLDAP:  dn = " + dn);
                LdapEntry newEntry = new LdapEntry(dn, attributeSet);
                LdapConnection conn = new LdapConnection();
                //LogToFile("AddRootLDAP: Connecting to:" + ldapHost);
                conn.Connect(ldapHost, ldapPort);
                conn.Bind(loginDN, password);
                conn.Add(newEntry);
                LogToFile("AddRootLDAP: Entry:" + dn + "  Added Successfully");
                conn.Disconnect();
            }
            catch (LdapException e)
            {
                Console.WriteLine("Error:" + e.LdapErrorMessage);
                LogToFile("AddRootLDAP:LdapInfo:" + e.Message);
                return "Error";
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                LogToFile("AddRootLDAP:Exception Error:" + e.Message);
                return "Error";
            }
            Console.WriteLine("\n");
            return "OK";
        }

        public static string AddEntryLDAP(string ldapHost, string port, string loginDN, string password,
                    string containerName, string deviceId, string company,
                    string deviceURI, string descript, string serialno)
	    {
            int ldapPort = System.Convert.ToInt32(port);
            LogToFile("************ LDAPAddEntry started: ldapHost = " + ldapHost + " ************");

            try
            {
                LdapAttributeSet attributeSet = new LdapAttributeSet();
                /**** Adding a Specific Device ******************************************/
                attributeSet.Add(new LdapAttribute("cn", deviceId));
                attributeSet.Add(new LdapAttribute("labeledURI", deviceURI));
                attributeSet.Add(new LdapAttribute("objectclass", new string[] 
                            { "device", "labeledURIObject", "top" }));
                attributeSet.Add(new LdapAttribute("o", company));
                attributeSet.Add(new LdapAttribute("description", new string[] 
                            { "iso841Class: 6", descript }));
                attributeSet.Add(new LdapAttribute("serialNumber", serialno));
                string dn = "cn=" + deviceId + ",ou=" + company + "," + containerName;
                /***********************************************************************/
                LogToFile("LDAPAddEntry:  dn = " + dn);

                LdapEntry newEntry = new LdapEntry(dn, attributeSet);
                LdapConnection conn= new LdapConnection();
                //LogToFile("LDAPAddEntry: Connecting to:" + ldapHost);
                conn.Connect(ldapHost, ldapPort);
                conn.Bind(loginDN,password);
		        conn.Add( newEntry );
			    Console.WriteLine("Entry:" + dn + "  Added Successfully");
                LogToFile("LDAPAddEntry: Entry:" + dn + "  Added Successfully");
                conn.Disconnect();
            }
            catch(LdapException e)
            {
                LogToFile("LDAPAddEntry:LdapInfo:" + e.Message);
                return "Error";
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                LogToFile("LDAPAddEntry:Exception Error:" + e.Message);
                return "Error";
            }
            Console.WriteLine("\n");
            return "OK";
        }

        #region Common Methods
        public static void LogToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                "DeviceEntry.log");
            try
            {
                string logLine = System.String.Format(
                    "<Log> {0:G}: {1} </Log>", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

        public void SleepFor(int millis)
        {
            int milli = millis * 1000;
            int dtime = 0;
            for (int j = 0; j <= milli; j++)
            {
                dtime = dtime + 1;
            }
        }
        #endregion Common Methods

    }
}

