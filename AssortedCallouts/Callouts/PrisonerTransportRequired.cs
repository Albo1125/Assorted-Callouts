using Albo1125.Common.CommonLibrary;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using System.Threading.Tasks;
using System.Drawing;

namespace AssortedCallouts.Callouts
{
    [CalloutInfo("Prisoner Transport Required", CalloutProbability.Medium)]
    internal class PrisonerTransportRequired : AssortedCallout
    {
        private bool CalloutRunning = false;
        private Ped PoliceOfficer;
        private Vehicle PoliceCar;
        private Blip PoliceOfficerBlip;
        private string[] CityCarModels = new string[] { "POLICE", "POLICE2", "POLICE3", "POLICE4" };
        private string[] CountrysideCarModels = new string[] { "SHERIFF", "SHERIFF2" };
        private TupleList<Vector3, float> ValidTrafficStopSpawnPointsWithHeadings = new TupleList<Vector3, float>();
        private Tuple<Vector3, float> ChosenSpawnData;
        private Model CopCarModel;
        private string msg;

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("AssortedCallouts.PrisonerTransportRequired");
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
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 10f);
            CalloutMessage = "Prisoner Transport Required";

            CalloutPosition = SpawnPoint;

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Prisoner Transport Required", "Prisoner Transport", SpawnPoint, 0, "Officer is requesting transport for a prisoner. Situation currently under control. Please respond code 2.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_OFFICER_IN_NEED_OF_ASSISTANCE IN_OR_ON_POSITION", SpawnPoint);


            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            PoliceCar = new Vehicle(CopCarModel, SpawnPoint, SpawnHeading);
            PoliceCar.RandomiseLicencePlate();
            PoliceCar.MakePersistent();
            PoliceCar.IsSirenOn = true;
            PoliceCar.IsSirenSilent = true;
            if (AssortedCalloutsHandler.LightsOffForELSCars &&
                            PoliceCar.IsSirenOn &&
                            PoliceCar.VehicleModelIsELS())
            {
                PoliceCar.IsSirenOn = false;
            }
            PoliceOfficer = PoliceCar.CreateRandomDriver();
            PoliceOfficer.MakeMissionPed();
            PoliceOfficerBlip = PoliceOfficer.AttachBlip();
            PoliceOfficerBlip.Color = Color.Green;
            PoliceOfficerBlip.IsRouteEnabled = true;
            PoliceOfficer.RelationshipGroup = "COP";
            
            SuspectCar = new Vehicle(GroundVehiclesToSelectFrom[AssortedCalloutsHandler.rnd.Next(GroundVehiclesToSelectFrom.Length)], PoliceCar.GetOffsetPosition(Vector3.RelativeFront * 9f), PoliceCar.Heading);
            SuspectCar.RandomiseLicencePlate();
            SuspectCar.MakePersistent();
            Suspect = SuspectCar.CreateRandomDriver();
            Suspect.MakeMissionPed();

            Suspect.WarpIntoVehicle(PoliceCar, PoliceCar.PassengerCapacity - 1);
            Suspect.Tasks.PlayAnimation("mp_arresting", "idle", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
            Suspect.RelationshipGroup = "TBACKUPCRIMINAL";
            MainLogic();
            return base.OnCalloutAccepted();
        }
        private void DispatchResponse()
        {
            GameFiber.Wait(5000);
            Functions.PlayScannerAudio("COPY_THAT_MOVING_RIGHT_NOW REPORT_RESPONSE_COPY");
            Game.DisplayNotification("~b~Control: ~s~Please respond to the officer requesting prisoner transport.");
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

        private void MainLogic()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    DispatchResponse();
                    WaitForGetClose();
                    WaitForParkAndGetNearby();
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        PoliceCar.ShouldVehiclesYieldToThisVehicle = false;
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, PoliceOfficer.Position) < 4f)
                        {
                            Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                            {
                                SpeechHandler.HandleSpeech("Officer", ArrestWarrantSpeeches[AssortedCalloutsHandler.rnd.Next(ArrestWarrantSpeeches.Count)]);
                                PoliceOfficer.Tasks.EnterVehicle(PoliceCar, 5000, -1);
                                break;
                            }
                        }
                    }
                    if (CalloutRunning)
                    {
                        if (Suspect.IsInAnyVehicle(false))
                            if (!Suspect.IsInVehicle(Game.LocalPlayer.Character.LastVehicle, false))
                            {
                                Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(3000);
                            }

                    }

                    if (Suspect.IsInAnyVehicle(false))
                    {
                        if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                        if (PoliceOfficerBlip.Exists()) { PoliceOfficerBlip.Delete(); }
                    }
                    
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Suspect.Exists())
                        {
                            if (Functions.IsPedInPrison(Suspect))
                            {
                                break;
                            }
                        }
                        else
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
                        Game.DisplayNotification("~O~Traffic Stop Backup~s~callout crashed, sorry. Please send me your log file.");
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
                GameFiber.Sleep(10000);
                msg = "~b~Prisoner has been transported to jail. ~s~Prisoner transport call is ~g~code 4, over.";
                Game.DisplayNotification(msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }
        private List<List<string>> ArrestWarrantSpeeches = new List<List<string>>()
        {
            new List<string>() { "Hey! Glad you're here!", "The person I just arrested has an outstanding bench warrant.", "Thanks for taking them in for me. Much appreciated." },
            new List<string>() { "That was quick! What a response time!", "The vehicle I've stopped just ran a red light.", "Also, the driver had an outstanding felony warrant.", "Take them in for me, will ya?" },
            new List<string>() { "What took you so long?", "I guess you had to finish your doughnut.", "Anyway, this vehicle came up on my ANPR system.", "The driver was wanted for felony offences.", "Take them off to jail for me then, thanks." },
            new List<string>() { "Hey, thanks for coming along!", "This one's under arrest for DUI.", "Take them into custody for me, will ya?" },
            
        };














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
            Rage.Native.NativeFunction.Natives.RESET_AI_MELEE_WEAPON_DAMAGE_MODIFIER();
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
            base.End();
            
            if (PoliceOfficerBlip.Exists()) { PoliceOfficerBlip.Delete(); }
            
            if (CalloutFinished)
            {
                if (PoliceOfficer.Exists()) { PoliceOfficer.Dismiss(); }
                if (PoliceCar.Exists()) { PoliceCar.Dismiss(); }
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
    }
}
