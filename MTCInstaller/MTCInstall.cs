using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace MTCInstaller
{
    [RunInstaller(true)]
    public partial class MTCInstall : System.Configuration.Install.Installer
    {
        public MTCInstall()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);
           // string Target = this.Context.Parameters["Target"];
           // string IpAddresses = this.Context.Parameters["IpValue"];
        }
        // Override the 'Commit' method. 
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
        }
        // Override the 'Rollback' method. 
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
        public static void Main()
        {
            Console.WriteLine("Usage : installutil.exe Installer.exe ");
        }

    }
}
