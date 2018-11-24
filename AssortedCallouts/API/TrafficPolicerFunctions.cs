using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Traffic_Policer.API;

namespace AssortedCallouts.API
{
    internal static class TrafficPolicerFunctions
    {
        public static void SetPedAsDrunk(Ped ped)
        {
            Functions.SetPedAlcoholLevel(ped, Functions.GetRandomOverTheLimitAlcoholLevel());
        }

        public static void SetPedHasDrugsInSystem(Ped ped)
        {
            int roll = AssortedCalloutsHandler.rnd.Next(9);
            bool cannabis = false;
            bool cocaine = false; 
            if (roll < 3)
            {
                cannabis = true;
            }
            else if (roll < 6)
            {
                cocaine = true;
            }
            else
            {
                cannabis = true;
                cocaine = true;
            }
            Functions.SetPedDrugsLevels(ped, cannabis, cocaine);
        }
    }
}
