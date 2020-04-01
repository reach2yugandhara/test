Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized
Imports System.Data.SqlClient
Imports System.IO

Module Module1

    'Code for chambor
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

        Dim response As String = serverUpload("lastuploadinfo.php", params)
        'S-242
        '{^07-01-2017} wrong order format
        'new order format {^2017/04/30}
        'new order format {^2017/04/30 23:59:59 PM}
        Debug("lastuploadinfo=" + response)

        Dim count As Integer = 0

        Try

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            Dim docno As String = ""
            Dim docdate As String = ""
            Dim doctime As String = ""
            Dim ItemCode As String = ""
            Dim lineItemName As String = ""
            Dim tot_lineQuantity As Double = 0.0
            Dim tot_mrp As Double = 0.0
            Dim tot_tax As Double = 0.0
            Dim tot_disc As Double = 0.0
            Dim tot_net As Double = 0.0
            Dim transaction_id As String = ""
            Dim prev_bill_no As String = ""
            Dim SalesRecordList As ArrayList = New ArrayList()

            Dim dBaseConnection As New System.Data.OleDb.OleDbConnection(connectString)
            dBaseConnection.Open()

            'Dim dBaseCommand As New System.Data.OleDb.OleDbCommand("SELECT RECEIPT_NO,TIMESTAMP,PMT_NAME FROM pmtseg where TIMESTAMP >'" + response + "' order by TIMESTAMP", dBaseConnection)


            ' Dim dBaseCommand1 As New System.Data.OleDb.OleDbCommand("SELECT RECEIPT_NO,TIMESTAMP,ITEM_CODE,ITEM_NAME,ITEM_QTY,ITEM_PRICE,TAX_AMT,DISC_AMT,((ITEM_PRICE * ITEM_QTY)-DISC_AMT) as inv_amt FROM itmseg where TIMESTAMP >'" + response + "' order by TIMESTAMP", dBaseConnection)
            Dim dBaseCommand1 As New System.Data.OleDb.OleDbCommand("SELECT RECEIPT_NO,TIMESTAMP,ITEM_CODE,ITEM_NAME,ITEM_QTY,ITEM_PRICE,TAX_AMT,DISC_AMT,((ITEM_PRICE * ITEM_QTY)-DISC_AMT) as inv_amt FROM itmseg where  timestamp > " + response + " order by TIMESTAMP", dBaseConnection)
            Dim dBaseDataReader1 As System.Data.OleDb.OleDbDataReader = dBaseCommand1.ExecuteReader(CommandBehavior.Default)
            While dBaseDataReader1.Read
                Dim rec_tax As Double = 0.0
                Dim rec_net As Double = 0.0
                Dim rec_docno As String = dBaseDataReader1(0).ToString
                Dim docdt As DateTime = dBaseDataReader1(1)
                Dim rec_docdate As String = Format(docdt, "yyyy-MM-dd hh:mm:ss.ttt")
                Dim trndate As String = Format(docdt, "yyyy/MM/dd hh:mm:ss ttt")

                Dim rec_doctime As String = Format(docdt, "hh:mm:ss ttt")
                Dim rec_ItemCode As String = (dBaseDataReader1(2).ToString)
                Dim rec_lineItemName As String = (dBaseDataReader1(3).ToString)
                Dim rec_lineQuantity As Double = Convert.ToDouble(dBaseDataReader1(4))
                Dim rec_mrp As Double = Convert.ToDouble(dBaseDataReader1(5))
                If rec_lineQuantity < 0 Then
                    rec_tax = -(Convert.ToDouble(dBaseDataReader1(6)))

                Else
                    rec_tax = Convert.ToDouble(dBaseDataReader1(6))

                End If
                rec_net = Convert.ToDouble(dBaseDataReader1(8))
                Dim rec_disc As Double = Convert.ToDouble(dBaseDataReader1(7))

                '{^2017/04/30 23:59:59 PM}
                transaction_id = "{" + "^" + trndate + "}"

                If prev_bill_no = "" Then
                    prev_bill_no = rec_docno
                    line_items += rec_ItemCode + SEPARATOR_FIELDS + rec_lineItemName + SEPARATOR_FIELDS + rec_lineQuantity.ToString + SEPARATOR_FIELDS + rec_tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + rec_net.ToString + SEPARATOR_FIELDS + rec_mrp.ToString + SEPARATOR_ITEMFIELDS

                    SalesRecordList.Add(rec_docno)
                    SalesRecordList.Add(rec_docdate)
                    SalesRecordList.Add(rec_doctime)
                    SalesRecordList.Add(rec_lineQuantity)
                    SalesRecordList.Add(rec_mrp)
                    SalesRecordList.Add(rec_tax)
                    SalesRecordList.Add(rec_disc)
                    SalesRecordList.Add(rec_net)
                    SalesRecordList.Add(transaction_id)

                ElseIf rec_docno = prev_bill_no Then

                    line_items += rec_ItemCode + SEPARATOR_FIELDS + rec_lineItemName + SEPARATOR_FIELDS + rec_lineQuantity.ToString + SEPARATOR_FIELDS + rec_tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + rec_net.ToString + SEPARATOR_FIELDS + rec_mrp.ToString + SEPARATOR_ITEMFIELDS

                    SalesRecordList.RemoveAt(0)
                    SalesRecordList.Insert(0, rec_docno)

                    SalesRecordList.RemoveAt(1)
                    SalesRecordList.Insert(1, rec_docdate)

                    SalesRecordList.RemoveAt(2)
                    SalesRecordList.Insert(2, rec_doctime)

                    tot_lineQuantity = Convert.ToDouble(SalesRecordList.Item(3)) + rec_lineQuantity
                    SalesRecordList.RemoveAt(3)
                    SalesRecordList.Insert(3, tot_lineQuantity)

                    tot_mrp = Convert.ToDouble(SalesRecordList.Item(4)) + rec_mrp
                    SalesRecordList.RemoveAt(4)
                    SalesRecordList.Insert(4, tot_mrp)

                    tot_tax = Convert.ToDouble(SalesRecordList.Item(5)) + rec_tax
                    SalesRecordList.RemoveAt(5)
                    SalesRecordList.Insert(5, tot_tax)

                    tot_disc = Convert.ToDouble(SalesRecordList.Item(6)) + rec_disc
                    SalesRecordList.RemoveAt(6)
                    SalesRecordList.Insert(6, tot_disc)

                    tot_net = Convert.ToDouble(SalesRecordList.Item(7)) + rec_net
                    SalesRecordList.RemoveAt(7)
                    SalesRecordList.Insert(7, tot_net)

                    SalesRecordList.RemoveAt(8)
                    SalesRecordList.Insert(8, transaction_id)

                ElseIf prev_bill_no <> "" And prev_bill_no <> rec_docno Then
                    If (Convert.ToInt32(SalesRecordList.Item(3)) >= 0) Then
                        sales_record = SalesRecordList.Item(0).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(1).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(2).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(4).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(7).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(5).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(6).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(3).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(8).ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                        count = count + 1
                    ElseIf (Convert.ToInt32(SalesRecordList.Item(3)) < 0) Then
                        sales_record = SalesRecordList.Item(0).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(1).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(2).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(4).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(7).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(5).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(6).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(3).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(8).ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                        count = count + 1
                    End If

                    invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS

                    prev_bill_no = rec_docno
                    line_items = ""
                    sales_record = ""

                    tot_disc = 0.0
                    tot_lineQuantity = 0.0
                    tot_mrp = 0.0
                    tot_net = 0.0
                    tot_tax = 0.0

                    line_items += rec_ItemCode + SEPARATOR_FIELDS + rec_lineItemName + SEPARATOR_FIELDS + rec_lineQuantity.ToString + SEPARATOR_FIELDS + rec_tax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + rec_net.ToString + SEPARATOR_FIELDS + rec_mrp.ToString + SEPARATOR_ITEMFIELDS
                    SalesRecordList.Clear()


                    SalesRecordList.Add(rec_docno)
                    SalesRecordList.Add(rec_docdate)
                    SalesRecordList.Add(rec_doctime)
                    SalesRecordList.Add(rec_lineQuantity)
                    SalesRecordList.Add(rec_mrp)
                    SalesRecordList.Add(rec_tax)
                    SalesRecordList.Add(rec_disc)
                    SalesRecordList.Add(rec_net)
                    SalesRecordList.Add(transaction_id)

                Else
                    Console.WriteLine("There is some discrepancy in the conditions ")
                End If



                If (count = NumRecordsPerBatch) Then

                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)

                        params.Add("salesbatch", invoice_text)
                        ' Console.WriteLine("invoice text " + invoice_text)
                        response = serverUpload("savebatch.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response ->" + response)
                        count = 0

                        invoice_text = ""
                        sales_record = ""
                    End If

                End If


            End While


            If dBaseDataReader1.HasRows Then
                If (Convert.ToInt32(SalesRecordList.Item(3)) >= 0) Then
                    sales_record = SalesRecordList.Item(0).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(1).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(2).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(4).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(7).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(5).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(6).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(3).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(8).ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                    count = count + 1
                ElseIf (Convert.ToInt32(SalesRecordList.Item(3)) < 0) Then
                    sales_record = SalesRecordList.Item(0).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(1).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(2).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(4).ToString + SEPARATOR_FIELDS + SalesRecordList.Item(7).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(5).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(6).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(3).ToString() + SEPARATOR_FIELDS + SalesRecordList.Item(8).ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                    count = count + 1
                End If

                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                SalesRecordList.RemoveAt(0)
                count = count + 1

            End If

            If (count > 0) Then
                If invoice_text.Length >= 5 Then
                    invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    params.Add("salesbatch", invoice_text)
                   
                    response = serverUpload("savebatch.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Response ->" + response)
                    invoice_text = ""
                End If
            End If


            dBaseConnection.Close()

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
