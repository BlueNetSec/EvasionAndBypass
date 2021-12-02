## Process Injection and Migration TTPs
 After getting a shell, we want to extend longevity of our payload by inject into a process that is unlikely to terminate. Some good one to consider
 - explorer.exe -> hosting user's desktop experience
 - create a hidden notepad.exe process
 - migrate to a process that performs network communication. ex. svchost.exe
 
Think about this: A process is a container that is created to house a running application. Each process maintains its own virtual memory space. We can use Win32 APIs to interact with other process's virtual memory space.

A threat executes compiled assembly code of the application. A process may have multiple threads to perfom simultaneous actions, and each thread have its own stack and shares the memeory space of the process.

To inject a process we can use the folloing Win32 API
- 1.[OpenProcess](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess) open a channel from one process to another
- 2.[VirtualAllocEx](https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualallocex) and [WriteProcessMemory](https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-writeprocessmemory) to modify the memory space
- 3.[CreateRemoteThread](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createremotethread) create a new execution thread inside of the remote process

Now, We know what API to use, let's create a c# Console App(.NET Framework) [process inject code](/ProcessInjectionMigration/Program.cs) using the above APIs. Hint, don't forget use [P/Invoke resource](www.pinvoke.net)for reference on how to declear Win32API in C#.
- ToDo: The C# inject code works fine. Instead of hardcoding the Process ID, try to use **Process.GetProcessByName** method to resolve it dynamically.(page 140)
- ToDO:APIs NtCreateSection, NtMapViewOfSection, NtUnMapViewOfSection, and NtClose in ntdll.dll can be used as alternatives to VirtualAllocEx and WriteProcessMemory. Let's do some research and write this one in c#.(page 140)

## DLL injection
When a process need to use an API from DLL, it calls LoadLibrary API to load it into virual memory space. LoadLibrary can not be invoked on a remote, so we need to trick the remote process into executing LoadLrary, where lpLibFileName is the name of the dll.

```
HMODULE LoadLibraryA(
LPCSTR lpLibFileName
);
```

Consider CreateRemoteThread, argument **lpStartAddress** is the start address of the function run in the new thread, and fifth argument **lpParameter** is the memory address containing arguments for that address. So what we can do is to pass LoadLibraryA address to CreateRemoteThread's 4th argument, and pass in our dll name to 5th argument.
```
HANDLE CreateRemoteThread(
  [in]  HANDLE                 hProcess,
  [in]  LPSECURITY_ATTRIBUTES  lpThreadAttributes,
  [in]  SIZE_T                 dwStackSize,
  [in]  LPTHREAD_START_ROUTINE lpStartAddress,
  [in]  LPVOID                 lpParameter,
  [in]  DWORD                  dwCreationFlags,
  [out] LPDWORD                lpThreadId
);
```
We must consider some restictions:
- 1. the dll must be written in C/C++ and must be unmanaged, because managed c# based DLL will NOT work with unmanaged process.
- 2.DLLs contian APIs that are called after the DLL is loaded.  In order to call these APIs, an application would first have to “resolve” their names to memory addresses using GetProcAddress. In our case, GetProcAddress can't reslove an API in a remote process. We need to work around it.
- 3.let's create dll with msfvenom and write [inject c# code](/ProcessInjectionMigration/dllinject.cs) to force target to download our dll, load dll path into memory, and invoke dll with remotethreatexecute API by calling LoadlibaryA.

## Reflective DLL injection
Let's improve our ttp. The pervious inject method write dll to disk, which is significant compromise. LoadLibrary performs a series of actions including loading DLL files from disk and setting the correct memory permissions. In order to implement reflective DLL injection, we could write custom code to essentially recreate and improve upon the functionality of LoadLibrary.(TODO, I need to do more research for this....).

For Now, let's steal this [powershell reflective DLL injection code](/ProcessInjectionMigration/Invoke-ReflectivePEInjection.ps1) from other security researchers.
The script perfoms reflection to avoid writing assemblies to disk, and it parses the desired PE file. It has ablitiy toreflectively load dll or exe into local process, or load dll to a remote process. [ReflectedInjectDll](/ProcessInjectionMigration/ReflectiveDllInject.ps1) this code save our malicous dll to a byte array and invoke the ReflectivePEInjection to inject our dll into explorer process.

## Process Hollowing Theory
