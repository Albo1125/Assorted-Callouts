using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Engine.Scripting.Entities;
using AssortedCallouts.Extensions;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts.Solicitation
{
    internal class SolicitationTrafficStop
    {

        public Ped Driver;
        public Ped Passenger;
        public Vehicle veh;
        private Persona DriverPersona;
        private Persona PassengerPersona;
        public bool TrafficStopActive;
        public bool ReadyForCleanup = false;
        private Rage.Task driverEnter;
        private Rage.Task passengerEnter;
        private string[] Weapons = new string[] { "WEAPON_PISTOL50", "WEAPON_ASSAULTSMG", "WEAPON_KNIFE", "WEAPON_PISTOL", "WEAPON_UNARMED" };
        public List<string> QuestionsForDriver = new List<string>() { "Exit Questioning", "Why is this lady in your vehicle?", "What is the name of this lady?", "Excuse me, but how old is this lady?", "Where are you two going?", "How long have you known this lady for?" };
        public List<string> QuestionsForPassenger = new List<string>() { "Exit Questioning", "Why are you in this male's vehicle?", "What is the name of this male?", "How old is this male?", "Where are you two going?", "How long have you known this male for?" };

        public List<string> DriverAnswers;
        public List<string> PassengerAnswers;

        public bool DriverArrested = false;
        public bool PassengerArrested = false;
        public bool DriverDead = false;
        public bool PassengerDead = false;
        public bool MenuCanBeOpened = true;
        public bool DriverShouldBeArrested;
        public bool PassengerShouldBeArrested;
        public bool Questioning = false;
        public bool DriverPickedUp = false;
        public bool PassengerPickedUp = false;
        public bool ArrestCanBeMade = false;
        public bool BackupRequested = false;
        private LHandle Pursuit;

        public Vehicle BackupUnit;
        private bool HasDriverTriedFleeIfPassGetsArrested = false;
        public bool PursuitCreated = false;
        private bool HaveOccupantsTriedToFleeRunningOverFront = false;
        public bool FightCreated = false;
        private bool BackupOnScene = false;
        

        public SolicitationTrafficStop(Ped Driver, Ped Passenger, Vehicle veh, bool PassengerHookedOtherVehicles)
        {
            this.Driver = Driver;
            this.Passenger = Passenger;
            Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            DriverPersona = Functions.GetPersonaForPed(Driver);
            PassengerPersona = Functions.GetPersonaForPed(Passenger);
            this.veh = veh;
            this.Driver.MakeMissionPed();
            this.Passenger.MakeMissionPed();
            this.QuestionsForDriver = new List<string>(QuestionsForDriver);
            this.QuestionsForPassenger = new List<string>(QuestionsForPassenger);
            
            Functions.SetPedCantBeArrestedByPlayer(Driver, true);
            Functions.SetPedCantBeArrestedByPlayer(Passenger, true);
            Game.DisplayHelp("Press ~b~E ~s~to open the Solicitation Traffic Stop menu.");
            DetermineAnswers(PassengerHookedOtherVehicles);
            
            TrafficStopActive = true;
            MainLogic();
        }
        private void MainLogic()
        {
            GameFiber.StartNew(delegate
            {
                while (!ReadyForCleanup)
                {
                    GameFiber.Yield();
                    if (Driver.Exists())
                    {
                        Driver.IsPersistent = true;
                    }
                    if (Passenger.Exists())
                    {
                        Passenger.IsPersistent = true;
                    }
                    
                    if (!DriverArrested && Driver.Exists())
                    {
                        if (Functions.IsPedArrested(Driver))
                        {
                            DriverArrested = true;
                        }
                        if (Driver.IsDead)
                        {
                            DriverDead = true;
                        }
                    }
                    else if (!DriverPickedUp)
                    {
                        if (!Driver.Exists())
                        {
                            DriverPickedUp = true;
                        }
                        else
                        {
                            if (!Functions.IsPedArrested(Driver))
                            {
                                DriverArrested = false;
                            }
                            if (Driver.IsDead)
                            {
                                DriverDead = true;
                            }
                        }
                    }
                    


                    if (!PassengerArrested && Passenger.Exists())
                    {
                        if (Functions.IsPedArrested(Passenger))
                        {
                            PassengerArrested = true;
                        }
                        if (Functions.IsPedGettingArrested(Passenger))
                        {
                            //Game.LogTrivial("Passenger getting arrested 2");
                            if (!HasDriverTriedFleeIfPassGetsArrested && DriverShouldBeArrested && !DriverArrested && !PursuitCreated && !FightCreated)
                            {

                                GameFiber.Wait(2500);
                                if (AssortedCalloutsHandler.rnd.Next(3) == 0)
                                {
                                    Functions.SetPedCantBeArrestedByPlayer(Passenger, false);
                                    Functions.SetPedCantBeArrestedByPlayer(Driver, false);
                                    Pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(Pursuit, Driver);
                                    if (!Functions.IsPedArrested(Passenger))
                                    {
                                        Functions.AddPedToPursuit(Pursuit, Passenger);
                                    }
                                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                    if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    PursuitCreated = true;
                                    TrafficStopActive = false;
                                }
                                HasDriverTriedFleeIfPassGetsArrested = true;
                            }
                        }
                        if (Passenger.IsDead)
                        {
                            PassengerDead = true;
                        }
                    }
                    else if (!PassengerPickedUp)
                    {
                        if (!Passenger.Exists())
                        {
                            PassengerPickedUp = true;
                        }
                        else
                        {
                            if (!Functions.IsPedArrested(Passenger))
                            {
                                PassengerArrested = false;
                            }
                            if (Passenger.IsDead)
                            {
                                PassengerDead = true;
                            }
                        }
                    }

                    //Run player over if he walks in front of vehicle and they should be arrested
                    if (Driver.IsInVehicle(veh, false) && Passenger.IsInVehicle(veh, false) && !HaveOccupantsTriedToFleeRunningOverFront && DriverShouldBeArrested && PassengerShouldBeArrested && !PursuitCreated && !FightCreated)
                    {
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, veh.GetOffsetPositionFront(1.9f)) <= 2f)
                        {
                            if (AssortedCalloutsHandler.rnd.Next(5) == 0)
                            {
                                if (!DriverArrested || !PassengerArrested)
                                {
                                    Game.LogTrivial("Creating pursuit due to infront");
                                    Functions.SetPedCantBeArrestedByPlayer(Passenger, false);
                                    Functions.SetPedCantBeArrestedByPlayer(Driver, false);
                                    GameFiber.Wait(700);
                                    Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight).WaitForCompletion(700);
                                    Pursuit = Functions.CreatePursuit();
                                    if (!DriverArrested)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, Driver);
                                    }
                                    if (!PassengerArrested)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, Passenger);
                                    }

                                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                    if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    PursuitCreated = true;
                                    TrafficStopActive = false;
                                }
                            }
                            HaveOccupantsTriedToFleeRunningOverFront = true;
                        }


                    }
                    //If backup rolls up chance of them reacting
                    if (!BackupOnScene && BackupRequested && BackupUnit.Exists() && DriverShouldBeArrested && PassengerShouldBeArrested && !PursuitCreated && !FightCreated)
                    {
                        if (!BackupUnit.HasDriver && Vector3.Distance(BackupUnit.Position, veh.Position) < 40f)
                        {
                            BackupOnScene = true;
                            if (AssortedCalloutsHandler.rnd.Next(5) == 0)
                            {
                                
                                if (!DriverArrested || !PassengerArrested)
                                {
                                    Game.LogTrivial("Creating pursuit due to backup");
                                    Functions.SetPedCantBeArrestedByPlayer(Passenger, false);
                                    Functions.SetPedCantBeArrestedByPlayer(Driver, false);
                                    Pursuit = Functions.CreatePursuit();
                                    if (!DriverArrested)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, Driver);
                                    }
                                    if (!PassengerArrested)
                                    {
                                        Functions.AddPedToPursuit(Pursuit, Passenger);
                                    }

                                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                    if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                    Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    PursuitCreated = true;
                                    TrafficStopActive = false;
                                }
                            }
                            else { CreateFight(); }

                        }
                    }



                    

                    //if (!Solicitation.SolicitationMenuPool.IsAnyMenuOpen())
                    //{
                    //    if (Game.LocalPlayer.Character.DistanceTo(veh) < 6f)
                    //    {
                    //        Game.DisplayHelp("Press ~b~E ~s~to open the Solicitation Traffic Stop menu.");
                    //        MenuCanBeOpened = true;
                    //    }
                    //}


                }
            });
        }
        private int GetAge(DateTime Birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - Birthday.Year;

            if (Birthday > today.AddYears(-age))
            {
                age--;
            }
            Game.LogTrivial(age.ToString());
            return age;
                
        }
        private void GetIDForPersona(Persona pers)
        {
            string name = pers.FullName;
            string birthday = pers.BirthDay.ToLongDateString();
            string gender = pers.Gender.ToString();

            //string licensestate = DriverPersona.LicenseState.ToString();
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Assorted Callouts", "Driving Licence", "~b~Name: ~s~" + name + "~n~~b~DOB: ~s~" + birthday + "~n~~b~Age: ~s~" + GetAge(pers.BirthDay).ToString() + "~n~~s~" + gender);
            Game.DisplaySubtitle("~h~~b~Driving Licences can be checked with the police computer.", 4000);
            Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_THANKS");

        }
        public void GetDriverID()
        {
            GetIDForPersona(DriverPersona);
        }
        public void GetPassengerID()
        {
            GetIDForPersona(PassengerPersona);
        }
        private string GetRandomSurname()
        {
            string randomname = PersonaHelper.GetRandomFullName();
            return randomname.Substring(randomname.IndexOf(' ') + 1);
        }

        private void GetDriverAndPassengerAnswersMarried()
        {
            int roll = AssortedCalloutsHandler.rnd.Next(3);
            if (roll == 0)
            {
                PassengerAnswers = new List<string>() { "Because he's my husband, officer.", "His name is " + DriverPersona.FullName, "He's " + GetAge(DriverPersona.BirthDay).ToString() + " years old!", "We're returning home, to our children!", "3 years, officer. Long time, huh?" };
                DriverAnswers = new List<string>() { "Well, she's my wife!", "Uhhmm... " + PassengerPersona.FullName, "She is " + GetAge(PassengerPersona.BirthDay).ToString() + " years old, officer!", "We are going home to our kids!", "We celebrated our 3rd anniversary yesterday!" };
            }
            else if (roll == 1)
            {
                PassengerAnswers = new List<string>() { "He's my husband, officer!", "His name is " + DriverPersona.FullName, "He's " + GetAge(DriverPersona.BirthDay).ToString() + " years old, officer!", "We're returning home to have food.", "4 years, officer. That's a long time, hm?" };
                DriverAnswers = new List<string>() { "She's only my wife, but yea...", "My sunshine's name is " + PassengerPersona.FullName, "She is " + GetAge(PassengerPersona.BirthDay).ToString() + " years old. How rude to ask!", "We are going to have food at home.", "We celebrated our 4th anniversary a week ago!" };
            }
            else
            {
                
                PassengerAnswers = new List<string>() { "He's my husband, hadn't you noticed?", "Mr Sexy... Nah, it's " + DriverPersona.FullName, "He's " + GetAge(DriverPersona.BirthDay).ToString() + " years young.", "We're going to a restaurant.", "6 years, officer. That's a long time, hm?" };
                DriverAnswers = new List<string>() { "Why do you think? She's my wife!", "My honey's name is " + PassengerPersona.FullName, "She is " + GetAge(PassengerPersona.BirthDay).ToString() + " years old.", "We're on our way to a BurgerShot!", "For about 6 years, I think? Time passes quickly." };
            }
        }
        private void GetDriverAndPassengerAnswersNotMarried()
        {
            int roll = AssortedCalloutsHandler.rnd.Next(3);
            if (roll == 0)
            {
                PassengerAnswers = new List<string>() { "That's none of your business!", "His name is " + DriverPersona.Forename + " " + GetRandomSurname(), "He's " + (GetAge(DriverPersona.BirthDay) + AssortedCalloutsHandler.rnd.Next(1, 5)).ToString() + " years old!", "We're going shopping!", AssortedCalloutsHandler.rnd.Next(2, 6).ToString() + " months, officer!" };
                DriverAnswers = new List<string>() { "What do you care?", "Uhhmm... " + PassengerPersona.Forename + " " + GetRandomSurname(), "She is " + (GetAge(PassengerPersona.BirthDay) + 1).ToString() + " years old, officer!", "We are going back to my place!", "About " + AssortedCalloutsHandler.rnd.Next(2, 6).ToString() + " months, officer!" };

            }
            else if (roll == 1)
            {
                PassengerAnswers = new List<string>() { "He's my husband, officer!", "His name is " + DriverPersona.Forename + " " + GetRandomSurname(), "He's " + (GetAge(DriverPersona.BirthDay) + AssortedCalloutsHandler.rnd.Next(1, 5)).ToString() + " years old, officer!", "We're returning home to have food.", "A year, officer. That's a long time, hm?" };
                DriverAnswers = new List<string>() { "She's only my wife, but yea...", "My sunshine's name is " + PassengerPersona.Forename + " " + GetRandomSurname(), "She is " + (GetAge(PassengerPersona.BirthDay) + AssortedCalloutsHandler.rnd.Next(5)).ToString() + " years old. How rude to ask!", "We are going to have food at home.", "We celebrated our first anniversary a week ago!" };

            }
            else if (roll == 2)
            {
                PassengerAnswers = new List<string>() { "Because I like him!", "Ummm... It was " + DriverPersona.Forename + " " + GetRandomSurname(), "I think he must be " + (GetAge(DriverPersona.BirthDay) + AssortedCalloutsHandler.rnd.Next(1, 5)).ToString() + " years old?", "Just driving around, you know?!", "A few days, officer. I met him at the movies." };
                DriverAnswers = new List<string>() { "She's a very nice girl, don't you think?", "Her name is, uhh,  " + PassengerPersona.Forename + " " + GetRandomSurname(), "She looks " + (GetAge(PassengerPersona.BirthDay) + AssortedCalloutsHandler.rnd.Next(5)).ToString() + " years old to me.", "Just cruising, officer. Lovely day!", "I met her through LifeInvader a week ago." };

            }
        }

        public void DetermineAnswers(bool PassengerHookedOtherVehicles)
        {
            if (!PassengerHookedOtherVehicles)
            {
                
                int roll = AssortedCalloutsHandler.rnd.Next(5);
                
                Game.LogTrivial("Hookedothervehicles false, roll " + roll.ToString());
                if (roll == 0)
                {
                    DateTime DriverBirthday = DriverPersona.Birthday;
                    DateTime PassengerBirthday = PassengerPersona.Birthday;
                    while (GetAge(DriverBirthday) < 26) { DriverBirthday = DriverBirthday.AddYears(-1); }
                    while (GetAge(PassengerBirthday) - GetAge(DriverBirthday) > 3) { PassengerBirthday = PassengerBirthday.AddYears(1); GameFiber.Yield(); }
                    while (GetAge(DriverBirthday) - GetAge(PassengerBirthday) > 3) { PassengerBirthday = PassengerBirthday.AddYears(-1); GameFiber.Yield(); }

                    PassengerPersona.Birthday = PassengerBirthday;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Birthday = DriverBirthday;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersMarried();
                    DriverShouldBeArrested = false;
                    PassengerShouldBeArrested = false;
                }
                else if (roll == 1)
                {
                    DateTime DriverBirthday = DriverPersona.Birthday;
                    DateTime PassengerBirthday = PassengerPersona.Birthday;
                    while (GetAge(DriverBirthday) < 25) { DriverBirthday = DriverBirthday.AddYears(-1); }
                    while (GetAge(PassengerBirthday) - GetAge(DriverBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(1); GameFiber.Yield(); }
                    while (GetAge(DriverBirthday) - GetAge(PassengerBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(-1); GameFiber.Yield(); }
                    PassengerPersona.Birthday = PassengerBirthday;
                    PassengerPersona.Wanted = true;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Birthday = DriverBirthday;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersMarried();
                    DriverShouldBeArrested = false;
                    PassengerShouldBeArrested = true;
                }
                else if (roll == 2)
                {
                    DateTime DriverBirthday = DriverPersona.Birthday;
                    DateTime PassengerBirthday = PassengerPersona.Birthday;
                    while (GetAge(DriverBirthday) < 25) { DriverBirthday = DriverBirthday.AddYears(-1); }
                    while (GetAge(PassengerBirthday) - GetAge(DriverBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(1); GameFiber.Yield(); }
                    while (GetAge(DriverBirthday) - GetAge(PassengerBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(-1); GameFiber.Yield(); }
                    PassengerPersona.Birthday = PassengerBirthday;
                    
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Birthday = DriverBirthday;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    DriverPersona.Wanted = true;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = false;
                }
                else if (roll == 3)
                {
                    DateTime DriverBirthday = DriverPersona.Birthday;
                    DateTime PassengerBirthday = PassengerPersona.Birthday;
                    while (GetAge(DriverBirthday) < 25) { DriverBirthday = DriverBirthday.AddYears(-1); }
                    while (GetAge(PassengerBirthday) - GetAge(DriverBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(1); GameFiber.Yield(); }
                    while (GetAge(DriverBirthday) - GetAge(PassengerBirthday) > 4) { PassengerBirthday = PassengerBirthday.AddYears(-1); GameFiber.Yield(); }
                    PassengerPersona.Birthday = PassengerBirthday;
                    PassengerPersona.Wanted = true;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Birthday = DriverBirthday;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    DriverPersona.Wanted = true;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;
                }
                else
                {
                    PassengerPersona.Wanted = false;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = false;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersNotMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;
                }
            }
            else
            {
                int roll = AssortedCalloutsHandler.rnd.Next(5);
                
                Game.LogTrivial("Hookedothervehicles true, roll " + roll.ToString());
                if (roll == 0)
                {
                    PassengerPersona.Wanted = false;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = false;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersNotMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;

                }
                else if (roll == 1)
                {
                    PassengerPersona.Wanted = true;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = true;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersNotMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;

                }
                else if (roll == 2)
                {
                    PassengerPersona.Wanted = false;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = true;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersNotMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;

                }
                else if (roll == 3)
                {
                    PassengerPersona.Wanted = true;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = false;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersNotMarried();
                    DriverShouldBeArrested = true;
                    PassengerShouldBeArrested = true;

                }
                else
                {
                    DateTime DriverBirthday = DriverPersona.Birthday;
                    DateTime PassengerBirthday = PassengerPersona.Birthday;
                    while (GetAge(DriverBirthday) < 26) { DriverBirthday = DriverBirthday.AddYears(-1); }
                    while (GetAge(PassengerBirthday) - GetAge(DriverBirthday) > 3) { PassengerBirthday = PassengerBirthday.AddYears(1); GameFiber.Yield(); }
                    while (GetAge(DriverBirthday) - GetAge(PassengerBirthday) > 3) { PassengerBirthday = PassengerBirthday.AddYears(-1); GameFiber.Yield(); }

                    PassengerPersona.Birthday = PassengerBirthday;
                    PassengerPersona.Wanted = true;
                    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                    DriverPersona.Wanted = false;
                    DriverPersona.ELicenseState = ELicenseState.Valid;
                    DriverPersona.Birthday = DriverBirthday;
                    Functions.SetPersonaForPed(Driver, DriverPersona);
                    GetDriverAndPassengerAnswersMarried();
                    DriverShouldBeArrested = false;
                    PassengerShouldBeArrested = true;
                }
            }
        }

        public void QuestionDriver()
        {
            GameFiber.StartNew(delegate
            {
                if (DriverArrested || DriverPickedUp) { return; }
                Questioning = true;
                if (Driver.IsInAnyVehicle(false))
                {
                    Driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
                    
                    Vector3 directionFromPassengerToPlayer = (Game.LocalPlayer.Character.Position - Driver.Position);
                    directionFromPassengerToPlayer.Normalize();

                    float HeadingToPlayer = MathHelper.ConvertDirectionToHeading(directionFromPassengerToPlayer);
                    Driver.Tasks.AchieveHeading(HeadingToPlayer);
                }
                while (true)
                {
                    if (Solicitation.ActiveSolicitationTrafficStop == null || Functions.IsPedGettingArrested(Driver) || Functions.IsPedArrested(Driver))
                    {
                        break;
                    }

                    int index = SpeechHandler.DisplayAnswers(QuestionsForDriver, Shuffle:false);
                    if (index <= 0)
                    {
                        break;
                    }
                    else
                    {
                        SpeechHandler.HandleSpeech("Driver", DriverAnswers[index - 1]);
                        DriverAnswers.RemoveAt(index - 1);
                        QuestionsForDriver.RemoveAt(index);
                    }
                }
                if (!Driver.IsInAnyVehicle(false))
                {
                    Driver.Tasks.EnterVehicle(veh, 5000, -1).WaitForCompletion();
                }
                Questioning = false;
                Game.DisplayHelp("Press ~b~E ~s~to open the Solicitation Traffic Stop menu.");
            });

        }
        public void QuestionPassenger()
        {
            if (PassengerArrested || PassengerPickedUp) { return; }
            GameFiber.StartNew(delegate
            {
                Questioning = true;
                //WeaponDescriptor currentweapon = Game.LocalPlayer.Character.Inventory.EquippedWeapon;
                
                if (Passenger.IsInAnyVehicle(false))
                {
                    
                    Passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
                    Vector3 directionFromPassengerToPlayer = (Game.LocalPlayer.Character.Position - Passenger.Position);
                    directionFromPassengerToPlayer.Normalize();

                    float HeadingToPlayer = MathHelper.ConvertDirectionToHeading(directionFromPassengerToPlayer);
                    Passenger.Tasks.AchieveHeading(HeadingToPlayer);
                }
                while (true)
                {
                    if (Solicitation.ActiveSolicitationTrafficStop == null || Functions.IsPedGettingArrested(Passenger) || Functions.IsPedArrested(Passenger))
                    {
                        break;
                    }


                    int index = SpeechHandler.DisplayAnswers(QuestionsForPassenger, Shuffle: false);
                    if (index <= 0)
                    {
                        break;
                    }
                    else
                    {
                        SpeechHandler.HandleSpeech("Passenger", PassengerAnswers[index - 1]);
                        PassengerAnswers.RemoveAt(index - 1);
                        QuestionsForPassenger.RemoveAt(index);
                    }

                }
                if (!Passenger.IsInAnyVehicle(false))
                {
                    Passenger.Tasks.EnterVehicle(veh, 5000, 0).WaitForCompletion();
                }
                Game.DisplayHelp("Press ~b~E ~s~to open the Solicitation Traffic Stop menu.");
                Questioning = false;
            });
        }
            
        

        public void EndTrafficStop()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    if (DriverPickedUp || PassengerPickedUp)
                    {
                        if (DriverPickedUp && PassengerPickedUp)
                        {
                            //done
                        }
                        else if (DriverPickedUp)
                        {
                            if (!Functions.IsPedArrested(Passenger) && !Passenger.IsDead)
                            {
                                if (!Passenger.IsInVehicle(veh, false))
                                {
                                    Passenger.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f);
                                    Passenger.Tasks.EnterVehicle(veh, 7000, -1).WaitForCompletion();
                                }

                            }
                        }
                        else if (PassengerPickedUp)
                        {
                            if (!Functions.IsPedArrested(Driver) && !Driver.IsDead)
                            {
                                if (!Driver.IsInVehicle(veh, false))
                                {
                                    Driver.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f);
                                    Driver.Tasks.EnterVehicle(veh, 7000, -1).WaitForCompletion();
                                }
                            }
                        }
                    }



                    else
                    {


                        bool DriverInVehicle = Driver.IsInVehicle(veh, false);


                        bool PassengerInVehicle = Passenger.IsInVehicle(veh, false);

                        if (Functions.IsPedArrested(Driver) && Functions.IsPedArrested(Passenger))
                        {

                        }
                        else if (Functions.IsPedArrested(Driver) && !Passenger.IsDead)
                        {
                            if (!PassengerInVehicle)
                            {
                                Passenger.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f);
                                Passenger.Tasks.EnterVehicle(veh, 7000, -1).WaitForCompletion();
                            }
                        }
                        else if (Functions.IsPedArrested(Passenger) && !Driver.IsDead)
                        {
                            if (!DriverInVehicle)
                            {
                                Driver.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f).WaitForCompletion(3500);
                                Driver.Tasks.EnterVehicle(veh, 7000, -1).WaitForCompletion();
                            }
                        }
                        else
                        {


                            if (!DriverInVehicle || !PassengerInVehicle)
                            {

                                if (!DriverInVehicle && !PassengerInVehicle && !Driver.IsDead && !Passenger.IsDead)
                                {

                                    Driver.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f);
                                    Passenger.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeRight * 2f), veh.Heading - 90f, 1.4f);
                                    GameFiber.Wait(3500);
                                    driverEnter = Driver.Tasks.EnterVehicle(veh, 7000, -1);
                                    passengerEnter = Passenger.Tasks.EnterVehicle(veh, 7000, 0);
                                    while (driverEnter.IsActive || passengerEnter.IsActive)
                                    {
                                        GameFiber.Yield();
                                    }

                                }
                                else if (!DriverInVehicle && !Driver.IsDead)
                                {
                                    Driver.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeLeft * 2f), veh.Heading - 90f, 1.4f).WaitForCompletion(3500);
                                    Driver.Tasks.EnterVehicle(veh, 7000, -1).WaitForCompletion();
                                }
                                else if (!PassengerInVehicle && !Passenger.IsDead)
                                {
                                    Passenger.Tasks.FollowNavigationMeshToPosition(veh.GetOffsetPosition(Vector3.RelativeRight * 2f), veh.Heading - 90f, 1.4f);
                                    Passenger.Tasks.EnterVehicle(veh, 7000, 0).WaitForCompletion();
                                }

                            }

                        }
                    }

                    if (veh.HasDriver && veh.Driver != Game.LocalPlayer.Character)
                    {


                        veh.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight).WaitForCompletion(600);
                        veh.Driver.Tasks.CruiseWithVehicle(18f);
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE( veh.Driver, 786603);
                    }
                }
                catch (Exception e) { Game.LogTrivial(e.ToString()); }
                finally
                {
                    TrafficStopActive = false;
                    ReadyForCleanup = true;
                }



            });
        }
        

        public string DetermineConfirmEndCallItemText()
        {
            string text;
            if (DriverPickedUp || PassengerPickedUp)
            {
                if (DriverPickedUp && PassengerPickedUp)
                {
                    text = "Confirm end call?";
                }
                else if (DriverPickedUp)
                {
                    if (!Functions.IsPedArrested(Passenger))
                    {
                        text = "Confirm release passenger?";
                    }
                    else
                    {
                        text = "Confirm end call?";
                    }
                }
                else if (PassengerPickedUp)
                {
                    if (!Functions.IsPedArrested(Driver))
                    {
                        text = "Confirm release driver?";
                    }
                    else
                    {
                        text = "Confirm end call?";
                    }
                }
                else
                {
                    text = "Confirm end call?"; //should never occur
                }
            }
            else
            {
                if (Functions.IsPedArrested(Driver) && Functions.IsPedArrested(Passenger))
                {
                    text = "Confirm end call?";
                }
                else if (Functions.IsPedArrested(Driver))
                {
                    text = "Confirm release passenger?";
                }
                else if (Functions.IsPedArrested(Passenger))
                {
                    text = "Confirm release driver?";
                }
                else
                {
                    text = "Confirm release suspects?";
                }
            }
            return text;
        }
        
        
        public void OrderDriverOutOfVehicle()
        {
            if (!DriverArrested && !DriverPickedUp)
            {
                Functions.SetPedCantBeArrestedByPlayer(Driver, false);
                if (Driver.IsInAnyVehicle(false))
                {
                    Driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None);

                }
                ArrestCanBeMade = true;
                CreateFight();
            }
        }
        public void OrderPassengerOutOfVehicle()
        {
            if (!PassengerArrested && !PassengerPickedUp)
            {
                Functions.SetPedCantBeArrestedByPlayer(Passenger, false);
                if (Passenger.IsInAnyVehicle(false))
                {
                    Passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                }
                ArrestCanBeMade = true;
                CreateFight();
            }
        }
        public void OrderDriverInVehicle()
        {
            if (!DriverArrested && !DriverPickedUp)
            {
                Functions.SetPedCantBeArrestedByPlayer(Driver, true);
                if (!Driver.IsInAnyVehicle(false))
                {
                    Driver.Tasks.EnterVehicle(veh, 5000, -1);
                }
                if (!Passenger.IsInVehicle(veh, false))
                {
                    ArrestCanBeMade = true;
                }
                else
                {
                    ArrestCanBeMade = false;
                }
            }
        }
        public void OrderPassengerInVehicle()
        {
            if (!PassengerArrested && !PassengerPickedUp)
            {
                Functions.SetPedCantBeArrestedByPlayer(Passenger, true);
                //if (Functions.IsPedStoppedByPlayer(Passenger))
                //{
                //    Passenger = Passenger.ClonePed();
                //    Functions.SetPersonaForPed(Passenger, PassengerPersona);
                //}
                if (!Passenger.IsInAnyVehicle(false))
                {
                    Passenger.Tasks.EnterVehicle(veh, 5000, 0);
                }
                if (!Driver.IsInVehicle(veh, false))
                {
                    ArrestCanBeMade = true;
                }
                else
                {
                    ArrestCanBeMade = false;
                }
            }
        }
        public void RequestAssistance()
        {
            GameFiber.StartNew(delegate
            {
                BackupUnit = Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                BackupRequested = true;
            });
            //Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
        }

        private void CreateFight()
        {
            if (PassengerShouldBeArrested || DriverShouldBeArrested)
            {
                if (AssortedCalloutsHandler.rnd.Next(11) <= 1 && !FightCreated && !PursuitCreated)
                {
                    Game.LogTrivial("Creating fight");

                    GameFiber.StartNew(delegate
                    {
                        if (!PassengerArrested && PassengerShouldBeArrested)
                        {
                            Game.LogTrivial("Adding passenger to fight");
                            Passenger.Inventory.GiveNewWeapon(Weapons[AssortedCalloutsHandler.rnd.Next(Weapons.Length)], -1, true);

                            Passenger.RelationshipGroup = "ROBBERS";
                            Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                            
                            Passenger.Tasks.FightAgainstClosestHatedTarget(45f);
                        }

                        if (!DriverArrested && DriverShouldBeArrested)
                        {
                            Game.LogTrivial("Adding driver to fight");
                            Driver.Inventory.GiveNewWeapon(Weapons[AssortedCalloutsHandler.rnd.Next(Weapons.Length)], -1, true);

                            Driver.RelationshipGroup = "ROBBERS";
                            Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                            Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                            
                            Driver.Tasks.FightAgainstClosestHatedTarget(45f);

                        }
                        Functions.SetPedCantBeArrestedByPlayer(Passenger, false);
                        Functions.SetPedCantBeArrestedByPlayer(Driver, false);
                        FightCreated = true;
                        TrafficStopActive = false;

                    });
                }
            }
        }
    }
}
