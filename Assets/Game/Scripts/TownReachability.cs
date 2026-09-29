using System;
using System.Collections.Generic;
namespace MeadowQuest
{
    public static class TownReachability
    {
        public static HashSet<T> Reach<T>(T start,Func<T,bool> walkable,Func<T,IEnumerable<T>> neighbors,IDictionary<T,T> portals)
        {
            var seen=new HashSet<T>(); var queue=new Queue<T>();
            if(!walkable(start)) return seen;
            seen.Add(start); queue.Enqueue(start);
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();
                IEnumerable<T> next=portals.TryGetValue(cell,out var target)?new[]{target}:neighbors(cell);
                foreach(var n in next) if(walkable(n) && seen.Add(n)) queue.Enqueue(n);
            }
            return seen;
        }
    }
}
