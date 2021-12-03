## HTML Smuggling

Method 1: instructs the browser to automatically download a file when a user clicks on the hyperlink [method 1html file](/01ClientSideWithOffice/html) Method 1 works fine, but filename and extension of the dropper are exposed.

Method 2: a) **First**, Let's create a based64 payload and store it as [Blob Object](https://developer.mozilla.org/en-US/docs/Web/API/Blob), a javascript data type-file-like object of immutable, raw data; they can be read as text or binary data.  We can store the base64 payload inside of the Blob variable. **Second** use the Blob to create URL file obejct that simulates file on web server. **Last** create invisible anchor tag to trigger download actiopn when page loaded. The web url is invoke with **window.URL.createObjectURL** this ttp works against Chrome. [Method 2 html](/01ClientSideWithOffice/method2-html.html)

Method 3: To Do, modify the Method2 ttp to use **window.navigator.msSaveBlob**, so this hosting works IE and Edge.


## Phishing with Microsoft Office

Method 1 warm up: lanuch cmd with VBS marco, with vbHide and shell option.[link](/01ClientSideWithOffice/method1cmd.vbs)

Method 2 warm up: lanuch cmd with CreateObject WSH shell.[link](/01ClientSideWithOffice/method2cmd.vbs)

Method 3 : use powershell download method in vbs script, use a custom wait function to wait the download to finish, and then execute downloaded binary(ActiveDocument.Path->return the current path of the world documents)[vbs dowanload marco](/01ClientSideWithOffice/method3powershell.vbs)

Method 4: To Do, user Invoke-WebRequest method(page 51)


## Keep up Appearances

Method 1: Switcheroo, make user believe that the content is encrypted, the user has to click on enable to see decrypted content.
- Create decrypted text and select them all
- Insert > Quick Parts > AutoTexts and Save Selection to AutoText Gallery: (In the Create New Building Block dialog box, remember the **Name** )
- Remove the decrypted messages and insert encrypted text in the word document
- Create the [marco](/01ClientSideWithOffice/Switcheroo.vbs) that will delete the encrypted text and replace it with decrypted text

Method 2: TO DO, Create a Marco that use both swithceroo and execute a payload. (page 58)

Method 3: (run VBA shellcode in memory): Calling Win32APIs from VBA, useing 3 windows 32 APIs from kernel32.dll VirtualAlloc, RtlMoveMemory,and CreateThread.
- VirtualAlloc, allocate unmanaged memory that is writable, readable, and executable.
- RtlMoveMemory, copy shell code into memory space
- CreateThread, execute the shellcode
- [VBA memory shellcode runner](/01ClientSideWithOffice/method3vbamemoryshellcode.vbs)

## Calling Win32 API from powershell
Summary: PowerShell cannoy natively inteact with Win32APIs, but we can use c# in powershell session. In C#, We can declare and import Win32 APIs using **DllImportAttribute**, this allows us to invoke functions in unmanaged dynamic link libraries. The easily way is [PInvoke](www.pinvoke.net)

Method 1: Add-Type TTPs, Create a powershell variable and set it to a block of text. Inside that text, we use C# sytnx to import the desired APIs, finally we use **Add-Type** to compile the C# code contained in the decleared variable.
- Create the [powershell memory shell code runner](/01ClientSideWithOffice/memoryShellCodeRunner.ps1)
- Create the [VBA Marco Script](/01ClientSideWithOffice/powershellmemorydownloadCradle.vbs) for target to downaload the powershell code into memory and execute

Notes on Method 1: Add-Type Compilation, the Add-Type method 1 use the .NET framework to compile C# code. This process is performed by Visual C# command line compiler **csc**. During this process both C# soruce code along and the compilred C# assesmbly are temporarily written to disk.
- Investigating the powershell Add-Type keyword steps
- 1.Use Process Monitor to filter **powershell_ise.exe** process , clear the old events with Ctrl+X
- 2.Run the add type powershell ISE
- 3.Process Monitor lists many eventsincluding CreateFile, WriteFile, and CloseFile operations including rtylilrr.0.cs and rtylilrr.dll
- 4.file extensions suggest that both the C# source code and the compiled code have been written to the hard drive
- 5.If suspicion is correct, then the rtylilrr.dll assembly should be loaded into the PowerShell ISE process
- 6.list loaded assemblies using the GetAssemblies method on the CurrentDomain object
```
C:\Windows\SysWOW64\WindowsPowerShell\v1.0> [appdomain]::currentdomain.getassemblies() | Sort-Object -Property fullname | Format-Table fullname
....
....
qdrje0cy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
r1b1e3au, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
rtylilrr, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
```
- 7.investigation reveals that PowerShell writes a C# source code file (.cs) to the hard drive, which is compiled into an assembly (.dll) and then loaded into the process. Add-Type code will likely be flagged by endpoint antivirus

## Improved shellcode runner with dynamic lookup(Microsoft.Win32.UnsafeNativeMethods -> GetModuleHandle and GetProcAddress)
Summary: We want to create .NET assembly in memory instead of writitng code and compiling it. Since We can't create any new assemblies, we need to locate exisitng assembiles that we can reuse. 

**GetModuleHandle** obtains a handle to the specified DLL(the memory address of the DLL). We can then pass the DLL handle and the function name to **GetProcAddress**, which will return the function address. We can uses these two fucntions to locate any API, but we must invoke them without using Add-Type.
- 1.We will use this [parseLoadedAssemblies script](/01ClientSideWithOffice/parseLoadedAssemblies.ps1) to search assemblies that contain both GetModuleHandle and GetProcAddress
- 2.Now We identify the GetProcAddress and GetModuleHandle in **UnsafeNativeMethods**, let's search which dll module contains UnsafeNativeMethods with [Search dll script](/01ClientSideWithOffice/IdentifyAssemblies.ps1).
- 3.However, these functions are only meant to be used internally by the .NET, we need to obtain a erference of these functions to use them in powershell using **GetType** method. The reference to the **System.dll** will allow us to subsequently locate the GetModuleHandle and GetProcAddress methods inside it. Consider the following example code for [MessageBox method](/01ClientSideWithOffice/messageBoxMemoryExample.ps1)
- 4.Let's rewrite the message box example to a function, aka[LookupFunc](/01ClientSideWithOffice/LookupFunc.ps1) so we can import it anywhere.
- 5.Now we can get address of any function, let's manually create an message box assembly in memory and populate it with content, aka [full Delegate message box code](/01ClientSideWithOffice/messageBoxMemoryCompleted.ps1).This is an example of ReflectedDelegate attack. Note: In the Microsoft .NET framework, an assembly is a partially compiled code library could be dll or exe.
- 6.Now we can create a [reflected shell code runner](/01ClientSideWithOffice/ReflectedShellCodeRunner.ps1) by constructing our own dll in memory, we will need to create virtualAlloc, CreateThread, and WaitForSingleObject.
- 7.TO DO, the reflected shell code runner was based on 32-bit PowerShell Process. Modify the code so it works on 64-bit PowerShell Porcess.(pg 94)

## Powershell Script Web Hosting with Proxy 
Summary, the powershell ```Net.WebClient``` download cradle is proxy-aware. The proxy setting used by Net.WebClient are stored in the .proxy property and are populated
from the **DefaultWebProxy** property when creating the object. We can view these settings using the **GetProxy** method by specifying the URL to test against.
```
[System.Net.WebRequest]::DefaultWebProxy.GetProxy("http://yourwebhostip/run.ps1")
```
Now to pervent using the proxy,(sometime blue team use proxy to monitor network traffic), we can modify the download cradle by change the proxy setting to null. In addtion, the defult  Net.WebClinet download cradle has no user-agent string. We can add it to blend in more. [DownloadCradle code](/01ClientSideWithOffice/ImprovedDownloadCradle.ps1)

## Net.WebClient for SYSTEM Proxy
System account does not have a proxy configuration set. If the environment has Proxy for outgoing web traffic, the script host/web download cradle TTPs may fail to call back to C2.

To force the session through a proxy, we can copy a configuration from a standard user account on the system to system account's proxy setting. Proxy settings for each user are stored in the registry at the following path **HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\InternetSettings**

[System proxy download cradle code](/01ClientSideWithOffice/SystemProxyWebCradle.ps1) fixs this issue.

 

