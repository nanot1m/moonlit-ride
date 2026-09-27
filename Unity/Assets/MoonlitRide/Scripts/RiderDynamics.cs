using System;
using UnityEngine;

namespace MoonlitRide
{
    // Native Unity cloth and PhysX joints replace the browser Verlet simulation.
    public sealed class RiderDynamics : MonoBehaviour
    {
        public Cloth Dress { get; private set; }
        public WindZone Wind { get; private set; }
        public Rigidbody[] HairBodies { get; private set; }
        public Transform HairAnchor => anchor.transform;
        public Vector3 AppliedWind { get; private set; }
        Rigidbody anchor, torsoBody;
        Transform head, waistBone; Vector3 bodyOffset;
        SkinnedMeshRenderer hairRenderer;
        Mesh dressMesh, hairMesh;
        CapsuleCollider[] bodyCapsules;
        RideState ride;
        bool reduced;
        const int HairCount = 12;
        const float LinkLength = .08f;
        Quaternion hairRestRotation; Vector3 lastAttachment;
        Vector3 rootLocal = new Vector3(0, -.115f, .19f);
        public void Initialize(Transform headTransform, Material dressMaterial, Material hairMaterial)
        {
            head = headTransform;
            Wind = new GameObject("Coastal wind", typeof(WindZone)).GetComponent<WindZone>(); Wind.transform.SetParent(transform, false);
            Wind.mode = WindZoneMode.Directional; Wind.windMain = 1.6f; Wind.windTurbulence = .65f; Wind.windPulseMagnitude = .45f; Wind.windPulseFrequency = .24f; Wind.transform.localRotation = Quaternion.Euler(0, 55, 0);
            // Cloth only collides with explicitly assigned primitives, not scene geometry.
            bodyCapsules = new CapsuleCollider[3];
            bodyCapsules[0] = Capsule("Hip cloth envelope", new Vector3(0, 1.54f, .15f), .205f, .60f);
            bodyCapsules[1] = Capsule("Left leg cloth envelope", new Vector3(-.20f, 1.19f, .03f), .135f, .62f);
            bodyCapsules[2] = Capsule("Right leg cloth envelope", new Vector3(.20f, 1.19f, .03f), .135f, .62f);
            var torso = Capsule("Torso hair collider", new Vector3(0, 2.12f, .10f), .23f, .60f);
            torsoBody = torso.gameObject.AddComponent<Rigidbody>(); torsoBody.isKinematic = true;

            var dressObject = new GameObject("Blender dress - Unity Cloth", typeof(SkinnedMeshRenderer)); dressObject.transform.SetParent(transform, false);
            var renderer = dressObject.GetComponent<SkinnedMeshRenderer>(); dressMesh = Instantiate(Resources.Load<Mesh>("Rider/Dress"));
            var weights = new BoneWeight[dressMesh.vertexCount]; for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
            dressMesh.boneWeights = weights; dressMesh.bindposes = new[] { Matrix4x4.identity };
            waistBone = new GameObject("Animated skirt waist").transform; waistBone.SetParent(transform, false);
            renderer.sharedMesh = dressMesh; renderer.bones = new[] { waistBone }; renderer.rootBone = transform; renderer.sharedMaterial = dressMaterial; renderer.updateWhenOffscreen = true; renderer.localBounds = new Bounds(new Vector3(0, 1.45f, .2f), new Vector3(3, 3, 3));
            Dress = dressObject.AddComponent<Cloth>(); Dress.useGravity = true; Dress.damping = .08f; Dress.stretchingStiffness = .95f; Dress.bendingStiffness = .015f;
            Dress.clothSolverFrequency = 180; Dress.stiffnessFrequency = 120; Dress.friction = .45f; Dress.collisionMassScale = .3f;
            Dress.useTethers = true; Dress.enableContinuousCollision = true; Dress.useVirtualParticles = .5f; Dress.worldVelocityScale = .12f; Dress.worldAccelerationScale = .08f;
            Dress.capsuleColliders = bodyCapsules; Dress.selfCollisionDistance = .018f; Dress.selfCollisionStiffness = .35f;
            var points = Dress.vertices; var coefficients = new ClothSkinningCoefficient[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                float t = Mathf.Clamp01((1.855f - points[i].y) / .82f);
                coefficients[i].maxDistance = t < .075f ? 0 : Mathf.Lerp(.06f, .82f, t * t);
                coefficients[i].collisionSphereDistance = 0;
            }
            Dress.coefficients = coefficients;
            // Select sparse lower cloth vertices for self collision instead of every UV seam.
            var self = new System.Collections.Generic.List<uint>(); for (uint i = 0; i < points.Length; i += 4) if (points[i].y < 1.6f) self.Add(i); Dress.SetSelfAndInterCollisionIndices(self);

            anchor = new GameObject("Braid attachment - kinematic", typeof(Rigidbody)).GetComponent<Rigidbody>(); anchor.transform.SetParent(transform.parent, false); anchor.isKinematic = true;
            anchor.position = head.TransformPoint(rootLocal); anchor.rotation = head.rotation * Quaternion.Euler(-22, 0, 0); hairRestRotation = anchor.rotation;
            HairBodies = new Rigidbody[HairCount]; var bones = new Transform[HairCount];
            for (int i = 0; i < HairCount; i++)
            {
                var node = new GameObject("Braid physics " + i, typeof(Rigidbody), typeof(CapsuleCollider), typeof(ConfigurableJoint)); node.transform.SetParent(transform.parent, false);
                var rb = node.GetComponent<Rigidbody>(); rb.position = anchor.position + hairRestRotation * Vector3.down * ((i + .5f) * LinkLength); rb.rotation = hairRestRotation;
                rb.mass = .018f; rb.linearDamping = .03f; rb.angularDamping = 4.5f; rb.interpolation = RigidbodyInterpolation.None; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; rb.solverIterations = 16; rb.solverVelocityIterations = 8; rb.maxAngularVelocity = 12;
                node.transform.SetPositionAndRotation(rb.position, rb.rotation);
                var capsule = node.GetComponent<CapsuleCollider>(); capsule.radius = Mathf.Lerp(.055f, .018f, i / 11f); capsule.height = LinkLength + capsule.radius; capsule.direction = 1;
                var joint = node.GetComponent<ConfigurableJoint>(); joint.connectedBody = i == 0 ? anchor : HairBodies[i - 1]; joint.autoConfigureConnectedAnchor = false;
                joint.anchor = Vector3.up * (LinkLength / 2); joint.connectedAnchor = i == 0 ? Vector3.zero : Vector3.down * (LinkLength / 2);
                joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked; joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -28 }; joint.highAngularXLimit = new SoftJointLimit { limit = 28 }; joint.angularYLimit = new SoftJointLimit { limit = 22 }; joint.angularZLimit = new SoftJointLimit { limit = 28 };
                joint.rotationDriveMode = RotationDriveMode.Slerp; joint.slerpDrive = new JointDrive { positionSpring = .045f, positionDamper = .009f, maximumForce = .3f };
                joint.enableCollision = false; joint.projectionMode = JointProjectionMode.PositionAndRotation; joint.projectionDistance = .008f; joint.projectionAngle = 8;
                for (int j = 0; j < i; j++) Physics.IgnoreCollision(capsule, HairBodies[j].GetComponent<Collider>());
                foreach (var c in bodyCapsules) Physics.IgnoreCollision(capsule, c);
                HairBodies[i] = rb; bones[i] = rb.transform;
            }
            var hairObject = new GameObject("Blender woven braid - skinned to joints", typeof(SkinnedMeshRenderer)); hairObject.transform.SetParent(transform.parent, false);
            hairObject.transform.SetPositionAndRotation(anchor.position, hairRestRotation); hairRenderer = hairObject.GetComponent<SkinnedMeshRenderer>();
            hairMesh = Instantiate(Resources.Load<Mesh>("Rider/Braid")); var bindposes = new Matrix4x4[HairCount];
            // Bind in mesh-local rest space; Rigidbody transforms can be one physics
            // tick behind their assigned positions during initialization.
            for (int i = 0; i < HairCount; i++) bindposes[i] = Matrix4x4.Translate(Vector3.up * ((i + .5f) * LinkLength));
            weights = new BoneWeight[hairMesh.vertexCount]; var vertices = hairMesh.vertices;
            for (int i = 0; i < weights.Length; i++) { float along = Mathf.Clamp(-vertices[i].y / LinkLength - .5f, 0, HairCount - 1); int a = Mathf.FloorToInt(along), b = Mathf.Min(a + 1, HairCount - 1); float w = along - a; weights[i] = new BoneWeight { boneIndex0 = a, weight0 = 1 - w, boneIndex1 = b, weight1 = w }; }
            hairMesh.boneWeights = weights; hairMesh.bindposes = bindposes; hairRenderer.sharedMesh = hairMesh; hairRenderer.bones = bones; hairRenderer.rootBone = anchor.transform; hairRenderer.sharedMaterial = hairMaterial; hairRenderer.updateWhenOffscreen = true; hairRenderer.localBounds = new Bounds(Vector3.down * .5f, Vector3.one * 4);
        }
        CapsuleCollider Capsule(string name, Vector3 position, float radius, float height)
        {
            var c = new GameObject(name, typeof(CapsuleCollider)).GetComponent<CapsuleCollider>(); c.transform.SetParent(transform, false); c.transform.localPosition = position; c.radius = radius; c.height = height; return c;
        }
        public void PoseLeg(int index, Vector3 hip, Vector3 knee)
        {
            var capsule = bodyCapsules[index + 1]; capsule.transform.localPosition = (hip + knee) * .5f;
            capsule.transform.localRotation = Quaternion.FromToRotation(Vector3.up, knee - hip);
            capsule.height = Vector3.Distance(hip, knee) + .12f;
        }
        public void SetRide(RideState state, bool reducedMotion) { ride = state; reduced = reducedMotion; }
        public void SetBodyOffset(Vector3 offset) {
            bodyOffset = offset; waistBone.localPosition = offset;
            bodyCapsules[0].transform.localPosition = new Vector3(0, 1.54f, .15f) + offset;
        }
        public static Vector3 WindAcceleration(float seconds, float speed, WindZone wind, Vector3 backward, bool reduced)
        {
            float pulse = 1 + wind.windPulseMagnitude * Mathf.Sin(seconds * wind.windPulseFrequency * Mathf.PI * 2);
            return wind.transform.forward * wind.windMain * pulse + backward * Mathf.Min(speed * speed * .011f, 6) + Vector3.up * (Mathf.Sin(seconds * 1.4f) * (reduced ? .05f : .2f));
        }
        void FixedUpdate()
        {
            if (ride == null) return;
            var waist = new Vector3(0, 1.855f, .2f);
            torsoBody.MovePosition(transform.TransformPoint(bodyOffset + waist + head.localRotation * (new Vector3(0,2.12f,.1f)-waist)));
            torsoBody.MoveRotation(head.rotation);
            var attachment = head.TransformPoint(rootLocal);
            Vector3 riderVelocity = (attachment - lastAttachment) / Time.fixedDeltaTime; lastAttachment = attachment;
            anchor.MovePosition(attachment); anchor.MoveRotation(head.rotation * Quaternion.Euler(-22, 0, 0));
            AppliedWind = WindAcceleration(Time.time, ride.Speed, Wind, transform.forward, reduced);
            // A constant force settles into a static drape. Time-varying crosswind and
            // aerodynamic lift keep the lightweight hem fluttering during normal riding.
            float airflow = Mathf.Clamp01(ride.Speed / 10) * (reduced ? .3f : 1);
            float gust = Mathf.Sin(Time.time * 3.1f) + .45f * Mathf.Sin(Time.time * 7.3f);
            Dress.externalAcceleration = AppliedWind * 2.1f + transform.right * (gust * airflow * 5.5f)
                + Vector3.up * ((7 + 6 * Mathf.Sin(Time.time * 4.4f)) * airflow)
                + transform.forward * (airflow * (4 + 2 * Mathf.Sin(Time.time * 2.7f)));
            Dress.randomAcceleration = Vector3.one * Wind.windTurbulence * (reduced ? .1f : 1.4f);
            
            foreach (var rb in HairBodies) rb.AddForce(AppliedWind * .5f - (rb.linearVelocity - riderVelocity) * .65f, ForceMode.Acceleration);
            // Root bounds follow the rider although the joint bones are independent world bodies.
            hairRenderer.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }
        public void ResetSimulation()
        {
            if (!Dress) return;
            Dress.enabled = false; Dress.enabled = true; Dress.ClearTransformMotion();
            anchor.position = head.TransformPoint(rootLocal); anchor.rotation = head.rotation * Quaternion.Euler(-22, 0, 0);
            anchor.transform.SetPositionAndRotation(anchor.position, anchor.rotation); lastAttachment = anchor.position;
            for (int i = 0; i < HairBodies.Length; i++) { var rb = HairBodies[i]; rb.position = anchor.position + anchor.rotation * Vector3.down * ((i + .5f) * LinkLength); rb.rotation = anchor.rotation; rb.transform.SetPositionAndRotation(rb.position, rb.rotation); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            hairRenderer.transform.SetPositionAndRotation(anchor.position, anchor.rotation); Physics.SyncTransforms();
        }
        public float VisibleHairRootError()
        {
            var baked = new Mesh(); hairRenderer.BakeMesh(baked);
            float distance = Vector3.Distance(hairRenderer.transform.TransformPoint(baked.vertices[0]), anchor.position);
            Destroy(baked); return distance;
        }
        void OnDestroy()
        {
            if (anchor) Destroy(anchor.gameObject); if (hairRenderer) Destroy(hairRenderer.gameObject);
            if (HairBodies != null) foreach (var rb in HairBodies) if (rb) Destroy(rb.gameObject);
            if (dressMesh) Destroy(dressMesh); if (hairMesh) Destroy(hairMesh);
        }
    }
}
