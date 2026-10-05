# FortniteReplay local setup (Windows)

The app now uses a local .NET replay decoder instead of the older JavaScript parser. This is important for newer Fortnite replay builds: the FortNet project documents that its 2026 replay workflow uses `FortniteReplayReader` with fixes around replay-frame timing and telemetry. The underlying reader is packaged as `FortniteReplayReader` 3.1.0.

## Requirements

- Windows 10/11
- Node.js LTS
- Git
- .NET 10 SDK

Install the .NET 10 SDK from the official Microsoft .NET download page if `dotnet --version` does not report 10.x.

## First run

From the repository folder:

```bat
npm install
npm run build-decoder
npm start
```

The first decoder build downloads the `FortniteReplayReader` NuGet dependency. After that, importing a `.replay` file runs the compiled decoder locally.

## Importing a replay

1. Start the app with `npm start`.
2. Click **Load Replay**.
3. Select your `.replay` file.
4. The app runs the decoder on the local PC.
5. The dashboard is populated with the decoded match metadata, players, eliminations, positions, health/shield telemetry when available, weapons, teams and safe-zone data.

No replay is uploaded to a web server.

## If decoding fails

Run the decoder directly so the full parser error is visible:

```bat
dotnet decoder\bin\Release\net10.0\ReplayExport.ReplayReader.dll "C:\path\to\Killing Spree.replay"
```

Copy the error output if the replay still fails. Parser compatibility can depend on the Fortnite build and whether the recording was finalized.
