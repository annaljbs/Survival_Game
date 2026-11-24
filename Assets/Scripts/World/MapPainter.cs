using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Paints the pixel-art world when the game starts: grass and dirt paths on the ground, and a 32x32 pixel
/// tree on every solid forest block. The tiles are created here (not saved in the scene), so the scene
/// file stays small. The walls (colliders) are made by the scene builder from the same MapData.
/// </summary>
public class MapPainter : MonoBehaviour
{
    public Material material;

    const string Floor = "Pixel/Tilesets/TilesetFloor";
    const string Nature = "Pixel/Tilesets/TilesetNature";

    void Awake()
    {
        var rng = new System.Random(11);   // fixed seed: the map always looks the same
        PaintGround(rng);
        PaintTrees(rng);
    }

    void PaintGround(System.Random rng)
    {
        // 16x16 pieces of the floor picture: plain grass (most), grass with details, plain dirt
        var grass = new[]
        {
            MakeTile(Floor, 0, 192, 16), MakeTile(Floor, 0, 192, 16), MakeTile(Floor, 0, 192, 16), MakeTile(Floor, 0, 192, 16),
            MakeTile(Floor, 16, 192, 16), MakeTile(Floor, 32, 192, 16), MakeTile(Floor, 48, 192, 16), MakeTile(Floor, 64, 192, 16),
        };
        var dirt = new[] { MakeTile(Floor, 128, 128, 16), MakeTile(Floor, 128, 144, 16), MakeTile(Floor, 128, 144, 16) };

        var tilemap = NewTilemap("Ground tiles", 1f, -45);
        int w = Mathf.RoundToInt(MapData.HalfW * 2), h = Mathf.RoundToInt(MapData.HalfH * 2);
        var positions = new Vector3Int[w * h];
        var tiles = new TileBase[w * h];
        int n = 0;
        for (int cy = -Mathf.RoundToInt(MapData.HalfH); cy < Mathf.RoundToInt(MapData.HalfH); cy++)
        {
            for (int cx = -Mathf.RoundToInt(MapData.HalfW); cx < Mathf.RoundToInt(MapData.HalfW); cx++)
            {
                var c = new Vector2(cx + 0.5f, cy + 0.5f);
                bool open = !MapData.IsBlocked(MapData.CellCenterOf(c));
                bool path = open && MapData.DistanceToPaths(c) <= MapData.PathHalfWidth + 1f;
                var set = path ? dirt : grass;
                positions[n] = new Vector3Int(cx, cy, 0);
                tiles[n] = set[rng.Next(set.Length)];
                n++;
            }
        }
        tilemap.SetTiles(positions, tiles);
    }

    void PaintTrees(System.Random rng)
    {
        // 32x32 pixel trees on top of the grass: round (3 kinds) and spruce-like (more of them, they look like a real forest).
        // Each tree is a little off its grid spot and some are mirrored, so there are no straight rows.
        var trees = new[]
        {
            PixelArt.Get(Nature, 0, 0, 32, 32), PixelArt.Get(Nature, 256, 0, 32, 32), PixelArt.Get(Nature, 288, 0, 32, 32),
            PixelArt.Get(Nature, 32, 0, 32, 32), PixelArt.Get(Nature, 32, 0, 32, 32), PixelArt.Get(Nature, 32, 0, 32, 32),
        };
        var bushes = new[] { PixelArt.Get(Nature, 0, 160, 16, 16), PixelArt.Get(Nature, 16, 160, 16, 16), PixelArt.Get(Nature, 48, 160, 16, 16) };

        var parent = new GameObject("Trees").transform;
        parent.SetParent(transform, false);

        int cols = Mathf.RoundToInt(MapData.HalfW * 2 / MapData.Cell);
        int rows = Mathf.RoundToInt(MapData.HalfH * 2 / MapData.Cell);
        // From the top row down, so trees further down are drawn in front of the ones above them.
        for (int j = rows - 1; j >= 0; j--)
        {
            for (int i = 0; i < cols; i++)
            {
                Vector2 c = MapData.CellCenter(i, j);
                if (!MapData.IsBlocked(c)) continue;

                // A bush instead of a tree now and then; trees along a path edge stay full size.
                bool bush = rng.NextDouble() < 0.12;
                var sprite = bush ? bushes[rng.Next(bushes.Length)] : trees[rng.Next(trees.Length)];
                float jx = (float)(rng.NextDouble() - 0.5) * 1.1f;
                float jy = (float)(rng.NextDouble() - 0.5) * 1.1f;

                var go = new GameObject(bush ? "Bush" : "Tree");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(c.x + jx, c.y + jy, 0f);
                if (bush) go.transform.localScale = Vector3.one * 1.25f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.flipX = rng.NextDouble() < 0.5;
                sr.sortingOrder = 1;
                if (material != null) sr.sharedMaterial = material;
            }
        }
    }

    Tilemap NewTilemap(string tilemapName, float cellSize, int sortingOrder)
    {
        var gridGo = new GameObject(tilemapName + " (grid)");
        gridGo.transform.SetParent(transform, false);
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 0f);

        var go = new GameObject(tilemapName);
        go.transform.SetParent(gridGo.transform, false);
        var tilemap = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = sortingOrder;
        if (material != null) tr.sharedMaterial = material;
        return tilemap;
    }

    static Tile MakeTile(string resource, int x, int y, int size)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = PixelArt.Get(resource, x, y, size, size);
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }
}
