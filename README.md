# Import Dialog App

Minimal WPF application for the `ImportDialog` window in this repository.

## Requirements

- Bash shell
- .NET SDK 8.0+
- Windows to show the WPF window

## Environment setup

Run:

```bash
./setup-env.sh
```

The script installs .NET SDK 8 into `~/.dotnet` (if missing), adds it to `PATH`,
and runs `dotnet restore`.

## Build and run

Run:

```bash
./run-app.sh
```

On Windows, this starts the WPF window.
On Linux/macOS, WPF is not supported at runtime. You can still validate compilation with:

```bash
./run-app.sh --build-only
```
