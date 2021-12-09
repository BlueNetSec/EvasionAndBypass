## About Kiosk
kiosks are computer systems that are generally intended to be used by the public for tasks such as Internet browsing, registration, or information retrieval. Since the interfaces are designed with limited functionality, it is important to first enumerate
what is available. We have to relie on [living off the land](https://github.com/LOLBAS-Project/LOLBAS) ttps.

## Breaking out of Linux firefox example

There are variey of protocols that we can access with Uniform Resources Idenfitifes. For example
- chrome://
- ftp://
- malito://
- smb://
- irc://

In our example, we will use irc://, Internet Relay Char server, which allow us browsing to file system 

![image](https://user-images.githubusercontent.com/24555370/145454549-d1099b6f-fa85-4a62-9ee2-f2e27457dad0.png)

when we open /usr/bin/dunstify, the web browser dispaly a pop-up message,

![image](https://user-images.githubusercontent.com/24555370/145454909-5304d73e-63bd-49b6-b7f5-664fe986ea58.png)

This means there does not appear to be a protection mechanism in place that blocks
external applications. We have discovered that the URI (“irc://myhost”) was passed as an
argument to dunstify on the command line.
```
dunstify irc://myhost
```

#### leverage Firefox profiles

![image](https://user-images.githubusercontent.com/24555370/145455760-0cfadcfd-8e33-4d7c-914d-61777c404e7b.png)

From here, we can create our own profile. With the new profile, it might allow command execution.

#### Use /usr/bin/gtkdialog to create a shell

/usr/bin/gtkdialog has builds in builds interfaces with an HTML-style markup language. 
We can use the Firefox Web Developer tools -> Scratchpad, to open an editor and create our [interactive web shell code](/09KioskBreakouts/webshell.html). And we can lanuch it with gtkdialog binary in firefox.

![image](https://user-images.githubusercontent.com/24555370/145457801-0c83251d-5149-47b7-818d-c9a6c2f8f31d.png)

## Windows Kiosk 

Windows supports many different environment variables that can act as shortcuts to different locations on the system. As a result, kiosk developers sometimes forget about or disregard them when creating input restrictions.

A few other useful environment variables include
```
| Enviroment variable       | Location  
| %ALLUSERSPROFILE%         | C:\Documents and Settings\All Users
| %APPDATA%                 | C:\Documents and Settings\Username\Application Data
| %COMMONPROGRAMFILES%      | C:\Program Files\Common Files
| %COMMONPROGRAMFILES(x86)% | C:\Program Files (x86)\Common Files
| %COMSPEC%                 | C:\Windows\System32\cmd.exe
| %HOMEDRIVE%               | C:\
| %HOMEPATH%                | C:\Documents and Settings\Username
| %PROGRAMFILES%            | C:\Program Files
| %PROGRAMFILES(X86)%       | C:\Program Files (x86) (only in 64-bit version)
| %SystemDrive%             | C:\
| %SystemRoot%              | C:\Windows
| %TEMP% and %TMP%          | C:\Documents and Settings\Username\Local Settings\Temp
| %USERPROFILE%             | C:\Documents and Settings\Username
| %WINDIR%                  | C:\Windows
```
- If restricted from accessing **C:\Windows\System32**, We could try **\\127.0.0.1\C$\Windows\System32\**
