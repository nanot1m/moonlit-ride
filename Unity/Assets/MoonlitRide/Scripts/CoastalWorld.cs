using System.Collections.Generic;
using UnityEngine;

namespace MoonlitRide
{
    public sealed partial class CoastalWorld : MonoBehaviour
    {
        public sealed class Chunk { public int Index; public District Area; public bool HasPier; public Transform Root; public List<Pickup> Pickups; }
        public readonly List<Chunk> Chunks = new List<Chunk>();
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> sourceMeshes = new List<Mesh>();
        Material road, trim, dark, gold, roof, leaves, terrain, glow, mint, flowers;
        Material[] walls;
        Transform ocean, backdrop;
        Material water;
        public void SetTime(float time) => water.SetFloat("_RideTime", time);
        Mesh roofMesh, ring;
        NatureAssets nature = new NatureAssets();
        public void Initialize()
        {
            road = Mat("#B4A395"); trim = Mat("#E8CEAA"); dark = Mat("#23354A"); gold = Mat("#BF9554");
            roof = Mat("#9B554C"); leaves = Mat("#35675C"); terrain = Mat("#405D52"); glow = Mat("#FFD070", 1.8f); mint = Mat("#91FFE0", 2);
            flowers = Mat("#F05C54");
            walls = new[] { Mat("#EFB958"), Mat("#EACBB4"), Mat("#244ED5"), Mat("#F0D985"), Mat("#4976DB") };
            // Fine warm paving reads as a continuous promenade, not oversized tiles.
            road.color = new Color(.66f, .58f, .42f); road.SetFloat("_Glossiness", .26f);
            roofMesh = Own(Geometry.Cone(4.7f, 3.1f, 4)); ring = Own(Geometry.Torus(.48f, .035f));
            water = new Material(Shader.Find("MoonlitRide/Water")); materials.Add(water);
            ocean = Geometry.Box(transform, new Vector3(430, -1.3f, 0), new Vector3(900, .1f, 1400), water);
            ocean.gameObject.AddComponent<WaterReflection>();
            backdrop = new GameObject("Moon and distant headlands").transform; backdrop.SetParent(transform, false);
            Geometry.Ball(backdrop, new Vector3(190, 125, -480), Vector3.one * 16, Mat("#FFF0D4", 1));
            for (int layer = 0; layer < 1; layer++)
            {
                var vertices = new List<Vector3>(); var indices = new List<int>();
                for (int i = 0; i <= 110; i++)
                {
                    float x = -750 + i * 8, h = 3 + Mathf.Sin(i * .11f + layer) * 2 + Mathf.Sin(i * .27f + layer);
                    vertices.Add(new Vector3(x, -2, -330 - layer * 100)); vertices.Add(new Vector3(x, h, -330 - layer * 100));
                    if (i < 110) { int a = i * 2; indices.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
                }
                var ridge = Own(Geometry.Mesh(vertices.ToArray(), indices.ToArray())); Geometry.ReflectX(ridge);
                Geometry.MeshObject("Headland", backdrop, ridge, Mat(new[] { "#34435E", "#3A4965", "#44516B" }[layer]));
            }
            terrain = new Material(Shader.Find("MoonlitRide/CoastalGround")); materials.Add(terrain);
            InitializeDistricts();
            Stream(0);
        }
        Material Mat(string color, float emission = 0) { var m = Geometry.Material(color, emission); materials.Add(m); return m; }
        Mesh Own(Mesh mesh) { sourceMeshes.Add(mesh); return mesh; }
        public void Stream(float progress)
        {
            int current = Mathf.FloorToInt(-progress / 24);
            for (int i = Chunks.Count - 1; i >= 0; i--)
                if (Chunks[i].Index > current + 3 || Chunks[i].Index < current - 19) { Dispose(Chunks[i]); Chunks.RemoveAt(i); }
            for (int n = current - 19; n <= current + 3; n++)
                if (!Chunks.Exists(c => c.Index == n)) Chunks.Add(Create(n));
            foreach (var c in Chunks) { var light = c.Root.GetComponentInChildren<Light>(); if (light) light.enabled = Mathf.Abs(c.Index - current) < 3; }
            ocean.position = new Vector3(430, -1.3f, -progress); backdrop.position = new Vector3(0, 0, -progress);
        }
        public void ResetWorld() { foreach (var c in Chunks) Dispose(c); Chunks.Clear(); Stream(0); }
        void Dispose(Chunk c)
        {
            foreach (var f in c.Root.GetComponentsInChildren<MeshFilter>())
                if (f.transform.parent.name == "Static scenery") Destroy(f.sharedMesh);
            c.Root.gameObject.SetActive(false); Destroy(c.Root.gameObject);
        }
        Chunk Create(int n)
        {
            var root = new GameObject("Coast " + n).transform; root.SetParent(transform, false);
            var scenery = new GameObject("Static scenery").transform; scenery.SetParent(root, false);
            float start = n * 24;
            var area=DistrictAt(-start-12); bool hasPier=HasPier(n);
            root.name += " - " + area;
            var points = new List<Vector3>(); var triangles = new List<int>(); var roadUV = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
            {
                float z = start + i * 2;
                points.Add(new Vector3(Route.Center(z) - 5, Route.Elevation(z), z)); points.Add(new Vector3(Route.Center(z) + 5, Route.Elevation(z), z));
                roadUV.Add(new Vector2(0, i / 6f)); roadUV.Add(new Vector2(1.5f, i / 6f));
                if (i < 12) { int k = i * 2; triangles.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 }); }
            }
            Mesh roadMesh = Geometry.Mesh(points.ToArray(), triangles.ToArray(), roadUV.ToArray()); roadMesh.RecalculateTangents(); Geometry.MeshObject("Road", scenery, roadMesh, road);
            // Terraced coastal bank, rising inland rather than floating road pieces.
            points.Clear(); triangles.Clear();
            const int bankColumns=11;
            for (int j = 0; j <= 12; j++) {
                float z=start+j*2, shore=Shore(z);
                float[] columns={shore-8,shore,Mathf.Lerp(shore,-6.2f,.38f),Mathf.Lerp(shore,-6.2f,.72f),-6.2f,5.1f,8,18,40,80,170};
                for(int k=0;k<bankColumns;k++) {
                    float off=columns[k];points.Add(new Vector3(Route.Center(z)+off,BankHeight(z,off),z));
                    if(j<12 && k<bankColumns-1) {int a=j*bankColumns+k;triangles.AddRange(new[]{a,a+bankColumns,a+1,a+1,a+bankColumns,a+bankColumns+1});}
                }
            }
            Mesh bankMesh = Geometry.Mesh(points.ToArray(), triangles.ToArray()); Geometry.MeshObject("Coastal bank", scenery, bankMesh, terrain);
            AddWalkway(scenery, start, -6.1f, -4.95f); AddWalkway(scenery, start, 4.95f, 7);
            for (int j = 0; j < 24; j += 3)
            {
                float z = start + j, x = Route.Center(z), y = Route.Elevation(z);
                if(hasPier && j==12)continue;
                
                
                Geometry.Rod(scenery, new Vector3(x - 5.7f, y, z), new Vector3(x - 5.7f, y + 1.25f, z), .07f, dark);
                Geometry.Rod(scenery, new Vector3(x - 5.7f, y + 1.15f, z), new Vector3(Route.Center(z + 3) - 5.7f, Route.Elevation(z + 3) + 1.15f, z + 3), .05f, gold);
            }
            float lz = start + 4, lx = Route.Center(lz) - 4.8f, ly = Route.Elevation(lz);
            Geometry.Rod(scenery, new Vector3(lx, ly, lz), new Vector3(lx, ly + 5.3f, lz), .085f, dark);
            Geometry.Box(scenery, new Vector3(lx, ly + 5.3f, lz), new Vector3(.6f, .8f, .6f), glow);
            Geometry.Box(scenery, new Vector3(lx, ly + 5.8f, lz), new Vector3(.9f, .15f, .9f), dark);
            for (int j = 0; j < 3; j++) {
                if(j==1 && (area==District.Residential || area==District.Tourist && n%2==0)) { Plaza(scenery,start+j*8,area);continue; }
                Building(scenery, start + j * 8, n * 3 + j,area);
            }
            DistrictDetails(scenery,start,area);
            if(hasPier) Pier(scenery,start+13,n);
            // Mixed tree groups follow the terrain, with open views between clusters.
            string[] treeNames={"NormalTree_1","PineTree_2","NormalTree_3","MapleTree_1","PineTree_4"};
            for(int j=0;j<5;j++) {
                float z=start+2+j*4.6f, off=22+Hash(n*17+j)*19;
                nature.Place(scenery,treeNames[(int)(Hash(n*31+j)*treeNames.Length)],new Vector3(Route.Center(z)+off,BankHeight(z,off)-.1f,z),1.2f+Hash(n*13+j)*.9f,Hash(n*41+j)*360);
            }
            for(int j=0;j<2;j++) if(Hash(n*7+j)>.12f && !hasPier) {
                float z=start+4+j*12+Hash(n+j)*3,off=-7.7f-Hash(n*3+j)*1.2f;
                nature.Place(scenery,treeNames[(int)(Hash(n*37+j)*3)],new Vector3(Route.Center(z)+off,BankHeight(z,off)-.1f,z),1.25f+Hash(n*5+j)*.5f,Hash(n*29+j)*360);
            }
            for(int j=0;j<8;j++) {
                float z=start+Hash(n*89+j*11)*24; if(hasPier && Mathf.Abs(z-start-13)<2.5f)continue; float off=Mathf.Lerp(Shore(z)+1,-7.5f,Hash(n*53+j*7));
                nature.Place(scenery,j%2==0?"Rock_1":"Rock_2",new Vector3(Route.Center(z)+off,BankHeight(z,off)-.35f,z),1.3f+Hash(n*23+j)*3,Hash(n*19+j)*360);
                if(j%2==0) nature.Place(scenery,"Bush_Large",new Vector3(Route.Center(z)+off+1,BankHeight(z,off+1)-.05f,z),.7f+Hash(n*67+j),Hash(n+j)*360);
            }
            for(int j=0;j<12;j++) {
                float z=start+j*2+.4f; if(hasPier && Mathf.Abs(z-start-13)<2.5f)continue; float off=-6.9f-Hash(n*79+j)*2.5f;
                nature.Place(scenery,"Grass_Large",new Vector3(Route.Center(z)+off,BankHeight(z,off),z),.8f+Hash(n*43+j),Hash(n*17+j)*360);
            }
            if(!hasPier && n%3==0) {
                float z=start+13;Boat(scenery,new Vector3(Route.Center(z)+Shore(z)-12,-1.05f,z),n%2==0?0:1,Hash(n)*32-16,.8f);
            }
            var lamp = new GameObject("Warm lantern pool", typeof(Light)).GetComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.position = new Vector3(-lx, ly + 4.9f, lz); lamp.type = LightType.Point; lamp.color = new Color(1, .76f, .40f); lamp.intensity = 3.2f; lamp.range = 13; lamp.renderMode = LightRenderMode.ForcePixel;
            if (n % 2 == 0) {
                float tz=start+15, tx=Route.Center(tz)-5.6f, ty=Route.Elevation(tz);
                Geometry.Box(scenery,new Vector3(tx,ty+.35f,tz),new Vector3(1.05f,.7f,1.8f),roof);
                for(int f=0;f<9;f++) Geometry.Ball(scenery,new Vector3(tx+Mathf.Sin(f*2.4f)*.4f,ty+.85f,tz+Mathf.Cos(f*2.4f)*.7f),Vector3.one*.45f,f%3==0?flowers:leaves);

            }
            Geometry.Combine(scenery, true); Destroy(roadMesh); Destroy(bankMesh);
            var pickups = PickupLayout.ForChunk(n);
            foreach (var p in pickups)
            {
                var node = new GameObject(p.Booster ? "Mint booster" : "Firefly").transform; node.SetParent(root, false); node.position = Route.Position(p.Progress, p.Lane, 1.5f);
                Geometry.Ball(node, Vector3.zero, Vector3.one * (p.Booster ? .6f : .34f), p.Booster ? mint : glow);
                if (p.Booster) { var t = Geometry.MeshObject("Booster ring", node, ring, mint); t.localRotation = Quaternion.Euler(0, 90, 0); }
                p.Visual = node;
            }
            return new Chunk { Index = n, Area=area, HasPier=hasPier, Root = root, Pickups = pickups };
        }
        static float Hash(int seed) {return Mathf.Repeat(Mathf.Sin(seed*127.1f+311.7f)*43758.5453f,1);}
        public static float Shore(float z) => -26-Mathf.Sin(z*.021f)*8-Mathf.Sin(z*.057f)*4;
        public static float BankHeight(float z,float off) {
            float elevation=Route.Elevation(z);
            if(off < -6.2f) {
                float shore=Shore(z),t=Mathf.InverseLerp(shore,-6.2f,off);
                if(off<shore)return -.6f+(off-shore)*.42f;
                float bank=Mathf.Lerp(-.6f,elevation-.3f,t)+Mathf.Sin(t*Mathf.PI)*Mathf.Sin(z*.31f+off*.5f)*1.6f;
                int chunk=Mathf.FloorToInt(z/24);float pierZ=chunk*24+13,sideDistance=Mathf.Abs(z-pierZ);
                if(HasPier(chunk) && sideDistance<3.5f) {
                    float center=Route.Center(pierZ),worldX=Route.Center(z)+off;
                    float stairT=Mathf.InverseLerp(center-6,center+Shore(pierZ)+.5f,worldX);
                    float cut=Mathf.Lerp(Route.Elevation(pierZ)+.17f,-.15f,stairT)-.48f;
                    bank=Mathf.Lerp(bank,Mathf.Min(bank,cut),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.3f,3.5f,sideDistance)));
                }
                return bank;
            }
            return elevation-.3f+Mathf.Max(0,off-13)*.12f+(off>18?Mathf.Sin(z*.024f+off*.055f)*off*.035f:0);
        }
        void AddWalkway(Transform parent, float start, float left, float right)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            // Continuous bevelled profile swept along the actual road elevation.
            Vector2[] profile = { new Vector2(left, -.12f), new Vector2(left, .12f), new Vector2(left + .045f, .17f), new Vector2(right - .045f, .17f), new Vector2(right, .12f), new Vector2(right, -.12f) };
            for (int j = 0; j <= 24; j++) foreach (var p in profile) v.Add(new Vector3(Route.Center(start + j) + p.x, Route.Elevation(start + j) + p.y, start + j));
            for (int j = 0; j < 24; j++) for (int k = 0; k < 5; k++) { int a = j * 6 + k; t.AddRange(new[] { a, a + 6, a + 1, a + 1, a + 6, a + 7 }); }
            var mesh = Geometry.Mesh(v.ToArray(), t.ToArray()); Geometry.MeshObject("Continuous stone promenade", parent, mesh, trim); Destroy(mesh, .1f);
        }
        void Building(Transform p, float z, int index,District area)
        {
            int i = ((index % 15) + 15) % 15;
            bool home=area==District.Residential, visitor=area==District.Tourist;
            float w=home?4.6f+i%2:5+i%3, h=home?5.1f+i%3*1.2f:visitor?8+i%3*2:6.5f+i%4*1.35f;
            float x=Route.Center(z)+(home?12:visitor?11:10)+(i%3)*.35f,ground=Route.Elevation(z);
            var facade=home?homeWalls[i%homeWalls.Length]:visitor?hotelWalls[i%hotelWalls.Length]:walls[i%walls.Length];
            Geometry.Box(p, new Vector3(x, ground + h / 2, z), new Vector3(w, h, 7), facade);
            Geometry.Box(p, new Vector3(x, ground + h, z), new Vector3(w + .4f, .3f, 7.4f), trim);
            if(visitor && i%3==0) RoofTerrace(p,x,ground+h,z,w);
            else {
                var top=Geometry.MeshObject("Varied pitched roof",p,home?gableRoof:roofMesh,i%2==0?roof:slate);
                top.localPosition=new Vector3(x,ground+h,z);
                top.localRotation=Quaternion.Euler(0,home?0:45,0);
                top.localScale=home?new Vector3(w+.5f,1.9f,7.6f):new Vector3(w/6, .65f+i%3*.17f,1);
                Geometry.Box(p,new Vector3(x+w*.22f,ground+h+1.5f,z+1.8f),new Vector3(.65f,1.8f,.65f),trim);
            }
            // Pale pilasters, cornices and curved gables give the waterfront a varied silhouette.
            for (int side = -1; side <= 1; side += 2)
                Geometry.Box(p, new Vector3(x-w/2-.14f, ground+h/2, z+side*3.2f), new Vector3(.24f,h,.18f), trim);
            for (float floor = 3.35f; floor < h; floor += 2.7f)
                Geometry.Box(p,new Vector3(x-w/2-.18f,ground+floor,z),new Vector3(.36f,.13f,7.2f),trim);
            if (!home && i % 3 != 1) {
                var gv=new List<Vector3> {new Vector3(x-w/2-.10f,ground+h,z)};var gt=new List<int>();
                for(int k=0;k<=16;k++){float a=k*Mathf.PI/16;gv.Add(new Vector3(x-w/2-.10f,ground+h+Mathf.Sin(a)*2,z+Mathf.Cos(a)*3.15f));if(k>0)gt.AddRange(new[]{0,k,k+1});}
                var gm=Geometry.Mesh(gv.ToArray(),gt.ToArray());Geometry.MeshObject("Curved waterfront gable",p,gm,facade);Destroy(gm,.1f);
                for (int k=0;k<16;k++) {
                    float a=k/16f*Mathf.PI,b=(k+1)/16f*Mathf.PI;
                    Geometry.Rod(p,new Vector3(x-w/2-.12f,ground+h+Mathf.Sin(a)*2.0f,z+Mathf.Cos(a)*3.15f),new Vector3(x-w/2-.12f,ground+h+Mathf.Sin(b)*2.0f,z+Mathf.Cos(b)*3.15f),.09f,trim);
                }
                Geometry.Ball(p,new Vector3(x-w/2-.12f,ground+h+2.15f,z),Vector3.one*.36f,gold);
            }
            for (float y = home?2:4.6f; y < h - 1; y += 2.7f)
            {
                foreach (float dz in new[] { -1.8f, 1.8f })
                {
                    Geometry.Box(p, new Vector3(x - w / 2 - .08f, ground + y, z + dz), new Vector3(.16f, 1.8f, 1.3f), trim);
                    Geometry.Box(p, new Vector3(x - w / 2 - .18f, ground + y, z + dz), new Vector3(.1f, 1.4f, .9f), home && (i+(int)y)%3==0?dark:glow);
                    Geometry.Box(p, new Vector3(x - w / 2 - .25f, ground + y, z + dz), new Vector3(.1f, 1.4f, .06f), dark);
                    Geometry.Box(p, new Vector3(x - w / 2 - .25f, ground + y, z + dz), new Vector3(.1f, .065f, .9f), dark);
                    for (int a=0;a<10;a++) {
                        float t=a*Mathf.PI/10, u=(a+1)*Mathf.PI/10;
                        Geometry.Rod(p,new Vector3(x-w/2-.20f,ground+y+.63f+Mathf.Sin(t)*.57f,z+dz+Mathf.Cos(t)*.62f),new Vector3(x-w/2-.20f,ground+y+.63f+Mathf.Sin(u)*.57f,z+dz+Mathf.Cos(u)*.62f),.055f,trim);
                    }
                    if (y < 6) {
                        Geometry.Box(p, new Vector3(x - w / 2 - .33f, ground + y - 1, z + dz), new Vector3(.6f, .32f, 1.45f), roof);
                        for (int f = 0; f < 5; f++) Geometry.Ball(p, new Vector3(x - w / 2 - .4f, ground + y - .77f, z + dz - .5f + f * .25f), Vector3.one * .30f, flowers);
                    }
                    foreach (int side in new[] { -1, 1 }) Geometry.Box(p, new Vector3(x - w / 2 - .2f, ground + y, z + dz + side * .74f), new Vector3(.15f, 1.6f, .35f), leaves);
                }
                foreach (float dx in new[] { -1.5f, 1.5f })
                {
                    Geometry.Box(p, new Vector3(x + dx, ground + y, z + 3.54f), new Vector3(1.2f, 1.8f, .12f), trim);
                    Geometry.Box(p, new Vector3(x + dx, ground + y, z + 3.64f), new Vector3(.85f, 1.4f, .12f), glow);
                    Geometry.Box(p, new Vector3(x + dx, ground + y, z + 3.72f), new Vector3(.06f, 1.4f, .1f), dark);
                    Geometry.Box(p, new Vector3(x + dx, ground + y, z + 3.72f), new Vector3(.9f, .065f, .1f), dark);
                }
            }
            if(home) HomeDetails(p,x-w/2,ground,z,i);
            else ShopFront(p,x-w/2,ground,z,i,visitor);
            if(home || visitor) Balcony(p,x-w/2,ground+(home?3.25f:4.0f),z,visitor?5.8f:3.8f);
        }
        void OnDestroy() { nature.Dispose(); foreach (var m in materials) Destroy(m); foreach (var m in sourceMeshes) Destroy(m); }
    }
}
