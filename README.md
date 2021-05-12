# LoopScrollView
LoopScrollView-Unity

https://blog.csdn.net/wanghao1230707/article/details/116712132
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
