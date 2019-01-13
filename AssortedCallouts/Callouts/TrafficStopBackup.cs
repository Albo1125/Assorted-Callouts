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
    [CalloutInfo("Traffic Stop Backup", CalloutProbability.High)]

    internal class TrafficStopBackup : AssortedCallout
    {

        private bool CalloutRunning = false;
        private TupleList<Vector3, float> ValidTrafficStopSpawnPointsWithHeadings = new TupleList<Vector3, float>();
        private Tuple<Vector3, float> ChosenSpawnData;
        private List<Blip> BlipList = new List<Blip>();
        private Vehicle PoliceCar;
       
        private Ped PoliceOfficer;
        private Blip PoliceOfficerBlip;
        private string msg;
        private bool SuspectArrested = false;
        private bool PlayerArrest;
        private bool Chasing = false;
        private bool APICopSet = false;
        private Rage.Object notepad;
        private string[] CityCarModels = new string[] { "POLICE", "POLICE2", "POLICE3", "POLICE4" };
        private string[] CountrysideCarModels = new string[] { "SHERIFF", "SHERIFF2" };
        private Model CopCarModel;
        private string[] firearmsToSelectFrom = new string[] { "WEAPON_PISTOL", "WEAPON_APPISTOL",  "WEAPON_MICROSMG", "WEAPON_SMG",  "WEAPON_ASSAULTRIFLE"
                                                                , "WEAPON_ADVANCEDRIFLE", "WEAPON_PISTOL50", "WEAPON_ASSAULTSMG" };
        private Model suspectCarModel;
        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("AssortedCallouts.TrafficStopBackup");
            
            foreach (Tuple<Vector3, float> tuple in Albo1125.Common.CommonLibrary.CommonVariables.TrafficStopSpawnPointsWithHeadings)
            {
                //tuple.Item1 = PedInTuple
                //tuple.Item2 = Vector3 in tuple
                //tuple.Item3 = FloatinTuple
                //Game.LogTrivial(tuple.Item1.ToString());
                //Game.LogTrivial(tuple.Item2.ToString());
                
                if ((Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) < 750f) && (Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) > 280f))
                {
                    ValidTrafficStopSpawnPointsWithHeadings.Add(tuple);
                }
            }
            //foreach (Tuple<Ped, Vector3, float> tuple in PedVector3HeadingTupleList)
            //{
            //    //tuple.Item1 = PedInTuple
            //    //tuple.Item2 = Vector3 in tuple
            //    //tuple.Item3 = FloatinTuple
            //}
            if (ValidTrafficStopSpawnPointsWithHeadings.Count == 0) { return false; }
            ChosenSpawnData = ValidTrafficStopSpawnPointsWithHeadings[AssortedCalloutsHandler.rnd.Next(ValidTrafficStopSpawnPointsWithHeadings.Count)];


            SpawnPoint = ChosenSpawnData.Item1;
            SpawnHeading = ChosenSpawnData.Item2;
            uint zoneHash = Rage.Native.NativeFunction.CallByHash<uint>(0x7ee64d51e8498728, SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);

            
            if (Game.GetHashKey("city") == zoneHash)
            {
                CopCarModel = new Model(CityCarModels[AssortedCalloutsHandler.rnd.Next(CityCarModels.Length)]);
            }
            else
            {
                CopCarModel = new Model(CountrysideCarModels[AssortedCalloutsHandler.rnd.Next(CountrysideCarModels.Length)]);
            }
            CopCarModel.LoadAndWait();
            suspectCarModel = GroundVehiclesToSelectFrom[AssortedCalloutsHandler.rnd.Next(GroundVehiclesToSelectFrom.Length)];
            suspectCarModel.LoadAndWait();
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 10f);
            CalloutMessage = "Traffic Stop Backup Required";

            CalloutPosition = SpawnPoint;

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Traffic Stop Backup Required", "Traffic Stop Backup", SpawnPoint, 0, "Officer is requesting backup for a traffic stop. Situation currently under control. Please respond.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_OFFICER_IN_NEED_OF_ASSISTANCE IN_OR_ON_POSITION", SpawnPoint);


            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            PoliceCar = new Vehicle(CopCarModel, SpawnPoint, SpawnHeading);
            Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(PoliceCar);
            PoliceCar.MakePersistent();
            PoliceCar.IsSirenOn = true;
            PoliceCar.IsSirenSilent = true;
            PoliceOfficer = PoliceCar.CreateRandomDriver();
            PoliceOfficer.MakeMissionPed();
            PoliceOfficerBlip = PoliceOfficer.AttachBlip();
            PoliceOfficerBlip.Color = Color.Green;
            PoliceOfficerBlip.IsRouteEnabled = true;
            PoliceOfficer.RelationshipGroup = "PLAYER";
            SuspectCar = new Vehicle(suspectCarModel, PoliceCar.GetOffsetPosition(Vector3.RelativeFront * 9f), PoliceCar.Heading);
            Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(SuspectCar);
            SuspectCar.MakePersistent();
            Suspect = SuspectCar.CreateRandomDriver();
            Suspect.MakeMissionPed();
            SuspectCar.IsEngineOn = true;
            Suspect.RelationshipGroup = "TBACKUPCRIMINAL";


            //foreach (Tuple<Vector3, float> tuple in ValidTrafficStopSpawnPointsWithHeadings)
            //{
            //    Blip blip = new Blip(tuple.Item1);
            //    blip.Scale = 0.6f;

            //    BlipList.Add(blip);
            //}
            if (!CalloutRunning)
            {
                int MaxNumber = 15;
                if (!AssortedCalloutsHandler.IsLSPDFRPluginRunning("Traffic Policer", new Version("6.9.1.0")))
                {
                    MaxNumber = 12;
                }

                int SituationNumber = AssortedCalloutsHandler.rnd.Next(MaxNumber);
                //SituationNumber = 11;
                
                Game.LogTrivial("SituationNumber: " + SituationNumber.ToString());
                if (SituationNumber < 3)
                {
                    SituationOne();
                }
                else if (SituationNumber < 6)
                {
                    SituationTwo();
                }
                else if (SituationNumber < 9)
                {
                    SituationThree();
                }
                else if (SituationNumber < 12)
                {
                    SituationFour();
                }

                else if (SituationNumber < 15)
                {
                    SituationAlcohol();
                }

            }
            return base.OnCalloutAccepted();
        }
        private void HandleBackupOfficerCleanup()
        {
            GameFiber.StartNew(delegate
            {
                try
                {


                    if (PoliceOfficer.Exists() && PoliceCar.Exists())
                    {
                        if (PoliceOfficer.IsDead) { return; }
                        PoliceOfficer.BlockPermanentEvents = true;
                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceCar.Heading, 1.6f).WaitForCompletion(6000);
                        PoliceOfficer.Tasks.EnterVehicle(PoliceCar, 7000, -1).WaitForCompletion();
                        PoliceOfficer.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        while (true)
                        {
                            GameFiber.Yield();
                            if (!PoliceOfficer.Exists() || !PoliceCar.Exists()) { break; }
                            if (Vector3.Distance(PoliceOfficer.Position, Game.LocalPlayer.Character.Position) > 60f)
                            {
                                break;
                            }
                            else if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Game.LocalPlayer.Character.Speed > 7f)
                                {
                                    GameFiber.Wait(2000);
                                    break;
                                }
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    
                   
                }
                finally
                {
                    if (PoliceCar.Exists()) { PoliceCar.Dismiss(); }
                    if (PoliceOfficer.Exists()) { PoliceOfficer.Dismiss(); }
                    Game.LogTrivial("TrafficStopBackup officer cleaned.");
                }
            });
        }

        public override void End()
        {
            
            CalloutRunning = false;
            Rage.Native.NativeFunction.Natives.RESET_AI_MELEE_WEAPON_DAMAGE_MODIFIER();
            if (Game.LocalPlayer.Character.IsDead)
            {
                GameFiber.Wait(1500);
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                GameFiber.Wait(3000);


            }
            base.End();
            if (notepad.Exists()) { notepad.Delete(); }
            if (PoliceOfficerBlip.Exists()) { PoliceOfficerBlip.Delete(); }
            foreach (Blip blip in BlipList) { if (blip.Exists()) { blip.Delete(); } }
            if (CalloutFinished)
            {
                HandleBackupOfficerCleanup();
                if (Suspect.Exists())
                {
                    if (!Suspect.IsInAnyVehicle(false))
                    {
                        Suspect.Dismiss();
                    }
                    else
                    {
                        if (Suspect.CurrentVehicle.Driver == Suspect)
                        {
                            Suspect.Dismiss();
                        }
                        else
                        {
                            Suspect.Delete();
                        }
                        
                    }
                    
                }
                if (SuspectCar.Exists()) { SuspectCar.Dismiss(); }
            }
            else
            {
                if (PoliceCar.Exists()) { PoliceCar.Delete(); }


                if (PoliceOfficer.Exists()) { PoliceOfficer.Delete(); }
                if (Suspect.Exists()) { Suspect.Delete(); }
                if (SuspectCar.Exists()) { SuspectCar.Delete(); }
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

        private void DispatchResponse()
        {
            GameFiber.Wait(5000);
            Functions.PlayScannerAudio("COPY_THAT_MOVING_RIGHT_NOW REPORT_RESPONSE_COPY");
            Game.DisplayNotification("~b~Control: ~s~Please respond to the officer requesting backup.");
            PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, true);
        }
        private void WaitForGetClose()
        {
            while (CalloutRunning)
            {
                GameFiber.Yield();
                PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 45f)
                {
                    Game.DisplayHelp("Park up behind the vehicles and make contact with the officer.");
                    SuspectBlip = Suspect.AttachBlip();
                    SuspectBlip.Color = Color.Red;
                    SuspectBlip.Scale = 0.7f;
                    PoliceOfficerBlip.IsRouteEnabled = false;
                    PoliceOfficerBlip.Scale = 0.7f;
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                    }
                    break;
                }
            }
        }
        private void WaitForParkAndGetNearby()
        {
            while (CalloutRunning)
            {
                GameFiber.Yield();
                PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 6f)
                    {
                        if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                        {
                            PoliceOfficer.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            break;
                        }
                    }
                }
            }
        }
        private void DisplayCodeFourMessage()
        {
            if (CalloutRunning)
            {
                GameFiber.Sleep(4000);
                Game.DisplayNotification(msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }
        /// <summary>
        /// Felony, player chooses player/officer arrest.
        /// </summary>
        private void SituationOne()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    DispatchResponse();
                    WaitForGetClose();
                    WaitForParkAndGetNearby();
                    Suspect.Health += 180;
                    Suspect.Armor += 60;
                   
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 4f)
                        {
                            Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                            {
                                PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, true);
                                SpeechHandler.HandleSpeech("Officer", ArrestWarrantSpeeches[AssortedCalloutsHandler.rnd.Next(ArrestWarrantSpeeches.Count)]);

                                
                                Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(PoliceOfficer, Suspect.Position.X, Suspect.Position.Y, Suspect.Position.Z, -1, false, false);
                                List<string> answers = ArrestWarrantAnswers[AssortedCalloutsHandler.rnd.Next(ArrestWarrantAnswers.Count)];
                                int ans = SpeechHandler.DisplayAnswers(answers);
                                if (ans == 0)
                                {

                                    SpeechHandler.HandleSpeech("You", new List<string>() { answers[0] });
                                    PlayerArrest = true;
                                }
                                else
                                {
                                    SpeechHandler.HandleSpeech("You", new List<string>() { answers[1] });
                                    PlayerArrest = false;
                                }
                                break;
                            }
                        }
                        else
                        {
                            Game.HideHelp();
                        }
                    }
                    if (CalloutRunning)
                    {


                        if (PlayerArrest)
                        {
                            PlayerMakesArrest();
                        }
                        else
                        {
                            AIOfficerMakesArrest();
                        }
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }
        private void AIOfficerMakesArrest()
        {
            try
            {
                Game.LogTrivial("Officer arrest");
                Game.LogTrivial("Officer rel group:" + PoliceOfficer.RelationshipGroup.Name);
                Game.DisplayHelp("Provide backup and cover for your ~b~fellow officer.");
                Suspect.Model.LoadAndWait();
                GameFiber.Wait(3000);
                Rage.Native.NativeFunction.Natives.TASK_GOTO_ENTITY_AIMING(PoliceOfficer, Suspect, 0f, 15f);
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    if (Vector3.Distance(PoliceOfficer.Position, Suspect.Position) < 3.5f)
                    {
                        PoliceOfficer.Tasks.ClearImmediately();
                        Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(PoliceOfficer, Suspect.Position.X, Suspect.Position.Y, Suspect.Position.Z, -1, false, false);
                        
                        break;
                    }
                }
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.SetRelationshipBetweenRelationshipGroups("TBACKUPCRIMINAL", "PLAYER", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "TBACKUPCRIMINAL", Relationship.Hate);
                    int randomroll = AssortedCalloutsHandler.rnd.Next(9);
                    
                    Game.LogTrivial("Randomroll:" + randomroll);
                    if (randomroll < 3)
                    {
                        if (Suspect.IsInVehicle(SuspectCar, false))
                        {
                            GameFiber.Wait(1500);
                            Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion();

                        }
                        Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
                        Suspect.Tasks.FightAgainstClosestHatedTarget(25f);
                        GameFiber.Wait(3000);
                        PoliceOfficer.Tasks.FightAgainst(Suspect);


                    }
                    else if (randomroll < 6)
                    {
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        GameFiber.Wait(1500);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                        NativeFunction.Natives.TASK_SHOOT_AT_COORD(PoliceOfficer, SuspectCar.Wheels[0].LastContactPoint.X, SuspectCar.Wheels[0].LastContactPoint.Y, SuspectCar.Wheels[0].LastContactPoint.Z, 1500, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                        GameFiber.Wait(1500);
                        PoliceCar.IsSirenOn = true;
                        PoliceCar.IsSirenSilent = false;
                        Chasing = true;
                    }
                    else if (randomroll < 9)
                    {
                        if (Suspect.IsInVehicle(SuspectCar, false))
                        {
                            GameFiber.Wait(1500);
                            Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();

                        }

                        Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);

                        MakeArrest(PoliceOfficer, Suspect, false);
                        
                        Suspect.Tasks.FightAgainstClosestHatedTarget(20f);
                        GameFiber.Wait(2200);
                        PoliceOfficer.Tasks.FightAgainst(Suspect);

                    }
                    else
                    {
                        if (Suspect.IsInVehicle(SuspectCar, false))
                        {
                            GameFiber.Wait(1500);
                            Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();

                        }
                        Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
                        MakeArrest(PoliceOfficer, Suspect, true);
                        SuspectArrested = true;
                        NativeFunction.Natives.TASK_OPEN_VEHICLE_DOOR(PoliceOfficer, PoliceCar, 18000f, 1, 1.47f);
                        int waitCount = 0;
                        while (true)
                        {
                            GameFiber.Sleep(1000);
                            waitCount++;
                            if (PoliceCar.Doors[2].IsOpen)
                            {
                                GameFiber.Sleep(1000);
                                break;
                            }
                            if (waitCount >= 18)
                            {
                                break;
                            }
                            if (Suspect.Exists())
                            {
                                if (!Suspect.IsDead)
                                {
                                    NativeFunction.Natives.TASK_OPEN_VEHICLE_DOOR(PoliceOfficer, PoliceCar, 18000f, 1, 1.47f);
                                    Suspect.Tasks.FollowNavigationMeshToPosition(PoliceOfficer.GetOffsetPosition(Vector3.RelativeBack * 1f), PoliceOfficer.Heading, 1.33f);
                                }
                                else { break; }
                            }
                            else { break; }


                        }
                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceOfficer.GetOffsetPosition(Vector3.RelativeBack * 2.4f), PoliceOfficer.Heading, 1.47f).WaitForCompletion(2000);
                        Suspect.Tasks.EnterVehicle(PoliceCar, 6000, 1).WaitForCompletion();

                        PoliceCar.Doors[2].Close(false);
                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.GetOffsetPosition(Vector3.RelativeLeft * 1.3f), PoliceCar.Heading,
                            1.4f).WaitForCompletion(3000);
                        PoliceOfficer.Tasks.EnterVehicle(PoliceCar, 7000, -1).WaitForCompletion();

                    }
                    break;
                }
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                    if (!Suspect.Exists())
                    {
                        msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                        break;
                    }
                    else if (Functions.IsPedArrested(Suspect) || SuspectArrested)
                    {
                        msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                        break;
                    }
                    else if (Suspect.IsDead)
                    {
                        msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
                        break;
                    }




                    if (Chasing && !APICopSet)
                    {
                        if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                        {
                            Functions.SetPedAsCop(PoliceOfficer);
                            Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                            APICopSet = true;


                        }
                        else
                        {
                            if (Vector3.Distance(PoliceCar.Position, PoliceOfficer.Position) < 3.5f)
                            {
                                PoliceOfficer.Tasks.EnterVehicle(PoliceCar, -1).WaitForCompletion(1000);
                            }
                            else
                            {
                                PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.Position, PoliceCar.Heading, 1.6f).WaitForCompletion(300);
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
                    Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                    Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                    End();
                }
            }
        }

        
        private static void MakeArrest(Ped _Officer, Ped _Suspect, bool Successful)
        {
            Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(_Officer, _Suspect.Position.X, _Suspect.Position.Y, _Suspect.Position.Z, -1, false, false);
            _Suspect.Tasks.PlayAnimation("random@getawaydriver", "idle_2_hands_up", 1f, AnimationFlags.UpperBodyOnly | AnimationFlags.StayInEndFrame | AnimationFlags.SecondaryTask);
            _Suspect.Tasks.AchieveHeading(_Officer.Heading).WaitForCompletion(1500);
            _Suspect.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
            GameFiber.Wait(1000);
            _Officer.Tasks.FollowNavigationMeshToPosition(_Suspect.RearPosition, _Suspect.Heading, 1.2f, 0.8f).WaitForCompletion(7000);
            if (Successful)
            {
                GameFiber.Wait(600);
                NativeFunction.Natives.SET_PED_DROPS_WEAPON( _Suspect);
                _Officer.Tasks.PlayAnimation("mp_arresting", "a_arrest_on_floor", 1f, AnimationFlags.None).WaitForCompletion(7000);
                _Suspect.Tasks.PlayAnimation("mp_arresting", "idle", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                
            }
            else
            {
                _Suspect.Tasks.ClearImmediately();
            }


        }

        private void PlayerMakesArrest()
        {


            try
            {
                Game.LogTrivial("Player arrest");
                Game.DisplayHelp("Arrest the ~r~wanted suspect.");
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    if (!Suspect.Exists()) { break; }
                    Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(PoliceOfficer, Suspect.Position.X, Suspect.Position.Y, Suspect.Position.Z, -1, false, false);
                    if (!Suspect.IsInAnyVehicle(true))
                    {
                        GameFiber.Wait(100);

                        Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
                        int randomroll = AssortedCalloutsHandler.rnd.Next(14);

                        Game.LogTrivial("Randomroll: " + randomroll.ToString());
                        if (randomroll < 3)
                        {
                            Rage.Native.NativeFunction.Natives.TASK_SMART_FLEE_PED(Suspect, Game.LocalPlayer.Character, 100f, 2500, true, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            GameFiber.Wait(2500);
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.SetPedAsCop(PoliceOfficer);
                            Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                            APICopSet = true;

                        }
                        else if (randomroll < 6)
                        {
                            Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(Suspect, Game.LocalPlayer.Character, 2000, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                            GameFiber.Wait(2500);
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.SetPedAsCop(PoliceOfficer);
                            Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                            APICopSet = true;
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);

                        }
                        else if (randomroll < 9)
                        {
                            Suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                            Suspect.Model.LoadAndWait();
                            while (true)
                            {
                                GameFiber.Yield();
                                if (Functions.IsPedGettingArrested(Suspect))
                                {
                                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 1.4f)
                                    {
                                        GameFiber.Wait(AssortedCalloutsHandler.rnd.Next(1000, 1600));
                                        break;
                                    }
                                }
                            }
                            Suspect = Suspect.ClonePed();
                            Suspect.Inventory.GiveNewWeapon("WEAPON_KNIFE", -1, true);

                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                            Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_MOVEMENT(Suspect, 3);
                            Suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            GameFiber.Wait(3000);
                            
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.SetPedAsCop(PoliceOfficer);
                            Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                            APICopSet = true;
                        }
                        else if (randomroll < 12)
                        {
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            GameFiber.Wait(1500);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            NativeFunction.Natives.TASK_SHOOT_AT_COORD(PoliceOfficer, SuspectCar.Wheels[0].LastContactPoint.X, SuspectCar.Wheels[0].LastContactPoint.Y, SuspectCar.Wheels[0].LastContactPoint.Z, 1500, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                            GameFiber.Wait(1500);
                            PoliceCar.IsSirenOn = true;
                            PoliceCar.IsSirenSilent = false;
                            Chasing = true;
                        }
                        else
                        {
                            Suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                        }

                        break;
                    }
                    else
                    {
                        if (Vector3.Distance(PoliceOfficer.Position, Suspect.Position) > 35f)
                        {
                            if (!PoliceOfficer.IsInVehicle(PoliceCar, false))
                            {
                                PoliceOfficer.Tasks.EnterVehicle(PoliceCar, 5000, -1).WaitForCompletion();
                            }
                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(PoliceOfficer, Suspect);
                            GameFiber.Wait(500);


                        }
                    }
                }



                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                    if (!Suspect.Exists())
                    {
                        msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                        break;
                    }
                    else if (Functions.IsPedArrested(Suspect))
                    {
                        msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                        break;
                    }
                    else if (Suspect.IsDead)
                    {
                        msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
                        break;
                    }




                    if (Chasing && !APICopSet)
                    {
                        if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                        {
                            Functions.SetPedAsCop(PoliceOfficer);
                            Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                            APICopSet = true;


                        }
                        else
                        {
                            if (Vector3.Distance(PoliceCar.Position, PoliceOfficer.Position) < 3.5f)
                            {
                                PoliceOfficer.Tasks.EnterVehicle(PoliceCar, -1).WaitForCompletion(1000);
                            }
                            else
                            {
                                PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.Position, PoliceCar.Heading, 1.6f).WaitForCompletion(300);
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
                    Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                    Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                    End();
                }
            }

        }

        /// <summary>
        /// Flee method
        /// </summary>
        private void SituationTwo()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    bool initiatePursuitImmediately = false;
                    Suspect.Health += 150;
                    Suspect.Armor += 50;
                    DispatchResponse();
                    int randomroll = AssortedCalloutsHandler.rnd.Next(7);
                    Game.LogTrivial("Randomroll: " + randomroll);
                    if (randomroll < 3)
                    {
                        WaitForGetClose();
                        WaitForParkAndGetNearby();
                        GameFiber.Wait(1000);
                    }
                    else
                    {
                        float initiatePursuitDistance = Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) * 0.3f;
                        if (initiatePursuitDistance < 70f) { initiatePursuitDistance = 70f; }
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < initiatePursuitDistance)
                            {
                                initiatePursuitImmediately = true;
                                PoliceOfficerBlip.IsRouteEnabled = false;
                                if (AssortedCalloutsHandler.English == AssortedCalloutsHandler.EnglishTypes.BritishEnglish)
                                {
                                    Game.DisplayNotification("~b~Requesting Officer:~s~ The vehicle is ~r~making off.~b~ Giving chase.");
                                }
                                else
                                {
                                    Game.DisplayNotification("~b~Requesting Officer:~s~ The vehicle is ~r~fleeing,~b~ In pursuit.");
                                }
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicle fleeing. In pursuit.");
                                }
                                break;
                            }

                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) || initiatePursuitImmediately)
                        {
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            GameFiber.Wait(1500);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            Chasing = true;
                            PoliceCar.IsSirenOn = true;
                            PoliceCar.IsSirenSilent = false;
                            break;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        if (!Suspect.Exists())
                        {
                            msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                            break;
                        }
                        else if (Functions.IsPedArrested(Suspect))
                        {
                            msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                            break;
                        }
                        else if (Suspect.IsDead)
                        {
                            msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
                            break;
                        }




                        if (Chasing && !APICopSet)
                        {
                            if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                            {
                                Functions.SetPedAsCop(PoliceOfficer);
                                Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                                APICopSet = true;


                            }
                            else
                            {
                                if (Vector3.Distance(PoliceCar.Position, PoliceOfficer.Position) < 3.5f)
                                {
                                    PoliceOfficer.Tasks.EnterVehicle(PoliceCar, -1).WaitForCompletion(1000);
                                }
                                else
                                {
                                    PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.Position, PoliceCar.Heading, 1.6f).WaitForCompletion(300);
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        /// <summary>
        /// Ticket/Warning method
        /// </summary>
        private void SituationThree()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    DispatchResponse();
                    WaitForGetClose();
                    WaitForParkAndGetNearby();
                    Suspect.Health += 180;
                    Suspect.Armor += 60;

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 4f)
                        {
                            Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                            {
                                //PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, false);
                                int HighestIndex = TicketSpeeches.Count;
                                if (SuspectCar.Model.IsBike) { HighestIndex--; }
                                

                                SpeechHandler.HandleSpeech("Officer", TicketSpeeches[AssortedCalloutsHandler.rnd.Next(HighestIndex)]);
                                break;


                            }
                        }
                    }
                    if (CalloutRunning)
                    {
                        Game.DisplayHelp("Back up your fellow officer for the duration of the traffic stop.");
                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(SuspectCar.GetOffsetPosition(Vector3.RelativeLeft * 1f), Suspect.Heading + 270f, 1.3f).WaitForCompletion(9000);
                        GameFiber.Wait(1500);
                        int randomroll = AssortedCalloutsHandler.rnd.Next(5);
                        

                        Game.LogTrivial("Randomroll:" + randomroll.ToString());
                        if (randomroll < 3)
                        {
                            PoliceOfficer.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), 0, true);
                            notepad = new Rage.Object("prop_notepad_02", PoliceOfficer.Position);
                            int boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(PoliceOfficer, (int)PedBoneId.LeftThumb2);
                            NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(notepad, PoliceOfficer, boneIndex, 0f, 0f, 0f, 0f, 0f, 0f, true, false, false, false, 2, 1);
                            PoliceOfficer.Tasks.PlayAnimation("veh@busted_std", "issue_ticket_cop", 0.8f, AnimationFlags.UpperBodyOnly).WaitForCompletion(9000);
                            notepad.Delete();
                            PoliceOfficer.PlayAmbientSpeech("GENERIC_THANKS");
                            GameFiber.Wait(2000);
                            CalloutFinished = true;
                            msg = "Control, the ~r~suspect~s~ has ~g~been ticketed.~s~ We are ~g~CODE 4~s~, over.";
                            Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight).WaitForCompletion(600);
                            Suspect.Tasks.CruiseWithVehicle(18f);
                            Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);
                            DisplayCodeFourMessage();
                            return;

                        }
                        else 
                        {
                            PoliceOfficer.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), 0, true);
                            notepad = new Rage.Object("prop_notepad_02", PoliceOfficer.Position);
                            int boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(PoliceOfficer, (int)PedBoneId.LeftThumb2);
                            NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(notepad, PoliceOfficer, boneIndex, 0f, 0f, 0f, 0f, 0f, 0f, true, false, false, false, 2, 1);
                            PoliceOfficer.Tasks.PlayAnimation("veh@busted_std", "issue_ticket_cop", 0.8f, AnimationFlags.UpperBodyOnly).WaitForCompletion(3000);
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            GameFiber.Wait(1500);
                            
                            notepad.Delete();
                            PoliceOfficer.Tasks.Clear();
                            PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            //NativeFunction.Natives.TASK_SHOOT_AT_COORD(PoliceOfficer, SuspectCar.Wheels[0].LastContactPoint.X, SuspectCar.Wheels[0].LastContactPoint.Y, SuspectCar.Wheels[0].LastContactPoint.Z, 1500, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                            GameFiber.Wait(1500);
                            PoliceCar.IsSirenOn = true;
                            PoliceCar.IsSirenSilent = false;
                            Chasing = true;

                        }
                        
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                            if (!Suspect.Exists())
                            {
                                msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                                break;
                            }
                            else if (Functions.IsPedArrested(Suspect))
                            {
                                msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                                break;
                            }
                            else if (Suspect.IsDead)
                            {
                                msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
                                //PedVector3HeadingTupleList.Add(new Ped(new Vector3(0,0,0)), new Vector3(), 0f);
                                break;
                            }




                            
                            if (Chasing && !APICopSet)
                            {
                                if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                                {
                                    Functions.SetPedAsCop(PoliceOfficer);
                                    Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                                    APICopSet = true;
                                    

                                }
                                else
                                {
                                    if (Vector3.Distance(PoliceCar.Position, PoliceOfficer.Position) < 3.5f)
                                    {
                                        PoliceOfficer.Tasks.EnterVehicle(PoliceCar, -1).WaitForCompletion(1000);
                                    }
                                    else
                                    {
                                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.Position, PoliceCar.Heading, 1.6f).WaitForCompletion(300);
                                    }

                                }

                            }
                            
                        }
                        //TrafficStopSpawnPointsWithHeadings.ToList().Shuffle();
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }
        /// <summary>
        /// Shots fired, immediately or during response
        /// </summary>
        private void SituationFour()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    bool startfightimmediately = false;
                    Suspect.Health += 180;
                    Suspect.Armor += 60;
                    DispatchResponse();
                    int roll = AssortedCalloutsHandler.rnd.Next(5);
                    Game.LogTrivial("Roll: " + roll.ToString());
                    if (roll < 2)
                    {
                        WaitForGetClose();
                        WaitForParkAndGetNearby();
                        GameFiber.Wait(1000);
                    }
                    else
                    {
                        float initiatePursuitDistance = Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) * 0.6f;
                        if (initiatePursuitDistance < 160f) { initiatePursuitDistance = 160f; }
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            Game.SetRelationshipBetweenRelationshipGroups("TBACKUPCRIMINAL", "PLAYER", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "TBACKUPCRIMINAL", Relationship.Hate);
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < initiatePursuitDistance)
                            {

                                PoliceOfficerBlip.IsRouteEnabled = false;
                                startfightimmediately = true;
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspect fighting, has a weapon. Please respond as fast as possible.");
                                }

                                break;
                            }

                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        Game.SetRelationshipBetweenRelationshipGroups("TBACKUPCRIMINAL", "PLAYER", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "TBACKUPCRIMINAL", Relationship.Hate);
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) || startfightimmediately)
                        {
                            if (Suspect.IsInAnyVehicle(false)) { Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen).WaitForCompletion(5000); }

                            Suspect.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
                            Suspect.Tasks.FightAgainstClosestHatedTarget(25f);

                            
                            GameFiber.Wait(2700);
                            Game.DisplayNotification("~b~Requesting Officer:~r~ Shots fired! Need immediate assistance!");
                            Functions.PlayScannerAudioUsingPosition("AI_OFFICER_REQUEST_BACKUP WE_HAVE CRIME_OFFICER_UNDER_FIRE IN_OR_ON_POSITION UNITS_RESPOND_CODE_99", PoliceOfficer.Position);
                            PoliceOfficer.Tasks.FightAgainst(Suspect);
                            Functions.RequestBackup(PoliceOfficer.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                            Functions.RequestBackup(PoliceOfficer.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                            break;
                        }
                    }


                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        Game.SetRelationshipBetweenRelationshipGroups("TBACKUPCRIMINAL", "PLAYER", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "TBACKUPCRIMINAL", Relationship.Hate);
                        Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                        if (!Suspect.Exists())
                        {
                            msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                            break;
                        }
                        else if (Functions.IsPedArrested(Suspect) || SuspectArrested)
                        {
                            msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                            break;
                        }
                        else if (Suspect.IsDead)
                        {
                            msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        

        private void SituationAlcohol()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    DispatchResponse();
                    WaitForGetClose();
                    WaitForParkAndGetNearby();
                    Suspect.Health += 180;
                    Suspect.Armor += 60;

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 4f)
                        {
                            Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                            {
                                //PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, false);
                                


                                SpeechHandler.HandleSpeech("Officer", AlcoholSpeeches[AssortedCalloutsHandler.rnd.Next(AlcoholSpeeches.Count)]);
                                break;


                            }
                        }
                    }
                    if (CalloutRunning)
                    {
                        Game.DisplayHelp("Back up your fellow officer for the duration of the traffic stop.");
                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(SuspectCar.GetOffsetPosition(Vector3.RelativeLeft * 1f), Suspect.Heading + 270f, 1.3f).WaitForCompletion(9000);
                        GameFiber.Wait(1500);
                        int randomroll = AssortedCalloutsHandler.rnd.Next(5);


                        Game.LogTrivial("Randomroll:" + randomroll.ToString());
                        AnimationSet drunkAnimset = new AnimationSet("move_m@drunk@verydrunk");
                        drunkAnimset.LoadAndWait();
                        Suspect.MovementAnimationSet = drunkAnimset;
                        API.TrafficPolicerFunctions.SetPedAsDrunk(Suspect);
                        if (randomroll < 3)
                        {
                            
                            PoliceOfficer.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), 0, true);
                            PoliceOfficer.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_e", 2f, 0).WaitForCompletion(7000);
                            PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceCar.Heading, 1.3f).WaitForCompletion(9000);
                            while (CalloutRunning)
                            {
                                GameFiber.Yield();
                                PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 4f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        //PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, false);



                                        SpeechHandler.HandleSpeech("Officer", new List<string>() { "I could smell alcohol on the driver's breath.", "I don't have my breathalyzer with me, though.", "Could you breathalyze the driver for me?", "If they're over the limit, arrest them." } );
                                        break;


                                    }
                                }
                                else
                                {
                                    Game.DisplayHelp("Go and discuss the situation with your fellow officer.");
                                }
                            }

                        }
                        else
                        {
                            PoliceOfficer.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), 0, true);
                            PoliceOfficer.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_e", 2f, 0).WaitForCompletion(7000);
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            GameFiber.Wait(1500);

                            
                            PoliceOfficer.Tasks.Clear();
                            PoliceOfficer.Inventory.GiveNewWeapon("WEAPON_PISTOL", -1, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            //NativeFunction.Natives.TASK_SHOOT_AT_COORD(PoliceOfficer, SuspectCar.Wheels[0].LastContactPoint.X, SuspectCar.Wheels[0].LastContactPoint.Y, SuspectCar.Wheels[0].LastContactPoint.Z, 1500, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                            GameFiber.Wait(1500);
                            PoliceCar.IsSirenOn = true;
                            PoliceCar.IsSirenSilent = false;
                            Chasing = true;

                        }

                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 3f);
                            if (!Suspect.Exists())
                            {
                                msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ We are ~r~CODE 4~s~, over.";
                                break;
                            }
                            else if (Functions.IsPedArrested(Suspect))
                            {
                                msg = "Control, the ~r~suspect~s~ is ~g~under arrest. ~s~We are ~g~CODE 4~s~, over.";
                                break;
                            }
                            else if (Suspect.IsDead)
                            {
                                msg = "Control, the ~r~suspect~s~ is ~o~dead. ~s~We are ~o~CODE 4~s~, over.";
                                //PedVector3HeadingTupleList.Add(new Ped(new Vector3(0,0,0)), new Vector3(), 0f);
                                break;
                            }





                            if (Chasing && !APICopSet)
                            {
                                if (PoliceOfficer.IsInVehicle(PoliceCar, false))
                                {
                                    Functions.SetPedAsCop(PoliceOfficer);
                                    Functions.AddCopToPursuit(Pursuit, PoliceOfficer);
                                    APICopSet = true;


                                }
                                else
                                {
                                    if (Vector3.Distance(PoliceCar.Position, PoliceOfficer.Position) < 3.5f)
                                    {
                                        PoliceOfficer.Tasks.EnterVehicle(PoliceCar, -1).WaitForCompletion(1000);
                                    }
                                    else
                                    {
                                        PoliceOfficer.Tasks.FollowNavigationMeshToPosition(PoliceCar.Position, PoliceCar.Heading, 1.6f).WaitForCompletion(300);
                                    }

                                }

                            }

                        }
                        
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        private List<List<string>> AlcoholSpeeches = new List<List<string>>()
        {
            new List<string>() {"Howdy!", "This person here was all over the road!", "I spotted him and pulled him over.", "I'm going to see what the deal is.", "Please hang around for a minute." },
            new List<string>() {"Hey officer!", "This person here was swerving in and out of the lane.", "Their driving was pretty erratic, too.", "I'm going to check them out.", "Please hang around for a bit." },
        };

        private List<List<string>> TicketSpeeches = new List<List<string>>()
        {
            new List<string>() {"Hey officer! Having a good day?", "The vehicle I've pulled over was speeding.", "I'm going to write out a ticket for that offence.", "Please act as backup while I do so." },
            new List<string>() {"How are you today?", "This stretch of road is notorious for its many collisions.", "The driver of that vehicle was using their mobile phone.", "Please watch my back while I write out a ticket." },
            
            new List<string>() {"Hey, how's it going?", "The driver of that vehicle cut someone off back there.", "Their driving was pretty erratic and anti-social.", "I'm going to slap them with a ticket.", "Hang around for a moment, please." },
            new List<string>() {"Good day!", "The vehicle I've just pulled over has no insurance!", "They're going to be getting a ticket from me.", "Can you please stay around in case they kick off?" },

            new List<string>() {"How nice to see you again, officer!", "Do you understand why people don't wear seat belts?", "The driver of that vehicle wasn't wearing theirs!", "Hang around while I write a ticket, will you?" },

        };










        private List<List<string>> ArrestWarrantSpeeches = new List<List<string>>()
        {
            new List<string>() { "Hey! Glad you're here!", "There's an outstanding warrant for the registered owner.", "This person needs to be arrested. Who's going in for the arrest?" },
            new List<string>() { "That was quick! What a response time!", "The vehicle I've stopped just ran a red light.", "Also, the registered owner comes back as wanted.", "I thought you might need another arrest for your quota!" },
            new List<string>() { "What took you so long?", "I guess you had to finish your doughnut.", "Anyway, this vehicle came up on my ANPR system.", "The registered owner has an outstanding warrant!", "Who's going in guns drawn? You or I?" },
            new List<string>() { "Hey, thanks for coming along!", "I felt like I needed backup on this one.", "This vehicle came up on a fixed ANPR camera.", "Apparently, the registered owner is wanted.", "Do you want the arrest to be yours?" },
            new List<string>() { "Do you see that vehicle in front of me?", "The registered owner is wanted, I've just checked.", "The complication is they may have a firearm.", "Who's going in for the arrest on this person?" },
            new List<string>() {"Good day!", "Thanks for coming yet again.", "The owner of that vehicle has an outstanding warrant.", "They also have markers for violence against police.", "Someone needs to go in for the arrest.", "Who's it going to be?" }
        };
        private List<List<string>> ArrestWarrantAnswers = new List<List<string>>()
        {
            new List<string>() { "This felon's mine, I'll take the arrest!", "You can be the primary officer for the arrest, I'll back you up!" },
            new List<string>() { "I'm slightly behind on my arrest quota. This one's mine.", "My last doughnut didn't go down too well - they're yours." },
            new List<string>() { "I like that adrenaline rush. I'll go in for the arrest!", "Why would I put my life on the line? You can go!" },
            new List<string>() { "I'll go in for the arrest! Make sure you back me up!", "Nothing safer than me backing you up. All yours!" },
            new List<string>() {"They're mine! I eat criminals for breakfast!", "Have at it, I'll back you up!" }
           
        };
        //private TupleList<Ped, Vector3, float> PedVector3HeadingTupleList = new TupleList<Ped, Vector3, float>
        //{
        //    { new Ped(new Vector3()), new Vector3(0,0,0), 0f },
        //    { new Ped(new Vector3()), new Vector3(0,0,0), 0f },
        //    //etc
        //};
        

        
    }
}
