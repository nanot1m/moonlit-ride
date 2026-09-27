using System.Collections.Generic;
using UnityEngine;

namespace MoonlitRide
{
    public sealed class BicycleView : MonoBehaviour
    {
        public RiderDynamics Dynamics { get; private set; }
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Transform[] arms = new Transform[2], forearms = new Transform[2], upper = new Transform[2], lower = new Transform[2], shoes = new Transform[2];
        Transform torso, head;
        AnatomicalRider anatomy; CityBicycle bicycle;
        float tuck, standing; Vector3 bodyOffset; Quaternion posture = Quaternion.identity;
        public float Standing => standing;
        public float ForwardLean => -tuck * Mathf.Rad2Deg;
        public static float StandingTarget(float speed, bool pedalling) => pedalling ? (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2, 7, speed))) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.1f, 1, speed)) : 0;
        public static float LeanTarget(RideState s, float stand) => -.32f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 26, s.Speed)) - .30f * Mathf.Clamp01(s.Acceleration / 5) - .10f * Mathf.Clamp01(s.BoostRemaining / 9) - .12f * stand;
        Material Mat(string c, float emission = 0) { var m = Geometry.Material(c, emission); materials.Add(m); return m; }
        Mesh Own(Mesh m) { meshes.Add(m); return m; }
        public void Initialize()
        {
            var rubber = Mat("#202632"); var steel = Mat("#E9B35B"); var trim = Mat("#F8DDA6"); var jacket = Mat("#243956"); var blue = Mat("#355CCD"); var skin = Mat("#CEA78F");
            var fabric = new Material(Shader.Find("MoonlitRide/Fabric")); materials.Add(fabric);
            fabric.mainTexture = Resources.Load<Texture2D>("Rider/DressFabric");
            var hair = Mat("#FFFFFF"); hair.mainTexture = Resources.Load<Texture2D>("Rider/HairStrands"); hair.SetFloat("_Glossiness", .28f); hair.SetFloat("_Metallic", .02f); hair.EnableKeyword("_EMISSION"); hair.SetColor("_EmissionColor", new Color(.002f, .045f, .85f));
            bicycle=gameObject.AddComponent<CityBicycle>();bicycle.Initialize();
            torso = new GameObject("Bodice posture pivot").transform; torso.SetParent(transform, false); torso.localPosition = new Vector3(0, 1.855f, .2f);
            var blouse = new Material(Shader.Find("MoonlitRide/Blouse")); materials.Add(blouse);
            anatomy = gameObject.AddComponent<AnatomicalRider>(); anatomy.Initialize(blouse, skin, skin);
            head = new GameObject("Head and swept hair").transform; head.SetParent(transform, false); head.localPosition = new Vector3(0, 2.675f, .134f);
            head.localScale = Vector3.one * .86f;
            
            Geometry.MeshObject("Sculpted face", head, Resources.Load<Mesh>("Rider/Head"), skin);
            Geometry.MeshObject("Swept hair cap", head, Resources.Load<Mesh>("Rider/HairCap"), hair);
            var locks = new Material(hair); locks.mainTexture = null; locks.color = new Color(.008f, .10f, .98f); materials.Add(locks);
            Geometry.MeshObject("Combed locks gathered into braid", head, Resources.Load<Mesh>("Rider/HairDetail"), locks);

            var tie = Geometry.MeshObject("Brass braid clasp", head, Own(Geometry.Torus(.056f, .009f, 24)), steel);
            tie.localPosition = new Vector3(0, -.17f, .207f); tie.localRotation = Quaternion.Euler(-22, 0, 90);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                arms[i] = Geometry.Rod(transform, Vector3.zero, Vector3.up, .074f, jacket);
                forearms[i] = Geometry.Rod(transform, Vector3.zero, Vector3.up, .057f, skin);
                upper[i] = Geometry.Rod(transform, Vector3.zero, Vector3.up, .085f, rubber);
                lower[i] = Geometry.Rod(transform, Vector3.zero, Vector3.up, .065f, rubber);
                shoes[i] = Geometry.Ball(transform, Vector3.zero, new Vector3(.12f, .10f, .25f), jacket);
                foreach (var part in new[] { arms[i], forearms[i], upper[i], lower[i] }) part.GetComponent<Renderer>().enabled = false;
            }
            Geometry.Box(transform,new Vector3(0,1.14f,1.14f),new Vector3(.13f,.08f,.06f),rubber);
            Geometry.Box(transform,new Vector3(0,1.14f,1.18f),new Vector3(.10f,.05f,.025f),Mat("#EF5540",2));
            Geometry.Rod(transform,new Vector3(0,1.27f,-.61f),new Vector3(0,1.33f,-.83f),.014f,trim);
            Geometry.Ball(transform,new Vector3(0,1.33f,-.83f),new Vector3(.14f,.14f,.18f),trim);
            Geometry.Ball(transform,new Vector3(0,1.33f,-.92f),new Vector3(.11f,.11f,.025f),Mat("#FFF0CA",1));
            var light = new GameObject("Headlight", typeof(Light)).GetComponent<Light>(); light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0, 1.35f, -.8f); light.transform.localRotation = Quaternion.Euler(12, 180, 0);
            light.type = LightType.Spot; light.range = 20; light.spotAngle = 58; light.intensity = 3; light.color = new Color(1, .87f, .65f);
            Dynamics = gameObject.AddComponent<RiderDynamics>(); Dynamics.Initialize(head, fabric, hair);
            Pose(new RideState(), 0, true);
            Dynamics.ResetSimulation();
        }
        Vector3 Tuck(Vector3 p) => bodyOffset + new Vector3(0, 1.855f, .2f) + posture * (p - new Vector3(0, 1.855f, .2f));
        public void ResetPose() { tuck = standing = 0; bodyOffset = Vector3.zero; posture = Quaternion.identity; }
        public void Pose(RideState s, float dt, bool reduced)
        {
            float z = -s.Progress, heading = Mathf.Atan(Route.Derivative(z));
            transform.position = Route.Position(s.Progress, s.Lateral, -.04f + (reduced ? 0 : Mathf.Sin(s.Distance * 8) * .007f * Mathf.Min(s.Speed / 10, 1)));
            transform.rotation = Quaternion.Euler(0, (-heading + s.Heading) * Mathf.Rad2Deg, 0) * Quaternion.Euler(-Mathf.Atan(Route.Slope(z) * Mathf.Cos(heading)) * Mathf.Rad2Deg, 0, -s.Lean * Mathf.Rad2Deg);
            bicycle.Pose(s);
            standing += (StandingTarget(s.Speed, s.Pedalling) - standing) * (1 - Mathf.Exp(-dt * 4));
            bodyOffset = new Vector3(reduced ? 0 : Mathf.Sin(s.Cadence) * .014f, .12f + (reduced ? 0 : Mathf.Sin(s.Cadence * 2) * .012f), -.14f) * standing;
            float targetTuck = LeanTarget(s, standing);
            tuck += (targetTuck - tuck) * (1 - Mathf.Exp(-dt * 3));
            float sway = reduced ? 0 : Mathf.Sin(s.Cadence) * .95f * Mathf.Clamp01(s.Speed / 5);
            posture = Quaternion.Euler(tuck * Mathf.Rad2Deg, Mathf.Sin(s.Cadence*.5f)*.35f*(reduced?0:1), sway);
            torso.localPosition = bodyOffset + new Vector3(0, 1.855f, .2f); head.localPosition = Tuck(new Vector3(0, 2.63f, .134f)); torso.localRotation = head.localRotation = posture;
            Dynamics.SetBodyOffset(bodyOffset); Dynamics.SetRide(s, reduced); anatomy.PoseTorso(posture, bodyOffset);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1, a = s.Cadence + i * Mathf.PI;
                var shoulder = Tuck(new Vector3(side * .2405f, 2.2294f, .1599f));
                var hand = bicycle.Hand(side);
                var elbow = SolveJoint(shoulder, hand, new Vector3(side * .7f, 1.85f, .15f), .48f, .47f);
                Geometry.PoseRod(arms[i], shoulder, elbow, .074f); Geometry.PoseRod(forearms[i], elbow, hand, .057f);
                var foot = CityBicycle.Foot(i,s.Cadence);
                var hip = bodyOffset + new Vector3(side * .172f, 1.44f, .235f);
                var knee = SolveJoint(hip, foot, new Vector3(side * .3f, 1.1f, -1), .60f, .61f);
                Dynamics.PoseLeg(i, hip, knee);
                anatomy.PoseLimb(i, shoulder, elbow, hand, hip, knee, foot);
                Geometry.PoseRod(upper[i], new Vector3(side * .172f, 1.44f, .235f), knee, .085f); Geometry.PoseRod(lower[i], knee, foot, .065f); shoes[i].localPosition = foot; shoes[i].localRotation = Quaternion.Euler(Mathf.Sin(a)*7,0,0);
            }
        }
        // Analytic two-bone IK: fixed limb lengths, stable knee/elbow pole, reachable targets.
        public static Vector3 SolveJoint(Vector3 root, Vector3 target, Vector3 pole, float upperLength, float lowerLength)
        {
            var delta = target - root; float distance = Mathf.Clamp(delta.magnitude, .001f, upperLength + lowerLength - .001f);
            var axis = delta.normalized; var bend = Vector3.ProjectOnPlane(pole - root, axis).normalized;
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
            return root + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
        }
        void OnDestroy() { foreach (var m in meshes) Destroy(m); foreach (var m in materials) Destroy(m); }
    }
}
