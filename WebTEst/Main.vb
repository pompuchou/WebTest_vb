Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Resources

Public Class Main
    ' 20191001 created
    ' 20191004: 增加Hotkey, 找到正確身分證, 紀錄此事件, 倒入正式資料庫(大表)
    '           比較指標,看看使用程式與手動查詢的差別, 20191005 已找到
    ' 20191006: 完成lab, 第二頁以上, 匯入大表, 紀錄
    ' 20191008: DEBUG
    ' 20191018: DEBUG 2, 用cursor基本解決重複問題, 還有限定在前面11頁, 嘗試解決缺頁問題,看看有沒有效
    ' 20191019: 增加手術, 出院病摘, 復健醫療, 考慮中醫; 一鍵式
    ' 20191021: 增加中醫, 一鍵式成功, 仍有no lab, no source, no query現象
    ' 20191021: no lab, no source, no query發生在多頁面, 可能是沒有time out的問題, 加了time out, failed
    ' 1.0.0.19 才可以用,之後的都是不行的
    ' 1.0.1.14 重大改正, 此外改正p_source的輸入
    ' 1.0.1.15 可使用版本
    ' 1.0.1.18 20191025: 可以檔案輸入, 手術, 出院可以寫入資料庫了
    ' 1.0.1.20 20191025: 加入牙醫, 過敏
    ' 1.0.1.21 20191026: 加入復健, 中醫
    ' 20191028: 修改小bug, 關懷名單不能輸入; Write_DIS=>Write_REH, 可以成功寫入
    Private strUID As String
    Private Property Pageready As Boolean = False
    Public Const MOD_WINKEY As Integer = &H8 'Alt key for hotkey
    Public Const WM_HOTKEY As Integer = &H312   'Hotkey

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        RegisterHotKey(Me.Handle, 101, MOD_WINKEY, Keys.G)
        RegisterHotKey(Me.Handle, 102, MOD_WINKEY, Keys.Y)
        RegisterHotKey(Me.Handle, 103, MOD_WINKEY, Keys.H)
        Record_adm("Webkit Log in", "")
    End Sub

    Private Sub Form1_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Record_adm("Webkit Log out", "")
    End Sub

    Private Sub CloudToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CloudToolStripMenuItem.Click
        Me.WebBrowser1.Navigate("https://medcloud.nhi.gov.tw/imme0008/IMME0008S01.aspx")
    End Sub

    Private Sub SaveToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles SaveToolStripMenuItem1.Click
        ' how to click?
        ' 可以用invokeMember("Click"), 但是要按在下一層
        ' ContentPlaceHolder1_li_0008 沒有用
        ' ContentPlaceHolder1_a_0008 才有動作
        ' ContentPlaceHolder1_a_0008 是雲端藥歷
        ' ContentPlaceHolder1_a_0009 是特定管制藥品用藥資訊
        ' ContentPlaceHolder1_a_0010 是檢查檢驗紀錄
        ' ContentPlaceHolder1_a_0020 是手術明細紀錄
        ' ContentPlaceHolder1_a_0030 是牙科處置及手術
        ' ContentPlaceHolder1_a_0040 是過敏藥
        ' ContentPlaceHolder1_a_0060 是檢查檢驗結果
        ' ContentPlaceHolder1_a_0070 是出院病歷摘要
        ' ContentPlaceHolder1_a_0080 是復健醫療
        ' ContentPlaceHolder1_a_0090 是中醫用藥
        ' ContentPlaceHolder1_a_0110 是CDC預防接種
        Me.NotifyIcon1.ShowBalloonTip(2000, "hello", "this is a test", ToolTipIcon.Info)
    End Sub

    Private Sub FileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileToolStripMenuItem.Click
        Retrieve_from_file()
    End Sub

    Private Sub Retrieve_from_file()
#Region "Declaration"
        Dim loadpath As String()
        Dim filename As String()
        Dim f As String()
        Dim html As HtmlElement = Nothing
#End Region

#Region "讀取檔案路徑"
        ' 讀取要輸入的位置
        ' 只有一種html格式
        ' html格式的index=1
        Me.OpenFileDialog1.FilterIndex = 1
        Me.OpenFileDialog1.FileName = ""
        If Me.OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            loadpath = Me.OpenFileDialog1.FileNames
            filename = Me.OpenFileDialog1.SafeFileNames
        Else
            ' 取消, 什麼也沒有做
            Exit Sub
        End If
#End Region

        ' 檔案一個一個處理
        For i = 0 To loadpath.Length - 1
#Region "取得HTML"
            f = filename(i).Replace(".html", "").Split("_")
            ' f(0) 是類別, 例如discharge, OP, schedule, rehab, TCM,
            ' f(1) 是日期
            ' f(2) 是時間
            ' f(3) 是身分證字號
#End Region

            Me.WebBrowser1.Navigate(loadpath(i))
            WaitForPageLoad()

            If f.Length = 4 Then
                ' set UID
                strUID = f(3)

                ' send to proper sub
                Select Case f(0)
                    Case "OP"   '手術
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvList")
                        Write_OP(html)
                    Case "discharge"    '出院病摘
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvList")
                        Write_DIS(html)
                    Case "Allergy"  '過敏
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvList")
                        Write_ALL(html)
                    Case "dental"   '牙醫
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvList")
                        Write_Dental(html)
                    Case "rehab"    '復健
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvList")
                        Write_REH(html)
                    Case "schedule" '管制藥物關懷名單
                        html = Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_divResult")
                        Write_sch_re(html.Children(0))
                        Write_sch_up(html.Children(1))
                    Case "TCM"  '中醫
                        Write_TCM_GR(Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvGroup"))
                        Write_TCM_DE(Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_gvDetail"))
                    Case Else
                End Select

                strUID = ""
            Else
                ' 這裡來搞D.html的輸入
                Parsing_D(Me.WebBrowser1.Document)
            End If

        Next
    End Sub

    Private Sub Parsing_D(ByRef html As System.Windows.Forms.HtmlDocument)
        Dim uid As HtmlElementCollection = html.GetElementsByTagName("span")
        Dim tab As HtmlElementCollection = html.GetElementsByTagName("table")
        Dim u_n As Int16 = uid.Count
        Dim t_n As Int16 = tab.Count
        Dim header_want As String() = {"項次", "來源", "主診斷", "ATC3名稱", "ATC5名稱", "成分名稱", "藥品健保代碼", "藥品名稱",
                "用法用量", "給藥日數", "藥品用量", "就醫(調劑)日期(住院用藥起日)", "慢連箋領藥日(住院用藥迄日)", "慢連箋原處方醫事機構代碼"}
        Dim dc As New WebDataClassesDataContext
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim current_time As Date = Now

        If u_n <> t_n Then
            MessageBox.Show("UID table mismatch" + vbCrLf + u_n.ToString + vbCrLf + t_n.ToString)
            Exit Sub
        End If
        For i = 0 To u_n - 1
            strUID = MakeSure_UID(uid(i).InnerText)

            ' 找出要的順序
            order_n = 0
            For Each th As HtmlElement In tab(i).GetElementsByTagName("th")
                For j = 0 To header_want.Count - 1
                    If th.InnerText.Replace(vbCrLf, "") = header_want(j) Then
                        header_order.Add(j)
                        Exit For
                    End If
                Next
                If header_order.Count = order_n Then
                    header_order.Add(-1)
                End If
                order_n += 1
            Next
            current_time = Now
            Write_med(tab(i), header_order, current_time)

            ' 匯入大表
            Try
                dc.sp_insert_tbl_cloudmed(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            Try
                dc.sp_insert_p_cloudmed(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            ' 這裡原本多了一次沒有try包覆的insert_p_cloudmed, 一但p_cloudmed有錯誤就沒辦法處理source
            ' 處理source
            Dim q = (From p In dc.tbl_cloudmed_temp Where p.QDATE = current_time Select p.source).Distinct ' this is a query
            Dim r = q.ToList
            Dim s As String()
            For k = 0 To r.Count - 1
                s = r(k).Split(vbCrLf)
                Dim qq = (From pp In dc.p_source Where pp.source_id = s(2).Substring(1) Select pp)
                If qq.Count = 0 Then
                    Try
                        Dim so As New p_source With {.source_id = s(2).Substring(1), .[class] = s(1).Substring(1), .source_name = s(0)}
                        dc.p_source.InsertOnSubmit(so)
                        dc.SubmitChanges()
                    Catch ex As Exception
                        ' do nothing
                        '                    Record_error(ex.Message)
                    End Try
                End If
            Next

            GetNotifyIcon1().ShowBalloonTip(1000, u_n, (i + 1), ToolTipIcon.Info)
            strUID = ""
        Next
        MessageBox.Show("成功完成")
    End Sub

#Region "Hot key"
    <DllImport("User32.dll")>
    Public Shared Function RegisterHotKey(ByVal hwnd As IntPtr,
                        ByVal id As Integer, ByVal fsModifiers As Integer,
                        ByVal vk As Integer) As Integer
    End Function

    <DllImport("User32.dll")>
    Public Shared Function UnregisterHotKey(ByVal hwnd As IntPtr,
                        ByVal id As Integer) As Integer
    End Function

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = WM_HOTKEY Then
            Dim id As IntPtr = m.WParam
            Select Case (id.ToString)
                Case "101"
                    Query()
                Case "102"
                    Reflesh()
                Case "103"
                    Reflesh()
                    Query()
            End Select
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        UnregisterHotKey(Me.Handle, 101)
        UnregisterHotKey(Me.Handle, 102)
        UnregisterHotKey(Me.Handle, 103)
    End Sub

#End Region

#Region "Query"
    Private Sub Reflesh()
        Me.WebBrowser1.Navigate("https://medcloud.nhi.gov.tw/imme0008/IMME0008S01.aspx")
        WaitForPageLoad()
        GetNotifyIcon1().ShowBalloonTip(1000, "更新成功", "完成", ToolTipIcon.Info)
    End Sub

    Private Function GetNotifyIcon1() As NotifyIcon
        Return Me.NotifyIcon1
    End Function

    Private Sub Query()
        ' 找到身分證字號
        ' 20191004 夜, 試驗成功
        If Me.WebBrowser1.Document.GetElementById("ContentPlaceHolder1_lbluserID") Is Nothing Then
            ' avoid error
            GetNotifyIcon1().ShowBalloonTip(1000, "更新失敗", "Not working", ToolTipIcon.Info)
            Exit Sub
        End If
        Dim tempUID As String = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_lbluserID").InnerText
        strUID = MakeSure_UID(tempUID)
        ' 顯示成功更新
        GetNotifyIcon1().ShowBalloonTip(1000, "取得身分證字號", strUID, ToolTipIcon.Info)

        Try
            Dim bMED As Boolean = Retrieve_Med()
            Dim bSCH As Boolean = Check_schedule()
            Dim bLAB As Boolean = Retrieve_Lab()
            Dim bOPE As Boolean = Retrieve_OP()
            Dim bDIS As Boolean = Retrieve_discharge()
            Dim bDEN As Boolean = Retrieve_Den()
            Dim bALL As Boolean = Retrieve_ALL()
            Dim bREH As Boolean = Retrieve_rehab()
            Dim bTCM As Boolean = Retrieve_TCM()

            Dim dc As New WebDataClassesDataContext
            Dim new_query As New tbl_Query
            With new_query
                .uid = strUID
                .QDATE = Now
                .if_cloudlab = bLAB
                .if_schedule = bSCH
                .if_cloudmed = bMED
                .OP = bOPE
                .discharge = bDIS
                .rehab = bREH
                .if_TCM = bTCM
                .if_dental = bDEN
                .if_allergy = bALL
            End With
            dc.tbl_Query.InsertOnSubmit(new_query)
            dc.SubmitChanges()
            GetNotifyIcon1().ShowBalloonTip(1000, "寫入成功", strUID, ToolTipIcon.Info)
        Catch ex As Exception
            GetNotifyIcon1().ShowBalloonTip(1000, "寫入失敗", strUID, ToolTipIcon.Info)
            Record_error(ex.Message)
        End Try
        strUID = ""
    End Sub

    Private Function Retrieve_Med() As Boolean
        ' 找到有幾個tab
        Dim htmlTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_tab")

        ' 不是每個人都有雲端藥歷的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0008")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmlgvList As HtmlElement
            Dim pg As HtmlElement
            Dim pg_N As Int16 = 1
            Dim header_want As String() = {"項次", "來源", "主診斷", "ATC3名稱", "ATC5名稱", "成分名稱", "藥品健保代碼", "藥品名稱",
                "用法用量", "給藥日數", "藥品用量", "就醫(調劑)日期(住院用藥起日)", "慢連箋領藥日(住院用藥迄日)", "慢連箋原處方醫事機構代碼"}
            Dim dc As New WebDataClassesDataContext
            Dim header_order As New List(Of Int16)
            Dim order_n As Int16 = 0
            Dim current_time As Date = Now
#End Region

#Region "Prepare"
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0008").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' 取得gvList
            htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
            If htmlgvList Is Nothing Then
                Return False
            End If
            ' 找出要的順序
            order_n = 0
            For Each th As HtmlElement In htmlgvList.GetElementsByTagName("th")
                For i = 0 To header_want.Count - 1
                    If th.InnerText.Replace(vbCrLf, "").Replace(" ", "") = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                Next
                If header_order.Count = order_n Then
                    header_order.Add(-1)
                End If
                order_n += 1
            Next
            ' 找到雲端藥歷有幾頁
            pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
            If pg IsNot Nothing Then
                ' 有ContentPlaceHolder1_pg_gvList, 表示有多頁
                pg_N = pg.Children.Count - 5
                ' 按下日期排序, 有多頁才需要排序
                For Each th As HtmlElement In htmlgvList.GetElementsByTagName("th")
                    If th.InnerText = "就醫(調劑)日期(住院用藥起日)" Then
                        th.Children(0).InvokeMember("Click")
                        WaitForPageLoad()
                        Exit For
                    End If
                Next
                htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
            Else
                ' 沒有ContentPlaceHolder1_pg_gvList, 表示只有ㄧ頁
                pg_N = 1
            End If
#End Region

#Region "Write"
            ' 讀取第一頁
            Write_med(htmlgvList, header_order, current_time)

            ' 讀取第二至最後一頁
            ' FOR NEXT
            If pg_N > 1 Then
                pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
                For i = 1 To pg_N - 1
                    ' 按按鈕, 第二頁是i+2, 這裡犯了錯誤, 20191022改正, 照鴻林的健保卡在此犯錯,沒有通過
                    pg.Children.Item(i + 2).InvokeMember("Click")
                    WaitForPageLoad()
                    Threading.Thread.Sleep(500)
                    htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
                    ' 找到雲端藥歷有幾頁
                    pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
                    Write_med(htmlgvList, header_order, current_time)
                Next
            End If
#End Region

#Region "Ending"
            ' 匯入大表
            Try
                dc.sp_insert_tbl_cloudmed(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            Try
                dc.sp_insert_p_cloudmed(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            ' 這裡原本多了一次沒有try包覆的insert_p_cloudmed, 一但p_cloudmed有錯誤就沒辦法處理source
            ' 處理source
            Dim q = (From p In dc.tbl_cloudmed_temp Where p.QDATE = current_time Select p.source).Distinct ' this is a query
            Dim r = q.ToList
            Dim s As String()
            For i = 0 To r.Count - 1
                s = r(i).Split(vbCrLf)
                ' source_id s(2).substring(1)
                ' class s(1).substring(1)
                ' source_name s(0)
                Dim qq = (From pp In dc.p_source Where pp.source_id = s(2).Substring(1) Select pp)
                If qq.Count = 0 Then
                    Try
                        Dim so As New p_source With {.source_id = s(2).Substring(1), .[class] = s(1).Substring(1), .source_name = s(0)}
                        dc.p_source.InsertOnSubmit(so)
                        dc.SubmitChanges()
                    Catch ex As Exception
                        ' do nothing
                        '                    Record_error(ex.Message)
                    End Try
                End If
            Next

            ' 作紀錄
            GetNotifyIcon1().ShowBalloonTip(1000, "雲端藥歷", strUID, ToolTipIcon.Info)
            Return True
#End Region
        End If
        Return False
    End Function

    Private Sub Write_med(ByRef html As HtmlElement, ByRef header_order As List(Of Int16), ByRef current_time As DateTime)
        Dim dc As New WebDataClassesDataContext
        Dim order_n As Int16 = 0
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            Dim newCloud As New tbl_cloudmed_temp With {.uid = strUID, .QDATE = current_time}
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '項次
                        If td.InnerText IsNot Nothing Then
                            newCloud.item_n = td.InnerText
                        End If
                    Case 1
                        If td.InnerText IsNot Nothing Then
                            newCloud.source = td.InnerText
                        End If
                    Case 2
                        If td.InnerText IsNot Nothing Then
                            newCloud.diagnosis = td.InnerText
                        End If
                    Case 3
                        If td.InnerText IsNot Nothing Then
                            newCloud.atc3 = td.InnerText
                        End If
                    Case 4
                        If td.InnerText IsNot Nothing Then
                            newCloud.atc5 = td.InnerText
                        End If
                    Case 5
                        If td.InnerText IsNot Nothing Then
                            newCloud.comp = td.InnerText
                        End If
                    Case 6
                        If td.InnerText IsNot Nothing Then
                            newCloud.NHI_code = td.InnerText
                        End If
                    Case 7
                        If td.InnerText IsNot Nothing Then
                            newCloud.drug_name = td.InnerText
                        End If
                    Case 8
                        If td.InnerText IsNot Nothing Then
                            newCloud.dosing = td.InnerText
                        End If
                    Case 9
                        If td.InnerText IsNot Nothing Then
                            newCloud.days = td.InnerText
                        End If
                    Case 10
                        If td.InnerText IsNot Nothing Then
                            newCloud.amt = td.InnerText
                        End If
                    Case 11
                        If td.InnerText IsNot Nothing Then
                            Dim temp_d As String() = td.InnerText.Split("/")
                            newCloud.SDATE = CStr(CInt(temp_d(0)) + 1911) + "/" + temp_d(1) + "/" + temp_d(2)
                        End If
                    Case 12
                        If td.InnerText IsNot Nothing Then
                            Dim temp_d As String() = td.InnerText.Split("/")
                            newCloud.EDATE = CStr(CInt(temp_d(0)) + 1911) + "/" + temp_d(1) + "/" + temp_d(2)
                        End If
                    Case 13
                        If td.InnerText IsNot Nothing Then
                            newCloud.o_source = td.InnerText
                        End If
                    Case Else
                End Select
                order_n += 1
            Next
            dc.tbl_cloudmed_temp.InsertOnSubmit(newCloud)
            dc.SubmitChanges()
        Next
    End Sub

    Private Function Check_schedule() As Boolean
        ' 有興趣的就是雲端藥歷, 檢查檢驗結果, 跟關懷名單三種
        ' 不是每個人都有關懷名單的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0009")
        If queryTAB IsNot Nothing Then
            ' Declaration
            Dim htmldivResult As HtmlElement

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0009").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            '            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_PanS01")
            ' PanS01有點問題
            ' 跟人家不一樣,是在divResult
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                '製作自動檔名
                Dim temp_filepath As String = "C:\vpn\html"
                '存放目錄,不存在就要建立一個
                If Not (System.IO.Directory.Exists(temp_filepath)) Then
                    System.IO.Directory.CreateDirectory(temp_filepath)
                End If
                '自動產生名字
                temp_filepath += "\schedule_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                temp_filepath += "_" + strUID
                temp_filepath += ".html"

                '製作html檔 writing to html
                Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                sw.Write(htmldivResult.OuterHtml)
                sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_sch_re(htmldivResult.Children(0))
                    Write_sch_up(htmldivResult.Children(1))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region

                GetNotifyIcon1().ShowBalloonTip(1000, "關懷名單", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_sch_re(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"成分名稱（成分代碼）", "就醫年月", "就醫次數", "就醫院所數", "總劑量", "總DDD數"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_drug As String = ""
        Dim o_YM As String = ""
        Dim o_visit_n As Int16 = 0
        Dim o_clinic_n As Int16 = 0
        Dim o_t_dose As Int16 = 0
        Dim o_t_DDD As Int16 = 0
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("th").Count < 8 Then
                Continue For
            End If
            For Each th As HtmlElement In tr.GetElementsByTagName("th")
                Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
                For i = 0 To header_want.Count - 1
                    If strT.Length >= header_want(i).Length Then
                        If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                            header_order.Add(i)
                            Exit For
                        End If
                    End If
                Next
                If header_order.Count = order_n Then
                    header_order.Add(-1)
                End If
                order_n += 1
            Next
            ' 全部都是單頁的,不處理多頁的情形
        Next
#End Region

#Region "Write"
        Dim row_left As Int16 = 0
        Dim row_n As Int16 = 0
        Dim drug_name As String = ""
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            If row_left > 0 Then
                row_left -= 1
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                ' header(order_n)是資料表的位置與實際table的對照
                ' order_n是table的位置, header(order_n)的值是資料表的位置
                ' 有rowspan會干擾
                Dim actual_n As Int16
                If row_left <> row_n And row_left > 0 And order_n > 0 Then
                    actual_n = order_n + 1
                    o_drug = drug_name
                Else
                    actual_n = order_n
                End If
                ' 第一輪
                If order_n = 1 And CInt(td.GetAttribute("rowspan")) > 1 Then
                    ' order_n=1 名義上第一輪成分名稱的位置
                    If td.InnerText IsNot Nothing Then
                        drug_name = td.InnerText.Replace(vbCrLf, " ")
                    End If
                    row_n = CInt(td.GetAttribute("rowspan"))
                    row_left = row_n
                End If
                Select Case header_order(actual_n)
                    Case 0  '成分名稱
                        If td.InnerText IsNot Nothing Then
                            o_drug = td.InnerText.Replace(vbCrLf, " ")
                        End If
                    Case 1  '就醫年月
                        If td.InnerText IsNot Nothing Then
                            o_YM = td.InnerText
                        End If
                    Case 2  '就醫次數
                        If td.InnerText IsNot Nothing Then
                            o_visit_n = CInt(td.InnerText)
                        End If
                    Case 3  '就醫院所數
                        If td.InnerText IsNot Nothing Then
                            o_clinic_n = CInt(td.InnerText)
                        End If
                    Case 4  '總劑量
                        If td.InnerText IsNot Nothing Then
                            o_t_dose = CInt(td.InnerText)
                        End If
                    Case 5  '總DDD數
                        If td.InnerText IsNot Nothing Then
                            o_t_DDD = CInt(td.InnerText)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudSCH_R Where (p.uid = strUID And p.drug_name = o_drug And p.YM = o_YM) Select p
            If q.Count = 0 Then
                Dim newR As New tbl_cloudSCH_R With {.uid = strUID, .QDATE = current_time, .YM = o_YM, .drug_name = o_drug, .visit_n = o_visit_n, .clinic_n = o_clinic_n, .t_dose = o_t_dose, .t_DDD = o_t_DDD}
                '存檔

                dc.tbl_cloudSCH_R.InsertOnSubmit(newR)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Sub Write_sch_up(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"成分名稱（成分代碼）", "就診日期", "就診時間", "本院/他院", "總劑量", "總DDD數"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_drug As String = ""
        Dim o_SDATE As Date
        Dim o_STIME As String = ""
        Dim o_clinic As String = ""
        Dim o_t_dose As Int16 = 0
        Dim o_t_DDD As Int16 = 0
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("th").Count < 8 Then
                Continue For
            End If
            For Each th As HtmlElement In tr.GetElementsByTagName("th")
                Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
                For i = 0 To header_want.Count - 1
                    If strT.Length >= header_want(i).Length Then
                        If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                            header_order.Add(i)
                            Exit For
                        End If
                    End If
                Next
                If header_order.Count = order_n Then
                    header_order.Add(-1)
                End If
                order_n += 1
            Next
            ' 全部都是單頁的,不處理多頁的情形
        Next
#End Region

#Region "Write"
        Dim row_left As Int16 = 0
        Dim row_n As Int16 = 0
        Dim drug_name As String = ""
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            If row_left > 0 Then
                row_left -= 1
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                ' header(order_n)是資料表的位置與實際table的對照
                ' order_n是table的位置, header(order_n)的值是資料表的位置
                ' 有rowspan會干擾
                Dim actual_n As Int16
                If row_left <> row_n And row_left > 0 And order_n > 0 Then
                    actual_n = order_n + 1
                    o_drug = drug_name
                Else
                    actual_n = order_n
                End If
                ' 第一輪
                If order_n = 1 And CInt(td.GetAttribute("rowspan")) > 1 Then
                    ' order_n=1 名義上第一輪成分名稱的位置
                    If td.InnerText IsNot Nothing Then
                        drug_name = td.InnerText.Replace(vbCrLf, " ")
                    End If
                    row_n = CInt(td.GetAttribute("rowspan"))
                    row_left = row_n
                End If
                Select Case header_order(actual_n)
                    Case 0  '成分名稱
                        If td.InnerText IsNot Nothing Then
                            o_drug = td.InnerText.Replace(vbCrLf, " ")
                        End If
                    Case 1  '就診日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_d As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_d(0)) + 1911) + "/" + temp_d(1) + "/" + temp_d(2)
                        End If
                    Case 2  '就診時間
                        If td.InnerText IsNot Nothing Then
                            o_STIME = td.InnerText
                        End If
                    Case 3  '本院/他院
                        If td.InnerText IsNot Nothing Then
                            o_clinic = td.InnerText
                        End If
                    Case 4  '總劑量
                        If td.InnerText IsNot Nothing Then
                            o_t_dose = CInt(td.InnerText)
                        End If
                    Case 5  '總DDD數
                        If td.InnerText IsNot Nothing Then
                            o_t_DDD = CInt(td.InnerText)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudSCH_U Where (p.uid = strUID And p.drugname = o_drug And p.SDATE = o_SDATE And p.STIME = o_STIME) Select p
            If q.Count = 0 Then
                Dim newU As New tbl_cloudSCH_U With {.uid = strUID, .QDATE = current_time, .SDATE = o_SDATE, .drugname = o_drug, .STIME = o_STIME, .clinic = o_clinic, .t_dose = o_t_dose, .t_DDD = o_t_DDD}
                '存檔

                dc.tbl_cloudSCH_U.InsertOnSubmit(newU)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_Lab() As Boolean
        ' 找到有幾個tab
        Dim htmlTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_tab")

        ' 不是每個人都有檢查檢驗結果的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0060")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmlgvList As HtmlElement
            Dim pg As HtmlElement
            Dim pg_N As Int16 = 1
            Dim header_want As String() = {"項次", "來源", "就醫科別", "主診斷", "檢查檢驗類別", "醫令名稱", "檢查檢驗項目",
                "檢查檢驗結果/報告結果/病理發現及診斷", "參考值", "報告日期", "醫令代碼"}
            Dim header_order As New List(Of Int16)
            Dim order_n As Int16 = 0
            Dim dc As New WebDataClassesDataContext
            Dim current_time As Date = Now
#End Region

#Region "Prepare"
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0060").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' 取得gvList
            htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
            If htmlgvList Is Nothing Then
                Return False
            End If
            ' 找出要的順序
            order_n = 0
            For Each th As HtmlElement In htmlgvList.GetElementsByTagName("th")
                For i = 0 To header_want.Count - 1
                    If th.InnerText.Replace(vbCrLf, "").Replace(" ", "") = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                Next
                If header_order.Count = order_n Then
                    header_order.Add(-1)
                End If
                order_n += 1
            Next
            ' 找到雲端藥歷有幾頁
            pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
            If pg IsNot Nothing Then
                ' 有ContentPlaceHolder1_pg_gvList, 表示有多頁
                If pg.Children.Count > 15 Then
                    pg_N = 11
                Else
                    pg_N = pg.Children.Count - 5
                End If
                ' 按下日期排序
                For Each th As HtmlElement In htmlgvList.GetElementsByTagName("th")
                    If th.InnerText = "報告日期" Then
                        th.Children(0).InvokeMember("Click")
                        WaitForPageLoad()
                        Exit For
                    End If
                Next
                htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
                For Each th As HtmlElement In htmlgvList.GetElementsByTagName("th")
                    If th.InnerText = "報告日期▲" Then
                        th.Children(0).InvokeMember("Click")
                        WaitForPageLoad()
                        Exit For
                    End If
                Next
                htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
            Else
                ' 沒有ContentPlaceHolder1_pg_gvList, 表示只有ㄧ頁
                pg_N = 1
            End If
#End Region

#Region "Write"
            ' 讀取第一頁
            Write_lab(htmlgvList, header_order, current_time)

            ' 讀取第二至最後一頁
            ' FOR NEXT
            If pg_N > 1 Then
                pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
                For i = 1 To pg_N - 1
                    ' 按按鈕
                    ' i = 1是第二頁,以此類推
                    ' 第12頁又從5開始, 目前先取前11頁就好
                    'pg.Children.Item(i + 3).InvokeMember("Click") 這裡犯了很多錯誤,害得第二頁都沒有更新
                    ' 因為發生錯位, 其實第一頁在i+1, 第二頁在i+2, 以此類推, 所以當有2頁時,沒問題,會按到>, 一樣到第二頁
                    ' 可是當有三頁時, 就會1, 3, 3頁,難怪會插入重複的值, 如此一來又被sub query的try欄截走了, 後面都不執行了, 因此
                    ' no lab, no source, no query
                    pg.Children.Item(i + 2).InvokeMember("Click")
                    WaitForPageLoad()
                    Threading.Thread.Sleep(500)
                    htmlgvList = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_gvList")
                    ' 找到雲端藥歷有幾頁
                    pg = htmlgvList.Document.GetElementById("ContentPlaceHolder1_pg_gvList")
                    Write_lab(htmlgvList, header_order, current_time)
                Next
            End If
#End Region

#Region "Ending"
            ' 匯入大表
            Try
                dc.sp_insert_tbl_cloudlab(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            Try
                dc.sp_insert_p_cloudlab(current_time)
            Catch ex As Exception
                Record_error(ex.Message)
            End Try
            ' 處理source
            Dim q = (From p In dc.tbl_cloudlab_temp Where p.QDATE = current_time Select p.source).Distinct ' this is a query
            Dim r = q.ToList
            Dim s As String()
            For i = 0 To r.Count - 1
                s = r(i).Split(vbCrLf)
                Dim qq = (From pp In dc.p_source Where pp.source_id = s(2).Substring(1) Select pp)
                If qq.Count = 0 Then
                    Try
                        Dim so As New p_source With {.source_id = s(2).Substring(1), .[class] = s(1).Substring(1), .source_name = s(0)}
                        dc.p_source.InsertOnSubmit(so)
                        dc.SubmitChanges()
                    Catch ex As Exception
                        ' do nothing
                        '                    Record_error(ex.Message)
                    End Try
                End If
            Next

            ' 作紀錄
            GetNotifyIcon1().ShowBalloonTip(1000, "檢驗結果", strUID, ToolTipIcon.Info)
            Return True
#End Region
        End If
        Return False
    End Function

    Private Sub Write_lab(ByRef html As HtmlElement, ByRef header_order As List(Of Int16), ByRef current_time As DateTime)
        Dim order_n As Int16 = 0
        Dim dc As New WebDataClassesDataContext
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            Dim newLab As New tbl_cloudlab_temp With {.uid = strUID, .QDATE = current_time}
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                With newLab
                    Select Case header_order(order_n)
                        Case 0  '項次
                            If td.InnerText IsNot Nothing Then
                                .item_n = td.InnerText
                            End If
                        Case 1
                            If td.InnerText IsNot Nothing Then
                                .source = td.InnerText
                            End If
                        Case 2
                            If td.InnerText IsNot Nothing Then
                                .dep = td.InnerText
                            End If
                        Case 3
                            If td.InnerText IsNot Nothing Then
                                .diagnosis = td.InnerText
                            End If
                        Case 4
                            If td.InnerText IsNot Nothing Then
                                .class = td.InnerText
                            End If
                        Case 5
                            If td.InnerText IsNot Nothing Then
                                .order_name = td.InnerText
                            End If
                        Case 6
                            If td.InnerText IsNot Nothing Then
                                .lab_item = td.InnerText
                            End If
                        Case 7
                            If td.InnerText IsNot Nothing Then
                                .result = td.InnerText
                            End If
                        Case 8
                            If td.InnerText IsNot Nothing Then
                                .range = td.InnerText
                            End If
                        Case 9
                            If td.InnerText IsNot Nothing Then
                                Dim temp_d As String() = td.InnerText.Split("/")
                                .SDATE = CStr(CInt(temp_d(0)) + 1911) + "/" + temp_d(1) + "/" + temp_d(2)
                            End If
                        Case 10
                            If td.InnerText IsNot Nothing Then
                                .NHI_code = td.InnerText
                            End If
                        Case Else
                    End Select
                End With
                order_n += 1
            Next
            dc.tbl_cloudlab_temp.InsertOnSubmit(newLab)
            dc.SubmitChanges()
        Next
    End Sub

    Private Function Retrieve_OP() As Boolean
        ' ContentPlaceHolder1_a_0020 是手術明細紀錄
        ' 不是每個人都有手術明細紀錄的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0020")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmldivResult As HtmlElement
#End Region

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0020").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            ' 跟人家不一樣,是在divResult
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                '                '製作自動檔名
                '                Dim temp_filepath As String = "C:\vpn\html"
                '                '存放目錄,不存在就要建立一個
                '                If Not (System.IO.Directory.Exists(temp_filepath)) Then
                '                    System.IO.Directory.CreateDirectory(temp_filepath)
                '                End If
                '                '自動產生名字
                '                temp_filepath += "\OP_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                '                temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                '                temp_filepath += "_" + strUID
                '                temp_filepath += ".html"

                '                '製作html檔 writing to html
                '                Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                '                sw.Write(htmldivResult.OuterHtml)
                '                sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_OP(htmldivResult.Document.GetElementById("ContentPlaceHolder1_gvList"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "手術", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_OP(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"來源", "就醫科別", "主診斷名稱", "手術明細代碼", "手術明細名稱", "診療部位",
                "執行時間-起", "執行時間-迄", "醫令總量"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_source As String = ""
        Dim o_dep As String = ""
        Dim o_diagnosis As String = ""
        Dim o_NHI_code As String = ""
        Dim o_op_name As String = ""
        Dim o_loca As String = ""
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim o_amt As Int16 = 0
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '來源
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(2).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(2).Replace(vbLf, ""), .[class] = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 1  '就醫科別
                        If td.InnerText IsNot Nothing Then
                            o_dep = td.InnerText
                        End If
                    Case 2  '主診斷名稱
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText
                        End If
                    Case 3  '手術明細代碼
                        If td.InnerText IsNot Nothing Then
                            o_NHI_code = td.InnerText
                        End If
                    Case 4  '手術明細名稱
                        If td.InnerText IsNot Nothing Then
                            o_op_name = td.InnerText
                        End If
                    Case 5  '診療部位
                        If td.InnerText IsNot Nothing Then
                            o_loca = td.InnerText
                        End If
                    Case 6  '執行時間-起
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 7  '執行時間-迄
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 8  '醫令總量
                        If td.InnerText IsNot Nothing Then
                            o_amt = CInt(td.InnerText)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudOP Where (p.uid = strUID And p.source = o_source And p.NHI_code = o_NHI_code And p.SDATE = o_SDATE And p.EDATE = o_EDATE) Select p
            If q.Count = 0 Then
                Dim newOP As New tbl_cloudOP With {.uid = strUID, .QDATE = current_time, .source = o_source, .dep = o_dep, .diagnosis = o_diagnosis, .NHI_code = o_NHI_code, .op_name = o_op_name, .loca = o_loca, .SDATE = o_SDATE, .EDATE = o_EDATE, .amt = o_amt}
                '存檔

                dc.tbl_cloudOP.InsertOnSubmit(newOP)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_Den() As Boolean
        ' ContentPlaceHolder1_a_0030 是牙科處置及手術
        ' 不是每個人都有牙科處置及手術
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0030")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmldivResult As HtmlElement
#End Region

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0030").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            ' 跟人家不一樣,是在PanS01
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                ''製作自動檔名
                'Dim temp_filepath As String = "C:\vpn\html"
                ''存放目錄,不存在就要建立一個
                'If Not (System.IO.Directory.Exists(temp_filepath)) Then
                '    System.IO.Directory.CreateDirectory(temp_filepath)
                'End If
                ''自動產生名字
                'temp_filepath += "\dental_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                'temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                'temp_filepath += "_" + strUID
                'temp_filepath += ".html"

                ''製作html檔 writing to html
                'Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                'sw.Write(htmldivResult.OuterHtml)
                'sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_Dental(htmldivResult.Document.GetElementById("ContentPlaceHolder1_gvList"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "牙科", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_Dental(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"來源", "主診斷名稱", "牙醫處置代碼", "牙醫處置名稱", "診療部位",
                "執行時間-起", "執行時間-迄", "醫令總量"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_source As String = ""
        Dim o_diagnosis As String = ""
        Dim o_NHI_code As String = ""
        Dim o_op_name As String = ""
        Dim o_loca As String = ""
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim o_amt As Int16 = 0
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(vbLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '來源
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(2).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(2).Replace(vbLf, ""), .[class] = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 1  '主診斷名稱
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText.Replace(vbCrLf, "").Replace(vbLf, "")
                        End If
                    Case 2  '牙醫處置代碼
                        If td.InnerText IsNot Nothing Then
                            o_NHI_code = td.InnerText
                        End If
                    Case 3  '牙醫處置名稱
                        If td.InnerText IsNot Nothing Then
                            o_op_name = td.InnerText.Replace(vbCrLf, "").Replace(vbLf, "")
                        End If
                    Case 4  '診療部位
                        If td.InnerText IsNot Nothing Then
                            o_loca = td.InnerText
                        End If
                    Case 5  '執行時間-起
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 6  '執行時間-迄
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 7  '醫令總量
                        If td.InnerText IsNot Nothing Then
                            o_amt = CInt(td.InnerText)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudDEN Where (p.uid = strUID And p.source = o_source And p.NHI_code = o_NHI_code And p.SDATE = o_SDATE And p.EDATE = o_EDATE) Select p
            If q.Count = 0 Then
                Dim newDEN As New tbl_cloudDEN With {.uid = strUID, .QDATE = current_time, .source = o_source, .diagnosis = o_diagnosis, .NHI_code = o_NHI_code, .op_name = o_op_name, .loca = o_loca, .SDATE = o_SDATE, .EDATE = o_EDATE, .amt = o_amt}
                '存檔

                dc.tbl_cloudDEN.InsertOnSubmit(newDEN)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_ALL() As Boolean
        ' ContentPlaceHolder1_a_0040 是過敏藥
        ' 不是每個人都有過敏藥
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0040")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmldivResult As HtmlElement
#End Region

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0040").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            ' 跟人家不一樣,是在divResult
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                ''製作自動檔名
                'Dim temp_filepath As String = "C:\vpn\html"
                ''存放目錄,不存在就要建立一個
                'If Not (System.IO.Directory.Exists(temp_filepath)) Then
                '    System.IO.Directory.CreateDirectory(temp_filepath)
                'End If
                ''自動產生名字
                'temp_filepath += "\Allergy_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                'temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                'temp_filepath += "_" + strUID
                'temp_filepath += ".html"

                ''製作html檔 writing to html
                'Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                'sw.Write(htmldivResult.OuterHtml)
                'sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_ALL(htmldivResult.Document.GetElementById("ContentPlaceHolder1_gvList"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "過敏", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_ALL(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"上傳日期", "醫療院所", "上傳註記", "過敏藥物"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_SDATE As Date
        Dim o_source As String = ""
        Dim o_remark As String = ""
        Dim o_drug_name As String = ""
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '上傳日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 1  '醫療院所
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(1).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 2  '上傳註記
                        If td.InnerText IsNot Nothing Then
                            o_remark = td.InnerText
                        End If
                    Case 3  '過敏藥物
                        If td.InnerText IsNot Nothing Then
                            o_drug_name = td.InnerText
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudALL Where (p.uid = strUID And p.source = o_source And p.SDATE = o_SDATE And p.drug_name = o_drug_name) Select p
            If q.Count = 0 Then
                Dim newALL As New tbl_cloudALL With {.uid = strUID, .QDATE = current_time, .source = o_source, .SDATE = o_SDATE, .remark = o_remark, .drug_name = o_drug_name}
                '存檔

                dc.tbl_cloudALL.InsertOnSubmit(newALL)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_discharge() As Boolean
        ' ContentPlaceHolder1_a_0070 是出院病歷摘要
        ' 不是每個人都有關懷名單的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0070")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmldivResult As HtmlElement
#End Region

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0070").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            ' 跟人家不一樣,是在divResult
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                ''製作自動檔名
                'Dim temp_filepath As String = "C:\vpn\html"
                ''存放目錄,不存在就要建立一個
                'If Not (System.IO.Directory.Exists(temp_filepath)) Then
                '    System.IO.Directory.CreateDirectory(temp_filepath)
                'End If
                ''自動產生名字
                'temp_filepath += "\discharge_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                'temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                'temp_filepath += "_" + strUID
                'temp_filepath += ".html"

                ''製作html檔 writing to html
                'Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                'sw.Write(htmldivResult.OuterHtml)
                'sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_DIS(htmldivResult.Document.GetElementById("ContentPlaceHolder1_gvList"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "住院", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_DIS(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"來源", "出院科別", "出院診斷", "住院日期", "出院日期"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_source As String = ""
        Dim o_dep As String = ""
        Dim o_diagnosis As String = ""
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '來源
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(1).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 1  '出院科別
                        If td.InnerText IsNot Nothing Then
                            o_dep = td.InnerText
                        End If
                    Case 2  '出院診斷
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText
                        End If
                    Case 3  '住院日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 4  '出院日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudDIS Where (p.uid = strUID And p.source = o_source And p.SDATE = o_SDATE And p.EDATE = o_EDATE) Select p
            If q.Count = 0 Then
                Dim newDIS As New tbl_cloudDIS With {.uid = strUID, .QDATE = current_time, .source = o_source, .dep = o_dep, .diagnosis = o_diagnosis, .SDATE = o_SDATE, .EDATE = o_EDATE}
                '存檔

                dc.tbl_cloudDIS.InsertOnSubmit(newDIS)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_rehab() As Boolean
        ' ContentPlaceHolder1_a_0080 是復健醫療
        ' 不是每個人都有復健醫療的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0080")
        If queryTAB IsNot Nothing Then
#Region "Declaration"
            Dim htmldivResult As HtmlElement
#End Region

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0080").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            '            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_divResult")
            htmldivResult = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_PanS01")
            ' 跟人家不一樣,是在PanS01
            ' 不用排序

            If htmldivResult IsNot Nothing Then
#Region "儲存HTML檔"
                ''製作自動檔名
                'Dim temp_filepath As String = "C:\vpn\html"
                ''存放目錄,不存在就要建立一個
                'If Not (System.IO.Directory.Exists(temp_filepath)) Then
                '    System.IO.Directory.CreateDirectory(temp_filepath)
                'End If
                ''自動產生名字
                'temp_filepath += "\rehab_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                'temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                'temp_filepath += "_" + strUID
                'temp_filepath += ".html"

                ''製作html檔 writing to html
                'Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                'sw.Write(htmldivResult.OuterHtml)
                'sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_REH(htmldivResult.Document.GetElementById("ContentPlaceHolder1_gvList"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "復健", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_REH(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"診別", "來源", "主診斷碼", "治療類別", "強度", "醫令數量", "就醫日期/住院日期", "治療結束日期",
          "診療部位", "執行時間-起", "執行時間-迄"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_class As String = ""
        Dim o_source As String = ""
        Dim o_diagnosis As String = ""
        Dim o_type As String = ""
        Dim o_curegrade As String = ""
        Dim o_amt As Int16 = 0
        Dim o_begin_date As Date
        Dim o_end_date As Date
        Dim o_loca As String = ""
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '診別
                        If td.InnerText IsNot Nothing Then
                            o_class = td.InnerText
                        End If
                    Case 1  '來源
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(1).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 2  '主診斷碼
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText
                        End If
                    Case 3 '治療類別
                        If td.InnerText IsNot Nothing Then
                            o_type = td.InnerText
                        End If
                    Case 4 '強度
                        If td.InnerText IsNot Nothing Then
                            o_curegrade = td.InnerText
                        End If
                    Case 5 '醫令數量
                        If td.InnerText IsNot Nothing Then
                            o_amt = CInt(td.InnerText)
                        End If
                    Case 6  '就醫日期/住院日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_begin_date = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 7  '治療結束日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_end_date = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 8 '診療部位
                        If td.InnerText IsNot Nothing Then
                                o_loca = td.InnerText
                            End If
                    Case 9  '就醫日期/住院日期
                        If td.InnerText IsNot Nothing Then
                                Dim temp_s As String() = td.InnerText.Split("/")
                                o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                            End If
                    Case 10  '出院日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudREH Where (p.uid = strUID And p.source = o_source And p.type = o_type And p.SDATE = o_SDATE And p.EDATE = o_EDATE) Select p
            If q.Count = 0 Then
                Dim newREH As New tbl_cloudREH With {.uid = strUID, .QDATE = current_time, .[class] = o_class, .source = o_source, .type = o_type, .diagnosis = o_diagnosis,
                    .curegrade = o_curegrade, .amt = o_amt, .begin_date = o_begin_date, .end_date = o_end_date, .loca = o_loca, .SDATE = o_SDATE, .EDATE = o_EDATE}
                '存檔

                dc.tbl_cloudREH.InsertOnSubmit(newREH)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Function Retrieve_TCM() As Boolean
        ' ContentPlaceHolder1_a_0090 是中醫用藥
        ' 不是每個人都有中醫用藥的
        Dim queryTAB As HtmlElement = WebBrowser1.Document.GetElementById("ContentPlaceHolder1_li_0090")
        If queryTAB IsNot Nothing Then
            ' Declaration
            Dim htmlPanS01 As HtmlElement

            ' 判斷是否active?
            ' 如果沒有active, 就要點下去
            If queryTAB.GetAttribute("className") <> "active" Then
                queryTAB.Document.GetElementById("ContentPlaceHolder1_a_0090").InvokeMember("Click")
                WaitForPageLoad()
            End If
            ' Do something
            ' 資料在這個iframe
            ' 這個frame在frames(0)
            ' 取得gvList
            htmlPanS01 = WebBrowser1.Document.Window.Frames(0).Document.GetElementById("ContentPlaceHolder1_PanS01")
            ' 跟人家不一樣,是在PanS01
            ' 不用排序

            If htmlPanS01 IsNot Nothing Then
#Region "儲存HTML檔"
                '製作自動檔名
                Dim temp_filepath As String = "C:\vpn\html"
                '存放目錄,不存在就要建立一個
                If Not (System.IO.Directory.Exists(temp_filepath)) Then
                    System.IO.Directory.CreateDirectory(temp_filepath)
                End If
                '自動產生名字
                temp_filepath += "\TCM_" + Year(Now).ToString + (Month(Now) + 100).ToString.Substring(1, 2) + (DatePart("d", Now) + 100).ToString.Substring(1, 2)
                temp_filepath += "_" + Now.TimeOfDay.ToString.Replace(":", "").Replace(".", "")
                temp_filepath += "_" + strUID
                temp_filepath += ".html"

                '製作html檔 writing to html
                Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(temp_filepath, True, System.Text.Encoding.Unicode)
                sw.Write(htmlPanS01.OuterHtml)
                sw.Close()
#End Region

#Region "寫入資料庫"
                Try
                    Write_TCM_GR(htmlPanS01.Document.GetElementById("ContentPlaceHolder1_gvGroup"))
                    Write_TCM_DE(htmlPanS01.Document.GetElementById("ContentPlaceHolder1_gvDetail"))
                Catch ex As Exception
                    Record_error(ex.Message)
                End Try
#End Region
                GetNotifyIcon1().ShowBalloonTip(1000, "中醫", strUID, ToolTipIcon.Info)
                Return True
            Else
                Return False
            End If
        End If
        Return False
    End Function

    Private Sub Write_TCM_GR(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"來源", "主診斷", "給藥日數", "慢連籤", "就醫(調劑)日期", "慢連籤領藥日", "就醫序號"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_source As String = ""
        Dim o_diagnosis As String = ""
        Dim o_days As Int16 = 0
        Dim o_chronic As String = ""
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim o_serial As String = ""
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '來源
                        If td.InnerText IsNot Nothing Then
                            Dim s As String() = td.InnerText.Split(vbCrLf)
                            o_source = s(2).Replace(vbLf, "")
                            Dim q1 = From p1 In dc.p_source Where p1.source_id = o_source Select p1
                            If q1.Count = 0 Then
                                Dim new_source As New p_source With {.source_id = s(2).Replace(vbLf, ""), .[class] = s(1).Replace(vbLf, ""), .source_name = s(0)}
                                dc.p_source.InsertOnSubmit(new_source)
                                dc.SubmitChanges()
                            End If
                        End If
                    Case 1  '主診斷
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText.Replace(vbCrLf, " ").Replace(vbLf, "")
                        End If
                    Case 2 '給藥日數
                        If td.InnerText IsNot Nothing Then
                            o_days = CInt(td.InnerText)
                        End If
                    Case 3 '慢連籤
                        If td.InnerText IsNot Nothing Then
                            o_chronic = td.InnerText
                        End If
                    Case 4 '就醫(調劑)日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 5  '慢連籤領藥日
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 6  '就醫序號
                        If td.InnerText IsNot Nothing Then
                            o_serial = td.InnerText
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudTCM_G Where (p.uid = strUID And p.SDATE = o_SDATE And p.serial = o_serial) Select p
            If q.Count = 0 Then
                Dim newTCMG As New tbl_cloudTCM_G With {.uid = strUID, .QDATE = current_time, .source = o_source, .diagnosis = o_diagnosis,
                    .days = o_days, .chronic = o_chronic, .SDATE = o_SDATE, .EDATE = o_EDATE, .serial = o_serial}
                '存檔

                dc.tbl_cloudTCM_G.InsertOnSubmit(newTCMG)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub

    Private Sub Write_TCM_DE(ByRef html As HtmlElement)
#Region "Declaration"
        Dim header_want As String() = {"主診斷", "藥品代碼", "複方註記", "基準方名", "效能名稱", "用法用量", "給藥日數", "劑型",
            "給藥總量", "就醫(調劑)日期", "慢連籤領藥日", "就醫序號"}
        Dim header_order As New List(Of Int16)
        Dim order_n As Int16 = 0
        Dim o_diagnosis As String = ""
        Dim o_NHI_code As String = ""
        Dim o_complex As String = ""
        Dim o_base As String = ""
        Dim o_effect As String = ""
        Dim o_dosing As String = ""
        Dim o_days As Int16 = 0
        Dim o_type As String = ""
        Dim o_amt As Int16 = 0
        Dim o_SDATE As Date
        Dim o_EDATE As Date
        Dim o_serial As String = ""
        Dim dc As New WebDataClassesDataContext
        Dim current_time As Date = Now
#End Region

#Region "Prepare"
        If html Is Nothing Then
            Exit Sub
        End If
        ' 找出要的順序
        order_n = 0
        For Each th As HtmlElement In html.GetElementsByTagName("th")
            Dim strT = th.InnerText.Replace(vbCrLf, "").Replace(" ", "")
            For i = 0 To header_want.Count - 1
                If strT.Length >= header_want(i).Length Then
                    If strT.Substring(0, header_want(i).Length) = header_want(i) Then
                        header_order.Add(i)
                        '                            Exit For
                    End If
                End If
            Next
            If header_order.Count = order_n Then
                header_order.Add(-1)
            End If
            order_n += 1
        Next
        ' 全部都是單頁的,不處理多頁的情形
#End Region

#Region "Write"
        For Each tr As HtmlElement In html.GetElementsByTagName("tr")
            If tr.GetElementsByTagName("td").Count = 0 Then
                Continue For
            End If
            order_n = 0
            For Each td As HtmlElement In tr.GetElementsByTagName("td")
                Select Case header_order(order_n)
                    Case 0  '主診斷
                        If td.InnerText IsNot Nothing Then
                            o_diagnosis = td.InnerText.Replace(vbCrLf, " ").Replace(vbLf, "")
                        End If
                    Case 1  '藥品代碼
                        If td.InnerText IsNot Nothing Then
                            o_NHI_code = td.InnerText
                        End If
                    Case 2  '複方註記
                        If td.InnerText IsNot Nothing Then
                            o_complex = td.InnerText
                        End If
                    Case 3  '基準方名
                        If td.InnerText IsNot Nothing Then
                            o_base = td.InnerText
                        End If
                    Case 4  '效能名稱
                        If td.InnerText IsNot Nothing Then
                            o_effect = td.InnerText
                        End If
                    Case 5 '用法用量
                        If td.InnerText IsNot Nothing Then
                            o_dosing = td.InnerText
                        End If
                    Case 6  '給藥日數
                        If td.InnerText IsNot Nothing Then
                            o_days = CInt(td.InnerText)
                        End If
                    Case 7  '劑型
                        If td.InnerText IsNot Nothing Then
                            o_type = td.InnerText
                        End If
                    Case 8 '給藥總量
                        If td.InnerText IsNot Nothing Then
                            o_amt = CInt(td.InnerText)
                        End If
                    Case 9  '就醫(調劑)日期
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_SDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 10  '慢連籤領藥日
                        If td.InnerText IsNot Nothing Then
                            Dim temp_s As String() = td.InnerText.Split("/")
                            o_EDATE = CStr(CInt(temp_s(0)) + 1911) + "/" + temp_s(1) + "/" + temp_s(2)
                        End If
                    Case 11 '就醫序號
                        If td.InnerText IsNot Nothing Then
                            o_serial = td.InnerText
                        End If
                    Case Else
                End Select
                order_n += 1
            Next

            Dim q = From p In dc.tbl_cloudTCM_D Where (p.uid = strUID And p.NHI_code = o_NHI_code And p.SDATE = o_SDATE And p.serial = o_serial) Select p
            If q.Count = 0 Then
                Dim newTCMD As New tbl_cloudTCM_D With {.uid = strUID, .QDATE = current_time, .diagnosis = o_diagnosis, .NHI_code = o_NHI_code, .complex = o_complex,
                    .base = o_base, .effect = o_effect, .dosing = o_dosing, .days = o_days, .type = o_type, .amt = o_amt, .SDATE = o_SDATE, .EDATE = o_EDATE,
                    .serial = o_serial}
                '存檔

                dc.tbl_cloudTCM_D.InsertOnSubmit(newTCMD)
                dc.SubmitChanges()
            End If
        Next
#End Region

#Region "Ending"

#End Region
    End Sub
#End Region

#Region "Page Loading Functions"
    Private Sub WaitForPageLoad()
        'Dim ii As Int16 = 0
        'Do While ii < 5000
        '    Application.DoEvents()
        '    Threading.Thread.Sleep(1)
        '    ii += 1
        'Loop
        AddHandler Me.WebBrowser1.DocumentCompleted, New WebBrowserDocumentCompletedEventHandler(AddressOf PageWaiter)
        ' We need a time out, say 10 sec, 10000 = 10 sec
        Dim ii As Int16 = 0
        While (Not Pageready) And (ii < 10000)
            Application.DoEvents()
            Threading.Thread.Sleep(1)
            ii += 1
        End While
        If ii >= 10000 Then
            RemoveHandler Me.WebBrowser1.DocumentCompleted, New WebBrowserDocumentCompletedEventHandler(AddressOf PageWaiter)
        End If
        Pageready = False
    End Sub

    Private Sub PageWaiter(ByVal sender As Object, ByVal e As WebBrowserDocumentCompletedEventArgs)
        If Me.WebBrowser1.ReadyState = WebBrowserReadyState.Complete Then
            Pageready = True
            RemoveHandler Me.WebBrowser1.DocumentCompleted, New WebBrowserDocumentCompletedEventHandler(AddressOf PageWaiter)
        End If
    End Sub
#End Region
End Class
