using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoonlitRide.Editor
{
    public static class PortChecks
    {
        [Serializable] public sealed class ReferenceCase { public string name, mode; public float slope, speed, progress, lateral, lean, distance, boost; }
        [Serializable] public sealed class References { public ReferenceCase[] cases; }
        static void Check(bool ok, string name) { if (!ok) throw new Exception("Port regression: " + name); }
        static RideState Road(RideInput input, float slope, int steps, float speed = 0)
        {
            var s = new RideState { Speed = speed };
            for (int i = 0; i < steps; i++) RideSimulation.StepOnRoad(s, input, 0, slope, 0);
            return s;
        }
        [MenuItem("Moonlit Ride/Run port regression checks")]
        public static void Run()
        {
            for (int i = 0; i < 120; i++)
            {
                float angle = i * Mathf.PI / 60; var hip = new Vector3(.2f, 1.44f, .25f);
                var foot = new Vector3(.23f, .59f + Mathf.Sin(angle) * .2f, .1f + Mathf.Cos(angle) * .22f);
                var knee = BicycleView.SolveJoint(hip, foot, new Vector3(.3f, 1.1f, -1), .55f, .55f);
                Check(Mathf.Abs(Vector3.Distance(hip, knee) - .55f) < .001f && Mathf.Abs(Vector3.Distance(knee, foot) - .55f) < .001f, "pedal IK segment lengths");
            }
            Check(CoastalWorld.DistrictAt(0)==CoastalWorld.District.Market && CoastalWorld.DistrictAt(240)==CoastalWorld.District.Residential && CoastalWorld.DistrictAt(480)==CoastalWorld.District.Tourist,"three distinct waterfront districts");
            Check(CoastalWorld.DistrictAt(720)==CoastalWorld.DistrictAt(0) && CoastalWorld.DistrictAt(-1)==CoastalWorld.District.Tourist,"district loop wraps in both directions");
            int pierCount=0;for(int chunk=-30;chunk<0;chunk++)if(CoastalWorld.HasPier(chunk)){pierCount++;Check(CoastalWorld.DistrictAt(-chunk*24-12)==CoastalWorld.District.Tourist,"piers belong to marina district");}
            Check(pierCount==2,"two piers per coastal loop");
            for(int side=0;side<2;side++)for(int step=0;step<24;step++) {
                var foot=CityBicycle.Foot(side,step*Mathf.PI/12);
                Check(Mathf.Abs(new Vector2(foot.y-.59f,foot.z-.1f).magnitude-.21f)<.0001f,"shoe follows the pedal crank circle");
            }
            foreach(var name in new[]{"Frame","Steering","Wheel","Crank","Pedal"})Check(Resources.Load<GameObject>("Bicycle/"+name)!=null,"imported bicycle part "+name);
            var tree = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Nature/NormalTree_1"));
            var treeBounds=tree.GetComponentInChildren<Renderer>().bounds;
            Check(treeBounds.size.y>4 && treeBounds.size.y<10,"tree import preserves metre scale and upright axis");
            UnityEngine.Object.DestroyImmediate(tree);
            foreach(bool pedal in new[]{false,true}) {
                var fast=new RideState();
                for(int i=0;i<7200;i++) RideSimulation.StepOnRoad(fast,new RideInput{Pedal=pedal},0,-.1f,0,RideSimulation.StepTime,true);
                Check(fast.Speed*3.6f> (pedal?85:75) && fast.Speed*3.6f < (pedal?93:83),"fast cruise and uphill assist");
                fast.Boost();
                for(int i=0;i<600;i++) RideSimulation.StepOnRoad(fast,new RideInput{Pedal=true},0,0,0,RideSimulation.StepTime,true);
                Check(fast.Speed*3.6f>138,"booster reaches 140 km/h");
                for(int i=0;i<1200;i++) {RideSimulation.StepOnRoad(fast,new RideInput{Pedal=true},0,.2f,0,RideSimulation.StepTime,true);Check(fast.Speed<=RideSimulation.MaximumSpeed,"140 km/h speed cap");}
                for(int i=0;i<2400;i++) RideSimulation.StepOnRoad(fast,new RideInput{Brake=true},0,.2f,0,RideSimulation.StepTime,true);
                Check(fast.Speed==0 && fast.BoostRemaining==0,"high-speed downhill braking");
            }
            // Cloth must have only the waistband and hem as open boundaries, never a UV slit.
            var garment=Resources.Load<Mesh>("Rider/Dress"); var edgeUses=new Dictionary<ulong,int>();var triangles=garment.triangles;
            for(int t=0;t<triangles.Length;t+=3)for(int k=0;k<3;k++){uint a=(uint)triangles[t+k],b=(uint)triangles[t+(k+1)%3];ulong key=((ulong)Math.Min(a,b)<<32)|Math.Max(a,b);edgeUses.TryGetValue(key,out int count);edgeUses[key]=count+1;}
            var heights=garment.uv2;
            foreach(var edge in edgeUses)if(edge.Value==1){int a=(int)(edge.Key>>32),b=(int)(edge.Key&0xffffffff);Check((heights[a].x<.001f && heights[b].x<.001f)||(heights[a].x>.999f && heights[b].x>.999f),"no open dress texture seam");}
            var references = JsonUtility.FromJson<References>(File.ReadAllText("Assets/MoonlitRide/Editor/Fixtures/RideParity.json"));
            foreach (var expected in references.cases)
            {
                var actual = new RideState { Speed = 9 };
                var input = new RideInput { Pedal = expected.mode == "pedal", Brake = expected.mode == "brake", Coast = expected.mode == "coast", Steer = expected.mode == "turn" ? .7f : 0 };
                if (expected.mode == "boost") actual.Boost();
                for (int i = 0; i < 1200; i++) RideSimulation.StepOnRoad(actual, input, .2f, expected.slope, .001f);
                Check(Mathf.Abs(actual.Speed - expected.speed) < .005f && Mathf.Abs(actual.Progress - expected.progress) < .01f && Mathf.Abs(actual.Distance - expected.distance) < .01f && Mathf.Abs(actual.Lateral - expected.lateral) < .001f && Mathf.Abs(actual.Lean - expected.lean) < .001f && Mathf.Abs(actual.BoostRemaining - expected.boost) < .001f, "JS/C# parity " + expected.name);
            }
            Check(Road(new RideInput { Coast = true }, .1f, 600, 5).Speed > Road(new RideInput { Coast = true }, -.1f, 600, 5).Speed, "gravity direction");
            Check(Road(new RideInput { Brake = true }, .2f, 2400, 25).Speed == 0, "brake holds downhill");
            Check(Road(new RideInput { Brake = true, Steer = 1 }, 0, 600).Lateral == 0, "stationary steering");
            Check(Mathf.Abs(Road(new RideInput { Steer = 1 }, 0, 12000, 20).Lateral) <= 4.15f, "road edges");
            Check(Road(new RideInput { Pedal = true }, 0, 1200).Speed > Road(new RideInput(), 0, 1200).Speed, "pedal power");
            Check(Road(new RideInput(), -.12f, 2400).Speed > 5, "uphill assistance");
            var turn = Road(new RideInput { Steer = 1 }, 0, 120, 10);
            for (int i = 0; i < 600; i++) { RideSimulation.StepOnRoad(turn, new RideInput(), 0, 0, 0); Check(turn.Lean < .00001f, "no opposite lean"); }
            Check(Mathf.Abs(turn.Lean) < .002f, "upright settling");
            var boost = new RideState { Speed = 12 }; boost.Boost(); Check(boost.Speed == 22 && boost.BoostRemaining == 9, "boost kick");
            RideSimulation.Step(boost, new RideInput { Brake = true }); Check(boost.BoostRemaining == 0, "braking cancels boost");
            boost.Boost(); for (int i = 0; i < 1100; i++) RideSimulation.Step(boost, new RideInput()); Check(boost.BoostRemaining == 0, "boost expiry");
            Check(Mathf.Abs(Route.Elevation(0) - 30) < .001f && Mathf.Abs(Route.Elevation(-480) - 8) < .001f && Mathf.Abs(Route.Elevation(-720) - 30) < .001f, "route profile");
            foreach (float seam in new[] { 0f, -480f, -720f }) Check(Mathf.Abs(Route.Slope(seam)) < .001f, "smooth slope seams");
            var pickups = new List<Pickup>(); for (int i = -30; i < 0; i++) pickups.AddRange(PickupLayout.ForChunk(i));
            Check(pickups.FindAll(p => p.Row >= 0).Count == 30 && pickups.FindAll(p => p.Booster).Count == 1, "five rows plus booster");
            Check(pickups.Find(p => p.Booster).Progress == 478, "booster placement");
            var tracker = new RowTracker(); int awards = 0;
            foreach (var p in pickups) if (tracker.Collect(p)) awards++;
            Check(awards == 5, "complete row awards"); foreach (var p in pickups) Check(!tracker.Collect(p), "no duplicate awards"); tracker.Prune(800); Check(tracker.Count == 0, "bounded row tracking");
            tracker.Reset(); for (int i = 0; i < 5; i++) Check(!tracker.Collect(new Pickup { Row = 0, Index = i }), "incomplete row");
            for (int count = 1; count <= 10; count++) for (int i = 0; i < 4; i++)
            {
                double time = RideAudio.Beat * 16 - .1; int note = RideAudio.PickupNote(time, count, i);
                Check(Array.IndexOf(RideAudio.ChordAt(time + i * RideAudio.Beat / 4), note - 12) >= 0, "chord boundary membership");
            }
            var motion = new SecondaryMotion();
            for (int i = 0; i < 2400; i++) motion.Step(new RideState { Speed = 32, Acceleration = i % 100 < 50 ? -5 : 3, Lean = .19f, Distance = i / 4f }, RideSimulation.StepTime);
            foreach (var p in motion.Cloth) { Check(float.IsFinite(p.Position.sqrMagnitude) && p.Position.magnitude < 2, "finite bounded cloth"); if (p.Pinned) Check(p.Position == p.Rest, "pinned waist"); }
            foreach (var p in motion.Hair) Check(float.IsFinite(p.Position.sqrMagnitude) && p.Position.magnitude < 4, "finite bounded braid");
            for (int i = 1; i < motion.Hair.Count; i++) Check(Vector3.Distance(motion.Hair[i].Position, motion.Hair[i - 1].Position) < .12f, "braid stretch");
            motion.Reset(); foreach (var p in motion.Cloth) Check(p.Position == p.Rest && p.Previous == p.Rest, "cloth reset");
            Check(Mathf.Abs(SimulateFrames(30) - SimulateFrames(144)) < .02f, "frame rate independence");
            Debug.Log("MOONLIT_PORT_CHECKS_PASSED: 18 JS/C# parity fixtures, physics, route, rows, booster, harmony, cloth, braid, frame rates");
        }
        static float SimulateFrames(int fps)
        {
            var s = new RideState(); double accumulator = 0;
            for (int i = 0; i < fps * 20; i++) { accumulator += 1.0 / fps; while (accumulator + 1e-8 >= RideSimulation.StepTime) { RideSimulation.Step(s, new RideInput()); accumulator -= RideSimulation.StepTime; } }
            return s.Distance;
        }
    }
}
