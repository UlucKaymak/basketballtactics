using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int RollD6()
    {
        return UnityEngine.Random.Range(1, 7);
    }

    public int Roll2D6()
    {
        return UnityEngine.Random.Range(1, 7) + UnityEngine.Random.Range(1, 7);
    }

    /// <summary>
    /// Performs a single D6 roll with UI animation.
    /// </summary>
    public void RollD6WithUI(Color color, Action<int> onComplete, string label = "", string finalNoteFormat = "{0}")
    {
        int roll = RollD6();
        
        // Final note preparation (e.g., "4 + 2 = 6 (DC 5)")
        // We might want to pass more info for the note, but let's keep it flexible
        
        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll, color, null, null, label, () => {
            onComplete?.Invoke(roll);
        }, "")); // Note will be handled by the caller or we can improve this
    }

    /// <summary>
    /// Performs a Versus roll (e.g., Attack vs Defence) with UI animation.
    /// </summary>
    public void RollVersusWithUI(Color color1, Color color2, Action<int, int> onComplete, string label = "vs")
    {
        int roll1 = RollD6();
        int roll2 = RollD6();

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll1, color1, roll2, color2, label, () => {
            onComplete?.Invoke(roll1, roll2);
        }, ""));
    }
}
