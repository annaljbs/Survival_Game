using UnityEngine;

/// <summary>
/// Builds a playable prototype world at runtime from primitive sprites, so Play works
/// with the empty SampleScene. Once you have real prefabs/scenes, delete this file
/// (or disable the auto-run below).
/// </summary>
public static class Bootstrap
{
    static Sprite square;
    static Material litMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindFirstObjectByType<PlayerController>() != null) return; // scene already has a player

        // --- Player ---
        var player = CreateSprite("Player", Vector2.zero, new Color(0.95f, 0.85f, 0.3f), 0.8f);
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>().radius = 0.5f;
        var stats = player.AddComponent<SurvivalStats>();
        var inv = player.AddComponent<Inventory>();
        var controller = player.AddComponent<PlayerController>();

        // --- Camera ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.gameObject.AddComponent<CameraFollow>().target = player.transform;
        }

        // --- Systems ---
        new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
        new GameObject("GameManager").AddComponent<GameManager>().stats = stats;

        var missions = new GameObject("MissionManager").AddComponent<MissionManager>();
        missions.player = controller;
        missions.stats = stats;
        missions.inventory = inv;

        var hud = new GameObject("HUD").AddComponent<SurvivalHUD>();
        hud.stats = stats;
        hud.inventory = inv;
        hud.missions = missions;

        // --- Plentiful materials ---
        for (int i = 0; i < 30; i++) CreateResource("Tree", ItemType.Wood, new Color(0.35f, 0.2f, 0.1f), 1.0f, 2, 30f, 4f, 40f);
        for (int i = 0; i < 20; i++) CreateResource("Rock", ItemType.Stone, new Color(0.55f, 0.55f, 0.6f), 0.8f, 1, 30f, 4f, 40f);

        // --- Scarce food and water: few, far apart, slow to respawn ---
        // One of each within reach of the start so you can survive the first day.
        CreateResource("Berry Bush", ItemType.Berry, new Color(0.7f, 0.1f, 0.35f), 0.7f, 2, 300f, 10f, 14f);
        CreateResource("Water", ItemType.Water, new Color(0.2f, 0.5f, 0.9f), 1.4f, 1, 240f, 12f, 16f);
        for (int i = 0; i < 4; i++) CreateResource("Berry Bush", ItemType.Berry, new Color(0.7f, 0.1f, 0.35f), 0.7f, 2, 300f, 25f, 48f);
        for (int i = 0; i < 3; i++) CreateResource("Water", ItemType.Water, new Color(0.2f, 0.5f, 0.9f), 1.4f, 1, 240f, 28f, 48f);
    }

    static void CreateResource(string name, ItemType item, Color color, float size, int yield,
                               float respawn, float minDist, float maxDist)
    {
        Vector2 pos = Random.insideUnitCircle.normalized * Random.Range(minDist, maxDist);
        var go = CreateSprite(name, pos, color, size);
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        var node = go.AddComponent<ResourceNode>();
        node.item = item;
        node.yield = yield;
        node.respawnSeconds = respawn;
    }

    public static GameObject CreateSprite(string name, Vector2 pos, Color color, float size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        var mat = LitMaterial();
        if (mat != null) sr.sharedMaterial = mat;
        return go;
    }

    static Sprite Square()
    {
        if (square == null)
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return square;
    }

    // Lit material so sprites react to the day/night Light2D.
    static Material LitMaterial()
    {
        if (litMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null) litMaterial = new Material(shader);
        }
        return litMaterial;
    }
}
