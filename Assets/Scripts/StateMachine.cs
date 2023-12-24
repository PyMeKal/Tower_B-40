using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Designed by GPT-4.
// Used for holding current state of various AIs and accessing them through IState.

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

public class StateMachine {
    private IState currentState;

    public void ChangeState(IState newState) {
        if (currentState != null)
            currentState.Exit();

        currentState = newState;
        currentState.Enter();
    }

    public void Update() {
        if (currentState != null)
            currentState.Update();
    }
}
