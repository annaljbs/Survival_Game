using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel-art flame and rising smoke for a Campfire. The flame flickers while the fire burns and shrinks
/// when it is almost out. Pictures: FX/Particle/Fire (flame) and FX/Smoke (puffs) of the Ninja Adventure pack.
/// Add it to the same object as the Campfire. The material should be an unlit sprite material (glows at night).
/// </summary>
[RequireComponent(typeof(Campfire))]
public class CampfireVisual : MonoBehaviour
{
    public Material unlitMaterial;
    public float smokeEverySeconds = 0.8f;

    const string FirePath = "Pixel/Fx/Fire";
    const string SmokePath = "Pixel/Fx/Smoke";

    class Puff
    {
        public SpriteRenderer sr;
        public float age;
    }

    Campfire fire;
    SpriteRenderer flame;
    Sprite[] flameFrames;
    Sprite[] smokeFrames;
    readonly List<Puff> puffs = new List<Puff>();
    float smokeTimer;

    static readonly int[] FlameLoop = { 3, 4, 5, 6, 7, 6, 5, 4 };   // frames of the flame picture (8x12 pixels each)

    void Awake()
    {
        fire = GetComponent<Campfire>();

        flameFrames = new Sprite[12];
        for (int i = 0; i < 12; i++) flameFrames[i] = PixelArt.Get(FirePath, i * 8, 0, 8, 12);
        smokeFrames = new Sprite[6];
        for (int i = 0; i < 6; i++) smokeFrames[i] = PixelArt.Get(SmokePath, i * 32, 0, 32, 32);
        if (flameFrames[0] == null || smokeFrames[0] == null) { enabled = false; return; }

        var go = new GameObject("Flame");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        flame = go.AddComponent<SpriteRenderer>();
        flame.sortingOrder = 7;
        if (unlitMaterial != null) flame.sharedMaterial = unlitMaterial;
        flame.enabled = false;
    }

    void Update()
    {
        bool lit = fire.IsLit;
        flame.enabled = lit;
        if (lit)
        {
            // flicker: about 9 frames per second; a nearly dead fire (under 20 minutes) gets small
            int step = (int)(Time.time * 9f) % FlameLoop.Length;
            flame.sprite = flameFrames[FlameLoop[step]];
            float size = Mathf.Lerp(1f, 2f, Mathf.Clamp01(fire.TimeLeft / 30f));
            flame.transform.localScale = new Vector3(size, size, 1f);

            smokeTimer += Time.deltaTime;
            if (smokeTimer >= smokeEverySeconds)
            {
                smokeTimer = 0f;
                SpawnPuff();
            }
        }
        UpdatePuffs();
    }

    void SpawnPuff()
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), 0.9f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;
        if (unlitMaterial != null) sr.sharedMaterial = unlitMaterial;
        sr.sprite = smokeFrames[0];
        puffs.Add(new Puff { sr = sr });
    }

    void UpdatePuffs()
    {
        const float lifetime = 2.4f;
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var p = puffs[i];
            p.age += Time.deltaTime;
            if (p.age >= lifetime)
            {
                Destroy(p.sr.gameObject);
                puffs.RemoveAt(i);
                continue;
            }
            float t = p.age / lifetime;
            p.sr.sprite = smokeFrames[Mathf.Min(5, (int)(t * 6f))];
            var tr = p.sr.transform;
            tr.localPosition += new Vector3(0.12f * Time.deltaTime, 0.7f * Time.deltaTime, 0f);   // drifts up and a little sideways
            tr.localScale = Vector3.one * Mathf.Lerp(0.4f, 0.9f, t);
            p.sr.color = new Color(1f, 1f, 1f, 0.85f * (1f - t));
        }
    }
}
