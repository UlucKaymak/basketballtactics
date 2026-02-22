using UnityEngine;
using System;
using NaughtyAttributes;

public enum GameState { Initializing, Idle, Busy, Resolution, GameOver }

public class StateManager : MonoBehaviour
{
    public static StateManager Instance;

    [Header("Status")]
    [ReadOnly] [SerializeField] private GameState _currentState = GameState.Initializing;
    
    public GameState CurrentState => _currentState;

    // State değiştiğinde tetiklenecek olaylar
    public static event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetState(GameState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        Debug.Log($"<color=cyan>[StateManager] State Changed to: {newState}</color>");
        
        // Olayı (Event) ateşle
        OnStateChanged?.Invoke(newState);
    }

    public bool IsIdle() => _currentState == GameState.Idle;
    public bool IsBusy() => _currentState == GameState.Busy;
    public bool IsResolution() => _currentState == GameState.Resolution;
}
