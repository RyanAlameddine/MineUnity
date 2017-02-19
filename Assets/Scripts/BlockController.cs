using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour {
    public LayerMask lm;
    public GameObject BlockSelectorPrefab;
    GameObject BlockSelector;

    private void Awake()
    {
        BlockSelector = GameObject.Instantiate(BlockSelectorPrefab, Vector3.zero, Quaternion.identity) as GameObject;
    }

    private void Update()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.forward, out hit, 7f, lm))
        {
            Vector3 worldblockPos = hit.point - (hit.normal / 2);

            Vector3 blockPos = new Vector3(Mathf.FloorToInt(worldblockPos.x), Mathf.FloorToInt(worldblockPos.y), Mathf.FloorToInt(worldblockPos.z));

            Vector3 worldblockAddPos = hit.point + (hit.normal / 2);
            Vector3 blockAddPos = new Vector3(Mathf.FloorToInt(worldblockAddPos.x), Mathf.FloorToInt(worldblockAddPos.y), Mathf.FloorToInt(worldblockAddPos.z));

            BlockSelector.transform.position = blockPos;
            if (Input.GetMouseButtonDown(1))
            {
                Chunk c = Chunk.SetWorldBlock(blockAddPos, World.getBlockID("chest"), FacingDirection());
                if (c != null)
                {
                    c.dirty = true;
                    //Vector3 localPos = blockPos - c.chunkPosition;
                }
            }
            if (Input.GetMouseButtonDown(0))
            {
                byte b = Chunk.GetWorldBlock(blockPos);

                if(b > 0)
                {
                    Block selectedBlock = World.blocks[b - 1];
                    if (selectedBlock.name == "bedrock")
                    {
                        return;
                    }
                }
                Chunk c = Chunk.SetWorldBlock(blockPos, 0);
                if(c != null)
                {
                    c.dirty = true;
                    //Vector3 localPos = blockPos - c.chunkPosition;
                }
            }
        }else
        {
            BlockSelector.transform.position = Vector3.zero;
        }
    }

    public Vector3 FacingDirection()
    {
        Transform c = Camera.main.transform;

        Vector3 forward = c.forward.normalized;

        if (Vector3.Angle(forward, Vector3.forward) <= 45f)
        {
            //North
            return Vector3.forward;
        }
        else if (Vector3.Angle(forward, Vector3.right) <= 45f)
        {
            //East
            return Vector3.right;

        }
        else if (Vector3.Angle(forward, Vector3.back) <= 45f)
        {
            //South
            return Vector3.back;

        }
        else
        {
            //West
            return Vector3.left;

        }
    }
}
