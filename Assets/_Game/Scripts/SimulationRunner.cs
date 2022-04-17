using System;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS_CGOL.ECS;
using Tofunaut.TofuECS.Utilities;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Random = System.Random;

namespace Tofunaut.TofuECS_CGOL
{
    public class SimulationRunner : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float _tickInterval;
        [SerializeField] private bool _paused;
        
        [Header("Simulation Config")]
        [SerializeField, Range(2, 1024)] private int _boardSize;
        [SerializeField] private int _seed;
        //[SerializeField, Range(0f, 0.1f)] private float _perlinScale;
        [SerializeField, Range(0f, 0.01f)] private float _randomStatic;

        [Header("UI")]
        [SerializeField] private Button _tickButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _randomizeButton;
        [SerializeField] private Text _pauseButtonLabel;
        [SerializeField] private Text _currentTickLabel;
        [SerializeField] private Text _fpsLabel;
        
        private Simulation _simulation;
        private Texture2D _tex;
        private float _tickTimer;
        private float _prevRandomStatic;
        private float _fpsTimer;
        private int _fpsCounter;

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
            
            _randomizeButton.onClick.RemoveAllListeners();
            _randomizeButton.onClick.AddListener(RandomizeButton_OnClick);
#endregion
            
            // subscribe to an event so that we can respond to state changes
            BoardSystem.StateChanged += BoardSystem_StateChanged;

            // create our simulation 
            _simulation = new Simulation(new UnityLogService(),
                new ISystem[]
                {
                    new BoardSystem()
                });

            // register a fast RNG component (TofuECS.Utilities) as a singleton component
            _simulation.RegisterSingletonComponent(new XorShiftRandom(Convert.ToUInt64(_seed)));
            var index = _simulation.RegisterAnonymousComponent<bool>(_boardSize * _boardSize);
            _simulation.RegisterSingletonComponent(new BoardData
            {
                BufferIndex = index,
                BoardSize = _boardSize,
                StaticProbability = Convert.ToDouble(_randomStatic),
            });
            _simulation.Initialize();
            
            // randomize!
            RandomizeButton_OnClick();
        }

        private void Update()
        {
            UpdateRandomStaticValue();
            UpdateCurrentTickLabel();
            UpdateCheckIfReadyToTick();
            UpdateFPSLabel();
        }

        private void UpdateRandomStaticValue()
        {
            if (Math.Abs(_prevRandomStatic - _randomStatic) > float.Epsilon)
            {
                _prevRandomStatic = _randomStatic;
                _simulation.SystemEvent(new SetStaticProbabilityInput
                {
                    StaticProbability = Convert.ToDouble(_randomStatic),
                });
            }
        }

        private void UpdateCurrentTickLabel()
        {
            _currentTickLabel.text = $"Tick: {_simulation.CurrentTick}";
        }

        private void UpdateCheckIfReadyToTick()
        {
            if (_paused) 
                return;
            
            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval)
                return;
            
            _tickTimer -= _tickInterval;
            TickSimulationWithProfile();
        }

        private void UpdateFPSLabel()
        {
            _fpsCounter++;
            _fpsTimer += Time.deltaTime;

            if (_fpsTimer < 1f)
                return;

            _fpsLabel.text = $"FPS: {_fpsCounter}";
            _fpsTimer -= 1f;
            _fpsCounter = 0;
        }

        private void TickButton_OnClick()
        {
            _tickTimer = 0f;
            TickSimulationWithProfile();
        }

        private void PauseButton_OnClick()
        {
            _paused = !_paused;
            _pauseButtonLabel.text = _paused ? "Play" : "Pause";

            if (_paused)
                return;
            
            _tickTimer = 0f;
            TickSimulationWithProfile();
        }

        private void RandomizeButton_OnClick()
        {
            //UnityEngine.Random.InitState(_seed);
            //
            //var newValues = new bool[_boardSize * _boardSize];
            //if (_perlinScale % 1f == 0f)
            //    _perlinScale += 0.001f;
            //
            //for (var i = 0; i < newValues.Length; i++)
            //{
            //    var x = i % _boardSize;
            //    var y = i / _boardSize;
            //    newValues[i] = Mathf.PerlinNoise(x * _perlinScale, y * _perlinScale) * UnityEngine.Random.value > 0.5f;
            //}
            //
            //_simulation.SystemEvent(new SetBoardStateInput
            //{
            //    NewValues = newValues,
            //});
        }

        private void BoardSystem_StateChanged(BoardStateChangedEventData e)
        {
            for (var i = 0; i < e.NumToFlip; i++)
            {
                var flippedIndex = e.FlippedIndexes[i];
                var x = flippedIndex % e.BoardSize;
                var y = flippedIndex / e.BoardSize;
                var color = e.States[flippedIndex] ? Color.white : Color.black;
                _tex.SetPixel(x, y, color);
            }
            
            _tex.Apply();
        }

        private void TickSimulationWithProfile()
        {
            Profiler.BeginSample("Simulation.Tick()");
            _simulation.Tick();
            Profiler.EndSample();
        }
    }
}