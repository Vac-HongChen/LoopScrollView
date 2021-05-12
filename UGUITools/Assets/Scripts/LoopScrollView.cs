using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
    循环列表
    -- 格子大小一致的循环列表
    -- 通过Template的大小以及Content的大小计算最多需要创建多少个格子

    -- 拖拽过程
    -- 向上拖拽:判断第一个节点的位置是否超出上边界(节点高度 + 间隔)
    -- 向下拖拽:判断最后一个节点的位置是否超出下边界(- contend的高度 - 间隔)

    功能:
        SetContent()        -- 设置内容(也可以当做刷新内容设置)
        Reset()             -- 复原
        JumpIndex()         -- 跳转到指定节点
        JumpDelta()         -- 跳转指定偏移量

    处理节点数量少于显示数量的情况
        显示数量:Content.y / (Template.y + Spacing.y)
        处理方法:直接不能拖拽

    TODO
    -- 拖拽结束后,自动移动到最近的节点
*/
public class LoopScrollView : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IScrollHandler, IEndDragHandler, IBeginDragHandler
{
    public Transform content;
    public Transform template;
    public Vector2 spacing;             // x代表左右偏移量  y代表节点之间的间隔
    public float scrollSensitivity = 10; // 滚动灵敏度
    public float moveTime = 0.2f;       // 动画时间

    private int maxCount;                // 子节点的最大数量
    public bool isCenter;// 是否居中
    public float elasticity = 0.1f;
    private float centerPoint;
    private List<Transform> nodes = new List<Transform>();
    private Vector2 templateSize;
    private Vector2 contentSize;
    private string nodeName = "Node";
    private float unitDelta;         // 单位移动量
    private float topLimit;          // 上边界
    private float downLimit;         // 下边界
    private int itemCount = 0;       // 节点总数量
    private float moveDelta = 0;     // 累计移动偏移量
    private int showCount = 0;       // 最多显示个数
    private int curIndex = 0;        // 代表第一个节点的index
    private int unitCount = 0;       // 创建的单位数量
    private Action<int, Transform> onRenderNode;
    private IEnumerator moveRoutine;
    private float ScrollSensitivity
    {
        get
        {
            if (scrollSensitivity < 0)
            {
                return 0;
            }
            return scrollSensitivity;
        }
    }

    private float beginTime;
    private float endTime;
    private Vector2 beginPosition;
    private Vector2 endPosition;
    
    // 清空临时数据
    private void ClearData()
    {
        curIndex = 0;
        moveDelta = 0;
    }

    private void Init()
    {
        foreach (Transform item in content)
        {
            Destroy(item.gameObject);
        }
        nodes.Clear();

        moveRoutine = MoveAnim(0);
        contentSize = content.GetComponent<RectTransform>().rect.size;
        templateSize = template.GetComponent<RectTransform>().rect.size;
        maxCount = (int)(contentSize.y / (templateSize.y)) + 1;
        centerPoint = (contentSize.x - templateSize.x) / 2;
        topLimit = templateSize.y;
        unitDelta = templateSize.y + spacing.y;
        showCount = (int)(contentSize.y / (templateSize.y + spacing.y));
        downLimit = -contentSize.y;
        // maxCount = showCount + 2;
    }

    /// <summary>
    /// 动画移动指定偏移量
    /// </summary>
    private IEnumerator MoveAnim(float delta)
    {
        float time = 0;
        float sum = 0;
        while (time <= moveTime)
        {
            time += Time.deltaTime;
            var tempDelta = delta * Time.deltaTime / moveTime;
            // 最后一次强制让偏移量等于剩余偏移量
            if (time >= moveTime)
            {
                tempDelta = delta - sum;
            }
            OnMove(new Vector2(0, tempDelta));
            sum += tempDelta;
            yield return 0;
        }
    }

    /// <summary>
    /// 均速移动
    /// </summary>
    private IEnumerator MoveAnimSpeed(float speed)
    {
        float maxTime = 0.2f;
        float time = 0;
        while (time <= maxTime)
        {
            time += Time.deltaTime;
            OnMove(new Vector2(0, speed));
            yield return 0;
        }
    }

    private void StartMoveAnimSpeed(float speed)
    {
        StopCoroutine(moveRoutine);
        moveRoutine = MoveAnimSpeed(speed);
        StartCoroutine(moveRoutine);
    }

    private void StartMoveAnim(float delta)
    {
        StopCoroutine(moveRoutine);
        moveRoutine = MoveAnim(delta);
        StartCoroutine(moveRoutine);
    }

    private void OnMove(Vector2 delta)
    {
        if (itemCount <= showCount)
            return;
        moveDelta += delta.y;
        if (moveDelta >= itemCount * unitDelta)
        {
            moveDelta -= itemCount * unitDelta;
        }
        if (moveDelta < 0)
        {
            moveDelta += itemCount * unitDelta;
        }

        foreach (var item in nodes)
        {
            item.GetComponent<RectTransform>().anchoredPosition += Vector2.up * delta.y;
        }


        // 通过一个bool标志将所有符合条件的节点移动到对应位置
        bool sign = true;
        while (true)
        {
            sign = true;
            var firstNode = content.GetChild(0);
            var endNode = content.GetChild(content.childCount - 1);
            if (delta.y > 0)
            {
                // 向上移动
                if (firstNode.GetComponent<RectTransform>().anchoredPosition.y >= topLimit)
                {
                    firstNode.GetComponent<RectTransform>().anchoredPosition = endNode.GetComponent<RectTransform>().anchoredPosition - Vector2.up * (templateSize.y + spacing.y);
                    firstNode.SetAsLastSibling();
                    curIndex++;
                    if (curIndex >= itemCount)
                    {
                        curIndex -= itemCount;
                    }

                    // 第一个节点变为当前节点的最后一个
                    var tempIndex = curIndex + unitCount - 1;
                    if (tempIndex >= itemCount)
                    {
                        tempIndex -= itemCount;
                    }
                    firstNode.name = nodeName + tempIndex;

                    onRenderNode(tempIndex, firstNode);
                    sign = false;
                }
            }
            else
            {
                // 向下移动
                if (endNode.GetComponent<RectTransform>().anchoredPosition.y <= downLimit)
                {
                    endNode.GetComponent<RectTransform>().anchoredPosition = firstNode.GetComponent<RectTransform>().anchoredPosition + Vector2.up * (templateSize.y + spacing.y);
                    endNode.SetAsFirstSibling();
                    curIndex--;
                    if (curIndex < 0)
                    {
                        curIndex = itemCount - 1;
                    }
                    endNode.name = nodeName + curIndex;
                    onRenderNode(curIndex, endNode);

                    sign = false;
                }
            }
            if (sign)
            {
                break;
            }
        }
    }



    public void OnDrag(PointerEventData eventData)
    {
        OnMove(eventData.delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnScroll(PointerEventData eventData)
    {
        OnMove(eventData.scrollDelta * ScrollSensitivity);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        beginTime = Time.time;
        beginPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endTime = Time.time;
        endPosition = eventData.position;
    }


    /// <summary>
    /// 弹性TODO
    /// </summary>
    private void Elasticity()
    {
        var dis = endPosition.y - beginPosition.y;
        var time = endTime - beginTime;
        var speed = dis / time;
        StartMoveAnimSpeed(speed * elasticity);
    }



    /// <summary>
    /// 设置内容
    /// </summary>
    public void SetContent(int count, Action<int, Transform> action, string nodeName = null)
    {
        Init();
        ClearData();
        if (nodeName != null)
        {
            this.nodeName = nodeName;
        }
        if (action == null)
        {
            Debug.LogError("渲染事件为空");
        }
        onRenderNode = action;
        itemCount = count;
        Vector2 currentPosition = Vector2.zero;
        if (isCenter)
        {
            currentPosition = Vector2.right * centerPoint;
        }
        unitCount = Mathf.Min(count, maxCount);
        for (int i = 0; i < unitCount; i++)
        {
            var temp = Instantiate(template, content);
            temp.name = this.nodeName + i;
            temp.gameObject.SetActive(true);
            var rect = temp.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.anchoredPosition = currentPosition + Vector2.left * spacing.x;
            nodes.Add(temp);
            currentPosition -= Vector2.up * (templateSize.y + spacing.y);

            onRenderNode(i, temp);
        }
    }
    /// <summary>
    /// 复原
    /// </summary>
    public void Reset()
    {
        StartMoveAnim(-moveDelta);
    }
    /// <summary>
    /// 跳转到指定位置
    /// </summary>
    public void JumpIndex(int index)
    {
        var delta = index * unitDelta;
        StartMoveAnim(delta - moveDelta);
    }
    /// <summary>
    /// 跳转指定偏移量
    /// </summary>
    public void JumpDelta(float delta)
    {
        StartMoveAnim(delta);
    }
}
