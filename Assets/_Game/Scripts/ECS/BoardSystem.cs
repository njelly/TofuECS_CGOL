using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS_CGOL.ECS
{
    public class BoardSystem : ISystem, ISystemEventListener<SetBoardStateInput>
    {
        public static event EventHandler<BoardStateChangedEventArgs> StateChanged;

        public void Initialize(Simulation s)
        {
            var bufferSize = s.Buffer<bool>().Size;
            var width = (int)Math.Round(Math.Sqrt(bufferSize));
            var boardStateChangedEvent = new BoardStateChangedEventArgs
            {
                BoardWidth = width,
                FlippedIndexes = new int[bufferSize],
                States = new bool[bufferSize],
            };
            
            s.ModifySingletonComponent((ref XorShiftRandom r) =>
            {
                var buffer = s.Buffer<bool>();
                for (var i = 0; i < buffer.Size; i++)
                {
                    var someInt = r.NextInt32();
                    var value = someInt > 0;
                    boardStateChangedEvent.FlippedIndexes[i] = i;
                    boardStateChangedEvent.States[i] = value;
                    buffer.Set(s.CreateEntity(), value);
                }
            });

            StateChanged?.Invoke(this, boardStateChangedEvent);
        }

        public unsafe void Process(Simulation s)
        {
            s.Buffer<bool>().ModifyUnsafe((i, buffer) =>
            {
                var offset = s.Buffer<bool>().Size;
                var width = (int)Math.Round(Math.Sqrt(offset));
                // creates a fixed pointer on the stack that you can use as an array
                var toFlip = stackalloc int[offset];
                var numToFlip = 0;
                while (i.Next())
                {
                    var numAlive = 0;
                    var index = i.Current;
                    
                    // NOTE: 'offset' is used here to avoid awkward values with the % operator.

                    // top left
                    if (buffer[(index + width - 1 + offset) % offset])
                        numAlive++;
                    
                    // top center
                    if (buffer[(index + width + offset) % offset])
                        numAlive++;

                    // top right
                    if (buffer[(index + width + 1 + offset) % offset])
                        numAlive++;

                    // middle left
                    if (buffer[(index - 1 + offset) % offset])
                        numAlive++;

                    // middle right
                    if (buffer[(index + 1 + offset) % offset])
                        numAlive++;

                    // bottom left
                    if (buffer[(index - width - 1 + offset) % offset])
                        numAlive++;

                    // bottom center
                    if (buffer[(index - width + offset) % offset])
                        numAlive++;

                    // bottom right
                    if (buffer[(index - width + 1 + offset) % offset])
                        numAlive++;

                    var isAlive = buffer[index];
                    bool doFlip;
                    if (isAlive)
                        doFlip = numAlive is < 2 or > 3;
                    else
                        doFlip = numAlive is 3;

                    if (doFlip)
                        toFlip[++numToFlip] = index;
                }
                
                var boardStateChangedEvent = new BoardStateChangedEventArgs
                {
                    BoardWidth = width,
                    FlippedIndexes = new int[numToFlip],
                    States = new bool[numToFlip],
                };
                
                for (var j = 0; j < numToFlip; j++)
                {
                    var flippedIndex = toFlip[j];
                    buffer[flippedIndex] = !buffer[flippedIndex];
                    boardStateChangedEvent.FlippedIndexes[j] = toFlip[j];
                    boardStateChangedEvent.States[j] = buffer[flippedIndex];
                }
                
                StateChanged?.Invoke(this, boardStateChangedEvent);
            });
        }

        public unsafe void OnSystemEvent(Simulation s, in SetBoardStateInput eventData)
        {
            var newValues = eventData.NewValues;
            s.Buffer<bool>().ModifyUnsafe((i, buffer) =>
            {
                fixed (bool* newValuesPtr = newValues)
                {
                    var size = sizeof(bool) * newValues.Length;
                    Buffer.MemoryCopy(newValuesPtr, buffer, size, size);
                }
                
                var boardStateChangedEvent = new BoardStateChangedEventArgs
                {
                    BoardWidth = (int)Math.Round(Math.Sqrt(s.Buffer<bool>().Size)),
                    FlippedIndexes = new int[newValues.Length],
                    States = newValues,
                };

                for (var j = 0; j < newValues.Length; j++)
                    boardStateChangedEvent.FlippedIndexes[j] = j;
                
                StateChanged?.Invoke(this, boardStateChangedEvent);
            });
        }
    }

    public class BoardStateChangedEventArgs : EventArgs
    {
        public int BoardWidth;
        public int[] FlippedIndexes;
        public bool[] States;
    }
}