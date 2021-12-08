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
