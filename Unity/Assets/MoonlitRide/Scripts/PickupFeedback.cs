using UnityEngine;

namespace MoonlitRide
{
    public sealed class PickupFeedback : MonoBehaviour
    {
        sealed class Effect { public Transform Root; public Transform[] Sparks; public TextMesh Label; public Vector3 Origin; public float Age = 10; }
        readonly Effect[] pool = new Effect[16];
        int next;
        Material material;
        public void Initialize()
        {
            material = Geometry.Material("#FFD686", 2);
            for (int i = 0; i < pool.Length; i++)
            {
                var e = new Effect { Root = new GameObject("Pickup feedback").transform, Sparks = new Transform[8] }; e.Root.SetParent(transform, false);
                for (int j = 0; j < e.Sparks.Length; j++) e.Sparks[j] = Geometry.Ball(e.Root, Vector3.zero, Vector3.one * .06f, material);
                e.Label = new GameObject("Label", typeof(TextMesh)).GetComponent<TextMesh>(); e.Label.transform.SetParent(e.Root, false);
                e.Label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                e.Label.GetComponent<Renderer>().sharedMaterial = e.Label.font.material;
                e.Label.anchor = TextAnchor.MiddleCenter; e.Label.alignment = TextAlignment.Center; e.Label.fontSize = 48; e.Label.characterSize = .035f; e.Label.color = new Color(1, .9f, .65f);
                e.Root.gameObject.SetActive(false); pool[i] = e;
            }
        }
        public void Burst(Vector3 position, bool bonus, bool reduced)
        {
            var e = pool[next]; next = (next + 1) % pool.Length; e.Age = 0; e.Origin = position; e.Root.position = position; e.Root.gameObject.SetActive(true);
            e.Label.text = bonus ? "+1  +5\nROW COMPLETE" : "+1";
            for (int j = 0; j < e.Sparks.Length; j++) { e.Sparks[j].localPosition = Vector3.zero; e.Sparks[j].gameObject.SetActive(!reduced); }
            e.Label.transform.localPosition = Vector3.up * .3f;
        }
        public void Tick(float dt, Camera camera, bool reduced)
        {
            foreach (var e in pool)
            {
                if (!e.Root.gameObject.activeSelf) continue;
                e.Age += dt;
                if (e.Age > 1.3f) { e.Root.gameObject.SetActive(false); continue; }
                e.Label.transform.rotation = camera.transform.rotation;
                e.Label.transform.localPosition = Vector3.up * (.3f + (reduced ? 0 : e.Age * .6f));
                for (int j = 0; j < e.Sparks.Length; j++)
                {
                    float a = j * Mathf.PI / 4;
                    e.Sparks[j].gameObject.SetActive(!reduced);
                    e.Sparks[j].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a) + .5f, Mathf.Sin(a * 3)) * e.Age * .8f;
                    e.Sparks[j].localScale = Vector3.one * (.06f * (1 - e.Age / 1.3f));
                }
            }
        }
        public void ResetEffects() { foreach (var e in pool) { e.Age = 10; e.Root.gameObject.SetActive(false); } next = 0; }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
