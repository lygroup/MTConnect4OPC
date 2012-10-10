
strComputer = InputBox("Enter Machine Name") 



Set fso = CreateObject("Scripting.FileSystemObject")
src = "..\*"
dest = "\\" & strComputer & "\C$\MTC4OPCService\"
If Not fso.FolderExists(dest) Then
  fso.CreateFolder(dest)
End if
fso.CopyFile  src, dest, true
fso.CopyFolder  src, dest, true

