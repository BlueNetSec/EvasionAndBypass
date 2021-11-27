**HTML Smuggling**

Method 1: instructs the browser to automatically download a file when a user clicks on the hyperlink [method 1html file](/ClientSide/html) Method 1 works fine, but filename and extension of the dropper are exposed.

Method 2: a) **First**, Let's create a based64 payload and store it as [Blob Object](https://developer.mozilla.org/en-US/docs/Web/API/Blob), a javascript data type-file-like object of immutable, raw data; they can be read as text or binary data.  We can store the base64 payload inside of the Blob variable. **Second** use the Blob to create URL file obejct that simulates file on web server. **Last** create invisible anchor tag to trigger download actiopn when page loaded. The web url is invoke with **window.URL.createObjectURL** this ttp works against Chrome. [Method 2 html](/ClientSide/method2-html.html)

Method 3: To Do, modify the Method2 ttp to use **window.navigator.msSaveBlob**, so this hosting works IE and Edge.


**Phishing with Microsoft Office**

Method 1 warm up: lanuch cmd with VBS marco, with vbHide and shell option.[link](/ClientSide/method1cmd.vbs)

Method 2 warm up: lanuch cmd with CreateObject WSH shell.[link](/ClientSide/method2cmd.vbs)

page 48 tbc

