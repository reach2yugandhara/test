Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for Rajdhani
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
    Dim SEPARATOR_ItemFIELDS As String = "<<>>"

    Dim NumRecordsPerBatch As Integer = 0
    Dim isDebug As Boolean = False
    'Dim serverUrl As String
    Dim username As String
    Dim password As String
    Dim last_createdtransaction_id As Integer = 0
    Dim serverProtocol As String
    Dim serverPath As String

    Sub Main()
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


        Dim response As String = serverUpload("lastuploadinfo.php", params)
        ' Dim response = "B00118713"
        Debug("lastuploadinfo=" + response)
        Console.WriteLine("Response:" + response)

        'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
       
        Dim count1 = 0
        Dim transaction_type As String = 1

        Dim taxInfo = ""
        Try
            cn = New OleDbConnection(connectString)
            cn.Open()

            Dim querystr As String = "SELECT BillNo,BillMadeDate,BillMadeTime,billamount,TotalTax,TotalDiscount,Paymode,ID,RoundOffAmount,TotalTipAmount FROM dbo.TblBillHead_RA015 where ID>" + response + " order by ID"
            'Dim querystr As String = "SELECT BillNo,BillMadeDate,BillMadeTime,billamount,TotalTax,TotalDiscount,Paymode,ID,RoundOffAmount,TotalTipAmount FROM dbo.TblBillHead_RA015 where ID =" + response + "  order by ID"
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""

            Dim inv_count As Integer = 0

            While dr.Read()

                Dim receiptNo As String = dr.GetString(0)

                Dim docDt As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                'Dim docDt1 As String = dr.GetDateTime(1).ToString("yyyyMMdd")
                Dim docTime As String = dr.GetString(2) '.ToString("hh':'mm':'ss")
                Dim gross_amount As Decimal = dr.GetDecimal(3) '+ dr.GetDecimal(8)
                Dim totNetDocValue As Decimal = gross_amount
                Dim total_tax As Decimal = dr.GetDecimal(4)
                Dim total_dis As Decimal = dr.GetDecimal(5)
                Dim transaction_id As String = dr.GetValue(7).ToString
                Console.WriteLine("Order format:" + transaction_id)
                Dim PayMode As String = RTrim(LTrim(dr.GetString(6)))
                Dim saleQtySum As Integer = 0

                If PayMode.Equals("P") Then
                    PayMode = "PayParty"
                ElseIf PayMode.Equals("C") Then
                    PayMode = "PayCash"
                ElseIf PayMode.Equals("D") Then
                    PayMode = "PayCreditCard"
                End If




                Dim query_1 As String = "select  a.MenuItemCode ,b.ItemDescription ,a.Quantity , a.CalcDiscount , NetAmount as line_total, a.Rate as unit_price , a.ItemTotalTax from  [dbo].[TblBillDetail_RA015]  a,[dbo].[TblPOSRevenue] b where a.MenuItemCode= b.ItemCode and a.BillNo ='" + receiptNo + "' and  b.BillNo = '" + receiptNo + "'"
                cmd = New OleDbCommand(query_1, cn)
                dr1 = cmd.ExecuteReader


                While dr1.Read()
                    Dim ItemCode As String = dr1.GetString(0)

                    Dim lineItemName As String = dr1.GetString(1)
                    Dim lineQuantity As Decimal = dr1.GetDecimal(2)

                    Dim discountPct As String = ""
                    Dim discountVal As Decimal = dr1.GetDecimal(3)
                    Dim tax As Decimal = dr1.GetDecimal(6)
                    Dim lineTotal As Decimal = dr.GetDecimal(4)
                    Dim unitPrice As Decimal = dr1.GetDecimal(5)

                    line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ItemFIELDS

                    saleQtySum += Convert.ToInt32(lineQuantity)
                    'Console.WriteLine(lineQuantity.ToString)
                End While


                sales_record = receiptNo + "-" + docDt + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + gross_amount.ToString() + SEPARATOR_FIELDS + total_tax.ToString() + SEPARATOR_FIELDS + total_dis.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + taxInfo + SEPARATOR_FIELDS + PayMode + SEPARATOR_FIELDS + transaction_type + SEPARATOR_FIELDS
                Console.WriteLine("sales_record = " + sales_record)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS

                saleQtySum = 0
                line_items = ""
                inv_count = inv_count + 1
                taxInfo = ""
                sales_record = ""
                If (inv_count = NumRecordsPerBatch) Then

                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)

                        params.Add("salesbatch", invoice_text)
                        Console.WriteLine("invoice text " + invoice_text)
                        response = serverUpload("savebatchtext.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response -> " + response)
                        inv_count = 0

                        invoice_text = ""

                    End If
                    'Exit While
                End If




            End While


            If (inv_count > 0) Then
                If invoice_text.Length >= 5 Then
                    invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    params.Add("salesbatch", invoice_text)
                    inv_count = inv_count + 1
                    response = serverUpload("savebatchtext.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Response -> " + response)
                    invoice_text = ""
                End If
            End If
        Catch ex As Exception
            WriteToEventLog(ex.ToString)
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
            'Console.WriteLine("responseString    " + responseString)
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
