Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for accessarize
    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing
    Dim dr As OleDbDataReader = Nothing
    Dim cmd2 As OleDbCommand = Nothing
    Dim cmd3 As OleDbCommand
    Dim dr1 As OleDbDataReader = Nothing
    Dim line1 As OleDbDataReader = Nothing
    Dim dr2 As OleDbDataReader = Nothing
    Dim dr3 As OleDbDataReader = Nothing
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

        Dim recordTypeToFetch As String = iniProps.Get("RecordTypeToFetch")
        If (IsEmpty(recordTypeToFetch)) Then
            WriteToEventLog("INI ERROR:Missing RecordTypeToFetch")
            Return
        End If


        Dim java_path As String = iniProps.Get("java_path")
        If (IsEmpty(java_path)) Then
            WriteToEventLog("INI ERROR:Missing java.exe path")
            Return
        End If

        Dim loadinfo_path As String = iniProps.Get("loadinfo_file_path")
        If (IsEmpty(loadinfo_path)) Then
            WriteToEventLog("INI ERROR:Missing loadinfo file path")
            Return
        End If

        Dim upload_path As String = iniProps.Get("upload_file_path")
        If (IsEmpty(upload_path)) Then
            WriteToEventLog("INI ERROR:Missing upload data path")
            Return
        End If
        ''-----------------------------------------------------------------------------------------------------
        'recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        'Dim response1 As String = serverUpload("lastuploadinfo.php", params)


        'Dim sal_response As String = response1.Split(",")(0)
        'Dim ret_response As String = response1.Split(",")(1)
        'sal_response = sal_response.Trim()
        'ret_response = ret_response.Trim()

        'Console.WriteLine("sales order format : " + sal_response)
        'Console.WriteLine("return order format  " + ret_response)
        Dim outout As String = ""
        Dim myProcess As System.Diagnostics.Process
        myProcess = New System.Diagnostics.Process()
        myProcess.StartInfo.FileName = java_path
        'myProcess.StartInfo.Arguments = "E:\dev\batch2\Acceserize\AccessoriesStore\dist\AccessoriesStore.jar"
        myProcess.StartInfo.Arguments = "-jar " + loadinfo_path
        myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
        myProcess.StartInfo.UseShellExecute = False
        myProcess.StartInfo.RedirectStandardOutput = True
        myProcess.Start()
        Dim out As String = myProcess.StandardOutput.ReadToEnd()

        myProcess.WaitForExit()
        Console.WriteLine(out)
        Dim response1 As String = out
        Dim sal_response As String = response1.Split(",")(0)
        sal_response = sal_response.Trim()
        Dim ret_response As String = response1.Split(",")(1)
        ret_response = ret_response.Trim()


        Debug("lastuploadinfo=" + response1)


        '' FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            Dim querystr As String = "SELECT POSTILL,SHIFTNO,RECPT_NO,RECEIPTNO,CREATEDATE,CDATE,CONVERT(DECIMAL(15,2),(ISNULL((SELECT SUM(TAX_AMT) AS TAX FROM TXN_TAX WHERE TMP.RECPT_NO=TRANSNUM AND TMP.POSTILL =STORENUM),0))) AS TAXAMT,  INVAMT,DISCAMT,NETAMT, 0 AS RETAMT,'' AS   CUSTOMERNAME,'' AS GENDER,'' AS PASSPORTNUMBER,  '' AS NATIONCODE, '' AS CONNECTINGFLIGHT, '' AS FLIGHTNUMBER, '' AS  TICKETNUMBER, '' AS PAXTYPE,'SALES' AS TRANSACTION_STATUS  FROM  (    SELECT ISNULL(TM.STORENUM,1607) AS POSTILL,'1' AS SHIFTNO,TM.TRANSNUM AS RECPT_NO, MT.ADDINFO AS RECEIPTNO,isnull(TT.Createdate,TM.ITEMDATETIME)  AS CREATEDATE,         CONVERT(DATETIME,CONVERT(NVARCHAR(20), TM.ITEMDATETIME,106)) AS CDATE,   CONVERT(DECIMAL(15,2),SUM(TENDERAMT - CHANGEAMT))  AS NETAMT, CONVERT(DECIMAL(15,2) ,isnull(TT.DISC,0)) AS DISCAMT,CONVERT(DECIMAL(15,2),SUM(TENDERAMT - CHANGEAMT))  AS INVAMT FROM TXN_METHOD_OF_PAYMENT TM (nolock)   inner JOIN ( SELECT TRANSNUM,STORENUM,ADDINFO FROM  TXN_MISCELLANEOUS_TRANS (nolock) WHERE ADDINFO <>'' AND ADDINFO<>'CANCEL' AND ADDINFO<>'CANCELLED' GROUP BY TRANSNUM,STORENUM,ADDINFO  )MT ON TM.TRANSNUM=MT.TRANSNUM  AND TM.STORENUM=MT.STORENUM    inner join (select SUM(ISNULL(TOTALSAVED,0) ) as DISC,  ITEMDATETIME   as Createdate,TRANSNUM from TXN_TRANSACTION_TOTAL(nolock)  group by itemdatetime,TRANSNUM) TT ON TT.TRANSNUM=TM.TRANSNUM  WHERE (TM.TXNVOIDMOD =0) AND TM.TENDERAMT >0 AND    (convert(datetime,convert(nvarchar,TM.ITEMDATETIME,121)) > '" + sal_response + "' )     GROUP BY TM.STORENUM ,MT.ADDINFO,TM.TRANSNUM ,CONVERT(DATETIME,CONVERT(NVARCHAR(20), TM.ITEMDATETIME,106)),isnull(TT.Createdate,TM.ITEMDATETIME),TT.Disc  ) TMP  ORDER BY RECEIPTNO"
            cmd = New OleDbCommand(querystr, cn)

            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()
                Dim receiptNo As String = dr.GetValue(3).ToString.Trim
                Dim docDt As String = dr.GetDateTime(4).ToString("yyyy-MM-dd")
                Dim docTime As String = dr.GetDateTime(4).ToString("hh':'mm':'ss")
                Dim grossAmount As Decimal = 0
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(7))
                Dim tax As Double = dr.GetValue(6)
                Dim totDisc As String = dr.GetValue(8).ToString
                Dim netDiscount = totDisc
                Dim saleQtySum As Double = 0
                Dim transaction_id As String = dr.GetDateTime(4).ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")
                sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                Console.WriteLine("SALES RECORD ==>" + sales_record)
                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + "" + SEPARATOR_ITEMS
                line_items = ""
                sales_record = ""
                inv_count = inv_count + 1
                If (inv_count = NumRecordsPerBatch) Then
                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        invoice_text = invoice_text.Replace(" ", "_")
                        '-----
                        Dim uploadprocess As System.Diagnostics.Process
                        uploadprocess = New System.Diagnostics.Process()
                        uploadprocess.StartInfo.FileName = java_path
                        'myProcess.StartInfo.Arguments = "E:\dev\batch2\Acceserize\AccessoriesStore\dist\AccessoriesStore.jar"
                        uploadprocess.StartInfo.Arguments = "-jar " + upload_path + " " + invoice_text
                        uploadprocess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                        uploadprocess.StartInfo.UseShellExecute = False
                        uploadprocess.StartInfo.RedirectStandardOutput = True
                        uploadprocess.Start()
                        outout = uploadprocess.StandardOutput.ReadToEnd()
                        Console.WriteLine(outout)

                        'params.Add("salesbatch", invoice_text)
                        'Console.WriteLine("invoice text " + invoice_text)
                        'Console.WriteLine(invoice_text)
                        'response1 = serverUpload("savebatch.php", params)
                        'params.Remove("salesbatch")
                        response1 = outout
                        'Console.WriteLine("Response ->" + response1)
                        inv_count = 0
                        invoice_text = ""
                        sales_record = ""
                    End If
                End If
            End While

            If (inv_count > 0) Then
                If invoice_text.Length >= 5 Then
                    invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    invoice_text = invoice_text.Replace(" ", "_")

                    Dim uploadprocess As System.Diagnostics.Process
                    uploadprocess = New System.Diagnostics.Process()
                    uploadprocess.StartInfo.FileName = java_path
                    'myProcess.StartInfo.Arguments = "E:\dev\batch2\Acceserize\AccessoriesStore\dist\AccessoriesStore.jar"
                    uploadprocess.StartInfo.Arguments = "-jar " + upload_path + " " + invoice_text
                    uploadprocess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                    uploadprocess.StartInfo.UseShellExecute = False
                    uploadprocess.StartInfo.RedirectStandardOutput = True
                    uploadprocess.Start()
                    outout = uploadprocess.StandardOutput.ReadToEnd()
                    Console.WriteLine(outout)
                    response1 = outout

                    'invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    'params.Add("salesbatch", invoice_text)
                    'inv_count = inv_count + 1
                    ''Console.WriteLine(invoice_text)
                    'response1 = serverUpload("savebatch.php", params)
                    'params.Remove("salesbatch")
                    'Console.WriteLine("Sale Response ->" + response1)
                    invoice_text = ""
                End If
            End If

        Catch ex As Exception
            WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
        End Try
        count = 0
        Try
            Dim retun_send_count As Integer = 0
            Dim return_to_send As SortedList = New SortedList()
            Dim rcount1 As Integer = 0
            Dim return_record As String = ""
            Dim return_start_date As String = ""
            Dim return_end_date As String = ""
            Dim k As Integer = 0
            Dim invoice_text As String = ""
            Dim inv_count As Integer = 0
            cn = New OleDbConnection(connectString)
            cn.Open()
            'ShopNo= 9086
            'username = UID9086
            'Password=9086@123
            Dim return_query As String = "select transnum,itemdatetime,qty,extsellprice,extorigprice as netamt ,(extundiscprice -extsellprice ) as discount,(tax_amt_1+tax_amt_2+tax_amt_3+tax_amt_4+tax_amt_5+tax_amt_6+tax_amt_7+tax_amt_8+tax_amt_9+tax_amt_10+tax_amt_11+tax_amt_12+tax_amt_13+tax_amt_14+tax_amt_15+tax_amt_16 ) from  dbo.Txn_Merchandise_Sale where (convert(datetime,convert(nvarchar,ITEMDATETIME,121)) > '" + ret_response + "' )  AND txnmodifier = 5 AND txnvoidmod = 0 order by itemdatetime "
            cmd2 = New OleDbCommand(return_query, cn)
            dr5 = cmd2.ExecuteReader
            Dim rcnt As Integer = 0
            While (dr5.Read)
                rcnt = rcnt + 1
                Dim receiptNo As String = "C-" + dr5.GetValue(0).ToString + rcnt.ToString
                Dim docDt As String = dr5.GetDateTime(1).ToString("yyyy-MM-dd")
                Dim docTime As String = dr5.GetDateTime(1).ToString("hh':'mm':'ss")
                Dim grossAmount As Decimal = 0
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr5.GetValue(3) * -1)
                Dim tax As Double = dr5.GetValue(6) * -1
                Dim totDisc As String = dr5.GetValue(5).ToString
                Dim netDiscount = totDisc
                Dim saleQtySum As Double = dr5.GetValue(2)
                Dim transaction_id As String = dr5.GetDateTime(1).ToString("yyyy-MM-dd hh':'mm':'ss.fff tt")
                return_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                Console.WriteLine("return_record RECORD ==>" + return_record)
                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
                invoice_text += return_record + SEPARATOR_ITEMLINES + "" + SEPARATOR_ITEMS

                return_record = ""
                inv_count = inv_count + 1
                If (inv_count = NumRecordsPerBatch) Then
                    If invoice_text.Length >= 5 Then
                        invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        invoice_text = invoice_text.Replace(" ", "_")

                        Dim uploadprocess As System.Diagnostics.Process
                        uploadprocess = New System.Diagnostics.Process()
                        uploadprocess.StartInfo.FileName = java_path
                        'myProcess.StartInfo.Arguments = "E:\dev\batch2\Acceserize\AccessoriesStore\dist\AccessoriesStore.jar"
                        uploadprocess.StartInfo.Arguments = "-jar " + upload_path + " " + invoice_text
                        uploadprocess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                        uploadprocess.StartInfo.UseShellExecute = False
                        uploadprocess.StartInfo.RedirectStandardOutput = True
                        uploadprocess.Start()
                        outout = uploadprocess.StandardOutput.ReadToEnd()
                        Console.WriteLine(outout)
                        response1 = outout

                        'invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                        'params.Add("salesbatch", invoice_text)

                        ''Console.WriteLine("invoice text " + invoice_text)
                        'response1 = serverUpload("savebatch.php", params)
                        'params.Remove("salesbatch")
                        'Console.WriteLine("Response ->" + response1)
                        inv_count = 0
                        invoice_text = ""
                        return_record = ""
                    End If
                End If
            End While
            If (inv_count > 0) Then
                If invoice_text.Length >= 5 Then
                    'invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    'params.Add("salesbatch", invoice_text)
                    invoice_text = Left(invoice_text, Len(invoice_text) - 5)
                    invoice_text = invoice_text.Replace(" ", "_")

                    Dim uploadprocess As System.Diagnostics.Process
                    uploadprocess = New System.Diagnostics.Process()
                    uploadprocess.StartInfo.FileName = java_path
                    'myProcess.StartInfo.Arguments = "E:\dev\batch2\Acceserize\AccessoriesStore\dist\AccessoriesStore.jar"
                    uploadprocess.StartInfo.Arguments = "-jar " + upload_path + " " + invoice_text
                    uploadprocess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
                    uploadprocess.StartInfo.UseShellExecute = False
                    uploadprocess.StartInfo.RedirectStandardOutput = True
                    uploadprocess.Start()
                    outout = uploadprocess.StandardOutput.ReadToEnd()
                    Console.WriteLine(outout)
                    response1 = outout

                    inv_count = inv_count + 1
                    'Console.WriteLine(invoice_text)
                    'response1 = serverUpload("savebatch.php", params)
                    'params.Remove("salesbatch")
                    'Console.WriteLine("Sale Response ->" + response1)
                    invoice_text = ""
                End If
            End If

        Catch ex As Exception
            Console.WriteLine(ex.ToString)
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
        'Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        Dim url As String = serverProtocol + "://192.168.0.108/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://localhost:8085/phoenix_new/public_html/" + serverPath + "/" + subUrl
        '
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
