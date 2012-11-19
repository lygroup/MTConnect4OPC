using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using ServiceTools;

namespace Installer
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
            //MessageBox.Show("Startinstall");
            //System.Diagnostics.Debugger.Break();

            string Target = this.Context.Parameters["Target"];
            string IpAddresses = this.Context.Parameters["IpValue"];
            string Devices = this.Context.Parameters["Devices"];
            try
            {
                ServiceInstaller.Uninstall("MTCService4Opc");
            }
            catch (Exception) { }
 
            // Checks to make:
            // 1)  ip = # machines (devices)
            // 2) machine names unique
            // 3) delete any old service with same name
            

            // Uses reflection to find the location of the config file.
            string configfile = Target + "MTCService4Opc.exe" + ".config";
            System.IO.FileInfo FileInfo = new System.IO.FileInfo(configfile);
            if (FileInfo.Exists)
            {
                IpAddresses = IpAddresses.Replace(" ", "");
                Devices = Devices.Replace(" ", "");
                List<string> ips = IpAddresses.Split(',').Select(p => p.Trim()).ToList();
                List<string> devices = Devices.Split(',').Select(p => p.Trim()).ToList();
 
                string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
                xml += "<mt:MTConnectDevices  xmlns:mt=\"urn:mtconnect.com:MTConnectDevices:0.9\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\n";
                xml += "xsi:schemaLocation=\"urn:mtconnect.com:MTConnectDevices:0.9 http://www.mtconnect.org/schemas/MTConnectDevices.xsd\">\n";
                xml += "<Header version=\"1.0\" sender=\"NIST MTConnect Instance\" creationTime=\"2008-09-08T10:00:47-07:00\" instanceId=\"101\" bufferSize=\"100000\"/>\n";
                xml += "<Devices>\n";
                string str = Installer.Properties.Resources.DeviceTemplate;
                for(int j=0; j<devices.Count; j++) 
                {
                    string dstr = str.Replace("DDDD", devices[j]);
                    xml+=dstr;
                }

                 xml += " </Devices>\n";
                 xml += "</mt:MTConnectDevices>\n";
                 System.IO.File.WriteAllText(Target + "Devices.xml", xml);
                string contents = System.IO.File.ReadAllText(configfile);
                contents = contents.Replace("xxx.xxx.xxx.xxx", IpAddresses);
                contents = contents.Replace("Mazak1", Devices);
                System.IO.File.WriteAllText(configfile, contents);
            }
            string installfile = Target + "installservice.bat";
            FileInfo = new System.IO.FileInfo(installfile);
            if (FileInfo.Exists)
            {
                string contents = System.IO.File.ReadAllText(installfile);
                contents = contents.Replace("XXXX", "\"" + Target + "MTCService4Opc.exe\"");
                System.IO.File.WriteAllText(installfile, contents);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = installfile;
                startInfo.CreateNoWindow = false;
                Process.Start(startInfo);
            }
         }
        // Override the 'Commit' method. 
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            System.Diagnostics.Debugger.Break();
        }
        // Override the 'Rollback' method. 
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
        // Override the 'Uninstall' method. 
        public override void Uninstall(IDictionary savedState)
        {
             base.Uninstall(savedState);
             ServiceInstaller.Uninstall("MTCService4Opc");

        }
        public static void Main()
        {
            Console.WriteLine("Usage : installutil.exe Installer.exe ");
        }

    }

}
