using System.Collections.Generic;
using UnityEngine;

namespace MoonlitRide
{
    // Position-based constraints ported from secondary-motion.js, in rider local space.
    public sealed class SecondaryMotion
    {
        public sealed class Point
        {
            public Vector3 Position, Previous, Rest;
            public bool Pinned;
            public Point(Vector3 p, bool pinned) { Position = Previous = Rest = p; Pinned = pinned; }
        }
        struct Link { public Point A, B; public float Length; }
        public const int Segments = 40, Rows = 10;
        public readonly List<Point> Cloth = new List<Point>(), Hair = new List<Point>();
        readonly List<Link> links = new List<Link>(), hairLinks = new List<Link>();
        float time;
        Point At(int j, int i) => Cloth[j * Segments + (i + Segments) % Segments];
        void Connect(List<Link> list, Point a, Point b) => list.Add(new Link { A = a, B = b, Length = Vector3.Distance(a.Position, b.Position) });
        public SecondaryMotion()
        {
            for (int j = 0; j <= Rows; j++) for (int i = 0; i < Segments; i++)
            {
                float t = (float)j / Rows, a = i * Mathf.PI * 2 / Segments, r = (.235f + .435f * t) * (1 + Mathf.Sin(a * 12) * .025f * t);
                Cloth.Add(new Point(new Vector3(Mathf.Sin(a) * r, .475f - t * .95f, Mathf.Cos(a) * r), j == 0));
            }
            for (int j = 0; j <= Rows; j++) for (int i = 0; i < Segments; i++)
            {
                Connect(links, At(j, i), At(j, i + 1));
                if (j < Rows) { Connect(links, At(j, i), At(j + 1, i)); Connect(links, At(j, i), At(j + 1, i + 1)); Connect(links, At(j, i + 1), At(j + 1, i)); }
                if (j < Rows - 1) Connect(links, At(j, i), At(j + 2, i));
            }
            for (int i = 0; i < 16; i++)
            {
                Hair.Add(new Point(new Vector3(0, 2.65f - i * .073f, .26f + i * .047f), i == 0));
                if (i > 0) Connect(hairLinks, Hair[i - 1], Hair[i]);
            }
        }
        static void Integrate(Point p, Vector3 force, float damping, float dt)
        {
            if (p.Pinned) return;
            Vector3 old = p.Position; p.Position += (p.Position - p.Previous) * damping + force * dt * dt; p.Previous = old;
        }
        static void Satisfy(Link l, float stiffness)
        {
            Vector3 delta = l.B.Position - l.A.Position; float d = Mathf.Max(delta.magnitude, .000001f);
            Vector3 correction = delta * ((d - l.Length) / d * stiffness * (l.A.Pinned || l.B.Pinned ? 1 : .5f));
            if (!l.A.Pinned) l.A.Position += correction;
            if (!l.B.Pinned) l.B.Position -= correction;
        }
        public void Reset() { time = 0; foreach (var p in Cloth) p.Position = p.Previous = p.Rest; foreach (var p in Hair) p.Position = p.Previous = p.Rest; }
        public void Step(RideState s, float dt)
        {
            time += dt;
            float air = Mathf.Min(s.Speed * s.Speed * .012f, 5), side = Mathf.Clamp(Mathf.Tan(s.Lean) * 9.81f, -5, 5), inertia = Mathf.Clamp(s.Acceleration, -5, 3);
            for (int j = 1; j <= Rows; j++) for (int i = 0; i < Segments; i++)
            {
                var p = At(j, i); float t = (float)j / Rows, a = i * Mathf.PI * 2 / Segments;
                float flutter = Mathf.Sin(time * (5 + s.Speed * .25f) - j * .7f + a * 3) * air * .24f * t;
                Vector3 restore = p.Rest - p.Position;
                Integrate(p, new Vector3(side + Mathf.Sin(a) * flutter + restore.x * 18, -4.5f + restore.y * 26 + Mathf.Sin(s.Distance * 7) * Mathf.Min(s.Speed * .07f, .7f) * t, air * t + inertia * .45f + restore.z * 16 + Mathf.Cos(a) * flutter), .975f, dt);
            }
            foreach (var p in Hair) Integrate(p, new Vector3(side * .8f + Mathf.Sin(time * 5 + p.Rest.y * 3) * air * .07f, -9.81f, air * 1.6f + inertia * .7f), .984f, dt);
            for (int iteration = 0; iteration < 7; iteration++)
            {
                foreach (var l in links) Satisfy(l, .85f);
                foreach (var p in Cloth)
                {
                    if (p.Pinned) continue;
                    var v = p.Position; float t = (.475f - p.Rest.y) / .95f, radius = new Vector2(v.x, v.z).magnitude, min = (.235f + .435f * t) * .76f;
                    if (radius < min) { float scale = min / Mathf.Max(radius, .000001f); v.x *= scale; v.z *= scale; }
                    v.y = Mathf.Clamp(v.y, -.59f, .475f); p.Position = v;
                }
                foreach (var l in hairLinks) Satisfy(l, 1);
                foreach (var p in Hair)
                {
                    if (p.Pinned) continue;
                    var v = p.Position; var envelope = new Vector3(v.x / .46f, (v.y - 2.08f) / .64f, (v.z - .15f) / .37f);
                    if (envelope.sqrMagnitude < 1) { envelope = envelope.sqrMagnitude < .000001f ? Vector3.forward : envelope.normalized; v = Vector3.Scale(envelope, new Vector3(.46f, .64f, .37f)) + new Vector3(0, 2.08f, .15f); }
                    v.z = Mathf.Max(v.z, .19f); p.Position = v;
                }
            }
        }
    }
}
