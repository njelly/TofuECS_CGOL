using System;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS_CGOL.ECS;
using Tofunaut.TofuECS.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Tofunaut.TofuECS_CGOL
{
    public class SimulationRunner : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float _tickInterval;
        [SerializeField] private bool _paused;
        
        [Header("Simulation Config")]
        [SerializeField, Range(2, 1024)] private int _boardSize;
        [SerializeField] private int _seed;

        [Header("UI")]
        [SerializeField] private Button _tickButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;
        [SerializeField] private Text _currentTickLabel;
        
        private Simulation _simulation;
        private Texture2D _tex;
        private float _tickTimer;

        private void Start()
        {
#region BASIC UNITY SETUP
            // Create a texture manually, this will be the view for our ECS state
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
            
            // Instantiate a GameObject with a sprite renderer component... basic Unity stuff.
            var spriteRendererGo = new GameObject("SpriteRenderer", typeof(SpriteRenderer));
            var spriteRenderer = spriteRendererGo.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(_tex, new Rect(Vector2.zero, Vector2.one * _boardSize), Vector2.one * 0.5f);
            
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(TickButton_OnClick);
            
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(PauseButton_OnClick);
#endregion
            
            // create a system and subscribe to an event so that we can respond to state changes
            var boardSystem = new BoardSystem();
            boardSystem.StateChanged += BoardSystem_StateChanged;

            // create our simulation 
            _simulation = new Simulation(new UnityLogService(),
                new ISystem[]
                {
                    boardSystem
                });

            // register a fast RNG component (TofuECS.Utilities) as a singleton component
            _simulation.RegisterSingletonComponent(new XorShiftRandom(Convert.ToUInt64(_seed)));
            _simulation.RegisterComponent<bool>(_boardSize * _boardSize);
            _simulation.Initialize();
        }

        private void Update()
        {
            _currentTickLabel.text = $"Tick: {_simulation.CurrentTick}";

            if (_paused) 
                return;
            
            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval)
                return;
            
            _tickTimer -= _tickInterval;
            _simulation.Tick();
        }

        private void TickButton_OnClick()
        {
            _tickTimer = 0f;
            _simulation.Tick();
        }

        private void PauseButton_OnClick()
        {
            _paused = !_paused;
            _pauseButtonLabel.text = _paused ? "Play" : "Pause";

            if (_paused)
                return;
            
            _tickTimer = 0f;
            _simulation.Tick();
        }

        private void BoardSystem_StateChanged(object sender, BoardStateChangedEventArgs e)
        {
            for (var i = 0; i < e.States.Length; i++)
            {
                var flippedIndex = e.FlippedIndexes[i];
                _tex.SetPixel(flippedIndex % e.BoardWidth, flippedIndex / e.BoardWidth, e.States[i] ? Color.white : Color.black);
            }
            
            _tex.Apply();
        }
    }
}