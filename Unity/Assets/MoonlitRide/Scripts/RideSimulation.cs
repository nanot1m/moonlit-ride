using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonlitRide
{
    // Same SI units, force model and 120 Hz integration as physics.js.
    [Serializable]
    public sealed class RideState
    {
        public float Progress, Distance, Lateral, Speed, Heading, Steering, Lean;
        public float WheelAngle, Cadence, Acceleration, BoostRemaining;
        public bool Contact;
        public void Boost() { BoostRemaining = 9; Speed = Mathf.Min(RideSimulation.MaximumSpeed, Speed + 10); }
    }

    public struct RideInput { public float Steer; public bool Brake, Coast, Pedal; }

    public static class Route
    {
        public const float Length = 720, Descent = 480;
        public static float Center(float z) => Mathf.Sin(z * .012f) * 19 + Mathf.Sin(z * .028f) * 5;
        public static float Elevation(float z)
        {
            float p = Mathf.Repeat(-z, Length);
            return p < Descent ? 8 + 11 * (1 + Mathf.Cos(Mathf.PI * p / Descent))
                : 8 + 11 * (1 - Mathf.Cos(Mathf.PI * (p - Descent) / (Length - Descent)));
        }
        public static float Slope(float z) => (Elevation(z + .05f) - Elevation(z - .05f)) / .1f;
        public static float Derivative(float z) => .228f * Mathf.Cos(z * .012f) + .14f * Mathf.Cos(z * .028f);
        public static float Curvature(float z) => (-.002736f * Mathf.Sin(z * .012f) - .00392f * Mathf.Sin(z * .028f)) / Mathf.Pow(1 + Derivative(z) * Derivative(z), 1.5f);
        // Reflect the original right-handed X axis at the rendering boundary.
        // Physics remains identical to the browser; screen-right is now original +X.
        public static Vector3 Position(float progress, float lateral = 0, float height = 0) => new Vector3(-Center(-progress) - lateral, Elevation(-progress) + height, -progress);
    }

    public static class RideSimulation
    {
        public const float StepTime = 1f / 120;
        public const float MaximumSpeed = 140f / 3.6f;
        public static void Step(RideState s, RideInput input, float dt = StepTime)
        {
            float z = -s.Progress;
            StepOnRoad(s, input, Route.Derivative(z), Route.Slope(z), Route.Curvature(z), dt, true);
        }
        public static void StepOnRoad(RideState s, RideInput input, float dx, float dy, float curvature, float dt = StepTime, bool fastRide = false)
        {
            float metric = Mathf.Sqrt(1 + dx * dx + dy * dy);
            s.BoostRemaining = input.Brake ? 0 : Mathf.Max(0, s.BoostRemaining - dt);
            float boostForce = 4 * Mathf.Pow(s.BoostRemaining / 9, 2);
            float power = input.Brake || input.Coast ? 0 : input.Pedal ? 620 : 230;
            float drive = Mathf.Min(input.Pedal ? 3.1f : 1.7f, power / (85 * Mathf.Max(s.Speed, 1.4f)));
            float gravity = 9.81f * dy / metric;
            float assist = !input.Brake && !input.Coast && dy < 0 ? Mathf.Max(0, -gravity) * .9f + Mathf.Max(0, 7.5f - s.Speed) * .22f : 0;
            s.Acceleration = drive + gravity + assist + boostForce - .065f - .0035f * s.Speed * s.Speed - (input.Brake ? 5.5f : 0);
            // The browser parity profile remains available to migration tests. The Unity ride
            // uses an assisted arcade cruise of 80 km/h, 90 while pedalling, 140 boosted.
            if (fastRide) {
                float cruiseTarget = s.BoostRemaining > 0 ? MaximumSpeed : (input.Pedal ? 90f : 80f) / 3.6f;
                float resistance = .065f + .0012f*s.Speed*s.Speed;
                float propulsion = input.Brake || input.Coast ? 0 : Mathf.Clamp((cruiseTarget-s.Speed)*.8f + resistance-gravity, -3f, 5.2f);
                s.Acceleration = propulsion + gravity + boostForce - resistance - (input.Brake ? 8f : 0);
            }
            float previousSpeed = s.Speed;
            s.Speed = Mathf.Clamp(s.Speed + s.Acceleration * dt, 0, fastRide ? MaximumSpeed : 32);
            if (input.Brake && s.Speed < .08f) s.Speed = 0;
            float steerLimit = .24f / (1 + s.Speed * .075f);
            s.Steering += (Mathf.Clamp(input.Steer, -1, 1) * steerLimit - s.Steering) * (1 - Mathf.Exp(-dt * 6));
            float yaw = s.Speed / 1.1f * Mathf.Tan(s.Steering) - s.Heading * (3.6f + s.Speed * .16f);
            s.Heading = Mathf.Clamp(s.Heading + yaw * dt, -.42f, .42f);
            if (s.Speed < .1f) s.Heading *= Mathf.Exp(-dt * 7);
            float travel = (previousSpeed + s.Speed) * .5f * dt;
            s.Lateral += s.Speed * Mathf.Sin(s.Heading) * dt;
            s.Contact = Mathf.Abs(s.Lateral) > 4.15f;
            if (s.Contact)
            {
                s.Lateral = Mathf.Clamp(s.Lateral, -4.15f, 4.15f);
                s.Speed *= Mathf.Exp(-dt * 5);
                if (Mathf.Sign(s.Heading) == Mathf.Sign(s.Lateral)) s.Heading *= Mathf.Exp(-dt * 18);
            }
            s.Progress += travel * Mathf.Cos(s.Heading) / metric;
            s.Distance += travel;
            float target = -Mathf.Clamp(s.Steering * Mathf.Min(s.Speed * .13f, 1.3f) + curvature * s.Speed * s.Speed * .018f, -.19f, .19f);
            s.Lean += (target - s.Lean) * (1 - Mathf.Exp(-dt * 4.5f));
            s.WheelAngle += travel / .52f;
            if (power > 0 && s.Speed > .1f) s.Cadence += Mathf.Min(10, 3 + s.Speed * .42f) * dt;
        }
    }

    public sealed class Pickup
    {
        public float Progress, Lane, RowEnd;
        public int Loop, Row = -1, Index;
        public bool Booster, Taken;
        public Transform Visual;
    }

    public static class PickupLayout
    {
        public static List<Pickup> ForChunk(int chunk)
        {
            float start = chunk * 24, end = start + 24;
            var result = new List<Pickup>();
            for (int loop = Mathf.FloorToInt(-end / 720) - 1; loop <= Mathf.FloorToInt(-start / 720) + 1; loop++)
            {
                for (int row = 0; row < 5; row++)
                {
                    float first = loop * 720 + (row + 1) * 80 - 7.5f;
                    for (int i = 0; i < 6; i++)
                    {
                        float p = first + i * 3;
                        if (-p >= start && -p < end) result.Add(new Pickup { Progress = p, Loop = loop, Row = row, Index = i, RowEnd = first + 15 });
                    }
                }
                float boost = loop * 720 + 478;
                if (-boost >= start && -boost < end) result.Add(new Pickup { Progress = boost, Booster = true });
            }
            float middle = start + 12;
            if (Mathf.Repeat(-middle, 720) > 483) result.Add(new Pickup { Progress = -middle, Lane = Mathf.Sin(chunk * 7) * 3 });
            return result;
        }
    }

    public sealed class RowTracker
    {
        struct Entry { public int Mask; public float End; }
        readonly Dictionary<long, Entry> rows = new Dictionary<long, Entry>();
        readonly List<long> expired = new List<long>();
        public int Count => rows.Count;
        public bool Collect(Pickup p)
        {
            if (p.Row < 0) return false;
            long key = (long)p.Loop * 5 + p.Row;
            rows.TryGetValue(key, out var entry);
            int before = entry.Mask;
            entry.Mask |= 1 << p.Index; entry.End = p.RowEnd; rows[key] = entry;
            return before != 63 && entry.Mask == 63;
        }
        public void Prune(float progress)
        {
            expired.Clear();
            foreach (var row in rows) if (progress > row.Value.End + 10) expired.Add(row.Key);
            foreach (long key in expired) rows.Remove(key);
        }
        public void Reset() => rows.Clear();
    }
}
