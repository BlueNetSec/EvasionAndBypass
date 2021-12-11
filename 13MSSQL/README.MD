## MS SQL Enumeration

When a MS SQL server is running in the context of an Active Directory service account, it is
normally associated with a Service Principal Name (SPN). Threrefore a discreet way of locating instances of MS SQL in an Active Directory
environment is to query the domain controller for all registered SPNs related to MS SQL.

If we own a domain user, we can query the domain controller with the native setspn tool
