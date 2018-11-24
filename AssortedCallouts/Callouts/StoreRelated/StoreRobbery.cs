using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;
using AssortedCallouts.Extensions;
using Albo1125.Common.CommonLibrary;
using Rage.Native;

namespace AssortedCallouts.Callouts.StoreRelated
{
    [CalloutInfo("Store Robbery", CalloutProbability.Medium)]
    internal class StoreRobbery : AssortedCallout
    {
        private List<Ped> ShopKeepers = new List<Ped>();
        private List<Ped> Suspects = new List<Ped>();
        private bool CalloutRunning;
        private string msg = "CODE 4";
        private Store RobberyStore;
        private Tuple<Vector3, float> ChosenShopkeeperSpawnData;
        private Tuple<Vector3, float> ChosenRobberSpawnData;
        private WeaponAsset[] FirearmsToSelectFrom = new WeaponAsset[] { "WEAPON_PISTOL50", "WEAPON_SMG" };
        private List<Entity> RelatedEntities = new List<Entity>();
        private List<Rage.Object> MoneyBags = new List<Rage.Object>();
        private bool DeletingNearbyEntities = true;
        private Vehicle EscapeVan;
        private Ped EscapeVanDriver;
        
        


        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("Creating AssortedCallouts.StoreRobbery");

            List<Store> ValidStores = (from x in Store.Stores where (Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1) < 900f && Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1) > 300f) orderby (Game.LocalPlayer.Character.DistanceTo(x.ShopKeeperSpawnData[0].Item1)) select x).ToList<Store>();
            if (ValidStores.Count == 0) { Game.LogTrivial("No valid store found."); return false; }

            RobberyStore = ValidStores[AssortedCalloutsHandler.rnd.Next(ValidStores.Count)];
            SpawnPoint = RobberyStore.ShopKeeperSpawnData[0].Item1;
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 40f);
            CalloutMessage = "~b~" + RobberyStore.Name + " ~r~robbery.";
            CalloutPosition = SpawnPoint;

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout(RobberyStore.Name + " robbery", "Store Robbery", SpawnPoint, 1, "Reports of a robbery in progress. Weapons are involved. No units currently on scene.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_ROBBERY IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            
            SearchArea = new Blip(SpawnPoint);
            SearchArea.Color = System.Drawing.Color.Yellow;
            SearchArea.IsRouteEnabled = true;
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~" + RobberyStore.Name + " ~r~robbery", "Dispatch to ~b~" + AssortedCalloutsHandler.DivisionUnitBeat, "~b~Please respond to a ~r~robbery~b~ at " + RobberyStore.Name + ".");
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
                    if (SituationNumber <3)
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
                        Game.DisplayNotification("~O~StoreRobbery~s~ callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }
                }
            });
        }

        private void SpawnSingleRobberAndShopkeeper()
        {
            ChosenShopkeeperSpawnData = RobberyStore.ShopKeeperSpawnData[AssortedCalloutsHandler.rnd.Next(RobberyStore.ShopKeeperSpawnData.Count)];
            ChosenRobberSpawnData = RobberyStore.RobberSpawnData[AssortedCalloutsHandler.rnd.Next(RobberyStore.RobberSpawnData.Count)];
            ShopKeepers.Add(new Ped(Store.StoreShopkeeperModels[AssortedCalloutsHandler.rnd.Next(Store.StoreShopkeeperModels.Length)], ChosenShopkeeperSpawnData.Item1, ChosenShopkeeperSpawnData.Item2));
            ShopKeepers[0].MakeMissionPed();
            ShopKeepers[0].RelationshipGroup = "SHOPKEEPERS";
            ShopKeepers[0].Health = 100;
            ShopKeepers[0].Armor = 0;

            Suspects.Add(new Ped(Store.StoreRobberModels[AssortedCalloutsHandler.rnd.Next(Store.StoreRobberModels.Length)], ChosenRobberSpawnData.Item1, ChosenRobberSpawnData.Item2));
            Suspects[0].MakeMissionPed();
            Suspects[0].Inventory.GiveNewWeapon(FirearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(FirearmsToSelectFrom.Length)], -1, true);
            Suspects[0].RelationshipGroup = "ROBBERS";
            Suspects[0].Health += 180;
            Suspects[0].Armor = 15;
            RelatedEntities.AddRange(ShopKeepers);
            RelatedEntities.AddRange(Suspects);
            if (ComputerPlusRunning)
            {
                //API.ComputerPlusFuncs.AddPedToCallout(CalloutID, Suspects[0]);
            }
            Functions.SetPedCantBeArrestedByPlayer(Suspects[0], true);
            if (AssortedCalloutsHandler.rnd.Next(2) == 0)
            {
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( Suspects[0], 9, 2, 1, 0);
            }
            else
            {
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( Suspects[0], 9, 1, 1, 0);
            }

            NativeFunction.Natives.SET_PED_NON_CREATION_AREA(SpawnPoint.X - 10f, SpawnPoint.Y - 10f, SpawnPoint.Z - 10f, SpawnPoint.X + 10f, SpawnPoint.Y + 10f, SpawnPoint.Z + 10f);
            Suspects[0].Tasks.AimWeaponAt(ShopKeepers[0], -1);
            ShopKeepers[0].Tasks.PutHandsUp(-1, Suspects[0]);
            
        }
        

        private void SituationOne()
        {
            //One robber, one shopkeeper. Wait, fight, flee (pursuit).

            SpawnSingleRobberAndShopkeeper();
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
            bool fighting = false;
            int WaitCount = 0;
            bool PursuitCreated = false;
            bool MoneySpawned = false;
            int FightWaitCount = 0;
            while (CalloutRunning)
            {
                GameFiber.Yield();

                //NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", MoneyBag, true);
                if (Game.LocalPlayer.Character.DistanceTo(Suspects[0].Position) < 15f && !fighting)
                {
                    DeletingNearbyEntities = false;
                    if (!MoneySpawned)
                    {
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeLeft * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeRight * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeBack * 0.5f)));
                        foreach (Rage.Object MoneyBag in MoneyBags)
                        {
                            MoneyBag.IsPersistent = true;



                        }
                        MoneySpawned = true;
                        if (ComputerPlusRunning)
                        {
                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Single suspect. Armed with a firearm. Holding shopkeeper at gunpoint.");
                        }
                    }
                    if (!Suspects[0].IsAnySpeechPlaying)
                    {
                        Suspects[0].PlayAmbientSpeech("GENERIC_CURSE_HIGH");
                    }
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.DisplaySubtitle("~b~Robber~s~: Come any nearer and I'll kill you both, pig!", 50);
                        FightWaitCount++;
                    }

                    if (NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>( Game.LocalPlayer.Character) != 0 || FightWaitCount > 500)
                    {
                        Suspects[0].Tasks.FightAgainstClosestHatedTarget(50f);
                        fighting = true;
                    }
                    else
                    {
                        try
                        {
                            unsafe
                            {
                                uint entityHandle;
                                //Game.LogTrivial("Getting player aiming location.");
                                NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                                if (World.GetEntityByHandle<Rage.Entity>(entityHandle) == Suspects[0])
                                {
                                    Suspects[0].Tasks.FightAgainstClosestHatedTarget(50f);
                                    fighting = true;
                                }
                            }
                        }
                        catch (Exception e) { }
                    }

                }
                if (fighting)
                {

                    WaitCount++;
                }
                if (WaitCount > 1000 && !PursuitCreated)
                {
                    PursuitCreated = true;
                    Functions.SetPedCantBeArrestedByPlayer(Suspects[0], false);
                    Suspects[0].Tasks.ClearImmediately();
                    if (ShopKeepers[0].IsAlive && AssortedCalloutsHandler.rnd.Next(3) != 0)
                    {
                        Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(Suspects[0], ShopKeepers[0], 2000, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                    }
                    GameFiber.Wait(2000);

                    Pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(Pursuit, Suspects[0]);
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                }
                if (Suspects[0].Exists())
                {
                    if (Suspects[0].IsDead || Functions.IsPedArrested(Suspects[0]))
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

        private void SpawnAllRobbersAndShopkeepers()
        {
            foreach (Tuple<Vector3, float> spawndata in RobberyStore.ShopKeeperSpawnData)
            {
                Ped shopkeep = new Ped(Store.StoreShopkeeperModels[AssortedCalloutsHandler.rnd.Next(Store.StoreShopkeeperModels.Length)], spawndata.Item1, spawndata.Item2);

                shopkeep.MakeMissionPed();
                shopkeep.RelationshipGroup = "SHOPKEEPERS";
                shopkeep.Health = 100;
                shopkeep.Armor = 0;
                ShopKeepers.Add(shopkeep);
            }
            foreach (Tuple<Vector3, float> spawndata in RobberyStore.RobberSpawnData)
            {
                Ped suspect = new Ped(Store.StoreRobberModels[AssortedCalloutsHandler.rnd.Next(Store.StoreRobberModels.Length)], spawndata.Item1, spawndata.Item2);
                suspect.MakeMissionPed();
                suspect.Inventory.GiveNewWeapon(FirearmsToSelectFrom[AssortedCalloutsHandler.rnd.Next(FirearmsToSelectFrom.Length)], -1, true);
                suspect.RelationshipGroup = "ROBBERS";
                suspect.Health += 180;
                suspect.Armor = 15;
                Suspects.Add(suspect);
                if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( suspect, 9, 2, 1, 0);
                }
                else
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( suspect, 9, 1, 1, 0);
                }
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddPedToCallout(CalloutID, suspect);
                }
                Functions.SetPedCantBeArrestedByPlayer(suspect, true);
            }
            RelatedEntities.AddRange(ShopKeepers);
            RelatedEntities.AddRange(Suspects);
            NativeFunction.Natives.SET_PED_NON_CREATION_AREA(SpawnPoint.X - 10f, SpawnPoint.Y - 10f, SpawnPoint.Z - 10f, SpawnPoint.X + 10f, SpawnPoint.Y + 10f, SpawnPoint.Z + 10f);
            for (int i = 0; i < Suspects.Count; i++)
            {
                Suspects[i].Tasks.AimWeaponAt(ShopKeepers[i], -1);
                ShopKeepers[i].Tasks.PutHandsUp(-1, Suspects[i]);
            }
        }

        private void SituationTwo()
        {
            //Multiple robbers, multiple shopkeepers, threat, shoot, flee (pursuit).
            SpawnAllRobbersAndShopkeepers();
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea) < 40f)
                {
                    SearchArea.IsRouteEnabled = false;
                    break;
                }
            }
            bool fighting = false;
            int WaitCount = 0;
            bool PursuitCreated = false;
            bool MoneySpawned = false;
            int FightWaitCount = 0;
            while (CalloutRunning)
            {
                GameFiber.Yield();

                //NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", MoneyBag, true);
                if (Game.LocalPlayer.Character.DistanceTo(Suspects[0].Position) < 15f && !fighting)
                {
                    DeletingNearbyEntities = false;
                    if (!MoneySpawned)
                    {
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeLeft * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeRight * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeBack * 0.5f)));
                        foreach (Rage.Object MoneyBag in MoneyBags)
                        {
                            MoneyBag.IsPersistent = true;



                        }
                        if (ComputerPlusRunning)
                        {
                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Multiple suspects with firearms. Holding shopkeepers at gunpoint.");
                        }
                        MoneySpawned = true;
                    }
                    if (!Suspects[0].IsAnySpeechPlaying)
                    {
                        Suspects[0].PlayAmbientSpeech("GENERIC_CURSE_HIGH");
                    }
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.DisplaySubtitle("~b~Robber~s~: Come any nearer and we'll kill you all, pig!", 50);
                        FightWaitCount++;
                    }

                    if (NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>( Game.LocalPlayer.Character) != 0 || FightWaitCount > 500)
                    {
                        foreach (Ped suspect in Suspects)
                        {
                            suspect.Tasks.FightAgainstClosestHatedTarget(50f);
                        }
                        fighting = true;
                    }
                    else
                    {
                        try
                        {
                            unsafe
                            {
                                uint entityHandle;
                                //Game.LogTrivial("Getting player aiming location.");
                                NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                                if (Suspects.Contains(World.GetEntityByHandle<Rage.Entity>(entityHandle)))
                                {
                                    foreach (Ped suspect in Suspects)
                                    {
                                        suspect.Tasks.FightAgainstClosestHatedTarget(50f);
                                    }
                                    fighting = true;
                                }
                            }
                        }
                        catch (Exception e) { }
                    }

                }
                if (fighting)
                {
                    WaitCount++;
                }
                if (WaitCount > 1000 && !PursuitCreated)
                {
                    
                    PursuitCreated = true;
                    for (int i = 0; i < Suspects.Count; i++)
                    {
                        Functions.SetPedCantBeArrestedByPlayer(Suspects[i], false);
                        Suspects[i].Tasks.ClearImmediately();

                        if (ShopKeepers[i].IsAlive && AssortedCalloutsHandler.rnd.Next(3) != 0)
                        {
                            Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(Suspects[i], ShopKeepers[i], 2000, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                        }
                    }
                    GameFiber.Wait(2000);

                    bool created = false;
                    foreach (Ped suspect in Suspects)
                    {
                        if (suspect.IsAlive)
                        {
                            if (!created)
                            {
                                Pursuit = Functions.CreatePursuit();
                                created = true;
                            }
                            Functions.AddPedToPursuit(Pursuit, suspect);
                        }
                    }
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                }
                bool done = true;
                foreach (Ped suspect in Suspects)
                {
                    if (suspect.Exists())
                    {
                        if (suspect.IsAlive && !Functions.IsPedArrested(suspect))
                        {
                            done = false;
                            break;
                        }
                    }
                }
                if (done)
                {
                    break;
                }
            }
            DisplayCodeFourMessage();
        }

        Vector3 SafePedSpawn = Vector3.Zero;
        private void SituationThree()
        {
            SpawnAllRobbersAndShopkeepers();
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(SpawnPoint).GetSafeVector3ForPed(out SafePedSpawn))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10)
                {
                    
                    break;
                }
            }
            EscapeVan = new Vehicle("SPEEDO", World.GetNextPositionOnStreet(SpawnPoint), 0f);
            EscapeVan.Heading = EscapeVan.CalculateHeadingTowardsEntity(Suspects[0]) + 180f;
            EscapeVan.IsPersistent = true;
            EscapeVanDriver = EscapeVan.CreateRandomDriver();
            EscapeVanDriver.MakeMissionPed();
            RelatedEntities.Add(EscapeVanDriver);
            //SuspectBlip = EscapeVan.AttachBlip();
            EscapeVanDriver.Tasks.CruiseWithVehicle(10f).WaitForCompletion(2000);
            EscapeVanDriver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, EscapeVan) < 150f)
                {
                    
                    SearchArea.IsRouteEnabled = false;
                    for (int i = 0; i < Suspects.Count; i++)
                    {
                        Suspects[i].Tasks.ClearImmediately();

                        if (ShopKeepers[i].IsAlive && AssortedCalloutsHandler.rnd.Next(3) != 0)
                        {
                            Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(Suspects[i], ShopKeepers[i], 2000, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                        }
                    }
                    break;
                }
            }
            
            //Game.DisplaySubtitle("Starting robber tasks", 5000);
            GameFiber.Wait(500);
            if (CalloutRunning && SafePedSpawn != Vector3.Zero)
            {
                foreach (Ped suspect in Suspects)
                {
                    suspect.Position = SafePedSpawn.Around(1f, 3f);
                    Functions.SetPedCantBeArrestedByPlayer(suspect, false);
                }
            }
            foreach (Ped shopkeep in ShopKeepers)
            {
                if (shopkeep.IsAlive)
                {
                    shopkeep.Tasks.PutHandsUp(-1, null);
                }
            }
            List<Ped> SuspectsTryingToGetIn = new List<Ped>(Suspects);
            while (CalloutRunning)
            {
                GameFiber.Yield();
                bool done = true;
                foreach (Ped suspect in SuspectsTryingToGetIn.ToArray())
                {
                    Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(suspect, false);
                    if (Game.LocalPlayer.Character.DistanceTo(EscapeVan) < 20f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        break;
                    }
                    if (suspect.IsInVehicle(EscapeVan, false) || suspect.IsDead || Functions.IsPedArrested(suspect) || Functions.IsPedGettingArrested(suspect))
                    {
                        continue;
                    }
                    done = false;
                    if (Vector3.Distance(suspect.Position, EscapeVan.Position) < 6f)
                    {
                        suspect.Tasks.EnterVehicle(EscapeVan, 3000, Suspects.IndexOf(suspect));
                        SuspectsTryingToGetIn.Remove(suspect);
                    }
                    else
                    {
                        
                        suspect.Tasks.FollowNavigationMeshToPosition(EscapeVan.RearPosition, EscapeVan.Heading, 1.7f).WaitForCompletion(500);
                        //Game.LogTrivial("Follow task set");
                    }
                }
                if (done) { break; }
                GameFiber.Wait(3100);


                
            }  
            if (CalloutRunning)
            {
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Multiple suspects with firearms. Fleeing the scene. Conspirator driving a getaway van involved.");
                    API.ComputerPlusFuncs.AddVehicleToCallout(CalloutID, EscapeVan);
                }
                Pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(Pursuit, EscapeVanDriver);
                foreach (Ped suspect in Suspects)
                {
                    if (suspect.IsAlive)
                    {

                        Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(suspect, true);
                        Functions.AddPedToPursuit(Pursuit, suspect);
                    }
                }
                
                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                foreach (Ped passenger in EscapeVan.Passengers)
                {
                    passenger.Inventory.GiveNewWeapon("WEAPON_MICROSMG", -1, true);
                    passenger.Accuracy = 45;
                }
            }
            bool passengershooting = false;
            while (CalloutRunning)
            {
                GameFiber.Yield();

                foreach (Ped Passenger in EscapeVan.Passengers)
                {
                    if (!passengershooting)
                    {
                        if (Vector3.Distance(Passenger.Position, Game.LocalPlayer.Character.Position) < 16f)
                        {

                            passengershooting = true;
                            NativeFunction.Natives.TASK_DRIVE_BY( Passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 80, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
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

                bool done = true;

                foreach (Ped suspect in Suspects)
                {
                    if (suspect.Exists())
                    {
                        if (suspect.IsAlive && !Functions.IsPedArrested(suspect))
                        {
                            done = false;
                            break;
                        }
                    }
                }
                if (done)
                {
                    break;
                }
            }
            DisplayCodeFourMessage();
        }          
        
        private void SituationFour()
        {
            SpawnSingleRobberAndShopkeeper();
            while (CalloutRunning)
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SearchArea) < 40f)
                {
                    SearchArea.IsRouteEnabled = false;
                    break;
                }
            }
            bool fighting = false;
            int WaitCount = 0;
            bool PursuitCreated = false;
            bool MoneySpawned = false;
            int FightWaitCount = 0;
            bool surrendered = false;
            bool Surrendering = false;
            int Situation = AssortedCalloutsHandler.rnd.Next(4);
            while (CalloutRunning)
            {
                GameFiber.Yield();

                //NativeFunction.CallByName<uint>("SET_ENTITY_HAS_GRAVITY", MoneyBag, true);
                if (Game.LocalPlayer.Character.DistanceTo(Suspects[0].Position) < 15f && !fighting && !Surrendering)
                {
                    DeletingNearbyEntities = false;
                    if (!MoneySpawned)
                    {
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeLeft * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeRight * 0.5f)));
                        MoneyBags.Add(new Rage.Object("PROP_MONEY_BAG_01", Suspects[0].GetOffsetPosition(Vector3.RelativeBack * 0.5f)));
                        foreach (Rage.Object MoneyBag in MoneyBags)
                        {
                            MoneyBag.IsPersistent = true;



                        }
                        if (ComputerPlusRunning)
                        {
                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Single robber with a firearm. Holding shopkeeper at gunpoint.");
                            
                        }
                        MoneySpawned = true;
                    }
                    if (!Suspects[0].IsAnySpeechPlaying)
                    {
                        Suspects[0].PlayAmbientSpeech("GENERIC_CURSE_HIGH");
                    }
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.DisplaySubtitle("~b~Robber~s~: Come any nearer and I'll kill you both, pig!", 50);
                        FightWaitCount++;
                    }

                    if (NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>(Game.LocalPlayer.Character) != 0 || FightWaitCount > 1000)
                    {
                        if (Situation < 2)
                        {
                            Suspects[0].Tasks.FightAgainstClosestHatedTarget(50f);
                            fighting = true;
                        }
                        else
                        {
                            GameFiber.Wait(1000);
                            Game.DisplaySubtitle("~b~Robber~s~: Okay! Okay! Don't shoot, I'm surrendering!", 5000);
                            Surrendering = true;
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "The robber surrendered.");

                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            unsafe
                            {
                                uint entityHandle;
                                //Game.LogTrivial("Getting player aiming location.");
                                NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                                

                                
                                if (World.GetEntityByHandle<Rage.Entity>(entityHandle) == Suspects[0])
                                {
                                    if (Situation < 2)
                                    {
                                        GameFiber.Wait(1000);
                                        Game.DisplaySubtitle("~b~Robber~s~: Okay! Okay! Don't shoot, I'm surrendering!", 5000);
                                        Surrendering = true;
                                        if (ComputerPlusRunning)
                                        {
                                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "The robber surrendered.");

                                        }
                                    }
                                    else
                                    {
                                        Suspects[0].Tasks.FightAgainstClosestHatedTarget(50f);
                                        fighting = true;
                                    }
                                }
                            }
                        }
                        catch (Exception e) { }
                    }

                }
                if (fighting)
                {
                    WaitCount++;

                    if (WaitCount > 1000 && !PursuitCreated && fighting)
                    {
                        PursuitCreated = true;
                        Functions.SetPedCantBeArrestedByPlayer(Suspects[0], false);
                        Suspects[0].Tasks.ClearImmediately();
                        if (ShopKeepers[0].IsAlive && AssortedCalloutsHandler.rnd.Next(3) != 0)
                        {
                            Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(Suspects[0], ShopKeepers[0], 2000, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                        }
                        GameFiber.Wait(2000);

                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspects[0]);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                    }
                }
                else if (Surrendering && !surrendered)
                {
                    NativeFunction.Natives.SET_PED_DROPS_WEAPON( Suspects[0]);
                    Functions.SetPedCantBeArrestedByPlayer(Suspects[0], false);
                    Suspects[0].Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    surrendered = true;
                }
                if (Suspects[0].Exists())
                {
                    if (Suspects[0].IsDead || Functions.IsPedArrested(Suspects[0]))
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
                foreach (Rage.Object MoneyBag in MoneyBags)
                {
                    if (MoneyBag.Exists())
                    {
                        MoneyBag.Detach();
                        MoneyBag.Dismiss();
                    }
                }
                if (EscapeVanDriver.Exists())
                {
                    EscapeVanDriver.IsPersistent = false;
                }
                if (EscapeVan.Exists())
                {
                    EscapeVan.Dismiss();
                }
                
            }
            else
            {
                
                foreach (Ped ShopKeeper in ShopKeepers)
                {
                    if (ShopKeeper.Exists()) { ShopKeeper.Delete(); }
                }
                foreach (Rage.Object MoneyBag in MoneyBags)
                {
                    if (MoneyBag.Exists())
                    {
                        MoneyBag.Delete();
                    }
                }
                foreach (Ped suspect in Suspects)
                {
                    if (suspect.Exists()) { suspect.Delete(); }
                }
                if (EscapeVan.Exists())
                {
                    EscapeVan.Delete();
                }
                if (EscapeVanDriver.Exists())
                {
                    EscapeVanDriver.Delete();
                }
            }
        }
        private void DisplayCodeFourMessage()
        {

            DeletingNearbyEntities = false;
            msg = RobberyStore.Name + " ~r~ robbery ~b~code 4.";
            GameFiber.Wait(2000);
            //Game.DisplayHelp("If you wish, you can talk to the shopkeeper.")
            while (CalloutRunning)
            {
                GameFiber.Yield();
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.EndCallKey) + " ~s~to end the call.");
                if (Game.IsKeyDown(AssortedCalloutsHandler.EndCallKey))
                {
                    break;
                }
            }
            if (CalloutRunning)
            {
                GameFiber.Sleep(2000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~" + RobberyStore.Name + " ~r~robbery", "~b~" + AssortedCalloutsHandler.DivisionUnitBeat + "~s~ to Dispatch", msg);

                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                
                End();

            }
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
                            if (ent.Exists())
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
    }
}
