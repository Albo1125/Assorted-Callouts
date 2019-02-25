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
using Albo1125.Common.CommonLibrary;
using Rage.Native;

namespace AssortedCallouts.Callouts
{
    [CalloutInfo("Petrol Theft", CalloutProbability.Medium)]
    internal class PetrolTheft : AssortedCallout
    {
        private Vector3[] PetrolStations = new Vector3[] {new Vector3(-711.9525f, -921.0374f, 18.60401f), new Vector3(-528.0559f, -1218.164f, 17.85979f), new Vector3(818.0175f, -1037.259f, 26.10594f),
                                            new Vector3 (-2079.705f, -319.7511f, 12.74332f), new Vector3(53.59156f, 2784.886f, 57.60809f), new Vector3(-90.44811f, 6416.361f, 31.05175f), new Vector3(2565.666f,384.0004f,108.4633f) };
        private Ped Shopkeeper;
        private Blip ShopkeeperBlip;
        private List<string> ShopkeeperLines;
       
        private bool CalloutRunning = false;
        private string CarModelName;
        private bool ShowCCTV;
        private bool ShopkeeperKnowsVehicleDetails;
        private List<string> OfficerLines;
        private string CarColor;
        private string msg;
        private string PetrolGas;
        

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.PetrolTheft");
            SpawnPoint = new Vector3(0,0,3000);
            foreach (Vector3 pos in PetrolStations)
            {
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, pos) < Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint))
                {
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, pos) > 90f)
                    {
                        SpawnPoint = pos;
                    }
                }
            }
            if (SpawnPoint == new Vector3(0,0,3000))
            {
                Game.LogTrivial("Nullable vector");
                return false;
            }
            if (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) > 2000f)
            {
                Game.LogTrivial("Petrol station too far away.");
                return false;
            }
            if (AssortedCalloutsHandler.English == AssortedCalloutsHandler.EnglishTypes.BritishEnglish)
            {
                PetrolGas = "Petrol";
            }
            else
            {
                PetrolGas = "Gas";
            }
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            CalloutMessage = PetrolGas + " Theft";
            CalloutPosition = SpawnPoint;
            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout(CalloutMessage, CalloutMessage, SpawnPoint, 0, "Reports of a " + CalloutMessage + ". Please investigate with the shopkeeper.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " CITIZENS_REPORT CRIME_484 IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            
            Shopkeeper = new Ped("mp_m_shopkeep_01", SpawnPoint, 0.0f);
            Shopkeeper.IsPersistent = true;
            Shopkeeper.BlockPermanentEvents = true;
            Vector3 CarSpawn = Game.LocalPlayer.Character.Position.Around(250f).GetClosestMajorVehicleNode();
            while (Vector3.Distance(CarSpawn, Game.LocalPlayer.Character.Position) < 170f)
            {
                GameFiber.Yield();
                CarSpawn = Game.LocalPlayer.Character.Position.Around(250f).GetClosestMajorVehicleNode();
            }


            SuspectCar = new Vehicle(GroundVehiclesToSelectFrom[AssortedCalloutsHandler.rnd.Next(GroundVehiclesToSelectFrom.Length)], CarSpawn);
            SuspectCar.RandomiseLicencePlate();
            SuspectCar.IsPersistent = true;
            
            //GameFiber.Yield();
            Suspect = SuspectCar.CreateRandomDriver();
            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;

            ShopkeeperBlip = Shopkeeper.AttachBlip();
            ShopkeeperBlip.IsRouteEnabled = true;

            CarModelName = SuspectCar.Model.Name.ToLower();
            CarModelName = char.ToUpper(CarModelName[0]) + CarModelName.Substring(1);
            try {
                CarColor = SuspectCar.GetColors().PrimaryColorName + "~s~-coloured";
            }
            catch(Exception e)
            {
                CarColor = "weirdly-coloured";
            }
            if (!CalloutRunning) { CalloutHandler(); }
            return base.OnCalloutAccepted();
        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try {
                    //Responding...
                    
                    Suspect.Tasks.CruiseWithVehicle(SuspectCar, 17f, VehicleDrivingFlags.Normal);
                    Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Assorted Callouts", PetrolGas + " Theft", "~b~Control: ~s~We have reports of a ~r~" + PetrolGas.ToLower() + " theft. ~s~Please respond ~b~CODE 2~s~.");
                    Functions.PlayScannerAudio("REPORT_RESPONSE_COPY UNITS_RESPOND_CODE_02_02");
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(Suspect, 786603);
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Shopkeeper.Position) < 15f)
                        {
                            ShopkeeperBlip.IsRouteEnabled = false;
                            Vector3 directionFromShopkeeperToCar = (Game.LocalPlayer.Character.Position - Shopkeeper.Position);
                            directionFromShopkeeperToCar.Normalize();
                            Shopkeeper.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromShopkeeperToCar)).WaitForCompletion(1100);
                            Shopkeeper.Tasks.PlayAnimation("friends@frj@ig_1", "wave_a", 1.1f, AnimationFlags.Loop);
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
                                }
                                break;
                            }
                            
                            
                        }
                    }

                    //On scene
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Shopkeeper.Position) < 6f)
                        {
                            Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                            {
                                Shopkeeper.Tasks.ClearImmediately();
                                
                                break;
                            }
                        }
                        else
                        {
                            Game.HideHelp();
                        }
                    }
                    ShopkeeperBlip.Delete();
                    //CCTV and Dialogue
                    DetermineShopkeeperLines();
                    SpeechHandler.HandleSpeech("Shopkeeper", ShopkeeperLines, Shopkeeper);
                    if (ShowCCTV)
                    {
                        GameFiber.Sleep(2000);
                        WatchCameraFootage();
                    }
                    ShopkeeperLines.Clear();
                    if (ShopkeeperKnowsVehicleDetails)
                    {
                        ShopkeeperLines.Add("The vehicle was a ~b~" + CarColor + " ~b~" + CarModelName + " ~s~.");
                        ShopkeeperLines.Add("The licence plate was ~b~" + SuspectCar.LicensePlate + ".");
                        ShopkeeperLines.Add("They took off in that direction!");
                        SpeechHandler.HandleSpeech("Shopkeeper", ShopkeeperLines, Shopkeeper);
                    }
                    else
                    {
                        OfficerLines = new List<string>() { "I can make out the vehicle's details from the CCTV footage!", "The vehicle was a ~b~" + CarColor + " ~b~" + CarModelName + ".", "The licence plate was ~b~" + SuspectCar.LicensePlate + ".", "In which direction did they go?" };
                        SpeechHandler.HandleSpeech("You", OfficerLines, Game.LocalPlayer.Character);
                        ShopkeeperLines.Clear();
                        ShopkeeperLines.Add("They made off in that direction, officer!");
                        SpeechHandler.HandleSpeech("Shopkeeper", ShopkeeperLines, Shopkeeper);
                    }


                    GameFiber.Yield();
                    SuspectCar.Position = Game.LocalPlayer.Character.Position.Around(250f).GetClosestMajorVehicleNode();
                    while (Vector3.Distance(SuspectCar.Position, Game.LocalPlayer.Character.Position) < 170f)
                    {
                        GameFiber.Yield();
                        SuspectCar.Position = Game.LocalPlayer.Character.Position.Around(250f).GetClosestMajorVehicleNode();
                    }

                    
                    Vector3 directionFromShopkeeperToCar1 = (SuspectCar.Position - Shopkeeper.Position);
                    directionFromShopkeeperToCar1.Normalize();
                    Shopkeeper.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromShopkeeperToCar1)).WaitForCompletion(1200);
                    Shopkeeper.Tasks.PlayAnimation("gestures@f@standing@casual", "gesture_point", 0.8f, AnimationFlags.Loop);
                    GameFiber.Sleep(3000);
                    Game.DisplayNotification("Control, ~r~suspect's vehicle~s~ is a ~b~" + CarColor + " ~b~" + CarModelName + ".");
                    GameFiber.Sleep(2000);
                    Game.DisplayNotification("The plate is ~b~" + SuspectCar.LicensePlate + ". ~s~~n~Please pass on the details, I'm ~b~checking the area~s~, over.");
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspect's vehicle is a " + CarColor + " " + CarModelName + ".");
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "The plate is " + SuspectCar.LicensePlate + ". Checking the area.");
                    }
                    //Searching...
                    HandleSearchForVehicleWithANPR();
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicle located. Engaging traffic stop.");
                        
                    }
                    //float Radius = 140f;
                    //bool RouteEnabled = false;
                    //SearchArea = new Blip(SuspectCar.Position.Around(35f), Radius);
                    //SearchArea.Color = System.Drawing.Color.Yellow;
                    //SearchArea.Alpha = 0.5f;
                    //Shopkeeper.Tasks.ClearImmediately();


                    //Suspect.Tasks.CruiseWithVehicle(SuspectCar, 18f, VehicleDrivingFlags.Normal);
                    //int WaitCount = 0;
                    //int WaitCountTarget = 3100;
                    //while (CalloutRunning)
                    //{
                    //    GameFiber.Yield();
                    //    WaitCount++;
                    //    Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);

                    //    if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 9f), SuspectCar.Position) < 9f)
                    //    {
                    //        GameFiber.Sleep(2000);
                    //        if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 9f), SuspectCar.Position) < 9f)
                    //        {
                    //            Game.DisplayNotification("Control, I have located the ~b~" + CarModelName + "~s~ from the ~r~" + PetrolGas.ToLower() + " theft.");
                    //            Game.DisplayNotification("I'm preparing to ~b~stop them,~s~ over.");
                    //            SuspectBlip = Suspect.AttachBlip();
                    //            if (SearchArea.Exists()) { SearchArea.Delete(); }
                    //            Functions.PlayScannerAudio("DISPATCH_SUSPECT_LOCATED_ENGAGE REPORT_RESPONSE");

                    //            break;
                    //        }

                    //    }
                    //    else if (((Vector3.Distance(SuspectCar.Position, SearchArea.Position) > Radius + 230f) && (WaitCount > 1000)) || (WaitCount > WaitCountTarget))
                    //    {
                    //        Game.DisplayNotification("~b~Control: ~s~We have an ~o~ANPR Hit ~s~on ~b~" + SuspectCar.LicensePlate + ". ~g~Search area updated, ~s~over.");
                    //        Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION", SuspectCar.Position);
                    //        SearchArea.Delete();
                    //        Radius = 50f;
                    //        SearchArea = new Blip(SuspectCar.Position.Around(8f), Radius);
                    //        SearchArea.Color = System.Drawing.Color.Yellow;
                    //        SearchArea.Alpha = 0.5f;

                    //        RouteEnabled = false;
                    //        if (WaitCount > WaitCountTarget) { Game.LogTrivial("Updated for waitcount"); }
                    //        WaitCount = 0;
                    //        Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait).WaitForCompletion(1000);
                    //        Suspect.Tasks.CruiseWithVehicle(SuspectCar, 17f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                    //        WaitCountTarget -= EntryPoint.rnd.Next(200,500);
                    //        if (WaitCountTarget < 1900) { WaitCountTarget = 1900; }

                    //    }
                    //    if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) > Radius + 90f)
                    //    {
                    //        if (!RouteEnabled)
                    //        {
                    //            SearchArea.IsRouteEnabled = true;
                    //            RouteEnabled = true;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (RouteEnabled)
                    //        {
                    //            SearchArea.IsRouteEnabled = false;
                    //            RouteEnabled = false;
                    //        }
                    //    }
                    //}

                    //Found
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
                    if (Functions.IsPlayerPerformingPullover())
                    {
                        GameFiber.Wait(3000);
                    }
                    //How they react:
                
                    if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                    if ((AssortedCalloutsHandler.rnd.Next(11) <= 5) || (!Game.LocalPlayer.Character.IsInAnyVehicle(false)) || Functions.GetActivePursuit() != null)
                    {
                        if (CalloutRunning)
                        {
                            if (Functions.GetActivePursuit() != null) { Functions.ForceEndPursuit(Functions.GetActivePursuit()); }
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
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
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicle is fleeing. Engaging pursuit.");

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
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                GameFiber.Wait(1000);
                                if (AssortedCalloutsHandler.rnd.Next(5) == 0)
                                {
                                    Pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(Pursuit, Suspect);
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
                                    if (ComputerPlusRunning)
                                    {
                                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Vehicle is fleeing. Engaging pursuit.");

                                    }
                                }
                                break;

                            }
                        }
                        while (CalloutRunning)
                        {
                            GameFiber.Yield();
                            if (Suspect.Exists())
                            {
                                if (Functions.IsPedArrested(Suspect))
                                {
                                    break;
                                }
                                if (Suspect.IsDead)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //Done
                    if (Suspect.Exists())
                    {
                        if (Functions.IsPedArrested(Suspect))
                        {
                            msg = "Control, suspect is ~g~under arrest. ~s~The ~r~" + PetrolGas.ToLower() + " theft~s~ call is ~g~CODE 4~s~, over.";
                        }
                        else if (Suspect.IsDead)
                        {
                            msg = "Control, suspect is ~r~dead. ~s~The ~r~" + PetrolGas.ToLower() + " theft~s~ call is ~g~CODE 4~s~, over.";
                        }
                    }
                    else
                    {
                        msg = "Control, the suspects ~r~have escaped. ~s~The ~r~" + PetrolGas.ToLower() + " theft~s~ call is ~g~CODE 4~s~, over.";
                    }
                    if (CalloutRunning)
                    {
                        GameFiber.Sleep(4000);
                        Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Assorted Callouts", PetrolGas + " Theft", msg);

                        Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                        CalloutFinished = true;
                        End();
                    }
                    



                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    End();
                }
            });

        }


        private Vector3 OldLastVehiclePos;
        private DateTime OldDateTime;
        private void WatchCameraFootage()
        {

            CCTVShowing = true;
            Game.LocalPlayer.HasControl = false;
            Game.FadeScreenOut(1500, true);
            NativeFunction.Natives.SET_TIMECYCLE_MODIFIER("CAMERA_BW");
            if (Game.LocalPlayer.Character.LastVehicle.Exists())

            {
                OldLastVehiclePos = Game.LocalPlayer.Character.LastVehicle.Position;
                Game.LocalPlayer.Character.LastVehicle.IsVisible = false;
                Game.LocalPlayer.Character.LastVehicle.SetPositionZ(Game.LocalPlayer.Character.LastVehicle.Position.Z + 8f);
                Game.LocalPlayer.Character.LastVehicle.IsPositionFrozen = true;
            }
            bool DateTimeChanged = false;
            try
            {
                OldDateTime = World.DateTime;
                World.DateTime = DateTime.Now;
                //World.IsTimeOfDayFrozen = true;
                DateTimeChanged = true;
            }
            catch (Exception e) { }

            
            Game.LocalPlayer.Character.IsVisible = false;
            Shopkeeper.IsVisible = false;
            Vector3 suspectOldPosition = SuspectCar.Position;
            Rotator suspectOldRotator = SuspectCar.Rotation;
            Vector3 PlayerOldPos = Game.LocalPlayer.Character.Position;
            Vector3 ShopkeeperOldPos = Shopkeeper.Position;
            Game.LocalPlayer.Character.SetPositionZ(Game.LocalPlayer.Character.Position.Z + 8f);
            Game.LocalPlayer.Character.IsPositionFrozen = true;
            Shopkeeper.SetPositionZ(Shopkeeper.Position.Z + 8f);
            Shopkeeper.IsPositionFrozen = true;

            SuspectCar.Position = SpawnPoint;
            Camera cam = new Camera(true);
            cam.Position = SuspectCar.GetOffsetPosition(Vector3.RelativeFront * 4.4f);
            cam.SetPositionZ(cam.Position.Z + 3.6f);
            Vector3 directionFromShopkeeperToCar = (SuspectCar.Position - cam.Position);
            directionFromShopkeeperToCar.Normalize();
            cam.Rotation = directionFromShopkeeperToCar.ToRotator();
            Suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            GameFiber.Wait(100);
            SuspectCar.IsPositionFrozen = true;
            
            GameFiber.Sleep(2000);
            
            Game.FadeScreenIn(1500, true);
            CCTVCamNumber = AssortedCalloutsHandler.rnd.Next(1, 20);
            Game.FrameRender += DrawCCTVText;
            Game.DisplaySubtitle("~b~Shopkeeper~s~: There they are! Bastard!", 6600);
            GameFiber.Sleep(6500);
            Game.FadeScreenOut(1500, true);
            CCTVShowing = false;
            Game.LocalPlayer.Character.IsVisible = true;
            if (Game.LocalPlayer.Character.LastVehicle.Exists())
            {
                Game.LocalPlayer.Character.LastVehicle.Position = OldLastVehiclePos;
                Game.LocalPlayer.Character.LastVehicle.IsPositionFrozen = false;
                Game.LocalPlayer.Character.LastVehicle.IsVisible = true;
            }
            //World.IsTimeOfDayFrozen = false;
            if (DateTimeChanged) { World.DateTime = OldDateTime; }
            Shopkeeper.IsVisible = true;
            Game.LocalPlayer.Character.IsPositionFrozen = false;
            Shopkeeper.IsPositionFrozen = false;
            Game.LocalPlayer.Character.Position = PlayerOldPos;
            Shopkeeper.Position = ShopkeeperOldPos;
            

            SuspectCar.Position = suspectOldPosition;
            SuspectCar.Rotation = suspectOldRotator;
            SuspectCar.IsPositionFrozen = false;
            Game.LocalPlayer.HasControl = true;
            cam.Delete();
            Suspect.Tasks.CruiseWithVehicle(SuspectCar, 17f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
            Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);
            GameFiber.Sleep(2000);
            NativeFunction.CallByName<uint>("CLEAR_TIMECYCLE_MODIFIER");
            Game.FadeScreenIn(1500, true);

        }
        private bool CCTVShowing = false;
        private int CCTVCamNumber = 3;
        private void DrawCCTVText(System.Object sender, Rage.GraphicsEventArgs e)
        {
            if (CCTVShowing)
            {
                Rectangle drawRect = new Rectangle(0, 0, 200, 130);
                e.Graphics.DrawRectangle(drawRect, Color.FromArgb(100, Color.Black));

                e.Graphics.DrawText("CCTV #" + CCTVCamNumber.ToString("00"), "Aharoni Bold", 35.0f, new PointF(1, 6), Color.White);
                e.Graphics.DrawText(DateTime.Now.Day.ToString("00") + "/" + DateTime.Now.Month.ToString("00") + "/" + DateTime.Now.Year.ToString(), "Aharoni Bold", 35.0f, new PointF(1, 46), Color.White, drawRect);
                e.Graphics.DrawText(DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + ":" + DateTime.Now.Second.ToString("00"), "Aharoni Bold", 35.0f, new PointF(1, 86), Color.White, drawRect);
            }
            else
            {
                Game.FrameRender -= DrawCCTVText;
            }
        }

        private void DetermineShopkeeperLines()
        {
            int Roll = AssortedCalloutsHandler.rnd.Next(8);
            
            if (Roll == 0)
            {
                ShopkeeperLines = new List<string>() { "Good day officer, thank god you're here!", "Someone has just made off without paying for " + PetrolGas.ToLower() + "!", "Times are hard enough already without these thieves!", "I've got CCTV footage of them." };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = true;
            }
            else if (Roll == 1)
            {
                ShopkeeperLines = new List<string>() { "Hey officer! What took you so long?", "People seem to think " + PetrolGas.ToLower() + " is free these days!", "I get these kind of thefts at least twice a week nowadays.", "Anyway, I caught this thief on CCTV!" };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = true;
            }
            else if (Roll ==2)
            {
                ShopkeeperLines = new List<string>() { "Officer! Officer! I've just been robbed!", "Some person just stole " + PetrolGas.ToLower() + " from me!", "I don't have any CCTV, but I know the vehicle's details!" };
                ShowCCTV = false;
                ShopkeeperKnowsVehicleDetails = true;
            }
            else if (Roll == 3)
            {
                ShopkeeperLines = new List<string>() { "Police! I need the police!", "Some prick just nicked some of my fuel!", "Please catch them, officer, times are tough.", "My CCTV is out of order, but I've memorised the vehicle's details!" };
                ShowCCTV = false;
                ShopkeeperKnowsVehicleDetails = true;
            }
            else if (Roll == 4)
            {
                ShopkeeperLines = new List<string>() { "G'day officer! How's it going?", "Someone just used my pump without paying up!", "I have CCTV footage, but I'm no expert when it comes to vehicles.", "I was hoping you could take a look for me." };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = false;
            }
            else if (Roll == 5)
            {
                ShopkeeperLines = new List<string>() { "Finally! What a relief!", "I've just experienced a terrible situation!", "Some rude customer put " + PetrolGas.ToLower() + " into his vehicle.", "After that, he simply drove off without paying!", "Luckily, I've caught the idiot on CCTV.", "Can you make out the vehicle's details?" };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = false;
            }
            else if (Roll==6)
            {
                ShopkeeperLines = new List<string>() { "Pfffft, about time you arrived!", "The police don't seem to care about me at all.", "A theft is committed here almost every day.", "I've been forced to install CCTV cameras.", "Officer, please help me and take a look, will you?", "I can't really make out the vehicle's details." };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = false;
            }
            else if (Roll==7)
            {
                ShopkeeperLines = new List<string>() { "Hey officer, welcome to my " + PetrolGas + " station!", "Unfortunately, I have been getting a lot of thefts recently.", "I caught this one on CCTV.", "Let's take a look, shall we?" };
                ShowCCTV = true;
                ShopkeeperKnowsVehicleDetails = true;
            }
        }


        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
        }
        public override void End()
        {
            base.End();
            CalloutRunning = false;
            SpeechHandler.HandlingSpeech = false;
            if (!CalloutFinished)
            {
                if (Shopkeeper.Exists()) { Shopkeeper.Delete(); }
                if (ShopkeeperBlip.Exists()) { ShopkeeperBlip.Delete(); }
                if (SearchArea.Exists()) { SearchArea.Delete(); }
            }
            else
            {
                if (Shopkeeper.Exists()) { Shopkeeper.Dismiss(); }
                if (ShopkeeperBlip.Exists()) { ShopkeeperBlip.Delete(); }
                if (SearchArea.Exists()) { SearchArea.Delete(); }
                
            }
        }

        public override void Process()
        {
            base.Process();
            
        }
        private void HandleSearchForVehicleWithANPR()
        {
            float Radius = 220f;
            SearchArea = new Blip(SuspectCar.Position.Around(25f), Radius);
            SearchArea.Color = System.Drawing.Color.Yellow;
            SearchArea.Alpha = 0.5f;
            int WaitCount = 0;
            int WaitCountTarget = 2200;
            bool RouteEnabled = false;
            while (CalloutRunning)
            {
                GameFiber.Yield();
                WaitCount++;
                Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( Suspect, 786603);

                if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 9f), SuspectCar.Position) < 9f)
                {
                    GameFiber.Sleep(3000);
                    if (Vector3.Distance(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 9f), SuspectCar.Position) < 9f)
                    {
                        Game.DisplayNotification("Control, I have located the ~b~" + CarModelName + ".");
                        Game.DisplayNotification("I'm preparing to ~b~stop them,~s~ over.");
                        SuspectBlip = Suspect.AttachBlip();
                        if (SearchArea.Exists()) { SearchArea.Delete(); }
                        Functions.PlayScannerAudio("DISPATCH_SUSPECT_LOCATED_ENGAGE");

                        break;
                    }

                }
                else if (((Vector3.Distance(SuspectCar.Position, SearchArea.Position) > Radius + 20f) && (WaitCount > 400)) || (WaitCount > WaitCountTarget))
                {
                    Game.DisplayNotification("~o~ANPR Hit ~s~on the ~b~" + CarColor + " " + CarModelName + ", ~s~plate ~b~" + SuspectCar.LicensePlate + ".");
                    Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION", SuspectCar.Position);
                    SearchArea.Delete();
                    Radius -= 5f;
                    if (Radius < 120f) { Radius = 120f; }
                    SearchArea = new Blip(SuspectCar.Position.Around(5f, 15f), Radius);
                    SearchArea.Color = System.Drawing.Color.Yellow;
                    SearchArea.Alpha = 0.5f;


                    RouteEnabled = false;
                    if (WaitCount > WaitCountTarget) { Game.LogTrivial("Updated for waitcount"); }
                    WaitCount = 0;

                    Suspect.Tasks.CruiseWithVehicle(SuspectCar, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                    WaitCountTarget -= AssortedCalloutsHandler.rnd.Next(200, 400);
                    if (WaitCountTarget < 1200) { WaitCountTarget = 1900; }
                    SuspectBlip = Suspect.AttachBlip();
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
