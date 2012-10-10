using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpcLibrary;

namespace OpcLibrary
{
    public enum OPCACCESSRIGHTS
    {
        OPC_READABLE = 1,
        OPC_WRITEABLE = 2
    }

    public enum OPCDATASOURCE
    {
        OPC_DS_CACHE = 1,
        OPC_DS_DEVICE = 2
    }

    public enum OPCEUTYPE
    {
        OPC_NOENUM = 0,
        OPC_ANALOG = 1,
        OPC_ENUMERATED = 2
    }
    // ----------------------------------------------------------------- Item Mgmt
    [ComVisible(true), ComImport,
    Guid("39c13a54-011e-11d0-9675-0020afd8adb3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOPCItemMgt
    {
        [PreserveSig]
        int AddItems(
            [In]											int dwCount,
            [In]											IntPtr pItemArray,
            [Out]										out IntPtr ppAddResults,
            [Out]										out	IntPtr ppErrors);

        [PreserveSig]
        int ValidateItems(
            [In]											int dwCount,
            [In]											IntPtr pItemArray,
            [In, MarshalAs(UnmanagedType.Bool)]			bool bBlobUpdate,
            [Out]										out	IntPtr ppValidationResults,
            [Out]										out	IntPtr ppErrors);

        [PreserveSig]
        int RemoveItems(
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phServer,
            [Out]													out	IntPtr ppErrors);

        [PreserveSig]
        int SetActiveState(
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phServer,
            [In, MarshalAs(UnmanagedType.Bool)]						bool bActive,
            [Out]													out	IntPtr ppErrors);

        [PreserveSig]
        int SetClientHandles(
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phServer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phClient,
            [Out]													out	IntPtr ppErrors);

        [PreserveSig]
        int SetDatatypes(
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phServer,
            [In]														IntPtr pRequestedDatatypes,
            [Out]													out	IntPtr ppErrors);

        [PreserveSig]
        int CreateEnumerator(
            [In]										ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]	out	object ppUnk);

    }	// interface IOPCItemMgt



    // ----------------------------------------------------------------- Sync IO
    [ComVisible(true), ComImport,
    Guid("39c13a52-011e-11d0-9675-0020afd8adb3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOPCSyncIO
    {
        [PreserveSig]
        int Read(
            [In, MarshalAs(UnmanagedType.U4)]							OPCDATASOURCE dwSource,
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]	int[] phServer,
            [Out]													out IntPtr ppItemValues,
            [Out]													out	IntPtr ppErrors);

        [PreserveSig]
        int Write(
            [In]														int dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	int[] phServer,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	object[] pItemValues,
            [Out]													out	IntPtr ppErrors);

    }	// interface IOPCSyncIO

    [ComVisible(true),
    Guid("39c13a50-011e-11d0-9675-0020afd8adb3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOPCGroupStateMgt
    {
        void GetState(
            [Out]										out	int pUpdateRate,
            [Out, MarshalAs(UnmanagedType.Bool)]		out	bool pActive,
            [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string ppName,
            [Out]										out	int pTimeBias,
            [Out]										out	float pPercentDeadband,
            [Out]										out	int pLCID,
            [Out]										out	int phClientGroup,
            [Out]										out	int phServerGroup);

        void SetState(
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]											int[] pRequestedUpdateRate,
            [Out]																					out	int pRevisedUpdateRate,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Bool, SizeConst = 1)]		bool[] pActive,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]											int[] pTimeBias,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]		float[] pPercentDeadband,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]		int[] pLCID,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]		int[] phClientGroup);

        void SetName(
            [In, MarshalAs(UnmanagedType.LPWStr)]			string szName);

        void CloneGroup(
            [In, MarshalAs(UnmanagedType.LPWStr)]			string szName,
            [In]										ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]	out	object ppUnk);

    }	// interface IOPCGroupStateMgt

    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum OPCSERVERSTATE
    {
        OPC_STATUS_RUNNING = 1,
        OPC_STATUS_FAILED = 2,
        OPC_STATUS_NOCONFIG = 3,
        OPC_STATUS_SUSPENDED = 4,
        OPC_STATUS_TEST = 5
    }

    public struct _FILETIME
    {
        public uint dwHighDateTime;
        public uint dwLowDateTime;
    }
    public struct tagOPCSERVERSTATUS
    {
        public System.Runtime.InteropServices.ComTypes.FILETIME ftStartTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCurrentTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastUpdateTime;
        public OPCSERVERSTATE dwServerState;
        public uint dwGroupCount;
        public uint dwBandWidth;
        public ushort wMajorVersion;
        public ushort wMinorVersion;
        public ushort wBuildNumber;
        public ushort wReserved;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szVendorInfo;
    }

    public struct tagOPCITEMSTATE
    {
        public int hClient;  // OPCHANDLE
        public System.Runtime.InteropServices.ComTypes.FILETIME ftTimeStamp;
        public ushort wQuality;
        public ushort wReserved;
        public object vDataValue;
    } 
    public class HRESULTS
    {
        public static bool Failed(int hresultcode)
        { return (hresultcode < 0); }

        public static bool Succeeded(int hresultcode)
        { return (hresultcode >= 0); }

        public const int S_OK = 0x00000000;
        public const int S_FALSE = 0x00000001;

        public const int E_NOTIMPL = unchecked((int)0x80004001);		// winerror.h
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const int E_ABORT = unchecked((int)0x80004004);
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        public const int E_INVALIDARG = unchecked((int)0x80070057);

        public const int CONNECT_E_NOCONNECTION = unchecked((int)0x80040200);		// olectl.h
        public const int CONNECT_E_ADVISELIMIT = unchecked((int)0x80040201);

        public const int OPC_E_INVALIDHANDLE = unchecked((int)0xC0040001);		// opcerror.h
        public const int OPC_E_BADTYPE = unchecked((int)0xC0040004);
        public const int OPC_E_PUBLIC = unchecked((int)0xC0040005);
        public const int OPC_E_BADRIGHTS = unchecked((int)0xC0040006);
        public const int OPC_E_UNKNOWNITEMID = unchecked((int)0xC0040007);
        public const int OPC_E_INVALIDITEMID = unchecked((int)0xC0040008);
        public const int OPC_E_INVALIDFILTER = unchecked((int)0xC0040009);
        public const int OPC_E_UNKNOWNPATH = unchecked((int)0xC004000A);
        public const int OPC_E_RANGE = unchecked((int)0xC004000B);
        public const int OPC_E_DUPLICATENAME = unchecked((int)0xC004000C);
        public const int OPC_S_UNSUPPORTEDRATE = unchecked((int)0x0004000D);
        public const int OPC_S_CLAMP = unchecked((int)0x0004000E);
        public const int OPC_S_INUSE = unchecked((int)0x0004000F);
        public const int OPC_E_INVALIDCONFIGFILE = unchecked((int)0xC0040010);
        public const int OPC_E_NOTFOUND = unchecked((int)0xC0040011);
        public const int OPC_E_INVALID_PID = unchecked((int)0xC0040203);

    }	// class HRESULTS
#if ouch
   public enum OPCENUMSCOPE
    {
        OPC_ENUM_PRIVATE_CONNECTIONS = 1,
        OPC_ENUM_PUBLIC_CONNECTIONS = 2,
        OPC_ENUM_ALL_CONNECTIONS = 3,
        OPC_ENUM_PRIVATE = 4,
        OPC_ENUM_PUBLIC = 5,
        OPC_ENUM_ALL = 6,
    }
    // ----------------------------------------------------------------- SERVER
    [Guid("39C13A4D-011E-11D0-9675-0020AFD8ADB3")]
    [InterfaceType(1)]
    [ComConversionLoss]
    public interface IOPCServer
    {
        void AddGroup(string szName, int bActive, int dwRequestedUpdateRate, int hClientGroup, IntPtr pTimeBias, IntPtr pPercentDeadband, int dwLCID, out int phServerGroup, out int pRevisedUpdateRate, ref Guid riid, out object ppUnk);
        void CreateGroupEnumerator(OPCENUMSCOPE dwScope, ref Guid riid, out object ppUnk);
        void GetErrorString(int dwError, int dwLocale, out string ppString);
        void GetGroupByName(string szName, ref Guid riid, out object ppUnk);
        void GetStatus(out IntPtr ppServerStatus);
        void RemoveGroup(int hServerGroup, int bForce);
    }
#endif

    [ComVisible(true), ComImport,
    Guid("39c13a4d-011e-11d0-9675-0020afd8adb3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOPCServer
    {
        void AddGroup(
            [In, MarshalAs(UnmanagedType.LPWStr)]					string szName,
            [In, MarshalAs(UnmanagedType.Bool)]					bool bActive,
            [In]													int dwRequestedUpdateRate,
            [In]													int hClientGroup,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]		int[] pTimeBias,
            [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]		float[] pPercentDeadband,
            [In]													int dwLCID,
            [Out]													out	int phServerGroup,
            [Out]													out	int pRevisedUpdateRate,
            [In]													ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]				out	object ppUnk);

        void GetErrorString(
            [In]											int dwError,
            [In]											int dwLocale,
            [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string ppString);

        void GetGroupByName(
            [In, MarshalAs(UnmanagedType.LPWStr)]			string szName,
            [In]										ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]	out	object ppUnk);

        void GetStatus(
            [Out]	out IntPtr ppServerStatus);

        void RemoveGroup(
            [In]										int hServerGroup,
            [In, MarshalAs(UnmanagedType.Bool)]			bool bForce);

        [PreserveSig]
        int CreateGroupEnumerator(										// may return S_FALSE
            [In]											int dwScope,
            [In]										ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]	out	object ppUnk);

    }	// interface IOPCServer

    //****************************************************
    // OPC Quality flags
    [Flags]
    public enum OPC_QUALITY_MASKS : short
    {
        LIMIT_MASK = 0x0003,
        STATUS_MASK = 0x00FC,
        MASTER_MASK = 0x00C0,
    }

    [Flags]
    public enum OPC_QUALITY_MASTER : short
    {
        QUALITY_BAD = 0x0000,
        QUALITY_UNCERTAIN = 0x0040,
        ERROR_QUALITY_VALUE = 0x0080,		// non standard!
        QUALITY_GOOD = 0x00C0,
    }

    [Flags]
    public enum OPC_QUALITY_STATUS : short
    {
        BAD = 0x0000,	// STATUS_MASK Values for Quality = BAD
        CONFIG_ERROR = 0x0004,
        NOT_CONNECTED = 0x0008,
        DEVICE_FAILURE = 0x000c,
        SENSOR_FAILURE = 0x0010,
        LAST_KNOWN = 0x0014,
        COMM_FAILURE = 0x0018,
        OUT_OF_SERVICE = 0x001C,

        UNCERTAIN = 0x0040,	// STATUS_MASK Values for Quality = UNCERTAIN
        LAST_USABLE = 0x0044,
        SENSOR_CAL = 0x0050,
        EGU_EXCEEDED = 0x0054,
        SUB_NORMAL = 0x0058,

        OK = 0x00C0,	// STATUS_MASK Value for Quality = GOOD
        LOCAL_OVERRIDE = 0x00D8
    }

    [Flags]
    public enum OPC_QUALITY_LIMIT
    {
        LIMIT_OK = 0x0000,
        LIMIT_LOW = 0x0001,
        LIMIT_HIGH = 0x0002,
        LIMIT_CONST = 0x0003
    }
    // ------------------ INTERNAL item level structs ------------------

    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Unicode)]
    internal class OPCITEMDEFintern
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szAccessPath;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string szItemID;

        [MarshalAs(UnmanagedType.Bool)]
        public bool bActive;

        public int hClient;
        public int dwBlobSize;
        public IntPtr pBlob;

        public short vtRequestedDataType;

        public short wReserved;
    };




    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class OPCITEMRESULTintern
    {
        public int hServer = 0;
        public short vtCanonicalDataType = 0;
        public short wReserved = 0;

        [MarshalAs(UnmanagedType.U4)]
        public OPCACCESSRIGHTS dwAccessRights = 0;

        public int dwBlobSize = 0;
        public int pBlob = 0;
    };

}
