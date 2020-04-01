Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for Wall climbing
    Dim cn As OleDbConnection = Nothing
    Dim cn1 As OleDbConnection = Nothing

    Dim cmd As OleDbCommand = Nothing
    Dim cmd2 As OleDbCommand = Nothing
    Dim cmd3 As OleDbCommand = Nothing
    Dim cmdReturn As OleDbCommand = Nothing
    Dim cmdReturnItems As OleDbCommand = Nothing
    Dim cmdStock As OleDbCommand = Nothing

    Dim dr As OleDbDataReader = Nothing
    Dim dr1 As OleDbDataReader = Nothing
    Dim dr2 As OleDbDataReader = Nothing
    Dim dr3 As OleDbDataReader = Nothing
    Dim dr4 As OleDbDataReader = Nothing
    Dim dr5 As OleDbDataReader = Nothing
    Dim rdr As OleDbDataReader = Nothing
    Dim rdr1 As OleDbDataReader = Nothing
    Dim drReturn As OleDbDataReader = Nothing
    Dim drReturnItems As OleDbDataReader = Nothing
    Dim drStock As OleDbDataReader = Nothing
    Dim line1 As OleDbDataReader = Nothing

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

        '-------------------------------------------------------------------------------------------------------'

        recordTypeToFetch = "(" + recordTypeToFetch + ")"

        Dim numRecords As Integer
        numRecords = Integer.Parse(numrecs)

        Dim params As New NameValueCollection()
        params.Add("getorderformat", "1")

        'Dim format As String = "yyyyMMddHHmmss"
        'Dim response As String

        Dim response As String = serverUpload("lastuploadinfo.php", params)
        Dim res() As String = response.Split(",")
        Dim SalesRes As String = res(0)
        Dim ReturnRes As String = res(1)

        Dim resTP() As String = SalesRes.Split("^")
        Dim salesDate As DateTime = resTP(0)
        '    Dim salesDate As DateTime = res(0)
        Dim salesDate1 As String = salesDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
        Dim salesDateRes As String = resTP(0)
        Dim salesBill As Integer = resTP(1)

        Dim resTP1() As String = ReturnRes.Split("^")
        Dim retDate As DateTime = resTP1(0)
        Dim retDate1 As String = retDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
        Dim retBill As Integer = resTP1(1)
        Dim retDateRes As String = resTP1(0)
        Console.WriteLine("response ::::  " + response.ToString)
        Debug("lastuploadinfo=" + response)


        Dim original As DateTime = DateTime.Now
        Dim ts As TimeSpan = original.Subtract(salesDate)
        Dim daysDiff As Integer = Convert.ToInt32(ts.Days)
        Dim ts1 As TimeSpan = original.Subtract(retDate)
        Dim daysDiffRet As Integer = Convert.ToInt32(ts1.Days)


        Dim sales_send_count As Integer = 0
        Dim count As Integer = 0
        Dim count1 = 0

        Do

            response = serverUpload("lastuploadinfo.php", params)
            Dim resSale() As String = response.Split(",")
            Dim salesDateRes1 As String = resSale(0)
            Dim resTP11() As String = salesDateRes1.Split("^")
            salesDate = resTP11(0)
            salesDate1 = salesDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")


            salesDateRes = resTP11(0)
            salesBill = resTP11(1)
            'Check whether records are present for old date
            Try
                cn = New OleDbConnection(connectString)
                cn.Open()

                Dim querystr As String = "select BillID,CreateDate,BillTime,GrossAmt ,NetAmt,Discount  from dbo.BillMasterHistory where CreateDate ='" + salesDate1 + "' and BillID >" + salesBill.ToString + " order by CreateDate , BillID"

                Console.WriteLine(querystr)
                cmd = New OleDbCommand(querystr, cn)
                dr = cmd.ExecuteReader
                If (dr.HasRows) Then
                    Dim sales_record As String = ""
                    Dim invoice_text As String = ""
                    Dim line_items As String = ""
                    Dim inv_count As Integer = 0

                    While dr.Read()
                        Dim payMode As String = " "
                        Dim docDt As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                        Dim billNo As String = dr.GetValue(0).ToString
                        Dim receiptNo As String = dr.GetValue(0).ToString + "," + docDt

                        Dim docTime As String = dr.GetDateTime(2).ToString("hh':'mm':'ss")
                        Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(4))
                        Dim totDisc As String = Convert.ToString(dr.GetValue(5))
                        Dim transaction_id As String = (dr.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ") + "^" + billNo
                        Dim tax As Double = 0
                        Dim saleQtySum As Double = 0
                        Dim pmode As String = " "
                        Dim grossAmount As Double = Convert.ToDouble(dr.GetValue(3))
                        Console.WriteLine("-------------------------------------------------------------")
                        Dim billDt = (dr.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ")


                        Dim query_1 As String = "select a.PLUCode ,(select b.PluName  from dbo.PluMaster b where a.PLUCode =b.PluCode ),a.Quantity ,a.TaxAmount,a.DiscountPerc ,a.DiscountAmt,a.SalePrice*a.Quantity ,a.SalePrice  from dbo.BillTransHistory a where a.CreateDate = '" + billDt + "' and a.BillID =" + billNo + ""
                        cmd = New OleDbCommand(query_1, cn)
                        dr1 = cmd.ExecuteReader

                        While dr1.Read()

                            Dim ItemCode As String = Convert.ToString(dr1.GetValue(0))
                            Dim lineItemName As String = Convert.ToString(dr1.GetValue(1))
                            Dim lineQuantity As Double = Convert.ToDouble(dr1.GetValue(2))
                            Dim TaxAmt As Double = Convert.ToDouble(dr1.GetValue(3))
                            Dim discountPct As String = Convert.ToString(dr1.GetValue(4))
                            Dim discountVal As String = Convert.ToString(dr1.GetValue(5))
                            Dim unitPrice As Decimal = Convert.ToDecimal(dr1.GetValue(7))
                            Dim lineTotal As Decimal = Convert.ToDecimal(dr1.GetValue(6))

                            'Item(level)
                            line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct.ToString + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString
                            saleQtySum += lineQuantity
                            tax += TaxAmt

                        End While


                        sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + pmode + SEPARATOR_FIELDS
                        Console.WriteLine("Sales RECORD")



                        i = (i + 1)
                        Console.WriteLine("invoice count = " + i.ToString)
                        ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
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
                            invoice_text = ""
                        End If
                    End If

                Else

                    'increment the date and fetch records from next date

                    Dim incementDate As DateTime
                    Dim incementDate1 As String
lbl:
                    If (daysDiff > 0) Then
                        incementDate = salesDate.AddDays(1)
                        incementDate1 = incementDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
                        salesBill = 0
                    ElseIf (daysDiff = 0) Then

                        incementDate = salesDate
                        incementDate1 = incementDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
                        response = serverUpload("lastuploadinfo.php", params)
                        Dim res2() As String = response.Split(",")
                        Dim salesDateRes11 As String = resSale(0)
                        Dim resTP111() As String = ReturnRes.Split("^")
                        salesDateRes = resTP111(0)
                        salesBill = resTP111(1)
                    Else
                        Exit Sub
                    End If

                    Try
                        cn1 = New OleDbConnection(connectString)
                        cn1.Open()

                        Dim querystr2 As String = "select BillID,CreateDate,BillTime,GrossAmt ,NetAmt,Discount  from dbo.BillMasterHistory where CreateDate ='" + incementDate1 + "' and BillID >" + salesBill.ToString + " order by CreateDate , BillID"

                        Console.WriteLine(querystr2)
                        cmd2 = New OleDbCommand(querystr2, cn1)
                        dr2 = cmd2.ExecuteReader

                        If (Not dr2.HasRows) Then
                            salesDate = salesDate.AddDays(1)
                            daysDiff = daysDiff - 1
                            GoTo lbl
                        End If

                        Dim sales_record As String = ""
                        Dim invoice_text As String = ""
                        Dim line_items As String = ""
                        Dim inv_count As Integer = 0

                        While dr2.Read()
                            Dim payMode As String = " "
                            Dim docDt As String = dr2.GetDateTime(1).ToString("yyyy-MM-dd")
                            Dim receiptNo As String = dr2.GetValue(0).ToString + "," + docDt
                            Dim billNo As String = dr2.GetValue(0).ToString
                            Dim docTime As String = dr2.GetDateTime(2).ToString("hh':'mm':'ss")
                            Dim totNetDocValue As Decimal = Convert.ToDecimal(dr2.GetValue(4))
                            Dim totDisc As String = Convert.ToString(dr2.GetValue(5))
                            Dim transaction_id As String = (dr2.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ") + "^" + billNo
                            Dim tax As Double = 0
                            Dim saleQtySum As Double = 0
                            Dim pmode As String = " "
                            Dim grossAmount As Double = Convert.ToDouble(dr2.GetValue(3))
                            Console.WriteLine("-------------------------------------------------------------")
                            Dim billDt = (dr2.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ")


                            Dim query_111 As String = "select a.PLUCode ,(select b.PluName  from dbo.PluMaster b where a.PLUCode =b.PluCode ),a.Quantity ,a.TaxAmount,a.DiscountPerc ,a.DiscountAmt,a.SalePrice*a.Quantity ,a.SalePrice  from dbo.BillTransHistory a where a.CreateDate = '" + billDt + "' and a.BillID =" + billNo + ""
                            cmd2 = New OleDbCommand(query_111, cn1)
                            dr3 = cmd2.ExecuteReader

                            While dr3.Read()

                                Dim ItemCode As String = Convert.ToString(dr3.GetValue(0))
                                Dim lineItemName As String = Convert.ToString(dr3.GetValue(1))
                                Dim lineQuantity As Double = Convert.ToDouble(dr3.GetValue(2))
                                Dim TaxAmt As Double = Convert.ToDouble(dr3.GetValue(3))
                                Dim discountPct As String = Convert.ToString(dr3.GetValue(4))
                                Dim discountVal As String = Convert.ToString(dr3.GetValue(5))
                                Dim unitPrice As Decimal = Convert.ToDecimal(dr3.GetValue(7))
                                Dim lineTotal As Decimal = Convert.ToDecimal(dr3.GetValue(6))

                                'Item(level)
                                line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct.ToString + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString
                                saleQtySum += lineQuantity
                                tax += TaxAmt

                            End While


                            sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + pmode + SEPARATOR_FIELDS
                            Console.WriteLine("Sales RECORD")



                            i = (i + 1)
                            Console.WriteLine("invoice count = " + i.ToString)
                            ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
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
                                invoice_text = ""
                            End If
                        End If

                    Catch ex As Exception
                        WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
                    End Try
                    count = 0

                End If
            Catch ex As Exception
                WriteToEventLog("Exception in sending sales records :: " + ex.ToString)
            End Try
            count = 0

            daysDiff = daysDiff - 1

        Loop Until daysDiff = 0


        '-----------------------------------------------------------------return ----------------------------------------------



        Do
            response = serverUpload("lastuploadinfo.php", params)
            Dim res2() As String = response.Split(",")
            Dim ReturnRes1 As String = res2(1)
            Dim resTP11() As String = ReturnRes1.Split("^")

            retDate = resTP11(0)
            retDate1 = retDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")


            retDateRes = resTP11(0)
            retBill = resTP11(1)

            'Check whether records are present for old date
            Try
                cn = New OleDbConnection(connectString)
                cn.Open()

                Dim querystr As String = "select BillRetID,BillTime,BillDate ,TotalAmount  from dbo.BillRetMasterHistory where BillDate >'" + retDate1 + "'  and BillID >'" + retBill.ToString + "'  order by BillDate ,BillID "

                Console.WriteLine(querystr)
                cmd = New OleDbCommand(querystr, cn)
                dr = cmd.ExecuteReader
                If (dr.HasRows) Then
                    Dim sales_record As String = ""
                    Dim invoice_text As String = ""
                    Dim line_items As String = ""
                    Dim inv_count As Integer = 0

                    While dr.Read()
                        Dim payMode As String = " "
                        Dim docDt As String = dr.GetDateTime(2).ToString("yyyy-MM-dd")
                        Dim billNo As String = dr.GetValue(0).ToString
                        Dim receiptNo As String = dr.GetValue(0).ToString + "," + docDt

                        Dim docTime As String = dr.GetDateTime(1).ToString("hh':'mm':'ss")
                        Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(3))
                        Dim totDisc As String = " "
                        Dim transaction_id As String = (dr.GetDateTime(2)).ToString("yyyy-MM-dd 00':'00':'00.000 ") + "^" + billNo
                        Dim tax As Double = 0
                        Dim saleQtySum As Double = 0
                        Dim pmode As String = " "
                        Dim grossAmount As Double = 0.0
                        Console.WriteLine("-------------------------------------------------------------")
                        Dim billDt = (dr.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ")


                        Dim query_1 As String = "select a.PLUCode,(select b.PluName  from dbo.PluMaster b where a.PLUCode =b.PluCode),Quantity,TaxAmount,BillDiscountPerc,DiscountAmt ,(a.SalePrice*a.Quantity) as lineTotal,SalePrice from dbo.BillRetTransHistory a where a.CreateDate ='" + billDt + "'  and a.BillRetID =" + billNo.ToString + ""
                        cmd = New OleDbCommand(query_1, cn)
                        dr1 = cmd.ExecuteReader

                        While dr1.Read()

                            Dim ItemCode As String = Convert.ToString(dr1.GetValue(0))
                            Dim lineItemName As String = Convert.ToString(dr1.GetValue(1))
                            Dim lineQuantity As Double = Convert.ToDouble(dr1.GetValue(2))
                            Dim TaxAmt As Double = Convert.ToDouble(dr1.GetValue(3))
                            Dim discountPct As String = Convert.ToString(dr1.GetValue(4))
                            Dim discountVal As String = Convert.ToString(dr1.GetValue(5))
                            Dim unitPrice As Decimal = Convert.ToDecimal(dr1.GetValue(7))
                            Dim lineTotal As Decimal = Convert.ToDecimal(dr1.GetValue(6))

                            'Item(level)
                            line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct.ToString + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString
                            saleQtySum += lineQuantity
                            tax += TaxAmt

                        End While


                        sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + pmode + SEPARATOR_FIELDS
                        Console.WriteLine("Return RECORD")



                        i = (i + 1)
                        Console.WriteLine("invoice count = " + i.ToString)
                        ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
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
                            invoice_text = ""
                        End If
                    End If

                Else

                    'incement the date and fetch records from next date

                    Dim incementDate As DateTime
                    Dim incementDate1 As String
lbl1:
                    If (daysDiffRet > 0) Then
                        incementDate = retDate.AddDays(1)
                        incementDate1 = incementDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
                        salesBill = 0
                    ElseIf (daysDiffRet = 0) Then

                        incementDate = retDate
                        incementDate1 = incementDate.ToString("yyyy-MM-dd 00':'00':'00.000 ")
                        response = serverUpload("lastuploadinfo.php", params)
                        Dim res3() As String = response.Split(",")
                        Dim ReturnRes11 As String = res2(1)
                        Dim resTP111() As String = ReturnRes11.Split("^")

                        salesDateRes = resTP111(0)
                        salesBill = resTP111(1)
                    Else

                        Exit Sub

                    End If

                    Try
                        cn1 = New OleDbConnection(connectString)
                        cn1.Open()

                        Dim querystr2 As String = "select BillRetID,BillTime,BillDate ,TotalAmount  from dbo.BillRetMasterHistory where BillDate >'" + retDate1 + "'  and BillID >'" + retBill.ToString + "'  order by BillDate ,BillID "

                        Console.WriteLine(querystr2)
                        cmd2 = New OleDbCommand(querystr2, cn1)
                        dr2 = cmd2.ExecuteReader

                        If (Not dr2.HasRows) Then
                            retDate = retDate.AddDays(1)
                            daysDiffRet = daysDiffRet - 1
                            GoTo lbl1
                        End If

                        Dim sales_record As String = ""
                        Dim invoice_text As String = ""
                        Dim line_items As String = ""
                        Dim inv_count As Integer = 0

                        While dr2.Read()
                            Dim payMode As String = " "
                            Dim docDt As String = dr2.GetDateTime(2).ToString("yyyy-MM-dd")
                            Dim receiptNo As String = dr2.GetValue(0).ToString + "," + docDt
                            Dim billNo As String = dr2.GetValue(0).ToString
                            Dim docTime As String = dr2.GetDateTime(1).ToString("hh':'mm':'ss")
                            Dim totNetDocValue As Decimal = Convert.ToDecimal(dr2.GetValue(3))
                            Dim totDisc As String = " "
                            Dim transaction_id As String = (dr2.GetDateTime(2)).ToString("yyyy-MM-dd 00':'00':'00.000 ") + "^" + billNo
                            Dim tax As Double = 0
                            Dim saleQtySum As Double = 0
                            Dim pmode As String = " "
                            Dim grossAmount As Double = 0.0
                            Console.WriteLine("-------------------------------------------------------------")
                            Dim billDt = (dr2.GetDateTime(1)).ToString("yyyy-MM-dd 00':'00':'00.000 ")


                            Dim query_111 As String = "select a.PLUCode,(select b.PluName  from dbo.PluMaster b where a.PLUCode =b.PluCode),Quantity,TaxAmount,BillDiscountPerc,DiscountAmt ,(a.SalePrice*a.Quantity) as lineTotal,SalePrice from dbo.BillRetTransHistory a where a.CreateDate ='" + billDt + "'  and a.BillRetID =" + billNo + ""
                            cmd2 = New OleDbCommand(query_111, cn1)
                            dr3 = cmd2.ExecuteReader

                            While dr3.Read()

                                Dim ItemCode As String = Convert.ToString(dr3.GetValue(0))
                                Dim lineItemName As String = Convert.ToString(dr3.GetValue(1))
                                Dim lineQuantity As Double = Convert.ToDouble(dr3.GetValue(2))
                                Dim TaxAmt As Double = Convert.ToDouble(dr3.GetValue(3))
                                Dim discountPct As String = Convert.ToString(dr3.GetValue(4))
                                Dim discountVal As String = Convert.ToString(dr3.GetValue(5))
                                Dim unitPrice As Decimal = Convert.ToDecimal(dr3.GetValue(7))
                                Dim lineTotal As Decimal = Convert.ToDecimal(dr3.GetValue(6))

                                'Item(level)
                                line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct.ToString + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString
                                saleQtySum += lineQuantity
                                tax += TaxAmt

                            End While


                            sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + pmode + SEPARATOR_FIELDS
                            Console.WriteLine("Return RECORD")



                            i = (i + 1)
                            Console.WriteLine("invoice count = " + i.ToString)
                            ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
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
                                invoice_text = ""
                            End If
                        End If

                    Catch ex As Exception
                        WriteToEventLog("Exception in sending return records :: " + ex.ToString)
                    End Try
                    count = 0

                End If
            Catch ex As Exception
                WriteToEventLog("Exception in sending return records :: " + ex.ToString)
            End Try
            count = 0

            daysDiffRet = daysDiffRet - 1

        Loop Until daysDiffRet = 0
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
        'Dim url As String = serverProtocol + "://192.168.0.16/phoenix_new/public_html/" + serverPath + "/" + subUrl

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
