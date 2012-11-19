using System.Text;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Management;
using System.Collections;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ServiceInstaller
{
    [RunInstaller(true)]
    public partial class ServiceInstall : System.Configuration.Install.Installer
    {
        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);
            string Target = this.Context.Parameters["Target"];
            string IpAddresses = this.Context.Parameters["IpValue"];


        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
            //Add custom code here
        }

        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            //Add custom code here
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            //Add custom code here
        }

        static public void Main()
        {

        }
    }
}