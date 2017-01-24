using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {

    public Material material;
    public static Material materials;
    public static Transform me;

    //render Distance
    public int viewRange = 10;

    public void Awake()
    {
        Block.blocks.Add(new Block("dirt", "Dirt Block", 2, 15));
        Block.blocks.Add(new Block("stone", "Stone Block", 1, 15));
        Block.blocks.Add(new Block("grassblock", "Grass Block", 7, 2, 3, 15, 2, 15));

        materials = material;
        me = transform;
    }

    public void Update()
    {
        for (int x = -(viewRange / 2); x < (viewRange / 2); x++)
        {
            for (int z = -(viewRange / 2); z < (viewRange / 2); z++)
            {
                Vector3 cPos = new Vector3(x * Chunk.Width, 0, z * Chunk.Width);
                if (Chunk.ChunkExists(cPos))
                {
                    continue;
                }

                Chunk.AddChunk(cPos);
            }
        }
        Tick();
    }

    void Tick()
    {
        if (Chunk.Working) return;

        Dictionary<Vector3, Chunk> chunkList = Chunk.Chunks;

        foreach(var c in chunkList)
        {
            Chunk chunk = c.Value;
            Vector3 position = c.Key;

            chunk.chunkPosition = position;

            if (chunk.dirty)
            {
                Chunk.Working = true;
                if (!chunk.calculatedMap)
                {
                    chunk.calculateMap();
                }
                chunk.calculateMesh();

                return;
            }

            if (chunk.lightDirty)
            {
                Chunk.Working = true;
                chunk.calculateLight();
                chunk.lightDirty = false;
            }
        }
    }
}
