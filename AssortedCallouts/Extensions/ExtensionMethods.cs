using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using System.Drawing;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Extensions
{
    
    static class ExtensionMethods
    {
        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static bool GetClosestVehicleNodeWithHeading(this Vector3 StartPoint, out Vector3 SpawnPoint, out float Heading)
        {
            Vector3 TempSpawnPoint;
            float TempHeading;
            bool GuaranteedSpawnPointFound = true;
            unsafe
            {
                if (!NativeFunction.Natives.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING<bool>(StartPoint.X, StartPoint.Y, StartPoint.Z, out TempSpawnPoint, out TempHeading, 1, 0x40400000, 0))
                {
                    TempSpawnPoint = World.GetNextPositionOnStreet(StartPoint);

                    Entity closestent = World.GetClosestEntity(TempSpawnPoint, 30f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludePlayerVehicle);
                    if (closestent.Exists())
                    {
                        TempSpawnPoint = closestent.Position;
                        TempHeading = closestent.Heading;
                        closestent.Delete();
                    }
                    else
                    {
                        Vector3 directionFromSpawnToPlayer = (Game.LocalPlayer.Character.Position - TempSpawnPoint);
                        directionFromSpawnToPlayer.Normalize();

                        TempHeading = MathHelper.ConvertDirectionToHeading(directionFromSpawnToPlayer) + 180f;
                        GuaranteedSpawnPointFound = false;
                    }
                }
            }
            SpawnPoint = TempSpawnPoint;
            Heading = TempHeading;
            return GuaranteedSpawnPointFound;
        }
        


        public static TupleList<T1, T2, T3> Shuffle<T1, T2, T3>(this TupleList<T1, T2, T3> tuplelist)
        {
            TupleList<T1, T2, T3> ShuffledList = new TupleList<T1, T2, T3>(tuplelist);
            int n = ShuffledList.Count;
            while (n > 1)
            {
                n--;
                int k = AssortedCalloutsHandler.rnd.Next(n + 1);
                Tuple<T1, T2, T3> value = ShuffledList[k];
                ShuffledList[k] = ShuffledList[n];
                ShuffledList[n] = value;
            }
            return ShuffledList;
        }
        public static TupleList<T1, T2> Shuffle<T1, T2>(this TupleList<T1, T2> tuplelist)
        {
            TupleList<T1, T2> ShuffledList = new TupleList<T1, T2>(tuplelist);
            int n = ShuffledList.Count;
            while (n > 1)
            {
                n--;
                int k = AssortedCalloutsHandler.rnd.Next(n + 1);
                Tuple<T1, T2> value = ShuffledList[k];
                ShuffledList[k] = ShuffledList[n];
                ShuffledList[n] = value;
            }
            return ShuffledList;
        }
        public static void RegisterHatedTargetsAroundPed(this Ped ped, float radius)
        {
            Rage.Native.NativeFunction.Natives.REGISTER_HATED_TARGETS_AROUND_PED(ped, radius);
        }

        
        public static Vector3 Around(this Vector3 start, float MinDistance, float MaxDistance)
        {
            return start.Around(GetRandomFloat(MinDistance, MaxDistance));
        }
        public static float GetRandomFloat(float minimum, float maximum)
        {

            return (float)AssortedCalloutsHandler.rnd.NextDouble() * (maximum - minimum) + minimum;
        }

        public static float DistanceTo(this Vector3 start, Vector3 end)
        {
            return (end - start).Length();
        }

        public static Vector3 RandomXY()
        {


            Vector3 vector3 = new Vector3();
            vector3.X = (float)(AssortedCalloutsHandler.rnd.NextDouble() - 0.5);
            vector3.Y = (float)(AssortedCalloutsHandler.rnd.NextDouble() - 0.5);
            vector3.Z = 0.0f;
            vector3.Normalize();
            return vector3;
        }
    }
}

