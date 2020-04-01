Imports System.Data.OleDb
Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Collections.Specialized
Imports System.Xml

Module Module1
    'Code for TukTuk
    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing

    Dim dr As OleDbDataReader = Nothing
    Dim cmd2 As OleDbCommand

    Dim dr1 As OleDbDataReader = Nothing
    Dim line1 As OleDbDataReader = Nothing
    Dim dr2 As OleDbDataReader = Nothing
    Dim dr3 As OleDbDataReader = Nothing
    Dim dr4 As OleDbDataReader = Nothing
    Dim dr5 As OleDbDataReader = Nothing

    Dim drReturn As OleDbDataReader = Nothing
    Dim cmdReturn As OleDbCommand = Nothing

    Dim drReturnItems As OleDbDataReader = Nothing
    Dim cmdReturnItems As OleDbCommand = Nothing

    Dim drStock As OleDbDataReader = Nothing
    Dim cmdStock As OleDbCommand = Nothing


    Dim SEPARATOR_ITEMS As String = "|||||"
    Dim SEPARATOR_ITEMLINES As String = "<===>"
    Dim SEPARATOR_FIELDS As String = "<>"
    Dim SEPARATOR_ITEMFIELDS As String = "<<>>"


    Dim NumRecordsPerBatch As Integer = 0
    Dim isDebug As Boolean = False
    Dim username As String
    Dim password As String
    Dim last_createdtransaction_id As Integer = 0
    Dim serverProtocol As String
    Dim serverPath As String

    Sub Main()
        Console.WriteLine("Hello")

        Dim cmdArgs As String() = Environment.GetCommandLineArgs()
        Dim iniFile As String
        If (cmdArgs.Length = 2) Then
            iniFile = cmdArgs.ElementAt(1)
        Else
            WriteToEventLog("Missing INI file")
            Return
        End If
        Dim iniProps As NameValueCollection = ReadIniFile(iniFile)

        If (Not IsEmpty(iniProps.Get("DEBUG")) And iniProps.Get("DEBUG") = "1") Then
            isDebug = True
        End If

        serverProtocol = iniProps.Get("ServerProtocol")
        If (IsEmpty(serverProtocol)) Then
            WriteToEventLog("INI ERROR:Missing ServerProtocol")
            Return
        End If

        serverPath = iniProps.Get("ServerPath")
        If (IsEmpty(serverPath)) Then
            WriteToEventLog("INI ERROR:Missing ServerPath")
            Return
        End If

        username = iniProps.Get("Username")
        If (IsEmpty(username)) Then
            WriteToEventLog("INI ERROR:Missing Username")
            Return
        End If
        password = iniProps.Get("Password")
        If (IsEmpty(password)) Then
            WriteToEventLog("INI ERROR:Missing Password")
            Return
        End If
        Try
            password = RijndaelSimple.Decrypt(password, username)
        Catch ex As Exception

        End Try

        'Dim connectString As String = iniProps.Get("DBConnectString")
        'connectString = "Provider=SQLOLEDB;Data Source=DESKTOP-7DLH0GF;Initial Catalog=TUKTUK;Integrated Security=SSPI;"

        'If (IsEmpty(connectString)) Then
        'WriteToEventLog("INI ERROR:Missing DBConnectString")
        'Return
        'End If
        Dim numrecs As String = iniProps.Get("NumRecordsPerBatch")
        If (Not Integer.TryParse(numrecs, NumRecordsPerBatch)) Then
            WriteToEventLog("INI ERROR:Missing NumRecordsPerBatch")
            Return
        End If

        Dim ip As String = iniProps.Get("IP")
        If (IsEmpty(ip)) Then
            WriteToEventLog("INI ERROR:Missing IP")
            Return
        End If

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        'Dim order_table_name As String = ""
        'Dim tax_table_name As String = ""
        'Dim bill_table_name As String = ""
        'Dim set_table_name As String = ""


        'Dim response As String = serverUpload("lastuploadinfo.php", params)
        'Dim res_Arr() As String = response.Split("-")
        'Dim res_TukBill As String = res_Arr(0)
        'Dim res_TukDate As String = res_Arr(1)


        'need tochange response
        'from dec change date
        'from april change date and start bil from 0



        'Console.WriteLine("Res -> " + response)

        'Debug("lastuploadinfo=" + response)





        'last_sales_num = 3751
        Dim regDate As Date = Date.Now()
        Dim todaysdate As String = regDate.ToString("yyyyMMdd")
        ' Dim todaysdate As String = String.Format("yyyyMMdd", DateTime.Now)
        'Dim lastsalesyear As String = res_TukDate.Substring(0, 4)

        'order_table_name = "POSORD" + lastsalesyear
        'tax_table_name = "POSOTX" + lastsalesyear
        'bill_table_name = "PRISM.POSBIL" + lastsalesyear
        'set_table_name = "PRISM.POSSET" + lastsalesyear
        Dim transaction_id As String = ""



        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            'new code API call to fetch data
            'Dim dataurl As String = "192.168.1.3:88/cafenoir.svc/GetKotDetails/20180904/TUK/abcdxyz"
            Dim dataurl As String = "192.168.0.53/phoenix_new/public_html/api/test.php"
            'Console.WriteLine("dataurl : " + dataurl)

            Dim wHeader As WebHeaderCollection = New WebHeaderCollection()

            wHeader.Clear()
            wHeader.Add("Authorization: Bearer 0da6cf0d-848c-4266-9b47-cd32a6151b1f")
            wHeader.Add("Assume-User: john.doe%40smartsheet.com")

            Dim sUrl As String = serverProtocol + "://192.168.0.53/phoenix_new/public_html/api/test.php"
            Console.WriteLine("dataurl : " + sUrl)
            Dim wRequest As HttpWebRequest = DirectCast(System.Net.HttpWebRequest.Create(sUrl), HttpWebRequest)

            'wRequest.ContentType = "application/json" ' I don't know what your content type is
            wRequest.Headers = wHeader
            wRequest.Method = "GET"

            Dim wResponse As HttpWebResponse = DirectCast(wRequest.GetResponse(), HttpWebResponse)

            Dim sResponse As String = ""

            Using srRead As New StreamReader(wResponse.GetResponseStream())
                sResponse = srRead.ReadToEnd()
                Console.WriteLine(sResponse)
            End Using

            'Dim OutPutArray() As System.String
            'OutPutArray = Split(sResponse, "<")
            'Console.WriteLine(OutPutArray)

            'Dim document As New System.Xml.XmlDocument()
            'document.LoadXml(sResponse)



            Dim xmldata = sResponse.ToArray()
            Console.WriteLine(xmldata)


            'old code
            'cn = New OleDbConnection(connectString)
            'cn.Open()

            'Dim querystr As String = "select BILNUB,BILDAT,RESCOD from " + bill_table_name + " where BILDAT>='" + res_TukDate + "' and BILNUB>" + res_TukBill + " and UPDFLG=2 and VODSET<>0 and RESCOD='TUK' order by BILDAT,BILNUB"
            'Console.WriteLine("SaleHeader-" + querystr)
            'cmd = New OleDbCommand(querystr, cn)
            'dr = cmd.ExecuteReader

            'Dim sales_record As String = ""
            'Dim invoice_text As String = ""
            'Dim line_items As String = ""
            'Dim inv_count As Integer = 0

            'While dr.Read()
            '    Dim doc_no As String = dr.GetValue(0).ToString()
            '    Dim inv_no As String = ""
            '    Dim tot_disc As Double = 0.0
            '    Dim tot_gross_amt As Double = 0.0
            '    Dim tot_net_amt As Decimal = 0.0
            '    Dim tot_tax_amt As Decimal = 0.0
            '    Dim tot_sold_qty As Decimal = 0.0
            '    Dim tot_service_charge As Decimal = 0.0


            '    Dim inv_date As String = dr.GetValue(1).ToString()
            '    Dim docdat As String = inv_date
            '    Dim rescode As String = dr.GetValue(2).ToString()
            '    inv_no = doc_no + "-" + inv_date + "-" + rescode

            '    transaction_id = doc_no + "-" + inv_date

            '    Console.WriteLine("Order format" + transaction_id)
            '    inv_date = inv_date.Substring(0, 4) + "-" + inv_date.Substring(4, 2) + "-" + inv_date.Substring(6, 2)
            '    tot_gross_amt = 0.0
            '    tot_net_amt = 0.0
            '    tot_tax_amt = 0.0
            '    tot_disc = 0.0
            '    Dim payment_mode As String = ""
            '    tot_sold_qty = 0.0
            '    '/////////////////////
            '    Dim query_setamount As String = "select SETAMT from " + set_table_name + " where BILNUB=" + doc_no + " and RESCOD='" + rescode + "' and BILDAT=" + docdat + ""
            '    'Console.WriteLine("Items -" + query_1)

            '    cmd = New OleDbCommand(query_setamount, cn)
            '    dr2 = cmd.ExecuteReader
            '    While dr2.Read()
            '        Dim total_amt As Double = dr2.GetDecimal(0)
            '        tot_net_amt += total_amt
            '    End While

            '    Dim query_1 As String = "select ITMCOD,ITMNAM,RATAMT,VALAMT,Quanty from PRISM." + order_table_name + " where BILNUB=" + doc_no + " and RESCOD='" + rescode + "' and KOTDAT=" + docdat + ""
            '    'Console.WriteLine("Items -" + query_1)

            '    cmd = New OleDbCommand(query_1, cn)
            '    dr1 = cmd.ExecuteReader

            '    line_items = ""
            '    While dr1.Read()

            '        Dim itm_cd As String = dr1.GetValue(0).ToString()
            '        Dim item_name As String = dr1.GetString(1)
            '        Dim qty As Decimal = dr1.GetDecimal(4)
            '        Dim disc_amt As String = ""
            '        Dim net_value As String = dr1.GetDecimal(2)
            '        Dim total_amt As Double = dr1.GetDecimal(3)

            '        tot_sold_qty += qty
            '        'tot_net_amt += total_amt

            '        line_items += itm_cd.ToString() + SEPARATOR_FIELDS + item_name + SEPARATOR_FIELDS + qty.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + disc_amt.ToString() + SEPARATOR_FIELDS + total_amt.ToString() + SEPARATOR_FIELDS + net_value.ToString() + SEPARATOR_ITEMFIELDS
            '        'line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ITEMFIELDS
            '        'Console.WriteLine("invoice text : " + line_items)

            '    End While 'line item while closed

            '    Dim query_2 As String = "select TAXAMT,TAXCOD from PRISM." + tax_table_name + " where BILNUB=" + doc_no + " and RESCOD='" + rescode + "' and BILDAT=" + docdat + ""
            '    'Console.WriteLine("Items -" + query_1)

            '    cmd = New OleDbCommand(query_2, cn)
            '    dr2 = cmd.ExecuteReader


            '    While dr2.Read()
            '        Dim taxamt As Decimal = dr2.GetDecimal(0)
            '        Dim taxcode As String = dr2.GetValue(1).ToString()
            '        tot_tax_amt += taxamt
            '        'tot_service_charge()
            '        'If (taxcode.Equals("CGT")) Then
            '        '    tot_tax_amt += taxamt
            '        'ElseIf (taxcode.Equals("SGT")) Then
            '        '    tot_tax_amt += taxamt
            '        'End If

            '    End While 'line tax while closed

            '    tot_gross_amt = tot_net_amt + tot_service_charge
            '    Dim lastno As String = inv_no





            '    sales_record = lastno + SEPARATOR_FIELDS + inv_date + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + tot_gross_amt.ToString() + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS

            '    Console.WriteLine("sales record : " + sales_record)

            '    invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS

            '    'invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
            '    line_items = ""
            '    'sales_record = ""
            '    Console.WriteLine("invoice text : " + invoice_text)

            '    sales_send_count = sales_send_count + 1

            '    If sales_send_count = numrecs Then

            '        If invoice_text.Length >= 5 Then

            '            invoice_text = Left(invoice_text, Len(invoice_text) - 5)
            '            'Console.WriteLine(vbCrLf + " INV TEXT: " + vbCrLf)
            '            'Console.WriteLine(invoice_text)
            '            params.Add("salesbatch", invoice_text)
            '            sales_send_count = 0
            '            response = serverUpload("savebatch.php", params)
            '            Console.WriteLine("Saleresponse-" + response)
            '            params.Remove("salesbatch")
            '            invoice_text = ""

            '        End If

            '    End If

            '    tot_disc = 0.0
            '    tot_gross_amt = 0.0
            '    tot_net_amt = 0.0
            '    tot_tax_amt = 0.0
            '    tot_sold_qty = 0.0
            '    tot_service_charge = 0.0
            '    'line_items = ""
            'End While

            'If sales_send_count > 0 Then

            '    If invoice_text.Length >= 5 Then
            '        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
            '        Console.WriteLine(invoice_text)
            '        params.Add("salesbatch", invoice_text)
            '        sales_send_count = sales_send_count + 1
            '        response = serverUpload("savebatch.php", params)
            '        params.Remove("salesbatch")
            '        Console.WriteLine("Saleresponse-" + response)
            '        invoice_text = ""

            '    End If
            'End If
        Catch ex As Exception
            WriteToEventLog(ex.ToString)
        End Try

    End Sub

    Function ReadIniFile(ByVal iniFile As String) As NameValueCollection
        Dim lines As String() = System.IO.File.ReadAllLines(iniFile).ToArray()
        Dim line As String
        Dim tokens As String()
        Dim namevals As New NameValueCollection()
        For Each line In lines
            tokens = line.Split(New Char() {"="c}, 2)
            namevals.Add(tokens(0), tokens(1))
        Next line
        Return namevals
    End Function

    Function GetDate(ByVal dt As DateTime) As String
        Return dt.Date.ToString("yyyy-MM-dd")
    End Function

    Function GetTime(ByVal dt As DateTime) As String
        Return dt.TimeOfDay.ToString
    End Function

    Private Function serverUpload(ByVal subUrl As String, ByVal params As NameValueCollection)
        Dim webClient As New WebClient()

        ''Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.117:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://" + ip + "/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'Console.WriteLine("Server upload url :" + url)
        'Try
        '    'Console.WriteLine("URL is:" + url)
        '    Dim myCache As New CredentialCache()
        '    myCache.Add(New Uri(url), "Digest", New NetworkCredential(username, password))
        '    webClient.Credentials = myCache
        '    Dim responseArray As Byte() = webClient.UploadValues(url, params)
        '    Dim responseString As String = Encoding.ASCII.GetString(responseArray)
        '    serverUpload = responseString
        'Catch ex As Exception
        '    serverUpload = ex.Message()
        'End Try
        Return serverUpload

    End Function

    Private Function UrlAppend(ByVal url, ByVal suburl) As String
        If (url.EndsWith("/")) Then
            ' Do nothing
        Else
            url = url + "/"
        End If
        Return url + suburl
    End Function

    Private Function IsEmpty(ByVal textVal As String)
        If (textVal Is Nothing Or Trim(textVal) = "") Then
            IsEmpty = True
        Else
            IsEmpty = False
        End If
    End Function

    Private Sub Debug(ByVal msg As String)
        If (isDebug) Then
            WriteToEventLog(msg)
        End If
    End Sub

    Public Function WriteToEventLog(ByVal entry As String, Optional ByVal appName As String = "Intouch", Optional ByVal eventType As EventLogEntryType = EventLogEntryType.Information, Optional ByVal logName As String = "Application") As Boolean

        If (True) Then
            'Console.WriteLine("Debug:" + entry)
            Return True
        End If
        Dim objEventLog As New EventLog

        Try
            'Register the Application as an Event Source
            If Not EventLog.SourceExists(appName) Then
                EventLog.CreateEventSource(appName, logName)
            End If

            'log the entry
            objEventLog.Source = appName
            objEventLog.WriteEntry(entry, eventType)

            Return True

        Catch Ex As Exception

            'Console.WriteLine(Ex.ToString)
            Return False

        End Try

    End Function
End Module
