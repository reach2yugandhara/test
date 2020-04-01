Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'code for world of titan

    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing

    Dim dr As OleDbDataReader = Nothing
    Dim drcpy As OleDbDataReader = Nothing
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
    Dim itm_id As String = " "


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
     
        Dim salesres As String = response

       'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim Inv_amnt1 As Integer = 0
        Try
            Dim salesTrnType As Integer = 1
            cn = New OleDbConnection(connectString)
            cn.Open()

            Dim querystr As String = "select distinct invoiceNumber,InvoiceYear,CreateDate,StoreTimeStamp,TransactionType from dbo.PaymentTrn where StoreTimeStamp>" + salesres + " and sequenceNumber=1 order by StoreTimeStamp"
            cmd = New OleDbCommand(querystr, cn)
            Console.WriteLine(querystr)
            dr = cmd.ExecuteReader
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim sales_record As String = ""

            While dr.Read()

                Dim inv_no As Integer = dr.GetInt32(0)

                Dim doc_no As String = ""
                Dim inv_year As Integer = dr.GetInt16(1)
                Dim receipt_no As String = inv_no.ToString + "-" + inv_year.ToString
                Dim inv_date As Integer = dr.GetInt32(2)
                Dim invDateFormated As Date
                Dim inv_dateS As String = inv_date.ToString
                'Date formating---------------------------------------------------------------------
                Dim format As String = "yyyyMMdd"
                Dim provider As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
                Try
                    invDateFormated = Date.ParseExact(inv_dateS, Format, provider)
                Catch e As FormatException
                    Console.WriteLine("{0} is not in the correct format.", DateString)
                End Try
                invDateFormated = invDateFormated.ToString("yyyy-MM-dd 00:00:00")
                Dim transaction_id As String = dr.GetValue(3).ToString
                Console.WriteLine("-------------------orderformat:" + transaction_id)
                Dim TransType As Integer = dr.GetValue(4)
                Dim itemid As String = ""
                'For return

               
                If (TransType = 9) Then
                    'Dim querystrcpy1 As String = "select invoiceNumber,invoiceyear,BillCancellationNumber from BillCancellationTrn where BillCancellationNumber=" + inv_no.ToString + " and invoiceyear=" + inv_year.ToString + ""
                    'cmd = New OleDbCommand(querystrcpy1, cn)
                    'drcpy = cmd.ExecuteReader
                    'While drcpy.Read()
                    '    inv_no = drcpy.GetInt32(0)
                    '    inv_year = drcpy.GetInt16(1)
                    '    doc_no = drcpy.GetValue(2).ToString + "-" + inv_year.ToString
                    'End While
                    salesTrnType = 11111
                End If

                Dim tot_disc As Double = 0.0
                Dim tot_net_amt As Double = 0.0
                Dim Invoice_amt As Double = 0.0
                Dim tot_tax_amt = 0.0
                Dim tot_sold_qty = 0.0
                Dim SalesPrice As Double = 0.0
                Dim Rettax_amt As Double = 0.0
                Dim Retdisc_amt As Double = 0.0
                Dim tot_net_amt_ret As Double = 0.0
                Dim tot_tax_amt_ret As Double = 0.0
                Dim tot_disc_ret As Double = 0.0
                If (TransType = 5) Then

                    'Dim querystrcpy As String = "select SUM(LocalAmount) as LocalAmout,SUM(SalesTax) as SalesTax , sum( SalesPrice) as SalesPrice from dbo.SalesReturnTrn where SalesReturnNumber=" + inv_no.ToString + " and SalesReturnYear=" + inv_year.ToString + "and InvoiceType= 30 "
                    Dim querystrcpy As String = "select LocalAmount as LocalAmout,SalesTax as SalesTax ,  SalesPrice as SalesPrice, InvoiceType from dbo.SalesReturnTrn  where SalesReturnNumber=" + inv_no.ToString + " and SalesReturnYear=" + inv_year.ToString + ""
                    'Dim querystrcpy As String = "select LocalAmount,SalesTax , SalesPrice from dbo.SalesReturnTrn where SalesReturnNumber=" + inv_no.ToString + " and SalesReturnYear=" + inv_year.ToString + "and InvoiceType= 31 "
                    cmd = New OleDbCommand(querystrcpy, cn)
                    drcpy = cmd.ExecuteReader

                    While drcpy.Read()
                        Dim invoice_type As Byte = drcpy.GetByte(3)
                        'doc_no = inv_no.ToString + "-" + inv_year.ToString
                        
                        Dim gift_exchng As Integer = 1

                        If (invoice_type = 31) Then
                            Rettax_amt = drcpy.GetDecimal(1)
                        End If

                        If (invoice_type = 35) Then
                            Retdisc_amt = drcpy.GetDecimal(0)

                        End If
                        If (invoice_type = 30) Then
                            tot_net_amt = drcpy.GetDecimal(0)

                        End If

                        tot_net_amt_ret = tot_net_amt
                        tot_disc_ret = Retdisc_amt
                        tot_tax_amt_ret = Rettax_amt

                    End While
                    salesTrnType = 2

                    line_items = ""


                Else
                    'Dim query_1 As String = "select invoicetype,localamount,InvoiceQuantity,ItemNumber from CashOrderTrn where invoiceNumber=" + inv_no.ToString + " and InvoiceYear=" + inv_year.ToString + ""
                    Dim query_1 As String = "select invoicetype,localamount,InvoiceQuantity,ItemNumber,SalesPrice from CashOrderTrn where invoiceNumber=" + inv_no.ToString + " and InvoiceYear=" + inv_year.ToString + ""
                    cmd = New OleDbCommand(query_1, cn)
                    dr1 = cmd.ExecuteReader

                    Dim qty As Decimal = 0.0
                    Dim qty1 As Decimal = 0.0
                    '' Dim invoice_type As Byte = dr1.GetByte(0)
                    Dim total_amt As Double = 0
                    Dim tot_amt As Double = 0
                    Dim tax_amt As Double = 0
                    Dim tx_amt As Double = 0
                    Dim disc_amt As Double = 0
                    Dim dc_amt As Double = 0
                    Dim itemName As String = " "
                    tot_net_amt = 0

                    While dr1.Read()
                        itm_id = dr1.GetValue(3)

                        If (itm_id = "GIFT CARD" Or itm_id.Equals("600102ZNGAAP00")) Then
                            Exit While
                        Else
                            Dim invoice_type As Byte = dr1.GetByte(0)
                            If (invoice_type = 31) Then
                                total_amt = dr1.GetDecimal(1)
                                itm_id = dr1.GetString(3)
                                qty = dr1.GetDecimal(2)
                                line_items = line_items + itm_id + SEPARATOR_FIELDS + itemName + SEPARATOR_FIELDS + qty.ToString() + SEPARATOR_FIELDS + tx_amt.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + dc_amt.ToString() + SEPARATOR_FIELDS + total_amt.ToString() + SEPARATOR_FIELDS + total_amt.ToString() + SEPARATOR_ITEMFIELDS

                            End If

                            If (invoice_type = 68) Then
                                tax_amt = dr1.GetDecimal(1)
                            End If

                            If (invoice_type = 32) Then
                                disc_amt = dr1.GetDecimal(1)

                            End If
                            If (invoice_type = 35) Then
                                disc_amt = dr1.GetDecimal(1)

                            End If
                            'If (invoice_type = 30) Then
                            '    Invoice_amt = dr1.GetDecimal(1)

                            'End If

                            'If (invoice_type = 31) Then
                            '    Invoice_amt = dr1.GetDecimal(1)
                            '    'Inv_amnt1 = Invoice_amt
                            '    Inv_amnt1 += Invoice_amt

                            '    'tot_net_amt = Inv_amnt1 - tot_disc
                            'End If
                            
                            tot_disc += disc_amt
                            tot_tax_amt += tax_amt
                            tot_sold_qty += qty

                            If (disc_amt <> 0) Then
                                dc_amt = disc_amt
                                disc_amt = 0
                            End If
                            If (total_amt <> 0) Then
                                tot_amt = total_amt
                                total_amt = 0
                            End If
                            If (tax_amt <> 0) Then
                                tx_amt = tax_amt
                                tax_amt = 0
                            End If
                            If (qty <> 0) Then
                                qty1 = qty
                                qty = 0
                            End If







                        End If
                    End While
                    'select SUM(localamount),(select SUM(localamount) from CashOrderTrn where invoiceNumber=100002603 and InvoiceYear=2019 and invoicetype=35 ) as disc from CashOrderTrn where invoiceNumber=100002603 and InvoiceYear=2019 and invoicetype=31
                    Dim query_2 As String = "select SUM(localamount),(select SUM(localamount) from CashOrderTrn where invoiceNumber=" + inv_no.ToString + " and InvoiceYear=" + inv_year.ToString + " and invoicetype=35) as Disc from CashOrderTrn where invoiceNumber=" + inv_no.ToString + " and InvoiceYear=" + inv_year.ToString + " and invoicetype=31"
                    Dim disc As Double = 0.0
                    cmd = New OleDbCommand(query_2, cn)
                    dr2 = cmd.ExecuteReader
                    While dr2.Read()
                        Invoice_amt = dr2.GetDecimal(0)
                        If (IsDBNull(dr2.GetValue(1))) Then

                            disc = 0.0 'Do success ELSE 'Failure End If
                        Else

                            disc = Convert.ToDouble(dr2.GetValue(1))
                        End If

                        'tot_net_amt = Invoice_amt - tot_disc
                        tot_net_amt = Invoice_amt - disc
                        'tot_net_amt = tot_amt
                    End While

                End If


                'Dim GiftQuery As String = "select invoicenumber from dbo.GiftCardTrnLog where InvoiceYear=" + inv_year.ToString + " and invoicenumber=" + inv_no.ToString + " and TransactionType='A'"
                'cmd = New OleDbCommand(GiftQuery, cn)
                'dr3 = cmd.ExecuteReader

                'If dr3.HasRows() Then
                '    salesTrnType = 1
                '    ' Console.WriteLine("Cancled  " + inv_no.ToString + "  | " + salesTrnType.ToString)
                'Else
                '    salesTrnType = salesTrnType
                '    'Console.WriteLine("Not Cancled  " + inv_no.ToString + "  | " + salesTrnType.ToString)
                'End If
                If (itm_id = "GIFT CARD") Then
                    sales_record = ""
                ElseIf (TransType = 5) Then
                    salesTrnType = 2
                    'tot_net_amt = tot_net_amt * -1
                    'sales_record = receiptNo + SEPARATOR_FIELDS + docDt +               SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + " " +                   SEPARATOR_FIELDS + totNetDocValue.ToString() +       SEPARATOR_FIELDS + tottax.ToString() +           SEPARATOR_FIELDS + totreturnDisc + SEPARATOR_FIELDS + saleQtySum1.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                    'sales_record = doc_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + Invoice_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS
                    sales_record = receipt_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + SalesPrice.ToString() + SEPARATOR_FIELDS + tot_net_amt_ret.ToString() + SEPARATOR_FIELDS + tot_tax_amt_ret.ToString() + SEPARATOR_FIELDS + tot_disc_ret.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS

                    tot_net_amt = 0
                ElseIf (TransType = 6) Then
                    salesTrnType = 1
                    If (tot_net_amt < 0) Then
                        'tot_net_amt = tot_net_amt * -1
                        tot_net_amt = tot_net_amt
                    End If

                    If (tot_tax_amt < 0) Then
                        'tot_tax_amt = tot_tax_amt * -1
                        tot_tax_amt = tot_tax_amt

                    End If
                    'sales_record = receiptNo + SEPARATOR_FIELDS + docDt +               SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + " " +                   SEPARATOR_FIELDS + totNetDocValue.ToString() +       SEPARATOR_FIELDS + tottax.ToString() +           SEPARATOR_FIELDS + totreturnDisc + SEPARATOR_FIELDS + saleQtySum1.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                    'sales_record = doc_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + Invoice_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS
                    sales_record = receipt_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + SalesPrice.ToString() + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS
                    tot_net_amt = 0
                Else

                    'sales_record = receipt_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                    sales_record = receipt_no.ToString + SEPARATOR_FIELDS + invDateFormated.ToString("yyyy-MM-dd HH:mm:ss") + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + Invoice_amt.ToString() + SEPARATOR_FIELDS + tot_net_amt.ToString() + SEPARATOR_FIELDS + tot_tax_amt.ToString() + SEPARATOR_FIELDS + tot_disc.ToString() + SEPARATOR_FIELDS + tot_sold_qty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString() + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + salesTrnType.ToString + SEPARATOR_FIELDS
                    tot_net_amt = 0
                    End If




                salesTrnType = 1

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

                tot_net_amt_ret = 0.0
                tot_disc_ret = 0.0
                tot_tax_amt_ret = 0.0
                tot_disc = 0.0
                tot_net_amt = 0.0
                tot_tax_amt = 0.0
                tot_sold_qty = 0.0
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
