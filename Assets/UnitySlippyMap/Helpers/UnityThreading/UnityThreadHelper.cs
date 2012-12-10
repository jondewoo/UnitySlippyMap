using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System;

public class UnityThreadHelper : MonoBehaviour
{
    #region Singleton stuff

    private static UnityThreadHelper instance = null;

    public static void EnsureHelper()
    {
        if (null == (object)instance)
        {
            instance = FindObjectOfType(typeof(UnityThreadHelper)) as UnityThreadHelper;
            if (null == (object)instance)
            {
                var go = new GameObject("[UnityThreadHelper]");
                go.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                instance = go.AddComponent<UnityThreadHelper>();
                instance.EnsureHelperInstance();
            }
        }
    }

    private static UnityThreadHelper Instance
    {
        get
        {
            EnsureHelper();
            return instance;
        }
    }

    #endregion

    #region Private members

    /*
    private static System.Reflection.MethodInfo waitOneInt;
    private static System.Reflection.MethodInfo waitOneTimeSpan;
     */

    private static bool isWebPlayer;
    public static bool IsWebPlayer
    {
        get { return isWebPlayer; }
    }

    #endregion

    /// <summary>
    /// Returns the GUI/Main Dispatcher.
    /// </summary>
    public static UnityThreading.Dispatcher Dispatcher
    {
        get
        {
            return Instance.CurrentDispatcher;
        }
    }

    /// <summary>
    /// Returns the TaskDistributor.
    /// </summary>
    public static UnityThreading.TaskDistributor TaskDistributor
    {
        get
        {
            return Instance.CurrentTaskDistributor;
        }
    }

    private UnityThreading.Dispatcher dispatcher;
    public UnityThreading.Dispatcher CurrentDispatcher
    {
        get
        {
            return dispatcher;
        }
    }

    private UnityThreading.TaskDistributor taskDistributor;
    public UnityThreading.TaskDistributor CurrentTaskDistributor
    {
        get
        {
            return taskDistributor;
        }
    }

    private void EnsureHelperInstance()
    {
        if (dispatcher == null)
            dispatcher = new UnityThreading.Dispatcher();

        if (taskDistributor == null)
            taskDistributor = new UnityThreading.TaskDistributor();

        isWebPlayer = Application.isWebPlayer;
    }

    public static bool WaitOne(ManualResetEvent evt, int ms)
    {
        System.Reflection.MethodInfo waitOneInt;
        Type type = evt.GetType();
        waitOneInt = type.GetMethod("WaitOne", new Type[1] { typeof(int) });

        return (bool)waitOneInt.Invoke(evt, new object[1] { ms });
    }

    public static bool WaitOne(ManualResetEvent evt, TimeSpan ts)
    {
        System.Reflection.MethodInfo waitOneTimeSpan;
        Type type = evt.GetType();
        waitOneTimeSpan = type.GetMethod("WaitOne", new Type[1] { typeof(TimeSpan) });

        return (bool)waitOneTimeSpan.Invoke(evt, new object[1] { ts });
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action<UnityThreading.ActionThread> action, bool autoStartThread)
    {
        Instance.EnsureHelperInstance();

        System.Action<UnityThreading.ActionThread> actionWrapper = currentThread =>
            {
                try
                {
                    action(currentThread);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }
            };
        var thread = new UnityThreading.ActionThread(actionWrapper, autoStartThread);
        Instance.RegisterThread(thread);
        return thread;
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action<UnityThreading.ActionThread> action)
    {
        return CreateThread(action, true);
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action action, bool autoStartThread)
    {
        return CreateThread((thread) => action(), autoStartThread);
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ActionThread CreateThread(System.Action action)
    {
        return CreateThread((thread) => action(), true);
    }

    #region Enumeratable

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<UnityThreading.ThreadBase, IEnumerator> action, bool autoStartThread)
    {
        Instance.EnsureHelperInstance();

        var thread = new UnityThreading.EnumeratableActionThread(action, autoStartThread);
        Instance.RegisterThread(thread);
        return thread;
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<UnityThreading.ThreadBase, IEnumerator> action)
    {
        return CreateThread(action, true);
    }

    /// <summary>
    /// Creates new thread which runs the given action. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The enumeratable action which the new thread should run.</param>
    /// <param name="autoStartThread">True when the thread should start immediately after creation.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<IEnumerator> action, bool autoStartThread)
    {
        System.Func<UnityThreading.ThreadBase, IEnumerator> wrappedAction = (thread) => { return action(); };
        return CreateThread(wrappedAction, autoStartThread);
    }

    /// <summary>
    /// Creates new thread which runs the given action and starts it after creation. The given action will be wrapped so that any exception will be catched and logged.
    /// </summary>
    /// <param name="action">The action which the new thread should run.</param>
    /// <returns>The instance of the created thread class.</returns>
    public static UnityThreading.ThreadBase CreateThread(System.Func<IEnumerator> action)
    {
        System.Func<UnityThreading.ThreadBase, IEnumerator> wrappedAction = (thread) => { return action(); };
        return CreateThread(wrappedAction, true);
    }

    #endregion

    List<UnityThreading.ThreadBase> registeredThreads = new List<UnityThreading.ThreadBase>();
    public void RegisterThread(UnityThreading.ThreadBase thread)
    {
        if (registeredThreads.Contains(thread))
        {
            return;
        }

        registeredThreads.Add(thread);
    }

    void OnDestroy()
    {
        foreach (var thread in registeredThreads)
            thread.Dispose();

        if (dispatcher != null)
            dispatcher.Dispose();
        dispatcher = null;

        if (taskDistributor != null)
            taskDistributor.Dispose();
        taskDistributor = null;
    }

    void Update()
    {
        if (dispatcher != null)
            dispatcher.ProcessTasks();

        var finishedThreads = registeredThreads.Where(thread => !thread.IsAlive).ToArray();
        foreach (var finishedThread in finishedThreads)
        {
            finishedThread.Dispose();
            registeredThreads.Remove(finishedThread);
        }
    }
}