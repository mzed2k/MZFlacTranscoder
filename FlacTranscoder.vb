Imports System
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Reflection

' Metadaten der Anwendung
<Assembly: AssemblyTitle("MZ FLAC, WAV & MP3 Transcoder")>
<Assembly: AssemblyProduct("MZ FLAC & WAV Transcoder")>
<Assembly: AssemblyCompany("mzed.de")>
<Assembly: AssemblyDescription("Tool zum simultanen Transkodieren von FLAC und WAV Dateien inkl. Playlist-Erstellung.")>
<Assembly: AssemblyVersion("1.5.0.0")>
<Assembly: AssemblyFileVersion("1.5.0.0")>
<Assembly: AssemblyCopyright("© 2026 mz@mzed.de")>

' ==================================================================================
' KOMPILIEREN MIT VBC (Visual Basic Compiler .NET 4.0 / VB 2010):
'
' C:\Windows\Microsoft.NET\Framework64\v4.0.30319\vbc.exe /target:winexe /out:FlacTranscoder.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:mz-fimo-logo.ico FlacTranscoder.vb
' ==================================================================================

Public Class FlacTranscoderForm
    Inherits Form

    ' --- Konfiguration ---
    Private FFMPEG_EXE As String = "bin\ffmpeg.exe"
    Private DEBUGGING As Boolean = False
    Private ReadOnly iniFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FlacTranscoder.ini")

    ' --- Datenlisten ---
    Private sampleRateOptions As New List(Of KeyValuePair(Of String, String))()
    Private bitDepthOptions As New List(Of KeyValuePair(Of String, String))()
    Private mp3RateOptions As New List(Of KeyValuePair(Of String, String))()
    Private mp3QualOptions As New List(Of KeyValuePair(Of String, String))()

	' --- UI Elemente ---
	Private WithEvents textSource As TextBox
	Private btnBrowse, btnStart, btnOpenFolder, btnClose As Button
	Private rateRadios, bitRadios, mp3RateRadios, mp3QualRadios As RadioButton()
	Private chkFlac, chkMp3, chkWav As CheckBox
	Private chkSourceFlac, chkSourceWav As CheckBox
	Private chkRecursive As CheckBox                 ' NEU: rekursiv
	Private progressBar As ProgressBar
	Private textStatus As TextBox


    Public Sub New()
        LoadConfigFromIni()
        InitializeComponent()
        CheckFFmpeg()
    End Sub

    ' =============================================================================
    ' 1. INITIALISIERUNG
    ' =============================================================================

Private Sub LoadConfigFromIni()
    ' Wenn INI nicht existiert, Default-INI erstellen
    If Not File.Exists(iniFilePath) Then
        Try
            Dim sb As New StringBuilder()

            sb.AppendLine("[General]")
            sb.AppendLine("FfmpegPath=bin\ffmpeg.exe")
            sb.AppendLine("Debugging=False")
            sb.AppendLine()
            sb.AppendLine("[SampleRates]")
            sb.AppendLine("44.1 kHz=44100|44kHz")
            sb.AppendLine("48.0 kHz=48000|48kHz")
            sb.AppendLine("88.2 kHz=88200|88kHz")
            sb.AppendLine("96.0 kHz=96000|96kHz")
            sb.AppendLine()
            sb.AppendLine("[BitDepths]")
            sb.AppendLine("16 Bit=16|s16")
            sb.AppendLine("24 Bit=24|s32")
            sb.AppendLine()
            sb.AppendLine("[Mp3SampleRates]")
            sb.AppendLine("44.1 kHz (Standard)=44100")
            sb.AppendLine("48.0 kHz=48000")
            sb.AppendLine(";32.0 kHz=32000")
            sb.AppendLine()
            sb.AppendLine("[Mp3Qualities]")
            sb.AppendLine("V0 (Extrem ~245 kbps)=0")
            sb.AppendLine("V2 (Hoch ~190 kbps)=2")
            sb.AppendLine(";320k (Konstant)=320k")
            sb.AppendLine("128k (Konstant)=128k")

            File.WriteAllText(iniFilePath, sb.ToString(), Encoding.UTF8)
        Catch ex As Exception
            MessageBox.Show("INI-Datei konnte nicht erstellt werden: " & ex.Message, _
                            "INI-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End If

    ' Listen zurücksetzen
    sampleRateOptions.Clear()
    bitDepthOptions.Clear()
    mp3RateOptions.Clear()
    mp3QualOptions.Clear()

    ' INI einlesen
    If File.Exists(iniFilePath) Then
        Try
            Dim currentSection As String = ""
            For Each rawLine As String In File.ReadAllLines(iniFilePath, Encoding.UTF8)
                Dim line As String = rawLine.Trim()
                If line = "" OrElse line.StartsWith(";") OrElse line.StartsWith("#") Then Continue For

                If line.StartsWith("[") AndAlso line.EndsWith("]") Then
                    currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant()
                    Continue For
                End If

                Dim parts() As String = line.Split(New Char() {"="c}, 2)
                If parts.Length <> 2 Then Continue For

                Dim key As String = parts(0).Trim()
                Dim value As String = parts(1).Trim()

                Select Case currentSection
                    Case "samplerates"
                        sampleRateOptions.Add(New KeyValuePair(Of String, String)(key, value))
                    Case "bitdepths"
                        bitDepthOptions.Add(New KeyValuePair(Of String, String)(key, value))
                    Case "mp3samplerates"
                        mp3RateOptions.Add(New KeyValuePair(Of String, String)(key, value))
                    Case "mp3qualities"
                        mp3QualOptions.Add(New KeyValuePair(Of String, String)(key, value))
                    Case "general"
                        If key.ToLower() = "ffmpegpath" Then FFMPEG_EXE = value
                        If key.ToLower() = "debugging" Then Boolean.TryParse(value, DEBUGGING)
                End Select
            Next
        Catch ex As Exception
            MessageBox.Show("Fehler beim Lesen der INI: " & ex.Message, _
                            "INI-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End If

    ' Fallback-Defaults setzen, falls eine Sektion leer ist
    If sampleRateOptions.Count = 0 Then
        sampleRateOptions.Add(New KeyValuePair(Of String, String)("44.1 kHz", "44100|44kHz"))
        sampleRateOptions.Add(New KeyValuePair(Of String, String)("48.0 kHz", "48000|48kHz"))
        sampleRateOptions.Add(New KeyValuePair(Of String, String)("88.2 kHz", "88200|88kHz"))
        sampleRateOptions.Add(New KeyValuePair(Of String, String)("96.0 kHz", "96000|96kHz"))
    End If

    If bitDepthOptions.Count = 0 Then
        bitDepthOptions.Add(New KeyValuePair(Of String, String)("16 Bit", "16|s16"))
        bitDepthOptions.Add(New KeyValuePair(Of String, String)("24 Bit", "24|s32"))
    End If

    If mp3RateOptions.Count = 0 Then
        mp3RateOptions.Add(New KeyValuePair(Of String, String)("44.1 kHz (Standard)", "44100"))
        mp3RateOptions.Add(New KeyValuePair(Of String, String)("48.0 kHz", "48000"))
        ' 32 kHz bleibt optional/auskommentiert
    End If

    If mp3QualOptions.Count = 0 Then
        mp3QualOptions.Add(New KeyValuePair(Of String, String)("V0 (Extrem ~245 kbps)", "0"))
        mp3QualOptions.Add(New KeyValuePair(Of String, String)("V2 (Hoch ~190 kbps)", "2"))
        mp3QualOptions.Add(New KeyValuePair(Of String, String)("128k (Konstant)", "128k"))
    End If
End Sub

    Private Sub InitializeComponent()
        Me.Text = "MZ FLAC, WAV & MP3 Transcoder"
        Me.Size = New Size(620, 850)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.BackColor = Color.FromArgb(245, 245, 245)

        Dim currentY As Integer = 20

        ' --- Quellordner ---
        CreateLabel("Quellordner (FLAC/WAV):", currentY)   ' ANPASSUNG: Text
        textSource = New TextBox()
        textSource.Location = New Point(20, currentY + 25)
        textSource.Size = New Size(430, 25)
        textSource.ReadOnly = True
        textSource.AllowDrop = True

        AddHandler textSource.DragEnter, AddressOf TextSource_DragEnter
        AddHandler textSource.DragDrop, AddressOf TextSource_DragDrop

        btnBrowse = New Button()
        btnBrowse.Location = New Point(460, currentY + 23)
        btnBrowse.Size = New Size(110, 27)
        btnBrowse.Text = "Durchsuchen"
        AddHandler btnBrowse.Click, AddressOf BtnBrowse_Click

        Me.Controls.Add(textSource)
        Me.Controls.Add(btnBrowse)
        currentY += 70

        ' --- Quellformate auswählen (FLAC / WAV) ---
        Dim gbSourceFormats As New GroupBox()
        gbSourceFormats.Text = "Quellformate auswählen"
        gbSourceFormats.Location = New Point(20, currentY)
        gbSourceFormats.Size = New Size(550, 55)

        chkSourceFlac = New CheckBox()
        chkSourceFlac.Text = "FLAC (.flac)"
        chkSourceFlac.Location = New Point(15, 22)
        chkSourceFlac.Checked = True
        chkSourceFlac.AutoSize = True

        chkSourceWav = New CheckBox()
        chkSourceWav.Text = "WAV (.wav)"
        chkSourceWav.Location = New Point(150, 22)
        chkSourceWav.Checked = False
        chkSourceWav.AutoSize = True
    
		chkRecursive = New CheckBox()
		chkRecursive.Text = "rekursiv (Unterordner einschließen)"
		chkRecursive.Location = New Point(285, 22)
		chkRecursive.AutoSize = True
		chkRecursive.Checked = False

        gbSourceFormats.Controls.Add(chkSourceFlac)
        gbSourceFormats.Controls.Add(chkSourceWav)
		gbSourceFormats.Controls.Add(chkRecursive)
        Me.Controls.Add(gbSourceFormats)
        currentY += 70

    ' --- Zielformate ---
    Dim gbFormats As New GroupBox()
    gbFormats.Text = "Zielformate auswählen"
    gbFormats.Location = New Point(20, currentY)
    gbFormats.Size = New Size(550, 55)


        chkFlac = New CheckBox()
        chkFlac.Text = "FLAC erzeugen"
        chkFlac.Location = New Point(15, 22)
        chkFlac.Checked = True
        chkFlac.AutoSize = True

        chkMp3 = New CheckBox()
        chkMp3.Text = "MP3 erzeugen"
        chkMp3.Location = New Point(150, 22)
        chkMp3.Checked = True
        chkMp3.AutoSize = True

        chkWav = New CheckBox()
        chkWav.Text = "WAV erzeugen"
        chkWav.Location = New Point(285, 22)
        chkWav.Checked = False
        chkWav.AutoSize = True

        gbFormats.Controls.Add(chkFlac)
        gbFormats.Controls.Add(chkMp3)
        gbFormats.Controls.Add(chkWav)
        Me.Controls.Add(gbFormats)
        currentY += 70

        ' --- Optionen ---
        rateRadios = CreateOptionGroup("Ziel-Samplerate (FLAC/WAV)", sampleRateOptions, currentY)   ' ANPASSUNG: Titel
        currentY += 85
        bitRadios = CreateOptionGroup("Bittiefe (FLAC/WAV)", bitDepthOptions, currentY)              ' ANPASSUNG: Titel
        currentY += 85
        mp3QualRadios = CreateOptionGroup("MP3 Qualität (LAME VBR/CBR)", mp3QualOptions, currentY)
        currentY += 85
        mp3RateRadios = CreateOptionGroup("MP3 Samplerate", mp3RateOptions, currentY)
        currentY += 85

        ' --- Status ---
        progressBar = New ProgressBar()
        progressBar.Location = New Point(20, currentY)
        progressBar.Size = New Size(550, 20)
        Me.Controls.Add(progressBar)

        textStatus = New TextBox()
        textStatus.Location = New Point(20, currentY + 30)
        textStatus.Size = New Size(550, 150)
        textStatus.Multiline = True
        textStatus.ReadOnly = True
        textStatus.ScrollBars = ScrollBars.Vertical
        textStatus.Font = New Font("Consolas", 8)
        Me.Controls.Add(textStatus)
        currentY += 195

        ' --- Buttons ---
        btnStart = New Button()
        btnStart.Location = New Point(20, currentY)
        btnStart.Size = New Size(130, 40)
        btnStart.Text = "Starten"
        btnStart.BackColor = Color.LightGreen
        btnStart.Font = New Font(Me.Font, FontStyle.Bold)
        AddHandler btnStart.Click, AddressOf BtnStart_Click
        Me.Controls.Add(btnStart)

        btnOpenFolder = New Button()
        btnOpenFolder.Location = New Point(160, currentY)
        btnOpenFolder.Size = New Size(130, 40)
        btnOpenFolder.Text = "Ordner öffnen"
        AddHandler btnOpenFolder.Click, AddressOf BtnOpenFolder_Click
        Me.Controls.Add(btnOpenFolder)

        btnClose = New Button()
        btnClose.Location = New Point(440, currentY)
        btnClose.Size = New Size(130, 40)
        btnClose.Text = "Beenden"
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub

    ' =============================================================================
    ' 2. HAUPTLOGIK & VERARBEITUNG
    ' =============================================================================

Private Sub BtnStart_Click(sender As Object, e As EventArgs)
    If String.IsNullOrEmpty(textSource.Text) OrElse Not Directory.Exists(textSource.Text) Then
        MessageBox.Show("Bitte gültigen Quellordner wählen!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Return
    End If

    ' Prüfen, ob mindestens ein Quellformat gewählt ist
    If Not chkSourceFlac.Checked AndAlso Not chkSourceWav.Checked Then
        MessageBox.Show("Bitte mindestens ein Quellformat (FLAC oder WAV) auswählen!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Return
    End If

    ' Prüfen, ob mindestens ein Zielformat gewählt ist
    If Not chkFlac.Checked AndAlso Not chkMp3.Checked AndAlso Not chkWav.Checked Then
        MessageBox.Show("Bitte mindestens ein Zielformat auswählen!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Return
    End If

    btnStart.Enabled = False
    UpdateStatus("=== Starte Transkodierung ===")

    ' Gemeinsame Ziel-Samplerate & Bittiefe für FLAC/WAV
    Dim flacRateTag As String = GetSelectedTag(rateRadios)
    If String.IsNullOrEmpty(flacRateTag) Then
        MessageBox.Show("Keine gültige Ziel-Samplerate gewählt!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        btnStart.Enabled = True
        Return
    End If
    Dim flacRateParts() As String = flacRateTag.Split("|"c)
    Dim flacRate As String = flacRateParts(0)      ' z.B. "44100"
    Dim flacRateLabel As String = flacRateParts(1) ' z.B. "44kHz"

    Dim bitTag As String = GetSelectedTag(bitRadios)
    If String.IsNullOrEmpty(bitTag) Then
        MessageBox.Show("Keine gültige Bittiefe gewählt!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        btnStart.Enabled = True
        Return
    End If
    Dim bitParts() As String = bitTag.Split("|"c)
    Dim bitDepthLabel As String = bitParts(0)      ' "16" oder "24"
    Dim flacFmt As String = bitParts(1)            ' "s16" oder "s32"

    Dim mp3Rate As String = GetSelectedTag(mp3RateRadios)
    Dim mp3Qual As String = GetSelectedTag(mp3QualRadios)

    ' --- Quell-Dateiliste zusammenstellen (FLAC/WAV, optional rekursiv) ---
    Dim allSourceFiles As New List(Of String)()
    Dim searchOption As SearchOption = If(chkRecursive.Checked, SearchOption.AllDirectories, SearchOption.TopDirectoryOnly)

    If chkSourceFlac.Checked Then
        Try
            Dim flacFiles() As String = Directory.GetFiles(textSource.Text, "*.flac", searchOption)
            allSourceFiles.AddRange(flacFiles)
        Catch ex As Exception
            UpdateStatus("Fehler beim Einlesen von FLAC-Dateien: " & ex.Message)
        End Try
    End If

    If chkSourceWav.Checked Then
        Try
            Dim wavFiles() As String = Directory.GetFiles(textSource.Text, "*.wav", searchOption)
            allSourceFiles.AddRange(wavFiles)
        Catch ex As Exception
            UpdateStatus("Fehler beim Einlesen von WAV-Dateien: " & ex.Message)
        End Try
    End If

    If allSourceFiles.Count = 0 Then
        UpdateStatus("Keine passenden Quelldateien (.flac/.wav) gefunden.")
        MessageBox.Show("Keine passenden Quelldateien im Ordner.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        btnStart.Enabled = True
        Return
    End If

    Dim expectedFiles As New List(Of String)()

    ' Für rekursive Verarbeitung: welche Zielordner wurden verwendet?
    Dim usedFlacDirs As New HashSet(Of String)()
    Dim usedMp3Dirs As New HashSet(Of String)()
    Dim usedWavDirs As New HashSet(Of String)()

    ' Nicht-rekursiv: globaler Zielordner wie bisher
    Dim targetDirFlac As String = Nothing
    Dim targetDirMp3 As String = Nothing
    Dim targetDirWav As String = Nothing

    If Not chkRecursive.Checked Then
        If chkFlac.Checked Then
            targetDirFlac = Path.Combine(textSource.Text, String.Format("Output_FLAC_{0}_{1}bit", flacRateLabel, bitDepthLabel))
            If Not Directory.Exists(targetDirFlac) Then Directory.CreateDirectory(targetDirFlac)
        End If

        If chkMp3.Checked Then
            targetDirMp3 = Path.Combine(textSource.Text, String.Format("Output_MP3_{0}_{1}", mp3Qual, mp3Rate))
            If Not Directory.Exists(targetDirMp3) Then Directory.CreateDirectory(targetDirMp3)
        End If

        If chkWav.Checked Then
            targetDirWav = Path.Combine(textSource.Text, String.Format("Output_WAV_{0}_{1}bit", flacRateLabel, bitDepthLabel))
            If Not Directory.Exists(targetDirWav) Then Directory.CreateDirectory(targetDirWav)
        End If

        ' Zusatzdateien nur im Hauptquellordner -> Zielordner
        UpdateStatus("Prüfe auf Zusatzdateien (Cover/PDF)...")
        If chkFlac.Checked AndAlso targetDirFlac IsNot Nothing Then
            CopyExtras(textSource.Text, targetDirFlac)
        End If
        If chkMp3.Checked AndAlso targetDirMp3 IsNot Nothing Then
            CopyExtras(textSource.Text, targetDirMp3)
        End If
        If chkWav.Checked AndAlso targetDirWav IsNot Nothing Then
            CopyExtras(textSource.Text, targetDirWav)
        End If
    Else
        ' Rekursiv: Zusatzdateien werden pro Unterordner behandelt (siehe Schleife unten)
        UpdateStatus("Rekursiv: Ausgabeordner pro Unterordner, Zusatzdateien pro Ordner.")
    End If

    progressBar.Maximum = allSourceFiles.Count
    progressBar.Value = 0

    ' --- Transkodierung ---
    For Each fileP In allSourceFiles
        Dim fileName As String = Path.GetFileNameWithoutExtension(fileP)

        ' Basisordner der Quelldatei (für rekursive Struktur)
        Dim baseDir As String = Path.GetDirectoryName(fileP)

        Dim outFlacPath As String = Nothing
        Dim outMp3Path As String = Nothing
        Dim outWavPath As String = Nothing

        Dim localTargetFlac As String = targetDirFlac
        Dim localTargetMp3 As String = targetDirMp3
        Dim localTargetWav As String = targetDirWav

        If chkRecursive.Checked Then
            ' Für jede Quelldatei eigenen Zielordner relativ zum jeweiligen Quell-Unterordner
            If chkFlac.Checked Then
                localTargetFlac = Path.Combine(baseDir, String.Format("Output_FLAC_{0}_{1}bit", flacRateLabel, bitDepthLabel))
                If Not Directory.Exists(localTargetFlac) Then Directory.CreateDirectory(localTargetFlac)
                If Not usedFlacDirs.Contains(localTargetFlac) Then
                    usedFlacDirs.Add(localTargetFlac)
                    ' Zusatzdateien aus diesem Unterordner kopieren
                    CopyExtras(baseDir, localTargetFlac)
                End If
            End If

            If chkMp3.Checked Then
                localTargetMp3 = Path.Combine(baseDir, String.Format("Output_MP3_{0}_{1}", mp3Qual, mp3Rate))
                If Not Directory.Exists(localTargetMp3) Then Directory.CreateDirectory(localTargetMp3)
                If Not usedMp3Dirs.Contains(localTargetMp3) Then
                    usedMp3Dirs.Add(localTargetMp3)
                    CopyExtras(baseDir, localTargetMp3)
                End If
            End If

            If chkWav.Checked Then
                localTargetWav = Path.Combine(baseDir, String.Format("Output_WAV_{0}_{1}bit", flacRateLabel, bitDepthLabel))
                If Not Directory.Exists(localTargetWav) Then Directory.CreateDirectory(localTargetWav)
                If Not usedWavDirs.Contains(localTargetWav) Then
                    usedWavDirs.Add(localTargetWav)
                    CopyExtras(baseDir, localTargetWav)
                End If
            End If
        End If

        If chkFlac.Checked AndAlso localTargetFlac IsNot Nothing Then
            outFlacPath = Path.Combine(localTargetFlac, fileName & ".flac")
            expectedFiles.Add(outFlacPath)
        End If

        If chkMp3.Checked AndAlso localTargetMp3 IsNot Nothing Then
            outMp3Path = Path.Combine(localTargetMp3, fileName & ".mp3")
            expectedFiles.Add(outMp3Path)
        End If

        If chkWav.Checked AndAlso localTargetWav IsNot Nothing Then
            outWavPath = Path.Combine(localTargetWav, fileName & ".wav")
            expectedFiles.Add(outWavPath)
        End If

        UpdateStatus("Bearbeite: " & fileP)

        Dim args As String = BuildFfmpegArguments( _
            fileP, outFlacPath, outMp3Path, outWavPath, _
            flacRate, flacFmt, bitDepthLabel, _
            mp3Rate, mp3Qual)

        If DEBUGGING Then UpdateStatus("CMD: " & args)

        ExecuteFfmpeg(args)
        progressBar.Value += 1
        Application.DoEvents()
    Next

    ' --- Playlist Erstellung ---
    If Not chkRecursive.Checked Then
        ' Wie bisher: ein Zielordner pro Format
        If chkFlac.Checked AndAlso targetDirFlac IsNot Nothing Then
            CreatePlaylist(targetDirFlac, ".flac")
        End If

        If chkMp3.Checked AndAlso targetDirMp3 IsNot Nothing Then
            CreatePlaylist(targetDirMp3, ".mp3")
        End If

        If chkWav.Checked AndAlso targetDirWav IsNot Nothing Then
            CreatePlaylist(targetDirWav, ".wav")
        End If
    Else
        ' Rekursiv: für alle verwendeten Zielordner jeweils eine Playlist
        If chkFlac.Checked Then
            For Each dir As String In usedFlacDirs
                CreatePlaylist(dir, ".flac")
            Next
        End If

        If chkMp3.Checked Then
            For Each dir As String In usedMp3Dirs
                CreatePlaylist(dir, ".mp3")
            Next
        End If

        If chkWav.Checked Then
            For Each dir As String In usedWavDirs
                CreatePlaylist(dir, ".wav")
            Next
        End If
    End If

    ' --- Abschluss ---
    ValidateResults(expectedFiles)
    btnStart.Enabled = True
End Sub



    ''' <summary>
    ''' Erstellt eine M3U Playlist im Zielordner.
    ''' </summary>
    Private Sub CreatePlaylist(targetDir As String, extension As String)
        Try
            Dim files As String() = Directory.GetFiles(targetDir, "*" & extension)
            Array.Sort(files) ' Alphabetisch sortieren

            ' Name der Playlist = Name des Ordners
            Dim folderName As String = New DirectoryInfo(targetDir).Name
            Dim playlistPath As String = Path.Combine(targetDir, folderName & ".m3u")

            Using writer As New StreamWriter(playlistPath, False, Encoding.UTF8)
                For Each f In files
                    writer.WriteLine(Path.GetFileName(f))
                Next
            End Using

            UpdateStatus("  + Playlist erstellt: " & folderName & ".m3u")
        Catch ex As Exception
            UpdateStatus("  ! Fehler bei Playlist-Erstellung: " & ex.Message)
        End Try
    End Sub

    Private Sub CopyExtras(sourceDir As String, targetDir As String)
        If String.IsNullOrEmpty(targetDir) OrElse Not Directory.Exists(targetDir) Then Return
        Dim extensions() As String = { "*.jpg", "*.jpeg", "*.pdf" }
        For Each ext In extensions
            Dim extraFiles As String() = Directory.GetFiles(sourceDir, ext)
            For Each f In extraFiles
                Try
                    Dim destPath As String = Path.Combine(targetDir, Path.GetFileName(f))
                    File.Copy(f, destPath, True)
                    UpdateStatus("  + Kopiert: " & Path.GetFileName(f))
                Catch ex As Exception
                    UpdateStatus("  ! Fehler Kopie: " & ex.Message)
                End Try
            Next
        Next
    End Sub

    ''' <summary>
    ''' Baut die FFmpeg-Argumente für bis zu drei Ausgabestreams (FLAC / MP3 / WAV).
    ''' FLAC und WAV teilen sich Samplerate & Bittiefe.
    ''' </summary>
    Private Function BuildFfmpegArguments(input As String, _
                                          outFlac As String, _
                                          outMp3 As String, _
                                          outWav As String, _
                                          fRate As String, _
                                          fFmt As String, _
                                          bitDepthLabel As String, _
                                          mRate As String, _
                                          mQual As String) As String

        Dim args As String = "-v error -i """ & input & """ "

        Dim streamIndex As Integer = 0

        ' FLAC-Ausgabe
        If outFlac IsNot Nothing Then
            Dim idx As Integer = streamIndex
            streamIndex += 1
            args &= String.Format( _
                "-map 0:a:0 -c:a:{0} flac -sample_fmt {1} -af ""aresample={2}:dither_method=triangular"" -map_metadata 0 ""{3}"" ", _
                idx, fFmt, fRate, outFlac)
        End If

        ' WAV-Ausgabe (PCM, gleiche Samplerate/Bittiefe wie FLAC)
        If outWav IsNot Nothing Then
            Dim idx As Integer = streamIndex
            streamIndex += 1

            ' Bitabhängiger WAV-Codec
            Dim wavCodec As String = "pcm_s16le"
            Dim bitDepthInt As Integer
            If Integer.TryParse(bitDepthLabel, bitDepthInt) Then
                If bitDepthInt >= 24 Then
                    ' 24 Bit -> 24-bit little endian
                    wavCodec = "pcm_s24le"
                Else
                    wavCodec = "pcm_s16le"
                End If
            End If

            args &= String.Format( _
                "-map 0:a:0 -c:a:{0} {1} -af ""aresample={2}:dither_method=triangular"" -map_metadata 0 ""{3}"" ", _
                idx, wavCodec, fRate, outWav)
        End If

        ' MP3-Ausgabe
        If outMp3 IsNot Nothing Then
            Dim idx As Integer = streamIndex
            streamIndex += 1

            Dim bitrateParam As String
            If Not String.IsNullOrEmpty(mQual) AndAlso mQual.Contains("k") Then
                ' CBR
                bitrateParam = "-b:a:" & idx & " " & mQual
            Else
                ' VBR-Qualität
                bitrateParam = "-q:a:" & idx & " " & mQual
            End If

            args &= String.Format( _
                "-map 0:a:0 -c:a:{0} libmp3lame {1} -af ""aresample={2}:dither_method=triangular"" -id3v2_version 3 -map_metadata 0 ""{3}"" ", _
                idx, bitrateParam, mRate, outMp3)
        End If

        args &= "-y"
        Return args
    End Function

    Private Sub ExecuteFfmpeg(args As String)
        Try
            Dim psi As New ProcessStartInfo(FFMPEG_EXE, args)
            psi.CreateNoWindow = True
            psi.UseShellExecute = False
            psi.RedirectStandardError = True
            Using p As Process = Process.Start(psi)
                Dim err As String = p.StandardError.ReadToEnd()
                p.WaitForExit()
                If p.ExitCode <> 0 AndAlso Not String.IsNullOrEmpty(err) Then
                    UpdateStatus("  FFMPEG Fehler: " & err)
                End If
            End Using
        Catch ex As Exception
            UpdateStatus("  Systemfehler: " & ex.Message)
        End Try
    End Sub

    Private Sub ValidateResults(expectedFiles As List(Of String))
        Dim missingCount As Integer = 0
        For Each f In expectedFiles
            If Not File.Exists(f) Then
                UpdateStatus("  FEHLT: " & Path.GetFileName(f))
                missingCount += 1
            End If
        Next
        UpdateStatus("--------------------------------------------------")
        If missingCount = 0 Then
            UpdateStatus("ERFOLG: Alle Dateien erstellt & validiert.")
            MessageBox.Show("Vorgang erfolgreich abgeschlossen!", "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            UpdateStatus("WARNUNG: " & missingCount & " Dateien fehlen!")
            MessageBox.Show("Es gab Fehler.", "Warnung", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ' --- UI Helfer ---
    Private Sub TextSource_DragEnter(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim files As String() = DirectCast(e.Data.GetData(DataFormats.FileDrop), String())
            If files.Length > 0 AndAlso Directory.Exists(files(0)) Then
                e.Effect = DragDropEffects.Copy
            Else
                e.Effect = DragDropEffects.None
            End If
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub TextSource_DragDrop(sender As Object, e As DragEventArgs)
        Try
            Dim files As String() = DirectCast(e.Data.GetData(DataFormats.FileDrop), String())
            If files.Length > 0 AndAlso Directory.Exists(files(0)) Then
                textSource.Text = files(0)
                UpdateStatus("Ordner gewählt: " & files(0))
            End If
        Catch ex As Exception
            MessageBox.Show("Fehler beim Drag & Drop: " & ex.Message)
        End Try
    End Sub

    Private Function CreateOptionGroup(title As String, options As List(Of KeyValuePair(Of String, String)), yPos As Integer) As RadioButton()
        Dim gb As New GroupBox()
        gb.Text = title
        gb.Location = New Point(20, yPos)
        gb.Size = New Size(550, 60)
        Dim rbs As New List(Of RadioButton)()
        Dim x As Integer = 15
        For Each opt In options
            Dim rb As New RadioButton()
            rb.Text = opt.Key
            rb.Tag = opt.Value
            rb.Location = New Point(x, 25)
            rb.AutoSize = True
            If rbs.Count = 0 Then rb.Checked = True
            gb.Controls.Add(rb)
            rbs.Add(rb)
            x += rb.PreferredSize.Width + 20
        Next
        Me.Controls.Add(gb)
        Return rbs.ToArray()
    End Function

    Private Sub CreateLabel(text As String, y As Integer)
        Dim lbl As New Label()
        lbl.Text = text
        lbl.Location = New Point(20, y)
        lbl.AutoSize = True
        lbl.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        Me.Controls.Add(lbl)
    End Sub

    Private Function GetSelectedTag(radios As RadioButton()) As String
        If radios Is Nothing Then Return ""
        For Each rb In radios
            If rb.Checked Then Return rb.Tag.ToString()
        Next
        Return ""
    End Function

    Private Sub UpdateStatus(msg As String)
        textStatus.AppendText(msg & vbCrLf)
        textStatus.SelectionStart = textStatus.Text.Length
        textStatus.ScrollToCaret()
    End Sub

    Private Sub BtnBrowse_Click(sender As Object, e As EventArgs)
        Using fbd As New FolderBrowserDialog()
            If fbd.ShowDialog() = DialogResult.OK Then textSource.Text = fbd.SelectedPath
        End Using
    End Sub

    Private Sub BtnOpenFolder_Click(sender As Object, e As EventArgs)
        If Directory.Exists(textSource.Text) Then Process.Start("explorer.exe", textSource.Text)
    End Sub

    Private Sub CheckFFmpeg()
        If Not File.Exists(FFMPEG_EXE) Then
            Try
                Dim psi As New ProcessStartInfo("ffmpeg", "-version")
                psi.CreateNoWindow = True
                psi.UseShellExecute = False
                Using p = Process.Start(psi)
                    p.WaitForExit()
                    If p.ExitCode = 0 Then FFMPEG_EXE = "ffmpeg"
                End Using
            Catch
                MessageBox.Show("FFMPEG nicht gefunden.", "Fehler")
            End Try
        End If
    End Sub

End Class

Module MainModule
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New FlacTranscoderForm())
    End Sub
End Module
