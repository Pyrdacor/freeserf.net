using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK
{
    internal class IndexPool
    {
        Dictionary<int, bool> assignedIndices = new Dictionary<int, bool>();
        int firstFree = 0;

        public int AssignNextFreeIndex()
        {
            bool firstRun = true;

            while (assignedIndices.ContainsKey(firstFree) && assignedIndices[firstFree])
            {
                if (++firstFree == int.MaxValue)
                {
                    if (!firstRun)
                        throw new Exception("Now free index available.");

                    firstFree = 0;
                    firstRun = false;
                }
            }

            assignedIndices[firstFree] = true;

            return firstFree;
        }

        public void UnassignIndex(int index)
        {
            assignedIndices[index] = false;
        }
    }
}
