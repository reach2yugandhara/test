Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for children place
    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing
    Dim dr As OleDbDataReader = Nothing
    Dim cmd2 As OleDbCommand
    Dim cmd3 As OleDbCommand
    Dim dr1 As OleDbDataReader = Nothing
    Dim line1 As OleDbDataReader = Nothing
    Dim dr2 As OleDbDataReader = Nothing
    Dim dr3 As OleDbDataReader = Nothing
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
    'Dim serverUrl As String
    Dim username As String
    Dim password As String
    Dim last_createdtransaction_id As Integer = 0
    Dim serverProtocol As String
    Dim serverPath As String

    Sub Main()
        Dim i As Integer = 0
        Dim j As Integer = 0
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

        Dim connectString As String = iniProps.Get("DBConnectString")
        If (IsEmpty(connectString)) Then
            WriteToEventLog("INI ERROR:Missing DBConnectString")
            Return
        End If
        Dim numrecs As String = iniProps.Get("NumRecordsPerBatch")
        If (Not Integer.TryParse(numrecs, NumRecordsPerBatch)) Then
            WriteToEventLog("INI ERROR:Missing NumRecordsPerBatch")
            Return
        End If

        'Dim recordTypeToFetch As String = iniProps.Get("RecordTypeToFetch")
        'If (IsEmpty(recordTypeToFetch)) Then
        ' WriteToEventLog("INI ERROR:Missing RecordTypeToFetch")
        ' Return
        ' End If
        '-----------------------------------------------------------------------------------------------------
        ' recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        Dim response1 As String = serverUpload("lastuploadinfo.php", params)
        'Dim response_dates() As String = response1.Split(",")
        Dim sale_date As String = response1
        'Dim return_date As String = response_dates(1)


        Debug("lastuploadinfo=" + response1)
        '' FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()

            Dim querystr As String = "select RIH_SR_CD,RIH_INVNO ,RIH_INVDTE   ,(RIH_BILLDISCPER + RIH_DISCAMT) as disc ,RIH_TOTALQTY ,RIH_BILLAMT ,RIH_TAXAMT,RIH_VOIDFLAG,RIH_PAYMENT_MODE  from dbo.RETAIL_INVOICEHEADER where  RIH_INVDTE > '" + sale_date + "' order by RIH_INVDTE"
            'Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()

                Dim billId As Integer = dr.GetValue(0)
                Dim receiptNo As String = dr.GetString(1)
                Dim docDt As String = dr.GetDateTime(2).ToString("yyyy-MM-dd")
                Dim docTime As String = dr.GetDateTime(2).ToString("hh':'mm':'ss")
                Dim grossValue As Decimal = 0
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(5))
                Dim tax As Double = dr.GetValue(6)
                Dim totDisc As String = Convert.ToString(dr.GetValue(3))
                Dim saleQtySum As Double = dr.GetValue(4)
                Dim transaction_id As String = dr.GetDateTime(2).ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")
                Dim inv_status As String = dr.GetString(7)
                Dim pmode As String = dr.GetString(8)
                Console.WriteLine("-------------------------------------------------------------")

                Dim query_1 As String = "select RID_ITEMCODE ,RID_QTY ,RID_BASIC_AMT ,RID_DISC_AMT ,RID_TOT_TAX_AMT,RID_ITEMDESC,RID_PRICE  from dbo.RETAIL_INVOICEDETAIL where RID_INVNO = '" + receiptNo + "'"
                ' Console.WriteLine(query_1)
                cmd = New OleDbCommand(query_1, cn)
                dr1 = cmd.ExecuteReader
                Dim ItemCode As String = ""
                Dim lineItemName As String = ""
                Dim lineQuantity As Double = 0
                Dim TaxAmt As Double = 0
                Dim discountVal As Double = 0
                Dim lineTotal As Decimal = 0
                Dim unitPrice As Decimal = 0
                Dim grossamt As Double = 0
                Dim cal_qty As Double = 0
                Dim cal_tax As Double = 0
                Dim cal_dis As Double = 0
                Dim cal_linetotal As Double = 0
                Dim cal_unit As Double = 0
                Dim cal_gross As Double = 0

                While dr1.Read()
                    ItemCode = dr1.GetString(0)
                    lineItemName = Convert.ToString(dr1.GetValue(5))

                    'cal_qty = dr1.GetValue(2)
                    lineQuantity = dr1.GetValue(1)

                    If IsDBNull(dr1.GetValue(4)) Then
                        TaxAmt = 0.0
                    Else
                        cal_tax = dr1.GetValue(4)
                        TaxAmt = TaxAmt + cal_tax
                    End If
                    
                    'cal_dis = dr1.GetValue(4)
                    discountVal = dr1.GetValue(3)

                    'cal_linetotal = dr1.GetValue(5)
                    lineTotal = dr1.GetValue(2)

                    cal_gross = dr1.GetValue(2)
                    grossamt = grossamt + cal_gross

                    unitPrice = Convert.ToDecimal(dr1.GetValue(6))

                    'Item(level)
                    line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ITEMFIELDS
                    'tax = TaxAmt
                    saleQtySum = lineQuantity
                    grossValue = grossamt
                    tax = TaxAmt
                End While
                'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossValue.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossValue.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                If inv_status = "N" Then
                    sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossValue.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                Else
                    sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossValue.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                End If
                '   Console.WriteLine("SALES RECORD ==>" + sales_record)
                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                inv_count = inv_count + 1
                If (inv_count = NumRecordsPerBatch) Then
                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        params.Add("salesbatch", invoice_text)
                        Console.WriteLine("invoice text " + invoice_text)
                        response1 = serverUpload("savebatch.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response ->" + response1)
                        inv_count = 0
                        invoice_text = ""
                        sales_record = ""
                    End If
                End If
            End While

            If (inv_count > 0) Then
                If invoice_text.Length >= 5 Then
                    invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    params.Add("salesbatch", invoice_text)
                    inv_count = inv_count + 1
                    'Console.WriteLine(invoice_text)
                    response1 = serverUpload("savebatch.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Sale Response ->" + response1)
                    invoice_text = ""
                End If
            End If

        Catch ex As Exception
            WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
        End Try
        count = 0
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
        'serverProtocol = 
        'Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        Dim url As String = serverProtocol + "://localhost:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl
        ' url = UrlAppend(url, subUrl)
        'Console.WriteLine("URL is: " + url)
        'Console.WriteLine(params.Get("salesbatch"))
        Try
            Dim myCache As New CredentialCache()
            myCache.Add(New Uri(url), "Digest", New NetworkCredential(username, password))
            webClient.Credentials = myCache
            Dim responseArray As Byte() = webClient.UploadValues(url, params)
            Dim responseString As String = Encoding.ASCII.GetString(responseArray)
            serverUpload = responseString
            'If subUrl = "savebatch.php" Then
            '    Console.WriteLine(responseString)
            'End If
        Catch ex As Exception
            serverUpload = ex.Message()
        End Try
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
            Console.WriteLine("Debug:" + entry)
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
