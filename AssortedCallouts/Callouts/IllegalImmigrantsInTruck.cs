using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;
using Albo1125.Common.CommonLibrary;
using AssortedCallouts.Extensions;
using System.Drawing;
using Rage.Native;
using LSPD_First_Response.Engine.Scripting.Entities;

namespace AssortedCallouts.Callouts
{
    [CalloutInfo("Illegal Immigrants in Truck", CalloutProbability.Medium)]
    internal class IllegalImmigrantsInTruck : AssortedCallout
    {
        private bool CalloutRunning;
        private string msg = "";
        private Model[] TruckModels = new Model[] { "MULE", "MULE3" };
        private string CarModelName;
        //private string CarColor;
        private int ArrestCount = 0;
        private int EscapeCount = 0;
        private List<Blip> SuspectBlips = new List<Blip>();
        private int DeadCount = 0;
        private List<Ped> IllegalImmigrants = new List<Ped>();
        private Model[] ImmigrantModels = new Model[] { "s_m_m_migrant_01", "s_f_y_migrant_01" };
        private WeaponAsset[] MeleeWeapons = new WeaponAsset[] { "WEAPON_UNARMED", "WEAPON_CROWBAR", "WEAPON_HAMMER", "WEAPON_KNIFE", "WEAPON_BAT" };
        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.IllegalImmigrantsInTruck");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f, 500f)).GetClosestVehicleNodeWithHeading(out SpawnPoint, out SpawnHeading))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }
            uint zoneHash = Rage.Native.NativeFunction.CallByHash<uint>(0x7ee64d51e8498728, SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);


            if (Game.GetHashKey("city") == zoneHash)
            {
                Game.LogTrivial("Aborting due to location");
                return false;
            }
            SearchAreaLocation = SpawnPoint.Around(40f, 90f);
            ShowCalloutAreaBlipBeforeAccepting(SearchAreaLocation, 280f);
            CalloutMessage = "Illegal Immigrants in Truck";
            CalloutPosition = SpawnPoint;
            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Illegal Immigrants in Truck", "Immigrants in Truck", SpawnPoint, 0, "Reports of suspicious activity coming from the back of a truck. Please investigate.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " CITIZENS_REPORT CRIME_SUSPICIOUSVEHICLE IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            
            SuspectCar = new Vehicle(TruckModels[AssortedCalloutsHandler.rnd.Next(TruckModels.Length)], SpawnPoint, SpawnHeading);
            SuspectCar.MakePersistent();
            CarModelName = SuspectCar.Model.Name.ToLower();
            CarModelName = char.ToUpper(CarModelName[0]) + CarModelName.Substring(1);
            try
            {
                //CarColor = SuspectCar.GetColors().PrimaryColorName + "~s~-coloured";
            }
            catch (Exception e)
            {
                //CarColor = "weirdly-coloured";
            }
            Suspect = SuspectCar.CreateRandomDriver();
            Suspect.MakeMissionPed();

            int NumberOfImmigrants = AssortedCalloutsHandler.rnd.Next(1, 5);
            for (int i=0;i<NumberOfImmigrants;i++)
            {
                Game.LogTrivial("Spawning immigrant");
                Ped immigrant = new Ped(ImmigrantModels[AssortedCalloutsHandler.rnd.Next(ImmigrantModels.Length)], Vector3.Zero, 0f);
                immigrant.MakeMissionPed();
                IllegalImmigrants.Add(immigrant);
                
                immigrant.WarpIntoVehicle(SuspectCar, i + 1);

                Persona immigrantpersona = Functions.GetPersonaForPed(immigrant);
                immigrantpersona= new Persona(immigrant, immigrantpersona.Gender, immigrantpersona.BirthDay, immigrantpersona.Citations, immigrantpersona.Forename, immigrantpersona.Surname, ELicenseState.None, immigrantpersona.TimesStopped, true, false, false);
                Functions.SetPersonaForPed(immigrant, immigrantpersona);
                
            }


            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Illegal Immigrants in Truck", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, "Citizens reporting ~r~illegal immigrants in a truck. ~b~Investigate the truck.");
            CalloutHandler();
            return base.OnCalloutAccepted();

        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    
                    Suspect.Tasks.CruiseWithVehicle(SuspectCar, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                    GameFiber.Wait(3000);
                    Game.DisplayNotification("Suspicious truck~s~ is a ~b~" + CarModelName + ".");
                    Game.DisplayNotification("The plate is ~b~" + SuspectCar.LicensePlate + ". ~s~Added to ~o~ANPR system.");
                    GameFiber.Wait(3000);
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Added " + CarModelName + " with licence plate " + SuspectCar.LicensePlate + " to fixed ANPR system.");
                        API.ComputerPlusFuncs.AddVehicleToCallout(CalloutID, SuspectCar);
                        
                    }
                    HandleSearchForVehicleWithANPR();
                    if (ComputerPlusRunning)
                    {

                        API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                        
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspicious truck located. Engaging traffic stop.");
                        

                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!CalloutRunning) { break; }
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            break;
                        }
                        if (Functions.IsPlayerPerformingPullover())
                        {

                            if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == Suspect)

                            {
                                break;
                            }
                        }
                    }
                    if (Functions.IsPlayerPerformingPullover() && CalloutRunning)
                    {
                        GameFiber.Wait(3000);
                    }
                    if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                    if (AssortedCalloutsHandler.rnd.Next(11) < 2 || !Game.LocalPlayer.Character.IsInAnyVehicle(false) || Functions.GetActivePursuit() != null)
                    {
                        if (CalloutRunning)
                        {
                            if (Functions.GetActivePursuit() != null) { Functions.ForceEndPursuit(Functions.GetActivePursuit()); }
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            foreach (Ped migrant in IllegalImmigrants)
                            {
                                Functions.AddPedToPursuit(Pursuit, migrant);
                            }
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            if (AssortedCalloutsHandler.English == AssortedCalloutsHandler.EnglishTypes.BritishEnglish)
                            {
                                Game.DisplayNotification("Control, the vehicle is ~r~making off.~b~ Giving chase.");
                            }
                            else
                            {
                                Game.DisplayNotification("Control, the vehicle is ~r~fleeing,~b~ In pursuit.");
                            }

                            while (Functions.IsPursuitStillRunning(Pursuit))
                            {
                                GameFiber.Yield();
                                if (!CalloutRunning) { break; }
                            }
                        }
                    }
                    else
                    {
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) || Functions.GetActivePursuit() != null)
                            {
                                GameFiber.Wait(1000);
                                if (AssortedCalloutsHandler.rnd.Next(6) < 2 || Functions.GetActivePursuit() != null)
                                {
                                    if (Functions.GetActivePursuit() != null) { Functions.ForceEndPursuit(Functions.GetActivePursuit()); }
                                    Pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(Pursuit, Suspect);
                                    foreach (Ped migrant in IllegalImmigrants)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, migrant);
                                    }
                                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                    if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }

                                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    if (AssortedCalloutsHandler.English == AssortedCalloutsHandler.EnglishTypes.BritishEnglish)
                                    {
                                        Game.DisplayNotification("Control, the vehicle is ~r~making off.~b~ Giving chase.");
                                    }
                                    else
                                    {
                                        Game.DisplayNotification("Control, the vehicle is ~r~fleeing,~b~ In pursuit.");
                                    }
                                }
                                break;

                            }
                        }

                        if (Pursuit == null)
                        {
                            while (CalloutRunning)
                            {
                                GameFiber.Yield();
                                if (Functions.GetActivePursuit() != null) { Functions.ForceEndPursuit(Functions.GetActivePursuit()); }
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SuspectCar.RearPosition) < 3f)
                                {
                                    //Game.LogTrivial("Illegalimmigrant length: " + IllegalImmigrants.Count.ToString());
                                    if (IllegalImmigrants[0].Exists())
                                    {
                                        if (!IllegalImmigrants[0].IsAnySpeechPlaying)
                                        {
                                            IllegalImmigrants[0].PlayAmbientSpeech("GENERIC_CURSE_MED");
                                        }
                                    }
                                    Game.DisplayHelp("You can open the back of the truck by pressing ~b~F.");
                                    if (Game.IsKeyDown(System.Windows.Forms.Keys.F))
                                    {
                                        Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                        Game.LocalPlayer.Character.Tasks.GoStraightToPosition(SuspectCar.RearPosition, 1.3f, SuspectCar.Heading, 1f, 3000).WaitForCompletion(2000);

                                        int wait = 0;

                                        while (CalloutRunning)
                                        {
                                            GameFiber.Yield();
                                            wait++;
                                            if (wait > 200) { break; }
                                            if (!SuspectCar.Doors[2].IsOpen)
                                            {
                                                SuspectCar.Doors[2].Open(true);
                                            }
                                            if (!SuspectCar.Doors[3].IsOpen)
                                            {
                                                SuspectCar.Doors[3].Open(true);
                                            }
                                        }
                                       
                                        foreach (Ped immigrant in IllegalImmigrants)
                                        {
                                            immigrant.PlayAmbientSpeech("GENERIC_CURSE_MED");
                                            if (immigrant.IsInAnyVehicle(false))
                                            {
                                                immigrant.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1000);
                                            }
                                        }
                                        SuspectCar.Doors[2].Open(true);
                                        SuspectCar.Doors[3].Open(true);
                                        GameFiber.Wait(3000);
                                        break;
                                    }
                                }
                                else
                                {
                                    Game.DisplaySubtitle("Move to the back of the truck to investigate it.", 50);
                                }
                            }

                            if (CalloutRunning)
                            {

                                int roll = AssortedCalloutsHandler.rnd.Next(8);
                                if (roll < 3)
                                {

                                    foreach (Ped immigrant in IllegalImmigrants)
                                    {
                                        immigrant.Inventory.GiveNewWeapon(MeleeWeapons[AssortedCalloutsHandler.rnd.Next(MeleeWeapons.Length)], -1, true);
                                        immigrant.RelationshipGroup = "ROBBERS";
                                        Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                                        GameFiber.Yield();
                                        immigrant.Tasks.FightAgainstClosestHatedTarget(60f);
                                    }
                                    if (Suspect.IsInAnyVehicle(false))
                                    {
                                        Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                                    }
                                    Suspect.RelationshipGroup = "ROBBERS";
                                    Suspect.Tasks.FightAgainstClosestHatedTarget(60f);
                                }
                                else if (roll < 6)
                                {

                                    Pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(Pursuit, Suspect);
                                    foreach (Ped migrant in IllegalImmigrants)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, migrant);
                                    }
                                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                    if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    if (AssortedCalloutsHandler.English == AssortedCalloutsHandler.EnglishTypes.BritishEnglish)
                                    {
                                        Game.DisplayNotification("Control, the suspects are ~r~making off. ~b~Giving chase.");
                                    }
                                    else
                                    {
                                        Game.DisplayNotification("Control, the suspects are ~r~fleeing,~b~ In pursuit.");
                                    }

                                }
                                else if (roll < 8)
                                {
                                    foreach (Ped migrant in IllegalImmigrants)
                                    {
                                        migrant.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                                    }
                                    if (Suspect.IsInAnyVehicle(false))
                                    {
                                        Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                                    }
                                    Suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                                }
                                List<Ped> Suspects = IllegalImmigrants;
                                Suspects.Add(Suspect);

                                while (CalloutRunning)
                                {
                                    GameFiber.Yield();
                                    if (Pursuit == null)
                                    {
                                        if (Suspects.Count == 0) { break; }
                                        foreach (Ped suspect in Suspects.ToArray())
                                        {
                                            if (!suspect.Exists())
                                            {
                                                EscapeCount++;
                                                Suspects.Remove(suspect);
                                            }
                                            else if (Functions.IsPedArrested(suspect))
                                            {
                                                ArrestCount++;
                                                Suspects.Remove(suspect);
                                            }
                                            else if (suspect.IsDead)
                                            {
                                                DeadCount++;
                                                Suspects.Remove(suspect);
                                            }
                                            else if (Vector3.Distance(suspect.Position, Game.LocalPlayer.Character.Position) > 1000f)
                                            {
                                                EscapeCount++;
                                                Suspects.Remove(suspect);
                                                if (suspect.CurrentVehicle.Exists()) { suspect.CurrentVehicle.Delete(); }
                                                suspect.Delete();
                                                Game.DisplayNotification("A suspect has escaped.");
                                            }

                                        }
                                    }
                                    else
                                    {
                                        if (!Functions.IsPursuitStillRunning(Pursuit)) { break; }
                                    }
                                }

                                while (CalloutRunning)
                                {
                                    GameFiber.Yield();
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~to end the callout.");
                                    if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                                    {
                                        Game.HideHelp();
                                        
                                        //foreach (Ped suspect in Suspects)
                                        //{
                                        //    EscapeCount++;
                                        //}
                                        msg = "Control,";
                                        if (ArrestCount > 0) { msg += " ~g~" + ArrestCount.ToString() + " suspects in custody."; }
                                        if (DeadCount > 0) { msg += " ~o~" + DeadCount.ToString() + " suspects dead."; }
                                        if (EscapeCount > 0) { msg += " ~r~" + EscapeCount.ToString() + " suspects escaped."; }
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            while (CalloutRunning)
                            {
                                GameFiber.Yield();
                                if (!Functions.IsPursuitStillRunning(Pursuit))
                                {
                                    break;
                                }
                            }
                        }
                        
                    }
                    DisplayCodeFourMessage();
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }
                catch (Exception e)
                {

                    if (CalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Assorted Callouts handled the exception successfully.");
                        Game.DisplayNotification("~O~IllegalImmigrantsInTruck~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        public override void Process()
        {
            base.Process();
            if (Game.LocalPlayer.Character.Exists())
            {
                if (Game.LocalPlayer.Character.IsDead)
                {

                    GameFiber.StartNew(End);
                }
            }
            else
            {
                GameFiber.StartNew(End);
            }
        }

        public override void End()
        {
            CalloutRunning = false;
            //Rage.Native.NativeFunction.Natives.RESET_AI_MELEE_WEAPON_DAMAGE_MODIFIER()
            if (Game.LocalPlayer.Character.IsDead)
            {
                GameFiber.Wait(1500);
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                GameFiber.Wait(3000);


            }
            base.End();
            if (SearchArea.Exists()) { SearchArea.Delete(); }
            foreach (Blip bl in SuspectBlips)
            {
                if (bl.Exists()) { bl.Delete(); }
            }
            if (CalloutFinished)
            {
                foreach (Entity ent in IllegalImmigrants)
                {
                    if (ent.Exists())
                    {
                        ent.Dismiss();
                    }
                }
            }
            else
            {
                foreach (Entity ent in IllegalImmigrants)
                {
                    if (ent.Exists())
                    {
                        ent.Delete();
                    }
                }
            }
        }


        private void DisplayCodeFourMessage()
        {
            if (CalloutRunning)
            {
                
                msg += "~b~ Immigrants in truck CODE 4.";
                GameFiber.Sleep(4000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Illegal Immigrants in Truck", "~b~" + AssortedCalloutsHandler.DivisionUnitBeat + "~s~ to Dispatch", msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }

        private void HandleSearchForVehicleWithANPR()
        {
            float Radius = 250f;
            SearchArea = new Blip(SearchAreaLocation, Radius);
            SearchArea.Color = System.Drawing.Color.Yellow;
            SearchArea.Alpha = 0.5f;
            int WaitCount = 0;
            int WaitCountTarget = 2300;
            bool RouteEnabled = false;
            while (CalloutRunning)
            {
                GameFiber.Yield();
                WaitCount++;
                Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);

                if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 11f), SuspectCar.Position) < 11f)
                {
                    GameFiber.Sleep(2000);
                    if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 11f), SuspectCar.Position) < 11f)
                    {
                        Game.DisplayNotification("Control, I have located the ~b~" + CarModelName + ".");
                        Game.DisplayNotification("I'm preparing to ~b~stop them,~s~ over.");
                        SuspectBlip = Suspect.AttachBlip();
                        if (SearchArea.Exists()) { SearchArea.Delete(); }
                        Functions.PlayScannerAudio("DISPATCH_SUSPECT_LOCATED_ENGAGE");

                        break;
                    }

                }
                else if (((Vector3.Distance(SuspectCar.Position, SearchArea.Position) > Radius + 20f) && (WaitCount > 500)) || (WaitCount > WaitCountTarget))
                {
                    Game.DisplayNotification("~o~ANPR Hit ~s~on the ~b~" + CarModelName + ", ~s~plate ~b~" + SuspectCar.LicensePlate + ".");
                    Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION", SuspectCar.Position);
                    SearchArea.Delete();
                    Radius -= 5f;
                    if (Radius < 140f) { Radius = 140f; }
                    SearchArea = new Blip(SuspectCar.Position.Around(5f, 25f), Radius);
                    SearchArea.Color = System.Drawing.Color.Yellow;
                    SearchArea.Alpha = 0.5f;


                    RouteEnabled = false;
                    if (WaitCount > WaitCountTarget) { Game.LogTrivial("Updated for waitcount"); }
                    WaitCount = 0;

                    Suspect.Tasks.CruiseWithVehicle(SuspectCar, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                    WaitCountTarget -= AssortedCalloutsHandler.rnd.Next(200, 500);
                    if (WaitCountTarget < 1400) { WaitCountTarget = 1400; }
                    SuspectBlip = new Blip(Suspect.Position);
                    SuspectBlip.Color = Color.Red;
                    GameFiber.Wait(4000);
                    SuspectBlip.Delete();

                }
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) > Radius + 90f)
                {
                    if (!RouteEnabled)
                    {
                        SearchArea.IsRouteEnabled = true;
                        RouteEnabled = true;
                    }
                }
                else
                {
                    if (RouteEnabled)
                    {
                        SearchArea.IsRouteEnabled = false;
                        RouteEnabled = false;
                    }
                }
            }
        }
    }
}
