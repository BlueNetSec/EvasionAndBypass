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
