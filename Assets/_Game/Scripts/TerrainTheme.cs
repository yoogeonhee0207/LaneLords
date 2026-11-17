using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinySwordsBattle
{
    /// <summary>
    /// Recolours the battlefield to the stage's terrain colour (Tilemap_color1..5) at the start of a battle.
    /// Every ground tile, whatever colour it has now, is swapped for the same tile of the chosen colour.
    /// </summary>
    public class TerrainTheme : MonoBehaviour
    {
        public const int TilesPerColor = 44;

        public Tilemap ground;
        [Tooltip("All sliced tiles, colour by colour: index = (colour - 1) * 44 + tile number")]
        public TileBase[] tiles;

        void Start()
        {
            var bm = BattleManager.Instance;
            if (bm == null || bm.stage == null) return;
            Recolor(bm.stage.terrainColor);
        }

        public void Recolor(int color)
        {
            if (ground == null || tiles == null) return;
            color = Mathf.Clamp(color, 1, 5);
            if (tiles.Length < color * TilesPerColor) return;

            var lookup = new Dictionary<TileBase, int>();
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i] != null && !lookup.ContainsKey(tiles[i])) lookup.Add(tiles[i], i % TilesPerColor);

            foreach (Vector3Int pos in ground.cellBounds.allPositionsWithin)
            {
                TileBase current = ground.GetTile(pos);
                if (current == null || !lookup.TryGetValue(current, out int index)) continue;
                TileBase replacement = tiles[(color - 1) * TilesPerColor + index];
                if (replacement != null && replacement != current) ground.SetTile(pos, replacement);
            }
        }
    }
}
