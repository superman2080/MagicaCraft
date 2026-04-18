using UnityEngine;
using System.Collections.Generic;

public class DialogState : SequenceStateBase
{
    public List<DialogData> datas;
    private bool finishDialog = false;
    private DialogUI dialogUI;
    private float lastTimeScale;


    public DialogState(SequenceController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        lastTimeScale = Time.timeScale;
        Time.timeScale = 0;
        dialogUI = UIManager.Instance.Get<DialogUI>();
        dialogUI.Dialog.SetActive(true);
        dialogUI.SetDialog(datas);

        dialogUI.OnExit += () => finishDialog = true;
    }

    public override void Exit()
    {
        Time.timeScale = lastTimeScale;
        base.Exit();
    }

    public override bool FinishSequenceCondition() => finishDialog == true;
}