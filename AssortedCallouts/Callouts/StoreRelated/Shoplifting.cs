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
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts.StoreRelated
{
    [CalloutInfo("Shoplifting", CalloutProbability.Medium)]
    class Shoplifting : AssortedCallout
    {
        private List<Ped> ShopKeepers = new List<Ped>();
        private List<Ped> Suspects = new List<Ped>();
        private List<Ped> Security = new List<Ped>();
        private Store ShopliftingStore;
        private bool CalloutRunning;
        private string msg = "CODE 4";
        private Tuple<Vector3, float> ChosenShopkeeperSpawnData;
        private Tuple<Vector3, float> ChosenShoplifterSpawnData;
        private Tuple<Vector3, float> ChosenSecuritySpawnData;
        private List<Entity> RelatedEntities = new List<Entity>();
        private List<Rage.Object> MoneyBags = new List<Rage.Object>();
        private bool DeletingNearbyEntities = true;
        private int StolenGoodsValue;

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.Shoplifting");

            List<Store> ValidStores = (from x in Store.Stores where (Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1) < 800f && Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1) > 210f) orderby (Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1)) select x).ToList<Store>();
            if (ValidStores.Count == 0) { Game.LogTrivial("No valid store found."); return false; }

            ShopliftingStore = ValidStores[AssortedCalloutsHandler.rnd.Next(ValidStores.Count)];
            SpawnPoint = ShopliftingStore.ShopliftingSecuritySpawnData[0].Item1;
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 40f);
            CalloutMessage = "~b~" + ShopliftingStore.Name + " ~r~shoplifting" ;
            CalloutPosition = SpawnPoint;

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout(ShopliftingStore.Name + " shoplifting", "Shoplifting", SpawnPoint, 1, "Reports of a shoplifting in progress. No units currently on scene. Please respond.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_484 IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            SearchArea = new Blip(SpawnPoint);
            SearchArea.Color = System.Drawing.Color.Yellow;
            SearchArea.IsRouteEnabled = true;
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~" + ShopliftingStore.Name + " ~r~shoplifting", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, "~b~Please respond to a ~r~shoplifting~b~ at " + ShopliftingStore.Name + ".");
            KeepStoreClearedFromUnrelatedPeds();
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
                    int SituationNumber = AssortedCalloutsHandler.rnd.Next(12);
                    Game.LogTrivial("SituationNumber: " + SituationNumber.ToString());
                    
                    if (SituationNumber < 8)
                    {
                        SituationOne();
                    }
                    else
                    {
                        SituationTwo();
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
                        Game.DisplayNotification("~O~Shoplifting~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }
        private void SituationOne()
        {
            //One robber, one shopkeeper. Wait, fight, flee (pursuit).
            StolenGoodsValue = AssortedCalloutsHandler.rnd.Next(5, 300);
            SpawnStorePeds();
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea) < 40f)
                {
                    SearchArea.IsRouteEnabled = false;
                    break;
                }
            }
            if (ComputerPlusRunning)
            {
                API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
            }
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>( Game.LocalPlayer.Character) != 0)
                {
                    SpeechHandler.HandleSpeech("Shopkeeper", "Hey officer, the scumbag is in the back!");
                    break;
                }
            }
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, Security[0].Position) < 4f)
                {
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        Security[0].Tasks.ClearImmediately();

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
                SearchArea.Delete();
                DetermineSecurityLines();
                SpeechHandler.HandleSpeech("Security", DetermineSecurityLines(), Security[0]);
                SpeechHandler.HandleSpeech("Suspect", DetermineSuspectLines(), Suspects[0]);
                
            }

            while (CalloutRunning)
            {
                GameFiber.Yield();
                Game.DisplayHelp("Deal with the situation as you see fit. Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~when done.");
                if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                {
                    Game.HideHelp();
                    break;
                }
            }
            DisplayCodeFourMessage();

        }

        private void SituationTwo()
        {
            StolenGoodsValue = AssortedCalloutsHandler.rnd.Next(5, 500);
            SpawnStorePeds();
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea) < 150f)
                {
                    SearchArea.IsRouteEnabled = false;
                    break;
                }
            }
            if (ComputerPlusRunning)
            {
                API.ComputerPlusFuncs.SetCalloutStatusToAtScene(CalloutID);
            }
            if (CalloutRunning)
            {
                SearchArea.Delete();
                Suspects[0].Position = World.GetNextPositionOnStreet(Suspects[0].Position);
                Pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(Pursuit, Suspects[0]);
                Functions.SetPedAsCop(Security[0]);
                Security[0].MakeMissionPed();
                Functions.AddCopToPursuit(Pursuit, Security[0]);
                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~" + ShopliftingStore.Name + " ~r~shoplifting", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, "Suspect has reportedly escaped from the store and is now being chased by security. Apprehend the suspect.");
                Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 CRIME_RESIST_ARREST IN_OR_ON_POSITION", SpawnPoint);
            }
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (!Functions.IsPursuitStillRunning(Pursuit))
                {
                    GameFiber.Wait(3000);
                    SearchArea = ShopKeepers[0].AttachBlip();
                    SearchArea.Color = System.Drawing.Color.Yellow;
                    SearchArea.IsRouteEnabled = true;
                    break;
                }
            }
            
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, ShopKeepers[0].Position) < 4f)
                {
                    if (SearchArea.Exists())
                    {
                        SearchArea.IsRouteEnabled = false;
                        SearchArea.Delete();
                    }
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        ShopKeepers[0].Tasks.ClearImmediately();
                        SpeechHandler.HandleSpeech("Shopkeeper", DetermineShopkeeperLines(), ShopKeepers[0]);
                        GameFiber.Wait(4000);
                        break;
                    }
                }
                else
                {
                    Game.DisplayHelp("Talk to the ~b~shopkeeper ~s~for a victim statement.");
                }
            }

            while (CalloutRunning)
            {
                GameFiber.Yield();
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~when you're done investigating.");
                if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                {
                    Game.HideHelp();
                    break;
                }
            }
            DisplayCodeFourMessage();

        }

        private void DisplayCodeFourMessage()
        {

            DeletingNearbyEntities = false;
            msg = ShopliftingStore.Name + " ~r~ shoplifting ~b~code 4.";
            GameFiber.Wait(2000);
            //Game.DisplayHelp("If you wish, you can talk to the shopkeeper.")
            
            if (CalloutRunning)
            {
                GameFiber.Sleep(2000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~" + ShopliftingStore.Name + " ~r~shoplifting", "~b~" + AssortedCalloutsHandler.DivisionUnitBeat + "~s~ to Dispatch", msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;

                End();

            }
        }

        private List<string> DetermineShopkeeperLines()
        {
            int roll = AssortedCalloutsHandler.rnd.Next(2);
            List<string> Lines = new List<string>();
            if (roll == 0)
            {
                Lines = new List<string>() { "Hey, thanks for coming back to me.", "This person that just ran away stole quite a few items.", "The estimated value of the items is $" + StolenGoodsValue, "They also hit our security guard.", "Make sure to throw the book at them!" };
            }
            else if (roll == 1)
            {
                Lines = new List<string>() { "Hey officer, thanks for getting back here.", "That scumbag that just fled the store had stolen multiple items.", "Their value is approximately $" + StolenGoodsValue, "Make sure to lock them up, please!" };

            }
            return Lines;
                
        }

        private List<string> DetermineSecurityLines()
        {
            int roll = AssortedCalloutsHandler.rnd.Next(5);
            List<string> Lines = new List<string>();
            if (roll == 0)
            {
                Lines = new List<string>() { "Hey officer, thanks for showing up for once.", "This person here just tried to shoplift some items from this store!", "They're a known 'customer' here, keep that in mind.", "We've banned ban them from the store, but they came back.", "The value of the items he tried to steal is $" + StolenGoodsValue + "." };
            }
            else if (roll == 1)
            {
                Lines = new List<string>() { "Goodday officer, glad you're here.", "This person here just tried to steal items from here!", "I haven't had dealings with this person before.", "We intend to ban him from the store, though.", "The value of the items he tried to steal is $" + StolenGoodsValue + "." };
            }
            else if (roll == 2)
            {
                Lines = new List<string>() { "How are you today, officer?", "We've just had an attempted shoplifting incident.", "Luckily, we managed to catch them in the act.", "Stuff like this happens so often nowadays.", "His attempted loot is valued at $" + StolenGoodsValue + ".", "Can you please take him in and kick him down the stairs?" };

            }
            else if (roll == 3)
            {
                Lines = new List<string>() { "How are you today, officer?", "We've just had an attempted shoplifting incident.", "Luckily, we managed to catch them in the act.", "Stuff like this happens so often nowadays.", "The value of the items he tried to steal is $" + StolenGoodsValue + ".", "Can you please take him in and kick him down the stairs?" };

            }
            else if (roll == 4)
            {
                Lines = new List<string>() { "What's rolling today, public servant?", "There's just been an attempted shoplifting incident.", "Luckily, the suspect got caught while escaping.", "Stuff like this happens too often nowadays.", "He tried to steal items worth $" + StolenGoodsValue + ".", "Deal with them harshly for once, will ya?" };

            }
            return Lines;
        }

        private List<string> DetermineSuspectLines()
        {
            int roll = AssortedCalloutsHandler.rnd.Next(4);
            List<string> Lines = new List<string>();
            if (roll == 0)
            {
                Lines = new List<string>() { "What complete rubbish.", "I was just going about my business, yanno, looking for stuff.", "Suddenly, when I'm about to leave with nothing, this prick just grabs me!", "I wanted to put the stuff back, but he dragged me here!", "I've basically been assaulted, officer! I want to make a complaint!" };

            }
            else if (roll == 1)
            {
                Lines = new List<string>() { "Well, officer, it isn't that simple.", "I've recently lost my job and times are tough.", "I really needed to get some stuff for my kids.", "I don't have much money though, so I HAD to try this.", "Can you forgive me and give me another chance? PLEASE?" };
            }

            else if (roll == 2)
            {
                Lines = new List<string>() { "Well, officer, what should I say?", "This plastic security prick should never have caught me.", "I've gotten away with this so many times.", "I pretty much have no further comment. Pigs, the lot of ya." };

            }

            else if (roll == 3)
            {
                Lines = new List<string>() { "Lies, you asshole!", "Why the fuck did this store get dedicated security?", "I'll need to head over downtown to get my free stuff now.", "Shoplifting isn't a crime. These retailers are the criminals here!", "Have you seen the recent prices, officer?", "No wonder shoplifting is so common. Scrooges!", "But I was doing nothing, just nosing around. This guy's lying."  };

            }
            return Lines;

        }
        
        private void SpawnStorePeds()
        {
            ChosenShopkeeperSpawnData = ShopliftingStore.ShopKeeperSpawnData[AssortedCalloutsHandler.rnd.Next(ShopliftingStore.ShopKeeperSpawnData.Count)];
            ChosenSecuritySpawnData = ShopliftingStore.ShopliftingSecuritySpawnData[AssortedCalloutsHandler.rnd.Next(ShopliftingStore.ShopliftingSecuritySpawnData.Count)];
            ChosenShoplifterSpawnData = ShopliftingStore.ShopliftingOffenderSpawnData[AssortedCalloutsHandler.rnd.Next(ShopliftingStore.ShopliftingOffenderSpawnData.Count)];
            ShopKeepers.Add(new Ped(Store.StoreShopkeeperModels[AssortedCalloutsHandler.rnd.Next(Store.StoreShopkeeperModels.Length)], ChosenShopkeeperSpawnData.Item1, ChosenShopkeeperSpawnData.Item2));
            ShopKeepers[0].MakeMissionPed();
            ShopKeepers[0].RelationshipGroup = "SHOPKEEPERS";
            ShopKeepers[0].Health = 100;
            ShopKeepers[0].Armor = 0;
            Suspects.Add(new Ped(Store.ShoplifterModels[AssortedCalloutsHandler.rnd.Next(Store.ShoplifterModels.Length)], ChosenShoplifterSpawnData.Item1, ChosenShoplifterSpawnData.Item2));
            Suspects[0].MakeMissionPed();
            
            Suspects[0].RelationshipGroup = "ROBBERS";
            Suspects[0].Health += 180;
            Suspects[0].Armor = 15;

            Security.Add(new Ped("ig_fbisuit_01", ChosenSecuritySpawnData.Item1, ChosenSecuritySpawnData.Item2));
            Security[0].MakeMissionPed();

            Security[0].RelationshipGroup = "SHOPKEEPERS";
            Security[0].Health += 100;
            Security[0].Armor = 15;
            RelatedEntities.AddRange(ShopKeepers);
            RelatedEntities.AddRange(Suspects);
            RelatedEntities.AddRange(Security);
            NativeFunction.Natives.SET_PED_NON_CREATION_AREA(SpawnPoint.X - 10f, SpawnPoint.Y - 10f, SpawnPoint.Z - 10f, SpawnPoint.X + 10f, SpawnPoint.Y + 10f, SpawnPoint.Z + 10f);

        }
        private void KeepStoreClearedFromUnrelatedPeds()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "SHOPKEEPERS", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("SHOPKEEPERS", "ROBBERS", Relationship.Hate);
                    if (DeletingNearbyEntities)
                    {
                        foreach (Entity ent in World.GetEntities(SpawnPoint, 10f, GetEntitiesFlags.ConsiderAllPeds | GetEntitiesFlags.ExcludePlayerPed | GetEntitiesFlags.ExcludePoliceOfficers))
                        {
                            if (ent.Exists() && DeletingNearbyEntities && CalloutRunning)
                            {
                                Ped ped = (Ped)ent;
                                if (!RelatedEntities.Contains(ent) && ent.Model != new Model("S_M_M_PARAMEDIC_01") && ped.RelationshipGroup != "COP")
                                {
                                    if (NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>( ent) != 0)
                                    {
                                        ent.Delete();
                                    }
                                }
                            }
                        }
                    }
                }
            });

        }

        public override void End()
        {
            CalloutRunning = false;
            Rage.Native.NativeFunction.Natives.CLEAR_PED_NON_CREATION_AREA();
            NativeFunction.Natives.SET_STORE_ENABLED(true);
            if (Game.LocalPlayer.Character.Exists())
            {
                if (Game.LocalPlayer.Character.IsDead)
                {
                    GameFiber.Wait(1500);
                    Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                    GameFiber.Wait(3000);
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Officer down. Urgent assistance required.");

                    }
                }
            }
            else
            {
                GameFiber.Wait(1500);
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                GameFiber.Wait(3000);
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Officer down. Urgent assistance required.");

                }
            }
            base.End();
            if (SearchArea.Exists()) { SearchArea.Delete(); }

            if (CalloutFinished)
            {

                foreach (Ped ShopKeeper in ShopKeepers)
                {
                    if (ShopKeeper.Exists()) { ShopKeeper.Tasks.Clear(); ShopKeeper.IsPersistent = false; }
                }
                foreach (Ped suspect in Suspects)
                {
                    if (suspect.Exists()) { Suspect.IsPersistent = false; }
                }
                foreach (Ped suspect in Security)
                {
                    if (suspect.Exists()) { suspect.IsPersistent = false; }
                }



            }
            else
            {

                foreach (Ped ShopKeeper in ShopKeepers)
                {
                    if (ShopKeeper.Exists()) { ShopKeeper.Delete(); }
                }
                
                foreach (Ped suspect in Suspects)
                {
                    if (suspect.Exists()) { suspect.Delete(); }
                }
                foreach (Ped suspect in Security)
                {
                    if (suspect.Exists()) { suspect.Delete(); }
                }

            }
        }
    }
}
