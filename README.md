# MZ FLAC, WAV & MP3 Transcoder

Ein portables Windows-Tool mit grafischer Oberfläche zum gleichzeitigen Transkodieren von **FLAC- und WAV-Dateien** nach **FLAC, MP3 und WAV**. Die Anwendung verwendet [FFmpeg](https://ffmpeg.org/) für die Audiokonvertierung, erstellt automatisch M3U-Playlists und übernimmt auf Wunsch Coverbilder sowie PDF-Dateien in die Ausgabeordner.

> [!IMPORTANT]
> **Hinweis zur Entstehung:** Der Quellcode dieses Projekts wurde zu **99,9 % mit künstlicher Intelligenz generiert**, anschließend jedoch **von einem Menschen getestet**.

## Funktionen

- FLAC und WAV als Quellformate
- Gleichzeitige Ausgabe als FLAC, MP3 und/oder WAV
- Einstellbare Samplerate und Bittiefe für FLAC/WAV
- MP3-Ausgabe mit LAME, wahlweise VBR oder CBR
- Rekursive Verarbeitung kompletter Ordnerstrukturen
- Automatische M3U-Playlist je Ausgabeordner
- Kopieren von `.jpg`, `.jpeg` und `.pdf` in die Ausgabeordner
- Übernahme vorhandener Metadaten über FFmpeg
- Konfiguration über eine automatisch erzeugte INI-Datei
- Drag & Drop für die Auswahl des Quellordners
- Fortschrittsanzeige, Statusprotokoll und Ergebnisprüfung
- Portabel, keine Installation und keine Administratorrechte erforderlich

## Voraussetzungen

- Windows 10 oder Windows 11
- .NET Framework 4.x
- `ffmpeg.exe` mit FLAC-, WAV- und `libmp3lame`-Unterstützung
- Zum Kompilieren: Visual Basic Compiler aus dem .NET Framework

## Schnellstart

1. Erstelle einen Programmordner, zum Beispiel:

   ```text
   C:\Transcoder\
   ```

2. Kopiere `FlacTranscoder.exe` in diesen Ordner.
3. Erstelle den Unterordner `bin` und lege `ffmpeg.exe` dort ab:

   ```text
   C:\Transcoder\bin\ffmpeg.exe
   ```

4. Starte `FlacTranscoder.exe`.
5. Wähle einen Quellordner über **Durchsuchen** oder per Drag & Drop.
6. Aktiviere mindestens ein Quellformat und ein Zielformat.
7. Lege Samplerate, Bittiefe und gegebenenfalls MP3-Qualität fest.
8. Klicke auf **Starten**.

Die erzeugten Dateien liegen anschließend in Unterordnern wie:

```text
Output_FLAC_44kHz_16bit
Output_MP3_0_44100
Output_WAV_44kHz_16bit
```

## Beispielhafte Verzeichnisstruktur

```text
Transcoder\
├── FlacTranscoder.exe
├── FlacTranscoder.ini       # wird bei Bedarf automatisch erstellt
└── bin\
    └── ffmpeg.exe
```

Ein Quellordner kann beispielsweise so aussehen:

```text
Album1\
├── Track01.flac
├── Track02.flac
├── cover.jpg
└── booklet.pdf
```

## Bedienung

### 1. Quellordner wählen

Der Quellordner kann über den Button **Durchsuchen** oder per Drag & Drop auf das Quellordnerfeld ausgewählt werden.

### 2. Quellformate festlegen

- **FLAC (.flac)**
- **WAV (.wav)**
- **rekursiv (Unterordner einschließen)**

Mindestens ein Quellformat muss aktiviert sein.

### 3. Zielformate festlegen

- **FLAC erzeugen**
- **MP3 erzeugen**
- **WAV erzeugen**

Mehrere Zielformate können in einem Durchlauf erzeugt werden. Mindestens ein Zielformat muss aktiviert sein.

### 4. Audiooptionen wählen

Für FLAC und WAV stehen gemeinsame Einstellungen für Samplerate und Bittiefe bereit. Die MP3-Ausgabe besitzt eigene Einstellungen für Samplerate und Qualität.

Standardmäßig sind unter anderem folgende Profile verfügbar:

- FLAC/WAV-Sampleraten: 44,1, 48, 88,2 und 96 kHz
- Bittiefen: 16 und 24 Bit
- MP3: V0, V2 und 128 kbit/s CBR
- MP3-Sampleraten: 44,1 und 48 kHz

### 5. Transkodierung starten

Nach einem Klick auf **Starten** verarbeitet die Anwendung alle gefundenen Quelldateien. Das Statusfeld zeigt den aktuellen Fortschritt und mögliche FFmpeg-Fehler. Abschließend wird geprüft, ob alle erwarteten Ausgabedateien vorhanden sind.

## Rekursive Verarbeitung

Ist **rekursiv** deaktiviert, verarbeitet das Programm nur Dateien direkt im gewählten Quellordner und legt dort die Ausgabeordner an.

Ist **rekursiv** aktiviert, werden sämtliche Unterordner durchsucht. In jedem Ordner mit passenden Quelldateien entstehen eigene Ausgabeordner. Dadurch bleibt die bestehende Album- oder Projektstruktur erhalten.

> Bereits vorhandene Ausgabeordner innerhalb des Quellbaums sollten vor einem erneuten rekursiven Lauf geprüft werden, damit erzeugte Dateien nicht unbeabsichtigt erneut als Quellen erfasst werden.

## Zusatzdateien und Playlists

Die Anwendung kopiert folgende Dateien aus dem jeweiligen Quellordner in jeden verwendeten Ausgabeordner:

- `.jpg`
- `.jpeg`
- `.pdf`

Zusätzlich wird pro Ausgabeordner eine UTF-8-kodierte M3U-Playlist erzeugt. Sie trägt den Namen des Ausgabeordners und enthält die erzeugten Audiodateien in alphabetischer Reihenfolge.

## Konfiguration

Beim ersten Start wird neben der EXE automatisch eine `FlacTranscoder.ini` mit Standardwerten erstellt:

```ini
[General]
FfmpegPath=bin\ffmpeg.exe
Debugging=False

[SampleRates]
44.1 kHz=44100|44kHz
48.0 kHz=48000|48kHz
88.2 kHz=88200|88kHz
96.0 kHz=96000|96kHz

[BitDepths]
16 Bit=16|s16
24 Bit=24|s32

[Mp3SampleRates]
44.1 kHz (Standard)=44100
48.0 kHz=48000
;32.0 kHz=32000

[Mp3Qualities]
V0 (Extrem ~245 kbps)=0
V2 (Hoch ~190 kbps)=2
;320k (Konstant)=320k
128k (Konstant)=128k
```

Zeilen, die mit `;` oder `#` beginnen, werden ignoriert. Nach Änderungen muss die Anwendung neu gestartet werden.

### `[General]`

- `FfmpegPath`: relativer oder absoluter Pfad zu `ffmpeg.exe`
- `Debugging`: `True` zeigt die erzeugten FFmpeg-Befehlszeilen im Statusfeld an

### `[SampleRates]`

Format:

```ini
Anzeigename=SamplerateInHz|Ordnerlabel
```

Beispiel:

```ini
192.0 kHz=192000|192kHz
```

### `[BitDepths]`

Format:

```ini
Anzeigename=Bittiefe|FFmpeg-Sampleformat
```

### `[Mp3SampleRates]`

Der rechte Wert enthält die Samplerate in Hertz.

### `[Mp3Qualities]`

- Reine Zahl, zum Beispiel `0` oder `2`: Verwendung als VBR-Qualität mit `-q:a`
- Wert mit `k`, zum Beispiel `128k`: Verwendung als CBR-Bitrate mit `-b:a`

## Aus dem Quellcode kompilieren

Die Anwendung besteht aus einer einzelnen Visual-Basic-Datei und kann mit dem Compiler des .NET Framework 4.0 gebaut werden.

Beispiel für ein 64-Bit-Windows-System:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\vbc.exe ^
  /target:winexe ^
  /out:FlacTranscoder.exe ^
  /reference:System.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /win32icon:mz-fimo-logo.ico ^
  FlacTranscoder.vb
```

Wenn kein Icon verwendet wird, kann der Parameter `/win32icon` entfernt werden. Je nach System kann statt `Framework64` der Ordner `Framework` erforderlich sein.

## Fehlerbehebung

### FFmpeg wurde nicht gefunden

Prüfe, ob die Datei hier vorhanden ist:

```text
bin\ffmpeg.exe
```

Alternativ kann in der INI ein absoluter Pfad eingetragen oder FFmpeg über den Windows-Systempfad bereitgestellt werden.

### Es werden keine Dateien verarbeitet

Prüfe, ob:

- ein gültiger Quellordner ausgewählt wurde,
- mindestens ein Quellformat aktiviert ist,
- passende `.flac`- oder `.wav`-Dateien vorhanden sind,
- mindestens ein Zielformat aktiviert ist.

### Im Statusfeld erscheint ein FFmpeg-Fehler

Aktiviere für weitere Diagnoseinformationen:

```ini
[General]
Debugging=True
```

Starte das Programm anschließend neu. Prüfe außerdem Schreibrechte, Quelldateien und die verwendete FFmpeg-Version.

### Wo befinden sich die Ausgabedateien?

- Ohne rekursive Verarbeitung: direkt im gewählten Quellordner
- Mit rekursiver Verarbeitung: im jeweiligen Ordner der Quelldateien

## Technische Hinweise

- FLAC wird über den FFmpeg-Codec `flac` erzeugt.
- WAV wird als `pcm_s16le` beziehungsweise `pcm_s24le` ausgegeben.
- MP3 wird mit `libmp3lame` erzeugt.
- Resampling erfolgt über den FFmpeg-Filter `aresample` mit triangular Dithering.
- Metadaten werden, soweit von Eingabe und Ausgabeformat unterstützt, von der Quelle übernommen.
- Vorhandene Ausgabedateien werden durch FFmpeg überschrieben.

## Bekannte Einschränkungen

- Die grafische Oberfläche ist während eines laufenden FFmpeg-Prozesses nur eingeschränkt reaktionsfähig.
- Ein laufender Vorgang kann derzeit nicht über einen separaten Abbrechen-Button beendet werden.
- Die Ergebnisprüfung kontrolliert, ob erwartete Dateien existieren, führt jedoch keine inhaltliche Audioanalyse durch.
- Exotische oder ungültige INI-Werte können zu FFmpeg-Fehlern führen.

## Projektstatus

Aktuelle Version: **1.5.0.0**

Das Projekt ist als kompaktes, portables Windows-Werkzeug ausgelegt. Beiträge, Fehlerberichte und Verbesserungsvorschläge können über die Issues und Pull Requests des GitHub-Repositories eingereicht werden.

## Lizenz

Dieses Projekt steht unter der **GNU General Public License v3.0 (GPL-3.0)**.

Du darfst den Quellcode:

- nutzen
- studieren
- verändern
- weitergeben

gemäß den Bedingungen der GPL-3.0.

Eine Kopie der Lizenz befindet sich in der Datei `LICENSE`.

Weitere Informationen:
https://www.gnu.org/licenses/gpl-3.0.html

## Hinweis zur Entstehung

Der Quellcode dieses Projekts wurde zu **99,9 % mit künstlicher Intelligenz generiert** und anschließend **durch menschliche Tests, Validierung und praktische Nutzung überprüft**.

KI wurde als Entwicklungswerkzeug eingesetzt. Die fachliche Bewertung, Funktionsprüfung und Freigabe erfolgte durch einen Menschen.
