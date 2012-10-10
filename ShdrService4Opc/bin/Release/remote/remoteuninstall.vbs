
strComputer = InputBox("Enter Machine Name") 



Set objShell =  CreateObject("WScript.Shell")
cmd = "cmd /c psservice.exe \\" & strComputer  & " stop MTCService4Opc"
objShell.Run cmd, 1, True

dest = "\\" & strComputer & "  C:\MTC4OPCService\MTCService4Opc.exe /uninstall"
Set objShell =  CreateObject("WScript.Shell")
cmd = "cmd /c psexec.exe " & dest 
objShell.Run cmd, 1, True
