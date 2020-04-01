Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for Soch
    Dim cn As OleDbConnection = Nothing
    Dim cmd As OleDbCommand = Nothing
    Dim dr As OleDbDataReader = Nothing
    Dim cmd2 As OleDbCommand
    Dim dr1 As OleDbDataReader = Nothing
    Dim line1 As OleDbDataReader = Nothing
    Dim dr2 As OleDbDataReader = Nothing
    Dim dr3 As OleDbDataReader = Nothing
    Dim dr4 As OleDbDataReader = Nothing
    Dim dr5 As OleDbDataReader = Nothing

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
    Dim lastid As Integer
    ' Dim lastid As String

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

        password = RijndaelSimple.Decrypt(password, username)

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

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")
        '  Dim fetchDate As String
        Dim response As String = serverUpload("lastuploadinfo.php", params)
        Console.WriteLine("lastuploadinfo=" + response)

        lastid = response
        'Console.WriteLine("Order Format : " + fetchDate)
        '164219
        Try

            'Fetch invoices and their items and payment terms
            cn = New OleDbConnection(connectString)
            cn.Open()
            'Dim querystr As String = "SELECT id,billno,billdate,grossamt,netamt,discountamt FROM POSBill where id  > " + lastid.ToString + " "
            Dim querystr As String = "SELECT id,billno,billdate,SaleAmt,ReturnAmt,DiscountAmt,PromoAmt,MRPAmt FROM POSBill where id  > " + lastid.ToString + " "

            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader
            Dim inv_text As String = ""
            Dim sales_record As String = ""
            Dim payment_record As String = ""
            Dim line_items As String = ""
            Dim payment_mode As String = ""
            Dim invoices_to_send As SortedList = New SortedList
            Dim invoice_text As String = ""
            Dim count As Integer = 0
            While dr.Read()

                Dim inv_id As String = dr.GetInt32(0).ToString
                Dim inv_no As String = dr.GetString(1)
                Dim inv_datetime As Date = dr.GetDateTime(2)
                Dim sale_amt As Double = dr.GetDecimal(3)
                Dim return_amt As Double = dr.GetDecimal(4)
                Dim discount_amt As Double = dr.GetDecimal(5)
                Dim promo_amt As Double = dr.GetDecimal(6)
                Dim mrp_amt As Double = dr.GetDecimal(7)
                Dim transaction_id As String = dr.GetInt32(0).ToString
                Dim voucher_amt As Double = 0.0
                Dim vat As Double = 0.0
                Dim total_qty As Double = 0.0
                Dim tax_line_info As String = ""
                Dim tot_inv_cnt As String = 0
                Dim total_amt = 0.0
                Dim total_disc = 0.0
                Dim inv_date1 As String
                Dim inv_time As String

                If (sale_amt <> 0 Or (mrp_amt <> 0.0 And mrp_amt <> -1.0)) Then

                    total_amt = sale_amt + return_amt - discount_amt
                    total_disc = promo_amt + discount_amt

                   
                    Dim inv_details() As String = Split(inv_datetime, " ")
                    Dim inv_date As Date = inv_details(0)
                    inv_time = inv_details(1)
                    inv_date1 = inv_date.ToString("yyyy-MM-dd")


                ElseIf return_amt < 0 And sale_amt = 0 Then
                    total_amt = return_amt
                    total_disc = promo_amt + discount_amt

                 
                    Dim inv_details() As String = Split(inv_datetime, " ")
                    Dim inv_date As Date = inv_details(0)
                    inv_time = inv_details(1)
                    inv_date1 = inv_date.ToString("yyyy-MM-dd")
                   
                End If

                If (inv_no Is Nothing And Trim(inv_no) = "") Then
                    Console.WriteLine("Invalid invoice number")
                Else
                    'Invoice item details

                    'Dim querystr1 As String = "SELECT it.Barcode ,it.Name ,pbi.Qty ,pbi.TaxAmt ,pbi.DiscountAmt ,pbi.mrp,pbi.BasicAmt,pbi.TaxPercent,pbi.TaxDescription FROM [NPOS].[dbo].POSBillItem pbi join Item it on it.ItemId = pbi.ItemId  where pbi.POSBillId = " + inv_id + ""
                    Dim querystr1 As String = "SELECT it.Barcode,it.Name,pbi.Qty,pbi.MRP,pbi.TaxAmt,pbi.DiscountAmt,pbi.MRPAmt,pbi.PromoAmt,pbi.TaxPercent,pbi.TaxDescription FROM [NPOS].[dbo].POSBillItem pbi join Item it on it.ItemId = pbi.ItemId  where pbi.POSBillId = " + inv_id + ""
                    cmd = New OleDbCommand(querystr1, cn)
                    dr1 = cmd.ExecuteReader
                    While dr1.Read()
                        Dim barcode As String = dr1.GetString(0)
                        Dim item_name As String = dr1.GetString(1)
                        Dim item_qty As Double = dr1.GetDecimal(2)
                        Dim unit_price As Double = dr1.GetDecimal(3)
                        Dim tax As Double = dr1.GetDecimal(4)
                        Dim disc_value As Double = dr1.GetDecimal(5)
                        Dim line_total As Double = dr1.GetDecimal(6)
                        Dim promo_value As Double = dr1.GetDecimal(7)
                        Dim tax_pct As Double = dr1.GetDecimal(8)
                        Dim tax_desc As String = ""
                        If Not dr1.IsDBNull(9) Then
                            tax_desc = dr1.GetString(9)
                        End If

                        If (tax_desc.Equals("Non Taxable")) Then
                            voucher_amt += line_total
                        End If

                        Dim disc_price As Double = 0.0

                        total_qty = total_qty + item_qty
                        vat = vat + tax
                        tax_line_info = tax_line_info + tax_desc + "::" + tax_pct.ToString + "::" + tax.ToString + ","
                        line_items += barcode + SEPARATOR_FIELDS + item_name.ToString + SEPARATOR_FIELDS + item_qty.ToString + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + disc_price.ToString + SEPARATOR_FIELDS + disc_value.ToString + SEPARATOR_FIELDS + line_total.ToString + SEPARATOR_FIELDS + unit_price.ToString + SEPARATOR_FIELDS + tax_pct.ToString + SEPARATOR_FIELDS + tax_desc + SEPARATOR_ITEMFIELDS


                    End While


                    Dim pquery As String = "SELECT MOPDesc, BaseAmt FROM POSBillMOP WHERE POSBillId = " + inv_id + ""
                    cmd = New OleDbCommand(pquery, cn)
                    dr1 = cmd.ExecuteReader
                    While dr1.Read()
                        Dim desc As String = dr1.GetString(0)
                        Dim amt1 As Double = dr1.GetDecimal(1)
                        payment_mode = payment_mode + desc + "::" + amt1.ToString + ","

                    End While

                End If

                Dim gross_amt = total_amt - voucher_amt

                
                sales_record = inv_no.ToString + SEPARATOR_FIELDS + inv_date1.ToString + SEPARATOR_FIELDS + inv_time.ToString + SEPARATOR_FIELDS + gross_amt.ToString + SEPARATOR_FIELDS + gross_amt.ToString + SEPARATOR_FIELDS + vat.ToString + SEPARATOR_FIELDS + discount_amt.ToString + SEPARATOR_FIELDS + total_qty.ToString + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + tax_line_info + SEPARATOR_FIELDS + payment_mode + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS


                'payment_record = payment_mode.ToString
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMLINES + payment_record + SEPARATOR_ITEMS
                Console.WriteLine(invoice_text)
                line_items = ""
                sales_record = ""
                payment_record = ""
                payment_mode = ""
                tax_line_info = ""
                count = count + 1
                If count = 30 Then
                    'Console.WriteLine(invoice_text)
                    params = New NameValueCollection()
                    params.Add("salesbatch", invoice_text)
                    response = serverUpload("savebatch.php", params)
                    invoice_text = ""
                    count = 0

                End If

                ' If (sale_amt <> 0 And (mrp_amt <> 0.0 Or mrp_amt <> -1.0)) end


            End While

            If count > 0 Then
                'Console.WriteLine(invoice_text)
                params = New NameValueCollection()
                params.Add("salesbatch", invoice_text)
                response = serverUpload("savebatch.php", params)
            End If


            ''Fetch Inventory
            'cn = New OleDbConnection(connectString)
            'cn.Open()
            'Dim qry As String = "SELECT IT.Barcode,IT.Name,ST.Qty FROM ItemStock AS ST JOIN Item AS IT ON IT.ItemId = ST.ItemId WHERE ST.STOCKPOINTID = 1"
            'cmd = New OleDbCommand(qry, cn)
            'dr2 = cmd.ExecuteReader
            'Dim stock_details As String = ""
            'Dim tot_stock_items As Integer = 0
            'Dim response1 As String = ""
            'Dim tot_items As Integer = 0
            'Dim tot_quantity As Integer = 0
            'Dim inventory_data As String = ""

            'While dr2.Read()
            '    Dim stock_barcode As String = dr2.GetString(0)
            '    Dim stock_name As String = dr2.GetString(1)
            '    Dim curr_stock As Double = dr2.GetDecimal(2)
            '    stock_details += stock_barcode + SEPARATOR_FIELDS + stock_name + SEPARATOR_FIELDS + curr_stock.ToString + SEPARATOR_ITEMLINES
            '    tot_stock_items = tot_stock_items + 1
            '    tot_items = tot_items + 1
            '    tot_quantity = tot_quantity + curr_stock
            '    If tot_stock_items = 200 Then
            '        inventory_data = tot_items.ToString() + SEPARATOR_FIELDS + tot_quantity.ToString() + SEPARATOR_ITEMLINES + stock_details
            '        Console.WriteLine(inventory_data)
            '        params = New NameValueCollection()
            '        params.Add("stockbatch", inventory_data)
            '        response1 = serverUpload("savebatchInventory.php", params)
            '        Console.WriteLine("Survey Response" + response1)
            '        tot_stock_items = 0
            '        stock_details = ""
            '        inventory_data = ""
            '        tot_items = 0
            '        tot_quantity = 0
            '    End If
            'End While
            'If tot_stock_items > 0 Then
            '    params = New NameValueCollection()
            '    params.Add("stockbatch", stock_details)
            '    response1 = serverUpload("savebatchInventory.php", params)
            '    Console.WriteLine(response1)
            '    tot_stock_items = 0
            '    stock_details = ""
            'End If




        Catch ex As Exception
            WriteToEventLog(ex.ToString)
        End Try
        '    End If
        If (dr IsNot Nothing) Then
            dr.Close()
        End If
        If (cn IsNot Nothing) Then
            cn.Close()
        End If
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
        'Dim url As String = serverProtocol + "://192.168.0.26/phoenix_new/public_html/" + serverPath + "/" + subUrl
        Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl

        ' url = UrlAppend(url, subUrl)
        ' Console.WriteLine("URL is: " + url)
        Try

            Dim myCache As New CredentialCache()
            myCache.Add(New Uri(url), "Digest", New NetworkCredential(username, password))

            webClient.Credentials = myCache
            Dim responseArray As Byte() = webClient.UploadValues(url, params)
            Dim responseString As String = Encoding.ASCII.GetString(responseArray)
            serverUpload = responseString
            'Console.WriteLine(responseString)

            ' If subUrl = "savebatch.php" Then
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
