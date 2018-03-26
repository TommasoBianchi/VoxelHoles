using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class ThreadWorkManager
{
    private static Queue<Action> mainThreadActions = new Queue<Action>();
    private static List<Worker> workers = new List<Worker>();
    private static int i = 0;

    private ThreadWorkManager() { }

    static ThreadWorkManager()
    {
        new GameObject().AddComponent<ThreadWorkManagerUpdater>().name = "ThreadWorkManagerUpdater";

        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            workers.Add(new Worker());
        }
    }

    public static void RequestWork(Action work)
    {
        //Thread thread = new Thread(new ThreadStart(work));
        //thread.Start();
        workers[i].AddWork(new List<Action>(new Action[] { work }));
        i = (i + 1) % workers.Count;
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

    private class Worker
    {
        private Queue<Action> workToDo = new Queue<Action>();
        private ManualResetEvent onWorkAdded = new ManualResetEvent(false);
        private bool isAlive = true;

        public Worker()
        {
            new Thread(new ThreadStart(Run)).Start();
        }

        private void Run()
        {
            Action work = () => { };
            while (isAlive)
            {
                lock (workToDo)
                {
                    if (workToDo.Count == 0)
                        onWorkAdded.WaitOne();

                    work = workToDo.Dequeue();

                    if (workToDo.Count == 0)
                        onWorkAdded.Reset();
                }

                work.Invoke();
            }
        }

        public void AddWork(List<Action> work)
        {
            lock (workToDo)
            {
                foreach (Action a in work)
                {
                    workToDo.Enqueue(a);
                }
                onWorkAdded.Set();
            }
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
