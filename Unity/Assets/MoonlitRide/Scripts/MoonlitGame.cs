using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonlitRide
{
    [DefaultExecutionOrder(-100)]
    public sealed class MoonlitGame : MonoBehaviour
    {
        public RideState Ride { get; private set; } = new RideState();
        public int Collected { get; private set; }
        public int Score { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public CoastalWorld World { get; private set; }
        public BicycleView Bicycle { get; private set; }
        public Camera View { get; private set; }
        RowTracker rows = new RowTracker();
        PickupFeedback feedback;
        RideAudio music;
        Light pulse;
        float rideTime, pickupPulse;
        bool reduced, sound = true, touch;
        float volume = .55f;
        GUIStyle title, heading, body, small, button, stat;
        Texture2D panel;
        Material sky;
        float uiWidth, uiHeight, uiScale;
        string notice = "";
        bool suppressRideForSmoke, smokeAnimateRider;
        RideInput currentInput;

        void Awake()
        {
            Application.targetFrameRate = 120; QualitySettings.vSyncCount = 1; Time.fixedDeltaTime = 1f / 120; Time.timeScale = 0;
            reduced = PlayerPrefs.GetInt("ReducedMotion", 0) != 0; sound = PlayerPrefs.GetInt("Sound", 1) != 0; volume = PlayerPrefs.GetFloat("Volume", .55f);
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.32f, .40f, .58f); RenderSettings.ambientEquatorColor = new Color(.32f, .29f, .34f); RenderSettings.ambientGroundColor = new Color(.25f, .23f, .32f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(.27f, .38f, .66f); RenderSettings.fogStartDistance = 100; RenderSettings.fogEndDistance = 340;
            var sun = new GameObject("Blue-hour sunlight", typeof(Light)).GetComponent<Light>(); sun.transform.SetParent(transform); sun.type = LightType.Directional; sun.color = new Color(1, .85f, .65f); sun.intensity = .62f; sun.transform.rotation = Quaternion.Euler(40, -35, 0); sun.shadows = LightShadows.Soft;
            View = new GameObject("Ride camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); View.tag = "MainCamera"; View.transform.SetParent(transform); View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.11f, .19f, .36f); View.fieldOfView = 62; View.nearClipPlane = .1f; View.farClipPlane = 750; View.allowHDR = true; View.gameObject.AddComponent<CoastalBloom>();
            sky = new Material(Shader.Find("MoonlitRide/Sky")); RenderSettings.skybox = sky; View.clearFlags = CameraClearFlags.Skybox;
            World = new GameObject("Procedural coast", typeof(CoastalWorld)).GetComponent<CoastalWorld>(); World.transform.SetParent(transform); World.Initialize();
            Bicycle = new GameObject("Bicycle and rider", typeof(BicycleView)).GetComponent<BicycleView>(); Bicycle.transform.SetParent(transform); Bicycle.Initialize();
            feedback = new GameObject("Pooled pickup effects", typeof(PickupFeedback)).GetComponent<PickupFeedback>(); feedback.transform.SetParent(transform); feedback.Initialize();
            pulse = new GameObject("Collection glow", typeof(Light)).GetComponent<Light>(); pulse.transform.SetParent(Bicycle.transform, false); pulse.transform.localPosition = new Vector3(0, 1.8f, .4f); pulse.type = LightType.Point; pulse.range = 5; pulse.color = new Color(1, .8f, .42f); pulse.intensity = 0;
            music = gameObject.AddComponent<RideAudio>(); SnapCamera();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-moonlitSmoke") >= 0) StartCoroutine(Smoke());
        }
        public void Begin() { Running = true; Paused = false; Time.timeScale = 1; }
        public void TogglePause() { if (Running) { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; } }
        public void Restart()
        {
            Ride = new RideState(); Collected = Score = 0; rideTime = pickupPulse = 0; rows.Reset();
            Bicycle.ResetPose(); World.ResetWorld(); feedback.ResetEffects(); music.ResetScore();
            Running = true; Paused = false; Time.timeScale = 1; Bicycle.Pose(Ride, 0, reduced); Bicycle.Dynamics.ResetSimulation(); SnapCamera();
        }
        void OnApplicationFocus(bool focused) { if (!focused && Running && !Application.isBatchMode) { Paused = true; Time.timeScale = 0; } }
        bool Held(Rect r)
        {
            foreach (var t in Input.touches)
                if (t.phase != TouchPhase.Canceled && t.phase != TouchPhase.Ended && r.Contains(new Vector2(t.position.x / uiScale, (Screen.height - t.position.y) / uiScale))) return true;
            return Input.GetMouseButton(0) && r.Contains(new Vector2(Input.mousePosition.x / uiScale, (Screen.height - Input.mousePosition.y) / uiScale));
        }
        Rect Left => new Rect(28, uiHeight - 150, 76, 66);
        Rect Right => new Rect(116, uiHeight - 150, 76, 66);
        Rect Pedal => new Rect(uiWidth - 206, uiHeight - 150, 82, 66);
        Rect Brake => new Rect(uiWidth - 112, uiHeight - 150, 84, 66);
        void MeasureUI() { uiScale = Mathf.Max(.3f, Mathf.Min(Screen.width / 1100f, Screen.height / 720f)); uiWidth = Screen.width / uiScale; uiHeight = Screen.height / uiScale; }
        void Update()
        {
            MeasureUI();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Pause) || Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Return) && !Running) Begin();
            if (Input.GetKeyDown(KeyCode.R) && Running) Restart();
            bool controls = touch || Input.touchSupported;
            var input = new RideInput {
                Steer = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || controls && Held(Right) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || controls && Held(Left) ? 1 : 0),
                Pedal = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || controls && Held(Pedal),
                Brake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || controls && Held(Brake),
                Coast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            };
            currentInput = input;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f), activeDt = Running && !Paused && !suppressRideForSmoke ? dt : 0;
            rideTime += activeDt;
            // Water's clock is explicitly frozen together with pickup animation.
            World.SetTime(rideTime);
            World.Stream(Ride.Progress);
            foreach (var chunk in World.Chunks) foreach (var p in chunk.Pickups)
                if (!p.Taken) { p.Visual.position = Route.Position(p.Progress, p.Lane, 1.5f + (reduced ? 0 : Mathf.Sin(rideTime * 2 + p.Progress * .12f) * .15f)); if (p.Booster && !reduced) p.Visual.localRotation = Quaternion.Euler(0, rideTime * 90, 0); }
            UpdateCamera(dt); feedback.Tick(activeDt, View, reduced); pickupPulse = Mathf.Max(0, pickupPulse - activeDt * 2.8f); pulse.intensity = pickupPulse * (reduced ? 1.5f : 4);
            music.Configure(Running, Paused, sound, volume, Running && !Paused ? Ride.Speed : 0);
            notice = !Running ? "READY TO RIDE" : Paused ? "PAUSED" : input.Brake ? "BRAKING" : Ride.Contact ? "ROAD EDGE" : Ride.BoostRemaining > 0 ? "FIREFLY BOOST  /  " + Mathf.CeilToInt(Ride.BoostRemaining) + "s" : input.Coast ? "COASTING" : input.Pedal ? "PEDALING HARD" : Route.Slope(-Ride.Progress) < -.012f ? "ASSISTED CLIMB" : "DOWNHILL CRUISE";
        }
        void FixedUpdate()
        {
            if (!Running || Paused) return;
            if (suppressRideForSmoke) { if(smokeAnimateRider) {Ride.Cadence += 6 * RideSimulation.StepTime; Bicycle.Pose(Ride,RideSimulation.StepTime,false);} return; }
            // Run before RiderDynamics so the kinematic attachment and Cloth skin
            // see this tick's bicycle pose, not the previous rendered frame.
            Simulate(currentInput); Bicycle.Pose(Ride, RideSimulation.StepTime, reduced);
        }
        public void Simulate(RideInput input)
        {
            float before = Ride.Progress, laneBefore = Ride.Lateral;
            RideSimulation.Step(Ride, input);
            foreach (var chunk in World.Chunks) foreach (var p in chunk.Pickups)
            {
                if (p.Taken || p.Progress < before - 1.1f || p.Progress > Ride.Progress + 1.1f) continue;
                float fraction = Ride.Progress > before ? Mathf.Clamp01((p.Progress - before) / (Ride.Progress - before)) : 1;
                if (Mathf.Abs(p.Lane - Mathf.Lerp(laneBefore, Ride.Lateral, fraction)) >= .8f) continue;
                p.Taken = true; p.Visual.gameObject.SetActive(false); Collected++; Score++; pickupPulse = 1;
                bool bonus = rows.Collect(p); if (bonus) Score += 5;
                if (p.Booster) Ride.Boost();
                feedback.Burst(p.Visual.position, bonus, reduced); music.Collect(Collected, p.Booster, bonus);
            }
            rows.Prune(Ride.Progress);
        }
        void SnapCamera() { View.transform.position = new Vector3(-Route.Center(7), Route.Elevation(7) + 3.7f, 7); View.fieldOfView = 62; UpdateCamera(1); }
        void UpdateCamera(float dt)
        {
            float rush = reduced ? 0 : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(3, 20, Ride.Speed));
            float cameraZ = -Ride.Progress + 6 - rush * 1.3f;
            var target = new Vector3(-Route.Center(cameraZ) - Ride.Lateral * .86f, Route.Elevation(cameraZ) + 3.45f - rush * .45f, cameraZ);
            View.transform.position = Vector3.Lerp(View.transform.position, target, 1 - Mathf.Exp(-dt * 8));
            float lookZ = -Ride.Progress - 10 - Ride.Speed * .35f;
            View.transform.LookAt(new Vector3(-Route.Center(lookZ) - Ride.Lateral * .6f - Ride.Heading * 2, Route.Elevation(lookZ) + 1.7f, lookZ), Vector3.up);
            View.fieldOfView = Mathf.Lerp(View.fieldOfView, 54 + rush * 7, 1 - Mathf.Exp(-dt * 3));
        }
        void Styles()
        {
            if (panel) return;
            panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.035f, .07f, .13f, .9f)); panel.Apply();
            title = new GUIStyle(GUI.skin.label) { fontSize = 58, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            stat = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, padding = new RectOffset(14, 14, 8, 8) };
            foreach (var s in new[] { title, heading, body, small, stat }) s.normal.textColor = new Color(.95f, .94f, .89f);
        }
        void Panel(Rect rect) { GUI.DrawTexture(rect, panel); }
        void SavePreferences() { PlayerPrefs.SetInt("ReducedMotion", reduced ? 1 : 0); PlayerPrefs.SetInt("Sound", sound ? 1 : 0); PlayerPrefs.SetFloat("Volume", volume); PlayerPrefs.Save(); }
        void OnGUI()
        {
            MeasureUI(); Styles(); GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1));
            Panel(new Rect(24, 24, 296, 138));
            GUI.Label(new Rect(42, 35, 270, 26), "MOONLIT RIDE   /   UNITY", small);
            GUI.Label(new Rect(40, 61, 130, 60), Mathf.RoundToInt(Ride.Speed * 3.6f).ToString("00"), stat);
            GUI.Label(new Rect(112, 86, 100, 28), "km/h", small);
            GUI.Label(new Rect(42, 124, 270, 25), notice, small);
            Panel(new Rect(uiWidth - 324, 24, 300, 138));
            GUI.Label(new Rect(uiWidth - 304, 39, 275, 34), Score + "  SCORE     " + Collected + "  FIREFLIES", body);
            GUI.Label(new Rect(uiWidth - 304, 80, 270, 28), Mathf.FloorToInt(Ride.Distance) + " m  /  " + Mathf.RoundToInt(Route.Elevation(-Ride.Progress)) + " m elevation", body);
            GUI.Label(new Rect(uiWidth - 304, 120, 270, 26), "Collect a full row for +5", small);
            if (Running && GUI.Button(new Rect(uiWidth - 134, 178, 110, 40), Paused ? "Resume" : "Pause", button)) TogglePause();
            if (!Running || Paused)
            {
                float x = uiWidth / 2 - 260, y = Mathf.Max(177, uiHeight / 2 - 150);
                Panel(new Rect(x, y, 520, 356));
                GUI.Label(new Rect(x + 28, y + 18, 475, 80), Paused ? "Take a breath." : "Moonlit Ride", title);
                GUI.Label(new Rect(x + 30, y + 100, 465, 64), "A quiet ride along the coast. Follow the light, feel the hills, and take your time.", body);
                GUI.Label(new Rect(x + 30, y + 175, 465, 54), "A / D  steer    W  pedal    S  brake    Shift  coast\nSpace  pause    R  restart", small);
                if (GUI.Button(new Rect(x + 30, y + 239, 285, 52), Paused ? "Continue riding" : "Let's ride", button)) { if (Paused) TogglePause(); else Begin(); }
                if (Paused && GUI.Button(new Rect(x + 331, y + 239, 159, 52), "Restart", button)) Restart();
                GUI.Label(new Rect(x + 30, y + 306, 460, 26), "No timer. No finish line. Just one more turn.", small);
            }
            Panel(new Rect(24, uiHeight - 63, 664, 42));
            bool oldSound = sound, oldReduced = reduced; float oldVolume = volume;
            if (GUI.Button(new Rect(34, uiHeight - 57, 102, 30), sound ? "Sound on" : "Sound off", button)) sound = !sound;
            volume = GUI.HorizontalSlider(new Rect(154, uiHeight - 47, 118, 20), volume, 0, 1);
            if (GUI.Button(new Rect(290, uiHeight - 57, 186, 30), reduced ? "Motion: reduced" : "Motion: full", button)) reduced = !reduced;
            if (GUI.Button(new Rect(484, uiHeight - 57, 190, 30), touch ? "Touch: on" : "Touch: off", button)) touch = !touch;
            if (sound != oldSound || reduced != oldReduced || !Mathf.Approximately(volume, oldVolume)) SavePreferences();
            if ((touch || Input.touchSupported) && Running && !Paused)
            {
                // Leave the settings bar above touch controls so hit regions never overlap.
                GUI.Box(Left, "LEFT", button); GUI.Box(Right, "RIGHT", button); GUI.Box(Pedal, "PEDAL", button); GUI.Box(Brake, "BRAKE", button);
            }
        }
        static void Require(bool ok, string message) { if (!ok) throw new Exception("Runtime smoke: " + message); }
        IEnumerator Smoke()
        {
            // Runs the actual scene in a Windows player, including graphics and lifecycle paths.
            Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(4);
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-moonlitCapture");
            string capture = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
            string failure = null;
            Begin(); Ride.Boost(); TogglePause();
            float pausedProgress = Ride.Progress, pausedBoost = Ride.BoostRemaining;
            Vector3 pausedCloth = Bicycle.Dynamics.Dress.vertices[100];
            Vector3 pausedHair = Bicycle.Dynamics.HairBodies[11].position;
            yield return new WaitForSecondsRealtime(.2f);
            try
            {
                Require(Paused && Ride.Progress == pausedProgress && Ride.BoostRemaining == pausedBoost && Bicycle.Dynamics.Dress.vertices[100] == pausedCloth && Bicycle.Dynamics.HairBodies[11].position == pausedHair, "pause freezes ride, native cloth, and rigidbody braid");
                Restart();
                Vector3 centerPixel = View.WorldToViewportPoint(Route.Position(0));
                var steeringProbe = new RideState { Speed = 10 };
                for (int i = 0; i < 120; i++) RideSimulation.StepOnRoad(steeringProbe, new RideInput { Steer = 1 }, 0, 0, 0);
                Require(View.WorldToViewportPoint(Route.Position(0, steeringProbe.Lateral)).x > centerPixel.x, "right input moves right on screen");
                Require(View.WorldToViewportPoint(Route.Position(0, 10)).x > centerPixel.x && View.WorldToViewportPoint(Route.Position(0, -18)).x < centerPixel.x, "houses right, sea left");
                for (int frame = 0; frame < 900; frame++) { for (int step = 0; step < 8; step++) Simulate(new RideInput()); if (frame % 4 == 0) World.Stream(Ride.Progress); }
                Require(Ride.Progress > 478 && Ride.Distance > 500, "route progression");
                Require(Collected >= 31 && Score >= 56, "rows and booster collection");
                Require(World.Chunks.Count == 23, "bounded chunk streaming");
                Restart(); Require(Score == 0 && Collected == 0 && Ride.Speed == 0 && Ride.BoostRemaining == 0, "restart clears ride");
                for (int i = 0; i < 3200; i++) { Simulate(new RideInput()); if (i % 32 == 0) World.Stream(Ride.Progress); }
                Bicycle.Pose(Ride, 1, false); Bicycle.Dynamics.ResetSimulation(); UpdateCamera(1);
            }
            catch (Exception e) { failure = e.ToString(); Debug.LogError(failure); }
            suppressRideForSmoke = true; Ride.Speed = 0;
            var dynamics = Bicycle.Dynamics; dynamics.Wind.windMain = 0; dynamics.Wind.windTurbulence = 0;
            yield return new WaitForSecondsRealtime(1.5f);
            var calm = dynamics.Dress.vertices; Vector3 calmTip = dynamics.HairBodies[11].position;
            dynamics.Wind.windMain = 12; dynamics.Wind.windTurbulence = 1;
            yield return new WaitForSecondsRealtime(2);
            try
            {
                var windy = dynamics.Dress.vertices; float motion = 0;
                for (int i = 0; i < windy.Length; i++) { Require(float.IsFinite(windy[i].sqrMagnitude) && windy[i].magnitude < 4, "native cloth bounded"); motion = Mathf.Max(motion, Vector3.Distance(windy[i], calm[i])); }
                Require(motion > .01f, "native cloth responds to wind");
                Require(Vector3.Distance(windy[0], calm[0]) < .002f, "cloth waist stays attached in wind");
                Require(Vector3.Distance(calmTip, dynamics.HairBodies[11].position) > .01f, "joint braid responds to wind");
                foreach (var rb in dynamics.HairBodies) Require(Vector3.Distance(rb.position, dynamics.HairAnchor.position) < 1.25f, "braid constraints bounded");
                Require(dynamics.Dress.coefficients[0].maxDistance == 0, "pinned dress waist");
                Require(dynamics.VisibleHairRootError() < .13f, "rendered braid stays attached to head");
                Debug.Log("MOONLIT_NATIVE_WIND_PASSED cloth displacement=" + motion);
            }
            catch (Exception e) { failure = e.ToString(); Debug.LogError(failure); }
            dynamics.Wind.windMain = 1.6f; dynamics.Wind.windTurbulence = .65f; Ride.Speed = RideSimulation.MaximumSpeed; suppressRideForSmoke = false;
            yield return new WaitForSecondsRealtime(1.5f);
            Ride.Speed = 0; yield return new WaitForSecondsRealtime(.5f);
            Ride.Speed = 12; yield return new WaitForSecondsRealtime(3);
            try
            {
                foreach (var rb in dynamics.HairBodies) Require(Vector3.Distance(rb.position, dynamics.HairAnchor.position) < 1.25f, "braid remains bounded after fast riding and abrupt stop");
                Require(dynamics.VisibleHairRootError() < .15f, "braid attachment follows moving rider");
                foreach (var v in dynamics.Dress.vertices) Require(float.IsFinite(v.sqrMagnitude) && v.magnitude < 4, "cloth remains bounded after fast riding and abrupt stop");
            }
            catch (Exception e) { failure = e.ToString(); Debug.LogError(failure); }
            // Verify rendered cloth at ordinary riding speed, not just an extreme wind test.
            suppressRideForSmoke = true; smokeAnimateRider = true; Ride.Speed = 10;
            var baseline = dynamics.Dress.vertices; float ordinaryMotion = 0;
            for (int frame = 0; frame < 16; frame++)
            {
                yield return new WaitForSecondsRealtime(.12f);
                var pose = dynamics.Dress.vertices;
                for (int v = 0; v < pose.Length; v++) ordinaryMotion = Mathf.Max(ordinaryMotion, Vector3.Distance(pose[v], baseline[v]));
                if (capture != null) {
                    View.transform.position = Bicycle.transform.TransformPoint(1.7f, 2.55f, 3.5f); View.transform.LookAt(Bicycle.transform.TransformPoint(0,1.8f,.1f)); View.fieldOfView=43;
                    string directory=Path.Combine(Path.GetDirectoryName(capture),"motion-frames");Directory.CreateDirectory(directory);
                    CaptureCamera(Path.Combine(directory, "frame-"+frame.ToString("D2")+".png"));
                }
            }
            try { Require(ordinaryMotion > .08f,"dress visibly moves during ordinary 10 m/s riding");Debug.Log("MOONLIT_ORDINARY_CLOTH_PASSED displacement="+ordinaryMotion); }
            catch(Exception e){failure=e.ToString();Debug.LogError(failure);}
            smokeAnimateRider = false; UpdateCamera(1);
            feedback.ResetEffects(); pickupPulse = 0; pulse.intensity = 0;
            if (failure == null) Debug.Log("MOONLIT_RUNTIME_SMOKE_PASSED");
            yield return new WaitForEndOfFrame();
            if (capture != null)
            {
                CaptureCamera(capture);
                View.transform.position=Bicycle.transform.TransformPoint(3.3f,1.5f,2.1f);
                View.transform.LookAt(Bicycle.transform.TransformPoint(0,.95f,0));View.fieldOfView=48;
                CaptureCamera(Path.Combine(Path.GetDirectoryName(capture),"Bicycle-detail.png"));
                // Capture each district from a fixed, repeatable viewpoint; exercise streaming across all zones.
                foreach(float districtProgress in new[]{72f,312f,516f}) {
                    World.Stream(districtProgress);
                    View.transform.position=Route.Position(districtProgress-12,-15,8);
                    View.transform.LookAt(Route.Position(districtProgress+12,7,3));View.fieldOfView=62;
                    CaptureCamera(Path.Combine(Path.GetDirectoryName(capture),"District-"+CoastalWorld.DistrictAt(districtProgress)+".png"));
                    if(districtProgress==516) {
                        View.transform.position=Route.Position(504,-78,25);
                        View.transform.LookAt(Route.Position(516,-29,-6));View.fieldOfView=65;
                        CaptureCamera(Path.Combine(Path.GetDirectoryName(capture),"District-Marina.png"));
                    }
                }
                World.Stream(Ride.Progress);
                View.transform.position=Route.Position(Ride.Progress+15,-45,19);
                View.transform.LookAt(Route.Position(Ride.Progress+40,-8,-6));View.fieldOfView=65;
                CaptureCamera(Path.Combine(Path.GetDirectoryName(capture),Path.GetFileNameWithoutExtension(capture)+"-shore.png"));
                View.transform.position = Bicycle.transform.TransformPoint(new Vector3(2.1f, 2.6f, 3.2f));
                View.transform.LookAt(Bicycle.transform.TransformPoint(new Vector3(0, 1.9f, .1f))); View.fieldOfView = 40;
                CaptureCamera(Path.Combine(Path.GetDirectoryName(capture), Path.GetFileNameWithoutExtension(capture) + "-rider.png"));                View.transform.position = Bicycle.transform.TransformPoint(1.9f, 2.5f, -2.7f); View.transform.LookAt(Bicycle.transform.TransformPoint(0, 2.15f, .1f));
                CaptureCamera(Path.Combine(Path.GetDirectoryName(capture), Path.GetFileNameWithoutExtension(capture) + "-face.png"));
                Ride.Speed = 25; Ride.Acceleration = 3; Ride.BoostRemaining = 9; Bicycle.Pose(Ride, 2, false); Bicycle.Dynamics.ResetSimulation();
                View.transform.position = Bicycle.transform.TransformPoint(3.4f, 2.3f, .6f); View.transform.LookAt(Bicycle.transform.TransformPoint(0, 1.8f, 0));
                CaptureCamera(Path.Combine(Path.GetDirectoryName(capture), Path.GetFileNameWithoutExtension(capture) + "-boost.png"));
            }
            yield return null; yield return null;
            Application.Quit(failure == null ? 0 : 1);
        }
        void CaptureCamera(string path)
        {
            var target = new RenderTexture(1440, 900, 24); var pixels = new Texture2D(1440, 900, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            View.targetTexture = target; View.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
            View.targetTexture = null; RenderTexture.active = previous; Destroy(pixels); target.Release(); Destroy(target);
        }
        void OnDestroy() { Time.timeScale = 1; if (panel) Destroy(panel); if (sky) Destroy(sky); }
    }
}
