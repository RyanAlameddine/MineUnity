using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
	public const int ChunkStack = 10;

	public const int Width = 16, Height = 16;

    public Material material;

	public static Dictionary<Vector3, Chunk> Chunks = new Dictionary<Vector3, Chunk> ();

    public static bool Working = false;

    public List<byte[,,]> maps = new List<byte[,,]>();
    public byte[,] heightMap = new byte[Width, Width];

    public List<MeshFilter> meshes = new List<MeshFilter>();

    public List<List<Vector3>> verts = new List<List<Vector3>>();
    public List<List<int>> triangles = new List<List<int>>();
    public List<List<Vector2>> UVs = new List<List<Vector2>>();
    public List<List<Color>> colors = new List<List<Color>>();

    public bool dirty = true;
    public bool lightDirty = true;
    bool calculatedMap = false;

    private void Awake()
    {
        transform.parent = World.me;
        for(int i = 0; i < ChunkStack; i++)
        {
            maps.Add(new byte[Width, Height, Width]);
            GameObject gO = new GameObject("ChunkStack " + i);

            meshes.Add(gO.AddComponent<MeshFilter>());
            
            MeshRenderer mr = gO.AddComponent<MeshRenderer>();
            gO.transform.position = new Vector3(transform.position.x, i * Height, transform.position.z);
            gO.transform.SetParent(transform);

            mr.material = World.materials;
        }
        heightMap = new byte[Width, Width];
    }

    private void Update()
    {
        if (Working) return;

        if (dirty)
        {
            Working = true;
            if (!calculatedMap)
            {
                calculateMap();
            }
            calculateMesh();
            dirty = false;
        }

        if (lightDirty)
        {
            Working = true;
            calculateLight();
            lightDirty = false;
        }
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
                        if(worldYPos < 5)
                        {
                            map[x, y, z] = 1;
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
                            addFace(x, y, z, FaceDir.Top, i);
                        }
                    }
                }
            }

            if (verts.Count <= i) continue;

            Mesh m = new Mesh();

            m.vertices = verts[i].ToArray();
            m.triangles = triangles[i].ToArray();

            m.RecalculateNormals();
            meshes[i].mesh = m;
        }
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

            UVs[chunkID].Add(new Vector2(0, 0));
            UVs[chunkID].Add(new Vector2(1, 0));
            UVs[chunkID].Add(new Vector2(1, 1));
            UVs[chunkID].Add(new Vector2(0, 1));
        }
    }

    public static Chunk GetChunk(Vector3 Position)
    {
        int x = Mathf.FloorToInt(Position.x / Width) * Width;
        int y = Mathf.FloorToInt(Position.y / Width) * Width;
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
        int y = Mathf.FloorToInt(Position.y / Width) * Width;
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