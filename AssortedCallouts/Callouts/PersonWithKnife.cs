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
using Rage.Native;
using Albo1125.Common;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts
{
    
    [CalloutInfo("Person with a knife", CalloutProbability.Medium)]
    internal class PersonWithKnife : AssortedCallout
    {
        private bool CalloutRunning;
        private string msg = "CODE 4";
        
        private Ped Victim;
        private int suspecthealth;
        private int paincount;
        private Blip VictimBlip;
        private static List<Zones.EWorldZone> AllowedCountrySideZones = new List<Albo1125.Common.CommonLibrary.Zones.EWorldZone>() { Albo1125.Common.CommonLibrary.Zones.EWorldZone.SANDY, Albo1125.Common.CommonLibrary.Zones.EWorldZone.PALETO };
        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.PersonWithKnife");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(260f)).GetSafeVector3ForPed(out SpawnPoint))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }
            uint zoneHash = Rage.Native.NativeFunction.CallByHash<uint>(0x7ee64d51e8498728, SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);


            if (Game.GetHashKey("city") != zoneHash && !AllowedCountrySideZones.Contains(Albo1125.Common.CommonLibrary.Zones.GetZone(SpawnPoint)))
            {
                //Game.LogTrivial("Invalid zone: " + Zones.GetLowerZoneName(SpawnPoint));
                return false;
            }
            SearchAreaLocation = SpawnPoint.Around(10f, 30f);
            ShowCalloutAreaBlipBeforeAccepting(SearchAreaLocation, 40f);
            CalloutMessage = "Person with a knife";
            CalloutPosition = SpawnPoint;
            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Person with a knife", "Knifeman", SpawnPoint, 1, "Reports of a person with a knife. Respond as fast as possible and locate the suspect to prevent escalation.",
                1, null, null);
            }

            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_PERSONCARRYINGKNIFE IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            Suspect = NativeFunction.Natives.CREATE_RANDOM_PED<Ped>(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
            
            Suspect.MakeMissionPed();
            

            //SuspectBlip = Suspect.AttachBlip();
            SearchArea = new Blip(SearchAreaLocation, 40f);
            SearchArea.Color = Color.Yellow;
            SearchArea.Alpha = 0.6f;
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Person with a knife", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, "We have a ~r~person with a knife. ~b~Locate and arrest them.");
            CalloutHandler();
            return base.OnCalloutAccepted();
        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            int roll = AssortedCalloutsHandler.rnd.Next(8);
            
            Game.LogTrivial("Roll: " + roll.ToString());
            
            if (roll < 3)
            {
                SituationWaitForArriveFleeStab();
            }
            else if (roll < 6)
            {
                SituationAttackOtherPed();
            }
            else 
            {
                SituationPutHandsUp();
            }
        }
        private void PlaySuspectDescriptionAudio()
        {
            if (ComputerPlusRunning)
            {
                API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
            }
            if (Suspect.IsMale)
            {
                Functions.PlayScannerAudio("SUSPECT_IS MALE");
                Game.DisplaySubtitle("~b~Dispatch: ~s~Suspect is male.", 4000);
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspect is a male.");
                }
            }
            else
            {
                Functions.PlayScannerAudio("SUSPECT_IS FEMALE");
                Game.DisplaySubtitle("~b~Dispatch: ~s~Suspect is female.", 4000);
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspect is a female.");
                }
            }
        }
        
        private void SituationPutHandsUp()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    Suspect.Tasks.Wander();
                    Suspect.Inventory.GiveNewWeapon("WEAPON_KNIFE", -1, true);
                    Functions.SetPedCantBeArrestedByPlayer(Suspect, true);
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) < 55f)
                        {
                            PlaySuspectDescriptionAudio();
                            break;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 15f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            SuspectBlip = Suspect.AttachBlip();
                            SuspectBlip.Color = Color.Red;
                            SuspectBlip.Scale = 0.6f;
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            GameFiber.Yield();
                            NativeFunction.Natives.TASK_SMART_FLEE_PED(Suspect, Game.LocalPlayer.Character, 150f, 20000, true, true);
                            SearchArea.Delete();
                            GameFiber.Wait(3000);
                            break;
                        }
                        if (Vector3.Distance(Suspect.Position, SearchArea.Position) > 42f)
                        {
                            SearchAreaLocation = Suspect.Position.Around(10f, 30f);
                            SearchArea.Position = SearchAreaLocation;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 10f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            
                            //Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 1.5f);

                            Suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                            Functions.SetPedCantBeArrestedByPlayer(Suspect, false);

                            break;

                        }

                    }
                    //bool isragdoll = false;
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Suspect.IsDead || Functions.IsPedArrested(Suspect)) { break; }

                        //if (Suspect.IsRagdoll && !isragdoll)
                        //{
                        //    isragdoll = true;
                        //    paincount++;
                        //    Game.LogTrivial("Paincount ragdoll");
                        //}
                        //else if (!Suspect.IsRagdoll)
                        //{
                        //    isragdoll = false;
                        //}

                        
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
                        Game.DisplayNotification("~O~Personwithaknife~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }


        private void SituationAttackOtherPed()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    Suspect.Tasks.Wander();
                    Suspect.Inventory.GiveNewWeapon("WEAPON_KNIFE", -1, true);
                    Functions.SetPedCantBeArrestedByPlayer(Suspect, true);
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 120f)
                        {
                            //Victim = new Ped(Model.PedModels.ToList().Shuffle()[0], Suspect.GetOffsetPosition(Vector3.RelativeFront * 3f), 0f);
                            Victim = NativeFunction.Natives.CREATE_RANDOM_PED<Ped>(Suspect.GetOffsetPosition(Vector3.RelativeFront * 3f).X, Suspect.GetOffsetPosition(Vector3.RelativeFront * 3f).Y, Suspect.GetOffsetPosition(Vector3.RelativeFront * 3f).Z);
                            Victim.MakeMissionPed();
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 1.5f);
                            Game.LogTrivial("Victim health: " + Victim.Health.ToString());
                            Victim.Health = 150;
                            Functions.PlayScannerAudio("WE_HAVE CRIME_STABBING UNITS_RESPOND_CODE_99");
                            Game.DisplayNotification("~b~Control: ~s~Suspect is reportedly stabbing a victim. Respond ~b~CODE 99!");
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Suspect is stabbing a victim. Units urgently required at scene.");
                            }
                            GameFiber.Wait(2000);
                            Suspect.Tasks.FightAgainst(Victim);
                            GameFiber.Wait(600);
                            NativeFunction.Natives.TASK_SMART_FLEE_PED(Victim, Suspect, 10f, -1, true, true);
                            Functions.RequestBackup(Suspect.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                            
                            break;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 30f)
                        {
                            PlaySuspectDescriptionAudio();
                            
                            break;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 15f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            SuspectBlip = Suspect.AttachBlip();
                            SuspectBlip.Color = Color.Red;
                            SuspectBlip.Scale = 0.6f;
                            VictimBlip = Victim.AttachBlip();
                            VictimBlip.Color = Color.Green;
                            VictimBlip.Scale = 0.6f;
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            GameFiber.Yield();
                            NativeFunction.Natives.TASK_SMART_FLEE_PED(Suspect, Game.LocalPlayer.Character, 150f, -1, true, true);
                            SearchArea.Delete();
                            GameFiber.Wait(3000);
                            
                            break;
                        }
                        if (Vector3.Distance(Suspect.Position, SearchArea.Position) > 42f)
                        {
                            SearchAreaLocation = Suspect.Position.Around(10f, 30f);
                            SearchArea.Position = SearchAreaLocation;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 10f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            GameFiber.Wait(2500);
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 1.5f);

                            Suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);

                            break;

                        }

                    }
                    //bool isragdoll = false;
                    if (CalloutRunning)
                    {
                        suspecthealth = Suspect.Health;
                        paincount = 0;
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Suspect.IsDead || Functions.IsPedArrested(Suspect)) { break; }

                        //if (Suspect.IsRagdoll && !isragdoll)
                        //{
                        //    isragdoll = true;
                        //    paincount++;
                        //    Game.LogTrivial("Paincount ragdoll");
                        //}
                        //else if (!Suspect.IsRagdoll)
                        //{
                        //    isragdoll = false;
                        //}

                        if (Suspect.Health < suspecthealth)
                        {
                            paincount++;
                            suspecthealth = Suspect.Health;
                            //Game.LogTrivial("Paincount health");
                        }

                        if (paincount >= 2)
                        {
                            Functions.SetPedCantBeArrestedByPlayer(Suspect, false);
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        Game.DisplayHelp("When you're done, press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~to end the call.");
                        if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                        {
                            Game.HideHelp();
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
                        Game.DisplayNotification("~O~Personwithaknife~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }



        private void SituationWaitForArriveFleeStab()
        { 

            GameFiber.StartNew(delegate
            {
                try
                {
                    Suspect.Tasks.Wander();
                    Suspect.Inventory.GiveNewWeapon("WEAPON_KNIFE", -1, true);
                    Functions.SetPedCantBeArrestedByPlayer(Suspect, true);
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea.Position) < 55f)
                        {
                            PlaySuspectDescriptionAudio();
                            break;
                        }
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 15f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            SuspectBlip = Suspect.AttachBlip();
                            SuspectBlip.Color = Color.Red;
                            SuspectBlip.Scale = 0.6f;
                            Pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(Pursuit, Suspect);
                            Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                            GameFiber.Yield();
                            NativeFunction.Natives.TASK_SMART_FLEE_PED(Suspect, Game.LocalPlayer.Character, 150f, 30000, true, true);
                            SearchArea.Delete();
                            GameFiber.Wait(3000);
                            break;
                        }
                        if (Vector3.Distance(Suspect.Position, SearchArea.Position) > 42f)
                        {
                            SearchAreaLocation = Suspect.Position.Around(10f, 30f);
                            SearchArea.Position = SearchAreaLocation;
                        }
                    }
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, Suspect.Position) < 8.5f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Suspect.Speed > 3f)
                            {
                                GameFiber.Wait(2500);
                            }
                            
                            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER( 1.5f);

                            Suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            
                            break;

                        }
                        
                    }
                    bool isragdoll = false;
                    if (CalloutRunning)
                    {
                        suspecthealth = Suspect.Health;
                        paincount = 0;
                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Suspect.IsDead || Functions.IsPedArrested(Suspect)) { break; }

                        if (Suspect.IsRagdoll && !isragdoll)
                        {
                            isragdoll = true;
                            paincount++;
                            //Game.LogTrivial("Paincount ragdoll");
                        }
                        else if (!Suspect.IsRagdoll && isragdoll)
                        {
                            isragdoll = false;
                        }

                        else if (Suspect.Health < suspecthealth)
                        {
                            paincount++;
                            
                            //Game.LogTrivial("Paincount health");
                        }
                        suspecthealth = Suspect.Health;
                        if (paincount >= 3)
                        {
                            Functions.SetPedCantBeArrestedByPlayer(Suspect, false);
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
                        Game.DisplayNotification("~O~Personwithaknife~s~ callout crashed, sorry. Please send me your log file.");
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
                msg = "";
                if (Suspect.Exists())
                {
                    if (Functions.IsPedArrested(Suspect))
                    {
                        msg = "The suspect is ~g~under arrest~s~. ";
                    }
                    else if (Suspect.IsDead)
                    {
                        msg = "The suspect is ~o~dead~s~. ";
                    }
                }
                if (Victim.Exists())
                {
                    if (Victim.IsAlive)
                    {
                        msg += "The victim ~g~survived. ~s~";
                    }
                    else
                    {
                        msg += "The victim ~r~was killed. ~s~";
                    }
                }
                msg += "We are ~g~CODE 4~s~.";
                GameFiber.Sleep(4000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Person with a knife", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, msg);



                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
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
            if (VictimBlip.Exists()) { VictimBlip.Delete(); }
            //SpeechHandler.HandlingSpeech = false;
            if (!CalloutFinished)
            {
                if (Victim.Exists()) { Victim.Delete(); }
            }
            else
            {

                if (Victim.Exists()) { Victim.Dismiss(); }
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
    }
}
