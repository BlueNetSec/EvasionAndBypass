##pipe all assemblies into Where-Object and filter on Global Assembly Cache( only native and registered assemblies on Windows)
##Second filter to Split the full path and identify the last element to find System.dll

$systemdll = ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].Equals('System.dll')})

##Use GetType to obatin a reference to the System.dll assembly at runtime, (this is an example of the reflection technique),get the UnsafeNativeMethods
$unsafeObj = $systemdll.GetType('Microsoft.Win32.UnsafeNativeMethods')

##Use the same TTP with GetMethod function ti obtain a reference to the internal GetModuleHandle method:
$GetModuleHandle = $unsafeObj.GetMethod('GetModuleHandle')

##Now we can use the internal GetModuleHandle Function
# $GetModuleHandle.Invoke($null, @("user32.dll")) -> this will return the base address of an unmanaged user32.dll.
# you can verify this by  Process Explorer, we’ll select the PowerShell ISE process. Navigate to View > Lower Pane View  DLLs, in the new sub window locate user32.dll

##Now the same TTP with GetProcAddress result in "Ambiguous match found." error, because there are more than 1 GetProcAddress with in system32.dll
# $GetProcAddress =$unsafeObj.GetMethod('GetProcAddress')

##We can use GetMethods, and filter to only print those called GetProcAddress
#$unsafeObj.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$_}}

##Let's create an array and save all output in the array, and we can just reference to the first one to use the function
$tmp=@()
$unsafeObj.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
$GetProcAddress = $tmp[0]

##Now We can use GetProcAddress to find MessageBoxA inside of user32.dll, this will return base address of MessageBoxA method
# $user32 = $GetModuleHandle.Invoke($null, @("user32.dll"))
# $GetProcAddress.Invoke($null, @($user32, "MessageBoxA"))