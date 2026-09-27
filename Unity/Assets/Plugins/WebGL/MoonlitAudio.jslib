mergeInto(LibraryManager.library, {
  MoonlitAudioConfigure: function(playing, paused, sound, volume, speed) { window.moonlitAudio.configure(playing, paused, sound, volume, speed); },
  MoonlitAudioCollect: function(count, booster, bonus) { window.moonlitAudio.collect(count, booster, bonus); },
  MoonlitAudioReset: function() { window.moonlitAudio.reset(); },
  MoonlitAudioDispose: function() { window.moonlitAudio.dispose(); }
});
