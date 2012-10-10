using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ServiceInstaller
{
    /// <summary>
    /// This is a custom project installer.
    /// Applies a unique name to the service using the /name switch
    /// Sets description to the service using the /desc switch
    /// Sets user name and password using the /user and /password switches
    /// Allows the use of a local account using the /account switch
    /// </summary>
    [RunInstaller(true)]
    public class DynamicInstaller : Installer
    {
        public string ServiceName
        {
            get { return serviceInstaller.ServiceName; }
            set { serviceInstaller.ServiceName = value; }
        }
        public string DisplayName
        {
            get { return serviceInstaller.DisplayName; }
            set { serviceInstaller.DisplayName = value; }
        }
        public string Description
        {
            get { return serviceInstaller.Description; }
            set { serviceInstaller.Description = value; }
        }
        public ServiceStartMode StartType
        {
            get { return serviceInstaller.StartType; }
            set { serviceInstaller.StartType = value; }
        }
        public ServiceAccount Account
        {
            get { return processInstaller.Account; }
            set { processInstaller.Account = value; }
        }
        public string ServiceUsername
        {
            get { return processInstaller.Username; }
            set { processInstaller.Username = value; }
        }
        public string ServicePassword
        {
            get { return processInstaller.Password; }
            set { processInstaller.Password = value; }
        }

        private ServiceProcessInstaller processInstaller;
        private System.ServiceProcess.ServiceInstaller serviceInstaller;

        public DynamicInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalService;
            processInstaller.Username = null;
            processInstaller.Password = null;
            serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "MyService";
            serviceInstaller.DisplayName = "";
            serviceInstaller.Description = "";

            Installers.AddRange(new Installer[] {
            processInstaller,
            serviceInstaller});
        }

        #region Access parameters

        /// <summary>
        /// Return the value of the parameter in dicated by key
        /// </summary>
        /// <PARAM name="key">Context parameter key</PARAM>
        /// <returns>Context parameter specified by key</returns>
        public string GetContextParameter(string key) 
        {
            string sValue = "";
            try 
            {
                sValue = this.Context.Parameters[key].ToString();
            }
            catch 
            {
                sValue = "";
            }

            return sValue;
        }

        #endregion

        /// <summary>
        /// This method is run before the install process.
        /// This method is overriden to set the following parameters:
        /// service name (/name switch)
        /// service description (/desc switch)
        /// account type (/account switch)
        /// for a user account user name (/user switch)
        /// for a user account password (/password switch)
        /// Note that when using a user account,
        /// if the user name or password is not set,
        /// the installing user is prompted for the credentials to use.
        /// </summary>
        /// <PARAM name="savedState"></PARAM>
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);

            bool isUserAccount = false;
            
            // Decode the command line switches
            string name = GetContextParameter("name").Trim();
            if (name != "")
            {
                serviceInstaller.ServiceName = name;
            }
            string desc = GetContextParameter("desc").Trim();
            if (desc != "")
            {
                serviceInstaller.Description = desc;
            }

            // What type of credentials to use to run the service
            string acct = GetContextParameter("account");
            switch (acct.ToLower())
            {
                case "user":
                    processInstaller.Account = ServiceAccount.User;
                    isUserAccount = true;
                    break;
                case "localservice":
                    processInstaller.Account = ServiceAccount.LocalService;
                    break;
                case "localsystem":
                    processInstaller.Account = ServiceAccount.LocalSystem;
                    break;
                case "networkservice":
                    processInstaller.Account = ServiceAccount.NetworkService;
                    break;
            }

            // User name and password
            string username = GetContextParameter("user").Trim();
            string password = GetContextParameter("password").Trim();

            // Should I use a user account?
            if (isUserAccount)
            {
                // If we need to use a user account,
                // set the user name and password
                if (username != "")
                {
                    processInstaller.Username = username;
                }
                if (password != "")
                {
                    processInstaller.Password = password;
                }
            }
        }

        /// <summary>
        /// Uninstall based on the service name
        /// </summary>
        /// <PARAM name="savedState"></PARAM>
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);

            // Set the service name based on the command line
            string name = GetContextParameter("name").Trim();
            if (name != "")
            {
                serviceInstaller.ServiceName = name;
            }
        }

    }//end class
}