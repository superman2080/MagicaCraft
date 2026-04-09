using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CoroutineRunner : Singleton<CoroutineRunner>
{
    private Dictionary<int, Coroutine> runningCoroutines = new Dictionary<int, Coroutine>();
    private int nextCoroutineID = 0;

    public int StartManagedCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null)
            return -1;

        int coroutineID = nextCoroutineID++;
        Coroutine runningCoroutine = StartCoroutine(ExecuteCorouine(coroutine, coroutineID));
        runningCoroutines[coroutineID] = runningCoroutine;

        return coroutineID;
    }

    public void StopManagedCoroutine(int id)
    {
        if (runningCoroutines.TryGetValue(id, out Coroutine co))
        {
            if (co != null)
                StopCoroutine(co);
            runningCoroutines.Remove(id);
        }
    }

    private IEnumerator ExecuteCorouine(IEnumerator co, int id)
    {
        yield return co;

        runningCoroutines.Remove(id);
    }

    public static int Start(IEnumerator co)
    {
        return Instance.StartManagedCoroutine(co);
    }

    public static void Stop(int id) => Instance.StopManagedCoroutine(id);

    public bool IsCoroutineRunning(int coroutineId)
    {
        return runningCoroutines.ContainsKey(coroutineId) && runningCoroutines[coroutineId] != null;
    }
}