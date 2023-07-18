/**
 * 最小堆
 * author: ZhouHuaJian
 * Time: 2021-10-18 12:36:00
 */
using System;
using System.Collections;

public class MinHeap<T> : Heap<T> where T : IComparable
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="_capacity"></param>
    public MinHeap(int _capacity)
    {
        capacity = _capacity;
        container = new T[capacity + 1];
        count = 0;
    }

    override public bool Push(T item)
    {
        if (this.Count >= capacity)
        {
            return false;
        }
        container[++count] = item;
        int son = this.Count, father = son >> 1;
        while (father >= 1 && container[son].CompareTo(container[father]) < 0)
        {
            this.Swap(son, father);
            son = father;
            father = son >> 1;
        }
        return true;
    }

    override public T Pop()
    {
        if(this.Count == 0)
        {
            return default;
        }
        T popValue = container[1];
        Swap(1, count);
        container[count--] = default;
        int father = 1, son = 2;
        while (son <= Count)
        {
            if (son < Count && container[son].CompareTo(container[son + 1]) > 0)
            {
                son++;
            }
            if (container[father].CompareTo(container[son]) > 0)
            {
                this.Swap(father, son);
                father = son;
                son = father << 1;
            }
            else
            {
                break;
            }
        }
        return popValue;
    }
}