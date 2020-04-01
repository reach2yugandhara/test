Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'code for 612 league new
    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing
    Dim dr As OleDbDataReader = Nothing
    Dim cmd2 As OleDbCommand
    Dim cmd3 As OleDbCommand
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
    'Dim serverUrl As String
    Dim username As String
    Dim password As String
    Dim last_createdtransaction_id As Integer = 0
    Dim serverProtocol As String
    Dim serverPath As String

    Sub Main()
        Dim i As Integer = 0
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
            Main()
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

        Dim recordTypeToFetch As String = iniProps.Get("RecordTypeToFetch")
        If (IsEmpty(recordTypeToFetch)) Then
            WriteToEventLog("INI ERROR:Missing RecordTypeToFetch")
            Return
        End If
        '-----------------------------------------------------------------------------------------------------
        recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")




        Dim response As String = serverUpload("lastuploadinfo.php", params)
        Dim res() As String = response.Split(",")
        Dim salres As String = res(0)
        Dim retres As String = res(1)

        Debug("lastuploadinfo=" + response)
        'orderformat=17406

        Console.WriteLine("sales Response :: " + response)

        '' FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            'select VchIDPrefix,VchIDYMD,VchNumber,VoucherDate,BillAmount+(RoundOffAmt*-1) as InvooiceAmt,TaxTotal,QtyTotal,SubTotal from dbo.SaleHeader
            Dim querystr As String = "select VchIDPrefix,VchIDYMD,VchNumber,VoucherDate,BillAmount+(RoundOffAmt*-1) as InvooiceAmt,TaxTotal,QtyTotal,SubTotal from dbo.SaleHeader where VoucherDate>'" + salres + "'  order by VoucherDate"
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()
                Dim Prefix As String = dr.GetValue(0).ToString
                Dim IDYMD As String = dr.GetValue(1).ToString
                Dim VchNumber As String = dr.GetValue(2).ToString
                Dim receiptNo As String = Prefix + "/" + IDYMD + "/" + VchNumber
                Dim docDt As String = dr.GetDateTime(3).ToString("yyyy-MM-dd")
                'receiptNo = receiptNo + "-" + docDt
                Dim docTime As String = dr.GetDateTime(3).ToString("hh':'mm':'ss")
                Dim totval As Double = Convert.ToDouble(dr.GetValue(4))
                Dim gross As Decimal = Convert.ToDecimal(dr.GetValue(7))
                'Dim totval As Double = 
                'totNetDocValue = totval
                Dim totDisc As String = "0.0"
                Dim transaction_id As String = dr.GetDateTime(3).ToString("yyyy-MM-dd hh':'mm':'ss")
                Console.WriteLine("-------------------------------------------------------------")
               
                Dim tax As Double = Convert.ToDouble(dr.GetValue(5))
                Dim saleQtySum As Double = Convert.ToDouble(dr.GetValue(6))
                ' Dim pmode As String = Convert.ToString(dr.GetValue(6))
                'Dim cm_id As String = Convert.ToString(dr.GetValue(7))
     


                
                'totval = totval + tax
                'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + gross.ToString + SEPARATOR_FIELDS + totval.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + gross.ToString + SEPARATOR_FIELDS + totval.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                Console.WriteLine("SALES RECORD" + sales_record)
              
                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + "" + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                inv_count = inv_count + 1
                If (inv_count = NumRecordsPerBatch) Then

                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        params.Add("salesbatch", invoice_text)
                        response = serverUpload("savebatch.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response ->" + response)
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
                    response = serverUpload("savebatch.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Response ->" + response)
                    invoice_text = ""
                End If
            End If

        Catch ex As Exception
            WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
        End Try
        count = 0

        '////////////////////////////////////////////////return////////////////
        'Dim sales_send_count As Integer = 0
        'Dim invoices_to_send As SortedList = New SortedList()
        'Dim count As Integer = 0
        'Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            'select a.INVOICENO , a.invoicedate,a.basicamt,a.BILLAMT,((a.discount/100)*a.BASICAMT) as discount,a.invoiceno,(select b.payment_type from dbo.PAYMENTMODE b where a.paymenttype=b.PKEY),pkey from dbo.invoice a where a.invoiceno>'" + salesres + "' and a.status='BILLED' order by a.invoiceno
            'Dim querystr As String = "select cnno,CREDITNOTEAMT,INVOICEDATE,pkey from dbo.invoice where PAYMENTTYPE=7 and pkey >'" + retres + "'  order by pkey"
            Dim querystr As String = "select VchIDPrefix,VchIDYMD,VchNumber,VoucherDate,BillAmount+(RoundOffAmt*-1) as InvooiceAmt,TaxTotal,QtyTotal,SubTotal from dbo.SaleReturnHeader where VoucherDate>'" + retres + "'  order by VoucherDate"
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()

                Dim Prefix As String = dr.GetValue(0).ToString
                Dim IDYMD As String = dr.GetValue(1).ToString
                Dim VchNumber As String = dr.GetValue(2).ToString
                Dim receiptNo As String = Prefix + "/" + IDYMD + "/" + VchNumber
                Dim docDt As String = dr.GetDateTime(3).ToString("yyyy-MM-dd")
                'receiptNo = receiptNo + "-" + docDt
                Dim docTime As String = dr.GetDateTime(3).ToString("hh':'mm':'ss")
                Dim totval As Double = Convert.ToDouble(dr.GetValue(4))
                Dim gross As Decimal = Convert.ToDecimal(dr.GetValue(7))
                'Dim totval As Double = 
                'totNetDocValue = totval
                Dim totDisc As String = "0.0"
                Dim transaction_id As String = dr.GetDateTime(3).ToString("yyyy-MM-dd hh':'mm':'ss")
                Console.WriteLine("-------------------------------------------------------------")

                Dim tax As Double = Convert.ToDouble(dr.GetValue(5))
                Dim saleQtySum As Double = Convert.ToDouble(dr.GetValue(6))
                receiptNo = "R-" + receiptNo

               

                sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + gross.ToString + SEPARATOR_FIELDS + totval.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + gross.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                Console.WriteLine("SALES RECORD" + sales_record)

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
                        ' Console.WriteLine("invoice text " + invoice_text)
                        response = serverUpload("savebatch.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response ->" + response)
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
                    response = serverUpload("savebatch.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Response ->" + response)
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
        Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://localhost/phoenix_new/public_html/" + serverPath + "/" + subUrl

        ' url = UrlAppend(url, subUrl)
        Console.WriteLine("URL is: " + url)
        'Console.WriteLine(params.Get("salesbatch"))
        Try

            Dim myCache As New CredentialCache()
            myCache.Add(New Uri(url), "Digest", New NetworkCredential(username, password))
            webClient.Credentials = myCache
            Dim responseArray As Byte() = webClient.UploadValues(url, params)
            Dim responseString As String = Encoding.ASCII.GetString(responseArray)
            serverUpload = responseString
            'If subUrl = "savebatch.php" Then
            'Console.WriteLine(responseString)
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
