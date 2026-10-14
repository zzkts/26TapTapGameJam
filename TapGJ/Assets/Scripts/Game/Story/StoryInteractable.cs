using UnityEngine;

public sealed class StoryInteractable : WorldInteractable
{
    public StoryBeat beat=StoryBeat.LabRecord;
    public bool autoSelectRecordByRegion=true;
    private StoryBeat Resolved=>beat==StoryBeat.LabRecord&&autoSelectRecordByRegion&&transform.position.x>38f?StoryBeat.ExperimentRecord:beat;
    public override string Title=>Resolved==StoryBeat.Core?"三号回流阀":Resolved==StoryBeat.ExperimentRecord?"实验记录 17—19":"维修日志 · NODE-03";
    public override string Detail=>Resolved==StoryBeat.Core?"查看最后记录，决定水的去向":"阅读留下来的记录 · E";
    public override bool CanUse(PlayerController player)
    {
        StorySession session=StorySession.Current;
        return base.CanUse(player)&&session!=null&&session.Ready&&!session.IsModal&&
            (Resolved!=StoryBeat.Core||session.GuardianCleared);
    }
    public override bool Interact(PlayerController player)=>CanUse(player)&&StorySession.Current.Reach(Resolved,transform.position,true);
}
