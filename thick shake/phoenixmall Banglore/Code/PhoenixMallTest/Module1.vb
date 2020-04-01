Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'code for thick shake

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


        Dim response As String = serverUpload("lastuploadinfo.php", params)
        '2018-08-31 23:31:34.280
        Console.WriteLine("response : " + response.ToString)
        
        'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Try

            cn = New OleDbConnection(connectString)
            cn.Open()
            Dim querystr As String = "select Invoice_Number,DateTime,Grand_Total,(Total_Tax8+Total_Tax9) as tax,disc_amount from dbo.Invoice_Hdr where DateTime>'" + response + "' order by DateTime"

            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim sales_record As String = ""
            Dim saletype As String = ""
            Dim invno As String = ""
            Dim docDt As String = ""
            Dim docdate As String = ""
            Dim docTime As String = ""
            Dim docnet As Double
            Dim docqty As Double
            Dim docgross As Double
            Dim docdisc As Double
            Dim doctax As Double
            Dim trnid As String
            ' Dim sArr() As String
            While dr.Read()
                invno = dr.GetValue(0).ToString
                docdate = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                docTime = dr.GetDateTime(1).ToString("hh':'mm':'ss")
                docnet = Convert.ToDouble(dr.GetValue(2))
                doctax = Convert.ToDouble(dr.GetValue(3))
                docdisc = Convert.ToDouble(dr.GetValue(4))
                ' docqty = Convert.ToDouble(dr.GetValue(5))
                docgross = docnet - doctax
                trnid = dr.GetValue(1).ToString

                Dim itemid As String = ""
                Dim itemname As String = ""
                Dim itemcode As String = ""
                Dim itemqty As String = ""
                Dim itemprice As String = ""
                Dim itemnet As String = ""
                Dim itemdisc As String = ""
                Dim itemtax As String = ""

                Dim query_2 As String = "select ProdNum,Quantity,PricePer,(Tax8Per+Tax9Per) as tax  from dbo.Invoice_Dtl where Invoice_Number='" + invno + "'"

                cmd = New OleDbCommand(query_2, cn)
                dr2 = cmd.ExecuteReader

                While dr2.Read()
                    itemid = dr2.GetValue(0).ToString
                    itemqty = dr2.GetValue(1).ToString
                    itemnet = dr2.GetValue(2).ToString
                    itemtax = dr2.GetValue(3).ToString
                    itemcode = dr2.GetValue(0).ToString
                    itemprice = dr2.GetValue(2).ToString
                    docqty += itemprice
                    line_items = line_items + itemcode.ToString + SEPARATOR_FIELDS + itemname.ToString + SEPARATOR_FIELDS + itemqty.ToString + SEPARATOR_FIELDS + itemtax.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + itemdisc.ToString() + SEPARATOR_FIELDS + itemnet.ToString() + SEPARATOR_FIELDS + itemprice + SEPARATOR_ITEMFIELDS

                End While



                '              $receiptNo,                         $billDate,                       $billTime,                      $grossAmount,                            $totalAmount,                           $vatAmount,                            $disountVal,                             $qty,                                $transaction_id,                                                   $tax_lines_info,    $payment_lines_info
                sales_record = invno.ToString + SEPARATOR_FIELDS + docdate.ToString + SEPARATOR_FIELDS + docTime.ToString + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + docnet.ToString() + SEPARATOR_FIELDS + doctax.ToString() + SEPARATOR_FIELDS + docdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + trnid.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS


                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS
                docqty = 0.0

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
        'Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl

        Dim url As String = serverProtocol + "://192.168.0.135/phoenix_new/public_html/" + serverPath + "/" + subUrl
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
