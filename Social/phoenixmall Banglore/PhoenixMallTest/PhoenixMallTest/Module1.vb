Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized
Imports MySql.Data.MySqlClient
Module Module1

    'Dim con As MySqlConnection
    Dim cn As MySqlConnection = Nothing
    Dim cn1 As MySqlConnection = Nothing
    Dim cn2 As MySqlConnection = Nothing
    Dim cn3 As MySqlConnection = Nothing
    Dim cmd As MySqlCommand = Nothing
    Dim cmd1 As MySqlCommand = Nothing
    Dim cmd2 As MySqlCommand = Nothing
    Dim cmd3 As MySqlCommand = Nothing
	Dim dr As MySqlDataReader = Nothing
    Dim dr1 As MySqlDataReader = Nothing
    Dim dr2 As MySqlDataReader = Nothing
    Dim dr3 As MySqlDataReader = Nothing

    'Dim cmd2 As OleDbCommand
    'Dim cmd3 As OleDbCommand
    Dim line1 As OleDbDataReader = Nothing
    'Dim dr2 As OleDbDataReader = Nothing
    'Dim dr3 As OleDbDataReader = Nothing
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

        'Dim connString As String = iniProps.Get("DBConnectString")
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

      

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        Dim response1 As String = serverUpload("lastuploadinfo.php", params)
        'Dim response As String = response1.ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")
        Dim response_num As String = response1
        'response_num = response_num.Trim()
        'Dim response_date As String = response1.Split("|")(1)
        'response_date = response_date.Trim()
        'Console.WriteLine("response1 ::::  " + response1)
        Debug("lastuploadinfo=" + response1)

        Console.WriteLine("start @" + response1.ToString)

        '' FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0
        Try
            cn = New MySqlConnection()
            cn1 = New MySqlConnection()
            cn2 = New MySqlConnection()
            cn3 = New MySqlConnection()
            'Console.WriteLine(cn)
            cn.ConnectionString = connectString.ToString
            cn1.ConnectionString = connectString.ToString
            cn2.ConnectionString = connectString.ToString
            cn3.ConnectionString = connectString.ToString
            'Console.WriteLine(cn.ConnectionString)
            cn.Open()
            'select POS_SBILL_NO,BILL_DATE,GROSS_AMT,Bill_status,(select sum(pid.comp_amt) from pos_item_det pid where pid.pos_sbill_no = '138079'  and comp_code not in ('basic','ISRVCHRG')) as Tax from  pos_tran_mast ptm where POS_SBILL_NO > '138079' order by POS_SBILL_NO
            'Dim querystr As String = "select POS_SBILL_NO,BILL_DATE,GROSS_AMT,Bill_status from  pos_tran_mast where POS_SBILL_NO > '" + response_num.ToString + "' order by POS_SBILL_NO"
            Dim querystr As String = "select POS_SBILL_NO,BILL_DATE,GROSS_AMT from pos_tran_mast where POS_SBILL_NO > '" + response_num.ToString + "' and Bill_status='SETTELED' order by pos_sbill_no;"
            Console.WriteLine(querystr)
            cmd = New MySqlCommand(querystr, cn)
            dr = cmd.ExecuteReader
            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0
            While dr.Read()
                'Dim billNo As Integer = dr.GetValue(0)
                Dim receiptNo As String = Convert.ToString(dr.GetValue(0))
                Dim docDt As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")

                Dim grossValue As Double
                If dr.GetValue(2) Is DBNull.Value Then
                    grossValue = 0
                Else
                    grossValue = Convert.ToDouble(dr.GetValue(2))
                End If
                Dim totNetDocValue As Double = grossValue

                Dim tax_query = "select sum(comp_amt) from pos_tran_details where pos_sbill_no = '" + receiptNo.ToString + "' and comp_code  in ('GTC0000022','GTC0000023','ISRVCHRG')"
                ''SERVICETAX'
                cn2.Open()
                cmd2 = New MySqlCommand(tax_query, cn2)
                dr2 = cmd2.ExecuteReader

                Dim tax As Double = 0.0
                While dr2.Read()
                    If dr2.GetValue(0) Is DBNull.Value Then
                        tax = 0.0
                    Else
                        tax = dr2.GetValue(0)
                    End If
                End While
                cn2.Close()

                Dim disc_qiery = "select sum(comp_amt) from pos_tran_details where pos_sbill_no = '" + receiptNo.ToString + "' and comp_code  in ('DISCOUNTP')"
                cn3.Open()
                cmd3 = New MySqlCommand(disc_qiery, cn3)
                dr3 = cmd3.ExecuteReader
                Dim totDisc As Double = 0.0
                While dr3.Read()
                    If dr3.GetValue(0) Is DBNull.Value Then
                        totDisc = 0.0
                    Else
                        totDisc = dr3.GetValue(0)
                    End If
                End While
                cn3.Close()

                Dim saleQtySum As Double = 0.0
                'Dim transaction_id As String = receiptNo + "|" + docDt
                Dim transaction_id As String = receiptNo

                Console.WriteLine(" >>> " + receiptNo.ToString + "<>" + docDt.ToString)
                'select POS_SBILL_NO,ORDER_ITEM_NUMBER,ITEM_CODE,ITEM_NAME,QTY,RATE,NET_AMT from pos_item_mast where POS_SBILL_NO = 
                cn1.Open()
                Dim query_1 As String = "select ITEM_CODE,ITEM_NAME,QTY,RATE,NET_AMT from pos_item_mast where POS_SBILL_NO = '" + receiptNo.ToString + "'"
                'Console.WriteLine(query_1)
                cmd1 = New MySqlCommand(query_1, cn1)

                dr1 = cmd1.ExecuteReader
                Dim lineQuantity As Double = 0.0
                Dim cal_lineQuantity As Double = 0.0
                Dim lineTotal As Double = 0.0
                Dim cal_lineTotal As Double = 0.0
                Dim unitPrice As Double = 0.0
                Dim cal_unitPrice As Double = 0.0
                While dr1.Read()
                    Dim ItemCode As String = dr1.GetString(0)
                    Dim lineItemName As String = dr1.GetString(1)

                    cal_lineQuantity = dr1.GetValue(2)
                    lineQuantity = lineQuantity + cal_lineQuantity

                    cal_lineTotal = dr1.GetValue(4)
                    lineTotal = cal_lineTotal + lineTotal

                    cal_unitPrice = dr1.GetValue(3)
                    unitPrice = unitPrice + cal_unitPrice

                    'No return found.
                    'Item(level)
                    line_items += ItemCode + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ITEMFIELDS
                End While
                cn1.Close()

                'tax = grossValue - lineTotal
                saleQtySum = lineQuantity

                sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                Console.WriteLine("sales Invoice")


                i = i + 1
                saleQtySum = 0.0
                tax = 0.0
                totDisc = 0.0
                'Invoice Text
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                inv_count = inv_count + 1
                Console.WriteLine("invoice count = " + i.ToString)
                If (inv_count = NumRecordsPerBatch) Then
                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        params.Add("salesbatch", invoice_text)
                        'Console.WriteLine("invoice text " + invoice_text)
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
            cn.Close()
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
        'serverProtocol = 
        Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.13:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl

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
