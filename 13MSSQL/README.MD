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
