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
  try {
    const stdout = runDotnet([DECODER_DLL, filePath]);
    return JSON.parse(stdout);
  } catch (err) {
    if (err instanceof SyntaxError) throw new Error('The decoder returned invalid JSON.');
    const detail = String(err?.stderr || err?.stdout || err?.message || err);
    throw new Error(`The native replay decoder could not parse this replay.\n\n${detail}`);
  }
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

function attachImporter(win) {
  const script = `(()=>{
    if(window.__nativeImporterInstalled)return;
    window.__nativeImporterInstalled=true;
    const input=document.querySelector('#file');
    if(!input)return;
    input.addEventListener('change',async(ev)=>{
      const file=ev.target.files&&ev.target.files[0];
      if(!file||!window.replayAPI)return;
      const name=document.querySelector('#fileName'); if(name)name.textContent=file.name;
      if(!/\\.replay$/i.test(file.name)){ if(typeof importFile==='function') return importFile(file); return; }
      try{
        if(typeof notice==='function')notice('Decoding replay','Reading the local Fortnite replay. This can take a little while on the first import.');
        const result=await window.replayAPI.parseFile(file.path);
        if(!result||!result.ok)throw new Error(result?.error||'Decoder failed.');
        if(typeof setData!=='function')throw new Error('Dashboard importer is unavailable.');
        setData(result.data,file.name);
      }catch(err){
        if(typeof notice==='function')notice('Replay decode failed',err.message||String(err));
        else alert('Replay decode failed\\n\\n'+(err.message||err));
      }
    },true);
  })()`;
  win.webContents.executeJavaScript(script).catch(()=>{});
}

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
  win.webContents.once('did-finish-load', () => attachImporter(win));
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
