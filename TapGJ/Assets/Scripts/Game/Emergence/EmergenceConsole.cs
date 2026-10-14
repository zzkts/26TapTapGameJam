using UnityEngine;

public sealed class EmergenceConsole:WorldInteractable
{
    public enum ConsoleKind { PressureRelease, Calibration, BackflowSwitch }
    public ConsoleKind kind;
    public EmergenceBody body;
    public EmergenceGate gate;
    public string completionId;
    public override string Title=>kind==ConsoleKind.PressureRelease?"泄压阀 · 压力 "+(body!=null?body.Pressure:0)+" / 3":kind==ConsoleKind.Calibration?"高处的回流节点":"回流控制台";
    public override string Detail=>kind==ConsoleKind.PressureRelease?"水球蓄压也能驱动；水火相遇会更快":"记录这条回流通路 · E";
    public override bool CanUse(PlayerController player)=>base.CanUse(player)&&
        (kind==ConsoleKind.PressureRelease||!EmergenceProgress.Current.IsComplete(completionId));
    public override bool Interact(PlayerController player)
    {
        if(!CanUse(player))return false;
        if(kind==ConsoleKind.PressureRelease)return body!=null&&body.Pressure>=3&&body.ReleaseSteam();
        EmergenceProgress.Current?.Complete(completionId);gate?.OpenPermanently();return true;
    }
}
