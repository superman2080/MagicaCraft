using UnityEngine;
using System.Collections.Generic;

public class TutorialDialogState : SequenceStateBase
{
    public List<DialogData> datas;
    private bool finishDialog = false;
    private Entity[] entities;
    private DialogUI dialogUI;

    public TutorialDialogState(SequenceController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        entities = FindObjectsByType<Entity>(FindObjectsSortMode.InstanceID);
        //foreach (var entity in entities)
        //{
        //    entity.canBehavior = false;
        //}
        //UIManager.Instance.dialogSystem.SetDialog(datas, () => {
        //    finishDialog = true;
        //});
    }

    public override void Execute()
    {
        //if (finishDialog == true)
        //    caster.SetNextTutorial();
    }

    public override void Exit()
    {
        foreach (var entity in entities)
        {
            //entity.canBehavior = true;
        }
    }
}