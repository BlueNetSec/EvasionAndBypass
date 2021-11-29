##Identify which dll module contians GetProcAddress and GetModuleHandle functions

$Assemblies = [AppDomain]::CurrentDomain.GetAssemblies()

## First For Loop print the locations, and then Pipe Type information to match "Microsoft.Win32.UnsafeNativeMethods instead of listing all methods with the static keyword"
$Assemblies |
	ForEach-Object {
		$_.Location
		$_.GetTypes()|
			ForEach-Object {
				$_ | Get-Member -Static| Where-Object {$_.TypeName.Equals('Microsoft.Win32.UnsafeNativeMethods')}
			} 2> $null
		}