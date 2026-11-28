// Deadzone2D online backend — accounts, friends, chat and matchmaking.
import express from 'express';
import http from 'http';
import crypto from 'crypto';
import fs from 'fs';
import path from 'path';
import { WebSocketServer } from 'ws';

const PORT = Number(process.env.PORT || 3000);
const DATA_FILE = path.join(process.cwd(), 'data', 'server.json');

const BAD_WORDS = [
  'fuck', 'shit', 'bitch', 'asshole', 'cunt', 'nigger', 'faggot', 'damn',
  'merda', 'bosta', 'caralho', 'foda', 'fodido', 'viado', 'puta', 'pilantra',
  'idiota', 'estupido', 'imbecil', 'babaca', 'mierda', 'basura', 'cabron', 'pendejo'
];
const BAD_RE = new RegExp('\\b(' + BAD_WORDS.join('|') + ')s?\\b', 'gi');

function censor(text) {
  return String(text || '').replace(BAD_RE, (m) => m[0].toUpperCase() + '*'.repeat(Math.max(1, m.length - 1)));
}

function loadDb() {
  try {
    if (fs.existsSync(DATA_FILE)) return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
  } catch { }
  return { accounts: [], chat: [], matches: [] };
}
const db = loadDb();
function saveDb() {
  try {
    fs.mkdirSync(path.dirname(DATA_FILE), { recursive: true });
    fs.writeFileSync(DATA_FILE, JSON.stringify(db, null, 2));
  } catch (e) { console.error('save failed', e.message); }
}

function hashPassword(password, salt) {
  return crypto.scryptSync(String(password), salt, 64).toString('hex');
}
function newId() { return 'DZ-' + crypto.randomBytes(4).toString('hex').toUpperCase(); }
function findAccount(value) {
  const v = String(value || '').trim().toLowerCase();
  return db.accounts.find(a => a.email.toLowerCase() === v || a.id.toLowerCase() === v || a.nick.toLowerCase() === v);
}

const sessions = new Map(); // token -> account id
const sockets = new Map();  // ws -> { id, matchId }
let queue = [];

const app = express();
app.use(express.json({ limit: '64kb' }));
app.get('/health', (_req, res) => res.json({ ok: true, version: '4.5.1', players: sockets.size }));

app.post('/api/register', (req, res) => {
  const email = String(req.body.email || '').trim().toLowerCase();
  const password = String(req.body.password || '');
  const nick = String(req.body.nick || '').trim();
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return res.status(400).json({ error: 'Invalid email.' });
  if (password.length < 6) return res.status(400).json({ error: 'Password must be at least 6 characters.' });
  if (nick.length < 3 || nick.length > 24) return res.status(400).json({ error: 'Invalid nick.' });
  if (db.accounts.some(a => a.email.toLowerCase() === email)) return res.status(409).json({ error: 'Email already registered.' });
  if (db.accounts.some(a => a.nick.toLowerCase() === nick.toLowerCase())) return res.status(409).json({ error: 'Nick already in use.' });
  const salt = crypto.randomBytes(16).toString('hex');
  const acc = {
    id: newId(), email, nick,
    displayName: String(req.body.displayName || nick).slice(0, 20),
    salt, passwordHash: hashPassword(password, salt),
    coins: 1500, level: 1, xp: 0, wins: 0, kills: 0,
    friends: [], requests: [], createdAt: Date.now()
  };
  db.accounts.push(acc);
  const token = crypto.randomBytes(24).toString('hex');
  sessions.set(token, acc.id);
  saveDb();
  res.json({ token, profile: publicProfile(acc) });
});

app.post('/api/login', (req, res) => {
  const acc = findAccount(req.body.email);
  if (!acc) return res.status(401).json({ error: 'Account not found.' });
  const hash = hashPassword(req.body.password || '', acc.salt);
  if (!crypto.timingSafeEqual(Buffer.from(hash, 'hex'), Buffer.from(acc.passwordHash, 'hex')))
    return res.status(401).json({ error: 'Invalid password.' });
  const token = crypto.randomBytes(24).toString('hex');
  sessions.set(token, acc.id);
  res.json({ token, profile: publicProfile(acc) });
});

function publicProfile(a) {
  return { id: a.id, email: a.email, nick: a.nick, displayName: a.displayName, coins: a.coins, level: a.level, xp: a.xp };
}
function accountBySocket(ws) {
  const s = sockets.get(ws);
  return s ? db.accounts.find(a => a.id === s.id) : null;
}
function send(ws, type, data) {
  if (ws && ws.readyState === 1) ws.send(JSON.stringify({ type, ...data }));
}
function broadcast(type, data, filter = null) {
  for (const [ws, state] of sockets) if (!filter || filter(ws, state)) send(ws, type, data);
}

const server = http.createServer(app);
const wss = new WebSocketServer({ server, path: '/ws' });

wss.on('connection', (ws, req) => {
  const url = new URL(req.url, 'http://localhost');
  const token = url.searchParams.get('token') || '';
  const id = sessions.get(token);
  if (!id) { ws.close(4001, 'Invalid token'); return; }
  sockets.set(ws, { id, matchId: null, lastState: 0 });
  const acc = db.accounts.find(a => a.id === id);
  send(ws, 'welcome', {
    profile: publicProfile(acc),
    friends: acc.friends.map(fid => {
      const f = db.accounts.find(a => a.id === fid);
      return f ? publicProfile(f) : null;
    }).filter(Boolean),
    requests: acc.requests,
    chat: db.chat.slice(-40),
    queue: queue.length
  });
  broadcast('presence', { players: sockets.size });

  ws.on('message', (buf) => {
    let msg;
    try { msg = JSON.parse(buf.toString()); } catch { return; }
    const me = accountBySocket(ws);
    if (!me) return;

    switch (msg.type) {
      case 'chat': {
        const text = censor(String(msg.text || '').slice(0, 160));
        if (!text.trim()) return;
        const entry = { from: me.id, author: me.nick, text, scope: msg.scope === 'all' ? 'all' : 'team', at: Date.now() };
        db.chat.push(entry);
        while (db.chat.length > 100) db.chat.shift();
        saveDb();
        if (entry.scope === 'all') broadcast('chat', { message: entry });
        else broadcast('chat', { message: entry }, (_s, st) => me.friends.includes(st.id) || st.id === me.id);
        break;
      }
      case 'friend:add': {
        let target = findAccount(msg.value);
        if (!target) {
          // Permite adicionar uma conta local pelo Gmail: cria um perfil ligado ao e-mail.
          const email = String(msg.value || '').trim().toLowerCase();
          if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            let nick = email.split('@')[0].replace(/[^a-zA-Z0-9_.-]/g, '').slice(0, 24) || 'Player';
            while (db.accounts.some(a => a.nick.toLowerCase() === nick.toLowerCase())) nick += '1';
            const salt = crypto.randomBytes(16).toString('hex');
            target = {
              id: newId(), email, nick, displayName: nick.slice(0, 20),
              salt, passwordHash: hashPassword(crypto.randomBytes(24).toString('hex'), salt),
              coins: 1500, level: 1, xp: 0, wins: 0, kills: 0,
              friends: [], requests: [], createdAt: Date.now()
            };
            db.accounts.push(target);
            saveDb();
          }
        }
        if (!target || target.id === me.id) { send(ws, 'error', { message: 'Player not found.' }); return; }
        if (me.friends.includes(target.id)) { send(ws, 'error', { message: 'Already friends.' }); return; }
        if (target.requests.some(r => r.from === me.id)) { send(ws, 'error', { message: 'Request already sent.' }); return; }
        target.requests.push({ from: me.id, nick: me.nick, at: Date.now() });
        saveDb();
        send(ws, 'info', { message: 'Friend request sent.' });
        const tws = [...sockets.entries()].find(([, st]) => st.id === target.id)?.[0];
        if (tws) send(tws, 'friend:request', { from: me.id, nick: me.nick });
        break;
      }
      case 'friend:accept': {
        const rid = String(msg.id || '');
        const req = me.requests.find(r => r.from === rid);
        if (!req) return;
        me.requests = me.requests.filter(r => r.from !== rid);
        if (!me.friends.includes(rid)) me.friends.push(rid);
        const other = db.accounts.find(a => a.id === rid);
        if (other && !other.friends.includes(me.id)) other.friends.push(me.id);
        saveDb();
        send(ws, 'friend:list', { friends: me.friends.map(fid => db.accounts.find(a => a.id === fid)).filter(Boolean).map(publicProfile) });
        break;
      }
      case 'friend:decline': {
        me.requests = me.requests.filter(r => r.from !== String(msg.id || ''));
        saveDb();
        break;
      }
      case 'queue:join': {
        if (queue.some(q => q.id === me.id)) return;
        queue.push({ id: me.id, nick: me.nick, mode: msg.mode || '1v1', teamSize: Number(msg.teamSize || 1), ws, at: Date.now() });
        broadcast('queue', { count: queue.length });
        send(ws, 'info', { message: 'Searching for players...' });
        break;
      }
      case 'queue:leave': {
        queue = queue.filter(q => q.id !== me.id);
        broadcast('queue', { count: queue.length });
        break;
      }
      case 'match:state': {
        const st = sockets.get(ws);
        if (!st?.matchId) return;
        if (Date.now() - st.lastState < 40) return;
        st.lastState = Date.now();
        broadcast('match:state', {
          id: me.id,
          x: Number(msg.x) || 0,
          y: Number(msg.y) || 0,
          angle: Number(msg.angle) || 0,
          weapon: msg.weapon || '',
          firing: !!msg.firing
        }, (_s, other) => other.matchId === st.matchId && other.id !== me.id);
        break;
      }
      case 'match:leave': {
        const st = sockets.get(ws);
        if (st?.matchId) {
          const match = db.matches.find(m => m.id === st.matchId);
          if (match) match.players = match.players.filter(p => p.id !== me.id);
          db.matches = db.matches.filter(m => m.players.length > 0);
          st.matchId = null;
        }
        break;
      }
    }
  });

  ws.on('close', () => {
    sockets.delete(ws);
    queue = queue.filter(q => q.id !== id);
    broadcast('presence', { players: sockets.size });
    broadcast('queue', { count: queue.length });
  });
});

// Simple matchmaking tick: 1v1 needs 2, 2v2 needs 4, royale starts with 2+ after 10s.
setInterval(() => {
  const now = Date.now();
  const groups = {};
  for (const q of queue) (groups[q.mode] ||= []).push(q);
  for (const [mode, list] of Object.entries(groups)) {
    const need = mode === '2v2' ? 4 : 2;
    const ready = list.length >= need && (mode !== 'royale' || list.length >= 2 && now - list[0].at >= 10000);
    if (!ready) continue;
    const players = list.splice(0, mode === '2v2' ? 4 : Math.min(list.length, 8));
    queue = queue.filter(q => !players.includes(q));
    const match = { id: 'M' + Date.now().toString(36), mode, players: players.map((p, i) => ({ id: p.id, nick: p.nick, team: mode === '2v2' ? (i % 2) : i + 1, spawn: i })), at: now };
    db.matches.push(match);
    for (let i = 0; i < players.length; i++) {
      const state = sockets.get(players[i].ws);
      if (state) state.matchId = match.id;
      send(players[i].ws, 'match:start', { match: { ...match, you: players[i].id } });
    }
    broadcast('queue', { count: queue.length });
  }
}, 1000);

server.listen(PORT, '0.0.0.0', () => console.log(`Deadzone2D server listening on 0.0.0.0:${PORT}`));
