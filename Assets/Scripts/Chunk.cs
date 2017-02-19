using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Chunk : MonoBehaviour {
	public const int ChunkStack = 10;

	public const int Width = 16, Height = 16;

    public Material material;

	public static Dictionary<Vector3, Chunk> Chunks = new Dictionary<Vector3, Chunk> ();
	public Dictionary<Vector3, byte> EventBlocks = new Dictionary<Vector3, byte> ();

    public static bool Working = false;
    public bool runTick = false;

    public Vector3 chunkPosition;

    public List<MapBlock[,,]> maps = new List<MapBlock[,,]>();

    public List<GameObject>[] blockEntities;
    public byte[,] heightMap = new byte[Width, Width];

    public List<MeshFilter> meshes = new List<MeshFilter>();

    public List<List<Vector3>> verts = new List<List<Vector3>>();
    public List<List<int>> triangles = new List<List<int>>();
    public List<List<Vector2>> UVs = new List<List<Vector2>>();
    public List<List<Color>> colors = new List<List<Color>>();

    public bool dirty = true;
    public bool dirtyCollider = true;
    public bool lightDirty = true;
    public bool calculatedMap = false;

    private static readonly System.Random random = new System.Random();
    private static readonly object syncLock = new object();
    public static int RandomNumber(int min, int max)
    {
        lock (syncLock)
        { // synchronize
            return random.Next(min, max);
        }
    }

    public bool canGenerateMesh = false;

    private void Awake()
    {
        transform.parent = World.me;
        MapBlock[,,] map = new MapBlock[Width, Height, Width];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Width; z++)
                {
                    map[x, y, z] = new MapBlock(0);
                }
            }
        }
        for (int i = 0; i < ChunkStack; i++)
        {
            maps.Add((MapBlock[,,])map.Clone());
            GameObject gO = new GameObject("ChunkStack " + i);

            meshes.Add(gO.AddComponent<MeshFilter>());
            
            MeshRenderer mr = gO.AddComponent<MeshRenderer>();
            gO.AddComponent<MeshCollider>();
            gO.transform.position = new Vector3(transform.position.x, i * Height, transform.position.z);
            gO.transform.SetParent(transform);

            mr.material = World.materials;
        }
        heightMap = new byte[Width, Width];
        blockEntities = new List<GameObject>[ChunkStack];

        for(int i = 0; i < ChunkStack; i++)
        {
            blockEntities[i] = new List<GameObject>();
        }
    }

    private void Update()
    {
        if (canGenerateMesh) return;

        Chunk right = GetChunk(chunkPosition + new Vector3(Width, 0, 0));
        Chunk left = GetChunk(chunkPosition + new Vector3(-Width, 0, 0));

        Chunk back = GetChunk(chunkPosition + new Vector3(0, 0, Width));
        Chunk front = GetChunk(chunkPosition + new Vector3(0, 0, -Width));

        Chunk up = GetChunk(chunkPosition + new Vector3(0, Height, 0));
        Chunk down = GetChunk(chunkPosition + new Vector3(0, -Height, 0));

        if (right != null && right.calculatedMap &&
            left != null && left.calculatedMap &&
            back != null && back.calculatedMap &&
            front != null && front.calculatedMap &&
            up != null && up.calculatedMap &&
            down != null && down.calculatedMap)
        {
            canGenerateMesh = true;
        }
    }

    public void calculateMap()
    {
        Working = false;

        for(int i = 0; i < ChunkStack; i++) {
            //MapBlock[,,] map = new MapBlock[Width, Height, Width];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Width; z++)
                    {
                        int worldYPos = i * Height + y;
                        Vector3 pos = new Vector3(x, worldYPos, z) + transform.position;

                        if (worldYPos <= Height + 5)
                        {
                            SetWorldBlock(pos, World.getBlockID("dirt"));
                        }

                        if (worldYPos == Height + 5 && Random.Range(0, 20) == 1)
                        {
                            SetWorldBlock(pos, World.getBlockID("stone"));
                        }
                        if (worldYPos == Height + 5 && Random.Range(0, 20) == 1)
                        {
                            SetWorldBlock(pos, World.getBlockID("grass"));
                            EventBlocks.Remove(pos);
                        }

                        if (worldYPos == 0)
                        {
                            SetWorldBlock(pos, World.getBlockID("bedrock"));
                        }
                        else if (worldYPos < 3 && Random.Range(0, 3) == 1)
                        {
                            SetWorldBlock(pos, World.getBlockID("bedrock"));
                        }
                    }
                }
            }
            //maps[i] = map;
        }
    }

    public void TickUpdate()
    {
        Dictionary<Vector3, byte> chunkList = new Dictionary<Vector3, byte>(EventBlocks);

        bool changedBlock = false;
        foreach (var result in chunkList)
        {
            Vector3 worldPos = result.Key;
            byte block = result.Value;
            if (block == World.getBlockID("grass") && RandomNumber(0,2) == 0)
            {
                byte blockAbove = GetWorldBlock(worldPos + new Vector3(0, 1, 0));
                if (blockAbove > 0 && !World.getBlock(blockAbove).isTransparent)
                {
                    SetWorldBlock(worldPos, World.getBlockID("dirt"));
                    continue;
                }
                Vector3[] positions = new Vector3[4]
                {
                worldPos + new Vector3(0, 0, 1),
                worldPos + new Vector3(1, 0, 0),
                worldPos + new Vector3(0, 0, -1),
                worldPos + new Vector3(-1, 0, 0)
                };
                byte[] blocksAround = new byte[4]
                {
                GetWorldBlock(positions[0]),
                GetWorldBlock(positions[1]),
                GetWorldBlock(positions[2]),
                GetWorldBlock(positions[3]),
                };

                int index = RandomNumber(0, 4);
                if (blocksAround[index] == World.getBlockID("dirt"))
                {
                    Chunk c = SetWorldBlock(positions[index], World.getBlockID("grass"));
                    if (!Equals(c, null))
                    {
                        changedBlock = true;
                    }
                }
            }
        }
        if (changedBlock)
            dirty = true;

    }

    public void calculateMesh()
    {

        verts.Clear();
        triangles.Clear();
        UVs.Clear();
        colors.Clear();
        Working = false;
        for (int i = 0; i < ChunkStack; i++)
        {
            foreach(GameObject go in blockEntities[i])
            {
                Destroy(go);
            }
            MapBlock[,,] map = maps[i];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Width; z++)
                    {
                        if(map[x,y,z] != null && map[x, y, z].blockID > 0)
                        {
                            Block b = map[x,y,z].getBlock();

                            if (b.isEntityBlock)
                            {
                                GameObject entity = Instantiate(b.blockModel, chunkPosition + new Vector3(x, y + i * Height, z), Quaternion.identity) as GameObject;
                                if (map[x, y, z].direction == Vector3.forward)
                                {
                                    entity.transform.forward = map[x, y, z].direction;
                                    Debug.Log("Forward");
                                }else if (map[x, y, z].direction == Vector3.back)
                                {
                                    Debug.Log("Backwards");
                                    entity.transform.position += Vector3.right;
                                    entity.transform.position += Vector3.forward;
                                    entity.transform.forward = map[x, y, z].direction;
                                }else if (map[x, y, z].direction == Vector3.left)
                                {
                                    Debug.Log("Left");
                                    entity.transform.position += Vector3.right;
                                    entity.transform.forward = map[x, y, z].direction;
                                }else if (map[x, y, z].direction == Vector3.right)
                                {
                                    Debug.Log("Right");

                                    entity.transform.forward = map[x, y, z].direction;
                                    entity.transform.position += Vector3.forward;
                                }
                                blockEntities[i].Add(entity);
                            }
                            else
                            {

                                if (isBlockTransparent(x, y, z + 1, i))
                                    addFace(x, y, z, FaceDir.Front, i);

                                if (isBlockTransparent(x, y, z - 1, i))
                                    addFace(x, y, z, FaceDir.Back, i);

                                if (isBlockTransparent(x - 1, y, z, i))
                                    addFace(x, y, z, FaceDir.Left, i);

                                if (isBlockTransparent(x + 1, y, z, i))
                                    addFace(x, y, z, FaceDir.Right, i);

                                if (isBlockTransparent(x, y + 1, z, i))
                                    addFace(x, y, z, FaceDir.Top, i);

                                if (isBlockTransparent(x, y - 1, z, i))
                                    addFace(x, y, z, FaceDir.Bottom, i);
                            }
                        }
                    }
                }
            }

            if (verts.Count <= i) continue;

            Mesh m = new Mesh();

            m.vertices = verts[i].ToArray();
            m.triangles = triangles[i].ToArray();
            m.uv = UVs[i].ToArray();

            m.RecalculateNormals();
            meshes[i].mesh = m;
            if (dirtyCollider)
            {
                meshes[i].gameObject.GetComponent<MeshCollider>().sharedMesh = m;
            }
        }
        dirty = false;
        dirtyCollider = false;
    }

    public void calculateLight()
    {
        Working = false;
    }

    public static Chunk SetWorldBlock(Vector3 pos, byte blockID)
    {
        Chunk c = Chunk.GetChunk(pos);
        if (Equals(c, null)) return null;

        byte oldBlock = GetWorldBlock(pos);

        if (oldBlock == 0 || blockID != oldBlock && blockID == 0)
        {
            c.dirtyCollider = true;
        }

        Vector3 localPos = pos - c.chunkPosition;

        int chunkID = Mathf.FloorToInt(localPos.y / Height);
        if (chunkID >= ChunkStack || chunkID < 0)
            return null;

        int x = (int)localPos.x;
        int y = (int)localPos.y - (chunkID * Height);
        int z = (int)localPos.z;

        int worldY = ((chunkID * Height) + y);
        c.maps[chunkID][x, y, z] = new MapBlock(blockID, Vector3.forward);

        if (c.EventBlocks.ContainsKey(pos))
        {
            c.EventBlocks.Remove(pos);
        }
        
        if (blockID != 0 && World.blocks[blockID - 1].hasTickEvent)
        {
            c.EventBlocks.Add(pos, (byte)blockID);
        }

        /*if (c.heightMap[x, z] < y)
        {
            c.heightMap[x, z] = y;
        }*/
        return c;
    }

    public static Chunk SetWorldBlock(Vector3 pos, byte blockID, Vector3 facingDir)
    {
        Chunk c = Chunk.GetChunk(pos);
        if (Equals(c, null)) return null;

        byte oldBlock = GetWorldBlock(pos);
        if (oldBlock == 0 || blockID != oldBlock && blockID == 0)
        {
            c.dirtyCollider = true;
        }

        Vector3 localPos = pos - c.chunkPosition;

        int chunkID = Mathf.FloorToInt(localPos.y / Height);
        if (chunkID >= ChunkStack || chunkID < 0)
            return null;

        int x = (int)localPos.x;
        int y = (int)localPos.y - (chunkID * Height);
        int z = (int)localPos.z;

        int worldY = ((chunkID * Height) + y);
        c.maps[chunkID][x, y, z] = new MapBlock(blockID, facingDir * -1);


        if (c.EventBlocks.ContainsKey(pos))
        {
            c.EventBlocks.Remove(pos);
        }
        if (World.blocks[blockID].hasTickEvent)
        {
            c.EventBlocks.Add(pos, (byte)blockID);
        }

        /*if (c.heightMap[x, z] < y)
        {
            c.heightMap[x, z] = y;
        }*/
        return c;
    }


    public void addFace(int x, int y, int z, FaceDir dir, int chunkID)
    {
        if (verts.Count >= chunkID)
        {
            verts.Add(new List<Vector3>());
        }
        if (triangles.Count >= chunkID)
        {
            triangles.Add(new List<int>());
        }
        if (this.UVs.Count >= chunkID)
        {
            this.UVs.Add(new List<Vector2>());
        }
        if (colors.Count >= chunkID)
        {
            colors.Add(new List<Color>());
        }

        float blockWidth = (float)1 / (float)16;

        byte blockID = (byte)(maps[chunkID][x, y, z].blockID - 1);

        /*int worldY = ((chunckID * Height) + y);
        
        if ((worldY) < heightMap[x, z])
        {
            colors[chunckID].Add(Color.gray);
            colors[chunckID].Add(Color.gray);
            colors[chunckID].Add(Color.gray);
            colors[chunckID].Add(Color.gray);
        }
        else
        {
            colors[chunckID].Add(Color.white);
            colors[chunckID].Add(Color.white);
            colors[chunckID].Add(Color.white);
            colors[chunckID].Add(Color.white);
        }*/

        colors[chunkID].Add(Color.white);
        colors[chunkID].Add(Color.white);
        colors[chunkID].Add(Color.white);
        colors[chunkID].Add(Color.white);

        if (dir == FaceDir.Top)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 1);

            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 2);

            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 1));
            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 1));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].top.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].top.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].top.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].top.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].top.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].top.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].top.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].top.y - 15)) * blockWidth));
        }
        else if (dir == FaceDir.Bottom)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 1);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 1));
            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 1));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].bottom.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].bottom.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].bottom.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].bottom.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].bottom.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].bottom.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].bottom.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].bottom.y - 15)) * blockWidth));
        }
        else if (dir == FaceDir.Front)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 1);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 1));
            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 1));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 1));
            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 1));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].front.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].front.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].front.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].front.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].front.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].front.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].front.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].front.y - 15)) * blockWidth));
        }
        else if (dir == FaceDir.Back)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 1);

            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 2);

            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 0));
            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 0));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].back.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].back.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].back.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].back.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].back.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].back.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].back.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].back.y - 15)) * blockWidth));
        }
        else if (dir == FaceDir.Right)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 1);

            triangles[chunkID].Add(verts[chunkID].Count + 0);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 2);

            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 1, y + 0, z + 1));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 1));
            verts[chunkID].Add(new Vector3(x + 1, y + 1, z + 0));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].right.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].right.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].right.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].right.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].right.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].right.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].right.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].right.y - 15)) * blockWidth));
        }
        else if (dir == FaceDir.Left)
        {
            triangles[chunkID].Add(verts[chunkID].Count + 1);
            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            triangles[chunkID].Add(verts[chunkID].Count + 2);
            triangles[chunkID].Add(verts[chunkID].Count + 3);
            triangles[chunkID].Add(verts[chunkID].Count + 0);

            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 0));
            verts[chunkID].Add(new Vector3(x + 0, y + 0, z + 1));
            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 1));
            verts[chunkID].Add(new Vector3(x + 0, y + 1, z + 0));

            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].left.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].left.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].left.x) * blockWidth, (0 + Mathf.Abs(World.blocks[blockID].left.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + World.blocks[blockID].left.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].left.y - 15)) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + World.blocks[blockID].left.x) * blockWidth, (1 + Mathf.Abs(World.blocks[blockID].left.y - 15)) * blockWidth));
        }
    }

    public bool isBlockTransparent(int x, int y, int z, int chunkID)
    {
        if(x >= Width || z >= Width || y >= Height || x < 0 || y < 0 || z < 0)
        {
            //return true;
            int blockID = GetWorldBlock(new Vector3(x, y + (chunkID * Height), z) + chunkPosition);
            if (blockID == 0) return true;
            Block b = World.blocks[blockID - 1];
            return b.isTransparent;
        }else
        {
            int blockID = maps[chunkID][x, y, z].blockID;
            if (blockID == 0) return true;
            Block b = World.blocks[blockID - 1];
            return b.isTransparent;
        }
    }

    public static byte GetWorldBlock(Vector3 pos)
    {
        Chunk c = Chunk.GetChunk(pos);
        if (Equals(c, null)) return 1;

        Vector3 localPos = pos - c.chunkPosition;

        int chunckID = Mathf.FloorToInt(localPos.y / Height);
        if (chunckID >= ChunkStack || chunckID < 0)
            return 0;

        int x = (int)localPos.x;
        int y = (int)localPos.y - (chunckID * Height);
        int z = (int)localPos.z;
        MapBlock b = c.maps[chunckID][x, y, z];
        if (b == null) return 0;
        
        return b.blockID;
    }

    public byte GetBlock(Vector3 worldPos)
    {
        Chunk c = GetChunk(worldPos);

        int chunkID =(int) (worldPos.y / Height);

        Vector3 localPosition = worldPos - c.chunkPosition;
        if (localPosition.y < 0) return 0;
        else if (localPosition.y >= Height * ChunkStack) return 0;

        int x = (int)localPosition.x;
        int y = (int)localPosition.y;
        int z = (int)localPosition.z;
        return c.maps[chunkID][x, y, z].blockID;
    }

    public static Chunk GetChunk(Vector3 Position)
    {
        int x = Mathf.FloorToInt(Position.x / Width) * Width;
        int y = 0;
        int z = Mathf.FloorToInt(Position.z / Width) * Width;

        Vector3 cPos = new Vector3(x, y, z);
        if (Chunks.ContainsKey(cPos))
        {
            return Chunks[cPos];
        }
        return null;
    }

    public static bool ChunkExists(Vector3 Position)
    {
        int x = Mathf.FloorToInt(Position.x / Width) * Width;
        int y = 0;
        int z = Mathf.FloorToInt(Position.z / Width) * Width;

        Vector3 cPos = new Vector3(x, y, z);
        if (Chunks.ContainsKey(cPos))
        {
            return true;
        }
        return false;
    }

    public static bool AddChunk(Vector3 Position){
		int x = Mathf.FloorToInt (Position.x / Width) * Width;
		int y = Mathf.FloorToInt (Position.y / Width) * Width;
		int z = Mathf.FloorToInt (Position.z / Width) * Width;

		Vector3 cPos = new Vector3 (x, y, z);
		if (Chunks.ContainsKey (cPos)) {
			return false;
		}
        GameObject chunk = new GameObject("Chunk" + cPos);
        Chunk thisChunk = chunk.AddComponent<Chunk>();
        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
        chunk.AddComponent<MeshFilter>();

        mr.material = World.materials;

        chunk.transform.position = cPos;
        thisChunk.chunkPosition = cPos;



        Chunks.Add (cPos, thisChunk);
		return true;
	}

    public static bool RemoveChunk(Vector3 Position)
    {
        int x = Mathf.FloorToInt(Position.x / Width) * Width;
        int y = Mathf.FloorToInt(Position.y / Width) * Width;
        int z = Mathf.FloorToInt(Position.z / Width) * Width;

        Vector3 cPos = new Vector3(x, y, z);
        if (Chunks.ContainsKey(cPos))
        {
            Destroy(Chunks[cPos].gameObject);
            Chunks.Remove(cPos);
            return false;
        }

        return true;
    }
}


public enum FaceDir
{
    Top,
    Bottom,
    Right,
    Left,
    Front,
    Back
}

public class MapBlock
{
    public byte blockID;
    public Vector3 direction;

    public MapBlock(byte b)
    {
        blockID = b;
    }

    public MapBlock(byte b, Vector3 dir)
    {
        blockID = b;
        direction = dir;
    }

    public Block getBlock()
    {
        Block b = World.getBlock(blockID);
        return b;
    }
}

//public class Block
//{
//    public static List<Block> blocks = new List<Block>();

//    public string displayName;
//    public string name;
//    public byte id;

//    public byte textureX;
//    public byte textureY;

//    public byte textureXTop;
//    public byte textureYTop;

//    public byte textureXBottom;
//    public byte textureYBottom;

//    public byte textureXSide;
//    public byte textureYSide;

//    public bool hasTickEvent = false;

//    public Block(string name, string displayName, byte tX, byte tY)
//    {
//        id = (byte)(blocks.Count + 1);
//        this.name = name;
//        this.displayName = displayName;
//        textureXBottom = tX;
//        textureXSide = tX;
//        textureXTop = tX;

//        textureYBottom = tY;
//        textureYSide = tY;
//        textureYTop = tY;
//    }

//    public Block(string name, string displayName, byte topX, byte topY, byte sideX, byte sideY, byte bottomX, byte bottomY)
//    {
//        id = (byte)(blocks.Count + 1);
//        this.name = name;
//        this.displayName = displayName;
//        textureXBottom = bottomX;
//        textureXSide = sideX;
//        textureXTop = topX;

//        textureYBottom = bottomY;
//        textureYSide = sideY;
//        textureYTop = topY;
//    }

//    public Block SetHasTickEvent(bool v)
//    {
//        hasTickEvent = true;

//        return this;
//    }
//}