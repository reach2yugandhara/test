Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'code for Wrogan

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

    'Dim drStock As OleDbDataReader = Nothing
    'Dim cmdStock As OleDbCommand = Nothing


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
    Dim V_line_items As String = ""
    Dim V_sales_record As String = ""
    Dim V_invoice_text As String = ""

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
            Console.WriteLine(password)

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

        recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")
        Dim lastDocNo As String

        Dim response As String = serverUpload("lastuploadinfo.php", params)
        '12457,295

        lastDocNo = response

        Dim bill_nos() As String = lastDocNo.Split(",")
        Console.WriteLine("Last doc no : " + lastDocNo)

        Dim saleLastNo As Integer = Integer.Parse(bill_nos(0))
        Dim returnLastNo As Integer = Integer.Parse(bill_nos(1))




        'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Try

            cn = New OleDbConnection(connectString)
            cn.Open()
            Dim querystr As String = "SELECT s.DocNoPrefix,s.DocNo,s.DocDt,s.TotDocValue,s.TotDocTax,s.NetDocValue,s.TotDocDisc,s.TotalLineItems,s.TrnType,s.TrnCtrlNo, s.DocVoidInd,s.FrwdRefCtrlNo,FrwdRefTrnType FROM StkTrnHdr s where s.TrnCtrlNo > " + saleLastNo.ToString() + " and DocNoPrefix like 'S%' order by s.TrnCtrlNo"

            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader


            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim sales_record As String = ""

            While dr.Read()

                Dim docNoPrefix As String = dr.GetString(0)
                Dim docNo As Integer = dr.GetInt32(1)
                Dim docDt As System.DateTime = dr.GetDateTime(2)
                Dim totDocVal As Decimal = dr.GetDecimal(3)
                Dim totDocTax As Decimal = dr.GetDecimal(4)
                Dim totNetDocValue As Decimal = dr.GetDecimal(5)
                Dim totDocDisc As Decimal = dr.GetDecimal(6)
                Dim totLineItems As Integer = dr.GetInt32(7)
                Dim trnType As Integer = dr.GetInt32(8)
                Dim trnCtrlNo As Integer = dr.GetInt32(9)
                Dim trnDocVoidInd As Integer = dr.GetInt32(10)
                Dim FrwdRefCtrlNo As Integer = dr.GetInt32(11)
                Dim receiptNo As String = docNoPrefix + "-" + docNo.ToString()
                Dim gross_amount As Decimal = totNetDocValue - (totDocTax)

                Dim inv_qty As Integer = 0

                Dim query_1 As String = "SELECT s.StockNo,s.DocQty,s.StkUpdtRate,s.DocEntValue,s.DocEntTotDisc,s.DocEntValAftDisc,s.DocEntTax,s.DocEntNetValue,s.DocEntRate from StkTrnDtls s where s.DocNo = " + docNo.ToString() + " and s.DocNoPrefix = '" + docNoPrefix + "'"

                cmd = New OleDbCommand(query_1, cn)
                dr1 = cmd.ExecuteReader
                While dr1.Read()

                    Dim stockNo As String = dr1.GetString(0)
                    Dim docQty As Integer = Convert.ToInt32(dr1.GetFloat(1))
                    Dim stkUpdateRate As Decimal = dr1.GetDecimal(2)
                    Dim docEntValue As Decimal = dr1.GetDecimal(3)
                    Dim docEntTotDisc As Decimal = dr1.GetDecimal(4)
                    Dim docEntValAftDisc As Decimal = dr1.GetDecimal(5)
                    Dim docEntTax As Decimal = dr1.GetDecimal(6)
                    Dim docEntNetValue As Decimal = dr1.GetDecimal(7)
                    Dim docEntRate As Decimal = dr1.GetDecimal(8)



                    inv_qty = inv_qty + docQty

                    Dim itemName As String = ""
                    Dim query_2 As String = "SELECT ItemDesc from ItemMaster where StockNo = '" + stockNo + "'"

                    cmd = New OleDbCommand(query_2, cn)
                    dr2 = cmd.ExecuteReader

                    While dr2.Read()
                        itemName = dr2.GetString(0)
                    End While

                    line_items = line_items + stockNo + SEPARATOR_FIELDS + itemName + SEPARATOR_FIELDS + docQty.ToString() + SEPARATOR_FIELDS + docEntTax.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docEntTotDisc.ToString() + SEPARATOR_FIELDS + docEntNetValue.ToString() + SEPARATOR_FIELDS + docEntRate.ToString() + SEPARATOR_ITEMFIELDS

                End While


                sales_record = receiptNo + SEPARATOR_FIELDS + docDt.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + gross_amount.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + totDocTax.ToString() + SEPARATOR_FIELDS + totDocDisc.ToString() + SEPARATOR_FIELDS + inv_qty.ToString() + SEPARATOR_FIELDS + trnCtrlNo.ToString() + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS


                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS


                sales_record = ""
                line_items = ""
                count = count + 1
                Console.WriteLine("Count " + count.ToString())
                If (count = NumRecordsPerBatch) Then
                    'Console.WriteLine(invoice_text)
                    params.Add("salesbatch", invoice_text)
                    sales_send_count = sales_send_count + 1
                    response = serverUpload("savebatch.php", params)
                    Console.WriteLine("SALES Response " + response)
                    params.Remove("salesbatch")
                    invoice_text = ""
                    line_items = ""
                    count = 0

                End If
            End While

            If (count > 0) Then
                invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                'Console.WriteLine(invoice_text)
                params.Add("salesbatch", invoice_text)
                sales_send_count = sales_send_count + 1
                response = serverUpload("savebatch.php", params)
                Console.WriteLine("LAST SALES Response " + response)
                params.Remove("salesbatch")
                invoice_text = ""
                line_items = ""
                count = 0
            End If

        Catch ex As Exception
            WriteToEventLog(ex.ToString)
        End Try

        sales_send_count = 0

        count = 0

        '------------------------------------Return------------------------------------------------
        Dim retcount As Integer = 0
        Try

            cn = New OleDbConnection(connectString)
            cn.Open()
            Dim querystr As String = "SELECT s.DocNoPrefix,s.DocNo,s.DocDt,s.TotDocValue,s.TotDocTax,s.NetDocValue,s.TotDocDisc,s.TotalLineItems,s.TrnType,s.TrnCtrlNo, s.DocVoidInd,s.FrwdRefCtrlNo,FrwdRefTrnType FROM StkTrnHdr s where s.TrnCtrlNo > " + returnLastNo.ToString() + " and (DocNoPrefix like 'R%' or DocNoPrefix like 'V%') order by s.TrnCtrlNo"

            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader


            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim ret_record As String = ""

            While dr.Read()

                Dim docNoPrefix As String = dr.GetString(0)
                Dim docNo As Integer = dr.GetInt32(1)
                Dim docDt As System.DateTime = dr.GetDateTime(2)
                Dim totDocVal As Decimal = dr.GetDecimal(3)
                Dim totDocTax As Decimal = dr.GetDecimal(4)
                totDocTax = totDocTax * -1
                Dim totNetDocValue As Decimal = dr.GetDecimal(5)
                Dim totDocDisc As Decimal = dr.GetDecimal(6)
                totDocDisc = totDocDisc * -1
                Dim totLineItems As Integer = dr.GetInt32(7)
                Dim trnType As Integer = dr.GetInt32(8)
                Dim trnCtrlNo As Integer = dr.GetInt32(9)
                Dim trnDocVoidInd As Integer = dr.GetInt32(10)
                Dim FrwdRefCtrlNo As Integer = dr.GetInt32(11)
                Dim receiptNo As String = docNoPrefix + "-" + docNo.ToString()
                Dim gross_amount As Decimal = totNetDocValue - (totDocTax)

                Dim inv_qty As Integer = 0

                Dim query_1 As String = "SELECT s.StockNo,s.DocQty,s.StkUpdtRate,s.DocEntValue,s.DocEntTotDisc,s.DocEntValAftDisc,s.DocEntTax,s.DocEntNetValue,s.DocEntRate from StkTrnDtls s where s.DocNo = " + docNo.ToString() + " and s.DocNoPrefix = '" + docNoPrefix + "'"

                cmd = New OleDbCommand(query_1, cn)
                dr1 = cmd.ExecuteReader
                While dr1.Read()

                    Dim stockNo As String = dr1.GetString(0)
                    Dim docQty As Integer = Convert.ToInt32(dr1.GetFloat(1))
                    Dim stkUpdateRate As Decimal = dr1.GetDecimal(2)
                    Dim docEntValue As Decimal = dr1.GetDecimal(3)
                    Dim docEntTotDisc As Decimal = dr1.GetDecimal(4)
                    Dim docEntValAftDisc As Decimal = dr1.GetDecimal(5)
                    Dim docEntTax As Decimal = dr1.GetDecimal(6)
                    Dim docEntNetValue As Decimal = dr1.GetDecimal(7)
                    Dim docEntRate As Decimal = dr1.GetDecimal(8)



                    inv_qty = inv_qty + docQty

                    Dim itemName As String = ""
                    Dim query_2 As String = "SELECT ItemDesc from ItemMaster where StockNo = '" + stockNo + "'"

                    cmd = New OleDbCommand(query_2, cn)
                    dr2 = cmd.ExecuteReader

                    While dr2.Read()
                        itemName = dr2.GetString(0)
                    End While

                    line_items = line_items + stockNo + SEPARATOR_FIELDS + itemName + SEPARATOR_FIELDS + docQty.ToString() + SEPARATOR_FIELDS + docEntTax.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docEntTotDisc.ToString() + SEPARATOR_FIELDS + docEntNetValue.ToString() + SEPARATOR_FIELDS + docEntRate.ToString() + SEPARATOR_ITEMFIELDS

                End While


                ret_record = receiptNo + SEPARATOR_FIELDS + docDt.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + gross_amount.ToString() + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + totDocTax.ToString() + SEPARATOR_FIELDS + totDocDisc.ToString() + SEPARATOR_FIELDS + inv_qty.ToString() + SEPARATOR_FIELDS + trnCtrlNo.ToString() + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS


                invoice_text += ret_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS


                ret_record = ""
                line_items = ""
                retcount = retcount + 1
                Console.WriteLine("Count " + retcount.ToString())
                If (retcount = NumRecordsPerBatch) Then
                    'Console.WriteLine(invoice_text)
                    params.Add("salesbatch", invoice_text)
                    sales_send_count = sales_send_count + 1
                    response = serverUpload("savebatch.php", params)
                    Console.WriteLine("SALES Response " + response)
                    params.Remove("salesbatch")
                    invoice_text = ""
                    line_items = ""
                    retcount = 0

                End If
            End While

            If (retcount > 0) Then
                invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                'Console.WriteLine(invoice_text)
                params.Add("salesbatch", invoice_text)
                sales_send_count = sales_send_count + 1
                response = serverUpload("savebatch.php", params)
                Console.WriteLine("LAST SALES Response " + response)
                params.Remove("salesbatch")
                invoice_text = ""
                line_items = ""
                retcount = 0
            End If

        Catch ex As Exception
            WriteToEventLog(ex.ToString)
        End Try

        sales_send_count = 0

        retcount = 0





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


    Function GetTime(ByVal dt As DateTime) As String
        Return dt.TimeOfDay.ToString
    End Function

    Private Function serverUpload(ByVal subUrl As String, ByVal params As NameValueCollection)
        Dim webClient As New WebClient()
        Console.WriteLine("inside server upload ")
        'serverProtocol = 
        Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl

        'Dim url As String = serverProtocol + "://192.168.0.16/phoenix_new/public_html/" + serverPath + "/" + subUrl
        Console.WriteLine("after url ")
        Console.WriteLine(url.ToString)
        'url = UrlAppend(url, subUrl)
        'Console.WriteLine("URL is ******* " + url)
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
