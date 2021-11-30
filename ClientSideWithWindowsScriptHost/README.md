## Basic Dropper in Jscript
Jscript is developed by Microsft that used in Internet Explorer. It can also be executed outside the browser through Windows Script Host(an automation technology for Microsoft Windows operating systems that provides scripting abilities think like batch files, but beefer).
We can use jscript to interact with ActiveX( software framework created by Microsoft back in 1996, it was desiged for content downloaded from a network(IE)) Consider the following code, We invoke **ActiveXObject** and use **WScript.Shell** to interact with the Windows Script Host Shell to execute external application. The file extension should be .js

```
var shell = new ActiveXObject("WScript.Shell")
var res = shell.Run("cmd.exe");
```

Method 1: Let's Create [Jscript payload Dropper](/ClientSideWithWindowsScriptHost/JscriptPayloadDropper.js) to download and execute payload from our wbesite.
Method 2: To Do, Let's make the dropper proxy-aware, research setProxy method, Page 107.
