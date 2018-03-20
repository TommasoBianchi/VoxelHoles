using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class ThreadWorkManager
{
    public static ThreadHandle EmptyThreadHandle = new _EmptyThreadHandle();

    private static Queue<Action> actions = new Queue<Action>();

    private ThreadWorkManager() { }

    static ThreadWorkManager()
    {
        new GameObject().AddComponent<ThreadWorkManagerUpdater>().name = "ThreadWorkManagerUpdater";
    }

    public static ThreadHandle RequestWork(Action work)
    {
        Thread thread = new Thread(new ThreadStart(work));
        thread.Start();
        return new ThreadHandle(thread);
    }

    public static void RequestMainThreadWork(Action work)
    {
        lock (actions)
        {
            actions.Enqueue(work);
        }
    }

    protected static void Update()
    {
        if (actions.Count > 0)
        {
            Action work = () => { };
            lock (actions)
            {
                work = actions.Dequeue();
            }
            work.Invoke();
        }
    }

    public class ThreadHandle
    {
        private Thread thread;

        public ThreadHandle(Thread thread)
        {
            this.thread = thread;
        }

        public virtual void Wait()
        {
            thread.Join();
        }
    }

    private class _EmptyThreadHandle : ThreadHandle
    {
        public _EmptyThreadHandle() : base(null) { }

        public override void Wait()
        {
            
        }
    }

    private class ThreadWorkManagerUpdater : MonoBehaviour
    {
        private void Update()
        {
            ThreadWorkManager.Update();
        }
    }
}
