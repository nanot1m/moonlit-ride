# Moonlit Ride

Read HANDOFF.md and README.md before modifying the game.
For the Unity migration, also read Unity/README.md. Use Unity 6000.3.17f1. Run MoonlitRide.Editor.PortChecks.Run and build Windows through MoonlitRide.Editor.ProjectSetup.BuildWindows after C# changes. Keep Unity .meta files and ProjectSettings under version control; never commit Library, Temp, UserSettings, or Builds. Browser build instructions below apply when browser sources change.
Use Node.js 22+ and npm ci. npm start launches the local game at http://127.0.0.1:8765.
Run npm test for simulation and layout regression checks. Run npm run build:pages after browser source changes; this regenerates bundle.js and docs/classic/ without overwriting the Unity edition. For Unity web publication run MoonlitRide.Editor.ProjectSetup.BuildWebGL with -buildTarget WebGL, then npm run package:unity-pages; commit docs/ and Unity sources/settings/meta files.
Keep the existing relaxed riding feel, stable camera horizon, and gentle steering. Preserve mute, volume, touch controls, and reduced-motion support.
GitHub Pages serves main:/docs. Pushing main publishes changes to the public game.
Do not put credentials, local machine files, or chat transcripts in the repository.
