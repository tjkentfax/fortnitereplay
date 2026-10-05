const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const { execFileSync } = require('child_process');

const ROOT = __dirname;
const DECODER_PROJECT = path.join(ROOT, 'decoder', 'ReplayExport.csproj');
const DECODER_DLL = path.join(ROOT, 'decoder', 'bin', 'Release', 'net10.0', 'ReplayExport.ReplayReader.dll');

function runDotnet(args) {
  return execFileSync('dotnet', args, {
    cwd: ROOT,
    encoding: 'utf8',
    windowsHide: true,
    maxBuffer: 256 * 1024 * 1024,
  });
}

function ensureDecoder() {
  if (fs.existsSync(DECODER_DLL)) return;
  try {
    runDotnet(['build', DECODER_PROJECT, '-c', 'Release', '--nologo']);
  } catch (err) {
    const detail = String(err?.stderr || err?.stdout || err?.message || err);
    throw new Error('The local decoder is not built. Install the .NET 10 SDK and run "npm run build-decoder".\n\n' + detail);
  }
  if (!fs.existsSync(DECODER_DLL)) throw new Error('Decoder build completed without producing the expected DLL.');
}

function parseReplay(filePath) {
  if (!filePath) throw new Error('No replay selected.');
  if (!fs.existsSync(filePath)) throw new Error(`Replay file not found: ${filePath}`);
  if (!/\.replay$/i.test(filePath)) throw new Error('Please select a Fortnite .replay file.');
  ensureDecoder();
  let stdout;
  try {
    stdout = runDotnet([DECODER_DLL, filePath]);
  } catch (err) {
    const detail = String(err?.stderr || err?.stdout || err?.message || err);
    throw new Error(`The native replay decoder could not parse this replay.\n\n${detail}`);
  }
  try { return JSON.parse(stdout); }
  catch { throw new Error(`The decoder returned invalid JSON.\n\n${stdout.slice(0, 4000)}`); }
}

ipcMain.handle('parse-replay', (_event, filePath) => {
  try { return { ok: true, data: parseReplay(filePath) }; }
  catch (err) { return { ok: false, error: err?.message || String(err) }; }
});

ipcMain.handle('open-replay', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile'],
    filters: [{ name: 'Fortnite Replay', extensions: ['replay'] }],
  });
  return result.canceled ? null : result.filePaths[0];
});

function createWindow() {
  const win = new BrowserWindow({
    width: 1500,
    height: 950,
    minWidth: 1100,
    minHeight: 700,
    backgroundColor: '#080b10',
    webPreferences: {
      preload: path.join(ROOT, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });
  win.loadFile(path.join(ROOT, 'index.html'));
}

app.whenReady().then(() => {
  createWindow();
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});
