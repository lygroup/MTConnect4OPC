Dim WshShell, oExec, sCmd
Dim objFSO, objFile, strFolder
Dim colFiles, file

dim objWMIService 

dim servicename :  servicename  = "MTCService4Opc"
dim servicepath :  servicepath  = "C:\MTCService4Opc\"


Set WshShell = CreateObject("WScript.Shell")


strPath = Wscript.ScriptFullName
Set objFSO = CreateObject("Scripting.FileSystemObject")

Set objFile = objFSO.GetFile(strPath)
servicepath  = objFSO.GetParentFolderName(objFile)  & "\"


Set objWMIService = GetObject("winmgmts:" _
    & "{impersonationLevel=impersonate}!\\" & "." & "\root\cimv2")


Set colFiles = objWMIService.ExecQuery("Select * from CIM_DataFile where Path = '"&servicepath&"'")

For Each file in colFiles
    Wscript.Echo file.Name 
Next
