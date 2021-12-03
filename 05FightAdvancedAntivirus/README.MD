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
Frida is a opensource tool allow us to hook Win32APi through a python backend and display the result(return values, input arguments) with JavaScript. If we want to learn more ASMI behavior, we have hook the powershell with Frida to observe the API calls.

Setting up Frida
- 1. install python
- 2. install pip with python3 [get-pip.py](/05FightAdvancedAntivirus/get-pip.py)
