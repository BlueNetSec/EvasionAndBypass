#GetAssemblies to search preloaded assemblies in the PowerShell process
$Assemblies = [AppDomain]::CurrentDomain.GetAssemblies()

#$_. pipe variable short for $Assemblies,
#first for loop, to obtain all assemblies' methods and structures

# Unsafe keyword are required when C# code invoke Win32 APIs directly,
# function must be declared as static to avoid instantiation,

#therefore the second for loop to filter static and Unsafe 
$Assemblies |
	ForEach-Object {
		$_.GetTypes()|
			ForEach-Object {
				$_ | Get-Member -Static| Where-Object {
					$_.TypeName.Contains('Unsafe')
				}
			} 2> $null
		}1>> C:\Users\mst\Desktop\output.txt