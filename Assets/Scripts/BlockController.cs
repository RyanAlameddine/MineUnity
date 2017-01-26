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
            BlockSelector.transform.position = blockPos;
            if (Input.GetMouseButtonDown(0))
            {
                
            }
        }else
        {
            BlockSelector.transform.position = Vector3.zero;
        }
    }
}
