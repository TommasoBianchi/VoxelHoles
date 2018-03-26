using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class ThreadWorkManager
{
    private static Queue<Action> mainThreadActions = new Queue<Action>();
    private static List<Worker> workers = new List<Worker>();
    private static Queue<Action> workerActions = new Queue<Action>();
    private static int workPoolSize = 4;

    private ThreadWorkManager() { }

    static ThreadWorkManager()
    {
        new GameObject().AddComponent<ThreadWorkManagerUpdater>().name = "ThreadWorkManagerUpdater";

        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            workers.Add(new Worker(Refill));
        }
        Debug.Log("ThreadWorkManager created " + workers.Count + " threads");
    }

    public static void RequestWork(List<Action> work)
    {
        lock (workerActions)
        {
            foreach (Action a in work)
            {
                workerActions.Enqueue(a);
            }
        }

        foreach (Worker worker in workers)
        {
            worker.SetWorkAdded();
        }
    }

    public static void RequestWork(Action work)
    {
        RequestWork(new List<Action>(new Action[] { work }));
    }

    public static void RequestMainThreadWork(Action work)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(work);
        }
    }

    protected static void Update()
    {
        if (mainThreadActions.Count > 0)
        {
            Action work = () => { };
            lock (mainThreadActions)
            {
                work = mainThreadActions.Dequeue();
            }
            work.Invoke();
        }
    }

    protected static void Destroy()
    {
        foreach (Worker worker in workers)
        {
            worker.Destroy();
        }
    }

    private static Queue<Action> Refill()
    {
        Queue<Action> result = new Queue<Action>();

        lock (workerActions)
        {
            for (int i = 0; i < workPoolSize && workerActions.Count > 0; i++)
            {
                result.Enqueue(workerActions.Dequeue());
            }
        }

        return result;
    }

    private class Worker
    {
        private Queue<Action> workToDo = new Queue<Action>();
        private ManualResetEvent onWorkAdded = new ManualResetEvent(false);
        private bool isAlive = true;
        private Func<Queue<Action>> refillFunction;

        public Worker(Func<Queue<Action>> refillFunction)
        {
            this.refillFunction = refillFunction;
            new Thread(new ThreadStart(Run)).Start();
        }

        private void Run()
        {
            Action work = () => { };
            while (isAlive)
            {
                if (workToDo.Count == 0)
                    TryToRefill();

                if (workToDo.Count == 0)
                    onWorkAdded.WaitOne();

                lock (workToDo)
                {
                    work = workToDo.Dequeue();

                    if (workToDo.Count == 0)
                        onWorkAdded.Reset();
                }                

                work.Invoke();
            }
        }

        public void SetWorkAdded()
        {
            lock (workToDo)
            {
                TryToRefill();
            }
        }

        private void TryToRefill()
        {
            Queue<Action> refill = refillFunction.Invoke();

            foreach (Action a in refill)
            {
                workToDo.Enqueue(a);
            }

            if (workToDo.Count > 0)
                onWorkAdded.Set();
        }

        public void Destroy()
        {
            isAlive = false;
        }
    }

    private class ThreadWorkManagerUpdater : MonoBehaviour
    {
        private void Update()
        {
            ThreadWorkManager.Update();
        }

        private void OnDestroy()
        {
            ThreadWorkManager.Destroy();
        }
    }
}
