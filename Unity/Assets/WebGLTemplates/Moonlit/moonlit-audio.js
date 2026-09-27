// Browser counterpart of RideAudio: the same harmony and pickup notes, using Web Audio.
(() => {
  const beat = .54, chords = [[50,57,62,65,69],[46,53,58,62,65],[53,60,65,69,72],[48,55,60,64,67]];
  let ctx, master, wind, delay, bus, timer, origin = 0, nextBeat = 0, beatIndex = 0;
  let state = { playing: false, paused: false, sound: true, volume: .55, speed: 0 };
  const voices = new Set();
  const chordAt = seconds => chords[Math.floor(Math.max(0, seconds) / (beat * 16)) % 4];
  function note(midi, when, duration, gain, pan = 0) {
    const osc = ctx.createOscillator(), amp = ctx.createGain(), stereo = ctx.createStereoPanner();
    osc.frequency.value = 440 * Math.pow(2, (midi - 69) / 12);
    stereo.pan.value = pan;
    amp.gain.setValueAtTime(0, when);
    amp.gain.linearRampToValueAtTime(gain, when + .025);
    amp.gain.exponentialRampToValueAtTime(.00001, when + duration);
    osc.connect(amp).connect(stereo).connect(bus);
    osc.start(when); osc.stop(when + duration);
    voices.add(osc);
    osc.onended = () => { voices.delete(osc); osc.disconnect(); amp.disconnect(); stereo.disconnect(); };
  }
  function schedule() {
    if (!ctx || ctx.state !== 'running' || !state.playing) return;
    if (origin === 0) origin = ctx.currentTime + .04;
    // A suspended/background tab must never enqueue a backlog of notes.
    if (origin + nextBeat < ctx.currentTime - beat) {
      beatIndex = Math.ceil((ctx.currentTime - origin) / beat); nextBeat = beatIndex * beat;
    }
    while (origin + nextBeat < ctx.currentTime + .12) {
      const chord = chordAt(nextBeat), when = Math.max(ctx.currentTime, origin + nextBeat);
      if (beatIndex % 16 === 0) {
        for (let i = 0; i < 4; i++) note(chord[i], when, 9, .022, (i - 1.5) * .35);
        note(chord[0] - 12, when, 8, .035);
      }
      if (beatIndex % 2 === 0) {
        const pattern = (beatIndex / 2) % 8;
        const index = pattern === 0 || pattern === 5 ? 2 : pattern === 2 || pattern === 6 ? 3 : pattern === 3 ? 1 : 4;
        note(chord[index] + 12, when, 3.8, .045, Math.sin(beatIndex) * .5);
      }
      nextBeat += beat; beatIndex++;
    }
  }
  function updateGain() {
    if (!ctx) return;
    const audible = state.playing && state.sound && !document.hidden;
    master.gain.setTargetAtTime(audible ? state.volume * (state.paused ? .3 : 1) : 0, ctx.currentTime, .06);
    wind.gain.setTargetAtTime(Math.pow(Math.min(state.speed / 20, 1), 2) * .12, ctx.currentTime, .1);
  }
  function unlock() {
    if (!ctx) {
      const Audio = window.AudioContext || window.webkitAudioContext;
      if (!Audio) return;
      ctx = new Audio(); master = ctx.createGain(); master.gain.value = 0; master.connect(ctx.destination);
      bus = ctx.createGain(); bus.connect(master);
      delay = ctx.createDelay(1); delay.delayTime.value = .75;
      const wet = ctx.createGain(), feedback = ctx.createGain(); wet.gain.value = .32; feedback.gain.value = .35;
      bus.connect(delay); delay.connect(wet).connect(master); delay.connect(feedback).connect(delay);
      const buffer = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate), samples = buffer.getChannelData(0);
      let filtered = 0;
      for (let i = 0; i < samples.length; i++) { filtered += (Math.random() * 2 - 1 - filtered) * .035; samples[i] = filtered; }
      const noise = ctx.createBufferSource(); noise.buffer = buffer; noise.loop = true;
      wind = ctx.createGain(); wind.gain.value = 0; noise.connect(wind).connect(master); noise.start();
      timer = setInterval(schedule, 50);
    }
    if (ctx.state === 'suspended') ctx.resume().catch(() => {});
    updateGain();
  }
  window.addEventListener('pointerdown', unlock);
  window.addEventListener('keydown', unlock);
  document.addEventListener('visibilitychange', updateGain);
  window.moonlitAudio = {
    configure(playing, paused, sound, volume, speed) {
      state = { playing: !!playing, paused: !!paused, sound: !!sound, volume: Math.max(0, Math.min(1, volume)), speed: Math.max(0, speed) };
      updateGain();
    },
    collect(count, booster, bonus) {
      if (!ctx || ctx.state !== 'running' || !state.playing || !state.sound) return;
      const length = booster || bonus ? 4 : 2, now = ctx.currentTime + .01;
      for (let i = 0; i < length; i++) {
        const offset = i * beat / 4, chord = chordAt(now - origin + offset);
        const index = (((bonus ? 1 : count) - 1 + i * 2) % 5 + 5) % 5;
        note(chord[index] + 12 + (bonus && i === 3 ? 12 : 0), now + offset, booster || bonus ? 1.4 : .8, i === 0 ? .085 : .045, i % 2 ? .15 : -.15);
      }
    },
    reset() {
      for (const voice of voices) { try { voice.stop(); } catch (_) {} }
      origin = nextBeat = beatIndex = 0;
    },
    dispose() {
      clearInterval(timer); window.removeEventListener('pointerdown', unlock); window.removeEventListener('keydown', unlock);
      document.removeEventListener('visibilitychange', updateGain);
      if (ctx) ctx.close();
    }
  };
})();
