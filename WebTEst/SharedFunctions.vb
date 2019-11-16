Imports System.Net

Module SharedFunctions
    Public Sub Record_error(ByVal er As String)
        '寫入錯誤訊息
        Dim dc As New WebDataClasses1DataContext
        Dim newErr As New log_Err With {
            .error_date = Now,
            .application_name = My.Application.Info.ProductName + " V" + My.Application.Info.Version.ToString,
            .machine_name = Dns.GetHostName,
            .ip_address = Dns.GetHostEntry(Dns.GetHostName).AddressList(0).ToString(),
            .userid = "Claudia",
            .error_message = er
        }
        dc.log_Err.InsertOnSubmit(newErr)
        dc.SubmitChanges()
    End Sub

    Public Sub Record_adm(ByVal op As String, ByVal des As String)
        '寫入作業訊息
        Dim dc As New WebDataClasses1DataContext
        Dim newLog As New log_Adm With {
            .regdate = Now,
            .application_name = My.Application.Info.ProductName + " V" + My.Application.Info.Version.ToString,
            .machine_name = Dns.GetHostName,
            .ip_address = Dns.GetHostEntry(Dns.GetHostName).AddressList(0).ToString(),
            .userid = "Claudia",
            .operation_name = op,
            .description = des
        }
        dc.log_Adm.InsertOnSubmit(newLog)
        dc.SubmitChanges()
    End Sub

    Public Function MakeSure_UID(ByVal tempUID) As String
        Dim inter_UID As String = ""
        Dim o As String = tempUID
        Dim dc As New WebDataClasses1DataContext
        Dim I4 As String = tempUID.Substring(0, 4)
        Dim F3 As String = tempUID.Substring(7, 3)

        ' 找到正確的身分證號碼, 1. 從C:\vpn\current_uid.txt, 90%情形
        If System.IO.File.Exists("C:\vpn\current_uid.txt") Then
            Dim sr As New System.IO.StreamReader("C:\vpn\current_uid.txt")
            inter_UID = sr.ReadLine()
            sr.Close()
            If inter_UID.Substring(0, 4) = I4 And inter_UID.Substring(7, 3) = F3 Then
                ' 要確認不要確認?
                ' 在看診情況下,這是90%的狀況
                ' passed test
                o = inter_UID
                Return o
            End If
        End If
        ' 如果沒有使用companion, 或是用別人的健保卡,單獨要查詢
        Dim q = From p In dc.tbl_patients Where p.uid.Substring(0, 4) = I4 And p.uid.Substring(7, 3) = F3 Select p.uid, p.cname     ' this is a querry

        Select Case q.Count
            Case 1
                ' passed test
                o = q.ToList(0).uid
            Case 0
                ' passed test
                Dim answer As String = ""
                Do While answer.Length <> 3
                    answer = InputBox("請補入中間三碼" + I4 + " _ _ _ " + F3)
                Loop
                o = I4 + answer + F3
            Case Else

                Dim qu As String = "請選擇" + vbCrLf
                For i = 0 To q.Count - 1
                    qu += (i + 1).ToString + ". " + q.ToList(i).uid + " " + q.ToList(i).cname + vbCrLf
                Next
                Dim answer As String = "0"
                Do Until (CInt(answer) > 0 And CInt(answer) < (q.Count + 1))
                    answer = InputBox(qu)
                    If Not IsNumeric(answer) Then
                        answer = "0"
                    End If
                Loop
                o = q.ToList(CInt(answer) - 1).uid

        End Select


        Return o
    End Function


End Module
