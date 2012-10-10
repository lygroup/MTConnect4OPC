using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.Configuration;

using Utilities;
using OpcLibrary;


namespace MTCService4Opc
{
    public class ProjectInstaller : System.Configuration.Install.Installer
    {
        public string gServiceName = "MTCService4Opc";
        public static int dwResetPeriod = 60 * 60; // 1 hour
        public static string sServiceUser = "";
        public static string sServicePassword = "";
        public static string sServiceType = "Auto";
        public static string sServiceName = "MTCService4OPC";
      
        public ProjectInstaller()
        {
            ServiceProcessInstaller process = new ServiceProcessInstaller();
            this.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ProjectInstaller_AfterInstall);

            process.Account = ServiceAccount.LocalSystem;

            System.ServiceProcess.ServiceInstaller serviceAdmin = new System.ServiceProcess.ServiceInstaller();

            serviceAdmin.StartType = ServiceStartMode.Manual;
            serviceAdmin.ServiceName = gServiceName;
            serviceAdmin.DisplayName = sServiceName;

            // now just add the installers that we created to our
            // parents container, the documentation
            // states that there is not any order that you need to
            // worry about here but I'll still
            // go ahead and add them in the order that makes sense.
            Installers.Add(process);
            Installers.Add(serviceAdmin);
        }
        private void ProjectInstaller_AfterInstall(object sender,
               System.Configuration.Install.InstallEventArgs e)
        {
            //Our code goes in this event because it is the only one that will do
            //a proper job of letting the user know that an error has occurred,
            //if one indeed occurs. Installation will be rolled back 
            //if an error occurs.

            int iSCManagerHandle = 0;
            int iSCManagerLockHandle = 0;
            int iServiceHandle = 0;
            bool bChangeServiceConfig = false;
            bool bChangeServiceConfig2 = false;
            modAPI.SERVICE_DESCRIPTION ServiceDescription;
            modAPI.SERVICE_FAILURE_ACTIONS ServiceFailureActions;
            modAPI.SC_ACTION[] ScActions = new modAPI.SC_ACTION[3];
            //There should be one element for each action. 
            //The Services snap-in shows 3 possible actions.

            bool bCloseService = false;
            bool bUnlockSCManager = false;
            bool bCloseSCManager = false;

            IntPtr iScActionsPointer = new IntPtr();
            try
            {
                //Obtain a handle to the Service Control Manager, 
                //with appropriate rights.
                //This handle is used to open the relevant service.
                iSCManagerHandle = modAPI.OpenSCManagerA(null, null,
                modAPI.ServiceControlManagerType.SC_MANAGER_ALL_ACCESS);

                //Check that it's open. If not throw an exception.
                if (iSCManagerHandle < 1)
                {
                    throw new Exception("Unable to open the Services Manager.");
                }

                //Lock the Service Control Manager database.
                iSCManagerLockHandle = modAPI.LockServiceDatabase(iSCManagerHandle);

                //Check that it's locked. If not throw an exception.
                if (iSCManagerLockHandle < 1)
                {
                    throw new Exception("Unable to lock the Services Manager.");
                }

                //Obtain a handle to the relevant service, with appropriate rights.
                //This handle is sent along to change the settings. The second parameter
                //should contain the name you assign to the service.
                iServiceHandle = modAPI.OpenServiceA(iSCManagerHandle, gServiceName,
                modAPI.ACCESS_TYPE.SERVICE_ALL_ACCESS);

                //Check that it's open. If not throw an exception.
                if (iServiceHandle < 1)
                {
                    throw new Exception("Unable to open the Service for modification.");
                }
                //Call ChangeServiceConfig to update the ServiceType 
                //to SERVICE_INTERACTIVE_PROCESS.
                //Very important is that you do not leave out or change the other relevant
                //ServiceType settings. The call will return False if you do.
                //Also, only services that use the LocalSystem account can be set to
                //SERVICE_INTERACTIVE_PROCESS.
                modAPI.ServiceType servicetype = modAPI.ServiceType.SERVICE_WIN32_OWN_PROCESS;
                modAPI.ServiceStartType starttype = modAPI.ServiceStartType.SERVICESTARTTYPE_NO_CHANGE;

                if (sServiceType.IndexOf("auto",StringComparison.CurrentCultureIgnoreCase)>=0)
                    starttype =  modAPI.ServiceStartType.SERVICE_AUTO_START;
                
                if (sServiceUser== null || sServiceUser.Length < 1)
                {
                    servicetype = servicetype | modAPI.ServiceType.SERVICE_INTERACTIVE_PROCESS;
                    sServiceUser = null;
                    sServicePassword = null;
                }

                 bChangeServiceConfig = modAPI.ChangeServiceConfigA(iServiceHandle,
                     servicetype,
                     (int) starttype, modAPI.SERVICE_NO_CHANGE,
                      null, null, 0, null, sServiceUser, sServicePassword, null);
                //null, null, 0, null, null, null, null);

                //If the call is unsuccessful, throw an exception.
                if (bChangeServiceConfig == false)
                {
                    throw new Exception("Unable to change the Service settings.");
                }

#if INTERACTIVESERVICE
                //To change the description, create an instance of the SERVICE_DESCRIPTION
                //structure and set the lpDescription member to your desired description.
                ServiceDescription.lpDescription =
                  "This is my custom description for my Windows Service Application!";

                //Call ChangeServiceConfig2 with SERVICE_CONFIG_DESCRIPTION in the second
                //parameter and the SERVICE_DESCRIPTION instance in the third parameter
                //to update the description.
                bChangeServiceConfig2 = modAPI.ChangeServiceConfig2A(iServiceHandle,
                modAPI.InfoLevel.SERVICE_CONFIG_DESCRIPTION, ref ServiceDescription);

                //If the update of the description is unsuccessful it is up to you to
                //throw an exception or not. The fact that the description did not update
                //should not impact the functionality of your service.
                if (bChangeServiceConfig2 == false)
                {
                    throw new Exception("Unable to set the Service description.");
                }
#endif
                // The service control manager counts the number of times each service has failed since the system booted. 
                // The count is reset to 0 if the service has not failed for dwResetPeriod seconds. 
                // When the service fails for the Nth time, the service controller performs the action specified 
                // in element [N-1] of the lpsaActions array. 
                // If N is greater than cActions, the service controller repeats the last action in the array.

                //To change the Service Failure Actions, create an instance of the
                //SERVICE_FAILURE_ACTIONS structure and set the members to your
                //desired values. See MSDN for detailed descriptions.
                // modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;
                // modAPI.SC_ACTION_TYPE.SC_ACTION_RUN_COMMAND;
                // modAPI.SC_ACTION_TYPE.SC_ACTION_REBOOT;

                ServiceFailureActions.dwResetPeriod = dwResetPeriod; // every 12 hours, will reboot every 24
                ServiceFailureActions.lpRebootMsg =
                         "Service failed to start! Rebooting...";
                ServiceFailureActions.lpCommand = "";
                ServiceFailureActions.cActions = ScActions.Length;

                //The lpsaActions member of SERVICE_FAILURE_ACTIONS is a pointer to an
                //array of SC_ACTION structures. This complicates matters a little,
                //and although it took me a week to figure it out, the solution
                //is quite simple. SC_ACTION_NONE

                //First order of business is to populate our array of SC_ACTION structures
                //with appropriate values.
                ScActions[0].Delay = 20000;
                ScActions[0].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;
                ScActions[1].Delay = 20000;
                ScActions[1].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;
                ScActions[2].Delay = 20000;
                ScActions[2].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;

                //Once that's done, we need to obtain a pointer to a memory location
                //that we can assign to lpsaActions in SERVICE_FAILURE_ACTIONS.
                //We use 'Marshal.SizeOf(New modAPI.SC_ACTION) * 3' because we pass 
                //3 actions to our service. If you have less 
                //actions change the * 3 accordingly.
                iScActionsPointer =
                  Marshal.AllocHGlobal(Marshal.SizeOf(new modAPI.SC_ACTION()) * 3);

                //Once we have obtained the pointer for the memory location we need to
                //fill the memory with our structure. We use the CopyMemory API function
                //for this. Please have a look at it's declaration in modAPI.
                modAPI.CopyMemory(iScActionsPointer,
                  ScActions, Marshal.SizeOf(new modAPI.SC_ACTION()) * 3);

                //We set the lpsaActions member 
                //of SERVICE_FAILURE_ACTIONS to the integer
                //value of our pointer.
                ServiceFailureActions.lpsaActions = iScActionsPointer.ToInt32();

                //We call bChangeServiceConfig2 with the relevant parameters.
                bChangeServiceConfig2 = modAPI.ChangeServiceConfig2A(iServiceHandle,
                      modAPI.InfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS,
                      ref ServiceFailureActions);

                //If the update of the failure actions 
                //are unsuccessful it is up to you to
                //throw an exception or not. The fact that 
                //the failure actions did not update
                //should not impact the functionality of your service.
                if (bChangeServiceConfig2 == false)
                {
                    throw new Exception("Unable to set the Service Failure Actions.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage(ex.Message, Logger.FATAL);
                //Throw the exception again so the installer can get to it
                throw new Exception(ex.Message);
            }
            finally
            {
                //Close the handles if they are open.
                Marshal.FreeHGlobal(iScActionsPointer);

                if (iServiceHandle > 0)
                {
                    bCloseService = modAPI.CloseServiceHandle(iServiceHandle);
                }

                if (iSCManagerLockHandle > 0)
                {
                    bUnlockSCManager = modAPI.UnlockServiceDatabase(iSCManagerLockHandle);
                }

                if (iSCManagerHandle != 0)
                {
                    bCloseSCManager = modAPI.CloseServiceHandle(iSCManagerHandle);
                }
            }
  
        }
    }

    public partial class Service1 : ServiceBase
    {
        private MyProgram myProgram;
        public Service1()
        {
            InitializeComponent();
        }

        //InitDCOM dcom = new InitDCOM();
        // The main entry point for the process
        [MTAThread]
        static void Main(string[] args)
        {
            // Interop.CoInitializeSecurity(IntPtr.Zero, -1, null, IntPtr.Zero, Interop.RPC_C_AUTHN_LEVEL_NONE, Interop.RPC_C_IMP_LEVEL_IDENTIFY, IntPtr.Zero, Interop.EOAC_NONE, IntPtr.Zero);
            long hres;
            try
            {
                hres = Interop.CoInitializeSecurity(IntPtr.Zero, -1, IntPtr.Zero, IntPtr.Zero, (uint)RpcAuthnLevel.None, (uint)RpcImpLevel.Identify, IntPtr.Zero, (uint)EoAuthnCap.None, IntPtr.Zero);
            }
            catch (COMException e)
            {
                Logger.LogMessage("CoInitializeSecurity Execption: " + e.Message, Logger.FATAL);
            }
            bool restartLogAppend = false;
            string opt = null;

           
            int dwResetPeriod = 60 * 60;
//            Debugger.Break();

            try
            {
                ProjectInstaller.sServiceName = ConfigurationManager.AppSettings["ServiceName"];
                ProjectInstaller.dwResetPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["ResetPeriod"]);
                ProjectInstaller.sServiceUser = ConfigurationManager.AppSettings["ServiceUser"];
                ProjectInstaller.sServicePassword = ConfigurationManager.AppSettings["ServicePassword"];
                restartLogAppend = Convert.ToBoolean(ConfigurationManager.AppSettings["RestartLogAppend"]);
            }
            catch (Exception)
            {
                dwResetPeriod = 60 * 60;
            }

            Logger.RestartLog(restartLogAppend);

            string version = Misc.VersionNumber();
            Logger.LogMessage("MTConnect 4 OPC Agent OPC Version 2.0 - Release " + version, Logger.FATAL);

            if (args.Length > 0)
            {

                // check for arguments
                opt = args[0];

                ProjectInstaller.dwResetPeriod = dwResetPeriod;
                try
                {
                    if (opt != null && opt.ToLower() == "/install")
                    {

                        TransactedInstaller ti = new TransactedInstaller();
                        ProjectInstaller pi = new ProjectInstaller();
                        ti.Installers.Add(pi);
                        String path = String.Format("/assemblypath={0}",
                                                    Assembly.GetExecutingAssembly().Location);
                        String[] cmdline = { path };
                        InstallContext ctx = new InstallContext("", cmdline);
                        ti.Context = ctx;
                        ti.Install(new Hashtable());
                    }
                    else if (opt != null && opt.ToLower() == "/uninstall")
                    {
                        TransactedInstaller ti = new TransactedInstaller();
                        ProjectInstaller mi = new ProjectInstaller();
                        ti.Installers.Add(mi);
                        String path = String.Format("/assemblypath={0}",
                                                    Assembly.GetExecutingAssembly().Location);

                        String[] cmdline = { path };
                        InstallContext ctx = new InstallContext("", cmdline);
                        ti.Context = ctx;
                        ti.Uninstall(null);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogMessage(e.Message, Logger.FATAL);
                }
            }
            if (opt == null) // e.g. ,nothing on the command line
            {
#if ( ! DEBUG )
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] {new Service1()};
                ServiceBase.Run(ServicesToRun);
#else
                // debug code: allows the process to run as a non-service
                // will kick off the service start point, but never kill it
                // shut down the debugger to exit
                Service1 service = new Service1();
                service.OnStart(null);
                Thread.Sleep(Timeout.Infinite);
#endif
            }
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Break();
            myProgram = new MyProgram();
            myProgram.Start();
        }
        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            myProgram.Stop();
        }
    }

   
}

