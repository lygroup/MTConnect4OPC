
strComputer = InputBox("Enter Machine Name") 



Set fso = CreateObject("Scripting.FileSystemObject")
dest = "\\" & strComputer & " C:\MTC4OPCService\install.bat"
Set objShell =  CreateObject("WScript.Shell")
cmd = "cmd /c psexec.exe " & dest 
objShell.Run cmd, 1, True

WScript.sleep  1000