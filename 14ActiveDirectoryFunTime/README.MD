## AD Object Security Permissions

In active Drictory, access to an object is controlled through a Discretionary Access Control List(DACL), which consists of a series of Access Control Entries(ACE).  When multiple ACE's are present, their order is important. If a deny ACE comes before an allow ACE, the dent takes precedence. 

The ACE is stored arccording to the Secutiry Descriptor Definition Language(SDDL).
- ace_type:designates whether the ACE allows or denies permissions
- ace_flags:related to inheritance on child objects
- ace_rights
- object_guid
- inherit_object_guid
- account_sid:SID of the object

```
(A;;RPWPCCDCLCSWRCWDWOGA;;;S-1-1-0)
```
We can reference the [microsoft doc](https://docs.microsoft.com/en-us/windows/win32/secauthz/generic-access-rights) to read/understand the access rights
