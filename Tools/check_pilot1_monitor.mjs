// Run the Monitor's real embedded JavaScript with synthetic status packets.
import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('./ExperimenterMonitor/experimenter_monitor.py', import.meta.url), 'utf8');
const script = source.match(/<script>([\s\S]*?)<\/script>/)[1];
const elements = new Map();
const get = id => {
  if (!elements.has(id)) elements.set(id, {textContent: '', disabled: false, style: {}, dataset: {}});
  return elements.get(id);
};
let data = {};
const context = vm.createContext({document: {getElementById: get}, console,
  fetch: async () => ({json: async () => ({connected: true, headset_ip: 'synthetic', data})}),
  setTimeout: () => 0});
// Avoid an implicit first refresh so every packet is explicitly controlled.
vm.runInContext(script.replace(/refresh\(\);\s*$/, ''), context);
async function status(packet) {
  data = {state: 'Locked', phase: 'BlockCalibration', participant_id: 'SYNTHETIC',
    condition: 'C1', target_m: .8, actual_m: .8, anchor_tracking: true, ...packet};
  await vm.runInContext('refresh()', context);
}
await status({tetris_state: 'WaitingParticipant'});
assert.match(get('game').textContent, /WAITING FOR PARTICIPANT START/);
assert.equal(get('flowAction').disabled, true);
for (const n of [3, 2, 1]) {
  await status({phase: 'FormalCountdown', tetris_countdown: true, countdown_number: n, tetris_remaining_s: 900});
  assert.match(get('game').textContent, new RegExp('COUNTDOWN ' + n));
  assert.match(get('flowInstruction').textContent, /timer and gameplay have not started/);
  assert.equal(get('rescan').disabled, true);
  assert.equal(get('flowAction').disabled, true);
}
await status({phase: 'FormalTetris', tetris_running: true, tetris_remaining_s: 898.5});
assert.match(get('game').textContent, /RUNNING.*898\.5 s remaining/);
assert.equal(get('rescan').disabled, true);
await status({phase: 'PreBlockQuestionnaire'});
assert.equal(get('flowAction').dataset.command, 'CONTINUE');
await status({phase: 'Recovery', recovery_remaining_s: 60, recovery_ready: false});
assert.equal(get('flowAction').disabled, true);
await status({phase: 'Recovery', recovery_remaining_s: 0, recovery_ready: true});
assert.equal(get('flowAction').dataset.command, 'RECOVERY_DONE');
console.log('PASS: actual Monitor JS waiting/3-2-1/running/countdown locks/questionnaire/recovery with synthetic packets.');
