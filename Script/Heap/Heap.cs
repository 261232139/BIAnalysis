/**
 * author: LiuDeHua
 * time:2021-10-18 00:28:24
 */
using System;
using System.Collections.Generic;

public class Heap<T> where T : IComparable
{
    protected T[] container; // 存放堆元素的容器
    protected int capacity;  // 堆的容量，最大可以放多少个元素
    protected int count; // 堆中已经存储的数据个数

    /// <summary>
    /// 向堆中添加元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    virtual public bool Push(T item)
    {
        return true;
    }

    /// <summary>
    /// 获取堆顶元素
    /// </summary>
    /// <returns></returns>
    virtual public T Top()
    {
        if (count > 0)
            return container[1];
        else
            return default;
    }

    /// <summary>
    /// 删除最小的元素(堆顶元素)
    /// </summary>
    /// <returns></returns>
    virtual public T Pop()
    {
        return Top();
    }

    /// <summary>
    /// 空间大小
    /// </summary>
    public int Count => count;

    /// <summary>
    /// 交换
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    protected void Swap(int index1, int index2)
    {
        var value = container[index1];
        container[index1] = container[index2];
        container[index2] = value;
    }
}