Dim WshShell, oExec, sCmd
Dim objFSO, objFile, strFolder


dim servicename :  servicename  = "MTCService4Opc"
dim servicepath :  servicepath  = "C:\MTCService4Opc\"


Set WshShell = CreateObject("WScript.Shell")


strPath = Wscript.ScriptFullName
Set objFSO = CreateObject("Scripting.FileSystemObject")

Set objFile = objFSO.GetFile(strPath)
servicepath  = objFSO.GetParentFolderName(objFile)  & "\"



servicename = Inputbox( "Service Name:", "Setup", "MTCService4Opc")
if servicename = ""  Then
	Wscript.Quit(0)
End if



function run(byval cmd)
	'msgbox " run" & cmd
	oExec = WshShell.Run( cmd,1,true )
end function


sCmd = "sc.exe create " & servicename  &  " binPath= " & Chr(34) & servicepath  & servicename & ".exe" & Chr(34) &  " type= own start= auto"
run sCmd

sCmd = "sc.exe config " & servicename    & " start= auto"
run sCmd

sCmd = "sc.exe failure MTConnectAgentSHDRService reset= 3600 reboot= " & Chr(34) & "Restarting " & servicename  & Chr(34) & " actions= restart/5000/restart/5000/restart/5000"
run sCmd


' sCmd = "sc.exe config MTConnectAgentSHDRService obj= " & Chr(34) & ".\auduser" & Chr(34) & "password= & Chr(34) & "SUNRISE" & Chr(34) 

'sCmd = "sc.exe start " & servicename   

