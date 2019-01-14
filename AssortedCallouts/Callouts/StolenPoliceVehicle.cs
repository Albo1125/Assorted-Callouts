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
    [CalloutInfo("Stolen Police Vehicle", CalloutProbability.Medium)]
    class StolenPoliceVehicle : AssortedCallout
    {
        
        private SuspectStates SuspectState;
        private SuspectStates PassengerState; 
        private bool CalloutRunning = false;
        private string msg;
        private Ped Passenger;
        private string[] CityCarModels = new string[] { "POLICE", "POLICE2", "POLICE3", "POLICE4" };
        private string[] CountrysideCarModels = new string[] { "SHERIFF", "SHERIFF2" };
        private Model CopCarModel;
        private string[] firearmsToSelectFrom = new string[] { "WEAPON_PISTOL", "WEAPON_APPISTOL",  "WEAPON_MICROSMG", "WEAPON_SMG",  "WEAPON_ASSAULTRIFLE"
                                                                , "WEAPON_ADVANCEDRIFLE", "WEAPON_PISTOL50", "WEAPON_ASSAULTSMG" };

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(220f));
            while (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) < 180f)
            {
                GameFiber.Yield();
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(220f));
            }
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
            CalloutMessage = "Stolen Police Vehicle";

            CalloutPosition = SpawnPoint;

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Stolen Police Vehicle", "Stolen Police Vehicle", SpawnPoint, 1, "Reports of a stolen police vehicle. Tracking with GPS tracker. Please respond.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_STOLEN_POLICE_VEHICLE IN_OR_ON_POSITION", SpawnPoint);


            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            SuspectCar = new Vehicle(CopCarModel, SpawnPoint);
            SuspectCar.RandomiseLicencePlate();
            SuspectCar.MakePersistent();
            Suspect = new Ped(Vector3.Zero);
            Suspect.MakeMissionPed();
            SuspectBlip = Suspect.AttachBlip();
            SuspectBlip.Color = Color.DarkRed;
            
            //Suspect.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[EntryPoint.rnd.Next(firearmsToSelectFrom.Length)]), -1, true);
            Suspect.WarpIntoVehicle(SuspectCar, -1);
            Game.DisplayNotification("~b~Intercept~s~ the ~r~stolen police vehicle.");
            if (!CalloutRunning)
            {
                CalloutHandler();
            }
            return base.OnCalloutAccepted();
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
            if (CalloutFinished)
            {
                if (Passenger.Exists()) { Passenger.Dismiss(); }
            }
            else
            {
                if (Passenger.Exists()) { Passenger.Delete(); }
            }
            base.End();
        }
        public override void Process()
        {
            
            base.Process();
            if (Game.LocalPlayer.Character.IsDead)
            {

                GameFiber.StartNew(End);
            }
        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    GameFiber.Yield();
                    Suspect.Model.LoadAndWait();
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        Passenger = new Ped(Vector3.Zero);
                        Passenger.MakeMissionPed();
                        
                        Passenger.WarpIntoVehicle(SuspectCar, 0);
                        if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                        {
                            Passenger.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_MICROSMG"), -1, true);
                            Game.DisplayNotification("~b~Control: ~s~There's intel linking this theft to ~r~dangerous, organised criminals.");
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "There's intel linking this theft to ~r~dangerous, organised criminals.");
                            }
                        }
                    }
                    Suspect.Tasks.CruiseWithVehicle(SuspectCar, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Suspect.Position, Game.LocalPlayer.Character.Position) < 14f)
                        {
                            if (Math.Abs(Suspect.Position.Z - Game.LocalPlayer.Character.Position.Z) < 1.2f)
                            {
                                GameFiber.Wait(1200);
                                break;
                            }
                        }
                    }
                    if (CalloutRunning)
                    {
                        if (ComputerPlusRunning)
                        {
                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicle is fleeing. In pursuit");
                            API.ComputerPlusFuncs.AddVehicleToCallout(CalloutID, SuspectCar);
                        }
                        if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                        SuspectCar.IsSirenOn = true;
                        SuspectCar.IsSirenSilent = false;
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        //Functions.SetPursuitDisableAI(Pursuit, true);
                        //Suspect.Tasks.CruiseWithVehicle(SuspectCar, 50f, VehicleDrivingFlags.Emergency);
                        GameFiber.Wait(3000);
                        //Functions.SetPursuitDisableAI(Pursuit, false);

                        //Functions.SetPursuitDisableAI(Pursuit, false);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                    }
                    if (!Passenger.Exists())
                    {
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 5.5f);
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
                        }
                    }
                    else if (CalloutRunning)
                    {
                        Functions.AddPedToPursuit(Pursuit, Passenger);
                        PassengerState = SuspectStates.InPursuit;
                        SuspectState = SuspectStates.InPursuit;
                        bool passengershooting = false;
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 5.5f);
                            if (SuspectState == SuspectStates.InPursuit)
                            {
                                if (!Suspect.Exists())
                                {
                                    SuspectState = SuspectStates.Escaped;
                                }
                                else if (Suspect.IsDead)
                                {
                                    SuspectState = SuspectStates.Dead;
                                }
                                else if (Functions.IsPedArrested(Suspect))
                                {
                                    SuspectState = SuspectStates.Arrested;
                                }
                            }



                            if (PassengerState == SuspectStates.InPursuit)
                            {
                                if (!Passenger.Exists())
                                {
                                    PassengerState = SuspectStates.Escaped;
                                }
                                else if (Passenger.IsDead)
                                {
                                    PassengerState = SuspectStates.Dead;
                                }
                                else if (Functions.IsPedArrested(Passenger))
                                {
                                    PassengerState = SuspectStates.Arrested;
                                }
                                

                                
                            }

                            if (Game.LocalPlayer.Character.IsDead)
                            {
                                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                                break;
                            }
                            if ((SuspectState != SuspectStates.InPursuit) && (PassengerState != SuspectStates.InPursuit))
                            {
                                break;
                            }
                            if (PassengerState == SuspectStates.InPursuit)
                            {
                                if (Passenger.IsInVehicle(SuspectCar, false))
                                {


                                    if (!passengershooting)
                                    {
                                        if (Vector3.Distance(Passenger.Position, Game.LocalPlayer.Character.Position) < 15f)
                                        {

                                            passengershooting = true;
                                            NativeFunction.Natives.TASK_DRIVE_BY( Passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                        }
                                    }
                                    else
                                    {
                                        if (Vector3.Distance(Passenger.Position, Game.LocalPlayer.Character.Position) > 25f)
                                        {
                                            Passenger.Tasks.ClearSecondary();
                                            
                                            passengershooting = false;
                                        }
                                    }
                                }
                            }
                            
                        }
                        msg = "Control, the driver ";
                        if (SuspectState == SuspectStates.Arrested)  
                        {
                            msg += "is ~g~under arrest.";
                        }
                        else if (SuspectState == SuspectStates.Dead)
                        {
                            msg += "is ~o~dead.";
                        }
                        else if (SuspectState == SuspectStates.Escaped)
                        {
                            msg += "has ~r~escaped.";
                        }

                        msg += "~s~ The passenger ";
                        if (PassengerState == SuspectStates.Arrested)
                        {
                            msg += "is ~g~under arrest.";
                        }
                        else if (PassengerState == SuspectStates.Dead)
                        {
                            msg += "is ~o~dead.";
                        }
                        else if (PassengerState == SuspectStates.Escaped)
                        {
                            msg += "has ~r~escaped.";
                        }
                        msg += "~s~ We are ~g~CODE 4, ~s~over.";
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
                        Game.DisplayNotification("~O~Stolen Police Vehicle~s~callout crashed, sorry. Please send me your log file.");
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
                GameFiber.Sleep(4000);
                Game.DisplayNotification(msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }

    }

}
