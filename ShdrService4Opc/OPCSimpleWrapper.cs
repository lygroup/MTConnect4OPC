using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using OpcLibrary;

using Utilities;

namespace OpcLibrary
{
  
    class OpcServer
    {
        public IOPCServer ifServer = null;
        private object OPCserverObj = null;
        public static RpcAuthnSrv eRpcAuthzSrv;
        public static RpcAuthnLevel eRpcAuthnLevel;
        public static RpcImpersLevel eRpcImpersLevel;
        public static string sUser ;
        public static string sPassword ;
        public static string sDomain ;
        public static int nSimpleOPCActivate;

        public OpcServer()
        {
        }
        ~OpcServer()
        {
            Disconnect();
        }

        public void Connect(Guid clsidOPCserver, string szPCName)
        {
            try
            {
                Disconnect();
                Guid[] iids = new System.Guid[1] { new System.Guid("39c13a4d-011e-11d0-9675-0020afd8adb3") }; // IOPServer - maybe unknown better
                if (nSimpleOPCActivate > 0)
                {
                    object[] ptrs = DCOM.CoCreateInstanceEx(clsidOPCserver,
                        ClsCtx.All, szPCName, iids,
                        eRpcAuthzSrv,
                        RpcAuthzSrv.None,
                        "",
                        eRpcAuthnLevel,
                        eRpcImpersLevel,
                        sDomain,
                        sUser,
                        sPassword);
                    OPCserverObj = ptrs[0];
                }
                else
                {
                    Type typeofOPCserver;

                    if (szPCName.Length > 0)
                        typeofOPCserver = Type.GetTypeFromCLSID(clsidOPCserver, szPCName);
                    else
                        typeofOPCserver = Type.GetTypeFromCLSID(clsidOPCserver);

                    if (typeofOPCserver == null)
                        Marshal.ThrowExceptionForHR(HRESULTS.E_FAIL);

                    OPCserverObj = Activator.CreateInstance(typeofOPCserver);
                }
                ifServer = (IOPCServer)OPCserverObj;
                if (ifServer == null)
                    Marshal.ThrowExceptionForHR(HRESULTS.CONNECT_E_NOCONNECTION);
                Logger.LogMessage("OPC Server Connected\n", Logger.INFORMATION );
            }
            catch (Exception e)
            {
                ifServer = null;
                Logger.LogMessage("OPC Connect Exception: " + e.Message, Logger.INFORMATION);
                throw e;
            }
        }
        public void Disconnect()
        {
            ifServer = null;
            if (!(OPCserverObj == null))
            {
                int rc = Marshal.ReleaseComObject(OPCserverObj);
                OPCserverObj = null;
            }
        }
        internal static SERVERSTATUS GetServerStatus(ref IntPtr pInput, bool deallocate)
        {
            SERVERSTATUS output = new SERVERSTATUS();;

            if (pInput != IntPtr.Zero)
            {
                tagOPCSERVERSTATUS status = (tagOPCSERVERSTATUS)Marshal.PtrToStructure(pInput, typeof(tagOPCSERVERSTATUS));

                output.szVendorInfo = status.szVendorInfo;
                output.ProductVersion = String.Format("{0}.{1}.{2}", status.wMajorVersion, status.wMinorVersion, status.wBuildNumber);
                output.eServerState = (OPCSERVERSTATE)status.dwServerState;
                output.StatusInfo = null;
                 if(output.eServerState == OPCSERVERSTATE.OPC_STATUS_RUNNING ) output.StatusInfo= "OPC_STATUS_RUNNING";
                 else  if(output.eServerState == OPCSERVERSTATE.OPC_STATUS_FAILED ) output.StatusInfo= "OPC_STATUS_FAILED";
                 else  if(output.eServerState == OPCSERVERSTATE.OPC_STATUS_NOCONFIG ) output.StatusInfo= "OPC_STATUS_NOCONFIG";
                 else  if(output.eServerState == OPCSERVERSTATE.OPC_STATUS_SUSPENDED ) output.StatusInfo= "OPC_STATUS_SUSPENDED";
                 else  if(output.eServerState == OPCSERVERSTATE.OPC_STATUS_TEST ) output.StatusInfo= "OPC_STATUS_TEST";

                 long fileT = (((long)status.ftCurrentTime.dwHighDateTime) << 32) + status.ftCurrentTime.dwLowDateTime;

                 //output.StartTime = DateTime.FromFileTime(fileT);
                 output.CurrentTime = DateTime.FromFileTime(fileT);
                 //output.LastUpdateTime = DateTime.FromFileTime(fileT);

                if (deallocate)
                {
                    Marshal.DestroyStructure(pInput, typeof(tagOPCSERVERSTATUS));
                    Marshal.FreeCoTaskMem(pInput);
                    pInput = IntPtr.Zero;
                }
            }

            return output;
        }
        public OpcGroup AddGroup(string groupName, bool setActive, int requestedUpdateRate)
        {
            return AddGroup(groupName, setActive, requestedUpdateRate, null, null, 0);
        }
        public OpcGroup AddGroup(string groupName, bool setActive, int requestedUpdateRate,
                                    int[] biasTime, float[] percentDeadband, int localeID)
        {
            if (ifServer == null)
                Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

            OpcGroup grp = new OpcGroup(ref ifServer, false, groupName, setActive, requestedUpdateRate);
            grp.internalAdd(biasTime, percentDeadband, localeID);
            return grp;
        }
        public SERVERSTATUS GetServerStatus()
        {

            lock (this)
            {
                if (ifServer == null)
                    Marshal.ThrowExceptionForHR(HRESULTS.CONNECT_E_NOCONNECTION);

                // initialize arguments.
                System.IntPtr pStatus = IntPtr.Zero;

                // invoke COM method.
                try
                {
                    ((IOPCServer)ifServer).GetStatus(out pStatus);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Print(e.Message);
                    Marshal.ThrowExceptionForHR(HRESULTS.E_FAIL);
                }

                // return status.
                return GetServerStatus(ref pStatus, true);
            }

        }
        public bool IsConnected()
        {
            return ifServer != null;

        }

    }

    public class SERVERSTATUS
    {
        public DateTime StartTime;
        public DateTime CurrentTime;
        public DateTime LastUpdateTime;

        public OPCSERVERSTATE eServerState;
        public string StatusInfo;

        public int dwGroupCount;
        public int dwBandWidth;
        public short wMajorVersion;
        public short wMinorVersion;
        public short wBuildNumber;
        public short wReserved;

        public string szVendorInfo;
        public string ProductVersion;
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(szVendorInfo + "\n");
            sb.AppendFormat("Updated {0}\n", CurrentTime.ToString());
            sb.AppendFormat("Version {0}\n", ProductVersion);

            return sb.ToString();



        }

    };

  

}
