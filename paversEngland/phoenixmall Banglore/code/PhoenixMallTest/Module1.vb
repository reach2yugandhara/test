Imports System.Data.OleDb
Imports System.Net
Imports System.Text
Imports System.Collections.Specialized
Imports System.Globalization

Module Module1
    'Code for Chilis
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
        Try
            connectString = RijndaelSimple.Decrypt(connectString, username)

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

        Dim acyear As String = iniProps.Get("acyear")
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

        'Dim format As String = "yyyyMMddHHmmss"
        Dim response As String = serverUpload("lastuploadinfo.php", params)
        Dim res() As String = response.Split(",")
        Dim salesres As String = res(0)
        Dim retres As String = res(1)

        '' FOR FETCHING SALES TRANSACTION FOR GIVEN TIME OF SPAN
        Dim sales_send_count As Integer = 0
        Dim invoices_to_send As SortedList = New SortedList()
        Dim count As Integer = 0
        Dim count1 = 0
        'Dim date1 = DateTime.ParseExact(response, "dd-mm-yyyy hh24:mm:ss", Nothing).ToString("yyyy-MM-dd hh':'mm':'ss")

        '265--return 08-Oct-18
        '2102 -- sales 08-Oct-18
        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            Console.WriteLine("Before qry")
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE,CASE WHEN I.INVC_TYPE =2 OR SUM(T.QTY) <=0 THEN 'Return' ELSE 'Sales' END AS DOCTYPE,SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 AND S.STORE_CODE ='033' and I.POST_DATE > to_date('" + response + "','DD-MON-YYYY HH24:MI:SS') AND I.PROC_STATUS not in (32, 65536,131072) GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE, SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 and I.INVC_TYPE <> 2  AND S.STORE_CODE ='033' and  I.PROC_STATUS not in (32, 65536,131072) and I.INVC_NO>'" + salesres + "' GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE, SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 and I.INVC_TYPE <> 2  AND S.STORE_CODE ='033' and  I.PROC_STATUS not in (32, 65536,131072) and I.POST_DATE > to_date('" + salesres + "','DD-MON-YYYY HH24:MI:SS')GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE,CASE WHEN I.INVC_TYPE =2 OR SUM(T.QTY) <=0 THEN 'Return' ELSE 'Sales' END AS DOCTYPE,SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 AND S.STORE_CODE ='033' and I.POST_DATE > to_date('08-OCT-18') AND I.PROC_STATUS not in (32, 65536,131072) GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE) desc "
            'Dim querystr As String = "select POST_DATE,INVC_NO from INVOICE_V where POST_DATE>'08-OCT-18' "
            Console.WriteLine("after qry")
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()

                Console.WriteLine("in loop")

                Console.WriteLine(String.Format("{0}, {1}", dr(0), dr(1)))

                Console.WriteLine(dr.ToString)
                Dim invoiceNo As String = dr.GetValue(0).ToString
                Console.WriteLine(invoiceNo.ToString)

                Dim invoiceDate As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                ' Dim invoiceDate As String = dr.GetValue(1)
                Console.WriteLine(invoiceDate.ToString)

                'Dim doctype As String = dr.GetValue(2).ToString
                'Console.WriteLine(doctype.ToString)

                Dim docqty As Double = Convert.ToDouble(dr.GetValue(2))
                Console.WriteLine(docqty.ToString)

                Dim docgross As Double = Convert.ToDouble(dr.GetValue(3))
                Console.WriteLine(docgross.ToString)

                Dim invdisc As Double = 0.0
                If dr.GetValue(5).ToString <> "" Then
                    invdisc = Convert.ToDouble(dr.GetValue(4))
                    Console.WriteLine(invdisc.ToString)
                End If

                Dim invamt As Double = 0.0
                'Dim invamt As Double = Convert.ToDouble(dr.GetValue(6))
                If dr.GetValue(5).ToString <> "" Then
                    invamt = Convert.ToDouble(dr.GetValue(5))
                    Console.WriteLine(invamt.ToString)
                End If

                'Dim invamt As Double = 0.0
                'Dim invcal As Double = 0.0
                'Dim invamt As Double = Convert.ToDouble(dr.GetValue(6))
                'If dr.GetValue(6).ToString <> "" Then
                '    invcal = Convert.ToDouble(dr.GetValue(6))
                '    Console.WriteLine(invamt.ToString)
                'End If

                Dim invtax As Double = 0.0
                If dr.GetValue(7).ToString <> "" Then
                    invtax = Convert.ToDouble(dr.GetValue(7))
                    
                End If


                Dim saletransaction_id As String = dr.GetDateTime(1).ToString("dd-MMM-yyyy").ToUpper

                'Console.WriteLine("sales record" + invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS)


                Console.WriteLine("-------------------------------------------------------------")
                '$receiptNo,                      $billDate,                 $billTime,                $grossAmount,                                        $totalAmount,                                 $vatAmount,                     $disountVal, $qty,$transaction_id,$tax_lines_info, $payment_lines_info
                'If (doctype = "Sales") Then
                '    sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                '    Console.WriteLine("SALES RECORD")
                '    Console.WriteLine(sales_record)
                'Else
                '    sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                '    Console.WriteLine("Return RECORD")
                '    Console.WriteLine(sales_record)
                'End If

                sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + saletransaction_id.ToString + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
                i = (i + 1)
                Console.WriteLine("invoice count = " + i.ToString)
                ' Console.WriteLine("Transaction id of sales  ::  " + transaction_id)
                invoice_text += sales_record + SEPARATOR_ITEMLINES + line_items + SEPARATOR_ITEMS

                line_items = ""
                sales_record = ""
                invamt = 0.0
                invcal = 0.0
                invtax = 0.0
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

        '----------------------------------return

        Try
            cn = New OleDbConnection(connectString)
            cn.Open()
            Console.WriteLine("Before qry")
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE,CASE WHEN I.INVC_TYPE =2 OR SUM(T.QTY) <=0 THEN 'Return' ELSE 'Sales' END AS DOCTYPE,SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 AND S.STORE_CODE ='033' and I.POST_DATE > to_date('" + response + "','DD-MON-YYYY HH24:MI:SS') AND I.PROC_STATUS not in (32, 65536,131072) GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE, SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 and I.INVC_TYPE = 2 AND S.STORE_CODE ='033'  AND I.PROC_STATUS not in (32, 65536,131072) and I.INVC_NO>'" + retres + "' GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE, SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 and I.INVC_TYPE = 2 AND S.STORE_CODE ='033'  AND I.PROC_STATUS not in (32, 65536,131072) and  I.POST_DATE > to_date('" + retres + "','DD-MON-YYYY HH24:MI:SS') GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE), I.INVC_NO"
            'Dim querystr As String = "SELECT I.INVC_NO AS BILLNO, TRUNC(I.POST_DATE) AS BILLDATE,CASE WHEN I.INVC_TYPE =2 OR SUM(T.QTY) <=0 THEN 'Return' ELSE 'Sales' END AS DOCTYPE,SUM(T.QTY*I.REPORT_MODIFIER) AS TOTQTY, SUM(T.ORIG_PRICE * T.QTY * I.REPORT_MODIFIER) AS GROSSVAL,(SUM(((T.ORIG_PRICE - T.PRICE) * T.QTY * I.REPORT_MODIFIER ))+ (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS DISCOUNT,(SUM((T.PRICE * T.QTY * I.REPORT_MODIFIER)) - (sum(  NVL(I.DISC_AMT, 0) * I.REPORT_MODIFIER))) AS NETAMT,SUM((T.PRICE*T.QTY * I.REPORT_MODIFIER) - ROUND(NVL((T.PRICE * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) - SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS TAXABLEAMT,SUM(((T.TAX_AMT+T.TAX_AMT2)*T.QTY* I.REPORT_MODIFIER) - ROUND(NVL(((T.TAX_AMT+T.TAX_AMT2) * T.QTY * I.DISC_PERC * I.REPORT_MODIFIER/ 100),0),2)) AS  TAXAMT FROM INVOICE_V I LEFT JOIN CUSTOMER C ON C.CUST_SID = I.CUST_SID INNER JOIN STORE S ON S.STORE_NO = I.STORE_NO AND S.SBS_NO = I.SBS_NO INNER JOIN INVC_ITEM T ON T.INVC_SID = I.INVC_SID WHERE  I.INVC_TYPE IN (0, 2) AND I.HELD <> 1 AND T.QTY <> 0 AND S.STORE_CODE ='033' and I.POST_DATE > to_date('08-OCT-18') AND I.PROC_STATUS not in (32, 65536,131072) GROUP BY I.INVC_SID, TRUNC(I.POST_DATE), I.INVC_NO,I.INVC_TYPE, I.DISC_AMT, S.STORE_CODE ORDER BY TRUNC(I.POST_DATE) desc "
            'Dim querystr As String = "select POST_DATE,INVC_NO from INVOICE_V where POST_DATE>'08-OCT-18' "
            Console.WriteLine("after qry")
            Console.WriteLine(querystr)
            cmd = New OleDbCommand(querystr, cn)
            dr = cmd.ExecuteReader

            Dim sales_record As String = ""
            Dim invoice_text As String = ""
            Dim line_items As String = ""
            Dim inv_count As Integer = 0

            While dr.Read()

                Console.WriteLine("in loop")

                Console.WriteLine(String.Format("{0}, {1}", dr(0), dr(1)))

                Console.WriteLine(dr.ToString)
                Dim invoiceNo As String = dr.GetValue(0).ToString
                Console.WriteLine(invoiceNo.ToString)

                Dim invoiceDate As String = dr.GetDateTime(1).ToString("yyyy-MM-dd")
                ' Dim invoiceDate As String = dr.GetValue(1)
                Console.WriteLine(invoiceDate.ToString)

                'Dim doctype As String = dr.GetValue(2).ToString
                'Console.WriteLine(doctype.ToString)

                Dim docqty As Double = Convert.ToDouble(dr.GetValue(2))
                Console.WriteLine(docqty.ToString)

                Dim docgross As Double = Convert.ToDouble(dr.GetValue(3))
                Console.WriteLine(docgross.ToString)

                Dim invdisc As Double = 0.0
                If dr.GetValue(5).ToString <> "" Then
                    invdisc = Convert.ToDouble(dr.GetValue(4))
                    Console.WriteLine(invdisc.ToString)
                End If

                Dim invamt As Double = 0.0
                'Dim invamt As Double = Convert.ToDouble(dr.GetValue(6))
                If dr.GetValue(5).ToString <> "" Then
                    invamt = Convert.ToDouble(dr.GetValue(5))
                    Console.WriteLine(invamt.ToString)
                End If

                Dim invtax As Double = 0.0
                If dr.GetValue(7).ToString <> "" Then
                    invtax = Convert.ToDouble(dr.GetValue(7))
                    invamt = invamt + invtax
                    Console.WriteLine(invtax.ToString)
                End If
                Dim rettransaction_id As String = dr.GetDateTime(1).ToString("dd-MMM-yyyy").ToUpper

                'Console.WriteLine("sales record" + invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS)


                Console.WriteLine("-------------------------------------------------------------")
                '$receiptNo,                      $billDate,                 $billTime,                $grossAmount,                                        $totalAmount,                                 $vatAmount,                     $disountVal, $qty,$transaction_id,$tax_lines_info, $payment_lines_info
                'If (doctype = "Sales") Then
                '    sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "1" + SEPARATOR_FIELDS
                '    Console.WriteLine("SALES RECORD")
                '    Console.WriteLine(sales_record)
                'Else
                '    sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + transaction_id.ToString + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS
                '    Console.WriteLine("Return RECORD")
                '    Console.WriteLine(sales_record)
                'End If

                sales_record = invoiceNo.ToString + SEPARATOR_FIELDS + invoiceDate.ToString + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS + docgross.ToString() + SEPARATOR_FIELDS + invamt.ToString() + SEPARATOR_FIELDS + invtax.ToString() + SEPARATOR_FIELDS + invdisc.ToString() + SEPARATOR_FIELDS + docqty.ToString() + SEPARATOR_FIELDS + rettransaction_id.ToString + SEPARATOR_FIELDS + "2" + SEPARATOR_FIELDS + " " + SEPARATOR_FIELDS + "" + SEPARATOR_FIELDS
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
        Dim url As String = serverProtocol + "://phoenixmall.onintouch.com/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.11:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'Dim url As String = serverProtocol + "://192.168.0.13:8080/phoenix_new/public_html/" + serverPath + "/" + subUrl
        'url = UrlAppend(url, subUrl)
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
