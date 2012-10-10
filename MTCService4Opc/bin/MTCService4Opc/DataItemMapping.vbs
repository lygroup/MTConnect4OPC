'
' This software was developed by U.S. Government employees as part of
' their official duties and is not subject to copyright. No warranty implied 
' or intended.
'
' Questions: john.michaloski@Nist.gov

' GetElementById
' http://www.vistax64.com/vb-script/174625-editing-extended-active-directory-users-informations-vbscrip.html
' innerHTML is readonly for as pointed out here: http://msdn.microsoft.com/en-us/library/ms533897%28VS.85%29.aspx
' The property is read/write for all objects except the following, for which it is read-only: COL, COLGROUP, FRAMESET, HEAD, HTML, STYLE, TABLE, TBODY, TFOOT, THEAD, TITLE, TR. 


' VBSCRIPT HELP: http://www.w3schools.com/vbScript/vbscript_ref_functions.asp
'                http://msdn.microsoft.com/en-us/library/sx7b3k7y(VS.85).aspx 
' XML HELP:      http://www.devguru.com/Technologies/xmldom/quickref/xmldom_properties.html 
'------------------------------------------------------------------------
'Define Constants & Variables
'------------------------------------------------------------------------
'BEGIN CALLOUT A
Option Explicit

Dim strStatus, strPriority, strDueDate
Dim intRowCount : intRowCount = 2
'END CALLOUT A

Dim sleepamt,loopamt
Dim item

Dim ie
Dim xmlDoc1 
Dim rootNode , nodes
dim i,j
dim html
dim table

dim style
dim samples, events
dim sample,  e, child
dim strComputer
dim xmlfile 

sleepamt = 3000 
loopamt = 10000 
dim ObjFSO , InitFSO 

Set ObjFSO = CreateObject("UserAccounts.CommonDialog") 
ObjFSO.Filter = "XML|*.xml|All Files|*.*" 


InitFSO = ObjFSO.ShowOpen

If InitFSO = False Then
    Wscript.Echo "Script Error: Please select a file!"
    Wscript.Quit

End If

xmlfile = ObjFSO.FileName


Dim readings, readingKeys 
Set readings = CreateObject("Scripting.Dictionary")


style= style & "P" & vbCrLf
style= style & "{" & vbCrLf
style= style & "	FONT-FAMILY: ""Verdana"", sans-serif;" & vbCrLf
style= style & "	FONT-SIZE: 70%;" & vbCrLf
style= style & "	LINE-HEIGHT: 12pt;" & vbCrLf
style= style & "	MARGIN-BOTTOM: 0px;" & vbCrLf
style= style & "	MARGIN-LEFT: 10px;" & vbCrLf
style= style & "	MARGIN-TOP: 10px" & vbCrLf
style= style & "}" & vbCrLf

style= style & "H1" & vbCrLf
style= style & "{" & vbCrLf
style= style & "	BACKGROUND-COLOR: #003366;" & vbCrLf
style= style & "	BORDER-BOTTOM: #336699 6px solid;" & vbCrLf
style= style & "	COLOR: #ffffff;" & vbCrLf
style= style & "	FONT-SIZE: 130%;" & vbCrLf
style= style & "	FONT-WEIGHT: normal;" & vbCrLf
style= style & "	MARGIN: 0em 0em 0em -20px;" & vbCrLf
style= style & "	PADDING-BOTTOM: 8px;" & vbCrLf
style= style & "	PADDING-LEFT: 30px;" & vbCrLf
style= style & "	PADDING-TOP: 16px" & vbCrLf
style= style & "}" & vbCrLf
style= style & "table {" & vbCrLf
style= style & " 	BACKGROUND-COLOR: #f0f0e0;" & vbCrLf
style= style & "	BORDER-BOTTOM: #ffffff 0px solid;" & vbCrLf
style= style & "	BORDER-COLLAPSE: collapse;" & vbCrLf
style= style & "	BORDER-LEFT: #ffffff 0px solid;" & vbCrLf
style= style & "	BORDER-RIGHT: #ffffff 0px solid;" & vbCrLf
style= style & "	BORDER-TOP: #ffffff 0px solid;" & vbCrLf
style= style & "	FONT-SIZE: 70%;" & vbCrLf
style= style & "	MARGIN-LEFT: 10px" & vbCrLf
style= style & "  }" & vbCrLf

style= style & "td {" & vbCrLf
style= style & "	BACKGROUND-COLOR: #e7e7ce;" & vbCrLf
style= style & "	BORDER-BOTTOM: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-LEFT: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-RIGHT: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-TOP: #ffffff 1px solid;" & vbCrLf
style= style & "	PADDING-LEFT: 3px" & vbCrLf
style= style & "  }" & vbCrLf
style= style & "th {" & vbCrLf
style= style & "	BACKGROUND-COLOR: #cecf9c;" & vbCrLf
style= style & "	BORDER-BOTTOM: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-LEFT: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-RIGHT: #ffffff 1px solid;" & vbCrLf
style= style & "	BORDER-TOP: #ffffff 1px solid;" & vbCrLf
style= style & "	COLOR: #000000;" & vbCrLf
style= style & "	FONT-WEIGHT: bold" & vbCrLf
style= style & "  }" & vbCrLf


''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
' http://support.microsoft.com/kb/246067
'
Const dictKey  = 1
Const dictItem = 2

Function SortDictionary(objDict,intSort)
  ' declare our variables
  Dim strDict()
  Dim objKey
  Dim strKey,strItem
  Dim X,Y,Z

  ' get the dictionary count
  Z = objDict.Count

  ' we need more than one item to warrant sorting
  If Z > 1 Then
    ' create an array to store dictionary information
    ReDim strDict(Z,2)
    X = 0
    ' populate the string array
    For Each objKey In objDict
        strDict(X,dictKey)  = CStr(objKey)
        strDict(X,dictItem) = CStr(objDict(objKey))
        X = X + 1
    Next

    ' perform a a shell sort of the string array
    For X = 0 to (Z - 2)
      For Y = X to (Z - 1)
        If StrComp(strDict(X,intSort),strDict(Y,intSort),vbTextCompare) > 0 Then
            strKey  = strDict(X,dictKey)
            strItem = strDict(X,dictItem)
            strDict(X,dictKey)  = strDict(Y,dictKey)
            strDict(X,dictItem) = strDict(Y,dictItem)
            strDict(Y,dictKey)  = strKey
            strDict(Y,dictItem) = strItem
        End If
      Next
    Next

    ' erase the contents of the dictionary object
    objDict.RemoveAll

    ' repopulate the dictionary with the sorted information
    For X = 0 to (Z - 1)
      objDict.Add strDict(X,dictKey), strDict(X,dictItem)
    Next

  End If

End Function


'
'
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
function updatetable(machine)
  dim entry
   	updatetable=""
	xmlDoc1.Load(xmlfile )
	if(  0 <> Err.Number ) then 
	     MsgBox "Bad xml file"
 	    Wscript.Quit(0)
	End if 

On Error Resume Next
	Set rootNode = xmlDoc1.documentElement
	set nodes = rootNode.selectNodes("//Device")
	For Each item In nodes

	  updatetable = updatetable &  "<P>" & item.attributes.getNamedItem("name").nodeValue
	  updatetable = updatetable &  "<TABLE>"
	  updatetable = updatetable & "<TR> <TH>Name </TH> <TH> DataItemID </TH> <TH>Category </TH> " & vbCRLF
	  updatetable = updatetable & " <TH>Type </TH> <TH> Units </TH> </TR>" & vbCRLF
	  readings.RemoveAll

	  ''''''''''''''''''''''''''''''''''''''''
	  set samples = item.selectNodes(".//DataItems")

	  For Each sample In samples

		For each child in sample.childNodes

			'updatetable =  updatetable & "<TR>"
	 		'updatetable =  updatetable &  "<TD> " & child.attributes.getNamedItem("name").nodeValue & "</TD> "
	 		'updatetable =  updatetable &  "<TD> " & child.attributes.getNamedItem("id").nodeValue & "</TD> "
	 		'updatetable =  updatetable &  "<TD> " & child.attributes.getNamedItem("category").nodeValue & "</TD> "
			'updatetable =  updatetable & "</TR>"
			entry = child.attributes.getNamedItem("id").nodeValue 
			entry = entry &  "</TD><TD>" & child.attributes.getNamedItem("category").nodeValue 
			entry = entry &  "</TD><TD>" & child.attributes.getNamedItem("type").nodeValue 
			entry = entry &  "</TD><TD>" & child.attributes.getNamedItem("units").nodeValue
			readings.Add child.attributes.getNamedItem("name").nodeValue,  entry
		Next
	  Next


	SortDictionary readings,1 
	readingKeys = readings.Keys
	for i = 0 to readings.Count -1
			updatetable =  updatetable & "<TR>"
			updatetable =  updatetable & "<TD> " & readingKeys(i)    & "</TD> "
			updatetable =  updatetable & "<TD> " & readings.Item( readingKeys(i))   & "</TD> "
			updatetable =  updatetable & "</TR>"

 	next 

	  updatetable = updatetable &  "</TABLE> " & vbCrLf 
	Next


end function
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
sub SetDiv(id, str)
  	ie.Document.GetElementById(id).innerHtml  = str
	if(  0 <> Err.Number ) then 
		MsgBox "Bye"
     		Wscript.Quit(0)
	End If
End Sub 
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Set ie = WScript.CreateObject("InternetExplorer.Application")
   ie.visible = 1         ' keep visible
 ie.navigate "about:blank"
 ie.Document.Open

'MakeIEDoc (" <BODY> Hello World </BODY> </HTML>")

Set xmlDoc1 = CreateObject("Msxml2.DOMDocument")
xmlDoc1.async = False
'xmlDoc1.setProperty "ServerHTTPRequest", true
j=0
i=2
On Error Resume Next

'for j = 0 to 1
   

html = ""
ie.Document.write "<HTML>" & vbCrLf 
ie.Document.write "<HEAD><STYLE>" & style & "</STYLE> </HEAD>" & vbCrLf 
ie.Document.write "<BODY>" & vbCrLf 
ie.Document.write "<H1> MTConnect Readings</H1>" & vbCrLf 
ie.Document.write "<DIV id=""Device""> Loading... </DIV>" & vbCrLf 
ie.Document.write "</BODY>" & vbCrLf 
ie.Document.write "</HTML>" & vbCrLf 


ie.Document.close






for j = 0 to loopamt 

'On Error Resume Next
table = ""
table = table &  "<P> Current Readings " & j  & "<BR>" 
table = table & updatetable(strComputer)

SetDiv "Device", table 




Wscript.Sleep(sleepamt)

Next