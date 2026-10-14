using System.Collections;
using UnityEngine;

/// <summary>Persists authored chapter rewards alongside the terminal checkpoint.</summary>
public sealed class CompleteRewardState : MonoBehaviour
{
    public string rewardId;
    private string Key => "reward:" + rewardId;

    private IEnumerator Start()
    {
        while (StorySession.Current != null && !StorySession.Current.Ready) yield return null;
        StorySession session = StorySession.Current;
        if (session != null && !string.IsNullOrEmpty(rewardId) && session.WasTriggerCompleted(Key))
            gameObject.SetActive(false);
    }

    public static bool IsAvailable(Component reward)
    {
        CompleteRewardState state = reward.GetComponent<CompleteRewardState>();
        if (state == null || string.IsNullOrEmpty(state.rewardId)) return true;
        StorySession session = StorySession.Current;
        return session != null && session.Ready && !session.WasTriggerCompleted(state.Key);
    }

    public static void Claim(Component reward)
    {
        CompleteRewardState state = reward.GetComponent<CompleteRewardState>();
        if (state == null || string.IsNullOrEmpty(state.rewardId)) return;
        StorySession session = StorySession.Current;
        if (session != null && session.Ready) session.RegisterTriggerComplete(state.Key);
    }
}
