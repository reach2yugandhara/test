Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized
Imports System.IO

Module Module1
    'Code for aurelia
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
        Console.WriteLine("in main")
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
        Console.WriteLine("after ini read")
        recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        Dim response As String

        Dim response1 As Date = serverUpload("lastuploadinfo.php", params)
        '2016-06-30 11:59:59
        Console.WriteLine("response:" + response1.ToString)

        Dim salres As String = response1.ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")

        Debug("lastuploadinfo=" + response1)


        ' FOR FETCHING SALES TRANSACTION 

        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()

            ' Dim querystr As String = "select RECEIPTID,CREATEDDATETIME,PAYMENTAMOUNT,DISCAMOUNT from ax.RETAILTRANSACTIONTABLE where CREATEDDATETIME>'" + salres + "' and (RECEIPTID like '62648001I%' or RECEIPTID like '62648001R%') and STORE='62648' "
            'Dim querystr As String = "select RECEIPTID,CREATEDDATETIME,PAYMENTAMOUNT,DISCAMOUNT from ax.RETAILTRANSACTIONTABLE where CREATEDDATETIME>'" + salres + "' and (RECEIPTID like '6264801I%' or RECEIPTID like '6264801R%') and STORE='62648' "
            Dim querystr As String = "select RECEIPTID,CREATEDDATETIME,PAYMENTAMOUNT,DISCAMOUNT from ax.RETAILTRANSACTIONTABLE where CREATEDDATETIME>'" + salres + "' and RECEIPTID like 'W345%' and STORE='W345' "

            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0
            Dim receiptNo As String = ""
            While dr.Read()

                receiptNo = dr.GetString(0).ToString()
                Dim docDt As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                Dim docTime As String = dr.GetDateTime(1).ToString("hh':'mm':'ss")
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(2))
                Dim totDisc As String = Convert.ToString(dr.GetValue(3))
                Dim trnid As Date = dr.GetDateTime(1)
                Dim transaction_id As String = trnid.ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")
                Dim taxamt As Double = 0.0
                Dim totqty As Double = 0.0
                Dim rettax As Double = 0.0
                Dim retqty As Double = 0.0
                Console.WriteLine("-------------------------------------------------------------")

                'Dim query_1 As String = "select ITEMID,TAXAMOUNT,QTY,DISCAMOUNT,PRICE,NETAMOUNTINCLTAX*-1 from ax.RETAILTRANSACTIONSALESTRANS where (RECEIPTID like '6264801I%' or RECEIPTID like '6264801R%') and store='62648' and TRANSACTIONSTATUS=0 and RECEIPTID='" + receiptNo + "'"
                Dim query_1 As String = "select ITEMID,TAXAMOUNT,QTY,DISCAMOUNT,PRICE,NETAMOUNTINCLTAX*-1 from ax.RETAILTRANSACTIONSALESTRANS where RECEIPTID like 'W345%' and store='W345' and TRANSACTIONSTATUS=0 and RECEIPTID='" + receiptNo + "'"
                cmd = New OleDbCommand(query_1, cn)
                dr1 = cmd.ExecuteReader
                Dim ItemCode As String = ""
                While dr1.Read()

                    ItemCode = dr1.GetString(0)
                    Dim Tax1 As Double = Convert.ToDouble(dr1.GetValue(1))
                    Tax1 = Tax1 * -1
                    Dim lineQuantity As Double = Convert.ToDouble(dr1.GetValue(2))
                    lineQuantity = lineQuantity * -1
                    Dim discountVal As String = Convert.ToString(dr1.GetValue(3))
                    Dim unitPrice As Decimal = Convert.ToDecimal(dr1.GetValue(4))
                    Dim lineTotal As Decimal = Convert.ToDecimal(dr1.GetValue(5))

                    If totNetDocValue < 0 Then
                        Tax1 *= -1
                        rettax += Tax1
                        retqty += lineQuantity

                    Else
                        taxamt += Tax1
                        totqty += lineQuantity
                    End If
                    'item level
                    '                    $itemCode,                   $lineItemName,     $lineQuantity,                                     $tax,                                $discountPct,           $discountVal,                              $lineTotal,                      $unitPrice
                    line_items += ItemCode.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + taxamt.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString + SEPARATOR_ITEMFIELDS

                End While


                If ItemCode = "GiftCard" Then
                    line_items = ""
                    sales_record = ""

                Else
                    If totNetDocValue < 0 Then
                        totNetDocValue *= -1
                        sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + rettax.ToString + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + retqty.ToString + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS

                    Else
                        'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                        sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + taxamt.ToString + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + totqty.ToString + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS

                    End If

                    i = (i + 1)
                    Console.WriteLine("invoice count = " + i.ToString)
                    'invoice line

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
                  
                End If
            End If

        Catch ex As Exception
            WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
            'LogError(ex, path)
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
        'Dim url As String = serverProtocol + "://192.168.0.13:8081/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.112/phoenix_new/public_html/" + serverPath + "/" + subUrl

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
