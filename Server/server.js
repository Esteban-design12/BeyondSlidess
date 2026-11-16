// Beyond Slides — servidor puente
// Unity (presentador)  <-- WebSocket -->  este servidor  <-- WebSocket -->  web de la audiencia
//
// Uso:   npm install   (solo la primera vez)
//        npm start
// La audiencia abre en el celular la dirección que se imprime en la consola.

const http = require('http');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { WebSocketServer } = require('ws');

const PORT = Number(process.env.PORT) || 8080;
const PING_COOLDOWN_MS = 400; // anti-spam: máximo un ping cada 400 ms por persona

// ---------- Página web ----------
const server = http.createServer((req, res) => {
  const url = req.url.split('?')[0];
  if (url === '/' || url === '/index.html') {
    fs.readFile(path.join(__dirname, 'public', 'index.html'), (err, data) => {
      if (err) { res.writeHead(500); res.end('No se encontró public/index.html'); return; }
      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
      res.end(data);
    });
  } else {
    res.writeHead(404); res.end('No encontrado');
  }
});

// ---------- Estado ----------
const wss = new WebSocketServer({ server });
const presenters = new Set();
const audience = new Set();
let state = { type: 'state', station: -1, title: '', targets: [], labels: [] };
let counts = {};      // id del objetivo -> número de pings
let ended = false;

function lanAddress() {
  const nets = os.networkInterfaces();
  for (const name of Object.keys(nets)) {
    for (const net of nets[name]) {
      if (net.family === 'IPv4' && !net.internal) return net.address;
    }
  }
  return 'localhost';
}
const audienceUrl = `http://${lanAddress()}:${PORT}`;

function send(ws, obj) {
  if (ws.readyState === 1) ws.send(JSON.stringify(obj));
}
function broadcast(set, obj) {
  for (const ws of set) send(ws, obj);
}
function sendInfo() {
  broadcast(presenters, { type: 'info', url: audienceUrl, clients: audience.size });
}
function stateForAudience() {
  return { ...state, counts, ended, clients: audience.size };
}

// ---------- Conexiones ----------
wss.on('connection', (ws) => {
  ws.role = 'audience';
  ws.lastPing = 0;
  audience.add(ws);
  send(ws, stateForAudience());
  sendInfo();

  ws.on('message', (raw) => {
    let msg;
    try { msg = JSON.parse(raw.toString()); } catch { return; }
    if (!msg || typeof msg.type !== 'string') return;

    switch (msg.type) {
      case 'hello':
        if (msg.role === 'presenter') {
          audience.delete(ws);
          presenters.add(ws);
          ws.role = 'presenter';
          ended = false;
          console.log('Unity (presentador) conectado');
          sendInfo();
        }
        break;

      case 'state': // Unity avisa en qué estación está
        if (ws.role !== 'presenter') return;
        state = {
          type: 'state',
          station: Number(msg.station) || 0,
          title: String(msg.title || ''),
          targets: Array.isArray(msg.targets) ? msg.targets.map(String) : [],
          labels: Array.isArray(msg.labels) ? msg.labels.map(String) : [],
        };
        console.log(`Estación actual: ${state.title || '(ninguna)'}`);
        broadcast(audience, stateForAudience());
        break;

      case 'ping': { // un asistente reacciona
        if (ws.role !== 'audience' || ended) return;
        const target = String(msg.target || '');
        if (!state.targets.includes(target)) return;
        const now = Date.now();
        if (now - ws.lastPing < PING_COOLDOWN_MS) return;
        ws.lastPing = now;
        counts[target] = (counts[target] || 0) + 1;
        broadcast(presenters, { type: 'ping', target, count: counts[target] });
        broadcast(audience, { type: 'count', target, count: counts[target] });
        break;
      }

      case 'end': // Unity cerró la presentación
        if (ws.role !== 'presenter') return;
        ended = true;
        broadcast(audience, { type: 'end' });
        console.log('Presentación cerrada');
        break;

      case 'reset':
        if (ws.role !== 'presenter') return;
        counts = {};
        ended = false;
        broadcast(audience, stateForAudience());
        break;
    }
  });

  ws.on('close', () => {
    if (presenters.delete(ws)) {
      console.log('Unity (presentador) desconectado');
      counts = {};       // una nueva sesión de Play empieza de cero
      ended = false;
      state = { type: 'state', station: -1, title: '', targets: [], labels: [] };
      broadcast(audience, stateForAudience());
    }
    audience.delete(ws);
    sendInfo();
  });

  ws.on('error', () => {});
});

server.listen(PORT, () => {
  console.log('==============================================');
  console.log(' Beyond Slides — servidor de audiencia listo');
  console.log(` Audiencia (celulares, misma WiFi): ${audienceUrl}`);
  console.log(` En este mismo PC:                  http://localhost:${PORT}`);
  console.log(` Unity (campo Server Url):          ws://localhost:${PORT}`);
  console.log(`   ...o desde un Quest:             ws://${lanAddress()}:${PORT}`);
  const all = [];
  const nets = os.networkInterfaces();
  for (const name of Object.keys(nets))
    for (const net of nets[name])
      if (net.family === 'IPv4' && !net.internal) all.push(`${net.address} (${name})`);
  if (all.length > 1) {
    console.log(' Este PC tiene varias direcciones de red; si la de arriba no abre');
    console.log(' en el celular, prueba con otra: ' + all.join(', '));
  }
  console.log('==============================================');
});
