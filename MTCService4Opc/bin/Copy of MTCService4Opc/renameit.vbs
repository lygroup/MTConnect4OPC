Dim WshShell, oExec, sCmd
Dim objFSO, objFile, strFolder
Dim colFiles,  f
Dim fld

dim objWMIService 

dim servicename :  servicename  = "MTCService4Opc"
dim servicepath :  servicepath  = "C:\MTCService4Opc\"


Set WshShell = CreateObject("WScript.Shell")


strPath = Wscript.ScriptFullName
Set objFSO = CreateObject("Scripting.FileSystemObject")

Set objFile = objFSO.GetFile(strPath)
servicepath  = objFSO.GetParentFolderName(objFile)  & "\"

Set fld = objFSO.getfolder(servicepath)
For Each f In fld.files
	If  InStrRev(f.Name, ".exe") Then
		objFSO.MoveFile f.Path, objFSO.GetParentFolderName(f)  & "\" & f.Name & ".exg"
	End If
Next