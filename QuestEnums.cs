using UnityEngine;

public enum QuestState
{
    Unassigned,
    Active,
    Completed,
    Failed
}

public enum GoalType
{
    Gathering,
    TalkToNPC,
    ScorePoints,
    DeliverToArea
}

public enum QuestIconState
{
    None,
    Available,
    ReadyToTurnIn
}
