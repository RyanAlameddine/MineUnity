using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

[CustomEditor(typeof(Block))]
public class BlockManagerEditor : Editor
{
    [MenuItem("Assets/Voxel Game/Create Block")]
    public static void CreateBlock()
    {
        Block newBlock = Block.CreateInstance<Block>();

        string path = (AssetDatabase.GetAssetPath(Selection.activeObject));

        string[] files = Directory.GetFiles(path);

        AssetDatabase.CreateAsset(newBlock, path + "/newBlock" + files.Length + ".asset");
        AssetDatabase.SaveAssets();
    }

    public Texture2D text;
    public Texture2D sq;

    public iVector2 top = new iVector2();
    public iVector2 bottom = new iVector2();
    public iVector2 right = new iVector2();
    public iVector2 left = new iVector2();
    public iVector2 front = new iVector2();
    public iVector2 back = new iVector2();

    public FaceDir selectedFace;

    public Vector2 mp = new Vector2(-1, -1);
    public float blockWidth;

    public override void OnInspectorGUI()
    {
        if (text == null)
        {
            text = Resources.Load("text") as Texture2D;
            sq = Resources.Load("sa") as Texture2D;
        }

        Block block = (Block)target;
        Event e = Event.current;

        GUI.color = Color.yellow;
        EditorGUILayout.LabelField("Block Editor");
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        GUI.color = Color.white;

        block.displayName = EditorGUILayout.TextField("Display Name", block.displayName);
        block.name = EditorGUILayout.TextField("Block Name", block.name);
        EditorGUILayout.Space();

        block.isTransparent = EditorGUILayout.Toggle("Is Transparent", block.isTransparent);
        block.canCollideWith = EditorGUILayout.Toggle("Player Can Collide", block.canCollideWith);
        block.hasTickEvent = EditorGUILayout.Toggle("Has Tick Event", block.hasTickEvent);
        block.isEntityBlock = EditorGUILayout.Toggle("Is EntityBlock", block.isEntityBlock);
        if (block.isEntityBlock)
        {
            block.blockModel = EditorGUILayout.ObjectField(block, typeof(GameObject)) as GameObject;
        }
        else
        {

            GUILayout.BeginHorizontal();
            {
                if (selectedFace == FaceDir.Top)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Top"))
                {
                    selectedFace = FaceDir.Top;
                    mp = new Vector2(block.top.x * blockWidth, (block.top.y) * blockWidth);
                }
                GUI.color = Color.white;

                if (selectedFace == FaceDir.Bottom)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Bottom"))
                {
                    selectedFace = FaceDir.Bottom;
                    mp = new Vector2(block.bottom.x * blockWidth, ((block.bottom.y)) * blockWidth);
                }
                GUI.color = Color.white;

                if (selectedFace == FaceDir.Front)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Front"))
                {
                    selectedFace = FaceDir.Front;
                    mp = new Vector2(block.front.x * blockWidth, ((block.front.y)) * blockWidth);
                }
                GUI.color = Color.white;

                if (selectedFace == FaceDir.Back)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Back"))
                {
                    selectedFace = FaceDir.Back;
                    mp = new Vector2(block.back.x * blockWidth, ((block.back.y)) * blockWidth);
                }
                GUI.color = Color.white;

                if (selectedFace == FaceDir.Right)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Right"))
                {
                    selectedFace = FaceDir.Right;
                    mp = new Vector2(block.right.x * blockWidth, ((block.right.y)) * blockWidth);
                }
                GUI.color = Color.white;

                if (selectedFace == FaceDir.Left)
                {
                    GUI.color = Color.cyan;
                }
                if (GUILayout.Button("Left"))
                {
                    selectedFace = FaceDir.Left;
                    mp = new Vector2(block.left.x * blockWidth, ((block.left.y)) * blockWidth);
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();


            GUI.color = Color.red;
            if (GUILayout.Button("Save All Changes"))
            {
                EditorUtility.SetDirty(block);
            }
            GUI.color = Color.white;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            Rect r = EditorGUILayout.GetControlRect();
            if (r.width / 16f > 1)
                blockWidth = r.width / 16f;

            GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.width), text);
            if (GUI.Button(new Rect(r.x, r.y, r.width, r.width), "", "label"))
            {
                mp = e.mousePosition - new Vector2(r.x, r.y);

                int x = Mathf.FloorToInt(mp.x / blockWidth);
                int y = Mathf.FloorToInt(mp.y / blockWidth);

                mp = new Vector2(x * blockWidth, y * blockWidth);

                if (selectedFace == FaceDir.Top)
                {
                    block.top = new iVector2(x, y);
                }
                if (selectedFace == FaceDir.Bottom)
                {
                    block.bottom = new iVector2(x, y);
                }
                if (selectedFace == FaceDir.Back)
                {
                    block.back = new iVector2(x, y);
                }
                if (selectedFace == FaceDir.Front)
                {
                    block.front = new iVector2(x, y);
                }
                if (selectedFace == FaceDir.Right)
                {
                    block.right = new iVector2(x, y);
                }
                if (selectedFace == FaceDir.Left)
                {
                    block.left = new iVector2(x, y);
                }
                EditorUtility.SetDirty(block);
            }


            GUI.DrawTexture(new Rect(r.x + mp.x, r.y + mp.y, blockWidth, blockWidth), sq);
        }
    }
}