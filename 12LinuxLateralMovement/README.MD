## Lateral Movement with SSH

Some systems permit password authentication to connect to a Linux machine via SSH, many require public key authentication. The public key is stored in the ~/.ssh/authorized_keys 
file of the server the user is connecting to. The private key is typically stored in the ~/.ssh/ directory on the system the user is connecting from.

Private SSH keys are a prime target for an attacker. We can search for it
 
```
find /home/ -name "id_rsa"
```
Once we located the private key file, we need to check if the key is protected with a passphrase by reading the key file
The Proc-Type: 4,ENCRYPTED tell us the key is encrypted with password passphrase.
```
root@linuxvictim:/home/linuxvictim# cat svuser.key
-----BEGIN RSA PRIVATE KEY-----
Proc-Type: 4,ENCRYPTED
DEK-Info: AES-128-CBC,351CBB3ECC54B554DD07029E2C377380
```

we can recon user’s ~/.ssh/known_hosts file to find machines that have been connected to recently or check the bash history

```
root@linuxvictim:/home/linuxvictim/.ssh# cat known_hosts
root@linuxvictim:/home/linuxvictim# tail .bash_history
```
Lastly we need to Convert private key to john readable format form ssh2john.py and crack the passphase for the private key using John


```
kali@kali:~$ python /usr/share/john/ssh2john.py svuser.key > svuser.hash
kali@kali:~$ sudo john --wordlist=/usr/share/wordlists/rockyou.txt ./svuser.hash
```
