using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace OpcLibrary
{

 
        [Flags]
        public enum ClsCtx
        {
            Inproc = 0x03,
            Server = 0x15,
            All = 0x17,

            InprocServer = 0x1,
            InprocHandler = 0x2,
            LocalServer = 0x4,
            InprocServer16 = 0x8,
            RemoteServer = 0x10,
            InprocHandler16 = 0x20,
            InprocServerX86 = 0x40,
            InprocHandlerX86 = 0x80,
            EServerHandler = 0x100,
            Reserved = 0x200,
            NoCodeDownload = 0x400,
            NoWX86Translation = 0x800,
            NoCustomMarshal = 0x1000,
            EnableCodeDownload = 0x2000,
            NoFailureLog = 0x4000,
            DisableAAA = 0x8000,
            EnableAAA = 0x10000,
            FromDefaultContext = 0x20000,
        }

        public enum RpcAuthnSrv
        {                                    // RPC_C_AUTHN_xxx
            None = 0,
            DcePrivate = 1,
            DcePublic = 2,
            DecPublic = 4,
            GssNegotiate = 9,
            WinNT = 10,
            GssSchannel = 14,
            GssKerberos = 16,
            DPA = 17,
            MSN = 18,
            Digest = 21,
            MQ = 100,
            Default = -1
        }
 
       // RPC_C_AUTHZ_NONE 0 The server performs no authorization. Currently, RPC_C_AUTHN_WINNT, RPC_C_AUTHN_GSS_SCHANNEL, and RPC_C_AUTHN_GSS_KERBEROS all use only RPC_C_AUTHZ_NONE.
        public enum RpcAuthzSrv
        {                                    // RPC_C_AUTHZ_xxx
            None = 0,
            Name = 1,
            DCE = 2,
            Default = -1
        }

        public enum RpcAuthnLevel
        {                                // RPC_C_AUTHN_LEVEL_xxx
            Default = 0,
            None = 1,
            Connect = 2,
            Call = 3,
            Pkt = 4,
            PktIntegrity = 5,
            PktPrivacy = 6
        }

        public enum RpcImpersLevel
        {                                // RPC_C_IMP_LEVEL_xxx
            Default = 0,
            Anonymous = 1,
            Identify = 2,
            Impersonate = 3,
            Delegate = 4
        }
 

        public class DCOM
        {
            public static object[] CoCreateInstanceEx(Guid clsid, ClsCtx ctx, string servername, Guid[] iids,
                                                        RpcAuthnSrv authent, RpcAuthzSrv author, string serverprinc,
                                                        RpcAuthnLevel level, RpcImpersLevel impers,
                                                        string domain,  string user, string password)
            {
                int num = iids.Length;
                MULTI_QI[] amqi = new MULTI_QI[num];

                IntPtr guidbuf = Marshal.AllocCoTaskMem(num * 16);    // allocate memory for IIDs
                for (int i = 0; i < num; i++)
                {
                    IntPtr piid = (IntPtr)((int)guidbuf + (i * 16));
                    Marshal.StructureToPtr(iids[i], piid, false);
                    amqi[i] = new MULTI_QI(piid);
                }

                COAUTHIDENTITY ci = new COAUTHIDENTITY(user, domain, password);
                IntPtr ciptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(ci));
                Marshal.StructureToPtr(ci, ciptr, false);

                COAUTHINFO ca;
                if (string.IsNullOrEmpty(user))
                {
                    ca = new COAUTHINFO(authent, author, serverprinc, level, impers, IntPtr.Zero /* ptr to coauth*/);
                }
                else
                {
                    ca = new COAUTHINFO(authent, author, serverprinc, level, impers, ciptr);

                }
                IntPtr captr = Marshal.AllocCoTaskMem(Marshal.SizeOf(ca));
                Marshal.StructureToPtr(ca, captr, false);

                COSERVERINFO cs = new COSERVERINFO(servername, captr);

                int hr = CoCreateInstanceEx(ref clsid, IntPtr.Zero, (int)ctx, cs, num, amqi);

                Marshal.DestroyStructure(captr, typeof(COAUTHINFO));
                Marshal.FreeCoTaskMem(captr);
                Marshal.FreeCoTaskMem(guidbuf);

                if (hr < 0)                    // FAILED()
                {
                    Marshal.DestroyStructure(ciptr, typeof(COAUTHIDENTITY));
                    Marshal.FreeCoTaskMem(ciptr);
                    return null;
                }
  
                int refcount;
                object[] ifret = new object[num];
                for (int i = 0; i < num; i++)
                {
                    if (amqi[i].hr != 0)
                    {
                        ifret[i] = (int)amqi[i].hr;
                        continue;
                    }

                    IntPtr ip = amqi[i].pItf;
                    amqi[i].pItf = IntPtr.Zero;
                    ifret[i] = Marshal.GetObjectForIUnknown(ip);
                    refcount = Marshal.Release(ip);
                    continue;
#if COMPROXY
 
                    hr = CoSetProxyBlanket(ip, authent, author, serverprinc, level, impers, ciptr, 0);
                    if (hr < 0)                    // FAILED()
                        ifret[i] = (int)hr;
                    else
                        ifret[i] = Marshal.GetObjectForIUnknown(ip);

                    refcount = Marshal.Release(ip);
 #endif
                }

                Marshal.DestroyStructure(ciptr, typeof(COAUTHIDENTITY));
                Marshal.FreeCoTaskMem(ciptr);

                return ifret;
            }

            [DllImport("ole32.dll")]
            private static extern int CoCreateInstanceEx(ref Guid clsid, IntPtr pUnkOuter,
                                                            int dwClsContext, [In, Out] COSERVERINFO srv,
                                                            int num, [In, Out] MULTI_QI[] amqi);
            [DllImport("ole32.dll")]
            private static extern int CoSetProxyBlanket(IntPtr pProxy, RpcAuthnSrv authent, RpcAuthzSrv author,
                                                            string serverprinc, RpcAuthnLevel level, RpcImpersLevel impers,
                                                            IntPtr ciptr, int dwCapabilities);
            [DllImport("ole32.dll")]
            public static extern int CoInitializeSecurity(IntPtr pVoid, int cAuthSvc, IntPtr asAuthSvc, IntPtr pReserved1,
                RpcAuthnLevel level, RpcImpersLevel impers, IntPtr pAuthList, int dwCapabilities, IntPtr pReserved3);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct MULTI_QI
        {
            public MULTI_QI(IntPtr pid)
            {
                piid = pid;
                pItf = IntPtr.Zero;
                hr = 0;
            }
            private IntPtr piid;        // 'Guid' can't be marshaled to GUID* here? use IntPtr buffer trick instead
            public IntPtr pItf;
            public int hr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal class COSERVERINFO
        {
            public COSERVERINFO(string srvname, IntPtr authinf)
            {
                servername = srvname;
                authinfo = authinf;
                reserved1 = 0;
                reserved2 = 0;
            }
            private int reserved1;
            [MarshalAs(UnmanagedType.LPWStr)]
            private string servername;
            private IntPtr authinfo;                // COAUTHINFO*
            private int reserved2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal class COAUTHINFO
        {
            public COAUTHINFO(RpcAuthnSrv authent, RpcAuthzSrv author, string serverprinc,
                                RpcAuthnLevel level, RpcImpersLevel impers, IntPtr ciptr)
            {
                authnsvc = authent;
                authzsvc = author;
                serverprincname = serverprinc;
                authnlevel = level;
                impersonationlevel = impers;
                authidentitydata = ciptr;
            }

            private RpcAuthnSrv authnsvc;
            private RpcAuthzSrv authzsvc;
            [MarshalAs(UnmanagedType.LPWStr)]
            private string serverprincname;
            private RpcAuthnLevel authnlevel;
            private RpcImpersLevel impersonationlevel;
            private IntPtr authidentitydata;        // COAUTHIDENTITY*
            private int capabilities = 0;        // EOAC_NONE
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        internal class COAUTHIDENTITY
        {
            public COAUTHIDENTITY(string usr, string dom, string pwd)
            {
                user = usr;
                if (user == null)
                    userlen = 0;
                else
                    userlen = user.Length;

                domain = dom;
                if (domain == null)
                    domainlen = 0;
                else
                    domainlen = domain.Length;

                password = pwd;
                if (password == null)
                    passwordlen = 0;
                else
                    passwordlen = password.Length;
            }

            [MarshalAs(UnmanagedType.LPWStr)]
            private string user;
            private int userlen;

            [MarshalAs(UnmanagedType.LPWStr)]
            private string domain;
            private int domainlen;

            [MarshalAs(UnmanagedType.LPWStr)]
            private string password;
            private int passwordlen;

            private int flags = 2;        // SEC_WINNT_AUTH_IDENTITY_UNICODE
        }
}
