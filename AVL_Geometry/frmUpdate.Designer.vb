﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmUpdate
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.lblStat = New System.Windows.Forms.Label()
        Me.BackgroundWorker2 = New System.ComponentModel.BackgroundWorker()
        Me.SuspendLayout()
        '
        'Label2
        '
        Me.Label2.Dock = System.Windows.Forms.DockStyle.Top
        Me.Label2.Location = New System.Drawing.Point(0, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(426, 24)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Status"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'lblStat
        '
        Me.lblStat.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblStat.Location = New System.Drawing.Point(0, 24)
        Me.lblStat.Name = "lblStat"
        Me.lblStat.Size = New System.Drawing.Size(426, 56)
        Me.lblStat.TabIndex = 11
        Me.lblStat.Text = "Pending"
        Me.lblStat.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'BackgroundWorker2
        '
        Me.BackgroundWorker2.WorkerReportsProgress = True
        Me.BackgroundWorker2.WorkerSupportsCancellation = True
        '
        'frmUpdate
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(426, 80)
        Me.Controls.Add(Me.lblStat)
        Me.Controls.Add(Me.Label2)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmUpdate"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Checking for Update"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents Label2 As Label
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents lblStat As Label
    Friend WithEvents BackgroundWorker2 As System.ComponentModel.BackgroundWorker
End Class
