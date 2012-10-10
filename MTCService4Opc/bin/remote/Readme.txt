README.TXT

To install the MTCService4OPC on a remote machine:
=================================================

1) Setup MTC Agent configuration found in the file "MTCService4Opc.exe.config" 

You will need to configure these items

    <add key="OPCMachine" value="192.168.16.100" />
    <add key="User" value=".\auduser" />
    <add key="Password" value="SUNRISE840d" />

    <add key="ServiceUser" value=".\auduser" />
    <add key="ServicePassword" value="SUNRISE840d" />

a) OPCMachine gives the name or ip address of the Siemens 840D OPC Server 
b) User is the Siemens 840D CNC login name on the remote machine: may be blank
c) Password Siemens 840D CNC password for the given User.
d) ServiceUser is the user under which the MT Connect agent will run (blank is the interactive user)
e) ServicePassword is the password for the MT Connect agent service

Note: If you use a domain name service for logon, use its name then your username,
e.g., NIST\auduser
if you are using the local logon service on the machine precede user name with .\
e.g., .\michalos

Note: if your password has non-html friendly characters, e.g., & or < >, use the html equivalent in the 
config file
	& = &amp;
        > = &gt;
        < = &lt;

Beware this can be a problem.

2) Change to the folder REMOTE. Then, to copy all the files to the remote pc

  	=> Double click "remotecopy.vbs" which is a vb script. 

	=> Enter the name of the computer you wish to copy the agent files.

It will create and copy all the files and subfolders to C:/MTC4OPCService on that machine

3) Remote install the MT Connect Agent

  	=> Double click "remoteinstall.vbs" which is a vb script. 

Enter the name of the computer you wish to copy the agent files.


3) Remote start of the MT Connect Agent

  	=> Double click "remotestart.vbs" which is a vb script. 

Enter the name of the computer you wish to copy the agent files.

To stop or uninstall the service run the

  	=> Double click "remotestop.vbs"  
  	=> Double click "remoteuninstall.vbs"  
