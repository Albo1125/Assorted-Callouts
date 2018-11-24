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
    [CalloutInfo("HotPursuit", CalloutProbability.Medium)]
    internal class HotPursuit : AssortedCallout
    {
        private bool CalloutRunning = false;
        private SuspectStates SuspectState;
        private SuspectStates PassengerState;
        private string msg;
        private Ped Passenger;
        private ModelWithName SelectedModelInfo;
        private static List<Model> _CarsToSelectFrom = new List<Model> {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR",
            "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };
        private static List<Model> _MotorBikesToSelectFrom = new List<Model> { "BATI", "BATI2", "AKUMA", "BAGGER", "DOUBLE", "NEMESIS", "HEXER" };
        private class ModelWithName
        {
            public Model ChosenModel;

            private Model[] models;
            public string Name;
            public ModelWithName(Model[] models, string Name)
            {
                this.models = models;
                this.Name = Name;
                ChosenModel = models[AssortedCalloutsHandler.rnd.Next(models.Length)];
            }
        }
        private List<ModelWithName> ModelsWithDisplayNames = new List<ModelWithName>();

        private void InitialiseModelsWithDisplayNames()
        {
            
           
            ModelsWithDisplayNames = new List<ModelWithName>() {
            new ModelWithName( new Model[] { "FIRETRUK" }, "a fire truck"),
            new ModelWithName( new Model[]{"AMBULANCE" }, "an ambulance" ),
            new ModelWithName(_MotorBikesToSelectFrom.ToArray(), "a motorbike" ),
            new ModelWithName(_CarsToSelectFrom.ToArray(), "a car"),
            new ModelWithName( new Model[] { "CADDY", "CADDY2" }, "a golf cart" ),
            new ModelWithName( new Model[] { "BUS", "RENTALBUS", "TOURBUS", "PBUS", "COACH" }, "a bus" ),
            new ModelWithName( new Model[] { "TRASH", "TRASH2" }, "a garbage truck" ),
            new ModelWithName( new Model[] { "TRACTOR", "TRACTOR2" }, "a tractor" ),
            new ModelWithName( new Model[] { "TAXI" }, "a taxi" ),
            new ModelWithName( new Model[] { "KURUMA2" , "DUKES2" }, "an armoured car" ),
            new ModelWithName( new Model[] { "MARSHALL" }, "a national offroader" ),
            new ModelWithName( new Model[] { "TANKER", "TANKER2", "ARMYTANKER" }, "a tanker" ),
            new ModelWithName( new Model[] { "BARRACKS", "BARRACKS2","BARRACKS3", "CRUSADER"}, "a military vehicle" ),
            new ModelWithName( new Model[] { "RHINO"}, "a military tank" ),
           };
            
        }
            

        
        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("AssortedCallouts.HotPursuit");
            int WaitCount = 0;

            while (true)
            {
                if (World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(225f, 320f)).GetClosestVehicleNodeWithHeading(out SpawnPoint, out SpawnHeading))
                {
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) > 180f)
                    {
                        break;
                    }
                }
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }
          
            InitialiseModelsWithDisplayNames();
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 70f);
            SelectedModelInfo = ModelsWithDisplayNames[AssortedCalloutsHandler.rnd.Next(ModelsWithDisplayNames.Count)];
         
            SelectedModelInfo.ChosenModel.LoadAndWait();
            
            CalloutMessage = "Hot Pursuit of " + SelectedModelInfo.Name;
            CalloutPosition = SpawnPoint;


            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE_01 CRIME_RESIST_ARREST IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            if (SelectedModelInfo.ChosenModel.IsTrailer)
            {
                SuspectCar = new Vehicle("PHANTOM", SpawnPoint, SpawnHeading);
                SuspectCar.Trailer = new Vehicle(SelectedModelInfo.ChosenModel, SpawnPoint, SpawnHeading);
            }
            else
            {
                SuspectCar = new Vehicle(SelectedModelInfo.ChosenModel, SpawnPoint, SpawnHeading);
            }
            SuspectCar.IsPersistent = true;
            SuspectCar.IsEngineOn = true;
            Suspect = new Ped(Vector3.Zero);
            Suspect.MakeMissionPed();
            Suspect.WarpIntoVehicle(SuspectCar, -1);
            NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED( Suspect.CurrentVehicle, 25f);
            Suspect.Tasks.CruiseWithVehicle(60f, VehicleDrivingFlags.Emergency);
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

                    
                    Pursuit = Functions.CreatePursuit();

                    NativeFunction.CallByName<uint>("SET_DRIVER_ABILITY", Suspect, 1.0f);
                    NativeFunction.CallByName<uint>("SET_DRIVER_AGGRESSIVENESS", Suspect, 1.0f);

                    Functions.AddPedToPursuit(Pursuit, Suspect);

                    if (AssortedCalloutsHandler.rnd.Next(3) == 0)
                    {
                        Passenger = new Ped(Vector3.Zero);
                        Passenger.MakeMissionPed();

                        Passenger.WarpIntoVehicle(SuspectCar, 0);
                        Functions.AddPedToPursuit(Pursuit, Passenger);
                    }
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                    GameFiber.Wait(3000);
                    Vehicle Backupveh = Functions.RequestBackup(Suspect.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
                    Backupveh.Position = SpawnPoint;
                    Backupveh.Heading = SpawnHeading;
                    NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED( Backupveh, 10f);
                    Game.DisplayNotification("~b~Pursuing Officer: ~s~Suspect is on ~b~" + World.GetStreetName(Suspect.Position) + ". ~s~Speed is ~r~" + Math.Round(MathHelper.ConvertMetersPerSecondToMilesPerHour(Suspect.Speed)).ToString() + " MPH.");
                    SuspectBlip = Suspect.AttachBlip();
                    SuspectBlip.Scale = 0.1f;
                    SuspectBlip.IsRouteEnabled = true;
                    SuspectBlip.RouteColor = Color.Red;
                    if (SuspectCar.HasSiren)
                    {
                        SuspectCar.IsSirenOn = true;
                    }
                    if (!Passenger.Exists())
                    {
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();

                            
                            if (!Suspect.Exists())
                            {
                                msg = "Control, the ~r~suspect~s~ has ~r~escaped.~s~ Hot pursuit is code 4, over.";
                                break;
                            }
                            else if (Functions.IsPedArrested(Suspect))
                            {
                                msg = "Control, the ~r~suspect~s~ is ~g~under arrest.~s~ Hot pursuit is code 4, over.";
                                break;
                            }
                            else if (Suspect.IsDead)
                            {
                                msg = "Control, the ~r~suspect~s~ is ~o~dead.~s~ Hot pursuit is code 4, over.";
                                break;
                            }

                            if (Vector3.Distance(Suspect.Position, Game.LocalPlayer.Character.Position) < 60f)
                            {
                                if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                            }
                        }
                    }
                    else if (CalloutRunning)
                    {
                        Functions.AddPedToPursuit(Pursuit, Passenger);
                        PassengerState = SuspectStates.InPursuit;
                        SuspectState = SuspectStates.InPursuit;

                        while (CalloutRunning)
                        {
                            GameFiber.Yield();

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
                            if ((SuspectState != SuspectStates.InPursuit) && (PassengerState != SuspectStates.InPursuit))
                            {
                                break;
                            }

                            if (Vector3.Distance(Suspect.Position, Game.LocalPlayer.Character.Position) < 60f)
                            {
                                if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                            }
                        }
                        msg = "Control, the driver's ";
                        if (SuspectState == SuspectStates.Arrested)
                        {
                            msg += "~g~under arrest.";
                        }
                        else if (SuspectState == SuspectStates.Dead)
                        {
                            msg += "~o~dead.";
                        }
                        else if (SuspectState == SuspectStates.Escaped)
                        {
                            msg += "~r~escaped.";
                        }

                        msg += "~s~ The passenger's ";
                        if (PassengerState == SuspectStates.Arrested)
                        {
                            msg += "~g~under arrest.";
                        }
                        else if (PassengerState == SuspectStates.Dead)
                        {
                            msg += "~o~dead.";
                        }
                        else if (PassengerState == SuspectStates.Escaped)
                        {
                            msg += "~r~escaped.";
                        }
                        msg += "~s~ Hot pursuit is code 4, over.";
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
                        Game.LogTrivial("British Policing Script handled the exception successfully.");
                        Game.DisplayNotification("~O~Failtostop~s~ callout crashed, sorry. Please send me your log file.");
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

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }
        public override void End()
        {

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

    }
}
