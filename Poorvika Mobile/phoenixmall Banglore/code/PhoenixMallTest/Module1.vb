Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized

Module Module1
    'Code for Poorvika
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
    Dim rdr As OleDbDataReader = Nothing
    Dim rdr1 As OleDbDataReader = Nothing
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
        'connectString = "Provider=SQLOLEDB;Server=192.168.1.18;Database=DEAL1004;User Id=mall;Password=;"
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
        Console.WriteLine("hi")
        Dim response As String = serverUpload("lastuploadinfo.php", params)
        Dim res() As String = response.Split(",")
        Dim salesres As String = res(0)
        Dim retres As String = res(1)
        'Dim response As String = response1.ToString("yyyy-MM-dd hh':'mm':'ss tt")
        Console.WriteLine("response ::::  " + response.ToString)



        Debug("lastuploadinfo=" + response)

        'FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        'Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0


        '-----------------------------------------------------------------return ----------------------------------------------

        sales_send_count = 0
        'invoices_to_send = New SortedList()
        Console.WriteLine("return  Response :: " + retres)
        count1 = 0
        Try
            cn = New OleDbConnection(connectString)
            cn.Open()


            'Dim retQuery As String = "select BillRetID,BillRetTime,GrossAmt,NetAmtPayable,ODiscountAmt from dbo.HH_BILL_RET_MASTER_TBL where BillRetTime > '" + retres + "' order by BillRetTime "

            'Dim retQuery As String = "select RECEIPT_NO,DATE_sTAMP,SUM(ITEM_QTY) AS TOT_QTY,SUM(ITEM_PRICE) AS GROSS_AMT,SUM(TAX_AMT) AS TAX_AMT,SUM(dIS_AMT) AS DIS_AMT,SUM(nET_AMT) AS BILL_AMT from [dbo].[THIRD_BLR_PHOENIX_MALL_ITEMS_SALES_RETURN] where date_stamp >= '" + retres + "' GROUP BY RECEIPT_NO,DATE_STAMP order by date_Stamp"
            Dim retQuery As String = "select RECEIPT_NO,DATE_STAMP,SUM(ITEM_QTY) AS TOT_QTY,SUM(ITEM_PRICE) AS GROSS_AMT,SUM(TAX_AMT) AS TAX_AMT,SUM(DIS_AMT) AS DIS_AMT,SUM(NET_AMT) AS BILL_AMT from [dbo].[THIRD_KL_MALL_TRAVANCORE_MALL_ITEMS_SALES_RETURN] where DATE_STAMP >= '" + retres + "' GROUP BY RECEIPT_NO,DATE_STAMP order by DATE_STAMP"
            cmd = New OleDbCommand(retQuery, cn)
            rdr = cmd.ExecuteReader
            count = 0
            Console.WriteLine(retQuery)

            Dim return_record As String = ""
            Dim ret_invoice_text As String = ""
            Dim ret_line_items As String = ""
            Dim ret_inv_count As Integer = 0

            While rdr.Read()
                Dim payMode As String = ""
                Dim retReceiptNo As String = rdr.GetValue(0).ToString

                Dim retDocDt1 As String = rdr.GetValue(1).ToString
                Dim retDocDt11 As String = retDocDt1.Insert(4, "-")
                Dim retDocDt12 As String = retDocDt11.Insert(7, "-")
                Dim retDocDt As String = retDocDt12
                Console.WriteLine(retDocDt)
                Dim retDocTime As String = "00:00"
                Dim retTotNetDocValue As Decimal = Convert.ToDecimal(rdr.GetValue(6))
                Dim retTotDisc As String = Convert.ToString(rdr.GetValue(5))
                Dim retTransaction_id As String = rdr.GetValue(1).ToString
                Console.WriteLine("-------------------------------------------------------------")

                Dim retTax As Double = Convert.ToDouble(rdr.GetValue(4))
                Dim retQtySum As Double = rdr.GetValue(2).ToString
                Dim retPmode As String = " "
                Dim retGrossAmount As Double = Convert.ToDouble(rdr.GetValue(3))



                'Dim retQuery_1 As String = "select PLUCode,ItemName,RetQty,RetTaxAmt+LSurTaxAmt+TaxAmt3+TaxAmt4 as Tax,LDiscountPerc,RetLDiscountAmt,RetExtendAmt,SellingRate from dbo.HH_BILL_RET_TRANS_TBL where BillRetID='" + retReceiptNo + "'"
                'cmd = New OleDbCommand(retQuery_1, cn)
                'rdr1 = cmd.ExecuteReader

                'While rdr1.Read()

                Dim retItemCode As String = ""
                Dim retLineItemName As String = ""
                Dim retLineQuantity As Double = 0.0
                Dim retTaxAmt As Double = 0.0
                Dim retDiscountPct As String = ""
                Dim retDiscountVal As String = ""
                Dim retUnitPrice As Decimal = 0.0
                Dim retLineTotal As Decimal = 0.0

                'Item(level)
                ret_line_items += retItemCode.ToString + SEPARATOR_FIELDS + retLineItemName + SEPARATOR_FIELDS + retLineQuantity.ToString + SEPARATOR_FIELDS + retTaxAmt.ToString + SEPARATOR_FIELDS + retDiscountPct.ToString + SEPARATOR_FIELDS + retDiscountVal.ToString + SEPARATOR_FIELDS + retLineTotal.ToString + SEPARATOR_FIELDS + retUnitPrice.ToString


                ' End While


                return_record = retReceiptNo + SEPARATOR_FIELDS + retDocDt + SEPARATOR_FIELDS + retDocTime + SEPARATOR_FIELDS + retGrossAmount.ToString + SEPARATOR_FIELDS + retTotNetDocValue.ToString() + SEPARATOR_FIELDS + retTax.ToString() + SEPARATOR_FIELDS + retTotDisc.ToString() + SEPARATOR_FIELDS + retQtySum.ToString() + SEPARATOR_FIELDS + retTransaction_id + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + retTax.ToString + SEPARATOR_FIELDS + retPmode + SEPARATOR_FIELDS
                Console.WriteLine("RETURN RECORD")


                j = (j + 1)
                Console.WriteLine(" Return invoice count = " + j.ToString)
                ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
                ret_invoice_text += return_record + SEPARATOR_ITEMLINES + ret_line_items + SEPARATOR_ITEMS
                ret_line_items = ""
                return_record = ""
                ret_inv_count = ret_inv_count + 1
                If (j = NumRecordsPerBatch) Then

                    If ret_invoice_text.Length >= 5 Then
                        ret_invoice_text = Left(ret_invoice_text, Len(ret_invoice_text) - 5)
                        params.Add("salesbatch", ret_invoice_text)
                        ' Console.WriteLine("invoice text " + invoice_text)
                        response = serverUpload("savebatch.php", params)
                        params.Remove("salesbatch")
                        Console.WriteLine("Response ->" + response)
                        ret_inv_count = 0
                        ret_invoice_text = ""
                        return_record = ""
                        j = 0
                    End If
                End If
            End While


            If (j > 0) Then
                If ret_invoice_text.Length >= 5 Then
                    ret_invoice_text = Left(ret_invoice_text, Len(ret_invoice_text) - 5)
                    params.Add("salesbatch", ret_invoice_text)
                    ret_inv_count = ret_inv_count + 1
                    'Console.WriteLine(invoice_text)
                    response = serverUpload("savebatch.php", params)
                    params.Remove("salesbatch")
                    Console.WriteLine("Response ->" + response)
                    ret_invoice_text = ""
                End If
            End If

        Catch ex As Exception
            WriteToEventLog("Exception in sending return records :: " + ex.ToString)
        End Try

        count = 0


        Try
            cn = New OleDbConnection(connectString)
            cn.Open()

            'Dim querystr As String = "select SONo,BillTime,GrossAmt,NetAmtPayable,ODiscountAmt from dbo.HH_BILL_MASTER_HIST_TBL where BillTime > '" + salesres + "' order by BillTime"
            'Dim querystr As String = "select RECEIPT_NO,DATE_sTAMP,SUM(ITEM_QTY) AS TOT_QTY,SUM(ITEM_PRICE) AS GROSS_AMT,SUM(TAX_AMT) AS TAX_AMT,SUM(dIS_AMT) AS DIS_AMT,SUM(nET_AMT) AS BILL_AMT from [dbo].[THIRD_BLR_PHOENIX_MALL_ITEMS_SALES] where date_stamp >= '" + salesres + "' GROUP BY RECEIPT_NO,DATE_STAMP order by date_Stamp "
            Dim querystr As String = "select RECEIPT_NO,DATE_STAMP,SUM(ITEM_QTY) AS TOT_QTY,SUM(ITEM_PRICE) AS GROSS_AMT,SUM(TAX_AMT) AS TAX_AMT,SUM(DIS_AMT) AS DIS_AMT,SUM(NET_AMT) AS BILL_AMT from [dbo].[THIRD_KL_MALL_TRAVANCORE_MALL_ITEMS_SALES] where DATE_STAMP >= '" + salesres + "' GROUP BY RECEIPT_NO,DATE_STAMP order by DATE_STAMP "
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()
                Dim payMode As String = ""
                Dim receiptNo As String = dr.GetValue(0).ToString
                Dim docDt1 As String = dr.GetValue(1).ToString
                Dim docDt11 As String = docDt1.Insert(4, "-")
                Dim docDt12 As String = docDt11.Insert(7, "-")
                Dim docDt As String = docDt12
                Dim docTime As String = "00:00"
                Console.WriteLine(docDt)
                Console.WriteLine(docTime)
                Dim totNetDocValue As Decimal = Convert.ToDecimal(dr.GetValue(6))
                Dim totDisc As String = Convert.ToString(dr.GetValue(5))
                Dim transaction_id As String = dr.GetValue(1).ToString
                Console.WriteLine("-------------------------------------------------------------")

                Dim tax As Double = Convert.ToDouble(dr.GetValue(4))
                Dim saleQtySum As Double = Convert.ToDouble(dr.GetValue(2))
                Dim pmode As String = " "
                Dim grossAmount As Double = Convert.ToDouble(dr.GetValue(3))



                'Dim query_1 As String = "select PLUCode,ItemName,Qty,TaxAmt,LDiscountPerc,LDiscountAmt,SellingRate,Extend from dbo.HH_SALEORDER_TRANS_HIST_TBL where SONo='" + receiptNo + "'"
                'cmd = New OleDbCommand(query_1, cn)
                'dr1 = cmd.ExecuteReader

                'While dr1.Read()

                Dim ItemCode As String = ""
                Dim lineItemName As String = ""
                Dim lineQuantity As Double = 0.0
                Dim TaxAmt As Double = 0.0
                Dim discountPct As String = ""
                Dim discountVal As String = 0.0
                Dim unitPrice As Decimal = 0.0
                Dim lineTotal As Decimal = 0.0

                'Item(level)
                line_items += ItemCode.ToString + SEPARATOR_FIELDS + lineItemName + SEPARATOR_FIELDS + lineQuantity.ToString + SEPARATOR_FIELDS + TaxAmt.ToString + SEPARATOR_FIELDS + discountPct.ToString + SEPARATOR_FIELDS + discountVal.ToString + SEPARATOR_FIELDS + lineTotal.ToString + SEPARATOR_FIELDS + unitPrice.ToString



                'End While


                sales_record = receiptNo + SEPARATOR_FIELDS + docDt + SEPARATOR_FIELDS + docTime + SEPARATOR_FIELDS + grossAmount.ToString + SEPARATOR_FIELDS + totNetDocValue.ToString() + SEPARATOR_FIELDS + tax.ToString() + SEPARATOR_FIELDS + totDisc.ToString() + SEPARATOR_FIELDS + saleQtySum.ToString() + SEPARATOR_FIELDS + transaction_id + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + tax.ToString + SEPARATOR_FIELDS + pmode + SEPARATOR_FIELDS
                Console.WriteLine("SALES RECORD")



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
        'Dim url As String = serverProtocol + "://192.168.0.16/phoenix_new/public_html/" + serverPath + "/" + subUrl
        Dim url As String = serverProtocol + "://101.53.131.55:8080/ppz/public_html/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.16/ascendas/home/" + serverPath + "/" + subUrl
        ' url = UrlAppend(url, subUrl)
        Console.WriteLine("URL is: " + url)
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
