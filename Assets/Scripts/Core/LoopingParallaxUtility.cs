using UnityEngine;

/// <summary>Shared creation and camera-follow logic for tiled parallax layers.</summary>
public static class LoopingParallaxUtility
{
    public sealed class Layer
    {
        public Transform Root;
        public Transform[] Tiles;
        public float Parallax;
        public float TileWorldWidth;
        public float RootStartX;
    }

    public static Layer CreateLayer(
        Transform parent, Camera camera, string name, Sprite sprite,
        float parallax, float z, int sortingOrder, string logPrefix)
    {
        Layer layer = new Layer { Parallax = parallax };
        if (sprite == null)
        {
            Debug.LogWarning($"[{logPrefix}] {name} sprite is null — skipping.");
            return layer;
        }

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, z);

        float scale = camera.orthographicSize * 2f / sprite.bounds.size.y;
        float tileWidth = sprite.bounds.size.x * scale;
        Transform[] tiles = new Transform[3];
        for (int i = 0; i < tiles.Length; i++)
        {
            GameObject tile = new GameObject($"{name}_Tile{i}");
            tile.transform.SetParent(root.transform);
            tile.transform.localPosition = new Vector3((i - 1) * tileWidth, 0f, 0f);
            tile.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            tiles[i] = tile.transform;
        }

        layer.Root = root.transform;
        layer.Tiles = tiles;
        layer.TileWorldWidth = tileWidth;
        layer.RootStartX = root.transform.position.x;
        Debug.Log($"[{logPrefix}] {name}: pos=({root.transform.position.x:F1},{root.transform.position.y:F1},{z}), scale={scale:F2}, tileW={tileWidth:F2}, sortOrder={sortingOrder}");
        return layer;
    }

    public static void UpdateLayers(Transform camera, float cameraStartX, Layer[] layers)
    {
        if (camera == null || layers == null) return;
        foreach (Layer layer in layers)
        {
            if (layer == null || layer.Root == null || layer.Tiles == null || layer.TileWorldWidth <= 0f)
                continue;

            float rootX = layer.RootStartX + (camera.position.x - cameraStartX) * layer.Parallax;
            layer.Root.position = new Vector3(rootX, layer.Root.position.y, layer.Root.position.z);
            float centerSlot = Mathf.Round((camera.position.x - rootX) / layer.TileWorldWidth);
            for (int i = 0; i < layer.Tiles.Length; i++)
            {
                float slotOffset = (i - 1) + centerSlot;
                layer.Tiles[i].localPosition = new Vector3(slotOffset * layer.TileWorldWidth, 0f, 0f);
            }
        }
    }

    public static void DisablePlaceholderBackground(string logPrefix)
    {
        GameObject background = GameObject.Find("Background");
        SpriteRenderer renderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
        if (renderer == null) return;
        renderer.enabled = false;
        Debug.Log($"[{logPrefix}] Disabled old Background SpriteRenderer.");
    }
}
