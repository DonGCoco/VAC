#!/usr/bin/env python3
"""
VAC experimenter monitor.

Standard-library only:
- actively discovers the Magic Leap 2 on the local network
- receives live calibration state from the headset over UDP
- serves a local browser UI for the experimenter
- sends C1/C2/C3, LOCK and RESCAN commands back to the headset

No laptop IP is stored in the Unity project.
START intentionally remains inside the headset for the participant.
"""

import argparse
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
DISCOVERY_INTERVAL_SECONDS = 0.75
DISCOVERY_MESSAGE = f"VAC_MONITOR_DISCOVER:{STATUS_PORT}".encode("utf-8")

state_lock = threading.Lock()
latest_status = {}
headset_address = None
last_seen = 0.0
manual_headset_ip = None

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
  <div id="connection">Searching for headset…</div>
  <div class="row">
    <input id="participantId" placeholder="Participant ID (e.g. P001)"
      style="font-size:18px;padding:10px 12px;border:1px solid #888;border-radius:9px;min-width:230px;">
    <select id="groupOverride"
      style="font-size:18px;padding:10px 12px;border:1px solid #888;border-radius:9px;background:white;">
      <option value="AUTO">Group: Auto</option>
      <option value="G1">Override G1</option>
      <option value="G2">Override G2</option>
      <option value="G3">Override G3</option>
    </select>
    <button onclick="assignParticipant()">ASSIGN</button>
  </div>
  <div id="participant" style="margin-top:12px;">Participant —</div>
  <div id="assignment">Group / order —</div>
  <div class="row">
    <button id="c1" onclick="cmd('C1')">C1 · 0.80 m</button>
    <button id="c2" onclick="cmd('C2')">C2 · 1.00 m</button>
    <button id="c3" onclick="cmd('C3')">C3 · 1.50 m</button>
  </div>
  <div class="small">C1/C2/C3 buttons remain as a development fallback while M6 is being integrated.</div>
</div>

<div class="card">
  <div id="phase" class="state">FLOW —</div>
  <div id="block">Block — · Sequence —</div>
  <div id="flowInstruction" style="font-size:18px;margin-top:10px;">Assign a participant to begin.</div>
  <div id="recovery" class="small" style="margin-top:8px;"></div>
  <div class="row">
    <button id="flowAction" onclick="flowAction()" disabled>NEXT</button>
  </div>
</div>

<div class="card">
  <div id="condition">Condition —</div>
  <div class="actual" id="actual">Actual —</div>
  <div id="target">Target —</div>
  <div class="state" id="state">WAITING</div>
  <div id="instruction">Searching for the Magic Leap 2 on the local network.</div>
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
let currentFlowCommand = null;

async function cmd(command) {
  await fetch('/command', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({command})
  });
}

async function assignParticipant() {
  const id = document.getElementById('participantId').value.trim();
  const group = document.getElementById('groupOverride').value;
  if (!id) return;
  await cmd('PARTICIPANT:' + id + ':' + group);
}

async function flowAction() {
  if (currentFlowCommand)
    await cmd(currentFlowCommand);
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
      connected ? ('Headset connected · ' + s.headset_ip) : 'Searching for headset…';

    if (connected && s.data) {
      const d = s.data;
      document.getElementById('participant').textContent =
        d.participant_id ? ('Participant ' + d.participant_id) : 'Participant —';
      document.getElementById('assignment').textContent =
        d.group ? (d.group + ' · ' + d.condition_order) : 'Group / order —';
      document.getElementById('phase').textContent =
        'FLOW · ' + String(d.phase || 'Idle');
      document.getElementById('block').textContent =
        (d.block > 0 ? ('Block ' + d.block) : 'Warm-up') +
        ' · Sequence ' + (d.sequence || '—');

      let flowInstruction = '';
      let flowLabel = 'NEXT';
      currentFlowCommand = null;

      if (d.phase === 'WarmupCalibration') {
        flowInstruction = 'Warm-up: calibrate at the configured training condition, LOCK, then participant presses START. Training uses sequence T.';
      } else if (d.phase === 'WarmupTetris') {
        flowInstruction = 'Warm-up Tetris is running. Participant practices move / rotate / hard drop.';
      } else if (d.phase === 'WarmupDepthPractice') {
        flowInstruction = 'Warm-up depth practice: choose which target is farther with ray + trigger.';
      } else if (d.phase === 'PreBlockQuestionnaire') {
        flowInstruction = 'Complete the external Pre-SSQ for this block, then continue to calibration.';
        flowLabel = 'PRE-SSQ DONE → CALIBRATION';
        currentFlowCommand = 'CONTINUE';
      } else if (d.phase === 'BlockCalibration') {
        flowInstruction = 'Move the physical board to the target distance, wait for READY, then LOCK. Participant presses START.';
      } else if (d.phase === 'FormalTetris') {
        flowInstruction = 'Formal Tetris is running.';
      } else if (d.phase === 'FormalDepth') {
        flowInstruction = 'Formal depth judgement is running.';
      } else if (d.phase === 'PostBlockQuestionnaire') {
        flowInstruction = 'Complete external Post-SSQ + QoE, then continue.';
        flowLabel = 'POST-SSQ + QoE DONE';
        currentFlowCommand = 'CONTINUE';
      } else if (d.phase === 'Recovery') {
        const remain = Math.max(0, Number(d.recovery_remaining_s || 0));
        flowInstruction = remain > 0
          ? 'Recovery in progress. Minimum interval has not finished yet.'
          : 'Minimum recovery finished. Continue only if the participant has recovered sufficiently.';
        flowLabel = remain > 0 ? 'RECOVERY — WAIT' : 'RECOVERY COMPLETE';
        if (d.recovery_ready) currentFlowCommand = 'RECOVERY_DONE';
      } else if (d.phase === 'Complete') {
        flowInstruction = 'Experiment complete. Save/check the session data before the participant leaves.';
        flowLabel = 'COMPLETE';
      } else {
        flowInstruction = 'Assign a participant to begin the formal flow.';
      }

      document.getElementById('flowInstruction').textContent = flowInstruction;
      document.getElementById('flowAction').textContent = flowLabel;
      document.getElementById('flowAction').disabled = !currentFlowCommand;
      document.getElementById('recovery').textContent =
        d.phase === 'Recovery'
          ? ('Minimum recovery remaining: ' + Math.ceil(Math.max(0, Number(d.recovery_remaining_s || 0))) + ' s')
          : '';

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
      const disableConditions = !!d.tetris_running || !!d.participant_id;
      document.getElementById('c1').disabled = disableConditions;
      document.getElementById('c2').disabled = disableConditions;
      document.getElementById('c3').disabled = disableConditions;
      document.getElementById('rescan').disabled = !!d.tetris_running;
    } else {
      document.getElementById('lock').disabled = true;
      document.getElementById('instruction').textContent =
        'Searching automatically. Headset and laptop must be on a network that allows local device communication.';
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

def discovery_sender():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

    while True:
        targets = [("255.255.255.255", COMMAND_PORT)]

        if manual_headset_ip:
            targets.append((manual_headset_ip, COMMAND_PORT))

        for target in targets:
            try:
                sock.sendto(DISCOVERY_MESSAGE, target)
            except OSError:
                pass

        time.sleep(DISCOVERY_INTERVAL_SECONDS)

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

        is_participant_command = command.startswith("PARTICIPANT:")
        if command not in {"C1", "C2", "C3", "LOCK", "RESCAN", "CONTINUE", "RECOVERY_DONE"} and not is_participant_command:
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

def parse_args():
    parser = argparse.ArgumentParser(description="VAC experimenter monitor")
    parser.add_argument(
        "--headset-ip",
        help=(
            "Optional fallback for networks that block broadcast discovery. "
            "No Unity rebuild is required."
        ),
    )
    return parser.parse_args()

def main():
    global manual_headset_ip

    args = parse_args()
    manual_headset_ip = args.headset_ip

    threading.Thread(target=udp_receiver, daemon=True).start()
    threading.Thread(target=discovery_sender, daemon=True).start()

    url = f"http://{HTTP_HOST}:{HTTP_PORT}"
    print(f"VAC Experimenter Monitor: {url}")
    print(f"Listening for headset status on UDP {STATUS_PORT}.")
    print(f"Automatically discovering headset on UDP {COMMAND_PORT}.")
    if manual_headset_ip:
        print(f"Fallback discovery target: {manual_headset_ip}")
    print("Headset and laptop must be on a network that allows local device communication.")
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
