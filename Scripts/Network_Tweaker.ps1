<# 
.NAME
    Tweaking Adapter
#>

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()

$Form                            = New-Object system.Windows.Forms.Form
$Form.ClientSize                 = New-Object System.Drawing.Point(1606,934)
$Form.text                       = "Network  Adapter - Tweaker"
$Form.TopMost                    = $false
$Form.BackColor                  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_AdapterNamesCombo            = New-Object system.Windows.Forms.ComboBox
$cb_AdapterNamesCombo.width      = 262
$cb_AdapterNamesCombo.height     = 20
$cb_AdapterNamesCombo.location   = New-Object System.Drawing.Point(64,16)
$cb_AdapterNamesCombo.Font       = New-Object System.Drawing.Font('Calibri',9)
$cb_AdapterNamesCombo.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_AdapterNamesCombo.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label1                          = New-Object system.Windows.Forms.Label
$Label1.text                     = "Adapter:"
$Label1.AutoSize                 = $true
$Label1.width                    = 25
$Label1.height                   = 10
$Label1.location                 = New-Object System.Drawing.Point(10,20)
$Label1.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label1.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label2                          = New-Object system.Windows.Forms.Label
$Label2.text                     = "Registry:"
$Label2.AutoSize                 = $true
$Label2.width                    = 25
$Label2.height                   = 10
$Label2.location                 = New-Object System.Drawing.Point(10,43)
$Label2.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label2.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$lbl_Path                        = New-Object system.Windows.Forms.Label
$lbl_Path.AutoSize               = $true
$lbl_Path.width                  = 25
$lbl_Path.height                 = 10
$lbl_Path.location               = New-Object System.Drawing.Point(63,45)
$lbl_Path.Font                   = New-Object System.Drawing.Font('Calibri',9)
$lbl_Path.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label3                          = New-Object system.Windows.Forms.Label
$Label3.text                     = "NDIS:"
$Label3.AutoSize                 = $true
$Label3.width                    = 25
$Label3.height                   = 10
$Label3.location                 = New-Object System.Drawing.Point(10,66)
$Label3.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label3.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$lbl_ndisver                     = New-Object system.Windows.Forms.Label
$lbl_ndisver.AutoSize            = $true
$lbl_ndisver.width               = 25
$lbl_ndisver.height              = 10
$lbl_ndisver.location            = New-Object System.Drawing.Point(63,65)
$lbl_ndisver.Font                = New-Object System.Drawing.Font('Calibri',10)
$lbl_ndisver.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Groupbox1                       = New-Object system.Windows.Forms.Groupbox
$Groupbox1.height                = 213
$Groupbox1.width                 = 233
$Groupbox1.text                  = "RSS Settings"
$Groupbox1.location              = New-Object System.Drawing.Point(10,93)

$Label4                          = New-Object system.Windows.Forms.Label
$Label4.text                     = "Profile:"
$Label4.AutoSize                 = $true
$Label4.width                    = 25
$Label4.height                   = 10
$Label4.location                 = New-Object System.Drawing.Point(8,66)
$Label4.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label4.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label5                          = New-Object system.Windows.Forms.Label
$Label5.text                     = "NumberOfReceiveQueues:"
$Label5.AutoSize                 = $true
$Label5.width                    = 25
$Label5.height                   = 10
$Label5.location                 = New-Object System.Drawing.Point(8,43)
$Label5.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label5.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label6                          = New-Object system.Windows.Forms.Label
$Label6.text                     = "Status:"
$Label6.AutoSize                 = $true
$Label6.width                    = 25
$Label6.height                   = 10
$Label6.location                 = New-Object System.Drawing.Point(8,20)
$Label6.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label6.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$lbl_rssstatus                   = New-Object system.Windows.Forms.Label
$lbl_rssstatus.AutoSize          = $true
$lbl_rssstatus.width             = 25
$lbl_rssstatus.height            = 10
$lbl_rssstatus.location          = New-Object System.Drawing.Point(61,20)
$lbl_rssstatus.Font              = New-Object System.Drawing.Font('Calibri',10)
$lbl_rssstatus.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_rss_onoff                    = New-Object system.Windows.Forms.ComboBox
$cb_rss_onoff.width              = 108
$cb_rss_onoff.height             = 20
@('Enable','Disable') | ForEach-Object {[void] $cb_rss_onoff.Items.Add($_)}
$cb_rss_onoff.location           = New-Object System.Drawing.Point(117,17)
$cb_rss_onoff.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_rss_onoff.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rss_onoff.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_apply                       = New-Object system.Windows.Forms.Button
$btn_apply.text                  = "Apply"
$btn_apply.width                 = 60
$btn_apply.height                = 21
$btn_apply.location              = New-Object System.Drawing.Point(10,312)
$btn_apply.Font                  = New-Object System.Drawing.Font('Calibri',10)
$btn_apply.ForeColor             = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_rssqueues                    = New-Object system.Windows.Forms.ComboBox
$cb_rssqueues.width              = 60
$cb_rssqueues.height             = 20
$cb_rssqueues.location           = New-Object System.Drawing.Point(165,41)
$cb_rssqueues.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_rssqueues.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rssqueues.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_unqueues                    = New-Object system.Windows.Forms.Button
$btn_unqueues.text               = "Unlock RSSQueues"
$btn_unqueues.width              = 112
$btn_unqueues.height             = 21
$btn_unqueues.location           = New-Object System.Drawing.Point(76,312)
$btn_unqueues.Font               = New-Object System.Drawing.Font('Calibri',9)
$btn_unqueues.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#f8e71c")

$cb_rssprofile                   = New-Object system.Windows.Forms.ComboBox
$cb_rssprofile.width             = 108
$cb_rssprofile.height            = 20
$cb_rssprofile.location          = New-Object System.Drawing.Point(117,66)
$cb_rssprofile.Font              = New-Object System.Drawing.Font('Calibri',9)
$cb_rssprofile.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rssprofile.BackColor         = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label7                          = New-Object system.Windows.Forms.Label
$Label7.text                     = "BaseProcessor:"
$Label7.AutoSize                 = $true
$Label7.width                    = 25
$Label7.height                   = 10
$Label7.location                 = New-Object System.Drawing.Point(8,92)
$Label7.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label7.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_rssbaseproc                  = New-Object system.Windows.Forms.ComboBox
$cb_rssbaseproc.width            = 108
$cb_rssbaseproc.height           = 20
@('0','1','2','3','4','5','6','7','8','9','10','11','12','13','14','15') | ForEach-Object {[void] $cb_rssbaseproc.Items.Add($_)}
$cb_rssbaseproc.location         = New-Object System.Drawing.Point(117,89)
$cb_rssbaseproc.Font             = New-Object System.Drawing.Font('Calibri',9)
$cb_rssbaseproc.ForeColor        = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rssbaseproc.BackColor        = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label8                          = New-Object system.Windows.Forms.Label
$Label8.text                     = "MaxProcessor:"
$Label8.AutoSize                 = $true
$Label8.width                    = 25
$Label8.height                   = 10
$Label8.location                 = New-Object System.Drawing.Point(7,115)
$Label8.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label8.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_rssmaxproc                   = New-Object system.Windows.Forms.ComboBox
$cb_rssmaxproc.width             = 108
$cb_rssmaxproc.height            = 20
@('0','1','2','3','4','5','6','7','8','9','10','11','12','13','14','15') | ForEach-Object {[void] $cb_rssmaxproc.Items.Add($_)}
$cb_rssmaxproc.location          = New-Object System.Drawing.Point(117,112)
$cb_rssmaxproc.Font              = New-Object System.Drawing.Font('Calibri',9)
$cb_rssmaxproc.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rssmaxproc.BackColor         = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label9                          = New-Object system.Windows.Forms.Label
$Label9.text                     = "MaxProcessors:"
$Label9.AutoSize                 = $true
$Label9.width                    = 25
$Label9.height                   = 10
$Label9.location                 = New-Object System.Drawing.Point(7,139)
$Label9.Font                     = New-Object System.Drawing.Font('Calibri',10)
$Label9.ForeColor                = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_rssmaxprocs                  = New-Object system.Windows.Forms.ComboBox
$cb_rssmaxprocs.width            = 108
$cb_rssmaxprocs.height           = 20
@('0','1','2','3','4','5','6','7','8','9','10','11','12','13','14','15') | ForEach-Object {[void] $cb_rssmaxprocs.Items.Add($_)}
$cb_rssmaxprocs.location         = New-Object System.Drawing.Point(117,136)
$cb_rssmaxprocs.Font             = New-Object System.Drawing.Font('Calibri',9)
$cb_rssmaxprocs.ForeColor        = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_rssmaxprocs.BackColor        = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_openreg                     = New-Object system.Windows.Forms.Button
$btn_openreg.text                = "Open"
$btn_openreg.width               = 86
$btn_openreg.height              = 20
$btn_openreg.location            = New-Object System.Drawing.Point(334,16)
$btn_openreg.Font                = New-Object System.Drawing.Font('Calibri',10)
$btn_openreg.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Groupbox2                       = New-Object system.Windows.Forms.Groupbox
$Groupbox2.height                = 190
$Groupbox2.width                 = 311
$Groupbox2.text                  = "Global Settings"
$Groupbox2.location              = New-Object System.Drawing.Point(250,93)

$Label10                         = New-Object system.Windows.Forms.Label
$Label10.text                    = "ReceiveSideScaling:"
$Label10.AutoSize                = $true
$Label10.width                   = 25
$Label10.height                  = 10
$Label10.location                = New-Object System.Drawing.Point(10,20)
$Label10.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label10.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_osrss                        = New-Object system.Windows.Forms.ComboBox
$cb_osrss.width                  = 108
$cb_osrss.height                 = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_osrss.Items.Add($_)}
$cb_osrss.location               = New-Object System.Drawing.Point(194,17)
$cb_osrss.Font                   = New-Object System.Drawing.Font('Calibri',9)
$cb_osrss.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_osrss.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label11                         = New-Object system.Windows.Forms.Label
$Label11.text                    = "ReceiveSegmentCoalescing:"
$Label11.AutoSize                = $true
$Label11.width                   = 148
$Label11.height                  = 10
$Label11.location                = New-Object System.Drawing.Point(10,44)
$Label11.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label11.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_osrsc                        = New-Object system.Windows.Forms.ComboBox
$cb_osrsc.width                  = 108
$cb_osrsc.height                 = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_osrsc.Items.Add($_)}
$cb_osrsc.location               = New-Object System.Drawing.Point(194,40)
$cb_osrsc.Font                   = New-Object System.Drawing.Font('Calibri',9)
$cb_osrsc.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_osrsc.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label12                         = New-Object system.Windows.Forms.Label
$Label12.text                    = "Chimney:"
$Label12.AutoSize                = $true
$Label12.width                   = 25
$Label12.height                  = 10
$Label12.location                = New-Object System.Drawing.Point(11,68)
$Label12.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label12.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_oschimney                    = New-Object system.Windows.Forms.ComboBox
$cb_oschimney.width              = 108
$cb_oschimney.height             = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_oschimney.Items.Add($_)}
$cb_oschimney.location           = New-Object System.Drawing.Point(194,63)
$cb_oschimney.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_oschimney.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_oschimney.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label13                         = New-Object system.Windows.Forms.Label
$Label13.text                    = "TaskOffload:"
$Label13.AutoSize                = $true
$Label13.width                   = 25
$Label13.height                  = 10
$Label13.location                = New-Object System.Drawing.Point(10,91)
$Label13.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label13.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ostaskoff                    = New-Object system.Windows.Forms.ComboBox
$cb_ostaskoff.width              = 108
$cb_ostaskoff.height             = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_ostaskoff.Items.Add($_)}
$cb_ostaskoff.location           = New-Object System.Drawing.Point(194,86)
$cb_ostaskoff.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_ostaskoff.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ostaskoff.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_osntd                        = New-Object system.Windows.Forms.ComboBox
$cb_osntd.width                  = 108
$cb_osntd.height                 = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_osntd.Items.Add($_)}
$cb_osntd.location               = New-Object System.Drawing.Point(194,109)
$cb_osntd.Font                   = New-Object System.Drawing.Font('Calibri',9)
$cb_osntd.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_osntd.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label14                         = New-Object system.Windows.Forms.Label
$Label14.text                    = "NetworkDirect:"
$Label14.AutoSize                = $true
$Label14.width                   = 25
$Label14.height                  = 10
$Label14.location                = New-Object System.Drawing.Point(10,114)
$Label14.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label14.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_osntdais                     = New-Object system.Windows.Forms.ComboBox
$cb_osntdais.width               = 108
$cb_osntdais.height              = 20
@('Blocked','Allowed') | ForEach-Object {[void] $cb_osntdais.Items.Add($_)}
$cb_osntdais.location            = New-Object System.Drawing.Point(194,132)
$cb_osntdais.Font                = New-Object System.Drawing.Font('Calibri',9)
$cb_osntdais.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_osntdais.BackColor           = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label15                         = New-Object system.Windows.Forms.Label
$Label15.text                    = "NetworkDirectAcrossIPSubnets:"
$Label15.AutoSize                = $true
$Label15.width                   = 25
$Label15.height                  = 10
$Label15.location                = New-Object System.Drawing.Point(10,137)
$Label15.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label15.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ospcf                        = New-Object system.Windows.Forms.ComboBox
$cb_ospcf.width                  = 108
$cb_ospcf.height                 = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_ospcf.Items.Add($_)}
$cb_ospcf.location               = New-Object System.Drawing.Point(194,156)
$cb_ospcf.Font                   = New-Object System.Drawing.Font('Calibri',9)
$cb_ospcf.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ospcf.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label16                         = New-Object system.Windows.Forms.Label
$Label16.text                    = "PacketCoalescingFilter:"
$Label16.AutoSize                = $true
$Label16.width                   = 25
$Label16.height                  = 10
$Label16.location                = New-Object System.Drawing.Point(10,161)
$Label16.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label16.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$btn_applyglobal                 = New-Object system.Windows.Forms.Button
$btn_applyglobal.text            = "Apply"
$btn_applyglobal.width           = 60
$btn_applyglobal.height          = 21
$btn_applyglobal.location        = New-Object System.Drawing.Point(251,286)
$btn_applyglobal.Font            = New-Object System.Drawing.Font('Calibri',10)
$btn_applyglobal.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Groupbox3                       = New-Object system.Windows.Forms.Groupbox
$Groupbox3.height                = 584
$Groupbox3.width                 = 399
$Groupbox3.text                  = "Adv. Adapter"
$Groupbox3.location              = New-Object System.Drawing.Point(570,93)

$Label17                         = New-Object system.Windows.Forms.Label
$Label17.text                    = "FlowControl:"
$Label17.AutoSize                = $true
$Label17.width                   = 25
$Label17.height                  = 10
$Label17.location                = New-Object System.Drawing.Point(9,20)
$Label17.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label17.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_flowcontrol                  = New-Object system.Windows.Forms.ComboBox
$cb_flowcontrol.width            = 190
$cb_flowcontrol.height           = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_flowcontrol.Items.Add($_)}
$cb_flowcontrol.location         = New-Object System.Drawing.Point(193,17)
$cb_flowcontrol.Font             = New-Object System.Drawing.Font('Calibri',9)
$cb_flowcontrol.ForeColor        = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_flowcontrol.BackColor        = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label18                         = New-Object system.Windows.Forms.Label
$Label18.text                    = "IPChecksumOffloadIPv4:"
$Label18.AutoSize                = $true
$Label18.width                   = 25
$Label18.height                  = 10
$Label18.location                = New-Object System.Drawing.Point(9,42)
$Label18.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label18.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label19                         = New-Object system.Windows.Forms.Label
$Label19.text                    = "TCPChecksumOffloadIPv4:"
$Label19.AutoSize                = $true
$Label19.width                   = 25
$Label19.height                  = 10
$Label19.location                = New-Object System.Drawing.Point(9,64)
$Label19.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label19.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label20                         = New-Object system.Windows.Forms.Label
$Label20.text                    = "TCPChecksumOffloadIPv6:"
$Label20.AutoSize                = $true
$Label20.width                   = 25
$Label20.height                  = 10
$Label20.location                = New-Object System.Drawing.Point(9,86)
$Label20.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label20.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label21                         = New-Object system.Windows.Forms.Label
$Label21.text                    = "UDPChecksumOffloadIPv4:"
$Label21.AutoSize                = $true
$Label21.width                   = 25
$Label21.height                  = 10
$Label21.location                = New-Object System.Drawing.Point(9,108)
$Label21.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label21.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label22                         = New-Object system.Windows.Forms.Label
$Label22.text                    = "UDPChecksumOffloadIPv6:"
$Label22.AutoSize                = $true
$Label22.width                   = 25
$Label22.height                  = 10
$Label22.location                = New-Object System.Drawing.Point(9,130)
$Label22.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label22.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label23                         = New-Object system.Windows.Forms.Label
$Label23.text                    = "InterruptModeration:"
$Label23.AutoSize                = $true
$Label23.width                   = 25
$Label23.height                  = 10
$Label23.location                = New-Object System.Drawing.Point(9,324)
$Label23.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label23.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_InterruptModeration          = New-Object system.Windows.Forms.ComboBox
$cb_InterruptModeration.width    = 190
$cb_InterruptModeration.height   = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_InterruptModeration.Items.Add($_)}
$cb_InterruptModeration.location  = New-Object System.Drawing.Point(193,321)
$cb_InterruptModeration.Font     = New-Object System.Drawing.Font('Calibri',9)
$cb_InterruptModeration.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_InterruptModeration.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_applyadv                    = New-Object system.Windows.Forms.Button
$btn_applyadv.text               = "Apply"
$btn_applyadv.width              = 60
$btn_applyadv.height             = 21
$btn_applyadv.location           = New-Object System.Drawing.Point(570,682)
$btn_applyadv.Font               = New-Object System.Drawing.Font('Calibri',10)
$btn_applyadv.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$btn_adaptrest                   = New-Object system.Windows.Forms.Button
$btn_adaptrest.text              = "Restart Adapter"
$btn_adaptrest.width             = 112
$btn_adaptrest.height            = 20
$btn_adaptrest.location          = New-Object System.Drawing.Point(511,16)
$btn_adaptrest.Font              = New-Object System.Drawing.Font('Calibri',10)
$btn_adaptrest.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#d0021b")

$cb_IPChecksumOffloadIPv4        = New-Object system.Windows.Forms.ComboBox
$cb_IPChecksumOffloadIPv4.width  = 190
$cb_IPChecksumOffloadIPv4.height  = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_IPChecksumOffloadIPv4.Items.Add($_)}
$cb_IPChecksumOffloadIPv4.location  = New-Object System.Drawing.Point(193,39)
$cb_IPChecksumOffloadIPv4.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_IPChecksumOffloadIPv4.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_IPChecksumOffloadIPv4.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_TCPChecksumOffloadIPv4       = New-Object system.Windows.Forms.ComboBox
$cb_TCPChecksumOffloadIPv4.width  = 190
$cb_TCPChecksumOffloadIPv4.height  = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_TCPChecksumOffloadIPv4.Items.Add($_)}
$cb_TCPChecksumOffloadIPv4.location  = New-Object System.Drawing.Point(193,61)
$cb_TCPChecksumOffloadIPv4.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_TCPChecksumOffloadIPv4.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_TCPChecksumOffloadIPv4.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_TCPChecksumOffloadIPv6       = New-Object system.Windows.Forms.ComboBox
$cb_TCPChecksumOffloadIPv6.width  = 190
$cb_TCPChecksumOffloadIPv6.height  = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_TCPChecksumOffloadIPv6.Items.Add($_)}
$cb_TCPChecksumOffloadIPv6.location  = New-Object System.Drawing.Point(193,83)
$cb_TCPChecksumOffloadIPv6.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_TCPChecksumOffloadIPv6.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_TCPChecksumOffloadIPv6.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_UDPChecksumOffloadIPv4       = New-Object system.Windows.Forms.ComboBox
$cb_UDPChecksumOffloadIPv4.width  = 190
$cb_UDPChecksumOffloadIPv4.height  = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_UDPChecksumOffloadIPv4.Items.Add($_)}
$cb_UDPChecksumOffloadIPv4.location  = New-Object System.Drawing.Point(193,105)
$cb_UDPChecksumOffloadIPv4.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_UDPChecksumOffloadIPv4.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_UDPChecksumOffloadIPv4.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_UDPChecksumOffloadIPv6       = New-Object system.Windows.Forms.ComboBox
$cb_UDPChecksumOffloadIPv6.width  = 190
$cb_UDPChecksumOffloadIPv6.height  = 20
@('0 - Disabled','1 - Tx Enabled','2 - Rx Enabled','3 - Rx and Tx Enabled') | ForEach-Object {[void] $cb_UDPChecksumOffloadIPv6.Items.Add($_)}
$cb_UDPChecksumOffloadIPv6.location  = New-Object System.Drawing.Point(193,127)
$cb_UDPChecksumOffloadIPv6.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_UDPChecksumOffloadIPv6.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_UDPChecksumOffloadIPv6.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label24                         = New-Object system.Windows.Forms.Label
$Label24.text                    = "InterruptModerationRate:"
$Label24.AutoSize                = $true
$Label24.width                   = 25
$Label24.height                  = 10
$Label24.location                = New-Object System.Drawing.Point(9,346)
$Label24.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label24.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_InterruptModerationRate      = New-Object system.Windows.Forms.ComboBox
$cb_InterruptModerationRate.width  = 190
$cb_InterruptModerationRate.height  = 20
@('0 - Disabled','200 - Minimal','400 - Low','950 - Medium','2000 - High','3600 - Extreme','65535 - Adaptive') | ForEach-Object {[void] $cb_InterruptModerationRate.Items.Add($_)}
$cb_InterruptModerationRate.location  = New-Object System.Drawing.Point(193,343)
$cb_InterruptModerationRate.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_InterruptModerationRate.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_InterruptModerationRate.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label25                         = New-Object system.Windows.Forms.Label
$Label25.text                    = "LsoV2IPv4"
$Label25.AutoSize                = $true
$Label25.width                   = 25
$Label25.height                  = 10
$Label25.location                = New-Object System.Drawing.Point(9,174)
$Label25.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label25.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label26                         = New-Object system.Windows.Forms.Label
$Label26.text                    = "LsoV2IPv6"
$Label26.AutoSize                = $true
$Label26.width                   = 25
$Label26.height                  = 10
$Label26.location                = New-Object System.Drawing.Point(9,196)
$Label26.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label26.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_LsoV2IPv4                    = New-Object system.Windows.Forms.ComboBox
$cb_LsoV2IPv4.width              = 190
$cb_LsoV2IPv4.height             = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_LsoV2IPv4.Items.Add($_)}
$cb_LsoV2IPv4.location           = New-Object System.Drawing.Point(193,171)
$cb_LsoV2IPv4.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_LsoV2IPv4.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_LsoV2IPv4.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_LsoV2IPv6                    = New-Object system.Windows.Forms.ComboBox
$cb_LsoV2IPv6.width              = 190
$cb_LsoV2IPv6.height             = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_LsoV2IPv6.Items.Add($_)}
$cb_LsoV2IPv6.location           = New-Object System.Drawing.Point(193,193)
$cb_LsoV2IPv6.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_LsoV2IPv6.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_LsoV2IPv6.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label27                         = New-Object system.Windows.Forms.Label
$Label27.text                    = "LsoV1IPv4"
$Label27.AutoSize                = $true
$Label27.width                   = 25
$Label27.height                  = 10
$Label27.location                = New-Object System.Drawing.Point(9,152)
$Label27.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label27.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_LsoV1IPv4                    = New-Object system.Windows.Forms.ComboBox
$cb_LsoV1IPv4.width              = 190
$cb_LsoV1IPv4.height             = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_LsoV1IPv4.Items.Add($_)}
$cb_LsoV1IPv4.location           = New-Object System.Drawing.Point(193,149)
$cb_LsoV1IPv4.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_LsoV1IPv4.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_LsoV1IPv4.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label28                         = New-Object system.Windows.Forms.Label
$Label28.text                    = "PMNSOffload"
$Label28.AutoSize                = $true
$Label28.width                   = 25
$Label28.height                  = 10
$Label28.location                = New-Object System.Drawing.Point(9,238)
$Label28.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label28.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_PMNSOffload                  = New-Object system.Windows.Forms.ComboBox
$cb_PMNSOffload.width            = 190
$cb_PMNSOffload.height           = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_PMNSOffload.Items.Add($_)}
$cb_PMNSOffload.location         = New-Object System.Drawing.Point(193,235)
$cb_PMNSOffload.Font             = New-Object System.Drawing.Font('Calibri',9)
$cb_PMNSOffload.ForeColor        = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_PMNSOffload.BackColor        = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label29                         = New-Object system.Windows.Forms.Label
$Label29.text                    = "PMARPOffload"
$Label29.AutoSize                = $true
$Label29.width                   = 25
$Label29.height                  = 10
$Label29.location                = New-Object System.Drawing.Point(9,216)
$Label29.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label29.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_PMARPOffload                 = New-Object system.Windows.Forms.ComboBox
$cb_PMARPOffload.width           = 190
$cb_PMARPOffload.height          = 20
@('0 - Disabled','1 - Enabled') | ForEach-Object {[void] $cb_PMARPOffload.Items.Add($_)}
$cb_PMARPOffload.location        = New-Object System.Drawing.Point(193,213)
$cb_PMARPOffload.Font            = New-Object System.Drawing.Font('Calibri',9)
$cb_PMARPOffload.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_PMARPOffload.BackColor       = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_PriorityVLANTag              = New-Object system.Windows.Forms.ComboBox
$cb_PriorityVLANTag.width        = 190
$cb_PriorityVLANTag.height       = 20
@('0 - Paketpriorität and VLAN disabled','1 - Paketpriorität enabled','2 - VLAN enabled','3 - Paketpriorität and VLAN enabled') | ForEach-Object {[void] $cb_PriorityVLANTag.Items.Add($_)}
$cb_PriorityVLANTag.location     = New-Object System.Drawing.Point(193,256)
$cb_PriorityVLANTag.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_PriorityVLANTag.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_PriorityVLANTag.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label00                         = New-Object system.Windows.Forms.Label
$Label00.text                    = "PriorityVLANTag"
$Label00.AutoSize                = $true
$Label00.width                   = 25
$Label00.height                  = 10
$Label00.location                = New-Object System.Drawing.Point(9,259)
$Label00.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label00.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label30                         = New-Object system.Windows.Forms.Label
$Label30.text                    = "ReceiveBuffers"
$Label30.AutoSize                = $true
$Label30.width                   = 25
$Label30.height                  = 10
$Label30.location                = New-Object System.Drawing.Point(9,281)
$Label30.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label30.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ReceiveBuffers               = New-Object system.Windows.Forms.ComboBox
$cb_ReceiveBuffers.width         = 190
$cb_ReceiveBuffers.height        = 20
$cb_ReceiveBuffers.location      = New-Object System.Drawing.Point(193,278)
$cb_ReceiveBuffers.Font          = New-Object System.Drawing.Font('Calibri',9)
$cb_ReceiveBuffers.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ReceiveBuffers.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label31                         = New-Object system.Windows.Forms.Label
$Label31.text                    = "TransmitBuffers"
$Label31.AutoSize                = $true
$Label31.width                   = 25
$Label31.height                  = 10
$Label31.location                = New-Object System.Drawing.Point(9,303)
$Label31.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label31.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_TransmitBuffers              = New-Object system.Windows.Forms.ComboBox
$cb_TransmitBuffers.width        = 190
$cb_TransmitBuffers.height       = 20
$cb_TransmitBuffers.location     = New-Object System.Drawing.Point(193,300)
$cb_TransmitBuffers.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_TransmitBuffers.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_TransmitBuffers.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Groupbox5                       = New-Object system.Windows.Forms.Groupbox
$Groupbox5.height                = 66
$Groupbox5.width                 = 230
$Groupbox5.text                  = "RSS Global"
$Groupbox5.location              = New-Object System.Drawing.Point(10,369)

$Label32                         = New-Object system.Windows.Forms.Label
$Label32.text                    = "TCP/IP_RssBaseCpu:"
$Label32.AutoSize                = $true
$Label32.width                   = 25
$Label32.height                  = 10
$Label32.location                = New-Object System.Drawing.Point(7,16)
$Label32.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label32.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label33                         = New-Object system.Windows.Forms.Label
$Label33.text                    = "NDIS_RssBaseCpu:"
$Label33.AutoSize                = $true
$Label33.width                   = 25
$Label33.height                  = 10
$Label33.location                = New-Object System.Drawing.Point(7,40)
$Label33.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label33.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_tcpiprssbasecpu              = New-Object system.Windows.Forms.TextBox
$cb_tcpiprssbasecpu.multiline    = $false
$cb_tcpiprssbasecpu.width        = 89
$cb_tcpiprssbasecpu.height       = 20
$cb_tcpiprssbasecpu.location     = New-Object System.Drawing.Point(133,14)
$cb_tcpiprssbasecpu.Font         = New-Object System.Drawing.Font('Calibri',10)
$cb_tcpiprssbasecpu.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_tcpiprssbasecpu.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_ndisrssbasecpu               = New-Object system.Windows.Forms.TextBox
$cb_ndisrssbasecpu.multiline     = $false
$cb_ndisrssbasecpu.width         = 89
$cb_ndisrssbasecpu.height        = 20
$cb_ndisrssbasecpu.location      = New-Object System.Drawing.Point(133,38)
$cb_ndisrssbasecpu.Font          = New-Object System.Drawing.Font('Calibri',10)
$cb_ndisrssbasecpu.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ndisrssbasecpu.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Groupbox4                       = New-Object system.Windows.Forms.Groupbox
$Groupbox4.height                = 173
$Groupbox4.width                 = 321
$Groupbox4.text                  = "PowerSaving Settings"
$Groupbox4.location              = New-Object System.Drawing.Point(570,715)

$Label34                         = New-Object system.Windows.Forms.Label
$Label34.text                    = "(APM) sleep states:"
$Label34.AutoSize                = $true
$Label34.width                   = 25
$Label34.height                  = 10
$Label34.location                = New-Object System.Drawing.Point(8,18)
$Label34.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label34.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EnablePME                    = New-Object system.Windows.Forms.ComboBox
$cb_EnablePME.width              = 108
$cb_EnablePME.height             = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_EnablePME.Items.Add($_)}
$cb_EnablePME.location           = New-Object System.Drawing.Point(204,15)
$cb_EnablePME.Font               = New-Object System.Drawing.Font('Calibri',9)
$cb_EnablePME.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EnablePME.BackColor          = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_applypowersettings          = New-Object system.Windows.Forms.Button
$btn_applypowersettings.text     = "Apply"
$btn_applypowersettings.width    = 60
$btn_applypowersettings.height   = 21
$btn_applypowersettings.location  = New-Object System.Drawing.Point(570,893)
$btn_applypowersettings.Font     = New-Object System.Drawing.Font('Calibri',10)
$btn_applypowersettings.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label36                         = New-Object system.Windows.Forms.Label
$Label36.text                    = "DynamicPowerGating:"
$Label36.AutoSize                = $true
$Label36.width                   = 25
$Label36.height                  = 10
$Label36.location                = New-Object System.Drawing.Point(8,40)
$Label36.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label36.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EnableDynamicPowerGating     = New-Object system.Windows.Forms.ComboBox
$cb_EnableDynamicPowerGating.width  = 108
$cb_EnableDynamicPowerGating.height  = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_EnableDynamicPowerGating.Items.Add($_)}
$cb_EnableDynamicPowerGating.location  = New-Object System.Drawing.Point(204,37)
$cb_EnableDynamicPowerGating.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_EnableDynamicPowerGating.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EnableDynamicPowerGating.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label37                         = New-Object system.Windows.Forms.Label
$Label37.text                    = "ConnectedPowerGating:"
$Label37.AutoSize                = $true
$Label37.width                   = 25
$Label37.height                  = 10
$Label37.location                = New-Object System.Drawing.Point(8,62)
$Label37.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label37.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EnableConnectedPowerGating   = New-Object system.Windows.Forms.ComboBox
$cb_EnableConnectedPowerGating.width  = 108
$cb_EnableConnectedPowerGating.height  = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_EnableConnectedPowerGating.Items.Add($_)}
$cb_EnableConnectedPowerGating.location  = New-Object System.Drawing.Point(204,59)
$cb_EnableConnectedPowerGating.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_EnableConnectedPowerGating.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EnableConnectedPowerGating.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label38                         = New-Object system.Windows.Forms.Label
$Label38.text                    = "AutoPowerSaveMode:"
$Label38.AutoSize                = $true
$Label38.width                   = 25
$Label38.height                  = 10
$Label38.location                = New-Object System.Drawing.Point(8,84)
$Label38.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label38.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_AutoPowerSaveModeEnabled     = New-Object system.Windows.Forms.ComboBox
$cb_AutoPowerSaveModeEnabled.width  = 108
$cb_AutoPowerSaveModeEnabled.height  = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_AutoPowerSaveModeEnabled.Items.Add($_)}
$cb_AutoPowerSaveModeEnabled.location  = New-Object System.Drawing.Point(204,81)
$cb_AutoPowerSaveModeEnabled.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_AutoPowerSaveModeEnabled.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_AutoPowerSaveModeEnabled.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_NicAutoPowerSaver            = New-Object system.Windows.Forms.ComboBox
$cb_NicAutoPowerSaver.width      = 108
$cb_NicAutoPowerSaver.height     = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_NicAutoPowerSaver.Items.Add($_)}
$cb_NicAutoPowerSaver.location   = New-Object System.Drawing.Point(204,103)
$cb_NicAutoPowerSaver.Font       = New-Object System.Drawing.Font('Calibri',9)
$cb_NicAutoPowerSaver.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_NicAutoPowerSaver.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label39                         = New-Object system.Windows.Forms.Label
$Label39.text                    = "NicAutoPowerSaver:"
$Label39.AutoSize                = $true
$Label39.width                   = 25
$Label39.height                  = 10
$Label39.location                = New-Object System.Drawing.Point(9,106)
$Label39.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label39.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label40                         = New-Object system.Windows.Forms.Label
$Label40.text                    = "DelayedPowerUp:"
$Label40.AutoSize                = $true
$Label40.width                   = 25
$Label40.height                  = 10
$Label40.location                = New-Object System.Drawing.Point(9,128)
$Label40.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label40.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisableDelayedPowerUp        = New-Object system.Windows.Forms.ComboBox
$cb_DisableDelayedPowerUp.width  = 108
$cb_DisableDelayedPowerUp.height  = 20
@('Enabled','Disabled') | ForEach-Object {[void] $cb_DisableDelayedPowerUp.Items.Add($_)}
$cb_DisableDelayedPowerUp.location  = New-Object System.Drawing.Point(204,125)
$cb_DisableDelayedPowerUp.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_DisableDelayedPowerUp.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisableDelayedPowerUp.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label41                         = New-Object system.Windows.Forms.Label
$Label41.text                    = "ReduceSpeedOnPowerDown:"
$Label41.AutoSize                = $true
$Label41.width                   = 25
$Label41.height                  = 10
$Label41.location                = New-Object System.Drawing.Point(9,150)
$Label41.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label41.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ReduceSpeedOnPowerDown       = New-Object system.Windows.Forms.ComboBox
$cb_ReduceSpeedOnPowerDown.width  = 108
$cb_ReduceSpeedOnPowerDown.height  = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_ReduceSpeedOnPowerDown.Items.Add($_)}
$cb_ReduceSpeedOnPowerDown.location  = New-Object System.Drawing.Point(204,147)
$cb_ReduceSpeedOnPowerDown.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_ReduceSpeedOnPowerDown.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ReduceSpeedOnPowerDown.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label35                         = New-Object system.Windows.Forms.Label
$Label35.text                    = "DisablePortScaling:"
$Label35.AutoSize                = $true
$Label35.width                   = 25
$Label35.height                  = 10
$Label35.location                = New-Object System.Drawing.Point(5,162)
$Label35.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label35.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisablePortScaling           = New-Object system.Windows.Forms.ComboBox
$cb_DisablePortScaling.width     = 108
$cb_DisablePortScaling.height    = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_DisablePortScaling.Items.Add($_)}
$cb_DisablePortScaling.location  = New-Object System.Drawing.Point(117,160)
$cb_DisablePortScaling.Font      = New-Object System.Drawing.Font('Calibri',9)
$cb_DisablePortScaling.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisablePortScaling.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label42                         = New-Object system.Windows.Forms.Label
$Label42.text                    = "ManyCoreScaling:"
$Label42.AutoSize                = $true
$Label42.width                   = 25
$Label42.height                  = 10
$Label42.location                = New-Object System.Drawing.Point(5,186)
$Label42.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label42.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ManyCoreScaling              = New-Object system.Windows.Forms.ComboBox
$cb_ManyCoreScaling.width        = 108
$cb_ManyCoreScaling.height       = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_ManyCoreScaling.Items.Add($_)}
$cb_ManyCoreScaling.location     = New-Object System.Drawing.Point(117,184)
$cb_ManyCoreScaling.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_ManyCoreScaling.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ManyCoreScaling.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Groupbox7                       = New-Object system.Windows.Forms.Groupbox
$Groupbox7.height                = 576
$Groupbox7.width                 = 312
$Groupbox7.text                  = "Interface Settings"
$Groupbox7.location              = New-Object System.Drawing.Point(252,314)

$Label44                         = New-Object system.Windows.Forms.Label
$Label44.text                    = "AdvertiseDefaultRoute:"
$Label44.AutoSize                = $true
$Label44.width                   = 25
$Label44.height                  = 10
$Label44.location                = New-Object System.Drawing.Point(10,40)
$Label44.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label44.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_AdvertiseDefaultRoute        = New-Object system.Windows.Forms.ComboBox
$cb_AdvertiseDefaultRoute.width  = 108
$cb_AdvertiseDefaultRoute.height  = 20
$cb_AdvertiseDefaultRoute.location  = New-Object System.Drawing.Point(194,37)
$cb_AdvertiseDefaultRoute.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_AdvertiseDefaultRoute.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_AdvertiseDefaultRoute.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label45                         = New-Object system.Windows.Forms.Label
$Label45.text                    = "Advertising:"
$Label45.AutoSize                = $true
$Label45.width                   = 25
$Label45.height                  = 10
$Label45.location                = New-Object System.Drawing.Point(10,62)
$Label45.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label45.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_Advertising                  = New-Object system.Windows.Forms.ComboBox
$cb_Advertising.width            = 108
$cb_Advertising.height           = 20
$cb_Advertising.location         = New-Object System.Drawing.Point(194,59)
$cb_Advertising.Font             = New-Object System.Drawing.Font('Calibri',9)
$cb_Advertising.ForeColor        = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_Advertising.BackColor        = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label46                         = New-Object system.Windows.Forms.Label
$Label46.text                    = "AutomaticMetric:"
$Label46.AutoSize                = $true
$Label46.width                   = 25
$Label46.height                  = 10
$Label46.location                = New-Object System.Drawing.Point(10,84)
$Label46.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label46.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_AutomaticMetric              = New-Object system.Windows.Forms.ComboBox
$cb_AutomaticMetric.width        = 108
$cb_AutomaticMetric.height       = 20
$cb_AutomaticMetric.location     = New-Object System.Drawing.Point(194,81)
$cb_AutomaticMetric.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_AutomaticMetric.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_AutomaticMetric.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_ClampMss                     = New-Object system.Windows.Forms.ComboBox
$cb_ClampMss.width               = 108
$cb_ClampMss.height              = 20
$cb_ClampMss.location            = New-Object System.Drawing.Point(194,103)
$cb_ClampMss.Font                = New-Object System.Drawing.Font('Calibri',9)
$cb_ClampMss.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ClampMss.BackColor           = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label47                         = New-Object system.Windows.Forms.Label
$Label47.text                    = "ClampMss:"
$Label47.AutoSize                = $true
$Label47.width                   = 25
$Label47.height                  = 10
$Label47.location                = New-Object System.Drawing.Point(10,106)
$Label47.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label47.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DirectedMacWolPattern        = New-Object system.Windows.Forms.ComboBox
$cb_DirectedMacWolPattern.width  = 108
$cb_DirectedMacWolPattern.height  = 20
$cb_DirectedMacWolPattern.location  = New-Object System.Drawing.Point(194,125)
$cb_DirectedMacWolPattern.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_DirectedMacWolPattern.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DirectedMacWolPattern.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label48                         = New-Object system.Windows.Forms.Label
$Label48.text                    = "DirectedMacWolPattern:"
$Label48.AutoSize                = $true
$Label48.width                   = 25
$Label48.height                  = 10
$Label48.location                = New-Object System.Drawing.Point(10,128)
$Label48.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label48.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label49                         = New-Object system.Windows.Forms.Label
$Label49.text                    = "EcnMarking:"
$Label49.AutoSize                = $true
$Label49.width                   = 25
$Label49.height                  = 10
$Label49.location                = New-Object System.Drawing.Point(10,150)
$Label49.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label49.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EcnMarking                   = New-Object system.Windows.Forms.ComboBox
$cb_EcnMarking.width             = 108
$cb_EcnMarking.height            = 20
$cb_EcnMarking.location          = New-Object System.Drawing.Point(194,147)
$cb_EcnMarking.Font              = New-Object System.Drawing.Font('Calibri',9)
$cb_EcnMarking.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EcnMarking.BackColor         = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label50                         = New-Object system.Windows.Forms.Label
$Label50.text                    = "ForceArpNdWolPattern:"
$Label50.AutoSize                = $true
$Label50.width                   = 25
$Label50.height                  = 10
$Label50.location                = New-Object System.Drawing.Point(10,172)
$Label50.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label50.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ForceArpNdWolPattern         = New-Object system.Windows.Forms.ComboBox
$cb_ForceArpNdWolPattern.width   = 108
$cb_ForceArpNdWolPattern.height  = 20
$cb_ForceArpNdWolPattern.location  = New-Object System.Drawing.Point(194,169)
$cb_ForceArpNdWolPattern.Font    = New-Object System.Drawing.Font('Calibri',9)
$cb_ForceArpNdWolPattern.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ForceArpNdWolPattern.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label51                         = New-Object system.Windows.Forms.Label
$Label51.text                    = "Forwarding:"
$Label51.AutoSize                = $true
$Label51.width                   = 25
$Label51.height                  = 10
$Label51.location                = New-Object System.Drawing.Point(10,194)
$Label51.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label51.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_Forwarding                   = New-Object system.Windows.Forms.ComboBox
$cb_Forwarding.width             = 108
$cb_Forwarding.height            = 20
$cb_Forwarding.location          = New-Object System.Drawing.Point(194,191)
$cb_Forwarding.Font              = New-Object System.Drawing.Font('Calibri',9)
$cb_Forwarding.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_Forwarding.BackColor         = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_IgnoreDefaultRoutes          = New-Object system.Windows.Forms.ComboBox
$cb_IgnoreDefaultRoutes.width    = 108
$cb_IgnoreDefaultRoutes.height   = 20
$cb_IgnoreDefaultRoutes.location  = New-Object System.Drawing.Point(194,213)
$cb_IgnoreDefaultRoutes.Font     = New-Object System.Drawing.Font('Calibri',9)
$cb_IgnoreDefaultRoutes.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_IgnoreDefaultRoutes.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label52                         = New-Object system.Windows.Forms.Label
$Label52.text                    = "IgnoreDefaultRoutes:"
$Label52.AutoSize                = $true
$Label52.width                   = 25
$Label52.height                  = 10
$Label52.location                = New-Object System.Drawing.Point(10,216)
$Label52.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label52.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label53                         = New-Object system.Windows.Forms.Label
$Label53.text                    = "ManagedAddressConfiguration:"
$Label53.AutoSize                = $true
$Label53.width                   = 25
$Label53.height                  = 10
$Label53.location                = New-Object System.Drawing.Point(10,238)
$Label53.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label53.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_ManagedAddressConfiguration   = New-Object system.Windows.Forms.ComboBox
$cb_ManagedAddressConfiguration.width  = 108
$cb_ManagedAddressConfiguration.height  = 20
$cb_ManagedAddressConfiguration.location  = New-Object System.Drawing.Point(194,235)
$cb_ManagedAddressConfiguration.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_ManagedAddressConfiguration.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_ManagedAddressConfiguration.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label54                         = New-Object system.Windows.Forms.Label
$Label54.text                    = "NeighborDiscoverySupported:"
$Label54.AutoSize                = $true
$Label54.width                   = 25
$Label54.height                  = 10
$Label54.location                = New-Object System.Drawing.Point(10,260)
$Label54.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label54.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_NeighborDiscoverySupported   = New-Object system.Windows.Forms.ComboBox
$cb_NeighborDiscoverySupported.width  = 108
$cb_NeighborDiscoverySupported.height  = 20
$cb_NeighborDiscoverySupported.location  = New-Object System.Drawing.Point(194,257)
$cb_NeighborDiscoverySupported.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_NeighborDiscoverySupported.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_NeighborDiscoverySupported.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label55                         = New-Object system.Windows.Forms.Label
$Label55.text                    = "NeighborUnreachDetection:"
$Label55.AutoSize                = $true
$Label55.width                   = 25
$Label55.height                  = 10
$Label55.location                = New-Object System.Drawing.Point(11,282)
$Label55.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label55.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_NeighborUnreachabilityDetection   = New-Object system.Windows.Forms.ComboBox
$cb_NeighborUnreachabilityDetection.width  = 108
$cb_NeighborUnreachabilityDetection.height  = 20
$cb_NeighborUnreachabilityDetection.location  = New-Object System.Drawing.Point(194,279)
$cb_NeighborUnreachabilityDetection.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_NeighborUnreachabilityDetection.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_NeighborUnreachabilityDetection.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label56                         = New-Object system.Windows.Forms.Label
$Label56.text                    = "OtherStatefulConfiguration:"
$Label56.AutoSize                = $true
$Label56.width                   = 25
$Label56.height                  = 10
$Label56.location                = New-Object System.Drawing.Point(10,304)
$Label56.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label56.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_OtherStatefulConfiguration   = New-Object system.Windows.Forms.ComboBox
$cb_OtherStatefulConfiguration.width  = 108
$cb_OtherStatefulConfiguration.height  = 20
$cb_OtherStatefulConfiguration.location  = New-Object System.Drawing.Point(194,301)
$cb_OtherStatefulConfiguration.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_OtherStatefulConfiguration.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_OtherStatefulConfiguration.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label57                         = New-Object system.Windows.Forms.Label
$Label57.text                    = "RouterDiscovery:"
$Label57.AutoSize                = $true
$Label57.width                   = 25
$Label57.height                  = 10
$Label57.location                = New-Object System.Drawing.Point(10,326)
$Label57.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label57.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_RouterDiscovery              = New-Object system.Windows.Forms.ComboBox
$cb_RouterDiscovery.width        = 108
$cb_RouterDiscovery.height       = 20
$cb_RouterDiscovery.location     = New-Object System.Drawing.Point(194,323)
$cb_RouterDiscovery.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_RouterDiscovery.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_RouterDiscovery.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label58                         = New-Object system.Windows.Forms.Label
$Label58.text                    = "Store:"
$Label58.AutoSize                = $true
$Label58.width                   = 25
$Label58.height                  = 10
$Label58.location                = New-Object System.Drawing.Point(10,348)
$Label58.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label58.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_Store                        = New-Object system.Windows.Forms.ComboBox
$cb_Store.width                  = 108
$cb_Store.height                 = 20
$cb_Store.location               = New-Object System.Drawing.Point(194,345)
$cb_Store.Font                   = New-Object System.Drawing.Font('Calibri',9)
$cb_Store.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#f5a623")
$cb_Store.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label59                         = New-Object system.Windows.Forms.Label
$Label59.text                    = "WeakHostReceive:"
$Label59.AutoSize                = $true
$Label59.width                   = 25
$Label59.height                  = 10
$Label59.location                = New-Object System.Drawing.Point(10,370)
$Label59.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label59.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_WeakHostReceive              = New-Object system.Windows.Forms.ComboBox
$cb_WeakHostReceive.width        = 108
$cb_WeakHostReceive.height       = 20
$cb_WeakHostReceive.location     = New-Object System.Drawing.Point(194,367)
$cb_WeakHostReceive.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_WeakHostReceive.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_WeakHostReceive.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label60                         = New-Object system.Windows.Forms.Label
$Label60.text                    = "WeakHostSend:"
$Label60.AutoSize                = $true
$Label60.width                   = 25
$Label60.height                  = 10
$Label60.location                = New-Object System.Drawing.Point(10,392)
$Label60.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label60.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_WeakHostSend                 = New-Object system.Windows.Forms.ComboBox
$cb_WeakHostSend.width           = 108
$cb_WeakHostSend.height          = 20
$cb_WeakHostSend.location        = New-Object System.Drawing.Point(194,389)
$cb_WeakHostSend.Font            = New-Object System.Drawing.Font('Calibri',9)
$cb_WeakHostSend.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_WeakHostSend.BackColor       = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label61                         = New-Object system.Windows.Forms.Label
$Label61.text                    = "CurrentHopLimit:"
$Label61.AutoSize                = $true
$Label61.width                   = 25
$Label61.height                  = 10
$Label61.location                = New-Object System.Drawing.Point(10,414)
$Label61.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label61.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_CurrentHopLimit              = New-Object system.Windows.Forms.TextBox
$tb_CurrentHopLimit.multiline    = $false
$tb_CurrentHopLimit.width        = 71
$tb_CurrentHopLimit.height       = 20
$tb_CurrentHopLimit.location     = New-Object System.Drawing.Point(195,411)
$tb_CurrentHopLimit.Font         = New-Object System.Drawing.Font('Calibri',10)
$tb_CurrentHopLimit.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_CurrentHopLimit.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label62                         = New-Object system.Windows.Forms.Label
$Label62.text                    = "BaseReachableTime:"
$Label62.AutoSize                = $true
$Label62.width                   = 25
$Label62.height                  = 10
$Label62.location                = New-Object System.Drawing.Point(10,436)
$Label62.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label62.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_BaseReachableTime            = New-Object system.Windows.Forms.TextBox
$tb_BaseReachableTime.multiline  = $false
$tb_BaseReachableTime.width      = 71
$tb_BaseReachableTime.height     = 20
$tb_BaseReachableTime.location   = New-Object System.Drawing.Point(195,433)
$tb_BaseReachableTime.Font       = New-Object System.Drawing.Font('Calibri',10)
$tb_BaseReachableTime.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_BaseReachableTime.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$tb_ReachableTime                = New-Object system.Windows.Forms.TextBox
$tb_ReachableTime.multiline      = $false
$tb_ReachableTime.width          = 71
$tb_ReachableTime.height         = 20
$tb_ReachableTime.location       = New-Object System.Drawing.Point(195,477)
$tb_ReachableTime.Font           = New-Object System.Drawing.Font('Calibri',10)
$tb_ReachableTime.ForeColor      = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_ReachableTime.BackColor      = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label63                         = New-Object system.Windows.Forms.Label
$Label63.text                    = "ReachableTime:"
$Label63.AutoSize                = $true
$Label63.width                   = 25
$Label63.height                  = 10
$Label63.location                = New-Object System.Drawing.Point(10,480)
$Label63.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label63.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label64                         = New-Object system.Windows.Forms.Label
$Label64.text                    = "DadRetransmitTime:"
$Label64.AutoSize                = $true
$Label64.width                   = 25
$Label64.height                  = 10
$Label64.location                = New-Object System.Drawing.Point(10,502)
$Label64.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label64.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_DadRetransmitTime            = New-Object system.Windows.Forms.TextBox
$tb_DadRetransmitTime.multiline  = $false
$tb_DadRetransmitTime.width      = 71
$tb_DadRetransmitTime.height     = 20
$tb_DadRetransmitTime.location   = New-Object System.Drawing.Point(195,499)
$tb_DadRetransmitTime.Font       = New-Object System.Drawing.Font('Calibri',10)
$tb_DadRetransmitTime.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_DadRetransmitTime.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label65                         = New-Object system.Windows.Forms.Label
$Label65.text                    = "DadTransmits:"
$Label65.AutoSize                = $true
$Label65.width                   = 25
$Label65.height                  = 10
$Label65.location                = New-Object System.Drawing.Point(10,524)
$Label65.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label65.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_DadTransmits                 = New-Object system.Windows.Forms.TextBox
$tb_DadTransmits.multiline       = $false
$tb_DadTransmits.width           = 71
$tb_DadTransmits.height          = 20
$tb_DadTransmits.location        = New-Object System.Drawing.Point(195,521)
$tb_DadTransmits.Font            = New-Object System.Drawing.Font('Calibri',10)
$tb_DadTransmits.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_DadTransmits.BackColor       = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label66                         = New-Object system.Windows.Forms.Label
$Label66.text                    = "NlMtu:"
$Label66.AutoSize                = $true
$Label66.width                   = 25
$Label66.height                  = 10
$Label66.location                = New-Object System.Drawing.Point(10,546)
$Label66.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label66.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_NlMtu                        = New-Object system.Windows.Forms.TextBox
$tb_NlMtu.multiline              = $false
$tb_NlMtu.width                  = 71
$tb_NlMtu.height                 = 20
$tb_NlMtu.location               = New-Object System.Drawing.Point(195,543)
$tb_NlMtu.Font                   = New-Object System.Drawing.Font('Calibri',10)
$tb_NlMtu.ForeColor              = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_NlMtu.BackColor              = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label67                         = New-Object system.Windows.Forms.Label
$Label67.text                    = "RetransmitTime:"
$Label67.AutoSize                = $true
$Label67.width                   = 25
$Label67.height                  = 10
$Label67.location                = New-Object System.Drawing.Point(10,458)
$Label67.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label67.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_RetransmitTime               = New-Object system.Windows.Forms.TextBox
$tb_RetransmitTime.multiline     = $false
$tb_RetransmitTime.width         = 71
$tb_RetransmitTime.height        = 20
$tb_RetransmitTime.location      = New-Object System.Drawing.Point(195,455)
$tb_RetransmitTime.Font          = New-Object System.Drawing.Font('Calibri',10)
$tb_RetransmitTime.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_RetransmitTime.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_applyall                    = New-Object system.Windows.Forms.Button
$btn_applyall.text               = "Apply All"
$btn_applyall.width              = 78
$btn_applyall.height             = 20
$btn_applyall.location           = New-Object System.Drawing.Point(427,16)
$btn_applyall.Font               = New-Object System.Drawing.Font('Calibri',10)
$btn_applyall.ForeColor          = [System.Drawing.ColorTranslator]::FromHtml("#7ed321")

$Groupbox6                       = New-Object system.Windows.Forms.Groupbox
$Groupbox6.height                = 111
$Groupbox6.width                 = 230
$Groupbox6.text                  = "Interrupt Settings"
$Groupbox6.location              = New-Object System.Drawing.Point(10,443)

$lb_MsiMode                      = New-Object system.Windows.Forms.Label
$lb_MsiMode.text                 = "MSI Mode:"
$lb_MsiMode.AutoSize             = $true
$lb_MsiMode.width                = 25
$lb_MsiMode.height               = 10
$lb_MsiMode.location             = New-Object System.Drawing.Point(7,15)
$lb_MsiMode.Font                 = New-Object System.Drawing.Font('Calibri',10)
$lb_MsiMode.ForeColor            = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_MsiMode                      = New-Object system.Windows.Forms.ComboBox
$cb_MsiMode.width                = 108
$cb_MsiMode.height               = 20
@('Disabled','Enabled') | ForEach-Object {[void] $cb_MsiMode.Items.Add($_)}
$cb_MsiMode.location             = New-Object System.Drawing.Point(114,11)
$cb_MsiMode.Font                 = New-Object System.Drawing.Font('Calibri',9)
$cb_MsiMode.ForeColor            = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_MsiMode.BackColor            = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$lb_InterruptPriority            = New-Object system.Windows.Forms.Label
$lb_InterruptPriority.text       = "Interrupt Priority:"
$lb_InterruptPriority.AutoSize   = $true
$lb_InterruptPriority.width      = 25
$lb_InterruptPriority.height     = 10
$lb_InterruptPriority.location   = New-Object System.Drawing.Point(7,37)
$lb_InterruptPriority.Font       = New-Object System.Drawing.Font('Calibri',10)
$lb_InterruptPriority.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_InterruptPriority            = New-Object system.Windows.Forms.ComboBox
$cb_InterruptPriority.width      = 108
$cb_InterruptPriority.height     = 20
@('Undefined','Low','Normal','High') | ForEach-Object {[void] $cb_InterruptPriority.Items.Add($_)}
$cb_InterruptPriority.location   = New-Object System.Drawing.Point(114,33)
$cb_InterruptPriority.Font       = New-Object System.Drawing.Font('Calibri',9)
$cb_InterruptPriority.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_InterruptPriority.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$lb_DevicePolicy                 = New-Object system.Windows.Forms.Label
$lb_DevicePolicy.text            = "DevicePolicy:"
$lb_DevicePolicy.AutoSize        = $true
$lb_DevicePolicy.width           = 25
$lb_DevicePolicy.height          = 10
$lb_DevicePolicy.location        = New-Object System.Drawing.Point(7,60)
$lb_DevicePolicy.Font            = New-Object System.Drawing.Font('Calibri',10)
$lb_DevicePolicy.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DevicePolicy                 = New-Object system.Windows.Forms.ComboBox
$cb_DevicePolicy.width           = 214
$cb_DevicePolicy.height          = 20
@('MachineDefault','AllCloseProcessors','OneCloseProcessor','AllProcessorsInMachine','SpecifiedProcessors','SreadMessagesAcrossAllProcessors') | ForEach-Object {[void] $cb_DevicePolicy.Items.Add($_)}
$cb_DevicePolicy.location        = New-Object System.Drawing.Point(7,81)
$cb_DevicePolicy.Font            = New-Object System.Drawing.Font('Calibri',9)
$cb_DevicePolicy.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DevicePolicy.BackColor       = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_applyInterfaceSettings      = New-Object system.Windows.Forms.Button
$btn_applyInterfaceSettings.text  = "Apply"
$btn_applyInterfaceSettings.width  = 60
$btn_applyInterfaceSettings.height  = 21
$btn_applyInterfaceSettings.location  = New-Object System.Drawing.Point(252,894)
$btn_applyInterfaceSettings.Font  = New-Object System.Drawing.Font('Calibri',10)
$btn_applyInterfaceSettings.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label69                         = New-Object system.Windows.Forms.Label
$Label69.text                    = "ms"
$Label69.AutoSize                = $true
$Label69.width                   = 25
$Label69.height                  = 10
$Label69.location                = New-Object System.Drawing.Point(269,436)
$Label69.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label69.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label70                         = New-Object system.Windows.Forms.Label
$Label70.text                    = "ms"
$Label70.AutoSize                = $true
$Label70.width                   = 25
$Label70.height                  = 10
$Label70.location                = New-Object System.Drawing.Point(269,458)
$Label70.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label70.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label71                         = New-Object system.Windows.Forms.Label
$Label71.text                    = "ms"
$Label71.AutoSize                = $true
$Label71.width                   = 25
$Label71.height                  = 10
$Label71.location                = New-Object System.Drawing.Point(269,480)
$Label71.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label71.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label72                         = New-Object system.Windows.Forms.Label
$Label72.text                    = "bytes"
$Label72.AutoSize                = $true
$Label72.width                   = 25
$Label72.height                  = 10
$Label72.location                = New-Object System.Drawing.Point(269,546)
$Label72.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label72.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label73                         = New-Object system.Windows.Forms.Label
$Label73.text                    = "TxIntDelay:"
$Label73.AutoSize                = $true
$Label73.width                   = 25
$Label73.height                  = 10
$Label73.location                = New-Object System.Drawing.Point(9,368)
$Label73.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label73.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$tb_TxIntDelay                   = New-Object system.Windows.Forms.TextBox
$tb_TxIntDelay.multiline         = $false
$tb_TxIntDelay.width             = 71
$tb_TxIntDelay.height            = 20
$tb_TxIntDelay.location          = New-Object System.Drawing.Point(193,365)
$tb_TxIntDelay.Font              = New-Object System.Drawing.Font('Calibri',10)
$tb_TxIntDelay.ForeColor         = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$tb_TxIntDelay.BackColor         = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label74                         = New-Object system.Windows.Forms.Label
$Label74.text                    = "PacketDirect:"
$Label74.AutoSize                = $true
$Label74.width                   = 25
$Label74.height                  = 10
$Label74.location                = New-Object System.Drawing.Point(9,390)
$Label74.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label74.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_PacketDirect                 = New-Object system.Windows.Forms.ComboBox
$cb_PacketDirect.width           = 190
$cb_PacketDirect.height          = 20
@('Disabled','Enabled','Undefined') | ForEach-Object {[void] $cb_PacketDirect.Items.Add($_)}
$cb_PacketDirect.location        = New-Object System.Drawing.Point(193,387)
$cb_PacketDirect.Font            = New-Object System.Drawing.Font('Calibri',9)
$cb_PacketDirect.ForeColor       = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_PacketDirect.BackColor       = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label75                         = New-Object system.Windows.Forms.Label
$Label75.text                    = "Coalesce:"
$Label75.AutoSize                = $true
$Label75.width                   = 25
$Label75.height                  = 10
$Label75.location                = New-Object System.Drawing.Point(9,412)
$Label75.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label75.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EnableCoalesce               = New-Object system.Windows.Forms.ComboBox
$cb_EnableCoalesce.width         = 190
$cb_EnableCoalesce.height        = 20
@('Disabled','Enabled','Undefined') | ForEach-Object {[void] $cb_EnableCoalesce.Items.Add($_)}
$cb_EnableCoalesce.location      = New-Object System.Drawing.Point(193,409)
$cb_EnableCoalesce.Font          = New-Object System.Drawing.Font('Calibri',9)
$cb_EnableCoalesce.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EnableCoalesce.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label76                         = New-Object system.Windows.Forms.Label
$Label76.text                    = "UdpTxScaling:"
$Label76.AutoSize                = $true
$Label76.width                   = 25
$Label76.height                  = 10
$Label76.location                = New-Object System.Drawing.Point(9,457)
$Label76.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label76.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_EnableUdpTxScaling           = New-Object system.Windows.Forms.ComboBox
$cb_EnableUdpTxScaling.width     = 190
$cb_EnableUdpTxScaling.height    = 20
@('Disabled','Enabled','Undefined') | ForEach-Object {[void] $cb_EnableUdpTxScaling.Items.Add($_)}
$cb_EnableUdpTxScaling.location  = New-Object System.Drawing.Point(193,454)
$cb_EnableUdpTxScaling.Font      = New-Object System.Drawing.Font('Calibri',9)
$cb_EnableUdpTxScaling.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_EnableUdpTxScaling.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_IPv6                         = New-Object system.Windows.Forms.CheckBox
$cb_IPv6.text                    = "IPv6"
$cb_IPv6.AutoSize                = $true
$cb_IPv6.width                   = 95
$cb_IPv6.height                  = 20
$cb_IPv6.location                = New-Object System.Drawing.Point(503,300)
$cb_IPv6.Font                    = New-Object System.Drawing.Font('Calibri',7)
$cb_IPv6.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_IPv4                         = New-Object system.Windows.Forms.CheckBox
$cb_IPv4.text                    = "IPv4"
$cb_IPv4.AutoSize                = $true
$cb_IPv4.width                   = 95
$cb_IPv4.height                  = 20
$cb_IPv4.location                = New-Object System.Drawing.Point(456,300)
$cb_IPv4.Font                    = New-Object System.Drawing.Font('Calibri',7)
$cb_IPv4.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$btn_rssaddsupport               = New-Object system.Windows.Forms.Button
$btn_rssaddsupport.text          = "Enable RSS Support"
$btn_rssaddsupport.width         = 178
$btn_rssaddsupport.height        = 21
$btn_rssaddsupport.location      = New-Object System.Drawing.Point(10,339)
$btn_rssaddsupport.Font          = New-Object System.Drawing.Font('Calibri',9)
$btn_rssaddsupport.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#b8e986")

$Groupbox8                       = New-Object system.Windows.Forms.Groupbox
$Groupbox8.height                = 584
$Groupbox8.width                 = 386
$Groupbox8.text                  = "Tweaks"
$Groupbox8.location              = New-Object System.Drawing.Point(975,93)

$Label43                         = New-Object system.Windows.Forms.Label
$Label43.text                    = "DefaultReceiveWindow:"
$Label43.AutoSize                = $true
$Label43.width                   = 25
$Label43.height                  = 10
$Label43.location                = New-Object System.Drawing.Point(6,20)
$Label43.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label43.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label77                         = New-Object system.Windows.Forms.Label
$Label77.text                    = "DefaultSendWindow:"
$Label77.AutoSize                = $true
$Label77.width                   = 25
$Label77.height                  = 10
$Label77.location                = New-Object System.Drawing.Point(6,42)
$Label77.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label77.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$btn_registrytweaksapply         = New-Object system.Windows.Forms.Button
$btn_registrytweaksapply.text    = "Apply"
$btn_registrytweaksapply.width   = 60
$btn_registrytweaksapply.height  = 21
$btn_registrytweaksapply.location  = New-Object System.Drawing.Point(976,682)
$btn_registrytweaksapply.Font    = New-Object System.Drawing.Font('Calibri',10)
$btn_registrytweaksapply.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_Afd_defaultrecWin            = New-Object system.Windows.Forms.ComboBox
$cb_Afd_defaultrecWin.width      = 190
$cb_Afd_defaultrecWin.height     = 20
@('8192','32767') | ForEach-Object {[void] $cb_Afd_defaultrecWin.Items.Add($_)}
$cb_Afd_defaultrecWin.location   = New-Object System.Drawing.Point(177,17)
$cb_Afd_defaultrecWin.Font       = New-Object System.Drawing.Font('Calibri',9)
$cb_Afd_defaultrecWin.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_Afd_defaultrecWin.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_Afd_defaultSendWin           = New-Object system.Windows.Forms.ComboBox
$cb_Afd_defaultSendWin.width     = 190
$cb_Afd_defaultSendWin.height    = 20
@('8192','32767') | ForEach-Object {[void] $cb_Afd_defaultSendWin.Items.Add($_)}
$cb_Afd_defaultSendWin.location  = New-Object System.Drawing.Point(177,39)
$cb_Afd_defaultSendWin.Font      = New-Object System.Drawing.Font('Calibri',9)
$cb_Afd_defaultSendWin.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_Afd_defaultSendWin.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_Opacity                     = New-Object system.Windows.Forms.Button
$btn_Opacity.text                = "Opacity On/Off"
$btn_Opacity.width               = 112
$btn_Opacity.height              = 20
$btn_Opacity.location            = New-Object System.Drawing.Point(628,16)
$btn_Opacity.Font                = New-Object System.Drawing.Font('Calibri',10)
$btn_Opacity.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label78                         = New-Object system.Windows.Forms.Label
$Label78.text                    = "DisableAddressSharing:"
$Label78.AutoSize                = $true
$Label78.width                   = 25
$Label78.height                  = 10
$Label78.location                = New-Object System.Drawing.Point(5,284)
$Label78.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label78.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisableAddressSharing        = New-Object system.Windows.Forms.ComboBox
$cb_DisableAddressSharing.width  = 190
$cb_DisableAddressSharing.height  = 20
@('0','1') | ForEach-Object {[void] $cb_DisableAddressSharing.Items.Add($_)}
$cb_DisableAddressSharing.location  = New-Object System.Drawing.Point(177,281)
$cb_DisableAddressSharing.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_DisableAddressSharing.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisableAddressSharing.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label79                         = New-Object system.Windows.Forms.Label
$Label79.text                    = "DoNotHoldNICBuffers:"
$Label79.AutoSize                = $true
$Label79.width                   = 25
$Label79.height                  = 10
$Label79.location                = New-Object System.Drawing.Point(6,108)
$Label79.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label79.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DoNotHoldNICBuffers          = New-Object system.Windows.Forms.ComboBox
$cb_DoNotHoldNICBuffers.width    = 190
$cb_DoNotHoldNICBuffers.height   = 20
@('0','1') | ForEach-Object {[void] $cb_DoNotHoldNICBuffers.Items.Add($_)}
$cb_DoNotHoldNICBuffers.location  = New-Object System.Drawing.Point(177,105)
$cb_DoNotHoldNICBuffers.Font     = New-Object System.Drawing.Font('Calibri',9)
$cb_DoNotHoldNICBuffers.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DoNotHoldNICBuffers.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label80                         = New-Object system.Windows.Forms.Label
$Label80.text                    = "SmallBufferSize:"
$Label80.AutoSize                = $true
$Label80.width                   = 25
$Label80.height                  = 10
$Label80.location                = New-Object System.Drawing.Point(6,130)
$Label80.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label80.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_SmallBufferSize              = New-Object system.Windows.Forms.ComboBox
$cb_SmallBufferSize.width        = 190
$cb_SmallBufferSize.height       = 20
@('128') | ForEach-Object {[void] $cb_SmallBufferSize.Items.Add($_)}
$cb_SmallBufferSize.location     = New-Object System.Drawing.Point(177,127)
$cb_SmallBufferSize.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_SmallBufferSize.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_SmallBufferSize.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label81                         = New-Object system.Windows.Forms.Label
$Label81.text                    = "MediumBufferSize:"
$Label81.AutoSize                = $true
$Label81.width                   = 25
$Label81.height                  = 10
$Label81.location                = New-Object System.Drawing.Point(6,152)
$Label81.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label81.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_MediumBufferSize             = New-Object system.Windows.Forms.ComboBox
$cb_MediumBufferSize.width       = 190
$cb_MediumBufferSize.height      = 20
@('1504') | ForEach-Object {[void] $cb_MediumBufferSize.Items.Add($_)}
$cb_MediumBufferSize.location    = New-Object System.Drawing.Point(177,149)
$cb_MediumBufferSize.Font        = New-Object System.Drawing.Font('Calibri',9)
$cb_MediumBufferSize.ForeColor   = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_MediumBufferSize.BackColor   = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label82                         = New-Object system.Windows.Forms.Label
$Label82.text                    = "LargeBufferSize:"
$Label82.AutoSize                = $true
$Label82.width                   = 25
$Label82.height                  = 10
$Label82.location                = New-Object System.Drawing.Point(6,174)
$Label82.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label82.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_LargeBufferSize              = New-Object system.Windows.Forms.ComboBox
$cb_LargeBufferSize.width        = 190
$cb_LargeBufferSize.height       = 20
@('3876') | ForEach-Object {[void] $cb_LargeBufferSize.Items.Add($_)}
$cb_LargeBufferSize.location     = New-Object System.Drawing.Point(177,171)
$cb_LargeBufferSize.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_LargeBufferSize.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_LargeBufferSize.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label83                         = New-Object system.Windows.Forms.Label
$Label83.text                    = "HugeBufferSize:"
$Label83.AutoSize                = $true
$Label83.width                   = 25
$Label83.height                  = 10
$Label83.location                = New-Object System.Drawing.Point(6,196)
$Label83.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label83.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_HugeBufferSize               = New-Object system.Windows.Forms.ComboBox
$cb_HugeBufferSize.width         = 190
$cb_HugeBufferSize.height        = 20
$cb_HugeBufferSize.location      = New-Object System.Drawing.Point(177,193)
$cb_HugeBufferSize.Font          = New-Object System.Drawing.Font('Calibri',9)
$cb_HugeBufferSize.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_HugeBufferSize.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$cb_BufferAlignment              = New-Object system.Windows.Forms.ComboBox
$cb_BufferAlignment.width        = 190
$cb_BufferAlignment.height       = 20
@('1','0') | ForEach-Object {[void] $cb_BufferAlignment.Items.Add($_)}
$cb_BufferAlignment.location     = New-Object System.Drawing.Point(177,83)
$cb_BufferAlignment.Font         = New-Object System.Drawing.Font('Calibri',9)
$cb_BufferAlignment.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_BufferAlignment.BackColor    = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label84                         = New-Object system.Windows.Forms.Label
$Label84.text                    = "BufferAlignment:"
$Label84.AutoSize                = $true
$Label84.width                   = 25
$Label84.height                  = 10
$Label84.location                = New-Object System.Drawing.Point(6,86)
$Label84.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label84.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_BufferMultiplier             = New-Object system.Windows.Forms.ComboBox
$cb_BufferMultiplier.width       = 190
$cb_BufferMultiplier.height      = 20
@('512') | ForEach-Object {[void] $cb_BufferMultiplier.Items.Add($_)}
$cb_BufferMultiplier.location    = New-Object System.Drawing.Point(177,61)
$cb_BufferMultiplier.Font        = New-Object System.Drawing.Font('Calibri',9)
$cb_BufferMultiplier.ForeColor   = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_BufferMultiplier.BackColor   = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label85                         = New-Object system.Windows.Forms.Label
$Label85.text                    = "(Rec/Send) BufferMultiplier:"
$Label85.AutoSize                = $true
$Label85.width                   = 25
$Label85.height                  = 10
$Label85.location                = New-Object System.Drawing.Point(6,64)
$Label85.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label85.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label86                         = New-Object system.Windows.Forms.Label
$Label86.text                    = "SmallBufferListDepth:"
$Label86.AutoSize                = $true
$Label86.width                   = 25
$Label86.height                  = 10
$Label86.location                = New-Object System.Drawing.Point(6,218)
$Label86.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label86.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_SmallBufferListDepth         = New-Object system.Windows.Forms.ComboBox
$cb_SmallBufferListDepth.width   = 190
$cb_SmallBufferListDepth.height  = 20
@('8','16') | ForEach-Object {[void] $cb_SmallBufferListDepth.Items.Add($_)}
$cb_SmallBufferListDepth.location  = New-Object System.Drawing.Point(177,215)
$cb_SmallBufferListDepth.Font    = New-Object System.Drawing.Font('Calibri',9)
$cb_SmallBufferListDepth.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_SmallBufferListDepth.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label87                         = New-Object system.Windows.Forms.Label
$Label87.text                    = "MediumBufferListDepth:"
$Label87.AutoSize                = $true
$Label87.width                   = 25
$Label87.height                  = 10
$Label87.location                = New-Object System.Drawing.Point(6,240)
$Label87.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label87.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_MediumBufferListDepth        = New-Object system.Windows.Forms.ComboBox
$cb_MediumBufferListDepth.width  = 190
$cb_MediumBufferListDepth.height  = 20
@('4','8','16') | ForEach-Object {[void] $cb_MediumBufferListDepth.Items.Add($_)}
$cb_MediumBufferListDepth.location  = New-Object System.Drawing.Point(177,237)
$cb_MediumBufferListDepth.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_MediumBufferListDepth.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_MediumBufferListDepth.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label88                         = New-Object system.Windows.Forms.Label
$Label88.text                    = "LargBufferListDepth:"
$Label88.AutoSize                = $true
$Label88.width                   = 25
$Label88.height                  = 10
$Label88.location                = New-Object System.Drawing.Point(6,262)
$Label88.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label88.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_LargBufferListDepth          = New-Object system.Windows.Forms.ComboBox
$cb_LargBufferListDepth.width    = 190
$cb_LargBufferListDepth.height   = 20
@('0','2','10') | ForEach-Object {[void] $cb_LargBufferListDepth.Items.Add($_)}
$cb_LargBufferListDepth.location  = New-Object System.Drawing.Point(177,259)
$cb_LargBufferListDepth.Font     = New-Object System.Drawing.Font('Calibri',9)
$cb_LargBufferListDepth.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_LargBufferListDepth.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label89                         = New-Object system.Windows.Forms.Label
$Label89.text                    = "DisableChainedReceive:"
$Label89.AutoSize                = $true
$Label89.width                   = 25
$Label89.height                  = 10
$Label89.location                = New-Object System.Drawing.Point(5,306)
$Label89.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label89.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisableChainedReceive        = New-Object system.Windows.Forms.ComboBox
$cb_DisableChainedReceive.width  = 190
$cb_DisableChainedReceive.height  = 20
@('0','1') | ForEach-Object {[void] $cb_DisableChainedReceive.Items.Add($_)}
$cb_DisableChainedReceive.location  = New-Object System.Drawing.Point(177,303)
$cb_DisableChainedReceive.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_DisableChainedReceive.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisableChainedReceive.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label90                         = New-Object system.Windows.Forms.Label
$Label90.text                    = "DisableDirectAcceptEx:"
$Label90.AutoSize                = $true
$Label90.width                   = 25
$Label90.height                  = 10
$Label90.location                = New-Object System.Drawing.Point(5,328)
$Label90.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label90.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisableDirectAcceptEx        = New-Object system.Windows.Forms.ComboBox
$cb_DisableDirectAcceptEx.width  = 190
$cb_DisableDirectAcceptEx.height  = 20
@('0','1') | ForEach-Object {[void] $cb_DisableDirectAcceptEx.Items.Add($_)}
$cb_DisableDirectAcceptEx.location  = New-Object System.Drawing.Point(177,325)
$cb_DisableDirectAcceptEx.Font   = New-Object System.Drawing.Font('Calibri',9)
$cb_DisableDirectAcceptEx.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisableDirectAcceptEx.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label91                         = New-Object system.Windows.Forms.Label
$Label91.text                    = "DisableRawSecurity:"
$Label91.AutoSize                = $true
$Label91.width                   = 25
$Label91.height                  = 10
$Label91.location                = New-Object System.Drawing.Point(5,350)
$Label91.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label91.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DisableRawSecurity           = New-Object system.Windows.Forms.ComboBox
$cb_DisableRawSecurity.width     = 190
$cb_DisableRawSecurity.height    = 20
@('0','1') | ForEach-Object {[void] $cb_DisableRawSecurity.Items.Add($_)}
$cb_DisableRawSecurity.location  = New-Object System.Drawing.Point(177,347)
$cb_DisableRawSecurity.Font      = New-Object System.Drawing.Font('Calibri',9)
$cb_DisableRawSecurity.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DisableRawSecurity.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label92                         = New-Object system.Windows.Forms.Label
$Label92.text                    = "DynamicSendBufferDisable:"
$Label92.AutoSize                = $true
$Label92.width                   = 25
$Label92.height                  = 10
$Label92.location                = New-Object System.Drawing.Point(5,372)
$Label92.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label92.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_DynamicSendBufferDisable     = New-Object system.Windows.Forms.ComboBox
$cb_DynamicSendBufferDisable.width  = 190
$cb_DynamicSendBufferDisable.height  = 20
@('0','1') | ForEach-Object {[void] $cb_DynamicSendBufferDisable.Items.Add($_)}
$cb_DynamicSendBufferDisable.location  = New-Object System.Drawing.Point(177,369)
$cb_DynamicSendBufferDisable.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_DynamicSendBufferDisable.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_DynamicSendBufferDisable.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label93                         = New-Object system.Windows.Forms.Label
$Label93.text                    = "FastSendDatagramThreshold:"
$Label93.AutoSize                = $true
$Label93.width                   = 25
$Label93.height                  = 10
$Label93.location                = New-Object System.Drawing.Point(5,394)
$Label93.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label93.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_FastSendDatagramThreshold    = New-Object system.Windows.Forms.ComboBox
$cb_FastSendDatagramThreshold.width  = 190
$cb_FastSendDatagramThreshold.height  = 20
@('1024') | ForEach-Object {[void] $cb_FastSendDatagramThreshold.Items.Add($_)}
$cb_FastSendDatagramThreshold.location  = New-Object System.Drawing.Point(177,391)
$cb_FastSendDatagramThreshold.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_FastSendDatagramThreshold.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_FastSendDatagramThreshold.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label94                         = New-Object system.Windows.Forms.Label
$Label94.text                    = "FastCopyReceiveThreshold:"
$Label94.AutoSize                = $true
$Label94.width                   = 25
$Label94.height                  = 10
$Label94.location                = New-Object System.Drawing.Point(5,416)
$Label94.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label94.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_FastCopyReceiveThreshold     = New-Object system.Windows.Forms.ComboBox
$cb_FastCopyReceiveThreshold.width  = 190
$cb_FastCopyReceiveThreshold.height  = 20
@('1024') | ForEach-Object {[void] $cb_FastCopyReceiveThreshold.Items.Add($_)}
$cb_FastCopyReceiveThreshold.location  = New-Object System.Drawing.Point(177,413)
$cb_FastCopyReceiveThreshold.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_FastCopyReceiveThreshold.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_FastCopyReceiveThreshold.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label95                         = New-Object system.Windows.Forms.Label
$Label95.text                    = "IgnorePushBitOnReceives:"
$Label95.AutoSize                = $true
$Label95.width                   = 25
$Label95.height                  = 10
$Label95.location                = New-Object System.Drawing.Point(5,438)
$Label95.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label95.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_IgnorePushBitOnReceives      = New-Object system.Windows.Forms.ComboBox
$cb_IgnorePushBitOnReceives.width  = 190
$cb_IgnorePushBitOnReceives.height  = 20
@('0','1') | ForEach-Object {[void] $cb_IgnorePushBitOnReceives.Items.Add($_)}
$cb_IgnorePushBitOnReceives.location  = New-Object System.Drawing.Point(177,435)
$cb_IgnorePushBitOnReceives.Font  = New-Object System.Drawing.Font('Calibri',9)
$cb_IgnorePushBitOnReceives.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_IgnorePushBitOnReceives.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label96                         = New-Object system.Windows.Forms.Label
$Label96.text                    = "IgnoreOrderlyRelease:"
$Label96.AutoSize                = $true
$Label96.width                   = 25
$Label96.height                  = 10
$Label96.location                = New-Object System.Drawing.Point(5,460)
$Label96.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label96.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_IgnoreOrderlyRelease         = New-Object system.Windows.Forms.ComboBox
$cb_IgnoreOrderlyRelease.width   = 190
$cb_IgnoreOrderlyRelease.height  = 20
@('0','1') | ForEach-Object {[void] $cb_IgnoreOrderlyRelease.Items.Add($_)}
$cb_IgnoreOrderlyRelease.location  = New-Object System.Drawing.Point(177,457)
$cb_IgnoreOrderlyRelease.Font    = New-Object System.Drawing.Font('Calibri',9)
$cb_IgnoreOrderlyRelease.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_IgnoreOrderlyRelease.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label97                         = New-Object system.Windows.Forms.Label
$Label97.text                    = "TransmitWorker:"
$Label97.AutoSize                = $true
$Label97.width                   = 25
$Label97.height                  = 10
$Label97.location                = New-Object System.Drawing.Point(5,482)
$Label97.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label97.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_TransmitWorker               = New-Object system.Windows.Forms.ComboBox
$cb_TransmitWorker.width         = 190
$cb_TransmitWorker.height        = 20
@('16','32') | ForEach-Object {[void] $cb_TransmitWorker.Items.Add($_)}
$cb_TransmitWorker.location      = New-Object System.Drawing.Point(177,479)
$cb_TransmitWorker.Font          = New-Object System.Drawing.Font('Calibri',9)
$cb_TransmitWorker.ForeColor     = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_TransmitWorker.BackColor     = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Label98                         = New-Object system.Windows.Forms.Label
$Label98.text                    = "PriorityBoost:"
$Label98.AutoSize                = $true
$Label98.width                   = 25
$Label98.height                  = 10
$Label98.location                = New-Object System.Drawing.Point(5,504)
$Label98.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label98.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_PriorityBoost                = New-Object system.Windows.Forms.ComboBox
$cb_PriorityBoost.width          = 190
$cb_PriorityBoost.height         = 20
@('2','1','0') | ForEach-Object {[void] $cb_PriorityBoost.Items.Add($_)}
$cb_PriorityBoost.location       = New-Object System.Drawing.Point(177,501)
$cb_PriorityBoost.Font           = New-Object System.Drawing.Font('Calibri',9)
$cb_PriorityBoost.ForeColor      = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_PriorityBoost.BackColor      = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$btn_InterruptApply              = New-Object system.Windows.Forms.Button
$btn_InterruptApply.text         = "Apply"
$btn_InterruptApply.width        = 60
$btn_InterruptApply.height       = 21
$btn_InterruptApply.location     = New-Object System.Drawing.Point(11,560)
$btn_InterruptApply.Font         = New-Object System.Drawing.Font('Calibri',10)
$btn_InterruptApply.ForeColor    = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$Label68                         = New-Object system.Windows.Forms.Label
$Label68.text                    = "CoalesceBufferSize:"
$Label68.AutoSize                = $true
$Label68.width                   = 25
$Label68.height                  = 10
$Label68.location                = New-Object System.Drawing.Point(9,435)
$Label68.Font                    = New-Object System.Drawing.Font('Calibri',10)
$Label68.ForeColor               = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")

$cb_CoalesceBufferSize           = New-Object system.Windows.Forms.ComboBox
$cb_CoalesceBufferSize.width     = 190
$cb_CoalesceBufferSize.height    = 20
@('2048') | ForEach-Object {[void] $cb_CoalesceBufferSize.Items.Add($_)}
$cb_CoalesceBufferSize.location  = New-Object System.Drawing.Point(193,432)
$cb_CoalesceBufferSize.Font      = New-Object System.Drawing.Font('Calibri',9)
$cb_CoalesceBufferSize.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$cb_CoalesceBufferSize.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#171717")

$Form.controls.AddRange(@($cb_AdapterNamesCombo,$Label1,$Label2,$lbl_Path,$Label3,$lbl_ndisver,$Groupbox1,$btn_apply,$btn_unqueues,$btn_openreg,$Groupbox2,$btn_applyglobal,$Groupbox3,$btn_applyadv,$btn_adaptrest,$Groupbox5,$Groupbox4,$btn_applypowersettings,$Groupbox7,$btn_applyall,$Groupbox6,$btn_applyInterfaceSettings,$cb_IPv6,$cb_IPv4,$btn_rssaddsupport,$Groupbox8,$btn_registrytweaksapply,$btn_Opacity,$btn_InterruptApply))
$Groupbox1.controls.AddRange(@($Label4,$Label5,$Label6,$lbl_rssstatus,$cb_rss_onoff,$cb_rssqueues,$cb_rssprofile,$Label7,$cb_rssbaseproc,$Label8,$cb_rssmaxproc,$Label9,$cb_rssmaxprocs,$Label35,$cb_DisablePortScaling,$Label42,$cb_ManyCoreScaling))
$Groupbox2.controls.AddRange(@($Label10,$cb_osrss,$Label11,$cb_osrsc,$Label12,$cb_oschimney,$Label13,$cb_ostaskoff,$cb_osntd,$Label14,$cb_osntdais,$Label15,$cb_ospcf,$Label16))
$Groupbox3.controls.AddRange(@($Label17,$cb_flowcontrol,$Label18,$Label19,$Label20,$Label21,$Label22,$Label23,$cb_InterruptModeration,$cb_IPChecksumOffloadIPv4,$cb_TCPChecksumOffloadIPv4,$cb_TCPChecksumOffloadIPv6,$cb_UDPChecksumOffloadIPv4,$cb_UDPChecksumOffloadIPv6,$Label24,$cb_InterruptModerationRate,$Label25,$Label26,$cb_LsoV2IPv4,$cb_LsoV2IPv6,$Label27,$cb_LsoV1IPv4,$Label28,$cb_PMNSOffload,$Label29,$cb_PMARPOffload,$cb_PriorityVLANTag,$Label00,$Label30,$cb_ReceiveBuffers,$Label31,$cb_TransmitBuffers,$Label73,$tb_TxIntDelay,$Label74,$cb_PacketDirect,$Label75,$cb_EnableCoalesce,$Label76,$cb_EnableUdpTxScaling,$Label68,$cb_CoalesceBufferSize))
$Groupbox5.controls.AddRange(@($Label32,$Label33,$cb_tcpiprssbasecpu,$cb_ndisrssbasecpu))
$Groupbox4.controls.AddRange(@($Label34,$cb_EnablePME,$Label36,$cb_EnableDynamicPowerGating,$Label37,$cb_EnableConnectedPowerGating,$Label38,$cb_AutoPowerSaveModeEnabled,$cb_NicAutoPowerSaver,$Label39,$Label40,$cb_DisableDelayedPowerUp,$Label41,$cb_ReduceSpeedOnPowerDown))
$Groupbox7.controls.AddRange(@($Label44,$cb_AdvertiseDefaultRoute,$Label45,$cb_Advertising,$Label46,$cb_AutomaticMetric,$cb_ClampMss,$Label47,$cb_DirectedMacWolPattern,$Label48,$Label49,$cb_EcnMarking,$Label50,$cb_ForceArpNdWolPattern,$Label51,$cb_Forwarding,$cb_IgnoreDefaultRoutes,$Label52,$Label53,$cb_ManagedAddressConfiguration,$Label54,$cb_NeighborDiscoverySupported,$Label55,$cb_NeighborUnreachabilityDetection,$Label56,$cb_OtherStatefulConfiguration,$Label57,$cb_RouterDiscovery,$Label58,$cb_Store,$Label59,$cb_WeakHostReceive,$Label60,$cb_WeakHostSend,$Label61,$tb_CurrentHopLimit,$Label62,$tb_BaseReachableTime,$tb_ReachableTime,$Label63,$Label64,$tb_DadRetransmitTime,$Label65,$tb_DadTransmits,$Label66,$tb_NlMtu,$Label67,$tb_RetransmitTime,$Label69,$Label70,$Label71,$Label72))
$Groupbox6.controls.AddRange(@($lb_MsiMode,$cb_MsiMode,$lb_InterruptPriority,$cb_InterruptPriority,$lb_DevicePolicy,$cb_DevicePolicy))
$Groupbox8.controls.AddRange(@($Label43,$Label77,$cb_Afd_defaultrecWin,$cb_Afd_defaultSendWin,$Label78,$cb_DisableAddressSharing,$Label79,$cb_DoNotHoldNICBuffers,$Label80,$cb_SmallBufferSize,$Label81,$cb_MediumBufferSize,$Label82,$cb_LargeBufferSize,$Label83,$cb_HugeBufferSize,$cb_BufferAlignment,$Label84,$cb_BufferMultiplier,$Label85,$Label86,$cb_SmallBufferListDepth,$Label87,$cb_MediumBufferListDepth,$Label88,$cb_LargBufferListDepth,$Label89,$cb_DisableChainedReceive,$Label90,$cb_DisableDirectAcceptEx,$Label91,$cb_DisableRawSecurity,$Label92,$cb_DynamicSendBufferDisable,$Label93,$cb_FastSendDatagramThreshold,$Label94,$cb_FastCopyReceiveThreshold,$Label95,$cb_IgnorePushBitOnReceives,$Label96,$cb_IgnoreOrderlyRelease,$Label97,$cb_TransmitWorker,$Label98,$cb_PriorityBoost))


#region Logic 
#Cleaning Code
cls
Add-Type -Name Window -Namespace Console -MemberDefinition '
[DllImport("Kernel32.dll")]
public static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
public static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);'

#Write your logic code here

#GUI Settings
#$ErrorActionPreference="Stop"
#$Form.Opacity = "0.90"

#Deselect Content for Updating Text/Value Changes
$Form.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox1.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox2.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox3.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox4.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox5.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox7.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox6.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox8.Add_MouseClick({$Form.ActiveControl = $null})
$Groupbox9.Add_MouseClick({$Form.ActiveControl = $null})


#Groupboxes
$Groupbox1.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox1.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox2.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox2.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox3.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox3.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox4.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox4.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox5.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox5.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox7.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox7.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox6.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox6.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")
$Groupbox8.Font                = New-Object System.Drawing.Font('Calibri',10)
$Groupbox8.ForeColor           = [System.Drawing.ColorTranslator]::FromHtml("#4a90e2")


#Buttons
$btn_openreg.Flatstyle = 'Flat'
$btn_apply.Flatstyle = 'Flat'
$btn_applyglobal.Flatstyle = 'Flat'
$btn_applyadv.Flatstyle = 'Flat'
$btn_adaptrest.Flatstyle = 'Flat'
$btn_unqueues.Flatstyle = 'Flat'
#$btn_applotadapters.Flatstyle = 'Flat'
$btn_applypowersettings.Flatstyle = 'Flat'
$btn_applyall.Flatstyle = 'Flat'
$btn_applyInterfaceSettings.Flatstyle = 'Flat'
$btn_rssaddsupport.Flatstyle = 'Flat'
$btn_registrytweaksapply.Flatstyle = 'Flat'
$btn_Opacity.Flatstyle = 'Flat'
$btn_InterruptApply.Flatstyle = 'Flat'



function Set-ConsoleColor ($bc, $fc) {
    $Host.UI.RawUI.BackgroundColor = $bc
    $Host.UI.RawUI.ForegroundColor = $fc
    Clear-Host
}
Set-ConsoleColor 'Black' 'Green'

#Locked Combos
$cb_AdapterNamesCombo.Flatstyle = 'Flat'
$cb_rss_onoff.Flatstyle = 'Flat'
$cb_rssprofile.Flatstyle = 'Flat'
$cb_rssbaseproc.Flatstyle = 'Flat'
$cb_rssmaxproc.Flatstyle = 'Flat'
$cb_rssmaxprocs.Flatstyle = 'Flat'
$cb_rssqueues.Flatstyle = 'Flat'
$cb_osrss.Flatstyle = 'Flat'
$cb_osrsc.Flatstyle = 'Flat'
$cb_oschimney.Flatstyle = 'Flat'
$cb_ostaskoff.Flatstyle = 'Flat'
$cb_osntd.Flatstyle = 'Flat'
$cb_osntdais.Flatstyle = 'Flat'
$cb_ospcf.Flatstyle = 'Flat'
$cb_flowcontrol.Flatstyle = 'Flat'
$cb_IPChecksumOffloadIPv4.Flatstyle = 'Flat'
$cb_TCPChecksumOffloadIPv4.Flatstyle = 'Flat'
$cb_TCPChecksumOffloadIPv6.Flatstyle = 'Flat'
$cb_UDPChecksumOffloadIPv4.Flatstyle = 'Flat'
$cb_UDPChecksumOffloadIPv6.Flatstyle = 'Flat'
$cb_InterruptModeration.Flatstyle = 'Flat'
$cb_LsoV1IPv4.Flatstyle = 'Flat'
$cb_LsoV2IPv4.Flatstyle = 'Flat'
$cb_LsoV2IPv6.Flatstyle = 'Flat'
$cb_PMARPOffload.Flatstyle = 'Flat'
$cb_PMNSOffload.Flatstyle = 'Flat'
$cb_PriorityVLANTag.Flatstyle = 'Flat'
$cb_ReceiveBuffers.Flatstyle = 'Flat'
$cb_TransmitBuffers.Flatstyle = 'Flat'
$cb_InterruptModerationRate.Flatstyle = 'Flat'
$cb_CoalesceBufferSize.Flatstyle = 'Flat'
$cb_rss_onoff.DropDownStyle = 'DropDownList'
$cb_AdapterNamesCombo.DropDownStyle = 'DropDownList'
$cb_rssprofile.DropDownStyle = 'DropDownList'
$cb_rssbaseproc.DropDownStyle = 'DropDownList'
$cb_rssmaxproc.DropDownStyle = 'DropDownList'
$cb_rssmaxprocs.DropDownStyle = 'DropDownList'
$cb_rssqueues.DropDownStyle = 'DropDownList'
$cb_osrss.DropDownStyle = 'DropDownList'
$cb_osrsc.DropDownStyle = 'DropDownList'
$cb_oschimney.DropDownStyle = 'DropDownList'
$cb_ostaskoff.DropDownStyle = 'DropDownList'
$cb_osntd.DropDownStyle = 'DropDownList'
$cb_osntdais.DropDownStyle = 'DropDownList'
$cb_ospcf.DropDownStyle = 'DropDownList'
$cb_flowcontrol.DropDownStyle = 'DropDownList'
$cb_IPChecksumOffloadIPv4.DropDownStyle = 'DropDownList'
$cb_TCPChecksumOffloadIPv4.DropDownStyle = 'DropDownList'
$cb_TCPChecksumOffloadIPv6.DropDownStyle = 'DropDownList'
$cb_UDPChecksumOffloadIPv4.DropDownStyle = 'DropDownList'
$cb_UDPChecksumOffloadIPv6.DropDownStyle = 'DropDownList'
$cb_InterruptModeration.DropDownStyle = 'DropDownList'
$cb_LsoV1IPv4.DropDownStyle = 'DropDownList'
$cb_LsoV2IPv4.DropDownStyle = 'DropDownList'
$cb_LsoV2IPv6.DropDownStyle = 'DropDownList'
$cb_PMARPOffload.DropDownStyle = 'DropDownList'
$cb_PMNSOffload.DropDownStyle = 'DropDownList'
$cb_PriorityVLANTag.DropDownStyle = 'DropDownList'
#$cb_CoalesceBufferSize.DropDownStyle = 'DropDownList'


$cb_Afd_defaultrecWin.Flatstyle = 'Flat'
$cb_Afd_defaultSendWin.Flatstyle = 'Flat'
$cb_BufferMultiplier.Flatstyle = 'Flat'
$cb_DisableAddressSharing.Flatstyle = 'Flat'
$cb_BufferAlignment.Flatstyle = 'Flat'
$cb_DoNotHoldNICBuffers.Flatstyle = 'Flat'
$cb_SmallBufferSize.Flatstyle = 'Flat'
$cb_MediumBufferSize.Flatstyle = 'Flat'
$cb_LargeBufferSize.Flatstyle = 'Flat'
$cb_HugeBufferSize.Flatstyle = 'Flat'
$cb_SmallBufferListDepth.Flatstyle = 'Flat'
$cb_MediumBufferListDepth.Flatstyle = 'Flat'
$cb_LargBufferListDepth.Flatstyle = 'Flat'
$cb_DisableChainedReceive.Flatstyle = 'Flat'
$cb_DisableDirectAcceptEx.Flatstyle = 'Flat'
$cb_DisableRawSecurity.Flatstyle = 'Flat'
$cb_DynamicSendBufferDisable.Flatstyle = 'Flat'
$cb_FastSendDatagramThreshold.Flatstyle = 'Flat'
$cb_FastCopyReceiveThreshold.Flatstyle = 'Flat'
$cb_IgnorePushBitOnReceives.Flatstyle = 'Flat'
$cb_IgnoreOrderlyRelease.Flatstyle = 'Flat'
$cb_TransmitWorker.Flatstyle = 'Flat'
$cb_PriorityBoost.Flatstyle = 'Flat'

$cb_DisablePortScaling.Flatstyle = 'Flat'
$cb_ManyCoreScaling.Flatstyle = 'Flat'
$cb_DisablePortScaling.DropDownStyle = 'DropDownList'
$cb_ManyCoreScaling.DropDownStyle = 'DropDownList'

$cb_PacketDirect.Flatstyle = 'Flat'
$cb_PacketDirect.DropDownStyle = 'DropDownList'

$cb_EnableCoalesce.Flatstyle = 'Flat'
$cb_EnableCoalesce.DropDownStyle = 'DropDownList'
$cb_EnableUdpTxScaling.Flatstyle = 'Flat'
$cb_EnableUdpTxScaling.DropDownStyle = 'DropDownList'

$cb_EnablePME.Flatstyle = 'Flat'
$cb_EnableDynamicPowerGating.Flatstyle = 'Flat'
$cb_EnableConnectedPowerGating.Flatstyle = 'Flat'
$cb_AutoPowerSaveModeEnabled.Flatstyle = 'Flat'
$cb_NicAutoPowerSaver.Flatstyle = 'Flat'
$cb_DisableDelayedPowerUp.Flatstyle = 'Flat'
$cb_ReduceSpeedOnPowerDown.Flatstyle = 'Flat'
$cb_EnablePME.DropDownStyle = 'DropDownList'
$cb_EnableDynamicPowerGating.DropDownStyle = 'DropDownList'
$cb_EnableConnectedPowerGating.DropDownStyle = 'DropDownList'
$cb_AutoPowerSaveModeEnabled.DropDownStyle = 'DropDownList'
$cb_NicAutoPowerSaver.DropDownStyle = 'DropDownList'
$cb_DisableDelayedPowerUp.DropDownStyle = 'DropDownList'
$cb_ReduceSpeedOnPowerDown.DropDownStyle = 'DropDownList'

$cb_DevicePolicy.Flatstyle = 'Flat'
$cb_MsiMode.Flatstyle = 'Flat'
$cb_InterruptPriority.Flatstyle = 'Flat'
$cb_MsiMode.DropDownStyle = 'DropDownList'
$cb_InterruptPriority.DropDownStyle = 'DropDownList'
$cb_DevicePolicy.DropDownStyle = 'DropDownList'
$cb_MsiMode.Enabled = $false
$cb_InterruptPriority.Enabled = $false
$lb_DevicePolicy.Enabled = $false
$cb_DevicePolicy.Enabled = $false
$btn_InterruptApply.Enabled = $false

$cb_AdvertiseDefaultRoute.Flatstyle = 'Flat'
$cb_Advertising.Flatstyle = 'Flat'
$cb_AutomaticMetric.Flatstyle = 'Flat'
$cb_ClampMss.Flatstyle = 'Flat'
$cb_DirectedMacWolPattern.Flatstyle = 'Flat'
$cb_EcnMarking.Flatstyle = 'Flat'
$cb_ForceArpNdWolPattern.Flatstyle = 'Flat'
$cb_Forwarding.Flatstyle = 'Flat'
$cb_IgnoreDefaultRoutes.Flatstyle = 'Flat'
$cb_ManagedAddressConfiguration.Flatstyle = 'Flat'
$cb_NeighborDiscoverySupported.Flatstyle = 'Flat'
$cb_NeighborUnreachabilityDetection.Flatstyle = 'Flat'
$cb_OtherStatefulConfiguration.Flatstyle = 'Flat'
$cb_RouterDiscovery.Flatstyle = 'Flat'
$cb_Store.Flatstyle = 'Flat'
$cb_WeakHostReceive.Flatstyle = 'Flat'
$cb_WeakHostSend.Flatstyle = 'Flat'
$cb_AdvertiseDefaultRoute.DropDownStyle = 'DropDownList'
$cb_Advertising.DropDownStyle = 'DropDownList'
$cb_AutomaticMetric.DropDownStyle = 'DropDownList'
$cb_ClampMss.DropDownStyle = 'DropDownList'
$cb_DirectedMacWolPattern.DropDownStyle = 'DropDownList'
$cb_EcnMarking.DropDownStyle = 'DropDownList'
$cb_ForceArpNdWolPattern.DropDownStyle = 'DropDownList'
$cb_Forwarding.DropDownStyle = 'DropDownList'
$cb_IgnoreDefaultRoutes.DropDownStyle = 'DropDownList'
$cb_ManagedAddressConfiguration.DropDownStyle = 'DropDownList'
$cb_NeighborDiscoverySupported.DropDownStyle = 'DropDownList'
$cb_NeighborUnreachabilityDetection.DropDownStyle = 'DropDownList'
$cb_OtherStatefulConfiguration.DropDownStyle = 'DropDownList'
$cb_RouterDiscovery.DropDownStyle = 'DropDownList'
$cb_Store.DropDownStyle = 'DropDownList'
$cb_WeakHostReceive.DropDownStyle = 'DropDownList'
$cb_WeakHostSend.DropDownStyle = 'DropDownList'

# ========================================================

# Loading at Startup Global Settings (OS Settings not Adapter specific)
$cb_osrss.text = (Get-NetOffloadGlobalSetting | select -expand ReceiveSideScaling)
$cb_osrsc.text = (Get-NetOffloadGlobalSetting | select -expand ReceiveSegmentCoalescing)
$cb_oschimney.text = (Get-NetOffloadGlobalSetting | select -expand Chimney)
$cb_ostaskoff.text = (Get-NetOffloadGlobalSetting | select -expand TaskOffload)
$cb_osntd.text = (Get-NetOffloadGlobalSetting | select -expand NetworkDirect)
$cb_osntdais.text = (Get-NetOffloadGlobalSetting | select -expand NetworkDirectAcrossIPSubnets)
$cb_ospcf.text = (Get-NetOffloadGlobalSetting | select -expand PacketCoalescingFilter)

# ========================================================
# RSS Global
$ErrorActionPreference = "SilentlyContinue"
$Global:TCPIP_RegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"
$Global:NDIS_RegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\NDIS\Parameters"

$TCPIP_RssBaseCpu = (Get-ItemPropertyValue -Path "$TCPIP_RegPath" -Name "RssBaseCpu" -ErrorAction SilentlyContinue)
if ($TCPIP_RssBaseCpu -eq $null)
    {
        $cb_tcpiprssbasecpu.text = '0'
    }
    else
    {
        $cb_tcpiprssbasecpu.text = $TCPIP_RssBaseCpu
    }
$NDIS_RssBaseCpu = (Get-ItemPropertyValue -Path "$NDIS_RegPath" -Name "RssBaseCpu"-ErrorAction SilentlyContinue)
if ($NDIS_RssBaseCpu -eq $null)
    {
        $cb_ndisrssbasecpu.text = '0'
    }
    else
    {
        $cb_ndisrssbasecpu.text = $NDIS_RssBaseCpu
    }
    
$TCPIPRssBaseCpuValue = ($cb_tcpiprssbasecpu.text)
$NDISRssBaseCpuValue = ($cb_ndisrssbasecpu.text)
$cb_tcpiprssbasecpu.add_TextChanged({
    Write-Host "TCP/IP - RSSBaseCpu = "$cb_tcpiprssbasecpu.text
    Set-ItemProperty -Path "$TCPIP_RegPath" -Name "RssBaseCpu" -Value $cb_tcpiprssbasecpu.text -Type DWord -Force})
$cb_ndisrssbasecpu.add_TextChanged({
    Write-Host "NDIS - RSSBaseCpu = "$cb_ndisrssbasecpu.text
    Set-ItemProperty -Path "$NDIS_RegPath" -Name "RssBaseCpu" -Value $cb_ndisrssbasecpu.text -Type DWord -Force})

#$cb_tcpiprssbasecpu.TextChanged = (Set-ItemProperty -Path "$TCPIP_RegPath" -Name "RssBaseCpu" -Value $TCPIPRssBaseCpuValue -Type DWord -Force)
#$cb_ndisrssbasecpu.TextChanged = (Set-ItemProperty -Path "$NDIS_RegPath" -Name "RssBaseCpu" -Value $NDISRssBaseCpuValue -Type DWord -Force)

# ========================================================
# Apply Button Global Settings
function applyglobal { 
    #cls
    if ($cb_osrss.text -eq (Get-NetOffloadGlobalSetting | select -expand ReceiveSideScaling))
    {
        #Write-Host " ReceiveSideScaling same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying ReceiveSideScaling to"$cb_osrss.text -ForegroundColor Green
        Set-NetOffloadGlobalSetting -ReceiveSideScaling $cb_osrss.text
        $cb_osrss.text = (Get-NetOffloadGlobalSetting | select -expand ReceiveSideScaling)
    }
    
    if ($cb_osrsc.text -eq (Get-NetOffloadGlobalSetting | select -expand ReceiveSegmentCoalescing))
    {
        #Write-Host " ReceiveSegmentCoalescing same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying ReceiveSegmentCoalescing to"$cb_osrsc.text -ForegroundColor Green
        Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing $cb_osrsc.text
        $cb_osrsc.text = (Get-NetOffloadGlobalSetting | select -expand ReceiveSegmentCoalescing)
    }

    if ($cb_oschimney.text -eq (Get-NetOffloadGlobalSetting | select -expand Chimney))
    {
        #Write-Host " Chimney same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying Chimney to"$cb_oschimney.text -ForegroundColor Green
        Set-NetOffloadGlobalSetting -Chimney $cb_oschimney.text
        $cb_oschimney.text = (Get-NetOffloadGlobalSetting | select -expand Chimney)
    }
    
    if ($cb_ostaskoff.text -eq (Get-NetOffloadGlobalSetting | select -expand TaskOffload))
    {
        #Write-Host " TaskOffload same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying TaskOffload to"$cb_ostaskoff.text -ForegroundColor Green
        Set-NetOffloadGlobalSetting -TaskOffload $cb_ostaskoff.text
        $cb_ostaskoff.text = (Get-NetOffloadGlobalSetting | select -expand TaskOffload)
    }
    
    if ($cb_osntd.text -eq (Get-NetOffloadGlobalSetting | select -expand NetworkDirect))
    {
        #Write-Host " NetworkDirect same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying NetworkDirect to"$cb_osntd.text -ForegroundColor Green
        #Set-NetOffloadGlobalSetting -NetworkDirect $cb_osntd.text
        Apply_NetworkDirect
        $cb_osntd.text = (Get-NetOffloadGlobalSetting | select -expand NetworkDirect)
    }
    
    if ($cb_osntdais.text -eq (Get-NetOffloadGlobalSetting | select -expand NetworkDirectAcrossIPSubnets))
    {
        #Write-Host " NetworkDirectAcrossIPSubnets same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying NetworkDirectAcrossIPSubnets to"$cb_osntdais.text -ForegroundColor Green
        ##Bypass
        ##Set-NetOffloadGlobalSetting -NetworkDirectAcrossIPSubnets $cb_osntdais.text
        Apply_NetworkDirectGlobalFlags
        $cb_osntdais.text = (Get-NetOffloadGlobalSetting | select -expand NetworkDirectAcrossIPSubnets)
    }
    
    if ($cb_ospcf.text -eq (Get-NetOffloadGlobalSetting | select -expand PacketCoalescingFilter))
    {
        #Write-Host " PacketCoalescingFilter same as Current, skipping." -ForegroundColor green
    }
    else
    {
        Write-Host "Applying PacketCoalescingFilter to"$cb_ospcf.text -ForegroundColor Green
        Set-NetOffloadGlobalSetting -PacketCoalescingFilter $cb_ospcf.text
        $cb_ospcf.text = (Get-NetOffloadGlobalSetting | select -expand PacketCoalescingFilter)
    }
}

function Apply_NetworkDirect{
$NetworkDirectAvaible = ((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters").PSObject.Properties.Name -contains "NetworkDirectDisable")
	if ($NetworkDirectAvaible -eq $false -and $cb_osntd.Text -eq 'Disabled' ){
		    #Write-Host "Creating NetworkDirect DWORD with Value $($cb_osntd.Text)."  -ForegroundColor Green
		    New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters" -Name "NetworkDirectDisable" -Typ "Dword" -Value "1"
		}else{
			#Write-Host "Removing NetworkDirect DWORD"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters" -Name "NetworkDirectDisable"
		}
}

function Apply_NetworkDirectGlobalFlags{
$NetworkDirectGlobalFlags = ((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters").PSObject.Properties.Name -contains "NetworkDirectGlobalFlags")
	if ($NetworkDirectGlobalFlags -eq $false -and $cb_osntdais.Text -eq 'Allowed' ){
		    New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters" -Name "NetworkDirectGlobalFlags" -Typ "Dword" -Value "1"
		}else{
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\NDIS\Parameters" -Name "NetworkDirectGlobalFlags"
		}
}



#Adapter Selection
$AdapterName = Get-NetAdapter -physical | where status -eq 'up' | Select -expand InterfaceDescription
#$AdapterName = Get-NetAdapter -IncludeHidden | Select -expand InterfaceDescription
#if($AdapterName )
@($AdapterName) | ForEach-Object {[void] $cb_AdapterNamesCombo.Items.Add($_)}

function a { 
    cls
    Write-Host $cb_AdapterNamesCombo.Text
    $Global:NIC_Desc = $cb_AdapterNamesCombo.Text
    $lbl_ndisver.Text = Get-NetAdapter -InterfaceDescription $AdapterName | Select -expand NdisVersion
    $PhysicalAdapter = Get-WmiObject -Class Win32_NetworkAdapter|Where-Object{$_.Name -like "$NIC_Desc"} 
            $PhysicalAdapterName = $PhysicalAdapter.Name
            $DeviceID = $PhysicalAdapter.DeviceID
                If([Int32]$DeviceID -lt 10)
		        {
			    $AdapterDeviceNumber = "000"+$DeviceID
		        }
		        Else
		        {
			    $AdapterDeviceNumber = "00"+$DeviceID
		        }
		    
		    $Global:EthernetClassGuid = Get-WmiObject Win32_PnPEntity | Where-Object{$_.Name -like "$NIC_Desc" } | Select -expand ClassGuid
            $Global:EthernetPNPDeviceID = Get-WmiObject Win32_PnPEntity | Where-Object{$_.Name -like "$NIC_Desc" } | Select -expand PNPDeviceID
		    $Global:NetConnectionID = Get-WmiObject -Class Win32_NetworkAdapter | Where-Object{$_.Name -like "$NIC_Desc" } | Select -expand NetConnectionID 
    #check whether the registry path exists.
		#SupressTerminationErrors
		$ErrorActionPreference="SilentlyContinue"
		
		$Global:KeyPath = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002bE10318}\$AdapterDeviceNumber"
		If(Test-Path -Path $KeyPath)
		{
			Write-Host "Path found at ($KeyPath)."
			$lbl_Path.Text = $KeyPath
		}
		Else
		{
			Write-Warning "The path ($KeyPath) not found."
		}
		#RSS Enabled or Not
		$Global:rssstatus = (Get-NetAdapterRss).Enabled
		if($rssstatus -eq 'True')
		        {
		        $cb_rss_onoff.Text = "Enable"
		        }
		        else
		        {
		        $cb_rss_onoff.Text = "Disable"
		        }
		if($rssstatus -eq $Null )
		        {
		            Write-Warning "RSS is handled by OS, because of Network Driver!"
		            $btn_rssaddsupport.Enabled = $true
		            #$cb_rssqueues.Enabled = $False
		            #$cb_rssprofile.Enabled = $False
		            #$cb_rssbaseproc.Enabled = $False
		            #$cb_rssmaxproc.Enabled = $False
		            #$cb_rssmaxprocs.Enabled = $False
		        }
		        else
		        {
		            $cb_rssqueues.Enabled = $true
		            $cb_rssprofile.Enabled = $true
		            $cb_rssbaseproc.Enabled = $true
		            $cb_rssmaxproc.Enabled = $true
		            $cb_rssmaxprocs.Enabled = $true
		            $btn_rssaddsupport.Enabled = $false
		        }
		  if($cb_rss_onoff.Text -eq 'Enable')
		    {
		        $Global:Rssstatusset = ($True)
		    }
		    else
		    {
		        $Global:Rssstatusset = ($False)
		    }
		    
		#IPv6 Disabled?
		$IPv6_1 = (Get-ItemPropertyValue -Path "REGISTRY::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" -Name "DisabledComponents")
		$IPv6_2 = (Get-ItemPropertyValue -Path "REGISTRY::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" -Name "EnableICSIPv6")
		
		if ($IPv6_1 -eq "255" -and $IPv6_2 -eq "0"){
		     Write-Warning  "IPv6 is Disabled by Registry."  -ForegroundColor Green
		     $cb_IPv6.Checked = $False
		     $cb_IPv6.Enabled = $False
		     $cb_IPv6.Visible = $False
		     $cb_IPv4.Checked = $True
		     $Global:AddressFamily = "IPv4"
		     }else{
		      Write-Warning  "IPv4/IPv6 is Enabled by Registry. Selecting IPv4 as Default for AddressFamily"  -ForegroundColor Green
		      $cb_IPv4.Checked = $true
		      $cb_IPv6.Checked = $false
		      $Global:AddressFamily = "IPv4"
		     }
		
		#RSS Queues
		#Query Available RSSQueues
		$AdapterQueuesRegTest = (Test-Path -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum")
		if($rssstatus -eq $null -Or $cb_rssqueues.Items.Count -eq '0' -and $AdapterQueuesRegTest -eq $false){
		    #Write-Host "Powershell"
		    $Global:AdapterQueues = Get-NetAdapterRss -InterfaceDescription $NIC_Desc | select -expand NumberOfReceiveQueues
		    $cb_rssqueues.Items.Add($AdapterQueues)
		    $cb_rssqueues.Text = $AdapterQueues
		    }else{
		    #Write-Host "Registry"
		    $AdapterQueues = Get-Item -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" | Select -ExpandProperty Property
            @($AdapterQueues) | ForEach-Object {[void] $cb_rssqueues.Items.Add($_)}
		    $AdapterQueues = Get-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues" -Name "Default" | Select -expand Default
		    $cb_rssqueues.Text = $AdapterQueues
		    }
		    
		    #$RegistryQueues = Get-ItemPropertyValue -Path "$KeyPath\Ndi\Params\*NumRssQueues" -Name "Default" | Select -expand Default
		    #$PowershellQueues = Get-NetAdapterRss -InterfaceDescription $NIC_Desc | select -expand NumberOfReceiveQueues
		    #if($RegistryQueues -eq $PowershellQueues){
		    #    Write-Host "NumberOfReceiveQueues is equal."
		    #}else{
		    #    Write-Warning "NumberOfReceiveQueues is not the same. (Powershell and Registry not equal!) Using Registry Value."
		    #}

        #RSS Profiles
        $OSRSSProfiles = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetAdapterRss.Profile].GetEnumValues()
        @($OSRSSProfiles) | ForEach-Object {[void] $cb_rssprofile.Items.Add($_)}
        $cb_rssprofile.Text = Get-NetAdapterRss | Select -ExpandProperty Profile
        
        #RSS BaseProc
        $cb_rssbaseproc.Text = Get-NetAdapterRss | Select -ExpandProperty "BaseProcessorNumber"
        $cb_rssmaxproc.Text = Get-NetAdapterRss | Select -ExpandProperty "MaxProcessorNumber"
        $cb_rssmaxprocs.Text = Get-NetAdapterRss | Select -ExpandProperty "MaxProcessors"
        
        # Network Adapter Advanced Settings
        #
        #FlowControl
        $FlowControl = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*FlowControl")
        $cb_flowcontrol.SelectedIndex=$FlowControl
        #IPChecksumOffloadIPv4
        $IPChecksumOffloadIPv4 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4")
        $cb_IPChecksumOffloadIPv4.SelectedIndex=$IPChecksumOffloadIPv4
        #TCPChecksumOffloadIPv4
        $TCPChecksumOffloadIPv4 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4")
        $cb_TCPChecksumOffloadIPv4.SelectedIndex=$TCPChecksumOffloadIPv4
        #TCPChecksumOffloadIPv6
        $TCPChecksumOffloadIPv6 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6")
        $cb_TCPChecksumOffloadIPv6.SelectedIndex=$TCPChecksumOffloadIPv6
        #UDPChecksumOffloadIPv4
        $UDPChecksumOffloadIPv4 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4")
        $cb_UDPChecksumOffloadIPv4.SelectedIndex=$UDPChecksumOffloadIPv4
        #UDPChecksumOffloadIPv6
        $UDPChecksumOffloadIPv6 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6")
        $cb_UDPChecksumOffloadIPv6.SelectedIndex=$UDPChecksumOffloadIPv6
        #InterruptModeration
        $InterruptModeration = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*InterruptModeration")
        $cb_InterruptModeration.SelectedIndex=$InterruptModeration
        #InterruptModerationRate
        $InterruptModerationRate = (Get-ItemPropertyValue -Path "$KeyPath" -Name "ITR")
        $cb_InterruptModerationRate.Text=$InterruptModerationRate
        #LsoV2IPv4
        $LsoV2IPv4 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV2IPv4")
        $cb_LsoV2IPv4.SelectedIndex=$LsoV2IPv4
        #LsoV2IPv6
        $LsoV2IPv6 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV2IPv6")
        $cb_LsoV2IPv6.SelectedIndex=$LsoV2IPv6
        #LsoV1IPv4
        $LsoV1IPv4 = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV1IPv4")
        $cb_LsoV1IPv4.SelectedIndex=$LsoV1IPv4
        #PMARPOffload
        $PMARPOffload = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PMARPOffload")
        $cb_PMARPOffload.SelectedIndex=$PMARPOffload
        #PMNSOffload
        $PMNSOffload = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PMNSOffload")
        $cb_PMNSOffload.SelectedIndex=$PMNSOffload
        #PriorityVLANTag
        $PriorityVLANTag = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PriorityVLANTag")
        $cb_PriorityVLANTag.SelectedIndex=$PriorityVLANTag
        #ReceiveBuffers
        $ReceiveBuffers = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*ReceiveBuffers")
        $cb_ReceiveBuffers.Text=$ReceiveBuffers
        #TransmitBuffers
        $TransmitBuffers = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TransmitBuffers")
        $cb_TransmitBuffers.Text=$TransmitBuffers
        #TxIntDelay
        $TxIntDelay = (Get-ItemPropertyValue -Path "$KeyPath" -Name "TxIntDelay")
        $tb_TxIntDelay.Text=$TxIntDelay
        #PacketDirect
        $PacketDirect = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PacketDirect")
        if($PacketDirect -eq $null){
            $cb_PacketDirect.Text="Undefined"
        }else{
            $cb_PacketDirect.SelectedIndex=$PacketDirect}
        #EnableCoalesce ( Default Enabled )
        $EnableCoalesce = (Get-ItemPropertyValue -Path "$KeyPath" -Name "EnableCoalesce")
        if($EnableCoalesce -eq $null){
            $cb_EnableCoalesce.Text="Enabled"
        }else{
            $cb_EnableCoalesce.SelectedIndex=$EnableCoalesce}
        #CoalesceBufferSize ( Default 2048 )
        $CoalesceBufferSize = (Get-ItemPropertyValue -Path "$KeyPath" -Name "CoalesceBufferSize")
        if($CoalesceBufferSize -eq $null){
            $cb_CoalesceBufferSize.Text="2048"
        }else{
            $cb_CoalesceBufferSize.Text=$CoalesceBufferSize}   
        #EnableUdpTxScaling
        $EnableUdpTxScaling = (Get-ItemPropertyValue -Path "$KeyPath" -Name "EnableUdpTxScaling")
        if($EnableUdpTxScaling -eq $null){
            $cb_EnableUdpTxScaling.Text="Enabled"
        }else{
            $cb_EnableUdpTxScaling.SelectedIndex=$EnableUdpTxScaling}

        #EnablePME
        $EnablePME = (Get-ItemPropertyValue -Path "$KeyPath" -Name "EnablePME")
            if ($EnablePME -eq '0' -or $EnablePME -eq '1' ){
                $cb_EnablePME.SelectedIndex=$EnablePME
            }else{
                $cb_EnablePME.Items.Add('Undeclared')
                $cb_EnablePME.Text = 'Undeclared'
            }
        #EnableDynamicPowerGating
        $EnableDynamicPowerGating = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*EnableDynamicPowerGating")
            if ($EnableDynamicPowerGating -eq 0 -or $EnableDynamicPowerGating -eq 1 ){
                $cb_EnableDynamicPowerGating.SelectedIndex=$EnableDynamicPowerGating
            }else{
                $cb_EnableDynamicPowerGating.Items.Add('Undeclared')
                $cb_EnableDynamicPowerGating.Text = 'Undeclared'
            }
        #EnableConnectedPowerGating
        $EnableConnectedPowerGating = (Get-ItemPropertyValue -Path "$KeyPath" -Name "EnableConnectedPowerGating")
            if ($EnableConnectedPowerGating -eq 0 -or $EnableConnectedPowerGating -eq 1 ){
                $cb_EnableConnectedPowerGating.SelectedIndex=$EnableConnectedPowerGating
            }else{
                $cb_EnableConnectedPowerGating.Items.Add('Undeclared')
                $cb_EnableConnectedPowerGating.Text = 'Undeclared'
            }
    
        #AutoPowerSaveModeEnabled
        $AutoPowerSaveModeEnabled = (Get-ItemPropertyValue -Path "$KeyPath" -Name "AutoPowerSaveModeEnabled")
            if ($AutoPowerSaveModeEnabled -eq 0 -or $AutoPowerSaveModeEnabled -eq 1 ){
                $cb_AutoPowerSaveModeEnabled.SelectedIndex=$AutoPowerSaveModeEnabled
            }else{
                $cb_AutoPowerSaveModeEnabled.Items.Add('Undeclared')
                $cb_EnableConnectedPowerGating.Text = 'Enabled'
            }
            
        #NicAutoPowerSaver
        $NicAutoPowerSaver = (Get-ItemPropertyValue -Path "$KeyPath" -Name "*NicAutoPowerSaver")
            if ($NicAutoPowerSaver -eq 0 -or $NicAutoPowerSaver -eq 1 ){
                $cb_NicAutoPowerSaver.SelectedIndex=$NicAutoPowerSaver
            }else{
                $cb_NicAutoPowerSaver.Items.Add('Undeclared')
                $cb_NicAutoPowerSaver.Text = 'Undeclared'
            }
        
        #DisableDelayedPowerUp
        $DisableDelayedPowerUp = (Get-ItemPropertyValue -Path "$KeyPath" -Name "DisableDelayedPowerUp")
            if ($DisableDelayedPowerUp -eq 0 -or $DisableDelayedPowerUp -eq 1 ){
                $cb_DisableDelayedPowerUp.SelectedIndex=$DisableDelayedPowerUp
            }else{
                $cb_DisableDelayedPowerUp.Items.Add('Undeclared')
                $cb_DisableDelayedPowerUp.Text = 'Undeclared'
            }
        
        #ReduceSpeedOnPowerDown
        $ReduceSpeedOnPowerDown = (Get-ItemPropertyValue -Path "$KeyPath" -Name "ReduceSpeedOnPowerDown")
            if ($ReduceSpeedOnPowerDown -eq 0 -or $ReduceSpeedOnPowerDown -eq 1 ){
                $cb_ReduceSpeedOnPowerDown.SelectedIndex=$ReduceSpeedOnPowerDown
            }else{
                $cb_ReduceSpeedOnPowerDown.Items.Add('Undeclared')
                $cb_ReduceSpeedOnPowerDown.Text = 'Undeclared'
            }
            
        #DisablePortScaling
        $DisablePortScaling = (Get-ItemPropertyValue -Path "$KeyPath" -Name "DisablePortScaling")
            if ($DisablePortScaling -eq 0 -or $DisablePortScaling -eq 1 ){
                $cb_DisablePortScaling.SelectedIndex=$DisablePortScaling
            }else{
                $cb_DisablePortScaling.Items.Add('Undeclared')
                $cb_DisablePortScaling.Text = 'Undeclared'
            }    
    
        #ManyCoreScaling
        $ManyCoreScaling = (Get-ItemPropertyValue -Path "$KeyPath" -Name "ManyCoreScaling")
            if ($ManyCoreScaling -eq 0 -or $ManyCoreScaling -eq 1 ){
                $cb_ManyCoreScaling.SelectedIndex=$ManyCoreScaling
            }else{
                $cb_ManyCoreScaling.Items.Add('Undeclared')
                $cb_ManyCoreScaling.Text = 'Undeclared'
            }


#Getting Tweaks Settings

        #AFDDefaultReceiveWindow
        $AFDDefaultReceiveWindow = (Get-ItemPropertyValue -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultReceiveWindow")
        $cb_Afd_defaultRecWin.Text=$AFDDefaultReceiveWindow

        #AFDDefaultSendWindow
        $AFDDefaultSendWindow = (Get-ItemPropertyValue -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultSendWindow")
        $cb_Afd_defaultSendWin.Text=$AFDDefaultSendWindow
        
        #BufferMultiplier
        $AFDBufferMultiplier = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferMultiplier")
        $cb_BufferMultiplier.Text=$AFDBufferMultiplier
        
        #DisableAddressSharing
        $AFDDisableAddressSharing = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableAddressSharing")
        $cb_DisableAddressSharing.Text=$AFDDisableAddressSharing

		#BufferAlignment
        $AFDBufferAlignment = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferAlignment")
        $cb_BufferAlignment.Text=$AFDBufferAlignment
        
    	#DoNotHoldNICBuffers
        $AFDDoNotHoldNICBuffers = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DoNotHoldNICBuffers")
        $cb_DoNotHoldNICBuffers.Text=$AFDDoNotHoldNICBuffers

		#SmallBufferSize
        $AFDSmallBufferSize = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferSize")
        $cb_SmallBufferSize.Text=$AFDSmallBufferSize

		#MediumBufferSize
        $AFDMediumBufferSize = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferSize")
        $cb_MediumBufferSize.Text=$AFDMediumBufferSize
        
		#LargeBufferSize
        $AFDLargeBufferSize = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargeBufferSize")
        $cb_LargeBufferSize.Text=$AFDLargeBufferSize
        
		#HugeBufferSize
        $AFDHugeBufferSize = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "HugeBufferSize")
        $cb_HugeBufferSize.Text=$AFDHugeBufferSize        
        
        #SmallBufferListDepth
        $AFDSmallBufferListDepth = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferListDepth")
        $cb_SmallBufferListDepth.Text=$AFDSmallBufferListDepth
		
		#MediumBufferListDepth
        $AFDMediumBufferListDepth = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferListDepth")
        $cb_MediumBufferListDepth.Text=$AFDMediumBufferListDepth
		
		#LargBufferListDepth
        $AFDLargBufferListDepth = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargBufferListDepth")
        $cb_LargBufferListDepth.Text=$AFDLargBufferListDepth
        
        #DisableChainedReceive
        $AFDDisableChainedReceive = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableChainedReceive")
        $cb_DisableChainedReceive.Text=$AFDDisableChainedReceive
		
		#DisableDirectAcceptEx
        $AFDDisableDirectAcceptEx = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableDirectAcceptEx")
        $cb_DisableDirectAcceptEx.Text=$AFDDisableDirectAcceptEx
		
		#DisableRawSecurity
        $AFDDisableRawSecurity = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableRawSecurity")
        $cb_DisableRawSecurity.Text=$AFDDisableRawSecurity
		
		#DynamicSendBufferDisable
        $AFDDynamicSendBufferDisable = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "DynamicSendBufferDisable")
        $cb_DynamicSendBufferDisable.Text=$AFDDynamicSendBufferDisable
        
        #FastSendDatagramThreshold
        $AFDFastSendDatagramThreshold = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastSendDatagramThreshold")
        $cb_FastSendDatagramThreshold.Text=$AFDFastSendDatagramThreshold
		
		#FastCopyReceiveThreshold
        $AFDFastCopyReceiveThreshold = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastCopyReceiveThreshold")
        $cb_FastCopyReceiveThreshold.Text=$AFDFastCopyReceiveThreshold
        
        #IgnorePushBitOnReceives
        $AFDIgnorePushBitOnReceives = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnorePushBitOnReceives")
        $cb_IgnorePushBitOnReceives.Text=$AFDIgnorePushBitOnReceives
        
        #IgnoreOrderlyRelease
        $AFDIgnoreOrderlyRelease = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnoreOrderlyRelease")
        $cb_IgnoreOrderlyRelease.Text=$AFDIgnoreOrderlyRelease
        
        #TransmitWorker
        $AFDTransmitWorker = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "TransmitWorker")
        $cb_TransmitWorker.Text=$AFDTransmitWorker
        
         #PriorityBoost
        $AFDPriorityBoost = (Get-ItemPropertyValue -Path "REGISTRY::HKLM\System\CurrentControlSet\Services\AFD\Parameters" -Name "PriorityBoost")
        $cb_PriorityBoost.Text=$AFDPriorityBoost
        
#NetIPInterface
        
        #NetIPInterface
        #AdvertiseDefaultRoute
        $AdvertiseDefaultRoute = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.AdvertiseDefaultRoute].GetEnumValues()
        @($AdvertiseDefaultRoute) | ForEach-Object {[void] $cb_AdvertiseDefaultRoute.Items.Add($_)}
        $cb_AdvertiseDefaultRoute.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AdvertiseDefaultRoute
            
        #Advertising
        $Advertising = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.Advertising].GetEnumValues()
        @($Advertising) | ForEach-Object {[void] $cb_Advertising.Items.Add($_)}
        $cb_Advertising.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Advertising
            
        #AutomaticMetric
        $AutomaticMetric = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.AutomaticMetric].GetEnumValues()
        @($AutomaticMetric) | ForEach-Object {[void] $cb_AutomaticMetric.Items.Add($_)}
        $cb_AutomaticMetric.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AutomaticMetric
            
        #ClampMss
        $ClampMss = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.ClampMss].GetEnumValues()
        @($ClampMss) | ForEach-Object {[void] $cb_ClampMss.Items.Add($_)}
        $cb_ClampMss.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ClampMss
            
        #DirectedMacWolPattern
        $DirectedMacWolPattern = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.DirectedMacWolPattern].GetEnumValues()
        @($DirectedMacWolPattern) | ForEach-Object {[void] $cb_DirectedMacWolPattern.Items.Add($_)}
        $cb_DirectedMacWolPattern.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DirectedMacWolPattern
            
        #EcnMarking
        $EcnMarking = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.EcnMarking].GetEnumValues()
        @($EcnMarking) | ForEach-Object {[void] $cb_EcnMarking.Items.Add($_)}
        $cb_EcnMarking.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand EcnMarking
            
        #ForceArpNdWolPattern
        $ForceArpNdWolPattern = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.ForceArpNdWolPattern].GetEnumValues()
        @($ForceArpNdWolPattern) | ForEach-Object {[void] $cb_ForceArpNdWolPattern.Items.Add($_)}
        $cb_ForceArpNdWolPattern.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ForceArpNdWolPattern
            
        #Forwarding
        $Forwarding = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.Forwarding].GetEnumValues()
        @($Forwarding) | ForEach-Object {[void] $cb_Forwarding.Items.Add($_)}
        $cb_Forwarding.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Forwarding
            
        #IgnoreDefaultRoutes
        $IgnoreDefaultRoutes = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.IgnoreDefaultRoutes].GetEnumValues()
        @($IgnoreDefaultRoutes) | ForEach-Object {[void] $cb_IgnoreDefaultRoutes.Items.Add($_)}
        $cb_IgnoreDefaultRoutes.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand IgnoreDefaultRoutes
            
        #ManagedAddressConfiguration
        $ManagedAddressConfiguration = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.ManagedAddressConfiguration].GetEnumValues()
        @($ManagedAddressConfiguration) | ForEach-Object {[void] $cb_ManagedAddressConfiguration.Items.Add($_)}
        $cb_ManagedAddressConfiguration.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ManagedAddressConfiguration
            
        #NeighborDiscoverySupported
        $NeighborDiscoverySupported = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.NeighborDiscoverySupported].GetEnumValues()
        @($NeighborDiscoverySupported) | ForEach-Object {[void] $cb_NeighborDiscoverySupported.Items.Add($_)}
        $cb_NeighborDiscoverySupported.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborDiscoverySupported
        
        #NeighborUnreachabilityDetection
        $NeighborUnreachabilityDetection = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.NeighborUnreachabilityDetection].GetEnumValues()
        @($NeighborUnreachabilityDetection) | ForEach-Object {[void] $cb_NeighborUnreachabilityDetection.Items.Add($_)}
        $cb_NeighborUnreachabilityDetection.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborUnreachabilityDetection
        
        #OtherStatefulConfiguration
        $OtherStatefulConfiguration = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.OtherStatefulConfiguration].GetEnumValues()
        @($OtherStatefulConfiguration) | ForEach-Object {[void] $cb_OtherStatefulConfiguration.Items.Add($_)}
        $cb_OtherStatefulConfiguration.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand OtherStatefulConfiguration
        
        #RouterDiscovery
        $RouterDiscovery = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.RouterDiscovery].GetEnumValues()
        @($RouterDiscovery) | ForEach-Object {[void] $cb_RouterDiscovery.Items.Add($_)}
        $cb_RouterDiscovery.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RouterDiscovery
        
        #Store
        $Store = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.Store].GetEnumValues()
        @($Store) | ForEach-Object {[void] $cb_Store.Items.Add($_)}
        $cb_Store.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Store
        
        #WeakHostReceive
        $WeakHostReceive = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.WeakHostReceive].GetEnumValues()
        @($WeakHostReceive) | ForEach-Object {[void] $cb_WeakHostReceive.Items.Add($_)}
        $cb_WeakHostReceive.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostReceive
        
        #WeakHostSend
        $WeakHostSend = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetIPInterface.WeakHostSend].GetEnumValues()
        @($WeakHostSend) | ForEach-Object {[void] $cb_WeakHostSend.Items.Add($_)}
        $cb_WeakHostSend.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostSend
        
        #CurrentHopLimit
        #When this parameter value is set to 0, it uses this default.    
        $tb_CurrentHopLimit.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand CurrentHopLimit
        
        #BaseReachableTime
        #Specifies the base value for random reachable time, in milliseconds. For more information, see RFC 2461.
        #The default value is 30000.
        $tb_BaseReachableTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand BaseReachableTime
    
        #ReachableTime
        #Specifies an array of reachable time values. This parameter is the time, in milliseconds, that a node assumes that a neighbor
        #is reachable after having received a reachability confirmation. This parameter works with the NeighborUnreachabilityDetection parameter.
        $tb_ReachableTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ReachableTime

        #DadRetransmitTime
        #Specifies a value for the time interval between neighbor solicitation messages.
        $tb_DadRetransmitTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadRetransmitTime

        #DadTransmits
        #Specifies a value for the number of consecutive messages sent while the network driver performs duplicate address detection.
        $tb_DadTransmits.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadTransmits

        #NlMtu
        #Specifies the network layer Maximum Transmission Unit (MTU) value, in bytes, for an IP interface. 
        #For IPv4 the minimum value is 576 bytes. For IPv6 the minimum is value is 1280 bytes.
        #For both IPv4 and IPv6, the maximum value is 2^32-1 (4294967295). You cannot set values outside these ranges.
        #If this parameter is set to 0, then it will remain unchanged and maintain its current value. The IP interface will not transmit packets larger than the maximum value.
        $tb_NlMtu.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NlMtu

        #RetransmitTime (RetransmitTimeMs)
        #Specifies a value for timeout and retransmission, in milliseconds, for Neighbor Solicitation messages. 
        #For more information, see RetransTimer in RFC 2461. 
        #By default, the value is set to 1000.
        $tb_RetransmitTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RetransmitTime
        
        $PathInterrupt = 'HKEY_LOCAL_MACHINE\System\CurrentControlSet\Enum'
        #$Global:EthernetClassGuid
        #$Global:EthernetPNPDeviceID
        $Global:NewPathInterrupt = "$PathInterrupt\$EthernetPNPDeviceID"
        $Test = Test-Path REGISTRY::$NewPathInterrupt
        if ($Test){
	        $cb_MsiMode.Enabled = $True
            $cb_InterruptPriority.Enabled = $True
            $lb_MsiMode.Enabled = $True
            $lb_InterruptPriority.Enabled = $True
            $lb_DevicePolicy.Enabled = $True
            $cb_DevicePolicy.Enabled = $True
            $btn_InterruptApply.Enabled = $True
            $MsiModeRegistry = Get-ItemPropertyValue -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" -Name "MSISupported"
            if($MsiModeRegistry -eq "1"){
                $cb_MsiMode.Text = "Enabled"}
            if($MsiModeRegistry -eq "0"){
                $cb_MsiMode.Text = "Disabled"}
            
            $DevicePriority = Get-ItemPropertyValue -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority"     
			$Global:DevicePriorityAvailable = ((Get-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy").PSObject.Properties.Name -contains "DevicePriority")
            if($DevicePriority -eq "0"){
                $cb_InterruptPriority.Text = "Undefined"}
            if($DevicePriority -eq "1"){
                $cb_InterruptPriority.Text = "Low"}
            if($DevicePriority -eq "2"){
                $cb_InterruptPriority.Text = "Normal"}
            if($DevicePriority -eq "3"){
                $cb_InterruptPriority.Text = "High"}
                
            $DevicePolicy = Get-ItemPropertyValue -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePolicy"         
            #Default Affinity policy, system dependent.
            if($DevicePolicy -eq "0" -or $DevicePolicy -eq $null){
                $cb_DevicePolicy.Text = "MachineDefault"}
            #Target all processors located in same NUMA Node Device.
            if($DevicePolicy -eq "1"){
                $cb_DevicePolicy.Text = "AllCloseProcessors"}
            #Target one processor located in same NUMA Node as Device.
            if($DevicePolicy -eq "2"){
                $cb_DevicePolicy.Text = "OneCloseProcessor"}
            #Target all Processors in machine.
            if($DevicePolicy -eq "3"){
                $cb_DevicePolicy.Text = "ProcessorsInMachine"}
            #Target processors specified in mask, use set Mask.
            if($DevicePolicy -eq "4"){
                $cb_DevicePolicy.Text = "SpecifiedProcessors"}
            #Spread Message-Signaled-Interrupts to different processors, if possible.
            if($DevicePolicy -eq "5"){
                $cb_DevicePolicy.Text = "SreadMessagesAcrossAllProcessors"}
            }
}

#AddRSSSupport
function RSSEnable{
    Write-Host "Enabling RSS Support for "$PhysicalAdapter.Name
    New-Item "$KeyPath\Ndi\Params\*RSS" -Force
        
    New-ItemProperty "$KeyPath\Ndi\Params\*RSS" -Name "ParamDesc" -PropertyTyp "String" -Value "Receive Side Scaling" -Force
    New-ItemProperty "$KeyPath\Ndi\Params\*RSS" -Name "default" -PropertyTyp "String" -Value "1" -Force
    New-ItemProperty "$KeyPath\Ndi\Params\*RSS" -Name "type" -PropertyTyp "String" -Value "enum" -Force
        
    New-Item "$KeyPath\Ndi\Params\*RSS\Enum" -Force
    New-ItemProperty "$KeyPath\Ndi\Params\*RSS\Enum" -Name "0" -PropertyTyp "String" -Value "Disabled" -Force
    New-ItemProperty "$KeyPath\Ndi\Params\*RSS\Enum" -Name "1" -PropertyTyp "String" -Value "Enabled" -Force
}


        function applyadvsettings {
        #cls
        #FlowControl
        if ($cb_flowcontrol.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*FlowControl")){
            Write-Host "FlowControl is same then Registry, skipping."  -ForegroundColor green}

            elseif ($cb_flowcontrol.SelectedIndex -eq '0'){
            Write-Host "Disabling FlowControl"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*FlowControl" -Value "0" -Force}
            
            elseif ($cb_flowcontrol.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for FlowControl" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*FlowControl" -Value "1" -Force}
            
            elseif ($cb_flowcontrol.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for FlowControl" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*FlowControl" -Value "2" -Force}
            
            elseif ($cb_flowcontrol.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for FlowControl" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*FlowControl" -Value "3" -Force}
            
        #IPChecksumOffloadIPv4        
        if ($cb_IPChecksumOffloadIPv4.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4")){
            Write-Host "IPChecksumOffloadIPv4 is same then Registry, skipping."  -ForegroundColor green}

            elseif ($cb_IPChecksumOffloadIPv4.SelectedIndex -eq '0'){
            Write-Host "Disabling IPChecksumOffloadIPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4" -Value "0" -Force}
            
            elseif ($cb_IPChecksumOffloadIPv4.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for IPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4" -Value "1" -Force}
            
            elseif ($cb_IPChecksumOffloadIPv4.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for IPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4" -Value "2" -Force}
            
            elseif ($cb_IPChecksumOffloadIPv4.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for IPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*IPChecksumOffloadIPv4" -Value "3" -Force}
           
        #TCPChecksumOffloadIPv4 
        if ($cb_TCPChecksumOffloadIPv4.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4")){
            Write-Host "TCPChecksumOffloadIPv4 is same then Registry, skipping."  -ForegroundColor green}

            elseif ($cb_TCPChecksumOffloadIPv4.SelectedIndex -eq '0'){
            Write-Host "Disabling TCPChecksumOffloadIPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4" -Value "0" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv4.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for TCPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4" -Value "1" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv4.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for TCPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4" -Value "2" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv4.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for TCPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv4" -Value "3" -Force}
            
        #TCPChecksumOffloadIPv6 
        if ($cb_TCPChecksumOffloadIPv6.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6")){
            Write-Host "TCPChecksumOffloadIPv6 is same then Registry, skipping."  -ForegroundColor green}

            elseif ($cb_TCPChecksumOffloadIPv6.SelectedIndex -eq '0'){
            Write-Host "Disabling TCPChecksumOffloadIPv6"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6" -Value "0" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv6.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for TCPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6" -Value "1" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv6.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for TCPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6" -Value "2" -Force}
            
            elseif ($cb_TCPChecksumOffloadIPv6.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for TCPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TCPChecksumOffloadIPv6" -Value "3" -Force}    
        
        #UDPChecksumOffloadIPv4 
        if ($cb_UDPChecksumOffloadIPv4.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4")){
            Write-Host "UDPChecksumOffloadIPv4 is same then Registry, skipping."  -ForegroundColor Green}

            elseif ($cb_UDPChecksumOffloadIPv4.SelectedIndex -eq '0'){
            Write-Host "Disabling UDPChecksumOffloadIPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4" -Value "0" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv4.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for UDPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4" -Value "1" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv4.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for UDPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4" -Value "2" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv4.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for UDPChecksumOffloadIPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv4" -Value "3" -Force}
        
        #UDPChecksumOffloadIPv6 
        if ($cb_UDPChecksumOffloadIPv6.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6")){
            Write-Host "UDPChecksumOffloadIPv6 is same then Registry, skipping."  -ForegroundColor Green}

            elseif ($cb_UDPChecksumOffloadIPv6.SelectedIndex -eq '0'){
            Write-Host "Disabling UDPChecksumOffloadIPv6"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6" -Value "0" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv6.SelectedIndex -eq '1'){
            Write-Host "Enabling Tx for UDPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6" -Value "1" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv6.SelectedIndex -eq '2'){
            Write-Host "Enabling Rx for UDPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6" -Value "2" -Force}
            
            elseif ($cb_UDPChecksumOffloadIPv6.SelectedIndex -eq '3'){
            Write-Host "Enabling Tx & Rx for UDPChecksumOffloadIPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*UDPChecksumOffloadIPv6" -Value "3" -Force} 
            
        #Large-Send-Offload V2 (IPv4)
        if ($cb_LsoV2IPv4.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV2IPv4")){
            Write-Host "LsoV2IPv4 is same then Registry, skipping."  -ForegroundColor green}    
            
            elseif ($cb_LsoV2IPv4.SelectedIndex -eq '0'){
            Write-Host "Disabling LsoV2IPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV2IPv4" -Value "0" -Force}
            
            elseif ($cb_LsoV2IPv4.SelectedIndex -eq '1'){
            Write-Host "Enabling LsoV2IPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV2IPv4" -Value "1" -Force}
            
        #Large-Send-Offload V2 (IPv6)
        if ($cb_LsoV2IPv6.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV2IPv6")){
            Write-Host "LsoV2IPv6 is same then Registry, skipping."  -ForegroundColor green}    
            
            elseif ($cb_LsoV2IPv6.SelectedIndex -eq '0'){
            Write-Host "Disabling LsoV2IPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV2IPv6" -Value "0" -Force}
            
            elseif ($cb_LsoV2IPv6.SelectedIndex -eq '1'){
            Write-Host "Enabling LsoV2IPv6" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV2IPv6" -Value "1" -Force}
            
        #Large-Send-Offload V1 (IPv4)
        if ($cb_LsoV1IPv4.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*LsoV1IPv4")){
            Write-Host "LsoV1IPv4 is same then Registry, skipping."  -ForegroundColor green}    
            
            elseif ($cb_LsoV1IPv4.SelectedIndex -eq '0'){
            Write-Host "Disabling LsoV1IPv4"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV1IPv4" -Value "0" -Force}
            
            elseif ($cb_LsoV1IPv4.SelectedIndex -eq '1'){
            Write-Host "Enabling LsoV1IPv4" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*LsoV1IPv4" -Value "1" -Force}               
            
        #PMARPOffload
        if ($cb_PMARPOffload.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PMARPOffload")){
            Write-Host "PMARPOffload is same then Registry, skipping."  -ForegroundColor green}    
            
            elseif ($cb_PMARPOffload.SelectedIndex -eq '0'){
            Write-Host "Disabling PMARPOffload"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PMARPOffload" -Value "0" -Force}
            
            elseif ($cb_PMARPOffload.SelectedIndex -eq '1'){
            Write-Host "Enabling PMARPOffload" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PMARPOffload" -Value "1" -Force}
            
        #PMNSOffload
        if ($cb_PMNSOffload.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PMNSOffload")){
            Write-Host "PMNSOffload is same then Registry, skipping."  -ForegroundColor green}    
            
            elseif ($cb_PMNSOffload.SelectedIndex -eq '0'){
            Write-Host "Disabling PMNSOffload"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PMNSOffload" -Value "0" -Force}
            
            elseif ($cb_PMNSOffload.SelectedIndex -eq '1'){
            Write-Host "Enabling PMNSOffload" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PMNSOffload" -Value "1" -Force}
            
        #PriorityVLANTag   
        if ($cb_PriorityVLANTag.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*PriorityVLANTag")){
            Write-Host "PriorityVLANTag is same then Registry, skipping."  -ForegroundColor Green}    
            
            elseif ($cb_PriorityVLANTag.SelectedIndex -eq '0'){
            Write-Host "Disabling PriorityVLANTag"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PriorityVLANTag" -Value "0" -Force}
            
            elseif ($cb_PriorityVLANTag.SelectedIndex -eq '1'){
            Write-Host "Enabling Paketpriorität" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PriorityVLANTag" -Value "1" -Force}
            
            elseif ($cb_PriorityVLANTag.SelectedIndex -eq '2'){
            Write-Host "Enabling VLAN" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PriorityVLANTag" -Value "2" -Force}
            
            elseif ($cb_PriorityVLANTag.SelectedIndex -eq '3'){
            Write-Host "Enabling Paketpriorität and VLAN" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*PriorityVLANTag" -Value "3" -Force}
            
        #ReceiveBuffers   
        if ($cb_ReceiveBuffers.Text -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*ReceiveBuffers"))
            {
            Write-Host "ReceiveBuffers is same then Registry, skipping."  -ForegroundColor Green
            }    
            else
            { 
            Write-Host "Set ReceiveBuffers to $($cb_ReceiveBuffers.Text)"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*ReceiveBuffers" -Value "$($cb_ReceiveBuffers.Text)" -Force
            }
            
        #TransmitBuffers   
        if ($cb_TransmitBuffers.Text -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*TransmitBuffers"))
            {
            Write-Host "TransmitBuffers is same then Registry, skipping."  -ForegroundColor Green
            }    
            else
            { 
            Write-Host "Set TransmitBuffers to $($cb_TransmitBuffers.Text)"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*TransmitBuffers" -Value "$($cb_TransmitBuffers.Text)" -Force
            }   
            
        #InterruptModeration
        if ($cb_InterruptModeration.SelectedIndex -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "*InterruptModeration")){
            Write-Host "InterruptModeration is same then Registry, skipping."  -ForegroundColor Green}

            elseif ($cb_InterruptModeration.SelectedIndex -eq '0'){
            Write-Host "Disabling InterruptModeration"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*InterruptModeration" -Value "0" -Force}
            
            elseif ($cb_InterruptModeration.SelectedIndex -eq '1'){
            Write-Host "Enabling InterruptModeration" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "*InterruptModeration" -Value "1" -Force}    
        
        #InterruptModerationRate
        #$RegITR = (Get-ItemPropertyValue -Path "$KeyPath" -Name "ITR")
        #if ($cb_InterruptModerationRate.Text -eq $RegITR -xor $cb_InterruptModerationRate.SelectedIndex >0){
            #Write-Host "InterruptModerationRate is same then Registry, skipping."  -ForegroundColor green}
            
            if ($cb_InterruptModerationRate.Text -match 'Disabled'){
            #Write-Host "Disabling InterruptModeration"  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "0" -Force}
            
            elseif ($cb_InterruptModerationRate.Text -match 'Minimal'){
            #Write-Host "Setting InterruptModerationRate to 200 - Minimal" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "200" -Force}        
            
            elseif ($cb_InterruptModerationRate.Text -match 'Low'){
            #Write-Host "Setting InterruptModerationRate to 400 - Low" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "400" -Force}   
            
            elseif ($cb_InterruptModerationRate.Text -match 'Medium'){
            #Write-Host "Setting InterruptModerationRate to 950 - Medium" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "950" -Force}  
            
            elseif ($cb_InterruptModerationRate.Text -match 'High'){
            #Write-Host "Setting InterruptModerationRate to 2000 - High" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "2000" -Force}  
            
            elseif ($cb_InterruptModerationRate.Text -match 'Extreme'){
            #Write-Host "Setting InterruptModerationRate to 3600 - Extreme" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "3600" -Force}  
            
            elseif ($cb_InterruptModerationRate.Text -match 'Adaptive'){
            #Write-Host "Setting InterruptModerationRate to 65535 - Adaptive" -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "ITR" -Value "65535" -Force}  
            #For applications where low latency is critical, this setting should be approximately 8000 interrupts per second.
            
            
            
            
            #TxIntDelay
            #I-219V 28TxDelay Default
            if ($tb_TxIntDelay.Text -eq (Get-ItemPropertyValue -Path "$KeyPath" -Name "TxIntDelay"))
            {
            Write-Host "TxIntDelay is same then Registry, skipping."  -ForegroundColor Green
            }    
            else
            { 
            Write-Host "Set TxIntDelay to"$tb_TxIntDelay.Text  -ForegroundColor Green
            Set-ItemProperty -Path "$KeyPath" -Name "TxIntDelay" -Value $tb_TxIntDelay.Text -Force
            }
            #PacketDirect
            #Unsure Default Enabled or Disabled
            #Ref:https://docs.microsoft.com/en-us/windows-hardware/drivers/network/introduction-to-ndis-pdpi
            if ($cb_PacketDirect.Text -match 'Undefined'){
                #Write-Host "PacketDirect to"$cb_PacketDirect.Text  -ForegroundColor Green
                Remove-ItemProperty -Path "$KeyPath" -Name "*PacketDirect" -Force}  
            if ($cb_PacketDirect.Text -match 'Enabled'){
                Write-Host "PacketDirect to"$cb_PacketDirect.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "*PacketDirect" -Value "1" -PropertyType "String" -Force}  
            if ($cb_PacketDirect.Text -match 'Disabled'){
                Write-Host "PacketDirect to"$cb_PacketDirect.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "*PacketDirect" -Value "0" -PropertyType "String" -Force}
                
            #EnableCoalesce
            if ($cb_EnableCoalesce.Text -match 'Undefined'){
                #Write-Host "EnableCoalesce to"$cb_EnableCoalesce.Text  -ForegroundColor Green
                Remove-ItemProperty -Path "$KeyPath" -Name "EnableCoalesce" -Force}  
            if ($cb_EnableCoalesce.Text -match 'Enabled'){
                Write-Host "EnableCoalesce to"$cb_EnableCoalesce.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "EnableCoalesce" -Value "1" -PropertyType "DWORD" -Force}  
            if ($cb_EnableCoalesce.Text -match 'Disabled'){
                Write-Host "EnableCoalesce to"$cb_EnableCoalesce.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "EnableCoalesce" -Value "0" -PropertyType "DWORD" -Force}
            
		    #CoalesceBufferSize
		    $A=((Get-ItemProperty -Path "$KeyPath").PSObject.Properties.Name -contains "CoalesceBufferSize")
		    if ($A -eq $false -and $cb_CoalesceBufferSize.Text -ne $null -and $cb_CoalesceBufferSize.Text -ne '2048' ){
			    Write-Host "Set CoalesceBufferSize to"$cb_CoalesceBufferSize.Text -ForegroundColor Green
	            New-ItemProperty -Path "$KeyPath" -Name "CoalesceBufferSize" -Typ "Dword" -Value $cb_CoalesceBufferSize.Text -Force
		    }elseif($A -eq $true -and $cb_CoalesceBufferSize.Text -eq $null -or $cb_CoalesceBufferSize.Text -eq ''){
			    Write-Warning "Removing CoalesceBufferSize"
			    Remove-ItemProperty -Path "$KeyPath" -Name "CoalesceBufferSize"
		    }else{
			    Write-Host "Set CoalesceBufferSize to"$cb_CoalesceBufferSize.Text -ForegroundColor Green
	            Set-ItemProperty -Path "$KeyPath" -Name "CoalesceBufferSize" -Value $cb_CoalesceBufferSize.Text -Force	
		    }
            
            #EnableUdpTxScaling
            if ($cb_EnableUdpTxScaling.Text -match 'Undefined'){
                #Write-Host "EnableUdpTxScaling to"$cb_EnableUdpTxScaling.Text  -ForegroundColor Green
                Remove-ItemProperty -Path "$KeyPath" -Name "EnableUdpTxScaling" -Force}  
            if ($cb_EnableUdpTxScaling.Text -match 'Enabled'){
                Write-Host "EnableUdpTxScaling to"$cb_EnableUdpTxScaling.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "EnableUdpTxScaling" -Value "1" -PropertyType "DWORD" -Force}  
            if ($cb_EnableUdpTxScaling.Text -match 'Disabled'){
                Write-Host "EnableUdpTxScaling to"$cb_EnableUdpTxScaling.Text  -ForegroundColor Green
                New-ItemProperty -Path "$KeyPath" -Name "EnableUdpTxScaling" -Value "0" -PropertyType "DWORD" -Force}
                
}

        function applypowersavingsettings {
            #cls
            # ========================================================
            # PowerSettings

            #EnablePME
            # Enables/disables wake-up from Advanced Power Management (APM) sleep states.
            if ($cb_EnablePME.SelectedIndex -eq '0'){
            Write-Host "Disabling (APM) sleep states"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "EnablePME" -PropertyType DWORD -Value "0" -Force}
            if ($cb_EnablePME.SelectedIndex -eq '1'){
            Write-Host "Enabling (APM) sleep states" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "EnablePME" -PropertyType DWORD -Value "1" -Force} 
            if ($cb_EnablePME.SelectedIndex -eq '2'){
            Write-Host "Skipping (APM) sleep states"  -ForegroundColor Green}
            
            #EnableDynamicPowerGating
            if ($cb_EnableDynamicPowerGating.SelectedIndex -eq '0'){
            Write-Host "Disabling DynamicPowerGating"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "*EnableDynamicPowerGating" -PropertyType String -Value "0" -Force}
            if ($cb_EnableDynamicPowerGating.SelectedIndex -eq '1'){
            Write-Host "Enabling DynamicPowerGating" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "*EnableDynamicPowerGating" -PropertyType String -Value "1" -Force} 
            if ($cb_EnableDynamicPowerGating.SelectedIndex -eq '2'){
            Write-Host "Skipping DynamicPowerGating" -ForegroundColor Green}

            #EnableConnectedPowerGating
            if ($cb_EnableConnectedPowerGating.SelectedIndex -eq '0'){
            Write-Host "Disabling ConnectedPowerGating"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "EnableConnectedPowerGating" -PropertyType DWORD -Value "0" -Force}
            if ($cb_EnableConnectedPowerGating.SelectedIndex -eq '1'){
            Write-Host "Enabling ConnectedPowerGating" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "EnableConnectedPowerGating" -PropertyType DWORD -Value "1" -Force} 
            if ($cb_EnableConnectedPowerGating.SelectedIndex -eq '2'){
            Write-Host "Skipping ConnectedPowerGating" -ForegroundColor Green}
            
            #AutoPowerSaveModeEnabled
            if ($cb_AutoPowerSaveModeEnabled.SelectedIndex -eq '0'){
            Write-Host "Disabling AutoPowerSaveMode"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "AutoPowerSaveModeEnabled" -PropertyType DWORD -Value "0" -Force}
            if ($cb_AutoPowerSaveModeEnabled.SelectedIndex -eq '1'){
            Write-Host "Enabling AutoPowerSaveMode" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "AutoPowerSaveModeEnabled" -PropertyType DWORD -Value "1" -Force} 
            if ($cb_AutoPowerSaveModeEnabled.SelectedIndex -eq '2'){
            Write-Host "Skipping AutoPowerSaveMode" -ForegroundColor Green}
           
            #NicAutoPowerSaver
            if ($cb_NicAutoPowerSaver.SelectedIndex -eq '0'){
            Write-Host "Disabling NicAutoPowerSaver"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "*NicAutoPowerSaver" -PropertyType String -Value "0" -Force}
            if ($cb_NicAutoPowerSaver.SelectedIndex -eq '1'){
            Write-Host "Enabling NicAutoPowerSaver" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "*NicAutoPowerSaver" -PropertyType String -Value "1" -Force} 
            if ($cb_NicAutoPowerSaver.SelectedIndex -eq '2'){
            Write-Host "Skipping NicAutoPowerSaver" -ForegroundColor Green}
            
            #DisableDelayedPowerUp
            if ($cb_DisableDelayedPowerUp.SelectedIndex -eq '0'){
            Write-Host "Enabling DelayedPowerUp" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "DisableDelayedPowerUp" -PropertyType DWORD -Value "0" -Force} 
            if ($cb_DisableDelayedPowerUp.SelectedIndex -eq '1'){
            Write-Host "Disabling DelayedPowerUp"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "DisableDelayedPowerUp" -PropertyType DWORD -Value "1" -Force}
            if ($cb_DisableDelayedPowerUp.SelectedIndex -eq '2'){
            Write-Host "Skipping DelayedPowerUp" -ForegroundColor Green}
            
            #ReduceSpeedOnPowerDown
            if ($cb_ReduceSpeedOnPowerDown.SelectedIndex -eq '0'){
            Write-Host "Disabling ReduceSpeedOnPowerDown" -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "ReduceSpeedOnPowerDown" -PropertyType DWORD -Value "0" -Force} 
            if ($cb_ReduceSpeedOnPowerDown.SelectedIndex -eq '1'){
            Write-Host "Enabling ReduceSpeedOnPowerDown"  -ForegroundColor Green
            New-ItemProperty -Path "$KeyPath" -Name "ReduceSpeedOnPowerDown" -PropertyType DWORD -Value "1" -Force}
            if ($cb_ReduceSpeedOnPowerDown.SelectedIndex -eq '2'){
            Write-Host "Skipping ReduceSpeedOnPowerDown" -ForegroundColor Green}
            
            
        }

        #Adding more then Default RSSQueues
        function RSSQueuesUnlock{
        $NumRssQueues1 = Test-Path -Path "$KeyPath\Ndi\Params\*NumRssQueues"
        $NumRssQueues2 = Test-Path -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum"
        $AdapterQueuesOriginal = Get-ItemProperty "$KeyPath\Ndi\Params\*NumRssQueues" -Name "default" | select -expand default
        
        
        #If($NumRssQueues1 -eq $False){
            New-Item -Path "$KeyPath\Ndi\Params\*NumRssQueues" -Force 
            New-ItemProperty "$KeyPath\Ndi\Params\*NumRssQueues" -Name "ParamDesc" -PropertyTyp "String" -Value "Maximum Number of RSS Queues" -Force
            New-ItemProperty "$KeyPath\Ndi\Params\*NumRssQueues" -Name "default" -PropertyTyp "String" -Value $AdapterQueuesOriginal -Force
            New-ItemProperty "$KeyPath\Ndi\Params\*NumRssQueues" -Name "type" -PropertyTyp "String" -Value "enum" -Force
        #}
        #If($NumRssQueues2 -eq $False){
            New-Item -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "1" -PropertyType STRING -Value "1 Queue" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "2" -PropertyType STRING -Value "2 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "3" -PropertyType STRING -Value "3 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "4" -PropertyType STRING -Value "4 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "5" -PropertyType STRING -Value "5 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "6" -PropertyType STRING -Value "6 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "7" -PropertyType STRING -Value "7 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "8" -PropertyType STRING -Value "8 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "9" -PropertyType STRING -Value "9 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "10" -PropertyType STRING -Value "10 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "11" -PropertyType STRING -Value "11 Queues" -Force
            New-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" -Name "12" -PropertyType STRING -Value "12 Queues" -Force
        #}   
        $cb_rssqueues.Items.Clear()
        $AdapterQueuesDefault = Get-ItemProperty "$KeyPath\Ndi\Params\*NumRssQueues" -Name "default" | select -expand default
        #Query Avaible RSSQueues
        $AdapterQueues = Get-Item -Path "$KeyPath\Ndi\Params\*NumRssQueues\Enum" | Select -ExpandProperty Property
        @($AdapterQueues) | ForEach-Object {[void] $cb_rssqueues.Items.Add($_)}
        $cb_rssqueues.Text = $AdapterQueuesDefault
        #$btn_unqueues.Enabled = $False
        }


$cb_AdapterNamesCombo.Add_SelectedValueChanged({ a; NetIPInterface })

function adapter_restart {
    Try {
    Restart-NetAdapter -InterfaceDescription $NIC_Desc
    Write-Host "Restarting Adapter $NIC_Desc now!" -ForegroundColor Red
    }
    catch{
        Write-Host "No Adapter selected." -ForegroundColor Red
        }
}

# Open RegPath Adapter per Button

function btn_regopadap {
        Try {
        $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit"
        $name = "LastKey"
        $value = "Computer\"+(Convert-Path ($Global:KeyPath))
                New-ItemProperty -Path $regPath -Name $name -Value $value -PropertyType String -Force | Out-Null
                Start-Process RegEdit
            } 
            catch 
            {
                Write-Host "No Adapter selected." -ForegroundColor Red
            }
}

function Interrupt{
    $Path = 'HKEY_LOCAL_MACHINE\System\CurrentControlSet\Enum'
    #$Global:EthernetClassGuid
    #$Global:EthernetPNPDeviceID
    $NewPath = "$Path\$EthernetPNPDeviceID"
    $Test = Test-Path REGISTRY::$NewPath
    if ($Test){
	
	}else{
	Write-Warning "GPU not found. Abort"
	}
}

function bypassrssqueues{
    $error.clear()
    try { Set-NetAdapterRss -InterfaceDescription $($Global:NIC_Desc) -BaseProcessorNumber $($cb_rssbaseproc.Text) -MaxProcessorNumber $($cb_rssmaxproc.Text) -Profile $($cb_rssprofile.Text) -MaxProcessors $($cb_rssmaxprocs.Text) -Enabled $($Global:Rssstatusset) -ErrorAction Stop
         } catch { Write-Warning "Error occured while Setting Set-NetAdapterRss, Abort." }
    if (!$error) { Write-Host 'No Error Occured, while Setting Set-NetAdapterRss without NumberOfReceiveQueues. Continue.'
                    Write-Host "Using Registry now, to set RSS Queues to $($cb_rssqueues.Text)"
                    Set-ItemProperty -Path "$KeyPath\Ndi\Params\*NumRssQueues" -Name "default" -Value $cb_rssqueues.Text -Force -ErrorAction "Stop"
                    Write-Host "Done."
        }
}
function applyrsssettings {
        
        #DisablePortScaling
        if ($cb_DisablePortScaling.SelectedIndex -eq '0'){
        Write-Host "Disabling DisablePortScaling" -ForegroundColor Green
        New-ItemProperty -Path "$KeyPath" -Name "DisablePortScaling" -PropertyType DWORD -Value "0" -Force} 
        if ($cb_DisablePortScaling.SelectedIndex -eq '1'){
        Write-Host "Enabling DisablePortScaling"  -ForegroundColor Green
        New-ItemProperty -Path "$KeyPath" -Name "DisablePortScaling" -PropertyType DWORD -Value "1" -Force}
        if ($cb_DisablePortScaling.SelectedIndex -eq '2'){
        Write-Host "Skipping DisablePortScaling"  -ForegroundColor Green}
        
        #ManyCoreScaling
        if ($cb_ManyCoreScaling.SelectedIndex -eq '0'){
        Write-Host "Disabling ManyCoreScaling" -ForegroundColor Green
        New-ItemProperty -Path "$KeyPath" -Name "ManyCoreScaling" -PropertyType DWORD -Value "0" -Force} 
        if ($cb_ManyCoreScaling.SelectedIndex -eq '1'){
        Write-Host "Enabling ManyCoreScaling"  -ForegroundColor Green
        New-ItemProperty -Path "$KeyPath" -Name "ManyCoreScaling" -PropertyType DWORD -Value "1" -Force}
        if ($cb_ManyCoreScaling.SelectedIndex -eq '2'){
        Write-Host "Skipping ManyCoreScaling"  -ForegroundColor Green}
        
        #Write-Host "Testing Setting RSS"
        $error.clear()
        try { 
            #Write-Host "Using Powersh. now to set rss"
            Set-NetAdapterRss -InterfaceDescription $($Global:NIC_Desc) -BaseProcessorNumber $($cb_rssbaseproc.Text) -MaxProcessorNumber $($cb_rssmaxproc.Text) -NumberOfReceiveQueues $($cb_rssqueues.Text) -Profile $($cb_rssprofile.Text) -MaxProcessors $($cb_rssmaxprocs.Text) -Enabled $($Global:Rssstatusset) -ErrorAction Stop
            Write-Host "Using Powershell to Set-NetAdapterRss."
            }
            catch { Write-Warning "Error occured while Setting Set-NetAdapterRss, testing now without NumberOfReceiveQueues!"
            bypassrssqueues
            }
        #if (!$error) { Write-Host "No Error Occured, while Setting Set-NetAdapterRss with NumberOfReceiveQueues." }
        
        #Set-NetAdapterRss -InterfaceDescription $($Global:NIC_Desc) -BaseProcessorNumber $($cb_rssbaseproc.Text) -MaxProcessorNumber $($cb_rssmaxproc.Text) -NumberOfReceiveQueues $($cb_rssqueues.Text) -Profile $($cb_rssprofile.Text) -MaxProcessors $($cb_rssmaxprocs.Text) -Enabled $($Global:Rssstatusset)
}

function ApplyInterfaceSettings{
    if ($cb_AdvertiseDefaultRoute.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AdvertiseDefaultRoute)){
        Write-Host "AdvertiseDefaultRoute is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "AdvertiseDefaultRoute:"$cb_AdvertiseDefaultRoute.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -AdvertiseDefaultRoute $cb_AdvertiseDefaultRoute.Text
        }
    if ($cb_Advertising.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Advertising)){
        Write-Host "Advertising is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "Advertising:"$cb_Advertising.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -Advertising $cb_Advertising.Text
        }
    if ($cb_AutomaticMetric.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AutomaticMetric)){
        Write-Host "AutomaticMetric is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "AutomaticMetric:"$cb_AutomaticMetric.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -AutomaticMetric $cb_AutomaticMetric.Text
        }
    if ($cb_ClampMss.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ClampMss)){
        Write-Host "ClampMss is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "ClampMss:"$cb_ClampMss.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -ClampMss $cb_ClampMss.Text
        }
    if ($DirectedMacWolPattern.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DirectedMacWolPattern)){
        Write-Host "DirectedMacWolPattern is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "DirectedMacWolPattern:"$DirectedMacWolPattern.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -DirectedMacWolPattern $DirectedMacWolPattern.Text
        }
    if ($cb_EcnMarking.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand EcnMarking)){
        Write-Host "EcnMarking is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "EcnMarking:"$cb_EcnMarking.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -EcnMarking $cb_EcnMarking.Text
        }
    if ($cb_ForceArpNdWolPattern.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ForceArpNdWolPattern)){
        Write-Host "ForceArpNdWolPattern is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "ForceArpNdWolPattern:"$cb_ForceArpNdWolPattern.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -ForceArpNdWolPattern $cb_ForceArpNdWolPattern.Text
        }
    if ($cb_Forwarding.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Forwarding)){
        Write-Host "Forwarding is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "Forwarding:"$cb_Forwarding.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -Forwarding $cb_Forwarding.Text
        }
    if ($cb_IgnoreDefaultRoutes.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand IgnoreDefaultRoutes)){
        Write-Host "IgnoreDefaultRoutes is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "IgnoreDefaultRoutes:"$cb_IgnoreDefaultRoutes.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -IgnoreDefaultRoutes $cb_IgnoreDefaultRoutes.Text
        }
    if ($cb_ManagedAddressConfiguration.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ManagedAddressConfiguration)){
        Write-Host "ManagedAddressConfiguration is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "ManagedAddressConfiguration:"$cb_ManagedAddressConfiguration.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -ManagedAddressConfiguration $cb_ManagedAddressConfiguration.Text
        }
    if ($cb_NeighborDiscoverySupported.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborDiscoverySupported)){
        Write-Host "NeighborDiscoverySupported is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "NeighborDiscoverySupported:"$cb_NeighborDiscoverySupported.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -NeighborDiscoverySupported $cb_NeighborDiscoverySupported.Text
        }
    if ($cb_NeighborUnreachabilityDetection.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborUnreachabilityDetection)){
        Write-Host "NeighborUnreachabilityDetection is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "NeighborUnreachabilityDetection:"$cb_NeighborUnreachabilityDetection.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -NeighborUnreachabilityDetection $cb_NeighborUnreachabilityDetection.Text
        }
    if ($cb_OtherStatefulConfiguration.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand OtherStatefulConfiguration)){
        Write-Host "OtherStatefulConfiguration is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "OtherStatefulConfiguration:"$cb_OtherStatefulConfiguration.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -OtherStatefulConfiguration $cb_OtherStatefulConfiguration.Text
        }
    if ($cb_RouterDiscovery.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RouterDiscovery)){
        Write-Host "RouterDiscovery is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "RouterDiscovery:"$cb_RouterDiscovery.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -RouterDiscovery $cb_RouterDiscovery.Text
        }
    if ($cb_Store.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Store)){
        Write-Host "Store is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "Store:"$cb_Store.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -Store $cb_Store.Text
        }
    if ($cb_WeakHostReceive.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostReceive)){
        Write-Host "WeakHostReceive is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "WeakHostReceive:"$cb_WeakHostReceive.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -Store $cb_WeakHostReceive.Text
        }
    if ($cb_WeakHostSend.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostSend)){
        Write-Host "WeakHostSend is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "WeakHostSend:"$cb_WeakHostSend.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -Store $cb_WeakHostSend.Text
        }
    if ($tb_CurrentHopLimit.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand CurrentHopLimit)){
        Write-Host "CurrentHopLimit is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "CurrentHopLimit:"$tb_CurrentHopLimit.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -CurrentHopLimit $tb_CurrentHopLimit.Text
        }
    if ($tb_BaseReachableTime.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand BaseReachableTime)){
        Write-Host "BaseReachableTime is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "BaseReachableTime:"$tb_BaseReachableTime.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -BaseReachableTime $tb_BaseReachableTime.Text
        }
    if ($tb_ReachableTime.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ReachableTime)){
        Write-Host "ReachableTime is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "ReachableTime:"$tb_ReachableTime.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -ReachableTime $tb_ReachableTime.Text
        }
    if ($tb_DadRetransmitTime.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadRetransmitTime)){
        Write-Host "DadRetransmitTime is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "DadRetransmitTime:"$tb_DadRetransmitTime.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -DadRetransmitTime $tb_DadRetransmitTime.Text
        }
    if ($tb_DadTransmits.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadTransmits)){
        Write-Host "DadTransmits is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "DadTransmits:"$tb_DadTransmits.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -DadTransmits $tb_DadTransmits.Text
        }
    if ($tb_NlMtu.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NlMtu)){
        Write-Host "NlMtu is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "NlMtu:"$tb_NlMtu.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -NlMtu $tb_NlMtu.Text
        }
    if ($tb_RetransmitTime.Text -eq (Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RetransmitTime)){
        Write-Host "RetransmitTime is same, skipping."  -ForegroundColor green}
        else{
            Write-Host "RetransmitTime:"$tb_RetransmitTime.Text  -ForegroundColor Green
            Set-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily -RetransmitTime $tb_RetransmitTime.Text
        }
}

function IPv4_CheckedChanged(){
if ($cb_IPv4.Checked){
    $cb_IPv6.Enabled = $false
    $Global:AddressFamily = "IPv4"
    RefreshingNetIPInterfaceSettings
    }else{
    $cb_IPv6.Enabled = $true
    $Global:AddressFamily = "IPv6"
    RefreshingNetIPInterfaceSettings
    }
if ($cb_IPv4.Checked -eq $false -and $cb_IPv6.Checked -eq $false -or $cb_IPv4.Checked -eq $false -and $cb_IPv6.Visible -eq $false ){
        Write-Warning "IPv4 and IPv6 are not Checked, Disabling Interface-Settings."
        $Groupbox7.Enabled = $false
        $btn_applyInterfaceSettings.Enabled = $false
    }else{
        $Groupbox7.Enabled = $true
        $btn_applyInterfaceSettings.Enabled = $true
    }
}

function IPv6_CheckedChanged(){
if ($cb_IPv6.Checked){
    $cb_IPv4.Enabled = $false
    $Global:AddressFamily = "IPv6"
    RefreshingNetIPInterfaceSettings
    }else{
    $cb_IPv4.Enabled = $true
    $Global:AddressFamily = "IPv4"}
    RefreshingNetIPInterfaceSettings
    }
    
if ($cb_IPv4.Checked -eq $false -and $cb_IPv6.Checked -eq $false -or $cb_IPv4.Checked -eq $false -and $cb_IPv6.Visible -eq $false  ){
        Write-Warning "IPv4 and IPv6 are not Checked, Disabling Interface-Settings."
        $Groupbox7.Enabled = $false
        $btn_applyInterfaceSettings.Enabled = $false
    }else{
        $Groupbox7.Enabled = $true
        $btn_applyInterfaceSettings.Enabled = $true
    }

function RefreshingNetIPInterfaceSettings{
    #NetIPInterface
        #AdvertiseDefaultRoute
        $cb_AdvertiseDefaultRoute.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AdvertiseDefaultRoute
            
        #Advertising
        $cb_Advertising.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Advertising
            
        #AutomaticMetric
        $cb_AutomaticMetric.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand AutomaticMetric
            
        #ClampMss
        $cb_ClampMss.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ClampMss
            
        #DirectedMacWolPattern
        $cb_DirectedMacWolPattern.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DirectedMacWolPattern
            
        #EcnMarking
        $cb_EcnMarking.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand EcnMarking
            
        #ForceArpNdWolPattern
        $cb_ForceArpNdWolPattern.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ForceArpNdWolPattern
            
        #Forwarding
        $cb_Forwarding.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Forwarding
            
        #IgnoreDefaultRoutes
        $cb_IgnoreDefaultRoutes.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand IgnoreDefaultRoutes
            
        #ManagedAddressConfiguration
        $cb_ManagedAddressConfiguration.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ManagedAddressConfiguration
            
        #NeighborDiscoverySupported
        $cb_NeighborDiscoverySupported.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborDiscoverySupported
        
        #NeighborUnreachabilityDetection
        $cb_NeighborUnreachabilityDetection.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NeighborUnreachabilityDetection
        
        #OtherStatefulConfiguration
        $cb_OtherStatefulConfiguration.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand OtherStatefulConfiguration
        
        #RouterDiscovery
        $cb_RouterDiscovery.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RouterDiscovery
        
        #Store
        $cb_Store.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand Store
        
        #WeakHostReceive
        $cb_WeakHostReceive.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostReceive
        
        #WeakHostSend
        $cb_WeakHostSend.Text =  Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand WeakHostSend
        
        #CurrentHopLimit
        #When this parameter value is set to 0, it uses this default.    
        $tb_CurrentHopLimit.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand CurrentHopLimit
        
        #BaseReachableTime
        #Specifies the base value for random reachable time, in milliseconds. For more information, see RFC 2461.
        #The default value is 30000.
        $tb_BaseReachableTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand BaseReachableTime
    
        #ReachableTime
        #Specifies an array of reachable time values. This parameter is the time, in milliseconds, that a node assumes that a neighbor
        #is reachable after having received a reachability confirmation. This parameter works with the NeighborUnreachabilityDetection parameter.
        $tb_ReachableTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand ReachableTime

        #DadRetransmitTime
        #Specifies a value for the time interval between neighbor solicitation messages.
        $tb_DadRetransmitTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadRetransmitTime

        #DadTransmits
        #Specifies a value for the number of consecutive messages sent while the network driver performs duplicate address detection.
        $tb_DadTransmits.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand DadTransmits

        #NlMtu
        #Specifies the network layer Maximum Transmission Unit (MTU) value, in bytes, for an IP interface. 
        #For IPv4 the minimum value is 576 bytes. For IPv6 the minimum is value is 1280 bytes.
        #For both IPv4 and IPv6, the maximum value is 2^32-1 (4294967295). You cannot set values outside these ranges.
        #If this parameter is set to 0, then it will remain unchanged and maintain its current value. The IP interface will not transmit packets larger than the maximum value.
        $tb_NlMtu.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand NlMtu

        #RetransmitTime (RetransmitTimeMs)
        #Specifies a value for timeout and retransmission, in milliseconds, for Neighbor Solicitation messages. 
        #For more information, see RetransTimer in RFC 2461. 
        #By default, the value is set to 1000.
        $tb_RetransmitTime.Text = Get-NetIPInterface -InterfaceAlias $NetConnectionID -AddressFamily $Global:AddressFamily | Select-Object -Expand RetransmitTime
    
}

function applyall{
    if($Groupbox7.Enabled -eq $False){
    Write-Host "Applying All Settings!" -ForegroundColor Yellow
    Write-Host "`n"
    Write-Host "===== Global Settings =====" -ForegroundColor Yellow
    applyglobal
    Write-Host "`n"
    Write-Host "===== RSS Settings =====" -ForegroundColor Yellow
    applyrsssettings
    Write-Host "`n"
    Write-Host "===== PowerSaving Settings =====" -ForegroundColor Yellow
    applypowersavingsettings
    Write-Host "`n"
    Write-Warning "===== IP-Interface Settings ====="
    Write-Warning "IPv4 or IPv6 not specified!"
    Write-Host "`n"
    Write-Host "===== Adapter Advanced Settings =====" -ForegroundColor Yellow
    applyadvsettings    
        
    }else{
    Write-Host "Applying All Settings!" -ForegroundColor Yellow
    
    Write-Host "===== Global Settings =====" -ForegroundColor Yellow
    applyglobal
    
    Write-Host "===== RSS Settings =====" -ForegroundColor Yellow
    applyrsssettings
    
    Write-Host "===== PowerSaving Settings =====" -ForegroundColor Yellow
    applypowersavingsettings
    
    Write-Host "===== IP-Interface Settings =====" -ForegroundColor Yellow
    ApplyInterfaceSettings
    
    Write-Host "===== Adapter Advanced Settings =====" -ForegroundColor Yellow
    applyadvsettings
    }
}

function RegistryTweaks{
        #$ErrorActionPreference = 'Continue'
		#DefaultReceiveWindow
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DefaultReceiveWindow")
		if ($A -eq $false -and $cb_Afd_defaultrecWin.Text -ne $null -and $cb_Afd_defaultrecWin.Text -ne '' ){
			Write-Host "Set AFDDefaultReceiveWindow to"$cb_Afd_defaultrecWin.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultReceiveWindow" -Typ "Dword" -Value $cb_Afd_defaultrecWin.Text -Force
		}elseif($A -eq $true -and $cb_Afd_defaultrecWin.Text -eq $null -or $cb_Afd_defaultrecWin.Text -eq ''){
			Write-Warning "Removing AFDDefaultReceiveWindow"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultReceiveWindow"
		}else{
			Write-Host "Set AFDDefaultReceiveWindow to"$cb_Afd_defaultrecWin.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultReceiveWindow" -Value $cb_Afd_defaultrecWin.Text -Force	
		}
        
        #DefaultSendWindow
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DefaultSendWindow")
		if ($A -eq $false -and $cb_Afd_defaultSendWin.Text -ne $null -and $cb_Afd_defaultSendWin.Text -ne '' ){
			Write-Host "Set AFDDefaultSendWindow to"$cb_Afd_defaultSendWin.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultSendWindow" -Typ "Dword" -Value $cb_Afd_defaultSendWin.Text -Force
		}elseif($A -eq $true -and $cb_Afd_defaultSendWin.Text -eq $null -or $cb_Afd_defaultSendWin.Text -eq ''){
			Write-Warning "Removing AFDDefaultSendWindow"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultSendWindow"
		}else{
			Write-Host "Set AFDDefaultSendWindow to"$cb_Afd_defaultSendWin.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DefaultSendWindow" -Value $cb_Afd_defaultSendWin.Text -Force	
		}
        
        #DisableAddressSharing
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DisableAddressSharing")
		if ($A -eq $false -and $cb_DisableAddressSharing.Text -ne $null -and $cb_DisableAddressSharing.Text -ne '' ){
			Write-Host "Set AFDDisableAddressSharing to"$cb_DisableAddressSharing.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableAddressSharing" -Typ "Dword" -Value $cb_DisableAddressSharing.Text -Force
		}elseif($A -eq $true -and $cb_DisableAddressSharing.Text -eq $null -or $cb_DisableAddressSharing.Text -eq ''){
			Write-Warning "Removing AFDDisableAddressSharing"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableAddressSharing"
		}else{
			Write-Host "Set AFDDisableAddressSharing to"$cb_DisableAddressSharing.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableAddressSharing" -Value $cb_DisableAddressSharing.Text -Force	
		}
		
		#BufferMultiplier
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "BufferMultiplier")
		if ($A -eq $false -and $cb_BufferMultiplier.Text -ne $null -and $cb_BufferMultiplier.Text -ne '' ){
			Write-Host "Set AFDBufferMultiplier to"$cb_BufferMultiplier.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferMultiplier" -Typ "Dword" -Value $cb_BufferMultiplier.Text -Force
		}elseif($A -eq $true -and $cb_BufferMultiplier.Text -eq $null -or $cb_BufferMultiplier.Text -eq ''){
			Write-Warning "Removing AFDBufferMultiplier"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferMultiplier"
		}else{
			Write-Host "Set AFDBufferMultiplier to"$cb_BufferMultiplier.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferMultiplier" -Value $cb_BufferMultiplier.Text -Force	
		}
		
        #BufferAlignment
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "BufferAlignment")
		if ($A -eq $false -and $cb_BufferAlignment.Text -ne $null -and $cb_BufferAlignment.Text -ne '' ){
			Write-Host "Set AFDBufferAlignment to"$cb_BufferAlignment.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferAlignment" -Typ "Dword" -Value $cb_BufferAlignment.Text -Force
		}elseif($A -eq $true -and $cb_BufferAlignment.Text -eq $null -or $cb_BufferAlignment.Text -eq ''){
			Write-Warning "Removing AFDBufferAlignment"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferAlignment"
		}else{
			Write-Host "Set AFDBufferAlignment to"$cb_BufferAlignment.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "BufferAlignment" -Value $cb_BufferAlignment.Text -Force	
		}
		
        #DoNotHoldNICBuffers
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DoNotHoldNICBuffers")
		if ($A -eq $false -and $cb_DoNotHoldNICBuffers.Text -ne $null -and $cb_DoNotHoldNICBuffers.Text -ne '' ){
			Write-Host "Set AFDDoNotHoldNICBuffers to"$cb_DoNotHoldNICBuffers.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DoNotHoldNICBuffers" -Typ "Dword" -Value $cb_DoNotHoldNICBuffers.Text -Force
		}elseif($A -eq $true -and $cb_DoNotHoldNICBuffers.Text -eq $null -or $cb_DoNotHoldNICBuffers.Text -eq ''){
			Write-Warning "Removing AFDDoNotHoldNICBuffers"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DoNotHoldNICBuffers"
		}else{
			Write-Host "Set AFDDoNotHoldNICBuffers to"$cb_DoNotHoldNICBuffers.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DoNotHoldNICBuffers" -Value $cb_DoNotHoldNICBuffers.Text -Force	
		}
		
		#SmallBufferSize
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "SmallBufferSize")
		if ($A -eq $false -and $cb_SmallBufferSize.Text -ne $null -and $cb_SmallBufferSize.Text -ne '' ){
			Write-Host "Set AFDSmallBufferSize to"$cb_SmallBufferSize.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferSize" -Typ "Dword" -Value $cb_SmallBufferSize.Text -Force
		}elseif($A -eq $true -and $cb_SmallBufferSize.Text -eq $null -or $cb_SmallBufferSize.Text -eq ''){
			Write-Warning "Removing AFDSmallBufferSize"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferSize"
		}else{
			Write-Host "Set AFDSmallBufferSize to"$cb_SmallBufferSize.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferSize" -Value $cb_SmallBufferSize.Text -Force	
		}
		
		#MediumBufferSize
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "MediumBufferSize")
		if ($A -eq $false -and $cb_MediumBufferSize.Text -ne $null -and $cb_MediumBufferSize.Text -ne '' ){
			Write-Host "Set AFDMediumBufferSize to"$cb_MediumBufferSize.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferSize" -Typ "Dword" -Value $cb_MediumBufferSize.Text -Force
		}elseif($A -eq $true -and $cb_MediumBufferSize.Text -eq $null -or $cb_MediumBufferSize.Text -eq ''){
			Write-Warning "Removing AFDMediumBufferSize"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferSize"
		}else{
			Write-Host "Set AFDMediumBufferSize to"$cb_MediumBufferSize.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferSize" -Value $cb_MediumBufferSize.Text -Force	
		}
		
		#LargeBufferSize
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "LargeBufferSize")
		if ($A -eq $false -and $cb_LargeBufferSize.Text -ne $null -and $cb_LargeBufferSize.Text -ne '' ){
			Write-Host "Set AFDLargeBufferSize to"$cb_LargeBufferSize.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargeBufferSize" -Typ "Dword" -Value $cb_LargeBufferSize.Text -Force
		}elseif($A -eq $true -and $cb_LargeBufferSize.Text -eq $null -or $cb_LargeBufferSize.Text -eq ''){
			Write-Warning "Removing AFDLargeBufferSize"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargeBufferSize"
		}else{
			Write-Host "Set AFDLargeBufferSize to"$cb_LargeBufferSize.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargeBufferSize" -Value $cb_LargeBufferSize.Text -Force	
		}
		
		#HugeBufferSize
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "HugeBufferSize")
		if ($A -eq $false -and $cb_HugeBufferSize.Text -ne $null -and $cb_HugeBufferSize.Text -ne '' ){
			Write-Host "Set AFDHugeBufferSize to"$cb_HugeBufferSize.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "HugeBufferSize" -Typ "Dword" -Value $cb_HugeBufferSize.Text -Force
		}elseif($A -eq $true -and $cb_HugeBufferSize.Text -eq $null -or $cb_HugeBufferSize.Text -eq ''){
			Write-Warning "Removing AFDHugeBufferSize"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "HugeBufferSize"
		}else{
			Write-Host "Set AFDHugeBufferSize to"$cb_HugeBufferSize.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "HugeBufferSize" -Value $cb_HugeBufferSize.Text -Force	
		}
		
		#SmallBufferListDepth
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "SmallBufferListDepth")
		if ($A -eq $false -and $cb_SmallBufferListDepth.Text -ne $null -and $cb_SmallBufferListDepth.Text -ne '' ){
			Write-Host "Set AFDSmallBufferListDepth to"$cb_SmallBufferListDepth.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferListDepth" -Typ "Dword" -Value $cb_SmallBufferListDepth.Text -Force
		}elseif($A -eq $true -and $cb_SmallBufferListDepth.Text -eq $null -or $cb_SmallBufferListDepth.Text -eq ''){
			Write-Warning "Removing AFDSmallBufferListDepth"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferListDepth"
		}else{
			Write-Host "Set AFDSmallBufferListDepth to"$cb_SmallBufferListDepth.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "SmallBufferListDepth" -Value $cb_SmallBufferListDepth.Text -Force	
		}
		
		#MediumBufferListDepth
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "MediumBufferListDepth")
		if ($A -eq $false -and $cb_MediumBufferListDepth.Text -ne $null -and $cb_MediumBufferListDepth.Text -ne '' ){
			Write-Host "Set AFDMediumBufferListDepth to"$cb_MediumBufferListDepth.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferListDepth" -Typ "Dword" -Value $cb_MediumBufferListDepth.Text -Force
		}elseif($A -eq $true -and $cb_MediumBufferListDepth.Text -eq $null -or $cb_MediumBufferListDepth.Text -eq ''){
			Write-Warning "Removing AFDMediumBufferListDepth"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferListDepth"
		}else{
			Write-Host "Set AFDMediumBufferListDepth to"$cb_MediumBufferListDepth.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "MediumBufferListDepth" -Value $cb_MediumBufferListDepth.Text -Force	
		}
		
		#LargBufferListDepth
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "LargBufferListDepth")
		if ($A -eq $false -and $cb_LargBufferListDepth.Text -ne $null -and $cb_LargBufferListDepth.Text -ne '' ){
			Write-Host "Set AFDLargBufferListDepth to"$cb_LargBufferListDepth.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargBufferListDepth" -Typ "Dword" -Value $cb_LargBufferListDepth.Text -Force
		}elseif($A -eq $true -and $cb_LargBufferListDepth.Text -eq $null -or $cb_LargBufferListDepth.Text -eq ''){
			Write-Warning "Removing AFDLargBufferListDepth"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargBufferListDepth"
		}else{
			Write-Host "Set AFDLargBufferListDepth to"$cb_LargBufferListDepth.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "LargBufferListDepth" -Value $cb_LargBufferListDepth.Text -Force	
		}
		
		#DisableDirectAcceptEx
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DisableDirectAcceptEx")
		if ($A -eq $false -and $cb_DisableDirectAcceptEx.Text -ne $null -and $cb_DisableDirectAcceptEx.Text -ne '' ){
			Write-Host "Set AFDDisableDirectAcceptEx to"$cb_DisableDirectAcceptEx.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableDirectAcceptEx" -Typ "Dword" -Value $cb_DisableDirectAcceptEx.Text -Force
		}elseif($A -eq $true -and $cb_DisableDirectAcceptEx.Text -eq $null -or $cb_DisableDirectAcceptEx.Text -eq ''){
			Write-Warning "Removing AFDDisableDirectAcceptEx"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableDirectAcceptEx"
		}else{
			Write-Host "Set AFDDisableDirectAcceptEx to"$cb_DisableDirectAcceptEx.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableDirectAcceptEx" -Value $cb_DisableDirectAcceptEx.Text -Force	
		}
		
		#DisableChainedReceive
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DisableChainedReceive")
		if ($A -eq $false -and $cb_DisableChainedReceive.Text -ne $null -and $cb_DisableChainedReceive.Text -ne '' ){
			Write-Host "Set AFDDisableChainedReceive to"$cb_DisableChainedReceive.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableChainedReceive" -Typ "Dword" -Value $cb_DisableChainedReceive.Text -Force
		}elseif($A -eq $true -and $cb_DisableChainedReceive.Text -eq $null -or $cb_DisableChainedReceive.Text -eq ''){
			Write-Warning "Removing AFDDisableChainedReceive"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableChainedReceive"
		}else{
			Write-Host "Set AFDDisableChainedReceive to"$cb_DisableChainedReceive.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableChainedReceive" -Value $cb_DisableChainedReceive.Text -Force	
		}
		
		#DisableRawSecurity
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DisableRawSecurity")
		if ($A -eq $false -and $cb_DisableRawSecurity.Text -ne $null -and $cb_DisableRawSecurity.Text -ne '' ){
			Write-Host "Set AFDDisableRawSecurity to"$cb_DisableRawSecurity.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableRawSecurity" -Typ "Dword" -Value $cb_DisableRawSecurity.Text -Force
		}elseif($A -eq $true -and $cb_DisableRawSecurity.Text -eq $null -or $cb_DisableRawSecurity.Text -eq ''){
			Write-Warning "Removing AFDDisableRawSecurity"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableRawSecurity"
		}else{
			Write-Host "Set AFDDisableRawSecurity to"$cb_DisableRawSecurity.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DisableRawSecurity" -Value $cb_DisableRawSecurity.Text -Force	
		}
		
		#DynamicSendBufferDisable
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "DynamicSendBufferDisable")
		if ($A -eq $false -and $cb_DynamicSendBufferDisable.Text -ne $null -and $cb_DynamicSendBufferDisable.Text -ne '' ){
			Write-Host "Set AFDDynamicSendBufferDisable to"$cb_DynamicSendBufferDisable.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DynamicSendBufferDisable" -Typ "Dword" -Value $cb_DynamicSendBufferDisable.Text -Force
		}elseif($A -eq $true -and $cb_DynamicSendBufferDisable.Text -eq $null -or $cb_DynamicSendBufferDisable.Text -eq ''){
			Write-Warning "Removing AFDDynamicSendBufferDisable"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DynamicSendBufferDisable"
		}else{
			Write-Host "Set AFDDynamicSendBufferDisable to"$cb_DynamicSendBufferDisable.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "DynamicSendBufferDisable" -Value $cb_DynamicSendBufferDisable.Text -Force	
		}
		
		#FastSendDatagramThreshold
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "FastSendDatagramThreshold")
		if ($A -eq $false -and $cb_FastSendDatagramThreshold.Text -ne $null -and $cb_FastSendDatagramThreshold.Text -ne '' ){
			Write-Host "Set AFDFastSendDatagramThreshold to"$cb_FastSendDatagramThreshold.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastSendDatagramThreshold" -Typ "Dword" -Value $cb_FastSendDatagramThreshold.Text -Force
		}elseif($A -eq $true -and $cb_FastSendDatagramThreshold.Text -eq $null -or $cb_FastSendDatagramThreshold.Text -eq ''){
			Write-Warning "Removing AFDFastSendDatagramThreshold"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastSendDatagramThreshold"
		}else{
			Write-Host "Set AFDFastSendDatagramThreshold to"$cb_FastSendDatagramThreshold.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastSendDatagramThreshold" -Value $cb_FastSendDatagramThreshold.Text -Force	
		}
		
		#FastCopyReceiveThreshold
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "FastCopyReceiveThreshold")
		if ($A -eq $false -and $cb_FastCopyReceiveThreshold.Text -ne $null -and $cb_FastCopyReceiveThreshold.Text -ne '' ){
			Write-Host "Set AFDFastCopyReceiveThreshold to"$cb_FastCopyReceiveThreshold.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastCopyReceiveThreshold" -Typ "Dword" -Value $cb_FastCopyReceiveThreshold.Text -Force
		}elseif($A -eq $true -and $cb_FastCopyReceiveThreshold.Text -eq $null -or $cb_FastCopyReceiveThreshold.Text -eq ''){
			Write-Warning "Removing AFDFastCopyReceiveThreshold"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastCopyReceiveThreshold"
		}else{
			Write-Host "Set AFDFastCopyReceiveThreshold to"$cb_FastCopyReceiveThreshold.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "FastCopyReceiveThreshold" -Value $cb_FastCopyReceiveThreshold.Text -Force	
		}
		
		#IgnorePushBitOnReceives
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "IgnorePushBitOnReceives")
		if ($A -eq $false -and $cb_IgnorePushBitOnReceives.Text -ne $null -and $cb_IgnorePushBitOnReceives.Text -ne '' ){
			Write-Host "Set AFDIgnorePushBitOnReceives to"$cb_IgnorePushBitOnReceives.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnorePushBitOnReceives" -Typ "Dword" -Value $cb_IgnorePushBitOnReceives.Text -Force
		}elseif($A -eq $true -and $cb_IgnorePushBitOnReceives.Text -eq $null -or $cb_IgnorePushBitOnReceives.Text -eq ''){
			Write-Warning "Removing AFDIgnorePushBitOnReceives"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnorePushBitOnReceives"
		}else{
			Write-Host "Set AFDIgnorePushBitOnReceives to"$cb_IgnorePushBitOnReceives.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnorePushBitOnReceives" -Value $cb_IgnorePushBitOnReceives.Text -Force	
		}
		
	    #IgnoreOrderlyRelease
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "IgnoreOrderlyRelease")
		if ($A -eq $false -and $cb_IgnoreOrderlyRelease.Text -ne $null -and $cb_IgnoreOrderlyRelease.Text -ne '' ){
			Write-Host "Set AFDIgnoreOrderlyRelease to"$cb_IgnoreOrderlyRelease.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnoreOrderlyRelease" -Typ "Dword" -Value $cb_IgnoreOrderlyRelease.Text -Force
		}elseif($A -eq $true -and $cb_IgnoreOrderlyRelease.Text -eq $null -or $cb_IgnoreOrderlyRelease.Text -eq ''){
			Write-Warning "Removing AFDIgnoreOrderlyRelease"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnoreOrderlyRelease"
		}else{
			Write-Host "Set AFDIgnoreOrderlyRelease to"$cb_IgnoreOrderlyRelease.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "IgnoreOrderlyRelease" -Value $cb_IgnoreOrderlyRelease.Text -Force	
		}
		
		#TransmitWorker
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "TransmitWorker")
		if ($A -eq $false -and $cb_TransmitWorker.Text -ne $null -and $cb_TransmitWorker.Text -ne '' ){
			Write-Host "Set AFDTransmitWorker to"$cb_TransmitWorker.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "TransmitWorker" -Typ "Dword" -Value $cb_TransmitWorker.Text -Force
		}elseif($A -eq $true -and $cb_TransmitWorker.Text -eq $null -or $cb_TransmitWorker.Text -eq ''){
			Write-Warning "Removing AFDTransmitWorker"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "TransmitWorker"
		}else{
			Write-Host "Set AFDTransmitWorker to"$cb_TransmitWorker.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "TransmitWorker" -Value $cb_TransmitWorker.Text -Force	
		}
		
		#PriorityBoost
		$A=((Get-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters").PSObject.Properties.Name -contains "PriorityBoost")
		if ($A -eq $false -and $cb_PriorityBoost.Text -ne $null -and $cb_PriorityBoost.Text -ne '' ){
			Write-Host "Set AFDPriorityBoost to"$cb_PriorityBoost.Text -ForegroundColor Green
	        New-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "PriorityBoost" -Typ "Dword" -Value $cb_PriorityBoost.Text -Force
		}elseif($A -eq $true -and $cb_PriorityBoost.Text -eq $null -or $cb_PriorityBoost.Text -eq ''){
			Write-Warning "Removing AFDPriorityBoost"
			Remove-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "PriorityBoost"
		}else{
			Write-Host "Set AFDPriorityBoost to"$cb_PriorityBoost.Text -ForegroundColor Green
	        Set-ItemProperty -Path "REGISTRY::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AFD\Parameters" -Name "PriorityBoost" -Value $cb_PriorityBoost.Text -Force	
		}
		
		
}

function Opacity {
    $modes = @{OpacityOn = "0.90"; OpacityOff = "1"}
    $Form.Opacity = $(if ($Form.Opacity -eq $modes.OpacityOn){ $modes.OpacityOff } else { $modes.OpacityOn})

}

function HWSettings {
    
	if ($cb_MsiMode.SelectedIndex -eq (Get-ItemPropertyValue -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" -Name "MSISupported")){
		Write-Host "MSI-Mode is same then Registry, skipping."  -ForegroundColor Green}
				
		elseif ($cb_MsiMode.SelectedIndex -eq '0'){
		Write-Host "Disabling MSI Mode." -ForegroundColor Green
        Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" -Name "MSISupported" -Value "0"}
		
		elseif ($cb_MsiMode.SelectedIndex -eq '1'){
		Write-Host "Enabling MSI Mode." -ForegroundColor Green
        Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" -Name "MSISupported" -Value "1"}
	
	
	if ($DevicePriorityAvailable -eq $false){
		    Write-Host "Creating Device Priority DWORD with Value $($cb_InterruptPriority.Text)."  -ForegroundColor Green
		    New-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority" -Typ "Dword"
		}elseif ($cb_InterruptPriority.SelectedIndex -eq '0'){
			Write-Host "Setting DevicePriority to Undefined." -ForegroundColor Green
            Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority" -Value "0"}
			
		elseif ($cb_InterruptPriority.SelectedIndex -eq '1'){
			Write-Host "Setting DevicePriority to Low." -ForegroundColor Green
            Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority" -Value "1"
		    }
		    
		elseif ($cb_InterruptPriority.SelectedIndex -eq '2'){
			    Write-Host "Setting DevicePriority to Normal." -ForegroundColor Green
                Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority" -Value "2"
		    }
			
		elseif ($cb_InterruptPriority.SelectedIndex -eq '3'){
			    Write-Host "Setting DevicePriority to High." -ForegroundColor Green
                Set-ItemProperty -Path "REGISTRY::$NewPathInterrupt\Device Parameters\Interrupt Management\Affinity Policy" -Name "DevicePriority" -Value "3"
		    }
}


$btn_InterruptApply.Add_Click({cls; HWSettings})
$btn_registrytweaksapply.Add_Click({cls; RegistryTweaks})
$cb_IPv4.Add_CheckedChanged({ IPv4_CheckedChanged })
$cb_IPv6.Add_CheckedChanged({ IPv6_CheckedChanged })
$btn_applyInterfaceSettings.Add_Click({cls; ApplyInterfaceSettings})
$btn_openreg.Add_Click({btn_regopadap})
$btn_adaptrest.Add_Click({cls; adapter_restart})
$btn_applyadv.Add_Click({cls; applyadvsettings})
$btn_applyglobal.Add_Click({cls; applyglobal })
#$btn_applotadapters.Add_Click({applyotAdapters})
$btn_apply.Add_Click({ cls; applyrsssettings })
# Source: https://community.spiceworks.com/topic/2239276-script-help-to-disable-power-management-on-network-cards
$btn_applypowersettings.Add_Click({cls; applypowersavingsettings})
$btn_applyall.Add_Click({cls; applyall})
$btn_rssaddsupport.Add_Click({cls; RSSEnable})
$btn_unqueues.Add_Click({cls; RSSQueuesUnlock})
$btn_Opacity.Add_Click({Opacity})

#Notes:
# Adding Additional Settings for Adv Tweaking

#endregion

[void]$Form.ShowDialog()