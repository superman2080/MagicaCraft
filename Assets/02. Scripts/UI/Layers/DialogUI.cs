using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using TMPro;
using UnityEngine.UI;

[Serializable]
public struct DialogData
{
    public float printTime;
    public string name;
    [TextArea(3, 10)] public string script;
}
public class DialogUI : SubUIManager
{
    [SerializeField] private GameObject dialog;
    [SerializeField] private TextMeshProUGUI dialogName;
    [SerializeField] private TextMeshProUGUI dialogScript;
    [SerializeField] private Button nextDialog;
    private Queue<DialogData> dialogQueue = new Queue<DialogData>();
    private Coroutine dialogCor;
    private DialogData nowData;

    public override int SortingOrder => sortingOrder;
    [SerializeField] private int sortingOrder;

    protected override void Awake()
    {
        base.Awake();
        dialog ??= transform.Find("Dialog").gameObject;
        dialogName ??= dialog.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        dialogScript ??= dialog.transform.Find("Script").GetComponent<TextMeshProUGUI>();
        nextDialog = dialog.transform.Find("Next").GetComponent<Button>();
        dialog.SetActive(false);
    }

    public void SetDialog(List<DialogData> datas, Action exit, bool startImmediately = true)
    {
        nextDialog.onClick.RemoveAllListeners();
        nextDialog.onClick.AddListener(() =>
        {
            NextDialog(exit);
        });
        foreach (var data in datas)
        {
            dialogQueue.Enqueue(data);
        }
        if (startImmediately)
            NextDialog(exit);
    }

    private void NextDialog(Action action)
    {
        if (dialogCor != null)
        {
            StopCoroutine(dialogCor);
            dialogCor = null;
            dialogName.text = nowData.name;
            dialogScript.text = nowData.script;
        }
        else if (dialogQueue.TryDequeue(out DialogData data))
        {
            nowData = data;
            dialogCor = StartCoroutine(PrintDialogScript(nowData));
        }
        else
        {
            action?.Invoke();
            dialog.SetActive(false);
        }
    }

    private IEnumerator PrintDialogScript(DialogData data)
    {
        dialog.SetActive(true);
        dialogName.text = data.name;
        dialogScript.text = "";
        int visibleCount = CountVisibleChars(data.script);
        WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(
            visibleCount > 0 ? data.printTime / visibleCount : data.printTime);
        for (int i = 0; i < data.script.Length; i++)
        {
            if (data.script[i] == '<')
            {
                int closeIndex = data.script.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    dialogScript.text += data.script.Substring(i, closeIndex - i + 1);
                    i = closeIndex;
                    continue;
                }
            }
            dialogScript.text += data.script[i];
            yield return waitTime;
        }
        dialogCor = null;
    }

    private int CountVisibleChars(string script)
    {
        int count = 0;
        for (int i = 0; i < script.Length; i++)
        {
            if (script[i] == '<')
            {
                int closeIndex = script.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    i = closeIndex;
                    continue;
                }
            }
            count++;
        }
        return count;
    }
}