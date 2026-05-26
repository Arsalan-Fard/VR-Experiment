GSR Toolbox
===========

Run:
    Double-click GSRToolbox.exe

Single window, three control strips stacked top to bottom:

  Stream  -  Start/stop the Shimmer GSR + PPG + Accel LSL streamer.
             Defaults: port COM7, rate 128 Hz. Pair the Shimmer in
             Windows Bluetooth settings first, then enter the COM port
             the OS assigned.

  XDF -> CSV - Pick an .xdf file recorded by LabRecorder; per-stream
             CSVs (gsr, ppg, accel, eeg, headpose, events) are written
             to a "csv/" subfolder next to the source .xdf. Streams are
             matched by LSL type, so it works regardless of the
             device-specific stream name.

  Live Chart - Click Connect to find Shimmer_* LSL streams on the
             network and plot the last 30 s of GSR / PPG / Accel.

Notes:
  - Keep the whole GSRToolbox folder together; GSRToolbox.exe needs
    the sibling _internal/ folder (Qt, lsl.dll, etc.).
  - Windows may warn "unknown publisher" the first run - the exe is
    not code-signed. Click "More info" > "Run anyway".
