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

            String execCmd = "select version from openquery(\"dc01\", 'select @@version as version')";
            String localCmd = "select SYSTEM_USER;";
            String remoteCmd = "select myuser from openquery(\"dc01\", 'select SYSTEM,_USER as myuser')";

            //check if we can run query tho link
            SqlCommand command = new SqlCommand(execCmd, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Linked SQL server version: " + reader[0]);
            reader.Close();

            //print security context at local database when execute using link
            command = new SqlCommand(localCmd, con);
            reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Executing as the login " + reader[0] + "on APPSRV01");

            //print security context at DC1 when execute using link
            command = new SqlCommand(remoteCmd, con);
            reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Executing as the login " + reader[0] + "on DC01");

            con.Close();


        }
    }
}
