using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;
using AssortedCallouts.Extensions;
using System.Drawing;
using System.Collections.Specialized;
using Rage.Native;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts
{
    //Never completed.
    [CalloutInfo("Bank Heist Pursuit", CalloutProbability.Medium)]
    class BankHeistPursuit : AssortedCallout
    {

        private string[] firearmsToSelectFrom = new string[] { "WEAPON_PISTOL", "WEAPON_APPISTOL",  "WEAPON_MICROSMG", "WEAPON_SMG",  "WEAPON_ASSAULTRIFLE"
                                                                , "WEAPON_ADVANCEDRIFLE", "WEAPON_PISTOL50", "WEAPON_ASSAULTSMG" };
        private bool CalloutRunning = false;


        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(220f));
            while (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) < 180f)
            {
                GameFiber.Yield();
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(220f));
            }
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 10f);
            CalloutMessage = "Bank Heist Pursuit";

            CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("ALL_UNITS WE_HAVE CRIME_BANKHEIST IN_OR_ON_POSITION CRIME_SUSPECTS_FLEEING_CRIME ALL_AVAILABLE_UNITS_CODE3", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            SuspectCar = new Vehicle("STOCKADE", SpawnPoint);
            SuspectCar.RandomiseLicencePlate();
            SuspectCar.MakePersistent();
            new Ped(Vector3.Zero);
            Suspect.MakeMissionPed();
            SuspectBlip = Suspect.AttachBlip();
            SuspectBlip.Color = Color.Red;

            Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
            Suspect.WarpIntoVehicle(SuspectCar, -1);
            if (!CalloutRunning)
            {
                CalloutHandler();
            }
            return base.OnCalloutAccepted();
        }

        private void CalloutHandler()
        {

        }
    }
}
