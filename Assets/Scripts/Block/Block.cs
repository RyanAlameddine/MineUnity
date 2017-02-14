using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Block : ScriptableObject
{
    public string displayName = "Block Name";
    public string name = "blockname";
    public byte id;

    public iVector2 top;
    public iVector2 bottom;
    public iVector2 right;
    public iVector2 left;
    public iVector2 front;
    public iVector2 back;

    public bool hasTickEvent;
    public bool canCollideWith = true;

    public bool isEntityBlock = false;
    public GameObject blockModel = null;
    public bool isTransparent = false;
}

[System.Serializable]
public class iVector2
{
    public int x;
    public int y;

    public iVector2()
    {

    }
    public iVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}