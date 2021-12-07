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
page 288