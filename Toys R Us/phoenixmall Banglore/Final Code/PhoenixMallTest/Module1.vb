Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for Spring Field
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

        'Dim format As String = "yyyyMMddHHmmss"

        Dim response1 As String = serverUpload("lastuploadinfo.php", params)
        Console.WriteLine("response1 ::::  " + response1.ToString)


        Debug("lastuploadinfo=" + response1)
        

        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            Dim querystr As String = "select MAX(RCPT_NUM),MAX(RCPT_DT),MAX(RCPT_TM),SUM(INV_AMT),SUM(TAX_AMT),SUM(RET_AMT),MAX(TRAN_STATUS)  from dbo.TBZ_STOREADSR where  TAX_AMT<>0.0000000000000000 and RCPT_DT >='" + response1 + "' group by RCPT_NUM order by MAX(RCPT_DT)"
            ' Dim querystr As String = "select RECEIPTID,BUSINESSDATE,sum(QTY),sum(DISCAMOUNT),sum(TAXAMOUNT),sum([GROSS AMOUNT]),sum(NETAMOUNT) from ax.SALESVIEW where BUSINESSDATE >= '" + response1 + "'  group by RECEIPTID,BUSINESSDATE order by BUSINESSDATE"
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()
                Dim receiptNo As String = Convert.ToString(dr.GetValue(0))
                Console.WriteLine("receiptNo")
                Dim docDt As String = dr.GetString(1)
                Dim year As String = docDt.Substring(0, 4)
                Dim month As String = docDt.Substring(4, 2)
                Dim day As String = docDt.Substring(6, 2)
                Dim newdate As String = year + "-" + month + "-" + day
                Console.WriteLine("newdate")
                Console.WriteLine(newdate)

                Dim docTime As String = ""
                Dim grossValue As Decimal = Convert.ToDecimal(dr.GetValue(3))
                Console.WriteLine("grossValue")

                Dim tax As Double = Convert.ToDouble(dr.GetValue(4))
                Console.WriteLine("tax")
                Dim totDisc As String = ""
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(3))
                Console.WriteLine("totNetDocValue")
                Dim totRetNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(5))
                Console.WriteLine("totRetNetDocValue")
                Dim Trntype As String = dr.GetString(6)
                Console.WriteLine("Trntype")
                Dim saleQtySum As Double = 0.0
                Console.WriteLine("saleQtySum")
                Dim transaction_id As String = docDt
                Console.WriteLine("transaction_id")

                Console.WriteLine("-------------------------------------------------------------")

                Dim ItemCode As String = ""
                Dim lineItemName As String = ""
                Dim lineQuantity As Double = 0.0
                Dim unitPrice As Decimal = 0.0
                Dim TaxAmt As Double = 0.0
                Dim discountVal As String = ""
                Dim lineTotal As Decimal = 0.0
                Dim discountPct As String = ""

                line_items += ItemCode + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct + SEPARATOR_FIELDS + discountVal + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ITEMFIELDS
                If (Trntype = "SALE") Then
                    tax = tax * -1


                    sales_record = receiptNo + SEPARATOR_FIELDS + newdate + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossValue.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                    Console.WriteLine("SALES RECORD")
                    Console.WriteLine(sales_record)
                Else
                    totRetNetDocValue = totRetNetDocValue * -1
                    sales_record = receiptNo + SEPARATOR_FIELDS + newdate + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + totRetNetDocValue.ToString() + SEPARATOR_FIELDS + totRetNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                    Console.WriteLine("RETURN RECORD")
                    Console.WriteLine(sales_record)
                End If



                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                'Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                inv_count = inv_count + 1
                If (inv_count = NumRecordsPerBatch) Then
                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        params.Add("salesbatch", invoice_text)
                        ' Console.WriteLine("invoice text " + invoice_text)
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
                    Console.WriteLine("Response ->" + response1)
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
        'Dim url As String = serverProtocol + "://192.168.0.18/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'url = UrlAppend(url, subUrl)
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
