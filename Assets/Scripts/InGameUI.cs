using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public StateMachine state = new StateMachine();

    public BasicState basicState;
    public SkillCanvasState skillCanvasState;

    public Queue<IState> stateQueue = new Queue<IState>();
    
    void Start()
    {
        state.ChangeState(basicState);
        basicState.Initialize(this);
        skillCanvasState.Initialize(this);
    }

    void Update()
    {
        if(stateQueue.Count > 0)
            state.ChangeState(stateQueue.Dequeue());
        state.Update();

        if (!state.CompareType(basicState))
        {
            basicState.Disabled();
            print(1);
        }

        if (!state.CompareType(skillCanvasState))
        {
            skillCanvasState.Disabled();
            print(2);
        }
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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Trigger skill canvas
                animator.SetTrigger("OnSpacePress");
            }
        }

        public void Exit()
        {
            panel.SetActive(false);
        }

        public void Disabled()
        {
            if(panel.activeSelf)
                panel.SetActive(false);
        }
    }
    
    [Serializable]
    public class SkillCanvasState : IState
    {
        private InGameUI inGameUI;
        private Animator animator;

        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform[] tiles;
        private Image[] tileImages;

        private const int TileSize = 140;
        [SerializeField] private Vector3 activeTileScale = new Vector3(1.2f, 1.2f, 1f);
        private const float ColorChangeSpeed = 0.5f;
        private Color disabledTileColor;

        private List<int> activeTileIndexes;

        [SerializeField] private LineRenderer lineRenderer;

        public SkillCanvasState(GameObject panel)
        {
            this.panel = panel;
        }
        
        public void Initialize(InGameUI inGameUI)
        {
            this.inGameUI = inGameUI;
            animator = inGameUI.GetComponent<Animator>();
            tileImages = tiles.Select(t => t.GetComponent<Image>()).ToArray();
            disabledTileColor = tileImages[0].color;
        }

        public void Enter()
        {
            activeTileIndexes = new List<int>();
            panel.SetActive(true);
        }

        public void Update()
        {
            // Ensure panel is active
            if (!panel.activeSelf)
                panel.SetActive(true);

            Vector2 mouseScreenPos = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
                activeTileIndexes = new List<int>();

            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];

                if (mouseScreenPos.x > Screen.width / 2 + tile.anchoredPosition.x - TileSize / 2f
                    && mouseScreenPos.x < Screen.width / 2 + tile.anchoredPosition.x + TileSize / 2f
                    && mouseScreenPos.y > Screen.height / 2 + tile.anchoredPosition.y - TileSize / 2f
                    && mouseScreenPos.y < Screen.height / 2 + tile.anchoredPosition.y + TileSize / 2f
                    && Input.GetMouseButton(0))
                {
                    // Activate tile
                    if (!activeTileIndexes.Contains(i))
                    {
                        activeTileIndexes.Add(i);
                    }
                }

                if (activeTileIndexes.Contains(i))
                {
                    tiles[i].GetComponent<Image>().color = Color.white;
                    print(tileImages[i].color.a);
                    //print(tiles[i].localScale);
                    tiles[i].localScale = Vector3.Lerp(tiles[i].localScale, activeTileScale, ColorChangeSpeed);
                }
                else
                {
                    tiles[i].localScale = Vector3.Lerp(tiles[i].localScale, Vector3.one, ColorChangeSpeed);
                    tileImages[i].color = disabledTileColor;
                }

            }

            if (Input.GetMouseButtonUp(0) && activeTileIndexes.Count > 0)
            {
                print("Skill Triggered: " + activeTileIndexes.Count);
                activeTileIndexes = new List<int>();
            }

            // Handle line renderer
            if (activeTileIndexes.Count > 0)
            {
                lineRenderer.positionCount = activeTileIndexes.Count + 1;
                for(int i = 0; i < activeTileIndexes.Count; i++)
                {
                    lineRenderer.SetPosition(i, Camera.main.ScreenToWorldPoint(tiles[activeTileIndexes[i]].position)
                                                - new Vector3(0,0, Camera.main.transform.position.z));
                }
                lineRenderer.SetPosition(activeTileIndexes.Count, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (activeTileIndexes.Count > 0)
                {
                    print("Skill Triggered: " + activeTileIndexes.Count);
                    activeTileIndexes = new List<int>();
                }
                animator.SetTrigger("OnSpaceRelease");
                inGameUI.state.ChangeState(inGameUI.basicState);
            }
        }

        public void Exit()
        {
            panel.SetActive(false);
            activeTileIndexes = new List<int>();
        }
        
        public void Disabled()
        {
            if(panel.activeSelf)
                panel.SetActive(false);
        }
    }

    public void BasicToSkillCanvasAnimationEnd()
    {
        stateQueue.Enqueue(skillCanvasState);
    }
}
