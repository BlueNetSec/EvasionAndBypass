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
