Sub Document_Open()
	SubstitutePage
End Sub

Sub AutoOpen()
	SubstitutePage
End Sub

' TheDoc is the Name variable in new Building block
Sub SubstitutePage()
	ActiveDocument.Content.Select
	Selection.Delete
	ActiveDocument.AttachedTemplate.AutoTextEntries("TheDoc").Insert Where:=Selection.Range, RichText:=True
End Sub