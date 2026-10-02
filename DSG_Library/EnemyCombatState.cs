using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Library
{
    public class EnemyCombatState
    {
        public DateTimeOffset NextShotTime { get; set; }

        public EnemyCombatState()
        {
            NextShotTime = DateTimeOffset.UtcNow;
        }
    }
}
