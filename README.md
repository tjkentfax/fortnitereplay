# FortniteReplay Local

Desktop build of the Fortnite replay dashboard. Replay files are parsed locally on your PC; this app does not upload replay files to a server.

## Windows

1. Install Node.js 18+.
2. Open a terminal in this repository.
3. Run `npm install`.
4. Run `npm start`.
5. Click **Load Replay** and choose a `.replay` file.

The app uses the open-source `fortnite-replay-parser` / xNocken replay-reader parser locally. Fortnite replay formats can change between game versions, so a replay from a newer build may require a newer parser.

## Files

- `index.html` — dashboard UI
- `main.js` — Electron desktop process and local replay decoder
- `preload.js` — secure IPC bridge
- `package.json` — local dependencies and start command
