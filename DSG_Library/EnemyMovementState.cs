using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Library
{
    public sealed class EnemyMovementState
    {
        public int Direction { get; set; } = 1;
        public DateTimeOffset DirectionLockedUntil { get; set; }
        public DateTimeOffset NextDirectionDecision { get; set; }

        public EnemyMovementState(DateTimeOffset now, int directionDecisionMilliseconds)
        {
            NextDirectionDecision = now.AddMilliseconds(directionDecisionMilliseconds);
        }
    }
}
