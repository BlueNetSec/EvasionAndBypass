**HTML Smuggling**

Method 1: instructs the browser to automatically download a file when a user clicks on the hyperlink [method 1html file](/ClientSide/html) Method 1 works fine, but filename and extension of the dropper are exposed.

Method 2: a) **First**, Let's create a based64 payload and store it as [Blob Object](https://developer.mozilla.org/en-US/docs/Web/API/Blob), a javascript data type-file-like object of immutable, raw data; they can be read as text or binary data.  We can store the base64 payload inside of the Blob variable. **Second** use the Blob to create URL file obejct that simulates file on web server. **Last** create invisible anchor tag to trigger download actiopn when page loaded. The web url is invoke with **window.URL.createObjectURL** this ttp works against Chrome. [Method 2 html](/ClientSide/method2-html.html)

Method 3: To Do, modify the Method2 ttp to use **window.navigator.msSaveBlob**, so this hosting works IE and Edge.


**Phishing with Microsoft Office**

Method 1 warm up: lanuch cmd with VBS marco, with vbHide and shell option.[link](/ClientSide/method1cmd.vbs)

Method 2 warm up: lanuch cmd with CreateObject WSH shell.[link](/ClientSide/method2cmd.vbs)

Method 3 : use powershell download method in vbs script, use a custom wait function to wait the download to finish, and then execute downloaded binary(ActiveDocument.Path->return the current path of the world documents)[vbs dowanload marco](/ClientSide/method3powershell.vbs)

Method 4: To Do, user Invoke-WebRequest method(page 51)


**Keep up Appearances**

Method 1: Switcheroo, make user believe that the content is encrypted, the user has to click on enable to see decrypted content.
- Create decrypted text and select them all
- Insert > Quick Parts > AutoTexts and Save Selection to AutoText Gallery: (In the Create New Building Block dialog box, remember the **Name** )
- Remove the decrypted messages and insert encrypted text in the word document
- Create the [marco](/ClientSide/Switcheroo.vbs) that will delete the encrypted text and replace it with decrypted text

Method 2: TO DO, Create a Marco that use both swithceroo and execute a payload. (page 58)

Method 3(run VBA shellcode in memory): Calling Win32APIs from VBA, useing 3 windows 32 APIs from kernel32.dll VirtualAlloc, RtlMoveMemory,and CreateThread.
- VirtualAlloc, allocate unmanaged memory that is writable, readable, and executable.
- RtlMoveMemory, copy shell code into memory space
- CreateThread, execute the shellcode
- [VBA memory shellcode runner](/ClientSide/method3vbamemoryshellcode.vbs)

