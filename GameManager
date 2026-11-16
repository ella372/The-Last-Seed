using System;
using UnityEngine;
using LastSeed.Domain;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private UIFeedback feedback;   // tërhiqe nga Inspector

    public GameState State { get; private set; }
    public RuleEngine Rules { get; private set; }

  
    public event Action StateChanged;

    private void Awake()
    {
        Instance = this;
        State = new GameState();
        Rules = new RuleEngine(State, feedback);
    }

    private void Start() { StateChanged?.Invoke(); }

    private void Update()
    {
      
        if (Input.GetKeyDown(KeyCode.R))
        {
            State.Reset();
            feedback.Show("Restart");
            StateChanged?.Invoke();
        }
    }

    public void NotifyChanged() { StateChanged?.Invoke(); }
}

