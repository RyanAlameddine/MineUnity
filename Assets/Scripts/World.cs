using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour {

    public static Block[] blocks;
    static Dictionary<string, byte> blockReferences = new Dictionary<string, byte>();

    public Material material;
    public static Material materials;
    public static Transform me;

    //render Distance
    public int viewRange = 10;

    public void Awake()
    {
        Object[] objs = Resources.LoadAll("Blocks", typeof(Block));
        blocks = new Block[objs.Length];

        for (int i = 0; i < objs.Length; i++)
        {
            blocks[i] = (Block)objs[i];
            blocks[i].id = (byte)i;
            blockReferences.Add(blocks[i].name, (byte)(i+1));
        }

        StartCoroutine(threadProcessor());

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

    public static List<Thread> threads = new List<Thread>();

    public static byte getBlockID(string name)
    {
        return blockReferences[name];
    }

    public static Block getBlock(int id)
    {
        return blocks[id - 1];
    }

    void Tick()
    {
        if (Chunk.Working) return;

        Dictionary<Vector3, Chunk> chunkList = Chunk.Chunks;

        foreach(var c in chunkList)
        {

            Chunk chunk = c.Value;
            Vector3 position = c.Key;

            if (Vector3.Distance(Camera.main.transform.position - new Vector3(0, Camera.main.transform.position.y, 0), position) > (Chunk.Width * viewRange / 2) + (Chunk.Width * 3))
            {
                chunk.gameObject.SetActive(false);
            }else if(!chunk.gameObject.activeInHierarchy)
            {
                chunk.gameObject.SetActive(true);
            }

            chunk.chunkPosition = position;

            if(Mathf.FloorToInt(Time.time) % 10 == 0)
            {
                if (chunk.runTick == false)
                {
                    Thread t = new Thread(c.Value.TickUpdate);
                    threads.Add(t);
                    chunk.runTick = true;
                }
            }else
            {
                chunk.runTick = false;
            }

            if (chunk.dirty)
            {
                //Chunk.Working = true;
                if (!chunk.calculatedMap)
                {
                    chunk.calculatedMap = true;
                    chunk.calculateMap();
                }
                if (chunk.canGenerateMesh)
                {
                    chunk.calculateMesh();
                    return;
                }
            }

            if (chunk.lightDirty)
            {
                Chunk.Working = true;
                chunk.calculateLight();
                chunk.lightDirty = false;
            }
        }
    }

    IEnumerator threadProcessor()
    {
        if (threads.Count > 0)
        {
            threads[0].Start();
            while (threads[0].IsAlive)
            {
                yield return 0;
            }
            threads.RemoveAt(0);
        }

        yield return 0;

        StartCoroutine(threadProcessor());
    }
}
