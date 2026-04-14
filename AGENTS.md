# AGENTS.md

## Cursor Cloud specific instructions

### Overview
This repository contains a C# (.NET/WPF) plugin for the NPFGEO geophysics software that provides import/export for well logging data formats: LIS (LIS79), DLIS (RP66 V1), and Interval (Elicom). There are no `.csproj`/`.sln` build files — the C# code is part of a larger proprietary Visual Studio solution requiring Windows, WPF, and external assemblies (`NPFGEO`, `Elicom`) that are **not included** in this repo.

### What CAN run on Linux (Cloud Agent VM)
- **Python helper script** at `LIS/DLISIO/main.py` uses the `dlisio` library (Python 3.12+) for testing LIS/DLIS import/export. Dependencies: `dlisio`, `numpy`.
- The script requires an actual `.lis` or `.dlis` file to process real data. Without sample data files, you can still verify library imports and API availability.

### What CANNOT run on Linux
- The C# source files require Windows + .NET Framework + WPF + proprietary NPFGEO/Elicom assemblies. These cannot be compiled or tested in this environment.
- No lint, build, or test tooling exists in the repository for the C# code.

### Running the Python helper
```bash
cd /workspace/LIS/DLISIO
python3 main.py  # requires a .lis or .dlis file; edit the `file` variable in main.py to point to your data file
```

### Key gotchas
- `dlisio` is installed to user site-packages (`pip3 install --user`). If imports fail, check `pip3 show dlisio`.
- The `main.py` script has a hardcoded filename `АКСТ.lis` — you must change this or provide a matching file.
