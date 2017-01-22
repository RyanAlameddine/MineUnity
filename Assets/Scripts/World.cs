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
    }
}
