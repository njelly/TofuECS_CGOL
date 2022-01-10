using System;
using TMPro;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS_COGL.ECS;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Tofunaut.TofuECS_COGL
{
    public class SimulationRunner : MonoBehaviour
    {
        [Header("Simulation Config")]
        [SerializeField, Range(2, COGLConfig.MaxBoardSize)] private int _boardSize;
        [SerializeField] private int _seed;
        
        [Header("UI")]
        [SerializeField] private Button _tickButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;
        
        private Simulation _simulation;
        private Texture2D _tex;
        private bool _isPaused;

        private void Start()
        {
            _tex = new Texture2D(_boardSize, _boardSize)
            {
                filterMode = FilterMode.Point
            };
            for (var x = 0; x < _boardSize; x++)
            {
                for (var y = 0; y < _boardSize; y++)
                {
                    _tex.SetPixel(x, y, Color.black);
                }
            }
            _tex.Apply();
            
            var spriteRendererGo = new GameObject("SpriteRenderer", typeof(SpriteRenderer));
            var spriteRenderer = spriteRendererGo.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(_tex, new Rect(Vector2.zero, Vector2.one * _boardSize), Vector2.one * 0.5f);
            
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(TickButton_OnClick);
            
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(PauseButton_OnClick);

            var ecsDatabase = new ECSDatabase();
            ecsDatabase.RegisterSingleton(new COGLConfig
            {
                BoardSize = _boardSize,
            });
            ecsDatabase.Seal();

            _simulation = new Simulation(ecsDatabase, new UnityLogService(), Convert.ToUInt64(_seed),
                new ISystem[]
                {
                    new BoardSystem(),
                });
            _simulation.RegisterSingletonComponent<Board>();
            _simulation.Initialize();
        }

        private void TickButton_OnClick()
        {
            _simulation.Tick();
            Debug.Log("tick");
        }

        private void PauseButton_OnClick()
        {
            _isPaused = !_isPaused;
            _pauseButtonLabel.text = _isPaused ? "Play" : "Pause";
            
            if(!_isPaused)
                _simulation.Tick();
        }
    }
}