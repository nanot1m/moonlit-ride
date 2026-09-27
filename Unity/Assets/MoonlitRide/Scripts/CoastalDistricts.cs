using System.Collections.Generic;
using UnityEngine;
namespace MoonlitRide {
    public sealed partial class CoastalWorld {
        public enum District { Market, Residential, Tourist }
        public static District DistrictAt(float progress) => (District)Mathf.FloorToInt(Mathf.Repeat(progress,720)/240);
        public static bool HasSideStreet(int chunk) {
            var area=DistrictAt(-chunk*24-12); int slot=((chunk%10)+10)%10;
            return area==District.Market ? slot==3 || slot==8 : area==District.Residential ? slot%2==0 : slot%3==0;
        }
        public static bool HasBuilding(int chunk,int slot) {
            var area=DistrictAt(-chunk*24-12);
            if(slot==1 && HasSideStreet(chunk))return false;
            if(area==District.Market)return true;
            if(area==District.Residential)return slot==0 || slot==2 && chunk%3==0;
            return slot!=1 && (Route.Waterfront(chunk*24+12)<.8f || slot==0);
        }
        public static bool HasPier(int chunk) {
            int slot=(((-chunk-1)%10)+10)%10;
            return DistrictAt(-chunk*24-12)==District.Tourist && (slot==1 || slot==5);
        }
        Material wood,slate,canvas,glass,rope;
        Material[] homeWalls,hotelWalls,accents;
        Mesh gableRoof,hull,sail,parasol;
        void InitializeDistricts() {
            wood=Mat("#77533B");slate=Mat("#344653");canvas=Mat("#F4E6C9");glass=Mat("#547D89");rope=Mat("#CAB58B");
            homeWalls=new[]{Mat("#D8BAAC"),Mat("#DDD2B2"),Mat("#A7BAAF"),Mat("#C1B8C5")};
            hotelWalls=new[]{Mat("#F2E0BA"),Mat("#E6BC83"),Mat("#91AABF")};
            accents=new[]{Mat("#AB4646"),Mat("#327E7E"),Mat("#D6A348")};
            gableRoof=Own(Geometry.Mesh(new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(0,1,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(0,1,.5f)},new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4}));
            sail=Own(Geometry.Mesh(new[]{new Vector3(0,0,0),new Vector3(0,4.6f,0),new Vector3(0,.25f,2.4f)},new[]{0,1,2,2,1,0}));
            parasol=Own(Geometry.Cone(1.25f,.55f,10));
            var v=new List<Vector3>();var t=new List<int>();
            float[] zs={-2.5f,-1.6f,.3f,1.7f,2.3f}, widths={.02f,.78f,.92f,.72f,.30f};
            for(int j=0;j<zs.Length;j++) {
                v.Add(new Vector3(-widths[j],.38f,zs[j]));v.Add(new Vector3(-widths[j]*.68f,-.15f,zs[j]));
                v.Add(new Vector3(0,-.40f,zs[j]));v.Add(new Vector3(widths[j]*.68f,-.15f,zs[j]));v.Add(new Vector3(widths[j],.38f,zs[j]));
                if(j>0)for(int k=0;k<4;k++){int a=(j-1)*5+k;t.AddRange(new[]{a,a+5,a+1,a+1,a+5,a+6});}
            }
            for(int k=0;k<t.Count;k+=3){int swap=t[k+1];t[k+1]=t[k+2];t[k+2]=swap;}
            t.AddRange(new[]{20,21,24,21,23,24,21,22,23});
            hull=Own(Geometry.Mesh(v.ToArray(),t.ToArray()));
        }
        void Balcony(Transform p,float front,float y,float z,float length) {
            Geometry.Box(p,new Vector3(front-.52f,y,z),new Vector3(1.15f,.15f,length),trim);
            Geometry.Rod(p,new Vector3(front-1.05f,y+.85f,z-length/2),new Vector3(front-1.05f,y+.85f,z+length/2),.045f,dark);
            for(float dz=-length/2;dz<=length/2;dz+=.36f)Geometry.Rod(p,new Vector3(front-1.05f,y,dz+z),new Vector3(front-1.05f,y+.85f,dz+z),.025f,dark);
            foreach(int side in new[]{-1,1}) Geometry.Rod(p,new Vector3(front-1.05f,y+.85f,z+side*length/2),new Vector3(front,y+.85f,z+side*length/2),.045f,dark);
        }
        void RoofTerrace(Transform p,float x,float y,float z,float w) {
            Geometry.Box(p,new Vector3(x,y+.15f,z),new Vector3(w+.35f,.3f,7.3f),trim);
            foreach(int side in new[]{-1,1}) {
                Geometry.Box(p,new Vector3(x+side*w/2,y+.55f,z),new Vector3(.16f,.8f,7),hotelWalls[0]);
                Geometry.Box(p,new Vector3(x,y+.55f,z+side*3.5f),new Vector3(w,.8f,.16f),hotelWalls[0]);
            }
            for(int i=0;i<4;i++)Geometry.Rod(p,new Vector3(x-w*.3f,y+.3f,z-2+i*1.3f),new Vector3(x-w*.3f,y+2.3f,z-2+i*1.3f),.07f,wood);
            Geometry.Box(p,new Vector3(x,y+2.4f,z),new Vector3(w*.75f,.12f,5),canvas);
        }
        void ShopFront(Transform p,float front,float ground,float z,int id,bool tourist) {
            // Wide display windows and a recessed entrance replace the repeated domestic ground floor.
            foreach(float dz in new[]{-2.1f,2.1f}) {
                Geometry.Box(p,new Vector3(front-.12f,ground+1.5f,z+dz),new Vector3(.20f,2.3f,2.25f),dark);
                Geometry.Box(p,new Vector3(front-.24f,ground+1.5f,z+dz),new Vector3(.08f,1.95f,1.95f),glow);
                for(int shelf=0;shelf<(tourist?0:2);shelf++) {
                    Geometry.Box(p,new Vector3(front-.34f,ground+.9f+shelf*.7f,z+dz),new Vector3(.12f,.07f,1.9f),wood);
                    for(int item=0;item<4;item++)Geometry.Box(p,new Vector3(front-.37f,ground+1.08f+shelf*.7f,z+dz-.65f+item*.43f),new Vector3(.16f,.30f,.23f),accents[(item+id)%3]);
                }
            }
            Geometry.Box(p,new Vector3(front-.15f,ground+1.2f,z),new Vector3(.25f,2.4f,1.15f),wood);
            Geometry.Box(p,new Vector3(front-.30f,ground+1.55f,z),new Vector3(.06f,1.2f,.78f),glass);
            Geometry.Ball(p,new Vector3(front-.36f,ground+1.05f,z+.36f),Vector3.one*.08f,gold);
            Geometry.Box(p,new Vector3(front-.2f,ground+3.15f,z),new Vector3(.28f,.5f,6.6f),accents[id%3]);
            for(int stripe=0;stripe<12;stripe++) {
                var awning=Geometry.Box(p,new Vector3(front-.95f,ground+2.83f,z-3.05f+stripe*.55f),new Vector3(1.9f,.10f,.55f),stripe%2==0?canvas:accents[id%3]);awning.localRotation=Quaternion.Euler(0,0,12);
                Geometry.Box(p,new Vector3(front-1.86f,ground+2.52f,z-3.05f+stripe*.55f),new Vector3(.08f,.30f,.55f),stripe%2==0?canvas:accents[id%3]);
            }
            if(tourist) {
                CafeTable(p,front-1.0f,ground,z-2,false,id);
                CafeTable(p,front-1.0f,ground,z+2,false,id+1);
                // A vertical hotel sign with three brass stars, readable as a landmark at speed.
                Geometry.Box(p,new Vector3(front-.5f,ground+6.4f,z+3.15f),new Vector3(.7f,2.2f,.22f),accents[1]);
                for(int k=0;k<3;k++)Geometry.Ball(p,new Vector3(front-.87f,ground+5.8f+k*.6f,z+3.15f),new Vector3(.08f,.24f,.24f),gold);
            } else {
                foreach(int side in new[]{-1,1}) {
                    float dz=z+side*2.5f;
                    Geometry.Box(p,new Vector3(front-1.1f,ground+.45f,dz),new Vector3(.8f,.7f,1.1f),wood);
                    for(int item=0;item<6;item++)Geometry.Ball(p,new Vector3(front-1.1f+(item%2)*.25f-.12f,ground+.86f,dz+(item/2-1)*.3f),Vector3.one*.24f,item%2==0?flowers:gold);
                }
            }
        }
        void HomeDetails(Transform p,float front,float ground,float z,int id) {
            Geometry.Box(p,new Vector3(front-.1f,ground+1.15f,z),new Vector3(.25f,2.3f,1.15f),accents[id%3]);
            for(int step=0;step<3;step++)Geometry.Box(p,new Vector3(front-.4f-step*.3f,ground+.12f*(3-step),z),new Vector3(.35f,.24f*(3-step),1.5f),trim);
            float fence=Route.Center(z)+7.25f;
            foreach(int side in new[]{-1,1}) {
                Geometry.Box(p,new Vector3((front+fence)/2,ground+.28f,z+side*2.5f),new Vector3(front-fence,.4f,1.1f),terrain);
                nature.Place(p,"Bush_Large",new Vector3(fence+.65f,ground+.25f,z+side*2.6f),.9f,id*43);
                for(int k=0;k<7;k++)Geometry.Box(p,new Vector3(fence,ground+.6f,z+side*(.9f+k*.37f)),new Vector3(.10f,1.1f,.12f),canvas);
                Geometry.Box(p,new Vector3(fence,ground+.72f,z+side*2.0f),new Vector3(.10f,.10f,2.6f),canvas);
            }
            // Domestic details: wall lamp, mailbox, a bench inside the garden.
            Geometry.Box(p,new Vector3(front-.25f,ground+2.35f,z-1),new Vector3(.3f,.4f,.3f),glow);
            Geometry.Box(p,new Vector3(fence,ground+1.0f,z-.7f),new Vector3(.4f,.4f,.3f),accents[id%3]);
            Bench(p,new Vector3(front-1,ground,z+1.5f));
        }
        void Bench(Transform p,Vector3 pos) {
            Geometry.Box(p,pos+Vector3.up*.5f,new Vector3(.65f,.12f,1.6f),wood);
            Geometry.Box(p,pos+new Vector3(.28f,.90f,0),new Vector3(.1f,.65f,1.6f),wood);
            foreach(int side in new[]{-1,1})Geometry.Box(p,pos+new Vector3(0,.25f,side*.6f),new Vector3(.45f,.5f,.08f),dark);
        }
        void CafeTable(Transform p,float x,float y,float z,bool umbrella,int id) {
            Geometry.Rod(p,new Vector3(x,y,z),new Vector3(x,y+.78f,z),.065f,dark);
            Geometry.Ball(p,new Vector3(x,y+.8f,z),new Vector3(1,.10f,1),wood);
            foreach(int side in new[]{-1,1}) {
                Geometry.Box(p,new Vector3(x,y+.42f,z+side*.76f),new Vector3(.42f,.09f,.42f),canvas);
                Geometry.Rod(p,new Vector3(x,y,z+side*.76f),new Vector3(x,y+.4f,z+side*.76f),.055f,dark);
            }
            if(umbrella) {
                Geometry.Rod(p,new Vector3(x,y,z),new Vector3(x,y+2.5f,z),.045f,wood);
                var canopy=Geometry.MeshObject("Cafe parasol",p,parasol,accents[id%3]);canopy.localPosition=new Vector3(x,y+2.15f,z);
            }
        }
        void Plaza(Transform p,float z,District area) {
            float x=Route.Center(z)+11,y=Route.Elevation(z);
            Geometry.Box(p,new Vector3(x,y-.18f,z),new Vector3(8,.7f,7.5f),trim);
            if(area==District.Tourist) {
                for(int k=0;k<3;k++)CafeTable(p,x-1+(k%2)*3,y+.2f,z-2+k*2,true,k);
                Geometry.Box(p,new Vector3(x+3,y+.6f,z),new Vector3(.4f,1.5f,7),leaves);
            } else {
                nature.Place(p,"NormalTree_3",new Vector3(x+1,y+.15f,z),1.1f,20);
                Bench(p,new Vector3(x-2,y+.18f,z-1));Bench(p,new Vector3(x-2,y+.18f,z+2));
            }
        }
        void SideStreet(Transform p,float z,District area) {
            // A real opening between facades, rising inland with the terrain.
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int k=0;k<=18;k++) {
                float offset=5.05f+k*2.5f;
                foreach(float side in new[]{-1f,1f}) {
                    float zz=z+side*2.85f;
                    vertices.Add(new Vector3(Route.Center(zz)+offset,BankHeight(zz,offset)+.32f,zz));
                }
                if(k<18){int a=k*2;indices.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
            }
            var mesh=Geometry.Mesh(vertices.ToArray(),indices.ToArray());Geometry.MeshObject("Side street to the upper town",p,mesh,road);Destroy(mesh,.1f);
            for(int k=0;k<18;k++) {
                float off=6.3f+k*2.5f;
                foreach(float side in new[]{-1f,1f}) {
                    float zz=z+side*3.12f;
                    Geometry.Box(p,new Vector3(Route.Center(zz)+off,BankHeight(zz,off)+.34f,zz),new Vector3(2.52f,.20f,.4f),trim);
                }
            }
            float x=Route.Center(z)+6.5f,y=Route.Elevation(z);
            Geometry.Rod(p,new Vector3(x,y,z+3.35f),new Vector3(x,y+2.3f,z+3.35f),.055f,dark);
            Geometry.Box(p,new Vector3(x,y+2.1f,z+3.35f),new Vector3(.12f,.42f,1.45f),accents[1]);
            // Set-back houses face the side street instead of filling the waterfront gap.
            foreach(int side in new[]{-1,1}) {
                float off=27,zz=z+side*6.5f,baseY=BankHeight(zz,off);
                Geometry.Box(p,new Vector3(Route.Center(zz)+off,baseY+2.3f,zz),new Vector3(7,4.6f,5.5f),homeWalls[side<0?0:2]);
                var top=Geometry.MeshObject("Lane cottage roof",p,gableRoof,roof);top.localPosition=new Vector3(Route.Center(zz)+off,baseY+4.6f,zz);top.localScale=new Vector3(7.4f,1.7f,5.9f);
            }
        }
        void DistrictDetails(Transform p,float start,District area) {
            float z=start+20,x=Route.Center(z)+6.3f,y=Route.Elevation(z);
            if(area==District.Market) {
                // Lantern bunting stays above the sidewalk, outside the cycle corridor.
                for(int k=0;k<8;k++) {
                    float zz=start+2+k*2.7f,xx=Route.Center(zz)+6.8f,yy=Route.Elevation(zz)+3.5f;
                    Geometry.Ball(p,new Vector3(xx,yy,zz),Vector3.one*.20f,k%2==0?glow:accents[0]);
                    if(k<7)Geometry.Rod(p,new Vector3(xx,yy+.15f,zz),new Vector3(Route.Center(zz+2.7f)+6.8f,Route.Elevation(zz+2.7f)+3.65f,zz+2.7f),.012f,dark);
                }
            } else if(area==District.Tourist) {
                // Information kiosk and telescope facing the bay.
                Geometry.Rod(p,new Vector3(x,y,z),new Vector3(x,y+2.3f,z),.075f,dark);
                Geometry.Box(p,new Vector3(x,y+1.65f,z),new Vector3(.16f,1.1f,.9f),accents[1]);
                Geometry.Box(p,new Vector3(x-.10f,y+1.65f,z),new Vector3(.04f,.8f,.65f),canvas);
                float seaX=Route.Center(z)-5.4f;
                Geometry.Rod(p,new Vector3(seaX,y,z),new Vector3(seaX,y+1.35f,z),.065f,dark);
                Geometry.Rod(p,new Vector3(seaX+.2f,y+1.25f,z),new Vector3(seaX-.4f,y+1.55f,z),.10f,gold);
            }
        }
        void Pier(Transform p,float z,int id) {
            float center=Route.Center(z),land=center-6.0f,edge=center+Shore(z)+.5f,end=edge-18,deck=-.15f;
            float high=Route.Elevation(z)+.17f;
            // At the low waterfront, extend the staircase over water rather than
            // compressing all steps into the narrow strip between road and sea.
            edge=Mathf.Min(edge,land-(high-deck)*1.35f); end=edge-18;
            int steps=Mathf.CeilToInt((high-deck)/.21f);
            for(int k=0;k<steps;k++) {
                float t=(k+.5f)/steps,x=Mathf.Lerp(land,edge,t),y=Mathf.Lerp(high,deck,t),run=(land-edge)/steps;
                Geometry.Box(p,new Vector3(x,y-.16f,z),new Vector3(run+.015f,.32f,2.3f),trim);
                if(k%5==0)foreach(int side in new[]{-1,1}) {
                    Geometry.Rod(p,new Vector3(x,y,z+side*1.1f),new Vector3(x,y+.9f,z+side*1.1f),.045f,dark);
                }
            }
            foreach(int side in new[]{-1,1})Geometry.Rod(p,new Vector3(land,high+.9f,z+side*1.1f),new Vector3(edge,deck+.9f,z+side*1.1f),.04f,gold);
            for(float x=edge;x>end;x-=.45f)Geometry.Box(p,new Vector3(x,deck,z),new Vector3(.42f,.22f,2.7f),wood);
            for(float zz=z-8;zz<=z+8;zz+=.45f)Geometry.Box(p,new Vector3(end,deck,zz),new Vector3(3,.22f,.42f),wood);
            for(float x=edge;x>=end;x-=3)foreach(int side in new[]{-1,1}) {
                Geometry.Rod(p,new Vector3(x,-3,z+side*1.15f),new Vector3(x,.5f,z+side*1.15f),.12f,wood);
                Geometry.Ball(p,new Vector3(x,.55f,z+side*1.15f),Vector3.one*.22f,rope);
            }
            for(int k=0;k<4;k++) {
                float zz=z-6+k*4;
                Geometry.Rod(p,new Vector3(end,-3,zz),new Vector3(end,.55f,zz),.14f,wood);
                Boat(p,new Vector3(end-3.6f,-1.05f,zz),k%3,90,.72f);
                Geometry.Rod(p,new Vector3(end,.38f,zz),new Vector3(end-2.2f,-.35f,zz),.018f,rope);
            }
            foreach(int side in new[]{-1,1}) {
                Geometry.Rod(p,new Vector3(end,deck,z+side*7.5f),new Vector3(end,deck+2.5f,z+side*7.5f),.075f,dark);
                Geometry.Box(p,new Vector3(end,deck+2.5f,z+side*7.5f),Vector3.one*.34f,glow);
            }
            Geometry.Box(p,new Vector3(edge-3,deck+.4f,z+.6f),new Vector3(.8f,.7f,.7f),wood);
        }
        void Boat(Transform p,Vector3 position,int kind,float yaw,float scale) {
            var boat=new GameObject(kind==0?"Moored sailboat":kind==1?"Fishing launch":"Runabout").transform;boat.SetParent(p,false);boat.localPosition=position;boat.localRotation=Quaternion.Euler(0,yaw,0);boat.localScale=Vector3.one*scale;
            Geometry.MeshObject("Shaped hull",boat,hull,kind==1?accents[1]:canvas);
            Geometry.Box(boat,new Vector3(0,.20f,.15f),new Vector3(1.25f,.16f,3.3f),wood);
            foreach(int side in new[]{-1,1})Geometry.Rod(boat,new Vector3(side*.7f,.4f,-1.4f),new Vector3(side*.7f,.4f,1.6f),.055f,trim);
            if(kind==0) {
                Geometry.Rod(boat,new Vector3(0,.25f,-.5f),new Vector3(0,5.4f,-.5f),.045f,wood);
                var cloth=Geometry.MeshObject("Raised sail",boat,sail,canvas);cloth.localPosition=new Vector3(0,.65f,-.5f);
                Geometry.Rod(boat,new Vector3(0,.4f,-2),new Vector3(0,5.2f,-.5f),.012f,rope);
                Geometry.Rod(boat,new Vector3(0,.4f,2),new Vector3(0,5.2f,-.5f),.012f,rope);
            } else if(kind==1) {
                Geometry.Box(boat,new Vector3(0,.8f,-.6f),new Vector3(1.1f,1.1f,1.2f),canvas);
                Geometry.Box(boat,new Vector3(0,1.0f,-1.22f),new Vector3(.8f,.45f,.06f),glass);
                Geometry.Box(boat,new Vector3(0,1.42f,-.6f),new Vector3(1.4f,.12f,1.5f),accents[0]);
                Geometry.Box(boat,new Vector3(0,.5f,1),new Vector3(.8f,.5f,.8f),wood);
            } else {
                foreach(float zz in new[]{-.5f,.8f})Geometry.Box(boat,new Vector3(0,.45f,zz),new Vector3(1.25f,.15f,.45f),accents[0]);
                Geometry.Box(boat,new Vector3(0,.6f,-1.1f),new Vector3(1.1f,.45f,.08f),glass);
                Geometry.Box(boat,new Vector3(0,.1f,2.2f),new Vector3(.4f,.7f,.35f),dark);
            }
            foreach(int side in new[]{-1,1})for(int k=0;k<2;k++)Geometry.Ball(boat,new Vector3(side*.83f,.13f,k*1.4f-.4f),new Vector3(.17f,.48f,.17f),canvas);
        }
    }
}
