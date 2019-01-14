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
    [CalloutInfo("Organised Street Race", CalloutProbability.Medium)]
    internal class OrganisedStreetRace : AssortedCallout
    {
        private bool CalloutRunning = false;
        private List<string> RaceModels = new List<string>() { "NEMESIS", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "BLISTA2", "BUFFALO", "HEXER", "ZENTORNO", "MASSACRO" };
        private int NumberOfVehicles;
        private List<Vehicle> Vehicles = new List<Vehicle>();
        private List<Model> VehicleModels = new List<Model>();
        //private Vector3 Destination;
        private List<Ped> Suspects = new List<Ped>();
        
        private string msg;
        private int ArrestCount;
        private int DeadCount;
        private int EscapeCount;
        private List<Blip> SuspectBlips = new List<Blip>();
        private bool IsRouteEnabled = true;
        private int audiocount = 0;
        

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("AssortedCallouts.OrganisedStreetRace");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f,400f)).GetClosestVehicleNodeWithHeading(out SpawnPoint, out SpawnHeading))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }

            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 70f);
            NumberOfVehicles = AssortedCalloutsHandler.rnd.Next(2, 5);
            RaceModels = RaceModels.Shuffle();
            for (int i = 0; i < NumberOfVehicles; i++)
            {
                Model modeltoadd = new Model(RaceModels[AssortedCalloutsHandler.rnd.Next(RaceModels.Count)]);
                VehicleModels.Add(modeltoadd);
                modeltoadd.LoadAndWait();
            }
            
            CalloutMessage = "Organised Street Race";
            CalloutPosition = SpawnPoint;
            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Organised Street Race", "Street Race", SpawnPoint, 1, "Reports of an organised street race taking place. Please investigate.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE_01 CRIME_VEHICLES_RACING IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();

        }

        public override bool OnCalloutAccepted()
        {
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
                    //Destination = World.GetNextPositionOnStreet(SpawnPoint.Around(300f));
                    SpawnAllEntities();
                    SearchArea = new Blip(Suspects[0].Position, 130f);
                    SearchArea.Color = Color.Yellow;
                    SearchArea.IsRouteEnabled = true;
                    IsRouteEnabled = true;
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            if (Suspects.Contains(Functions.GetPulloverSuspect(Functions.GetCurrentPullover())))
                            {
                                break;
                            }
                        }

                        foreach (Ped suspect in Suspects)
                        {
                            GameFiber.Yield();
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(suspect.Position, Game.LocalPlayer.Character.Position) < 15f)
                                {
                                    break;
                                }
                            }
                           
                            //if (Vector3.Distance(suspect.Position, Destination) < 50f)
                            //{
                            //    Destination = World.GetNextPositionOnStreet(Destination.Around(300f));

                            //    Suspects[0].Tasks.DriveToPosition(Destination, 60f, VehicleDrivingFlags.Emergency).WaitForCompletion(200);
                                
                            //}

                        }
                        if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            Ped nearestsuspect = (from x in Suspects where x.DistanceTo(Game.LocalPlayer.Character.Position) < 30f select x).FirstOrDefault();
                            if (nearestsuspect != null)
                            {

                                if (Game.LocalPlayer.Character.CurrentVehicle.IsPoliceVehicle)
                                {
                                    if (Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn)
                                    {
                                        
                                        break;
                                    }
                                }
                            }
                        }

                        if (Vector3.Distance(Suspects[0].Position, SearchArea.Position) > 180f)
                        {
                            SearchArea.Delete();
                            SearchArea = new Blip(Suspects[0].Position, 110f);
                            SearchArea.Color = Color.Yellow;
                            SearchArea.IsRouteEnabled = IsRouteEnabled;
                            audiocount++;
                            if (audiocount >= 3)
                            {
                                Functions.PlayScannerAudioUsingPosition("SUSPECTS_LAST_REPORTED IN_OR_ON_POSITION", SearchArea.Position);
                                audiocount = 0;
                            }

                        }

                        if (IsRouteEnabled != Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) > 180f)
                        {
                            IsRouteEnabled = Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) > 180f;
                            SearchArea.IsRouteEnabled = Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) > 180f;
                        }


                    }

                    if (CalloutRunning)
                    {
                        if (ComputerPlusRunning)
                        {
                            API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicles located. In pursuit.");
                        }
                        GameFiber.Wait(1500);
                        Pursuit = Functions.CreatePursuit();

                        foreach (Ped suspect in Suspects.ToArray())
                        {
                            if (Vector3.Distance(suspect.Position, Game.LocalPlayer.Character.Position) < 150f)
                            {
                                Functions.AddPedToPursuit(Pursuit, suspect);
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.AddVehicleToCallout(CalloutID, suspect.CurrentVehicle);
                                }
                            }
                            else
                            {
                                Suspects.Remove(suspect);
                                Vehicles.Remove(suspect.CurrentVehicle);
                                suspect.Dismiss();
                                suspect.CurrentVehicle.Dismiss();
                            }
                        }
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        Functions.ForceEndCurrentPullover();
                        if (SearchArea.Exists()) { SearchArea.Delete(); }
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                    }

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
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
                        if (!Functions.IsPursuitStillRunning(Pursuit))
                        {

                            break;
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
                        Game.DisplayNotification("~O~OrganisedStreetRace~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }
        private void DisplayCodeFourMessage()
        {
            if (CalloutRunning)
            {
                foreach (Ped suspect in Suspects)
                {
                    EscapeCount++;
                }
                msg = "Control,";
                if (ArrestCount > 0) { msg += " ~g~" + ArrestCount.ToString() + " suspects in custody."; }
                if (DeadCount > 0) { msg += " ~o~" + DeadCount.ToString() + " suspects dead."; }
                if (EscapeCount > 0) { msg += " ~r~" + EscapeCount.ToString() + " suspects escaped."; }
                msg += "~b~Street Race CODE 4, over.";
                GameFiber.Sleep(4000);
                Game.DisplayNotification(msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }


        private void SpawnAllEntities()
        {
            foreach (Model model in VehicleModels)
            {
                Vector3 vehspawn;
                float vehheading;
                if (Vehicles.Count == 0) { vehspawn = SpawnPoint; vehheading = SpawnHeading; }
                else
                {
                    vehspawn = Vehicles[Vehicles.Count - 1].GetOffsetPosition(Vector3.RelativeBack * 6f);
                    vehheading = Vehicles[Vehicles.Count - 1].Heading;
                }
                Vehicle veh = new Vehicle(model, vehspawn, vehheading);
                veh.RandomiseLicencePlate();
                veh.MakePersistent();
                int randomNumber = AssortedCalloutsHandler.rnd.Next(4);

                if (randomNumber == 0)
                {
                    veh.Mods.InstallModKit();
                    veh.Mods.ApplyAllMods();
                }
                else
                {
                    veh.Mods.InstallModKit();

                    veh.Mods.EngineModIndex = veh.Mods.EngineModCount - 1;

                    veh.Mods.ExhaustModIndex = veh.Mods.ExhaustModCount - 1;

                    veh.Mods.TransmissionModIndex = veh.Mods.TransmissionModCount - 1;

                    VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                    int wheelModIndex = MathHelper.GetRandomInteger(veh.Mods.GetWheelModCount(wheelType));
                    veh.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                    veh.Mods.HasTurbo = true;

                    veh.Mods.HasXenonHeadlights = true;
                }
                Ped driver = veh.CreateRandomDriver();
                driver.MakeMissionPed();

                if (Suspects.Count == 0)
                {
                    driver.Tasks.CruiseWithVehicle(veh, 80f, VehicleDrivingFlags.Emergency);
                }
                else
                {
                    Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(driver, Suspects[0]);
                    Rage.Native.NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG( driver, 32, 1);
                    Rage.Native.NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(driver, 20f);

                }
                NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED( veh, 9f);
                GameFiber.Wait(1000);
                Vehicles.Add(veh);
                Suspects.Add(driver);
                //SuspectBlips.Add(driver.AttachBlip());
            }
        }

        public override void Process()
        {
            base.Process();
            if (Game.LocalPlayer.Character.IsDead)
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
                foreach (Entity ent in Suspects)
                {
                    if (ent.Exists()) { ent.Dismiss(); }
                }
                foreach (Entity ent in Vehicles)
                {
                    if (ent.Exists()) { ent.Dismiss(); }
                }

            }
            else
            {
                foreach (Entity ent in Suspects)
                {
                    if (ent.Exists()) { ent.Delete(); }
                }
                foreach (Entity ent in Vehicles)
                {
                    if (ent.Exists()) { ent.Delete(); }
                }
            }
        }
        
    }
}

