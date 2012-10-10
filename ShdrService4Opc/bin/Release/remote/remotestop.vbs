
strUserIn = InputBox("Enter Machine Name") 


Set objShell =  CreateObject("WScript.Shell")
cmd = "cmd /c psservice.exe \\" & strUserIn   & " stop MTCService4Opc"
objShell.Run cmd, 1, True


