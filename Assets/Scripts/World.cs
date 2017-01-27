using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {

    public Material material;
    public static Material materials;
    public static Transform me;
    bool oneDirty;

    //render Distance
    public int viewRange = 10;

    public void Awake()
    {
        Block.blocks.Add(new Block("dirt", "Dirt Block", 2, 15));
        Block.blocks.Add(new Block("stone", "Stone Block", 1, 15));
        Block.blocks.Add(new Block("grassblock", "Grass Block", 7, 2, 3, 15, 2, 15).SetHasTickEvent(true));
        Block.blocks.Add(new Block("bedrock", "Bedrock", 1, 14));

        materials = material;
        me = transform;
    }

    public void Update()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        for (int x = (int)cameraPos.x - (viewRange * Chunk.Width); x < (viewRange * Chunk.Width) + (int)cameraPos.x; x += viewRange)
        {
            for (int z = (int)cameraPos.z - (viewRange * Chunk.Width); z < (viewRange * Chunk.Width) + (int)cameraPos.z; z += viewRange)
            {
                int xx = Mathf.FloorToInt(x / Chunk.Width) * Chunk.Width;
                int zz = Mathf.FloorToInt(z / Chunk.Width) * Chunk.Width;

                Vector3 cPos = new Vector3(xx, 0, zz);
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

            if (Vector3.Distance(Camera.main.transform.position, position) > (Chunk.Width * viewRange / 2) + (Chunk.Width * 3))
            {
                chunk.gameObject.SetActive(false);
            }else if(!chunk.gameObject.activeInHierarchy)
            {
                chunk.gameObject.SetActive(true);
            }

            chunk.chunkPosition = position;

            if(Mathf.FloorToInt(Time.time) % 5 == 0)
            {
                c.Value.TickUpdate();
            }

            if (chunk.dirty)
            {
                oneDirty = true;
                Chunk.Working = true;
                if (!chunk.calculatedMap)
                {
                    chunk.calculatedMap = true;
                    chunk.calculateMap();
                }
                chunk.calculateMesh();
                chunk.dirty = false;
                return;
            }

            if (chunk.lightDirty)
            {
                Chunk.Working = true;
                chunk.calculateLight();
                chunk.lightDirty = false;
            }
        }
        if (!oneDirty)
        {
            PlayerMovement.gravity = 20f;
        }
        else
            oneDirty = false;
    }
}
