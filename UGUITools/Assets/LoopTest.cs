using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoopTest : MonoBehaviour
{
    public int count = 100;
    private LoopScrollView loopScrollView;
    private void Start()
    {
        loopScrollView = GetComponent<LoopScrollView>();
        loopScrollView.SetContent(count, OnRenderNode);
    }
    private void OnRenderNode(int index, Transform node)
    {
        node.GetComponentInChildren<Text>().text = "node" + index;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            loopScrollView.JumpIndex(3);
        }
    }
}
