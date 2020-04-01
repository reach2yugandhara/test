Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized
Imports MySql.Data.MySqlClient


Module Module1
    'code for smoor
    Dim MysqlConn As MySqlConnection
    Dim MysqlConn2 As MySqlConnection
    Dim Mysqlconn3 As MySqlConnection
    Dim Mysqlconn4 As MySqlConnection

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
            Console.WriteLine("Username ::" + username)
            Console.WriteLine("Password ::" + password)

        Catch ex As Exception

        End Try

        Dim connectString As String = iniProps.Get("CS")
        If (IsEmpty(connectString)) Then
            WriteToEventLog("INI ERROR:Missing DBConnectString")
            Return
        End If

        Try
            ' connectString = RijndaelSimple.Decrypt(connectString, username)
            connectString = connectString.Replace("userid", "user id")


            'connectString = "server=localhost;user id=dbusr;password=dbpass;database=smoor_db"

        Catch ex As Exception

        End Try
      
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
        Console.WriteLine("Response ::" + response)

        'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim ret_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim retcount As Integer = 0
        Try
            MysqlConn = New MySqlConnection()
            MysqlConn2 = New MySqlConnection()



            MysqlConn.ConnectionString = connectString
            MysqlConn2.ConnectionString = connectString

            MysqlConn.Open()
            MysqlConn2.Open()

           
            '------------------------------------Sales Data-------------------------------------------------------------
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim sales_record As String = ""
            Dim querystr As String = "SELECT orderId,DTTM,bill,orderTempId from orders where isBilled=1 and orderTempId > '" + response + "' order by orderTempId"
            Console.WriteLine("Query" + querystr)
            Dim cmd As MySqlCommand = New MySqlCommand(querystr, MysqlConn)
            Dim reader As MySqlDataReader = cmd.ExecuteReader()
            Dim trntype As String = "1"


            While reader.Read()

                Dim docNo As String = reader.GetValue(0).ToString
                Dim inv_date As String = reader.GetDateTime(1).ToString("yyyy-MM-dd")
                Dim inv_time As String = reader.GetDateTime(1).ToString("hh':'mm':'ss")

                Dim trnid As String = reader.GetValue(3).ToString
                Dim Receiptno As String = docNo + "-" + inv_date
             
                Dim doctax As Double = 0.0
                Dim docnet As Double = Convert.ToDouble(reader.GetValue(2))

                Dim docgross As Double = 0.0
                Dim docqty As Double = 0.0
         

                Dim query_1 As String = "select dishId,quantity,dishPrice,(cgst+sgst) from orderdetails where orderId='" + docNo.ToString + "'"


                Dim cmd1 As MySqlCommand = New MySqlCommand(query_1, MysqlConn2)
                Dim reader1 As MySqlDataReader = cmd1.ExecuteReader()

                While reader1.Read()
                    Dim itemid As String = reader1.GetValue(0).ToString
                    Dim quant As Double = Convert.ToDouble(reader1.GetValue(1))
                    Dim mrp As Double = Convert.ToDouble(reader1.GetValue(2))
                    Dim net As Double = Convert.ToDouble(reader1.GetValue(2))
                    Dim tax As Double = Convert.ToDouble(reader1.GetValue(3))

                    docgross += mrp
                    docqty += quant
                    doctax += tax


                    '$itemCode,                         $lineItemName,          $lineQuantity,                          $tax,                            $discountPct,                                 $discountVal,              $lineTotal,                           $unitPrice
                    line_items = line_items + itemid.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + quant.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + net.ToString + SEPARATOR_FIELDS + mrp.ToString() + SEPARATOR_ITEMFIELDS
                End While

                reader1.Close()
                '$receiptNo,                                               $billDate,                     $billTime,                                            $grossAmount,                         $totalAmount,                               $vatAmount,                             $disountVal,                             $qty,                                 $transaction_id,              $tax_lines_info,          $payment_lines_info

                sales_record = Receiptno.ToString + SEPARATOR_FIELDS + inv_date + SEPARATOR_FIELDS + inv_time + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + docnet.ToString() + SEPARATOR_FIELDS + doctax.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + trnid.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + trntype + SEPARATOR_FIELDS
                'sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + taxamt.ToString + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + totqty.ToString + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS

                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS


                sales_record = ""
                line_items = ""
                count = count + 1
                Console.WriteLine("SALECount " + count.ToString())
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

            reader.Close()

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


    Function GetTime(ByVal dt As DateTime) As String
        Return dt.TimeOfDay.ToString
    End Function

    Private Function serverUpload(ByVal subUrl As String, ByVal params As NameValueCollection)
        Dim webClient As New WebClient()
        Console.WriteLine("inside server upload ")
        'serverProtocol = 
        'Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl

        Dim url As String = serverProtocol + "://localhost:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl

        'Console.WriteLine("after url ")
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
