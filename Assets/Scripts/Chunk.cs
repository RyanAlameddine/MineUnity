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

    public List<byte[,,]> maps = new List<byte[,,]>();
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

    private void Awake()
    {
        chunkPosition = transform.position;
        transform.parent = World.me;
        for(int i = 0; i < ChunkStack; i++)
        {
            maps.Add(new byte[Width, Height, Width]);
            GameObject gO = new GameObject("ChunkStack " + i);

            meshes.Add(gO.AddComponent<MeshFilter>());
            
            MeshRenderer mr = gO.AddComponent<MeshRenderer>();
            gO.AddComponent<MeshCollider>();
            gO.transform.position = new Vector3(transform.position.x, i * Height, transform.position.z);
            gO.transform.SetParent(transform);

            mr.material = World.materials;
        }
        heightMap = new byte[Width, Width];
    }

    public void calculateMap()
    {
        Working = false;

        for(int i = 0; i < ChunkStack; i++) {
            byte[,,] map = new byte[Width, Height, Width];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Width; z++)
                    {
                        int worldYPos = i * Height + y;
                        Vector3 pos = new Vector3(x, worldYPos, z) + transform.position;
                        if(worldYPos <= Height + 5)
                        {
                            map[x, y, z] = 1;
                        }

                        if (worldYPos == Height + 5 && Random.Range(0, 20) == 1)
                        {
                            map[x, y, z] = 2;
                        }
                        if (worldYPos == Height + 5 && Random.Range(0, 20) == 1)
                        {
                            map[x, y, z] = 3;
                            EventBlocks.Add(pos, 3);
                        }

                        if (worldYPos == 0)
                        {
                            map[x, y, z] = 4;
                        }
                        else if (worldYPos < 3 && Random.Range(0, 3) == 1)
                        {
                            map[x, y, z] = 4;
                        }
                    }
                }
            }
            maps[i] = map;
        }
    }

    public void TickUpdate()
    {
        Dictionary<Vector3, byte> chunkList = new Dictionary<Vector3, byte>(EventBlocks);

        foreach (var result in chunkList)
        {
            Vector3 worldPos = result.Key;
            byte block = result.Value;
            if (block == 3 && RandomNumber(0,2) == 0)
            {
                byte blockAbove = GetWorldBlock(worldPos + new Vector3(0, 1, 0));

                if (blockAbove > 0)
                {
                    SetWorldBlock(worldPos, 1, true);
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
                if (blocksAround[index] == 1)
                {
                    SetWorldBlock(positions[index], 3, true);
                    
                }
            }
        }
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
            byte[,,] map = maps[i];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Width; z++)
                    {
                        if(map[x, y, z] > 0)
                        {
                            if(isBlockTransparent(x, y , z+1, i))
                                addFace(x, y, z, FaceDir.Front, i);

                            if(isBlockTransparent(x, y, z-1, i))
                                addFace(x, y, z, FaceDir.Back, i);

                            if(isBlockTransparent(x-1, y, z, i))
                                addFace(x, y, z, FaceDir.Left, i);

                            if(isBlockTransparent(x+1, y, z, i))
                                addFace(x, y, z, FaceDir.Right, i);

                            if(isBlockTransparent(x, y + 1, z, i))
                                addFace(x, y, z, FaceDir.Top, i);

                            if(isBlockTransparent(x, y - 1, z, i))
                                addFace(x, y, z, FaceDir.Bottom, i);
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
        if(chunkID >= ChunkStack || chunkID < 0)
        {
            return null;
        }

        int x = (int)localPos.x;
        int y = (int)localPos.y - (chunkID * Height);
        int z = (int)localPos.z;

        c.maps[chunkID][x, y, z] = blockID;

        if (blockID != 0 && Block.blocks[blockID - 1].hasTickEvent & !c.EventBlocks.ContainsKey(pos))
        {
            c.EventBlocks.Add(pos, blockID);
        }
        return c;
    }

    public static Chunk SetWorldBlock(Vector3 pos, byte blockID, bool setDirty)
    {
        Chunk c = Chunk.GetChunk(pos);
        c.dirty = setDirty;
        return SetWorldBlock(pos, blockID);
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
        if (UVs.Count >= chunkID)
        {
            UVs.Add(new List<Vector2>());
        }
        if (colors.Count >= chunkID)
        {
            colors.Add(new List<Color>());
        }

        byte blockid = (byte) (maps[chunkID][x, y, z] - 1);
        float blockWidth = 1f / 16f;
        int xSide = Block.blocks[blockid].textureXSide;
        int ySide = Block.blocks[blockid].textureYSide;

        int xTop = Block.blocks[blockid].textureXTop;
        int yTop = Block.blocks[blockid].textureYTop;

        int xBottom = Block.blocks[blockid].textureXBottom;
        int yBottom = Block.blocks[blockid].textureYBottom;

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

            UVs[chunkID].Add(new Vector2((0 + xTop) * blockWidth, (0 + yTop) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xTop) * blockWidth, (0 + yTop) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xTop) * blockWidth, (1 + yTop) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xTop) * blockWidth, (1 + yTop) * blockWidth));
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

            UVs[chunkID].Add(new Vector2((0 + xBottom) * blockWidth, (0 + yBottom) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xBottom) * blockWidth, (0 + yBottom) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xBottom) * blockWidth, (1 + yBottom) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xBottom) * blockWidth, (1 + yBottom) * blockWidth));
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

            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (1 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (1 + ySide) * blockWidth));
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

            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (1 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (1 + ySide) * blockWidth));
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

            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (1 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (1 + ySide) * blockWidth));
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

            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (0 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((1 + xSide) * blockWidth, (1 + ySide) * blockWidth));
            UVs[chunkID].Add(new Vector2((0 + xSide) * blockWidth, (1 + ySide) * blockWidth));
        }
    }

    public bool isBlockTransparent(int x, int y, int z, int chunkID)
    {
        if(x >= Width || z >= Width || y >= Height || x < 0 || y < 0 || z < 0)
        {
            return true;
            //return GetWorldBlock(new Vector3(x, y + (chunkID * Height), z) + chunkPosition) == 0;
        }else
        {
            return maps[chunkID][x, y, z] == 0;
        }
    }

    public static byte GetWorldBlock(Vector3 pos)
    {
        Chunk c = GetChunk(pos);
        if (Equals(c, null)) return 1;

        int chunkID = Mathf.FloorToInt(pos.y / Height);
        //if (chunkID >= ChunkStack || chunkID < 10) return 0;

        Vector3 localPosition = pos - c.chunkPosition;

        int x = (int)localPosition.x;
        int y = (int)localPosition.y - (chunkID * Height);
        int z = (int)localPosition.z;

        byte block = c.maps[chunkID][x, y, z];
        return block;
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
        return c.maps[chunkID][x, y, z];
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
        GameObject Chunk = new GameObject("Chunk" + cPos);
        Chunk thisChunk = Chunk.AddComponent<Chunk>();
        MeshRenderer mr = Chunk.AddComponent<MeshRenderer>();
        Chunk.AddComponent<MeshFilter>();

        mr.material = World.materials;

        Chunk.transform.position = cPos;

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

public class Block
{
    public static List<Block> blocks = new List<Block>();

    public string displayName;
    public string name;
    public byte id;

    public byte textureX;
    public byte textureY;

    public byte textureXTop;
    public byte textureYTop;

    public byte textureXBottom;
    public byte textureYBottom;

    public byte textureXSide;
    public byte textureYSide;

    public bool hasTickEvent = false;

    public Block(string name, string displayName, byte tX, byte tY)
    {
        id = (byte)(blocks.Count + 1);
        this.name = name;
        this.displayName = displayName;
        textureXBottom = tX;
        textureXSide = tX;
        textureXTop = tX;

        textureYBottom = tY;
        textureYSide = tY;
        textureYTop = tY;
    }

    public Block(string name, string displayName, byte topX, byte topY, byte sideX, byte sideY, byte bottomX, byte bottomY)
    {
        id = (byte)(blocks.Count + 1);
        this.name = name;
        this.displayName = displayName;
        textureXBottom = bottomX;
        textureXSide = sideX;
        textureXTop = topX;

        textureYBottom = bottomY;
        textureYSide = sideY;
        textureYTop = topY;
    }

    public Block SetHasTickEvent(bool v)
    {
        hasTickEvent = true;

        return this;
    }
}