GSR Toolbox
===========

Run:
    Double-click GSRToolbox.exe

The window has three tabs:
  1. Stream      - start/stop Shimmer GSR + PPG + Accel streaming to LSL.
                   Default port COM7, default rate 128 Hz. Pair the Shimmer
                   in Windows Bluetooth settings first, then enter the COM
                   port the OS assigned.
  2. Live Chart  - rolling 30 s plot of any Shimmer_* LSL streams on the
                   network. Click Connect after starting the streamer (or
                   after another machine starts publishing on the LAN).
  3. XDF -> CSV  - pick an .xdf file recorded by LabRecorder; per-stream CSVs
                   (gsr, ppg, accel, events, headpose) plus summary.txt are
                   written next to the source file.

Notes:
  - Keep the whole GSRToolbox folder together; GSRToolbox.exe needs the
    sibling _internal/ folder (it contains Qt, lsl.dll, etc.).
  - Windows may warn "unknown publisher" the first time - the exe is not
    code-signed. Click "More info" > "Run anyway".
  - For Shimmer pairing/PIN, see the Shimmer device manual.
