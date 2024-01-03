using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InGameUI : MonoBehaviour
{
    public StateMachine state = new StateMachine();

    public BasicState basicState;
    public SkillCanvasState skillCanvasState;
    
    void Start()
    {
        state.ChangeState(basicState);
        basicState.Initialize(this);
        skillCanvasState.Initialize(this);
    }

    void Update()
    {
        state.Update();
    }

    [Serializable]
    public class BasicState : IState
    {
        private InGameUI inGameUI;
        private Animator animator;

        [SerializeField]
        private GameObject panel;

        public BasicState(GameObject panel)
        {
            this.panel = panel;
        }
        
        public void Initialize(InGameUI inGameUI)
        {
            this.inGameUI = inGameUI;
            animator = inGameUI.GetComponent<Animator>();
        }
        
        public void Enter()
        {
            panel.SetActive(true);
        }
        
        public void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                // Trigger skill canvas
                animator.SetBool("SpacePressed", true);
            }
        }

        public void Exit()
        {
            panel.SetActive(false);
        }
    }
    
    [Serializable]
    public class SkillCanvasState : IState
    {
        private InGameUI inGameUI;
        private Animator animator;

        [SerializeField]
        private GameObject panel;

        public SkillCanvasState(GameObject panel)
        {
            this.panel = panel;
        }
        
        public void Initialize(InGameUI inGameUI)
        {
            this.inGameUI = inGameUI;
            animator = inGameUI.GetComponent<Animator>();
        }

        public void Enter()
        {
            panel.SetActive(true);
        }
        
        public void Update()
        {
            if (!Input.GetKey(KeyCode.Space))
            {
                animator.SetBool("SpacePressed", false);
                inGameUI.state.ChangeState(inGameUI.basicState);
            }
        }

        public void Exit()
        {
            panel.SetActive(false);
        }
    }

    public void BasicToSkillCanvasAnimationEnd()
    {
        state.ChangeState(skillCanvasState);
    }
}
