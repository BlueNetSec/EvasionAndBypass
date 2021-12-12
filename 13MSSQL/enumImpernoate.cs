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
          
          //The SQL query to invoke xp_dirtree contains a number of backslashes, both to escape the double quote required by the SQL query and to escape the backslashes in the UNC path as required by C# strings.
          String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
          SqlCommand command = new SqlCommand(query, con);
          SqlDataReader reader = command.ExecuteReader();
          while(reader.Read() == true)
          {
            Console.WriteLine("Logins that can be impersonated: " + reader[0]);
          }
                    
          reader.Close();
    
  
          
     
         
          con.Close();         
          
        }
    }
}
