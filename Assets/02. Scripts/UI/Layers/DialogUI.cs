using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using TMPro;
using UnityEngine.UI;

[Serializable]
public class DialogData
{
    public float printTime;
    public string name;
    [TextArea(3, 10)] public string script;
    [Range(0, 1)] public float skipProgress = 0.75f;
}

public class DialogUI : SubUIManager
{
    public GameObject Dialog => dialog;
    [SerializeField] private GameObject dialog;
    [SerializeField] private TextMeshProUGUI dialogName;
    [SerializeField] private TextMeshProUGUI dialogScript;
    [SerializeField] private Button nextDialog;


    private Queue<DialogData> dialogQueue = new Queue<DialogData>();
    private Coroutine dialogCor;
    // 현재 데이터
    private DialogData nowData;
    //현재 텍스트 진행도
    private float progress;

    public Action OnExit;

    public override int SortingOrder => sortingOrder;
    [SerializeField] private int sortingOrder;

    private void Reset()
    {
        dialog = transform.Find("Dialog").gameObject;
        dialogName = dialog.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        dialogScript = dialog.transform.Find("Script").GetComponent<TextMeshProUGUI>();
        nextDialog = dialog.transform.Find("Next").GetComponent<Button>();
        dialog.SetActive(false);
    }

    protected override void Awake()
    {
        base.Awake();
        Reset();
    }

    public void SetDialog(List<DialogData> datas, bool startImmediately = true)
    {
        nextDialog.onClick.RemoveAllListeners();
        nextDialog.onClick.AddListener(() =>
        {
            NextDialog();
        });
        foreach (var data in datas)
        {
            dialogQueue.Enqueue(data);
        }
        if (startImmediately)
            NextDialog();
    }

    private void NextDialog()
    {
        if (nowData?.skipProgress > progress)
            return;

        if (dialogCor != null)
        {
            StopCoroutine(dialogCor);
            dialogCor = null;
            dialogName.text = nowData.name;
            dialogScript.text = nowData.script;
        }

        if (dialogQueue.TryDequeue(out DialogData data))
        {
            nowData = data;
            dialogCor = StartCoroutine(PrintDialogScript(nowData));
        }
        else
        {
            OnExit?.Invoke();
            dialog.SetActive(false);
        }
    }

    private IEnumerator PrintDialogScript(DialogData data)
    {
        dialog.SetActive(true);
        progress = 0f;
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
            float nowScriptLength = CountVisibleChars(dialogScript.text);
            progress = nowScriptLength / (float)visibleCount;
            yield return waitTime;
        }
        progress = 1f;
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