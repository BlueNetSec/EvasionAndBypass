function LookupFunc{
	## moduleName could be user32.dll, and the functionName would be MessageBoxA from pervious example
	Param ($moduleName, $functionName)
	$assem = ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].Equals('System.dll')}).GetType('Microsoft.Win32.UnsafeNativeMethods')
	$tmp=@()
	$assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
	return $tmp[0].Invoke($null, @(($assem.GetMethod('GetModuleHandle')).Invoke($null, @($moduleName)), $functionName))
}

##C# Delegate, let's do the same thing in powershell 
#int delegate MessageBoxSig(IntPtr hWnd, String text, String caption, int options);

##Step 1, Create a new assembly object using AssemblyName class and supply a custom name and NOT include symbol information
$MyAssembly = New-Object System.Reflection.AssemblyName('ReflectedDelegate')

##Setp 2, configure its access mode, use DefineDynamicAssembly to set it to be executable and not saved to disk
## System.Reflection.Emit.AssemblyBuilderAccess with run access, as second argument
$Domain = [AppDomain]::CurrentDomain
$MyAssemblyBuilder = $Domain.DefineDynamicAssembly($MyAssembly, [System.Reflection.Emit.AssemblyBuilderAccess]::Run)

##Step 3, Create the content, build the Module using DefineDynamicModule method. custom name with not symbol information
$MyModuleBuilder = $MyAssemblyBuilder.DefineDynamicModule('InMemoryModule', $false)

##Step4, Create custom type with DefineType method(take 3 arguments)
##argument 1 is the custom name, argument 2 is combined list of attributes for the type.
## Class(so we can later instantiate it), public, sealed->non-extendable, ansiclass->use ASCII, AutoClass->interpreted automatically
## third argument, allow us to call the target API with multiple arguments
$MyTypeBuilder = $MyModuleBuilder.DefineType('MyDelegateType', 'Class, Public, Sealed, AnsiClass, AutoClass', [System.MulticastDelegate])

##Step5, create a constructor for the custom delegate type,
##'RTSpecialName, HideBySig, Public' -> attributes of the constructor, it must by public and referenced by both name and signature
##[System.Reflection.CallingConventions]::Standard -> calling convention for the constructor,
##@([IntPtr], [String], [String], [int])-> define the parametertypes of the constructor that will become the function prototype, MessageBoxA method take 4 arguments [IntPtr], [String], [String], [int]
$MyConstructorBuilder = $MyTypeBuilder.DefineConstructor('RTSpecialName, HideBySig, Public', [System.Reflection.CallingConventions]::Standard, @([IntPtr], [String], [String], [int]))

##Step 6,Set implementation flag for the constructor
$MyConstructorBuilder.SetImplementationFlags('Runtime, Managed')

##Step7, define Invoke method for the constructor using DefineMethod, which takes 4 arguments
##Argument 1: Invoke-> name of the method to define
##Argument 2: 'Public, HideBySig, NewSlot, Virtual'-> method attributes taken from MethodAttributes Enum, NewSlot/Virtual to always gets a new slot in vtable
##Argument 3: function return type, for MessageBoxA is [int].
##Argument 4: input arguments for MessageBoxA
$MyMethodBuilder = $MyTypeBuilder.DefineMethod('Invoke', 'Public, HideBySig, NewSlot, Virtual', [int], @([IntPtr], [String], [String], [int]))

##Step 8:set the implementation flags to allow the Invoke method to be called and instantiate the delegate type with CreateType
$MyMethodBuilder.SetImplementationFlags('Runtime, Managed')
$MyDelegateType = $MyTypeBuilder.CreateType()


##Step 9: Use the custom dll to print message
$MessageBoxA = LookupFunc user32.dll MessageBoxA
##copy MessageBoxA to MyDelegateType
$MyFunction = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($MessageBoxA, $MyDelegateType)
##Invoke the function
$MyFunction.Invoke([IntPtr]::Zero,"Hello World","This is My MessageBox",0)