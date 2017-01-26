using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
	public const int ChunkStack = 10;

	public const int Width = 16, Height = 16;

    public Material material;

	public static Dictionary<Vector3, Chunk> Chunks = new Dictionary<Vector3, Chunk> ();

    public static bool Working = false;

    public Vector3 chunkPosition;

    public List<byte[,,]> maps = new List<byte[,,]>();
    public byte[,] heightMap = new byte[Width, Width];

    public List<MeshFilter> meshes = new List<MeshFilter>();

    public List<List<Vector3>> verts = new List<List<Vector3>>();
    public List<List<int>> triangles = new List<List<int>>();
    public List<List<Vector2>> UVs = new List<List<Vector2>>();
    public List<List<Color>> colors = new List<List<Color>>();

    public bool dirty = true;
    public bool lightDirty = true;
    public bool calculatedMap = false;

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
        System.Random r = new System.Random();

        for(int i = 0; i < ChunkStack; i++) {
            byte[,,] map = new byte[Width, Height, Width];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Width; z++)
                    {
                        int worldYPos = i * Height + y;
                        if(worldYPos < Height + 5)
                        {
                            map[x, y, z] = 1;
                        }
                        else if (worldYPos == Height + 5 && r.Next(0, 20) == 1)
                        {
                            map[x, y, z] = 2;
                        }
                        else if (worldYPos == Height + 5 && r.Next(0, 20) == 1)
                        {
                            map[x, y, z] = 3;
                        }
                    }
                }
            }
            maps[i] = map;
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
            MeshCollider mc = meshes[i].gameObject.GetComponent<MeshCollider>();
            mc.sharedMesh = m;
            mc.inflateMesh = true;
        }
        dirty = false;
    }

    public void calculateLight()
    {
        Working = false;
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
            //return GetBlock(new Vector3(x, y, z) + chunkPosition) == 0;
            return true;
        }else
        {
            return maps[chunkID][x, y, z] == 0;
        }
    }

    public static byte GetWorldBlock(Vector3 pos)
    {
        Chunk c = GetChunk(pos);
        if (c == null) return 1;

        int chunkID = Mathf.FloorToInt(pos.y / Height);
        if (chunkID >= ChunkStack) return 0;

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
        return c.maps[chunkID][x,y,z];
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
}