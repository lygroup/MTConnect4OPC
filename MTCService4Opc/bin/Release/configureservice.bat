
sc.exe create MTCService4OPC binpath= C:\MTCService4Opc\MTCService4Opc.exe type= own start= auto 
sc.exe config MTCService4OPC start= auto

sc.exe failure MTCService4OPC reset= 3600 reboot= "Restarting MTCService4OPC2" actions= restart/5000/restart/5000/restart/5000


REM sc.exe config MTCService4OPC obj= ".\auduser" password= "SUNRISE"

REM sc.exe start MTCService4OPC 
pause
