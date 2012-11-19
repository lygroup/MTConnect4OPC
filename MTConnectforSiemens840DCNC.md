
# 

# SIEMENS 840D MTConnect Installation

This document briefly describes deploying MTCAdapter4OPC, an MTConnect Agent with OPC .Net adapter client to do data access to a remote OPC Server.  The MTCAdapter4OPC Windows service described in this document is targeted for communication between an OPC client running on the FEPC with the MTConnect Agent and the Siemens 840D OPC Server running on a CNC.  Figure 1 shows the OPC Server running on the CNC and the MTConnect and OPC Client running on Front-End PC.  




       **
       ****Figure 1 Siemens 840D Deployment System Architecture**





### **

### ****Firewall Requirements**



1.  Local Users Authenticate as Themselves.

>Simple File Sharing forces every remote user to Authenticate as the Guest User Account, which causes problems. By default, the Simple File Sharing user interface is turned on in Windows XP Professional-based computers that are joined to a workgroup. Windows XP Professional-based computers that are joined to a domain use only the classic file sharing and security interface.


2.  DCOM Firewall Exception & DCOM Port Exception

>MTConnect/OPC uses DCOM to communicate. DCOM and port 135  which provides RPC-based services for DCOM, must be added to exceptions  to allow communication traffic to be able to go through all firewalls, including Windows as shown below: 







Above, if DCOM is ON, this means we should be able to directly access an 840D OPC Server. 

Generally, if there is a firewall blocking DCOM socket port access, you will get this error message.

0x800706ba - The RPC server is unavailable
3.  USER HAS CREDENTIALS ON REMOTE SYSTEM




DCOM is used to remotely create the OPC Server component for the OPC Client. To do this, DCOM obtains the Client's current username associated with the current. Windows guarantees that this user credential is authentic. DCOM then passes the username to the machine or process where the component is running. DCOM on the component's machine then validates the username again using whatever authentication mechanism is configured (can be NONE) and checks the access control list for the component. If the OPC Client's username is not included in this list (either directly or indirectly as a member of a group of users), DCOM rejects the call before the component is ever involved. The figure “OPC Server DCOM Component Creation Timeline” below describes the sequence events in creating the remote OPC Server component that involves permissions to remote log on, permissions to launch(create) the OPC Server component, permission to access the OPC Server component. 



If *ANY* of these fails, you get same error message, the dreaded:



0x80070005 - General access denied error











### **

### ****MTConnect Agent Installation Requirements**

The MTCAdapter4OPC program is configurable to change name and locations of OPC Server as well as properties of the MT Connect Agent.

The MTCAdapter4OPC software installation requirements were:



1.  MT-Connect requires installation of .Net Framework 3.5.  
2.  OPC .NET Api requires installation of .Net Framework 2.0.  
3.  Fortunately, the .Net Framework 3.5 is backwards compatible with .Net Framework 2.0.
4.  The OPC Proxy/Stub DLLs need to be installed on the FEPC. 
5.  All CNC Data Access exe or DLL files are located in the same directory as the exe file being used. The following table list the files that need to in this directory:



