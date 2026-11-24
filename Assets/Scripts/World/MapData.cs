using UnityEngine;

/// <summary>
/// The shape of the map: which places are open (clearings), where the paths run, and which spots are solid forest.
/// Used by the scene builder (colliders) and by MapPainter (pixel-art ground and trees), so both always agree.
/// To add a place or a road: add a rectangle to Clearings or a polyline to Paths, then rebuild the scene.
/// </summary>
public static class MapData
{
    public const float HalfW = 120f, HalfH = 90f;   // the map is 240 x 180 units
    public const float Cell = 2f;                    // one forest block = one 32x32 pixel tree
    public const float PathHalfWidth = 3.5f;         // paths are about 7 units wide
    public const float EdgeBand = 9f;                // forest wall thickness at the map edge (always solid)

    // Open areas (x, y, width, height). Everything outside these and the paths is solid forest.
    public static readonly Rect[] Clearings =
    {
        new Rect(-27, -21, 50, 34),     // crash site
        new Rect(30, -4, 24, 34),       // meadow by the stream (camp A, dry grass, stream)
        new Rect(-21, 33, 34, 22),      // forest edge (camp B, dead twigs)
        new Rect(-34, 43, 20, 16),      // log field
        new Rect(-90, 21, 16, 14),      // birch grove
        new Rect(61, -16, 18, 12),      // foot of the hill (climb up)
        new Rect(90, 62, 16, 16),       // rescue point (far north-east)
        new Rect(-14, 16, 20, 14),      // camp D2 (open ground on the way north)
    };

    // The forest shortcut to the rescue point: shorter than the other routes, but slow and tiring (TerrainZone).
    public static readonly Vector2[] TrailThick = { new Vector2(50, 28), new Vector2(72, 40), new Vector2(88, 56), new Vector2(96, 66) };

    // Paths: polylines of waypoints, kept free of trees. (TrailThick must be declared above.)
    public static readonly Vector2[][] Paths =
    {
        new[] { new Vector2(20, -2), new Vector2(32, 4) },                                              // crash site <-> meadow
        new[] { new Vector2(0, 10), new Vector2(-2, 24), new Vector2(-4, 34) },                         // crash site -> north
        new[] { new Vector2(-24, -4), new Vector2(-45, 6), new Vector2(-62, 18), new Vector2(-76, 25) },// crash site -> birch grove
        new[] { new Vector2(50, 0), new Vector2(58, -8), new Vector2(64, -9) },                         // meadow -> hill
        new[] { new Vector2(57, -14), new Vector2(57, 56) },                                            // along the stream (north-south)
        new[] { new Vector2(57, 50), new Vector2(57, 70), new Vector2(90, 70) },                        // stream -> rescue point
        new[] { new Vector2(12, 46), new Vector2(30, 50), new Vector2(50, 50), new Vector2(57, 50) },   // forest edge -> stream (long way round)
        new[] { new Vector2(-22, -16), new Vector2(-40, -24), new Vector2(-54, -28) },                  // dead end to the lake
        new[] { new Vector2(12, -20), new Vector2(24, -30) },                                           // dead end to the big rock
        TrailThick,                                                                                     // forest shortcut to the rescue point
    };

    /// <summary>Centre of the forest cell (column i, row j) from the bottom-left corner of the map.</summary>
    public static Vector2 CellCenter(int i, int j)
    {
        return new Vector2(-HalfW + Cell * (i + 0.5f), -HalfH + Cell * (j + 0.5f));
    }

    /// <summary>Centre of the forest cell that contains this point.</summary>
    public static Vector2 CellCenterOf(Vector2 p)
    {
        int i = Mathf.FloorToInt((p.x + HalfW) / Cell);
        int j = Mathf.FloorToInt((p.y + HalfH) / Cell);
        return CellCenter(i, j);
    }

    /// <summary>True if this spot is solid forest (not a clearing, not a path, or part of the map-edge wall).</summary>
    public static bool IsBlocked(Vector2 p)
    {
        if (Mathf.Abs(p.x) > HalfW - EdgeBand || Mathf.Abs(p.y) > HalfH - EdgeBand) return true;
        foreach (var r in Clearings) if (r.Contains(p)) return false;
        return DistanceToPaths(p) > PathHalfWidth;
    }

    public static float DistanceToPaths(Vector2 p)
    {
        float best = float.MaxValue;
        foreach (var path in Paths)
            for (int i = 0; i < path.Length - 1; i++)
                best = Mathf.Min(best, DistanceToSegment(p, path[i], path[i + 1]));
        return best;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
