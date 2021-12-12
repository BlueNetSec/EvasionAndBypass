using System;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.Diagnostics;
//we can compiled the code into a DLL, we have the assembly that we are going to load into the SQL server and execute
public class StoredProcedures
{
    //the method must be marked as store procedure
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void cmdExec(SqlString execCommand)
    {
        //create a cmd process 
        Process proc = new Process();
        proc.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
        proc.StartInfo.Arguments = string.Format(@" /C {0}", execCommand);

        proc.StartInfo.UseShellExecute = false;
        //we want to redirect output to pipe
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();


        SqlDataRecord record = new SqlDataRecord(new SqlMetaData("output", System.Data.SqlDbType.NVarChar, 4000));
        //start recording
        SqlContext.Pipe.SendResultsStart(record);
        //copy the contents of the Process object StandardOutput property into the record
        record.SetString(0, proc.StandardOutput.ReadToEnd().ToString());

        //record data,
        SqlContext.Pipe.SendResultsRow(record);
        //stop recording
        SqlContext.Pipe.SendResultsEnd();
        proc.WaitForExit();
        proc.Close();
    }
};