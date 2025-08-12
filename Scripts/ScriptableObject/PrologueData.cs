using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PrologueData", menuName = "Game/Prologue Data")]
public class PrologueData : ScriptableObject
{
    [SerializeField, TextArea(2, 10)] private List<string> storyTexts = new();
    
    public List<string> StoryTexts => storyTexts;
}