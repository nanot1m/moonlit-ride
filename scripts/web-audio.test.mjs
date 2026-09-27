import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

// Verify the browser bridge without requiring an audio device or autoplay permission.
const events = {}, gains = [], oscillators = [];
let context, scheduler;
const param = () => ({ value: 0, setValueAtTime(v) { this.value = v; }, linearRampToValueAtTime(v) { this.value = v; }, exponentialRampToValueAtTime(v) { this.value = v; }, setTargetAtTime(v) { this.value = v; } });
const node = () => ({ connect(target) { return target; }, disconnect() {} });
class AudioContext {
  constructor() { context = this; this.currentTime = 1; this.sampleRate = 1000; this.state = 'suspended'; this.destination = node(); }
  createGain() { const n = {...node(), gain:param()}; gains.push(n); return n; }
  createDelay() { return {...node(), delayTime:param()}; }
  createStereoPanner() { return {...node(), pan:param()}; }
  createBuffer() { return {getChannelData: () => new Float32Array(2000)}; }
  createBufferSource() { return {...node(), start() {}}; }
  createOscillator() { const n = {...node(), frequency:param(), start(t) { this.started = t; }, stop() { this.stopped = true; }}; oscillators.push(n); return n; }
  resume() { this.state = 'running'; return Promise.resolve(); }
  close() { this.state = 'closed'; }
}
const window = {AudioContext, addEventListener(name, fn) {events[name] = fn;}, removeEventListener(name) {delete events[name];}};
const document = {hidden:false, addEventListener(name, fn) {events[name] = fn;}, removeEventListener(name) {delete events[name];}};
vm.runInNewContext(await readFile('Unity/Assets/WebGLTemplates/Moonlit/moonlit-audio.js', 'utf8'), {window,document,Math,setInterval(fn) {scheduler = fn; return 1;},clearInterval() {scheduler = null;}});
const audio = window.moonlitAudio;
audio.configure(1,0,1,.55,25);
audio.reset(); audio.collect(1,0,0);
assert.equal(context, undefined, 'No AudioContext before a user gesture');
events.pointerdown(); scheduler();
assert.equal(context.state, 'running');
assert.equal(oscillators.length, 6, 'First chord, bass and melody');
assert.equal(gains[0].gain.value, .55);
const before = oscillators.length;
audio.collect(1,0,0);
assert.equal(oscillators.length - before, 2);
assert.equal(oscillators[before].frequency.value, 440 * 2 ** ((62 - 69) / 12), 'Pickup matches chord root one octave up');
audio.configure(1,1,1,.55,0);
assert.equal(gains[0].gain.value, .165, 'Pause softens the score');
audio.configure(1,0,0,.55,25);
assert.equal(gains[0].gain.value, 0, 'Mute controls the complete mix');
audio.configure(1,0,1,.55,25); document.hidden = true; events.visibilitychange();
assert.equal(gains[0].gain.value, 0, 'Background tabs are silent');
context.currentTime = 1000;
const count = oscillators.length; scheduler();
assert.ok(oscillators.length - count <= 6, 'No backlog after a suspended tab');
audio.reset(); assert.ok(oscillators.every(o => o.stopped));
audio.dispose(); assert.equal(context.state, 'closed'); assert.equal(scheduler, null);
console.log('Web audio checks passed: autoplay, harmony, pause, mute, visibility, scheduling, reset and disposal.');
