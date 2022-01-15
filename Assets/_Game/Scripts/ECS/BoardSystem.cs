using System;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS_COGL.ECS
{
    public unsafe class BoardSystem : ISystem
    {
        public event EventHandler<BoardStateChangedEventArgs> StateChanged;

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

        public void Process(Simulation s)
        {
            s.Buffer<bool>().ModifyUnsafe((i, buffer) =>
            {
                var offset = s.Buffer<bool>().Size;
                var width = (int)Math.Round(Math.Sqrt(offset));
                var toFlip = stackalloc int[offset];
                var numToFlip = 0;
                while (i.Next())
                {
                    var numAlive = 0;
                    
                    // NOTE: 'offset' is used here to avoid awkward values with the % operator.

                    // top left
                    if (buffer[(i + width - 1 + offset) % offset])
                        numAlive++;
                    
                    // top center
                    if (buffer[(i + width + offset) % offset])
                        numAlive++;

                    // top right
                    if (buffer[(i + width + 1 + offset) % offset])
                        numAlive++;

                    // middle left
                    if (buffer[(i - 1 + offset) % offset])
                        numAlive++;

                    // middle right
                    if (buffer[(i + 1 + offset) % offset])
                        numAlive++;

                    // bottom left
                    if (buffer[(i - width - 1 + offset) % offset])
                        numAlive++;

                    // bottom center
                    if (buffer[(i - width + offset) % offset])
                        numAlive++;

                    // bottom right
                    if (buffer[(i - width + 1 + offset) % offset])
                        numAlive++;

                    var isAlive = buffer[i];
                    bool doFlip;
                    if (isAlive)
                        doFlip = numAlive is < 2 or > 3;
                    else
                        doFlip = numAlive is 3;

                    if (doFlip)
                        toFlip[++numToFlip] = i;
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
    }

    public class BoardStateChangedEventArgs : EventArgs
    {
        public int BoardWidth;
        public int[] FlippedIndexes;
        public bool[] States;
    }
}