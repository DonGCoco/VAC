#!/usr/bin/env python3
"""
VAC experimenter monitor.

Standard-library only:
- receives live calibration state from the Magic Leap 2 over UDP
- serves a local browser UI for the experimenter
- sends C1/C2/C3, LOCK and RESCAN commands back to the headset

START intentionally remains inside the headset for the participant.
"""

import json
import socket
import threading
import time
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

STATUS_PORT = 45555
COMMAND_PORT = 45556
HTTP_HOST = "127.0.0.1"
HTTP_PORT = 8765

state_lock = threading.Lock()
latest_status = {}
headset_address = None
last_seen = 0.0

PAGE = r"""<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>VAC Experimenter Monitor</title>
<style>
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; margin: 28px; max-width: 760px; }
h1 { margin-bottom: 4px; }
.sub { opacity: .7; margin-bottom: 22px; }
.card { border: 1px solid #aaa; border-radius: 12px; padding: 18px; margin: 14px 0; }
.actual { font-size: 52px; font-weight: 650; margin: 4px 0; }
.state { font-size: 30px; font-weight: 650; margin: 8px 0; }
.row { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 12px; }
button { font-size: 18px; padding: 12px 20px; border-radius: 9px; border: 1px solid #888; background: white; }
button:disabled { opacity: .35; }
.small { font-size: 14px; opacity: .72; }
#instruction { font-size: 18px; margin-top: 10px; }
</style>
</head>
<body>
<h1>VAC Experimenter Monitor</h1>
<div class="sub">Participant wears the headset. Experimenter watches this screen and gives verbal distance instructions.</div>

<div class="card">
  <div id="connection">Waiting for headset…</div>
  <div class="row">
    <button id="c1" onclick="cmd('C1')">C1 · 0.80 m</button>
    <button id="c2" onclick="cmd('C2')">C2 · 1.00 m</button>
    <button id="c3" onclick="cmd('C3')">C3 · 1.50 m</button>
  </div>
</div>

<div class="card">
  <div id="condition">Condition —</div>
  <div class="actual" id="actual">Actual —</div>
  <div id="target">Target —</div>
  <div class="state" id="state">WAITING</div>
  <div id="instruction">Ask participant to hold still while the marker is acquired.</div>
</div>

<div class="card">
  <div id="anchor">Anchor —</div>
  <div id="game">Tetris —</div>
  <div class="row">
    <button id="lock" onclick="cmd('LOCK')" disabled>LOCK</button>
    <button id="rescan" onclick="cmd('RESCAN')">RESCAN</button>
  </div>
  <div class="small">There is intentionally no START button here. After LOCKED, START appears in the participant's headset.</div>
</div>

<script>
async function cmd(command) {
  await fetch('/command', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({command})
  });
}

function metres(v) {
  return (typeof v === 'number' && isFinite(v)) ? v.toFixed(3) + ' m' : '—';
}

async function refresh() {
  try {
    const r = await fetch('/status', {cache: 'no-store'});
    const s = await r.json();

    const connected = !!s.connected;
    document.getElementById('connection').textContent =
      connected ? ('Headset connected · ' + s.headset_ip) : 'Waiting for headset…';

    if (connected && s.data) {
      const d = s.data;
      document.getElementById('condition').textContent = 'Condition ' + d.condition;
      document.getElementById('actual').textContent = 'Actual ' + metres(d.actual_m);
      document.getElementById('target').textContent = 'Target ' + metres(d.target_m);
      document.getElementById('state').textContent = String(d.state || '—').toUpperCase();

      const anchor = d.anchor_tracking ? 'TRACKING' : (d.board_locked ? 'ANCHORING' : 'UNLOCKED');
      document.getElementById('anchor').textContent = 'Spatial anchor · ' + anchor;
      document.getElementById('game').textContent =
        'Tetris · ' + (d.tetris_running ? 'RUNNING' : 'NOT RUNNING');

      let instruction = '';
      if (d.state === 'TooClose') instruction = 'Tell participant: move slightly farther away.';
      else if (d.state === 'TooFar') instruction = 'Tell participant: move slightly closer.';
      else if (d.state === 'Ready') instruction = 'READY — tell participant: do not move. Then click LOCK.';
      else if (d.state === 'Locking') instruction = 'Hold still — spatial anchor is locking…';
      else if (d.state === 'Locked') instruction = 'LOCKED — START is now available inside the headset.';
      else if (d.state === 'AnchorError') instruction = 'Anchor failed. Click RESCAN and repeat calibration.';
      else instruction = 'Keep the QR visible and ask participant to hold position.';
      document.getElementById('instruction').textContent = instruction;

      document.getElementById('lock').disabled = d.state !== 'Ready' || d.tetris_running;
      const disableConditions = !!d.tetris_running;
      document.getElementById('c1').disabled = disableConditions;
      document.getElementById('c2').disabled = disableConditions;
      document.getElementById('c3').disabled = disableConditions;
      document.getElementById('rescan').disabled = !!d.tetris_running;
    }
  } catch (e) {}
  setTimeout(refresh, 100);
}
refresh();
</script>
</body>
</html>
"""

def udp_receiver():
    global latest_status, headset_address, last_seen
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(("", STATUS_PORT))

    while True:
        payload, address = sock.recvfrom(65535)
        try:
            data = json.loads(payload.decode("utf-8"))
        except Exception:
            continue

        with state_lock:
            latest_status = data
            headset_address = address[0]
            last_seen = time.time()

def send_command(command):
    with state_lock:
        address = headset_address

    if not address:
        return False

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        sock.sendto(command.encode("utf-8"), (address, COMMAND_PORT))
    finally:
        sock.close()

    return True

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == "/":
            body = PAGE.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return

        if self.path == "/status":
            with state_lock:
                connected = headset_address is not None and (time.time() - last_seen) < 2.0
                body_obj = {
                    "connected": connected,
                    "headset_ip": headset_address,
                    "data": latest_status if connected else None
                }

            body = json.dumps(body_obj).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return

        self.send_error(404)

    def do_POST(self):
        if self.path != "/command":
            self.send_error(404)
            return

        length = int(self.headers.get("Content-Length", "0"))
        try:
            request = json.loads(self.rfile.read(length).decode("utf-8"))
            command = str(request.get("command", "")).upper()
        except Exception:
            self.send_error(400)
            return

        if command not in {"C1", "C2", "C3", "LOCK", "RESCAN"}:
            self.send_error(400)
            return

        ok = send_command(command)
        body = json.dumps({"ok": ok}).encode("utf-8")
        self.send_response(200 if ok else 503)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, format, *args):
        pass

def main():
    threading.Thread(target=udp_receiver, daemon=True).start()

    url = f"http://{HTTP_HOST}:{HTTP_PORT}"
    print(f"VAC Experimenter Monitor: {url}")
    print(f"Listening for headset status on UDP {STATUS_PORT}.")
    print("Headset and laptop must be on the same network.")
    print("Press Ctrl+C to stop.")

    threading.Timer(0.5, lambda: webbrowser.open(url)).start()

    server = ThreadingHTTPServer((HTTP_HOST, HTTP_PORT), Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()

if __name__ == "__main__":
    main()
