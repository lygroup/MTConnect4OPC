using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpcLibrary;

namespace OpcLibrary
{

    public class OPCItemDef
    {
        public OPCItemDef() { }
        public OPCItemDef(string id, bool activ, int hclt, VarEnum vt)
        { ItemID = id; Active = activ; HandleClient = hclt; RequestedDataType = vt; }

        public string AccessPath = "";
        public string ItemID;
        public bool Active;
        public int HandleClient;
        public byte[] Blob = null;
        public VarEnum RequestedDataType;
    };

    public class OPCItemResult
    {
        public int Error;			// content below only valid if Error=S_OK
        public int HandleServer;
        public VarEnum CanonicalDataType;
        public OPCACCESSRIGHTS AccessRights;
        public byte[] Blob;
    }

    public class OPCItemState
    {
        public int Error;			// content below only valid if Error=S_OK
        public int HandleClient;	// always valid for callbacks
        public object DataValue;
        public long TimeStamp;
        public short Quality;


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("OPCIST: ", 256);
            sb.AppendFormat("error=0x{0:x} hclt=0x{1:x}", Error, HandleClient);
            if (Error == HRESULTS.S_OK)
            {
                sb.AppendFormat(" val={0} time={1} qual=", DataValue, TimeStamp);
                sb.Append(OpcGroup.QualityToString(Quality));
            }

            return sb.ToString();
        }
    }



    public class OPCWriteResult
    {
        public int Error;
        public int HandleClient;
    }


    public class OPCItemAttributes
    {
        public string AccessPath;
        public string ItemID;
        public bool Active;
        public int HandleClient;
        public int HandleServer;
        public OPCACCESSRIGHTS AccessRights;
        public VarEnum RequestedDataType;
        public VarEnum CanonicalDataType;
        public OPCEUTYPE EUType;
        public object EUInfo;
        public byte[] Blob;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("OPCIAT: '", 512);
            sb.Append(ItemID); sb.Append("' ('"); sb.Append(AccessPath);
            sb.AppendFormat("') hc=0x{0:x} hs=0x{1:x} act={2}", HandleClient, HandleServer, Active);
            sb.AppendFormat("\r\n\tacc={0} typr={1} typc={2}", AccessRights, RequestedDataType, CanonicalDataType);
            sb.AppendFormat("\r\n\teut={0} eui={1}", EUType, EUInfo);
            if (!(Blob == null))
                sb.AppendFormat(" blob size={0}", Blob.Length);

            return sb.ToString();
        }
    }



    public struct OPCGroupState
    {
        public string Name;
        public bool Public;
        public int UpdateRate;
        public bool Active;
        public int TimeBias;
        public float PercentDeadband;
        public int LocaleID;
        public int HandleClient;
        public int HandleServer;
    }

    public class OpcGroup
    {
        private OPCGroupState state;
        private IOPCServer ifServer = null;
        private IOPCGroupStateMgt ifMgt = null;
        private IOPCItemMgt ifItems = null;
        private IOPCSyncIO ifSync = null;

        // marshaling helpers:
        private readonly Type typeOPCITEMDEF;
        private readonly int sizeOPCITEMDEF;
        private readonly Type typeOPCITEMRESULT;
        private readonly int sizeOPCITEMRESULT;

        public OpcGroup(ref IOPCServer ifServerLink, bool isPublic, string groupName, bool setActive, int requestedUpdateRate)
        {
            ifServer = ifServerLink;

            state.Name = groupName;
            state.Public = isPublic;
            state.UpdateRate = requestedUpdateRate;
            state.Active = setActive;
            state.TimeBias = 0;
            state.PercentDeadband = 0.0f;
            state.LocaleID = 0;
            state.HandleClient = this.GetHashCode();
            state.HandleServer = 0;

            // marshaling helpers:
            typeOPCITEMDEF = typeof(OPCITEMDEFintern);
            sizeOPCITEMDEF = Marshal.SizeOf(typeOPCITEMDEF);
            typeOPCITEMRESULT = typeof(OPCITEMRESULTintern);
            sizeOPCITEMRESULT = Marshal.SizeOf(typeOPCITEMRESULT);
        }

        ~OpcGroup()
        {
            try
            {
                Remove(false);
            }
            catch (Exception) { }
        }
        public void internalAdd(int[] biasTime, float[] percentDeadband, int localeID)
        {
            Type typGrpMgt = typeof(IOPCGroupStateMgt);
            Guid guidGrpTst = typGrpMgt.GUID;
            object objtemp;
            try
            {
                ifServer.AddGroup(state.Name, state.Active, state.UpdateRate, state.HandleClient, null, null, 0,
                        out state.HandleServer, out state.UpdateRate, ref guidGrpTst, out objtemp);

                if (objtemp == null)
                    Marshal.ThrowExceptionForHR(HRESULTS.E_NOINTERFACE);
                ifMgt = (IOPCGroupStateMgt)objtemp;
                ifItems = (IOPCItemMgt)ifMgt;
                ifSync = (IOPCSyncIO)ifMgt;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print(e.Message);
                throw e;

            }
        }

        public static string QualityToString(short Quality)
        {
            StringBuilder sb = new StringBuilder(256);
            OPC_QUALITY_MASTER oqm = (OPC_QUALITY_MASTER)(Quality & (short)OPC_QUALITY_MASKS.MASTER_MASK);
            OPC_QUALITY_STATUS oqs = (OPC_QUALITY_STATUS)(Quality & (short)OPC_QUALITY_MASKS.STATUS_MASK);
            OPC_QUALITY_LIMIT oql = (OPC_QUALITY_LIMIT)(Quality & (short)OPC_QUALITY_MASKS.LIMIT_MASK);
            sb.AppendFormat("{0}+{1}+{2}", oqm, oqs, oql);
            return sb.ToString();
        }

        public void Remove(bool bForce)
        {
            ifItems = null;
            ifSync = null;

            if (!(ifMgt == null))
            {
                int rc = Marshal.ReleaseComObject(ifMgt);
                ifMgt = null;
            }

            if (!(ifServer == null))
            {
                if (!state.Public)
                    ifServer.RemoveGroup(state.HandleServer, bForce);
                ifServer = null;
            }

            state.HandleServer = 0;
        }

        public bool AddItems(OPCItemDef[] arrDef, out OPCItemResult[] arrRes)
        {
            arrRes = null;
            bool hasblobs = false;
            int count = arrDef.Length;

            IntPtr ptrDef = Marshal.AllocCoTaskMem(count * sizeOPCITEMDEF);
            int runDef = (int)ptrDef;
            OPCITEMDEFintern idf = new OPCITEMDEFintern();
            idf.wReserved = 0;
            foreach (OPCItemDef d in arrDef)
            {
                idf.szAccessPath = d.AccessPath;
                idf.szItemID = d.ItemID;
                idf.bActive = d.Active;
                idf.hClient = d.HandleClient;
                idf.vtRequestedDataType = (short)d.RequestedDataType;
                idf.dwBlobSize = 0; idf.pBlob = IntPtr.Zero;
                if (d.Blob != null)
                {
                    idf.dwBlobSize = d.Blob.Length;
                    if (idf.dwBlobSize > 0)
                    {
                        hasblobs = true;
                        idf.pBlob = Marshal.AllocCoTaskMem(idf.dwBlobSize);
                        Marshal.Copy(d.Blob, 0, idf.pBlob, idf.dwBlobSize);
                    }
                }

                Marshal.StructureToPtr(idf, (IntPtr)runDef, false);
                runDef += sizeOPCITEMDEF;
            }

            IntPtr ptrRes;
            IntPtr ptrErr;
            int hresult = ifItems.AddItems(count, ptrDef, out ptrRes, out ptrErr);

            runDef = (int)ptrDef;
            if (hasblobs)
            {
                for (int i = 0; i < count; i++)
                {
                    IntPtr blob = (IntPtr)Marshal.ReadInt32((IntPtr)(runDef + 20));
                    if (blob != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(blob);
                    Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
                    runDef += sizeOPCITEMDEF;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Marshal.DestroyStructure((IntPtr)runDef, typeOPCITEMDEF);
                    runDef += sizeOPCITEMDEF;
                }
            }
            Marshal.FreeCoTaskMem(ptrDef);

            if (HRESULTS.Failed(hresult))
                Marshal.ThrowExceptionForHR(hresult);

            int runRes = (int)ptrRes;
            int runErr = (int)ptrErr;
            if ((runRes == 0) || (runErr == 0))
                Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

            arrRes = new OPCItemResult[count];
            for (int i = 0; i < count; i++)
            {
                arrRes[i] = new OPCItemResult();
                arrRes[i].Error = Marshal.ReadInt32((IntPtr)runErr);
                if (HRESULTS.Failed(arrRes[i].Error))
                    continue;

                arrRes[i].HandleServer = Marshal.ReadInt32((IntPtr)runRes);
                arrRes[i].CanonicalDataType = (VarEnum)(int)Marshal.ReadInt16((IntPtr)(runRes + 4));
                arrRes[i].AccessRights = (OPCACCESSRIGHTS)Marshal.ReadInt32((IntPtr)(runRes + 8));

                int ptrblob = Marshal.ReadInt32((IntPtr)(runRes + 16));
                if ((ptrblob != 0))
                {
                    int blobsize = Marshal.ReadInt32((IntPtr)(runRes + 12));
                    if (blobsize > 0)
                    {
                        arrRes[i].Blob = new byte[blobsize];
                        Marshal.Copy((IntPtr)ptrblob, arrRes[i].Blob, 0, blobsize);
                    }
                    Marshal.FreeCoTaskMem((IntPtr)ptrblob);
                }

                runRes += sizeOPCITEMRESULT;
                runErr += 4;
            }

            Marshal.FreeCoTaskMem(ptrRes);
            Marshal.FreeCoTaskMem(ptrErr);
            return hresult == HRESULTS.S_OK;
        }

        public bool RemoveItems(int[] arrHSrv, out int[] arrErr)
        {
            arrErr = null;
            int count = arrHSrv.Length;
            IntPtr ptrErr;
            int hresult = ifItems.RemoveItems(count, arrHSrv, out ptrErr);
            if (HRESULTS.Failed(hresult))
                Marshal.ThrowExceptionForHR(hresult);

            arrErr = new int[count];
            Marshal.Copy(ptrErr, arrErr, 0, count);
            Marshal.FreeCoTaskMem(ptrErr);
            return hresult == HRESULTS.S_OK;
        }
        // ------------------------ IOPCSyncIO ---------------
        [DllImport("oleaut32.dll")]
        public static extern int VariantClear(IntPtr addrofvariant);

        public bool Read(OPCDATASOURCE src, int[] arrHSrv, out OPCItemState[] arrStat)
        {
            arrStat = null;
            int count = arrHSrv.Length;
            IntPtr ptrStat;
            IntPtr ptrErr;
            // FIXME: check if ifSync valid?
            int hresult = ifSync.Read(src, count, arrHSrv, out ptrStat, out ptrErr);
            if (HRESULTS.Failed(hresult))
                Marshal.ThrowExceptionForHR(hresult);

            int runErr = (int)ptrErr;
            int runStat = (int)ptrStat;
            if ((runErr == 0) || (runStat == 0))
                Marshal.ThrowExceptionForHR(HRESULTS.E_ABORT);

            arrStat = new OPCItemState[count];
            for (int i = 0; i < count; i++)
            {														// WORKAROUND !!!
                arrStat[i] = new OPCItemState();

                arrStat[i].Error = Marshal.ReadInt32((IntPtr)runErr);
                runErr += 4;

                arrStat[i].HandleClient = Marshal.ReadInt32((IntPtr)runStat);

                if (HRESULTS.Succeeded(arrStat[i].Error))
                {
                    short vt = Marshal.ReadInt16((IntPtr)(runStat + 16));
                    if (vt == (short)VarEnum.VT_ERROR)
                        arrStat[i].Error = Marshal.ReadInt32((IntPtr)(runStat + 24));

                    arrStat[i].TimeStamp = Marshal.ReadInt64((IntPtr)(runStat + 4));
                    arrStat[i].Quality = Marshal.ReadInt16((IntPtr)(runStat + 12));
                    arrStat[i].DataValue = Marshal.GetObjectForNativeVariant((IntPtr)(runStat + 16));
                    VariantClear((IntPtr)(runStat + 16));
                }
                else
                    arrStat[i].DataValue = null;

                runStat += 32;
            }
            Marshal.DestroyStructure(ptrStat, typeof(tagOPCITEMSTATE));

            Marshal.FreeCoTaskMem(ptrStat);
            Marshal.FreeCoTaskMem(ptrErr);
            return hresult == HRESULTS.S_OK;
        }

    }
}
