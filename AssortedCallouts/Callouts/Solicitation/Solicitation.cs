using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;
using AssortedCallouts.Extensions;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Windows.Forms;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts.Solicitation
{
    [CalloutInfo("Solicitation", CalloutProbability.Medium)]
    internal class Solicitation : AssortedCallout
    {
        private Ped Hooker;
        private bool CalloutRunning = false;
        public static SolicitationTrafficStop ActiveSolicitationTrafficStop = null;
        private Model[] HookerModels = new Model[] { "S_F_Y_HOOKER_01", "S_F_Y_HOOKER_02", "S_F_Y_HOOKER_03" };
        private static string[] hooker_idle_a_anims = new string[] { "idle_a", "idle_b", "idle_c", "idle_d" };
        private Model HookerModel;
       
        
        
        private bool Hooking = false;
        
        private Vector3 Destination;
        private Rage.Task DriveToHookerTask;
        private string msg = "";
        private List<Vehicle> HookedVehicles = new List<Vehicle>();
        private bool HookerScaredOff = false;
       
        private bool AllowHookAttempts = true;
        private bool SuspectsLost = false;
        private bool HookerLookingTowardsPlayer = false;
        private int Points = 0;
        private List<Vehicle> VehiclesConsidered = new List<Vehicle>();
        
        


        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.Solicitation");
            if (((World.DateTime.Hour == 20 && World.DateTime.Minute >= 30) || (World.DateTime.Hour > 20) || (World.DateTime.Hour == 6 && World.DateTime.Minute <= 30) || (World.DateTime.Hour < 6)) || !AssortedCalloutsHandler.SolicitationNightOnly)
            {
                foreach (Tuple<Vector3, float> tuple in SolicitationSpawnPointsWithHeadings.ToArray())
                {
                    if (Game.LocalPlayer.Character.DistanceTo(tuple.Item1) > 620f || Game.LocalPlayer.Character.DistanceTo(tuple.Item1) < 220f)
                    {
                        SolicitationSpawnPointsWithHeadings.Remove(tuple);
                    }
                }
                if (SolicitationSpawnPointsWithHeadings.Count == 0) { return false; }
                SolicitationSpawnPointsWithHeadings = SolicitationSpawnPointsWithHeadings.Shuffle();
                SpawnPoint = SolicitationSpawnPointsWithHeadings[0].Item1;
                SpawnHeading = SolicitationSpawnPointsWithHeadings[0].Item2;
                //SpawnPoint = new Vector3(0.7630921f, 7.076443f,  70.82912f);
                //SpawnHeading = 354.0538f;
                HookerModel = HookerModels[AssortedCalloutsHandler.rnd.Next(HookerModels.Length)];
                HookerModel.LoadAndWait();
                SearchAreaLocation = SpawnPoint.Around(35f, 65f);
                ShowCalloutAreaBlipBeforeAccepting(SearchAreaLocation, 90f);
                CalloutMessage = "Solicitation";
                CalloutPosition = SpawnPoint;
                Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " CITIZENS_REPORT CRIME_SOLICITATION IN_OR_ON_POSITION", SpawnPoint);


                return base.OnBeforeCalloutDisplayed();
            }
            else
            {
                Game.LogTrivial("Solicitation - no nighttime.");
                return false;
            }
        }
        private void PerformHookAttempt(Ped _Hooker, Ped _Suspect, bool Successful)
        {
            Hooking = true;
            GameFiber.StartNew(delegate
            {
                try {
                    if (_Suspect.Exists())
                    {
                        
                        _Suspect.MakeMissionPed();
                        Vehicle suspectveh = _Suspect.CurrentVehicle;

                        suspectveh.MakePersistent();
                        HookedVehicles.Add(suspectveh);
                        if (_Suspect.CurrentVehicle.Speed > 9f)
                        {
                            NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED(_Suspect.CurrentVehicle, 9f);
                        }
                        _Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight).WaitForCompletion(900);
                        _Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveLeft).WaitForCompletion(600);
                        _Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                        GameFiber.Wait(700);
                        _Hooker.Tasks.FollowNavigationMeshToPosition(_Suspect.CurrentVehicle.GetOffsetPosition(Vector3.RelativeRight * 2f), _Suspect.CurrentVehicle.Heading + 90f, 1.35f).WaitForCompletion(14000);
                        if (Successful)
                        {
                            Vehicle veh = _Suspect.CurrentVehicle;
                            _Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 0.8f, AnimationFlags.None).WaitForCompletion(10000);
                            _Hooker.Tasks.EnterVehicle(veh, 7000, 0).WaitForCompletion();
                            _Hooker.WarpIntoVehicle(veh, 0);
                            _Suspect.WarpIntoVehicle(veh, -1);
                            _Suspect.Tasks.CruiseWithVehicle(18f);
                        }
                        else
                        {
                            _Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 0.8f, AnimationFlags.None).WaitForCompletion(AssortedCalloutsHandler.rnd.Next(3000, 5000));
                            if (!Suspect.Exists()) { Game.LogTrivial("Suspect didn't exist"); _Suspect = suspectveh.CreateRandomDriver();_Suspect.MakeMissionPed(); }
                            
                            _Suspect.Dismiss();
                            suspectveh.Dismiss();
                            

                            GameFiber.Wait(2000);
                            if (CalloutRunning)
                            {
                                _Hooker.Tasks.FollowNavigationMeshToPosition(SpawnPoint, SpawnHeading, 1.35f).WaitForCompletion(14000);
                                if (CalloutRunning)
                                {
                                    _Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 1f, AnimationFlags.Loop);
                                }
                                else
                                {
                                    if (_Hooker.Exists())
                                    {
                                        _Hooker.Dismiss();
                                    }
                                }
                            }
                            else
                            {
                                if (_Hooker.Exists())
                                {
                                    _Hooker.Dismiss();
                                }
                            }
                        }
                    }
                    
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    if(_Suspect.Exists()) { _Suspect.Dismiss(); }
                    if (_Suspect.CurrentVehicle.Exists()) { _Suspect.CurrentVehicle.Dismiss(); }
                    End();

                }
                catch (Exception e)
                {

                    if (CalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Assorted Callouts handled the exception successfully.");
                        Game.DisplayNotification("~O~Solicitation~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                    if (_Suspect.Exists()) { _Suspect.Dismiss(); }
                    if (_Suspect.CurrentVehicle.Exists()) { _Suspect.CurrentVehicle.Dismiss(); }
                }
                finally { Hooking = false; }
            });
            
        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    
                    Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 1f, AnimationFlags.Loop);
                    SearchArea.IsRouteEnabled = true;
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) < 125f)
                        {
                            SearchArea.IsRouteEnabled = false;
                            Game.DisplayHelp("~b~Search for the potential hooker while remaining unnoticed.");
                            break;
                        }
                    }
                    if (CalloutRunning)
                    {
                        Vector3 CarSpawn;
                        float CarHeading;
                        World.GetNextPositionOnStreet(Hooker.GetOffsetPosition(Vector3.RelativeLeft * AssortedCalloutsHandler.rnd.Next(260, 410)).Around(70)).GetClosestVehicleNodeWithHeading(out CarSpawn, out CarHeading);
                        SuspectCar = new Vehicle(CarsToSelectFrom[AssortedCalloutsHandler.rnd.Next(CarsToSelectFrom.Length)], CarSpawn, CarHeading);
                        SuspectCar.IsPersistent = true;
                        Suspect = SuspectCar.CreateRandomDriver();
                        while (Suspect.IsFemale)
                        {
                            GameFiber.Yield();
                            if (Suspect.Exists()) { Suspect.Delete(); }
                            Suspect = SuspectCar.CreateRandomDriver();
                            //Game.LogTrivial("Ped female");

                        }
                        
                    }
                    int WaitCount = 0;
                    //Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[EntryPoint.rnd.Next(hooker_idle_a_anims.Length)], 1f, AnimationFlags.Loop);
                    while (CalloutRunning)
                    {
                        GameFiber.Wait(50);
                        
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 22f)
                        {

                            break;
                        }
                        //if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 45f)
                        //{
                        //    //Game.LogTrivial(Game.LocalPlayer.Character.Position.TravelDistanceTo(Hooker.Position).ToString());
                        //    if (Game.LocalPlayer.Character.Position.TravelDistanceTo(Hooker.Position) < 95f)
                        //    {
                        //        Vector3 directionFromPlayerToHooker = (Hooker.Position - Game.LocalPlayer.Character.Position);
                        //        directionFromPlayerToHooker.Normalize();

                        //        float HeadingToHooker = MathHelper.ConvertDirectionToHeading(directionFromPlayerToHooker);
                        //        //Game.LogTrivial("Heading diff: " + (Math.Abs(MathHelper.NormalizeHeading(Game.LocalPlayer.Character.Heading) - MathHelper.NormalizeHeading(HeadingToHooker))).ToString());
                        //        if (Math.Abs(MathHelper.NormalizeHeading(Game.LocalPlayer.Character.Heading) - MathHelper.NormalizeHeading(HeadingToHooker)) < 85f)
                        //        {


                        //            WaitCount++;
                        //        }
                        //        else
                        //        {
                        //            WaitCount = 0;
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    WaitCount = 0;
                        //}

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 48f)
                        {
                            if (NativeFunction.Natives.HAS_ENTITY_CLEAR_LOS_TO_ENTITY_IN_FRONT<bool>( Game.LocalPlayer.Character, Hooker))
                            {
                                WaitCount++;
                                
                            }
                        }
                        else
                        {
                            WaitCount = 0;
                        }

                        if (WaitCount > 25)
                        {

                            break;
                        }
                    }

                    if (CalloutRunning)
                    {
                        
                        
                        if (SearchArea.Exists()) { SearchArea.Delete(); }
                        SearchArea = new Blip(Hooker.Position, 35f);
                        SearchArea.Color = System.Drawing.Color.OrangeRed;
                        SearchArea.Alpha = 0.4f;
                        Destination = Hooker.GetOffsetPosition(Vector3.RelativeFront * 5.5f);
                        
                        DriveToHookerTask = Suspect.Tasks.DriveToPosition(Destination, 16f, VehicleDrivingFlags.Normal);
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);

                        AwarenessBarDisplayTime = true;
                        AwarenessBar.Percentage = 0;
                        Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 1f, AnimationFlags.Loop);
                        if (AssortedCalloutsHandler.rnd.Next(7) == 0)
                        {
                            AllowHookAttempts = false;
                            Game.LogTrivial("Allow hook attempts false");
                        }
                        GameFiber.StartNew(delegate
                        {
                            Game.DisplayHelp("Discreetly observe the ~r~potential hooker. ~s~Try to witness her entering a vehicle.", 7000);
                            GameFiber.Wait(5000);
                            Game.DisplayHelp("If the ~r~potential hooker ~s~enters a vehicle, perform a ~b~traffic stop ~s~on it.", 7000);
                        });
                    }
                    float percentagemodifier = 0;
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        //float speed = Destination.DistanceTo(Suspect.Position);
                        //if (speed > 18f) { speed = 18f; }
                        //if (speed < 9f) { speed = 9f; }
                        //Suspect.Tasks.DriveToPosition(Hooker.Position, 10f, VehicleDrivingFlags.Normal).WaitForCompletion(300);
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 18f || AwarenessBar.Percentage == 1)
                        {
                            ScareHookerOff();
                            return;
                        }
                        
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 21f)
                        {
                            //Game.DisplaySubtitle("~h~~r~Do not let the hooker notice you. ~b~Back off!", 200);
                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) < 38f)
                        {
                            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Game.LocalPlayer.Character.CurrentVehicle.HasSiren)
                                {
                                    if (Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn)
                                    {
                                        ScareHookerOff();
                                        return;
                                    }
                                }
                            }
                        }
                        //Awareness bar percentage calculations
                        float percentage = 1 - ((Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) - 18) / 20);
                        if (percentage > 0.3)
                        {
                            
                            percentagemodifier += 0.003f;
                            if (HookerLookingTowardsPlayer)
                            {
                                percentagemodifier += 0.0015f;
                            }
                            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (AssortedCalloutsHandler.UnmarkedPoliceVehicleModels.Contains(Game.LocalPlayer.Character.CurrentVehicle.Model))
                                {
                                    percentagemodifier -= 0.0015f;
                                }
                            }
                        }
                        else
                        {
                            percentagemodifier -= 0.005f;
                            if (percentagemodifier < 0) { percentagemodifier = 0; }
                        }
                       
                        percentage += percentagemodifier;
                        if (percentage > 1) { percentage = 1; }
                        if (percentage < 0) { percentage = 0; }
                        AwarenessBar.Percentage = percentage;

                        //Stuff to do when percentage is high - look at player
                        if (AwarenessBar.Percentage > 0.7f && !Hooking && !HookerLookingTowardsPlayer)
                        {
                            Vector3 directionFromHookerToPlayer = (Game.LocalPlayer.Character.Position - Hooker.Position);
                            directionFromHookerToPlayer.Normalize();
                            Hooker.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromHookerToPlayer)).WaitForCompletion(1100);
                            HookerLookingTowardsPlayer = true;
                            
                        }
                        else if (HookerLookingTowardsPlayer && AwarenessBar.Percentage < 0.3f && !Hooking)
                        {
                            Hooker.Tasks.AchieveHeading(SpawnHeading).WaitForCompletion(1100);
                            HookerLookingTowardsPlayer = false;
                            Hooker.Tasks.PlayAnimation("amb@world_human_prostitute@hooker@idle_a", hooker_idle_a_anims[AssortedCalloutsHandler.rnd.Next(hooker_idle_a_anims.Length)], 1f, AnimationFlags.Loop);

                        }

                        

                        //If not looking at player & allowing hooks & not currently hooking, check if there's a vehicle to hook.
                        if (!Hooking && AllowHookAttempts && !HookerLookingTowardsPlayer)
                        {
                            Entity[] nearbyvehs = World.GetEntities(Destination, 4.5f, GetEntitiesFlags.ConsiderCars | GetEntitiesFlags.ExcludeEmergencyVehicles | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludePlayerVehicle);
                            foreach (Entity ent in nearbyvehs)
                            {
                                if (!ent.Exists()) { continue; }
                                Vehicle veh = (Vehicle)ent;
                                if (veh != SuspectCar && !HookedVehicles.Contains(veh))
                                {
                                    
                                    if (veh.HasDriver)
                                    {
                                        
                                        
                                        if (veh.Driver.IsMale || (veh.Driver.IsFemale && AssortedCalloutsHandler.rnd.Next(3) == 0 && !VehiclesConsidered.Contains(veh)))
                                        {
                                            
                                            if (veh.FreePassengerSeatsCount > 0 && veh.FreePassengerSeatsCount < 4)
                                            {
                                                
                                                if (veh.GetOffsetPosition(Vector3.RelativeRight * 2f).DistanceTo(Hooker) < veh.GetOffsetPosition(Vector3.RelativeLeft * 2f).DistanceTo(Hooker))
                                                {
                                                    
                                                    if (Vector3.Distance(Destination, SuspectCar.Position) > 50f)
                                                    {
                                                        
                                                        veh.Driver.IsPersistent = true;
                                                        veh.IsPersistent = true;
                                                        PerformHookAttempt(Hooker, veh.Driver, false);
                                                        break;

                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            VehiclesConsidered.Add(veh);
                                        }
                                    }

                                }
                            }
                        }
                        //If the suspect is near
                        if (Destination.DistanceTo(Suspect.Position) < 15f && DriveToHookerTask.IsActive)
                        {
                            NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Suspect, 10f);
                        }
                        if ((Destination.DistanceTo(Suspect.Position) < 5.5f || !DriveToHookerTask.IsActive) && !HookerLookingTowardsPlayer)
                        {
                            
                            if (Suspect.CurrentVehicle.Speed > 9f)
                            {
                                NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED( Suspect.CurrentVehicle, 9f);
                            }
                            
                            Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                            if (!Hooking)
                            {
                                PerformHookAttempt(Hooker, Suspect, true);

                                Suspect.Model.LoadAndWait();
                                Hooker.Model.LoadAndWait();
                                break;
                            }
                        }
                    }

                    //Waiting for player to pull them over...
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == Suspect)
                            {
                                if (SearchArea.Exists())
                                {
                                    SearchArea.Delete();
                                }
                                AwarenessBarDisplayTime = false;
                                if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                {
                                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, SuspectCar.Position) < 5f)
                                    {
                                        //10
                                        if (AssortedCalloutsHandler.rnd.Next(10) == 0)
                                        {
                                            Game.LogTrivial("Creating pursuit before traffic stop");
                                            Pursuit = Functions.CreatePursuit();
                                            Functions.AddPedToPursuit(Pursuit, Suspect);
                                            Functions.AddPedToPursuit(Pursuit, Hooker);
                                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                        }
                                        else
                                        {
                                            Suspect = Suspect.ClonePed();
                                            Hooker = Hooker.ClonePed();


                                            ActiveSolicitationTrafficStop = new SolicitationTrafficStop(Suspect, Hooker, SuspectCar, HookedVehicles.Count > 1);
                                        }
                                        break;
                                    }
                                }

                            }
                        }
                        if (Functions.GetActivePursuit() != null)
                        {
                            Pursuit = Functions.GetActivePursuit();
                            Functions.AddPedToPursuit(Pursuit, Hooker);
                            break;
                        }
                        if (SearchArea.Exists())
                        {
                            float percentage = 1 - ((Vector3.Distance(Game.LocalPlayer.Character.Position, Hooker.Position) - 16) / 16);
                            if (percentage > 1) { percentage = 1; }
                            if (percentage < 0) { percentage = 0; }
                            AwarenessBar.Percentage = percentage;
                            if (Vector3.Distance(SearchArea.Position, Hooker.Position) > 80f)
                            {
                                SearchArea.Delete();
                                AwarenessBarDisplayTime = false;
                            }
                              
                        }
                        else
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) > 180f)
                            {
                                Game.DisplayHelp("If you lost the suspects, press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~to end the call.");
                                if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                                {
                                    Game.HideHelp();
                                    SuspectsLost = true;
                                    DisplayCodeFourMessage();
                                    break;

                                }
                            }
                        }
                    }


                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (ActiveSolicitationTrafficStop == null)
                        {
                            if (!Functions.IsPursuitStillRunning(Pursuit))
                            {
                                Game.DisplayHelp("When you're done, press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~to end the call.");
                                if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                                {
                                    Game.HideHelp();
                                    break;

                                }
                                
                            }
                        }
                        else
                        {
                            if (ActiveSolicitationTrafficStop.ReadyForCleanup)
                            {
                                break;
                            }
                        }

                    }
                    if (CalloutRunning)
                    {
                        DisplayCodeFourMessage();
                    }

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
                        Game.DisplayNotification("~O~Solicitation~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        private void ScareHookerOff()
        {
            HookerScaredOff = true;
            if (Hooker.IsInAnyVehicle(true))
            {
                Hooker.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
            }
            Hooker.PlayAmbientSpeech("GENERIC_FUCK_YOU");
            Hooker.Tasks.ClearImmediately();
            Game.DisplaySubtitle("~b~Potential hooker: ~s~Oi, what's your problem?!", 5000);
            if (Suspect.Exists()) { Suspect.Dismiss(); }
            //msg = "Dispatch, our cover was blown. Nothing illegal took place. ~o~CODE 4, ~s~over.";
            
            DisplayCodeFourMessage();

        }
        
        





        private void DisplayCodeFourMessage()
        {
            if (CalloutRunning)
            {
                
                if (ActiveSolicitationTrafficStop != null)
                {
                    //DRIVER
                    if (ActiveSolicitationTrafficStop.DriverDead)
                    {
                        if (ActiveSolicitationTrafficStop.PursuitCreated || ActiveSolicitationTrafficStop.FightCreated)
                        {
                            Game.LogTrivial("Driver died after resisting ");
                            msg += "Driver died after resisting ";
                            Points += 5;
                        }
                        else
                        {
                            if (ActiveSolicitationTrafficStop.DriverShouldBeArrested)
                            {
                                Game.LogTrivial("Driver died but should have been arrested peacefully");
                                msg += "Driver died and should have been arrested peacefully ";
                                Points += 0;
                            }
                            else
                            {
                                Game.LogTrivial("Driver Killed Unlawfully ");
                                msg += "Driver died unlawfully ";
                                Points -= 5;
                            }
                        }
                    }
                    else
                    {
                        if (ActiveSolicitationTrafficStop.DriverShouldBeArrested)
                        {
                            if (ActiveSolicitationTrafficStop.DriverArrested)
                            {
                                if (ActiveSolicitationTrafficStop.PursuitCreated || ActiveSolicitationTrafficStop.FightCreated)
                                {
                                    Game.LogTrivial("Driver arrested lawfully after resisting ");
                                    msg += "Driver arrested lawfully after resisting ";
                                    Points += 20;
                                }
                                else
                                {
                                    Game.LogTrivial("Driver arrested lawfully.");
                                    msg += "Driver arrested lawfully. ";
                                    Points += 10;
                                }

                            }
                            else
                            {
                                Game.LogTrivial("Driver should have been arrested.");
                                msg += "Driver should have been arrested. ";
                                Points -= 5;
                            }
                        }
                        else
                        {
                            if (ActiveSolicitationTrafficStop.DriverArrested)
                            {
                                Game.LogTrivial("Driver arrested unlawfully.");
                                msg += "Driver arrested unlawfully. ";
                                Points -= 5;
                            }
                            else
                            {
                                Game.LogTrivial("Driver let go lawfully.");
                                msg += "Driver let go lawfully. ";
                                Points += 5;
                            }
                        }
                    }

                    //PASSENGER
                    if (ActiveSolicitationTrafficStop.PassengerDead)
                    {
                        if (ActiveSolicitationTrafficStop.PursuitCreated || ActiveSolicitationTrafficStop.FightCreated)
                        {
                            Game.LogTrivial("Passenger died after resisting ");
                            msg += "Passenger died after resisting ";
                            Points += 5;
                        }
                        else
                        {
                            if (ActiveSolicitationTrafficStop.PassengerShouldBeArrested)
                            {
                                Game.LogTrivial("Passenger died but should have been arrested peacefully ");
                                msg += "Passenger died and should have been arrested peacefully ";
                                Points += 0;
                            }
                            else
                            {
                                Game.LogTrivial("Passenger Killed Unlawfully ");
                                msg += "Passenger died unlawfully ";
                                Points -= 5;
                            }
                        }
                    }
                    else
                    {
                        if (ActiveSolicitationTrafficStop.PassengerShouldBeArrested)
                        {
                            if (ActiveSolicitationTrafficStop.PassengerArrested)
                            {
                                if (ActiveSolicitationTrafficStop.PursuitCreated || ActiveSolicitationTrafficStop.FightCreated)
                                {
                                    Game.LogTrivial("Passenger arrested lawfully after resisting. ");
                                    msg += "Passenger arrested lawfully after resisting. ";
                                    Points += 20;
                                }
                                else {
                                    Game.LogTrivial("Passenger arrested lawfully. ");
                                    msg += "Passenger arrested lawfully. ";
                                    Points += 10;
                                }
                            }
                            else
                            {
                                Game.LogTrivial("Passenger should have been arrested.");
                                msg += "Passenger should have been arrested. ";
                                Points -= 5;
                            }
                        }
                        else
                        {
                            if (ActiveSolicitationTrafficStop.PassengerArrested)
                            {
                                Game.LogTrivial("Passenger arrested unlawfully.");
                                msg += "Passenger arrested unlawfully. ";
                                Points -= 5;
                            }
                            else
                            {
                                Game.LogTrivial("Passenger let go lawfully.");
                                msg += "Passenger let go lawfully. ";
                                Points += 5;
                            }
                        }
                    }
                }
                else if (HookerScaredOff)
                {
                    Game.LogTrivial("Hooker scared off");
                    msg += "Hooker was scared off. ";
                    Points -= 5;
                }
                else if (SuspectsLost)
                {
                    Game.LogTrivial("Suspects were lost.");
                    msg += "Suspects were lost. ";
                    Points -= 10;
                }
                else
                {
                    if (Suspect.Exists())
                    {
                        if (Functions.IsPedArrested(Suspect))
                        {
                            Game.LogTrivial("Driver arrested after pursuit. ");
                            msg += "Driver arrested after pursuit. ";
                            Points += 10;
                        }
                        else if (Suspect.IsDead)
                        {
                            Game.LogTrivial("Driver dead after pursuit. ");
                            msg += "Driver dead after pursuit. ";
                            Points += 2;
                        }
                    }
                    else
                    {
                        Game.LogTrivial("Driver arrested after pursuit.");
                        msg += "Driver arrested after pursuit. ";
                        Points += 10;
                    }

                    if (Hooker.Exists())
                    {
                        if (Functions.IsPedArrested(Hooker))
                        {
                            Game.LogTrivial("Hooker arrested after pursuit.");
                            msg += "Hooker arrested after pursuit. ";
                            Points += 10;
                        }
                        else if (Hooker.IsDead)
                        {
                            Game.LogTrivial("Hooker died after pursuit");
                            msg += "Hooker died after pursuit. ";
                            Points += 2;
                        }
                    }
                    else
                    {
                        Game.LogTrivial("Hooker arrested after pursuit");
                        msg += "Hooker arrested after pursuit. ";
                        Points += 10;
                    }
                }
                string pointsmsg;
                if (Points >= 0)
                {
                    pointsmsg = "~g~Points earned: ~b~" + Points.ToString();
                }
                else
                {
                    pointsmsg = "~r~Points lost: ~b~" + Math.Abs(Points).ToString();
                }
                GameFiber.Sleep(4000);
                
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Assorted Callouts", "Solicitation ~g~CODE 4", msg);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Assorted Callouts", "Solicitation ~g~CODE 4", pointsmsg);
                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }

        private static UIMenu SolicitationTrafficStopMenu;
        
        private static UIMenuItem CheckDriverIDItem;
        private static UIMenuItem CheckPassengerIDItem;
        private static UIMenuItem StartQuestioningItem;
        public static MenuPool SolicitationMenuPool;
        private static UIMenuItem LeaveVehicleItem;
        private static UIMenuItem EndCallItem;
        private static UIMenuItem RequestBackupItem;

        private static UIMenu StartQuestioningMenu;

        private static UIMenuItem QuestionDriverItem;
        private static UIMenuItem QuestionPassengerItem;

        private static UIMenu OrderInOutOfVehicleMenu;
        private static UIMenuItem OrderDriverOut;
        private static UIMenuItem OrderPassengerOut;
        private static UIMenuItem OrderPassengerIn;
        private static UIMenuItem OrderDriverIn;

        private static UIMenu EndCallMenu;
        private static UIMenuItem ConfirmEndCallItem;
        private static TimerBarPool timerBarPool;
        private static BarTimerBar AwarenessBar;



        public static void LoadSolicitationMenus()
        {
            Game.FrameRender += ProcessSolicitationMenu;
            SolicitationMenuPool = new MenuPool();
            SolicitationTrafficStopMenu = new UIMenu("Traffic Stop", "~b~Solicitation Traffic Stop");

            SolicitationMenuPool.Add(SolicitationTrafficStopMenu);

            SolicitationTrafficStopMenu.AddItem(CheckDriverIDItem = new UIMenuItem("Ask for driver's ID."));
            SolicitationTrafficStopMenu.AddItem(CheckPassengerIDItem = new UIMenuItem("Ask for passenger's ID."));
            SolicitationTrafficStopMenu.AddItem(StartQuestioningItem = new UIMenuItem("Start Questioning"));
            SolicitationTrafficStopMenu.AddItem(LeaveVehicleItem = new UIMenuItem("Orders"));
            SolicitationTrafficStopMenu.AddItem(RequestBackupItem = new UIMenuItem("Request Backup"));
            SolicitationTrafficStopMenu.AddItem(EndCallItem = new UIMenuItem("End call"));

            StartQuestioningMenu = new UIMenu("Questioning", "~b~Solicitation Traffic Stop");
            StartQuestioningMenu.AddItem(QuestionDriverItem = new UIMenuItem("Question driver"));
            StartQuestioningMenu.AddItem(QuestionPassengerItem = new UIMenuItem("Question passenger"));

            
            SolicitationTrafficStopMenu.BindMenuToItem(StartQuestioningMenu, StartQuestioningItem);
            SolicitationMenuPool.Add(StartQuestioningMenu);

            OrderInOutOfVehicleMenu = new UIMenu("Orders", "~b~Solicitation Traffic Stop");
            OrderInOutOfVehicleMenu.AddItem(OrderDriverOut = new UIMenuItem("Order driver out"));
            OrderInOutOfVehicleMenu.AddItem(OrderDriverIn = new UIMenuItem("Order driver in"));
            OrderInOutOfVehicleMenu.AddItem(OrderPassengerOut = new UIMenuItem("Order passenger out"));
            OrderInOutOfVehicleMenu.AddItem(OrderPassengerIn = new UIMenuItem("Order passenger in"));
            SolicitationTrafficStopMenu.BindMenuToItem(OrderInOutOfVehicleMenu, LeaveVehicleItem);
            SolicitationMenuPool.Add(OrderInOutOfVehicleMenu);

            EndCallMenu = new UIMenu("End call", "~b~Solicitation Traffic Stop");
            EndCallMenu.AddItem(ConfirmEndCallItem = new UIMenuItem("Confirm end call"));
            SolicitationTrafficStopMenu.BindMenuToItem(EndCallMenu, EndCallItem);
            SolicitationMenuPool.Add(EndCallMenu);


            

            

            SolicitationTrafficStopMenu.RefreshIndex();
            SolicitationTrafficStopMenu.OnItemSelect += OnItemSelect;
            SolicitationTrafficStopMenu.MouseControlsEnabled = false;
            SolicitationTrafficStopMenu.AllowCameraMovement = true;
            StartQuestioningMenu.RefreshIndex();
            StartQuestioningMenu.OnItemSelect += OnItemSelect;
            StartQuestioningMenu.MouseControlsEnabled = false;
            StartQuestioningMenu.AllowCameraMovement = true;
            OrderInOutOfVehicleMenu.RefreshIndex();
            OrderInOutOfVehicleMenu.OnItemSelect += OnItemSelect;
            OrderInOutOfVehicleMenu.MouseControlsEnabled = false;
            OrderInOutOfVehicleMenu.AllowCameraMovement = true;
            EndCallMenu.RefreshIndex();
            EndCallMenu.OnItemSelect += OnItemSelect;
            EndCallMenu.MouseControlsEnabled = false;
            EndCallMenu.AllowCameraMovement = true;

            timerBarPool = new TimerBarPool();
            AwarenessBar = new BarTimerBar("Hooker awareness");
            AwarenessBar.ForegroundColor = System.Drawing.Color.Red;
            
            AwarenessBar.BackgroundColor = ControlPaint.Dark(AwarenessBar.ForegroundColor);
            
            Game.FrameRender += ProcessAwarenessBar;
            
        }
        public static void OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (sender == SolicitationTrafficStopMenu)
            {
                if (selectedItem == CheckDriverIDItem)
                {
                    CheckDriverIDItem.Enabled = false;
                    ActiveSolicitationTrafficStop.GetDriverID();
                }
                else if (selectedItem == CheckPassengerIDItem)
                {
                    CheckPassengerIDItem.Enabled = false;
                    ActiveSolicitationTrafficStop.GetPassengerID();
                }
                else if (selectedItem == StartQuestioningItem)
                {
                    //menu bound already

                }
                else if (selectedItem == LeaveVehicleItem)
                {
                    //ActiveSolicitationTrafficStop.OrderSuspectsOutOfVehicle(); menu item bound
                }
                else if (selectedItem == RequestBackupItem)
                {
                    ActiveSolicitationTrafficStop.RequestAssistance();
                    RequestBackupItem.Enabled = false;
                }
                else if (selectedItem == EndCallItem)
                {
                    ConfirmEndCallItem.Text = ActiveSolicitationTrafficStop.DetermineConfirmEndCallItemText();
                }
            }

            else if (sender == StartQuestioningMenu)
            { 
                if (selectedItem == QuestionDriverItem)
                {
                    ActiveSolicitationTrafficStop.QuestionDriver();
                    SolicitationMenuPool.CloseAllMenus();
                }
                else if (selectedItem == QuestionPassengerItem)
                {
                    ActiveSolicitationTrafficStop.QuestionPassenger();
                    SolicitationMenuPool.CloseAllMenus();
                }

            }
            else if (sender == OrderInOutOfVehicleMenu)
            {
                if (selectedItem == OrderPassengerIn)
                {
                    ActiveSolicitationTrafficStop.OrderPassengerInVehicle();
                }
                else if (selectedItem == OrderPassengerOut)
                {
                    ActiveSolicitationTrafficStop.OrderPassengerOutOfVehicle();
                }
                else if (selectedItem == OrderDriverIn)
                {
                    ActiveSolicitationTrafficStop.OrderDriverInVehicle();
                }
                else if (selectedItem == OrderDriverOut)
                {
                    ActiveSolicitationTrafficStop.OrderDriverOutOfVehicle();
                }
            }
            else if (sender == EndCallMenu)
            {
                if (selectedItem == ConfirmEndCallItem)
                {
                    ActiveSolicitationTrafficStop.EndTrafficStop();
                    SolicitationMenuPool.CloseAllMenus();
                }
            }
            
        }
        public static bool TimerReady = true;
        
        public static void ProcessSolicitationMenu(object sender, GraphicsEventArgs e)
        {
            if (!Functions.IsPoliceComputerActive())
            {
                if (Game.IsKeyDown(System.Windows.Forms.Keys.E) && ActiveSolicitationTrafficStop != null && TimerReady && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    if ((!ActiveSolicitationTrafficStop.Questioning && ActiveSolicitationTrafficStop.TrafficStopActive && Game.LocalPlayer.Character.DistanceTo(ActiveSolicitationTrafficStop.veh.Position) < 8f))
                    {
                        if (ActiveSolicitationTrafficStop.ArrestCanBeMade)
                        {
                            TimerReady = false;
                            GameFiber.StartNew(delegate
                            {
                                GameFiber.Sleep(100);
                                if (!Game.IsKeyDownRightNow(System.Windows.Forms.Keys.E))
                                {
                                    SolicitationTrafficStopMenu.Visible = !SolicitationTrafficStopMenu.Visible;
                                }
                                else
                                {
                                    GameFiber.Sleep(300);


                                    if (!Game.IsKeyDownRightNow(System.Windows.Forms.Keys.E))
                                    {
                                        SolicitationTrafficStopMenu.Visible = !SolicitationTrafficStopMenu.Visible;
                                    }
                                }
                                TimerReady = true;


                            });

                        }
                        else
                        {
                            SolicitationTrafficStopMenu.Visible = !SolicitationTrafficStopMenu.Visible;
                        }

                    }

                }
                if (ActiveSolicitationTrafficStop == null) { SolicitationMenuPool.CloseAllMenus(); }


                else if (ActiveSolicitationTrafficStop.Questioning || ActiveSolicitationTrafficStop.ReadyForCleanup) { SolicitationMenuPool.CloseAllMenus(); }
                
                else if ((ActiveSolicitationTrafficStop.DriverPickedUp || ActiveSolicitationTrafficStop.DriverArrested || ActiveSolicitationTrafficStop.DriverDead) && (ActiveSolicitationTrafficStop.PassengerPickedUp || ActiveSolicitationTrafficStop.PassengerArrested || ActiveSolicitationTrafficStop.PassengerDead))
                { 
                    if (!SolicitationMenuPool.IsAnyMenuOpen() && !ActiveSolicitationTrafficStop.ReadyForCleanup) { SolicitationTrafficStopMenu.Visible = true; }
                }
                
                
                

                SolicitationMenuPool.ProcessMenus();
            }
            
        }
        private static bool AwarenessBarDisplayTime = false;
        private static bool AwarenessbarInPool = false;
        public static void ProcessAwarenessBar(object sender, GraphicsEventArgs e)
        {
            
            if (AwarenessBarDisplayTime && !AwarenessbarInPool)
            {

                timerBarPool.Add(AwarenessBar);
                AwarenessbarInPool = true;
            }

            if (!AwarenessBarDisplayTime &&  AwarenessbarInPool)
            {
                timerBarPool.Remove(AwarenessBar);
                AwarenessbarInPool = false;
            }

            if (AwarenessBarDisplayTime)
            {
                timerBarPool.Draw();
            }

        }

















        public override bool OnCalloutAccepted()
        {
            CheckDriverIDItem.Enabled = true;
            CheckPassengerIDItem.Enabled = true;
            RequestBackupItem.Enabled = true;
            Hooker = new Ped(HookerModel, SpawnPoint, SpawnHeading);
            Hooker.MakeMissionPed();
            
            //HookerBlip = Hooker.AttachBlip();
            SearchArea = new Blip(SearchAreaLocation, 80f);
            SearchArea.Alpha = 0.5f;

            
            CalloutHandler();
            return base.OnCalloutAccepted();
        }

        public override void End()
        {

            base.End();
            CalloutRunning = false;
            if (Game.LocalPlayer.Character.Exists())
            {
                if (Game.LocalPlayer.Character.IsDead)
                {
                    GameFiber.Wait(1500);
                    Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                    GameFiber.Wait(3000);


                }
            }
            else
            {
                GameFiber.Wait(1500);
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                GameFiber.Wait(3000);
            }
            
            SpeechHandler.DisplayTime = false;
            AwarenessBarDisplayTime = false;
            if (SearchArea.Exists()) { SearchArea.Delete(); }
            
            if (ActiveSolicitationTrafficStop != null)
            {
                ActiveSolicitationTrafficStop.ReadyForCleanup = true;
            }
            ActiveSolicitationTrafficStop = null;
            if (!CalloutFinished)
            {
                if (Hooker.Exists()) { Hooker.Delete(); }
                
            }
            else
            {
                if (Hooker.Exists()) { Hooker.Dismiss(); }
            }
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
        public TupleList<Vector3, float> SolicitationSpawnPointsWithHeadings = new TupleList<Vector3, float>
        {
            { new Vector3(1.11159f, 7.131366f, 70.81869f), 343.5511f },
            { new Vector3(16.09362f, -48.4472f, 65.39057f), 240.5426f },
            { new Vector3(-15.25734f, -1353.067f, 29.32001f), 191.4787f },
            { new Vector3(380.2707f, -1344.468f, 31.94797f), 153.3647f },
            { new Vector3(365.831f, -1284.331f, 32.39956f), 218.9624f },
            { new Vector3(500.5454f, -1538.381f, 29.25421f), 217.7685f },
            { new Vector3(507.6362f, -1873.111f, 26.0823f), 115.1593f },
            { new Vector3(406.0564f, -1899.016f, 25.46394f), 53.33085f },
            { new Vector3(181.1292f, -1941.212f, 20.72161f), 219.8559f },
            { new Vector3(-11.21224f, -1832.038f, 25.2803f), 149.7927f },
            { new Vector3(-68.08526f, -1697.976f, 29.14181f), 116.3411f },
            { new Vector3(-61.79557f, -1476.506f, 32.11839f), 17.46057f },
            { new Vector3(99.02916f, -1059.341f, 29.32947f), 65.31042f },
            { new Vector3(305.5355f, -960.3099f, 29.41679f), 339.4712f },
            { new Vector3(492.7875f, -856.8344f, 25.10402f), 271.5143f },
            { new Vector3(470.421f, -685.5983f, 26.85441f), 0.4894677f },
            { new Vector3(-326.2603f, -832.9312f, 31.5862f), 172.0287f },
            { new Vector3(409.998f, 239.8577f, 103.0925f), 73.68296f },
            { new Vector3(526.7806f, 158.0215f, 99.06181f), 245.0596f },
            { new Vector3(743.0109f, 194.0317f, 84.19306f), 157.0315f },
            { new Vector3(-146.4937f, 101.4305f, 70.72218f), 156.6243f },
            { new Vector3(-291.0634f, 6203.086f, 31.46759f), 322.6241f },
            { new Vector3(-247.559f, 6274.483f, 31.42747f), 53.361f },
            { new Vector3(-158.1036f, 6331.151f, 31.58081f), 305.3809f },
            { new Vector3(-425.1705f, -63.85234f, 43.15913f), 205.0624f },
            { new Vector3(-515.4243f, -72.55444f, 39.96117f), 153.811f },
            { new Vector3(-502.2392f, 117.7743f, 63.56012f), 347.6635f },
            { new Vector3(-644.139f, -297.4451f, 35.28395f), 118.4866f },
            { new Vector3(-393.8847f, -402.9625f, 31.75776f), 335.7862f },
            { new Vector3(215.6861f, -746.2953f, 33.6209f), 67.71255f },
            { new Vector3(157.8786f, -600.8004f, 43.73821f), 349.5942f },
            { new Vector3(-122.429f, -1228.022f, 28.64725f), 261.2349f },
            { new Vector3(-88.10262f, -1225.442f, 28.50907f), 93.0585f },
            { new Vector3(573.5306f, -1650.029f, 26.84852f), 187.808f },
        };


    }
}
