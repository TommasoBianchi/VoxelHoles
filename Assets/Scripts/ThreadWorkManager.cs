using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class ThreadWorkManager
{
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
        lock (actions)
        {
            if (actions.Count > 0)
            {
                actions.Dequeue().Invoke();
            }
        }
    }

    public class ThreadHandle
    {
        private Thread thread;

        public ThreadHandle(Thread thread)
        {
            this.thread = thread;
        }

        public void Wait()
        {
            thread.Join();
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
