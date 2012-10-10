README.TXT

To install the MTCService4OPC on a remote machine:
=================================================

1) Setup MTC Agent configuration found in the file "MTCService4Opc.exe.config" 

You will need to configure this item:

    <add key="OPCMachine" value="192.168.16.100" />
 a) OPCMachine gives the name or ip address of the Siemens 840D OPC Server 

These items may need to be configured if you are having problems starting the service or connecting to the OPC Server.
   <add key="User" value="" />
    <add key="Password" value="" />

    <add key="ServiceUser" value="" />
    <add key="ServicePassword" value="" />

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

Note: this can be a problem.

 

2) Move to C:\MTConnect4OPC folder (or wherever you extracted the zip contents - shouldn't matter.)

	DOUBLECLICK: configureservice.vbs 
 
This automatically installs the "MTConnect4OPCService" into the Microsoft Service Control Manager (SCM) as automatic service with error recovery. You can confirm this in the Windows SCM.
 


3) Start MTConnect4OPCService 

	DOUBLECLICK: startservice.bat
 
Optionally, you can go into the Windows Service control manager and manually start the process.
MyComputer->Right Click (Select Manage) -> Services and Applications ->  Services -> "MTConnect4OPCService" -> Right Click (Start)



4) See if agent is getting data.

	DOUBLECLICK: MTConnectPage.vbs 
 
will pop open an internet explorer web page with MTConnect data and update every 3 seconds.. 

or manually enter into Internet Explorer ]

	http://127.0.0.1/current/  

and you should see a web page of XML. 
