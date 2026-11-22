using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Sets the forest up at runtime from the painted tilemaps:
//  - builds invisible walls (on the "Walls" layer) over shrub masses, lakes and border trees,
//  - registers the individual bushes on the Objects tilemap as hiding spots,
//  - fixes sorting so characters aren't drawn under the ground and Y-sort with objects.
//
// It adds itself automatically if it isn't in the scene. To tweak settings, add it
// to the Grid (or any object) and change the values in the Inspector.
public class ForestSetup : MonoBehaviour
{
    [Header("Tilemaps (found by name if left empty)")]
    public Tilemap groundTilemap;      // "Tilemap": grass, paths, shrub masses, water
    public Tilemap objectsTilemap;     // "Objects": trees, logs, rocks, single bushes

    [Header("Walls")]
    [Tooltip("Tiles on the ground tilemap that block movement (matched by tile asset name).")]
    public string[] blockingTileNames = { "grasssutff_1", "water_0", "water1_0", "treee_0" };
    public string wallLayerName = "Walls";

    [Header("Rocks")]
    [Tooltip("Rock tiles (on either tilemap) that get a collider shaped to the rock.")]
    public string[] rockTileNames = { "MuahroomShit_4", "MuahroomShit_8" };
    [Tooltip("Collider size as a fraction of the rock sprite (width, height). Height is measured from the bottom, so you can walk a little 'behind' the top of the rock.")]
    public Vector2 rockColliderScale = new Vector2(0.7f, 0.45f);

    [Header("Logs")]
    [Tooltip("Fallen log tiles (on either tilemap) that get a collider shaped to the log.")]
    public string[] logTileNames = { "MuahroomShit_0" };
    [Tooltip("Collider size as a fraction of the log sprite (width, height), centred on the log.")]
    public Vector2 logColliderScale = new Vector2(0.8f, 0.3f);

    [Header("Hiding bushes")]
    [Tooltip("Tile on the Objects tilemap that the player can hide in.")]
    public string bushTileName = "grasssutff_1";
    [Tooltip("How close to the bush's middle the player must be to count as hidden.")]
    public float hideRadius = 0.75f;

    [Header("Sorting")]
    public bool fixSorting = true;
    [Tooltip("Ground tilemap goes below everything else.")]
    public int groundSortingOrder = -10;

    static bool bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoCreate()
    {
        if (bootstrapped || FindAnyObjectByType<ForestSetup>() != null)
            return;
        new GameObject("ForestSetup (auto)").AddComponent<ForestSetup>();
    }

    void Awake()
    {
        bootstrapped = true;

        if (groundTilemap == null) groundTilemap = FindTilemap("Tilemap");
        if (objectsTilemap == null) objectsTilemap = FindTilemap("Objects");

        if (groundTilemap == null)
            Debug.LogWarning("ForestSetup: no ground tilemap found (expected one named 'Tilemap').");
        else
            BuildWalls();

        BuildObjectColliders("Rocks (auto)", rockTileNames, rockColliderScale, true);
        BuildObjectColliders("Logs (auto)", logTileNames, logColliderScale, false);

        if (objectsTilemap != null)
            RegisterBushes();

        if (fixSorting)
            FixSorting();
    }

    static Tilemap FindTilemap(string objectName)
    {
        foreach (Tilemap t in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            if (t.name == objectName)
                return t;
        return null;
    }

    void BuildWalls()
    {
        var blocking = new HashSet<string>(blockingTileNames);

        // A separate, invisible tilemap that only holds colliders.
        var go = new GameObject("Walls (auto)");
        go.transform.SetParent(groundTilemap.transform.parent, false);
        go.transform.localPosition = groundTilemap.transform.localPosition;

        int layer = LayerMask.NameToLayer(wallLayerName);
        if (layer >= 0) go.layer = layer;
        else Debug.LogWarning("ForestSetup: layer '" + wallLayerName + "' not found; walls use Default.");

        var walls = go.AddComponent<Tilemap>();
        walls.tileAnchor = groundTilemap.tileAnchor;

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        var tileCollider = go.AddComponent<TilemapCollider2D>();
        tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        Tile wallTile = ScriptableObject.CreateInstance<Tile>();
        wallTile.sprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        wallTile.colliderType = Tile.ColliderType.Grid;

        var cells = new List<Vector3Int>();
        foreach (Vector3Int pos in groundTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = groundTilemap.GetTile(pos);
            if (tile != null && blocking.Contains(tile.name))
                cells.Add(pos);
        }

        var tiles = new TileBase[cells.Count];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = wallTile;
        walls.SetTiles(cells.ToArray(), tiles);

        composite.GenerateGeometry();
    }

    // One box collider per matching tile, sized to its sprite. Bottom-aligned boxes sit on
    // the lower part of the sprite (rocks); others are centred on it (logs).
    void BuildObjectColliders(string groupName, string[] tileNames, Vector2 scale, bool bottomAligned)
    {
        var rockNames = new HashSet<string>(tileNames);
        var parent = new GameObject(groupName);
        int layer = LayerMask.NameToLayer(wallLayerName);
        if (layer >= 0) parent.layer = layer;

        int count = 0;
        foreach (Tilemap map in new[] { groundTilemap, objectsTilemap })
        {
            if (map == null) continue;

            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                TileBase tile = map.GetTile(pos);
                if (tile == null || !rockNames.Contains(tile.name))
                    continue;

                Sprite sprite = map.GetSprite(pos);
                if (sprite == null) continue;

                // Sprite bounds in world space (includes the tile's own transform and the tilemap's).
                Matrix4x4 m = map.transform.localToWorldMatrix * Matrix4x4.Translate(map.GetCellCenterLocal(pos))
                    * map.GetTransformMatrix(pos);
                Bounds b = sprite.bounds;
                Vector2 min = m.MultiplyPoint3x4(b.min);
                Vector2 max = m.MultiplyPoint3x4(b.max);
                Vector2 size = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
                Vector2 bottomCenter = new Vector2((min.x + max.x) * 0.5f, Mathf.Min(min.y, max.y));

                var go = new GameObject(tile.name + " " + pos.x + "," + pos.y);
                go.layer = parent.layer;
                go.transform.SetParent(parent.transform, false);

                // Scale along the sprite's own axes, then rotate, so a log painted
                // vertically gets a tall thin box instead of a squashed square.
                Vector3 alongX = m.MultiplyVector(new Vector3(b.size.x * scale.x, 0f, 0f));
                Vector3 alongY = m.MultiplyVector(new Vector3(0f, b.size.y * scale.y, 0f));
                Vector2 colSize = new Vector2(Mathf.Abs(alongX.x) + Mathf.Abs(alongY.x),
                                              Mathf.Abs(alongX.y) + Mathf.Abs(alongY.y));
                go.transform.position = bottomAligned
                    ? bottomCenter + new Vector2(0f, colSize.y * 0.5f)
                    : (Vector2)((min + max) * 0.5f);

                var box = go.AddComponent<BoxCollider2D>();
                box.size = colSize;
                box.edgeRadius = Mathf.Min(colSize.x, colSize.y) * 0.4f; // well-rounded corners so you slide around instead of snagging
                box.size = colSize - Vector2.one * box.edgeRadius * 2f;
                count++;
            }
        }

        if (count == 0)
            Debug.LogWarning("ForestSetup: no tiles found for " + groupName + ".");
    }

    void RegisterBushes()
    {
        int count = 0;
        foreach (Vector3Int pos in objectsTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = objectsTilemap.GetTile(pos);
            if (tile == null || tile.name != bushTileName)
                continue;

            PlayerHiding.RegisterBush(objectsTilemap.GetCellCenterWorld(pos), hideRadius);
            count++;
        }

        // Give the player the hiding component.
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null && player.GetComponent<PlayerHiding>() == null)
            player.gameObject.AddComponent<PlayerHiding>();

        if (count == 0)
            Debug.LogWarning("ForestSetup: no '" + bushTileName + "' bushes found on the Objects tilemap.");
    }

    void FixSorting()
    {
        // The ground was drawn above NPCs and items (order 1 vs 0), hiding them.
        if (groundTilemap != null)
        {
            var r = groundTilemap.GetComponent<TilemapRenderer>();
            if (r != null) r.sortingOrder = groundSortingOrder;
        }

        // Characters and objects share one order and are sorted by Y,
        // so you can walk behind a tree and in front of it.
        int actorOrder = 5;
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null)
        {
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) actorOrder = sr.sortingOrder;
        }

        if (objectsTilemap != null)
        {
            var r = objectsTilemap.GetComponent<TilemapRenderer>();
            if (r != null)
            {
                r.mode = TilemapRenderer.Mode.Individual;
                r.sortingOrder = actorOrder;
            }
        }

        foreach (NPCGuard npc in FindObjectsByType<NPCGuard>(FindObjectsInactive.Include))
            foreach (SpriteRenderer sr in npc.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sortingOrder < actorOrder)
                    sr.sortingOrder = actorOrder;
    }

    void OnDestroy()
    {
        bootstrapped = false;
        PlayerHiding.ClearBushes();
    }
}
