using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using System.Windows.Forms;
using System.IO;
using System.Management;
using System.Threading;
using System.Net;
using Rage.Native;
using RAGENativeUI.Elements;
using System.Reflection;

[assembly: Rage.Attributes.Plugin("Assorted Callouts", Description = "INSTALL IN GTAV/PLUGINS/LSPDFR. Adds high quality callouts to LSPDFR", Author = "Albo1125")]
namespace AssortedCallouts
{
    public class EntryPoint
    {
        public static void Main()
        {
            Game.DisplayNotification("You have installed Assorted Callouts incorrectly. You must install it in the GTAV/Plugins/LSPDFR folder. It will then be automatically loaded when going on duty - you must NOT load it yourself via RAGEPluginHook. This is also explained in the Readme and Documentation. You will now be redirected to the installation tutorial.");
            GameFiber.Wait(5000);
            System.Diagnostics.Process.Start("https://youtu.be/af434m72rIo");
            return;
        }
    }

    internal static class AssortedCalloutsHandler
    {      
        public static Random rnd = new Random(MathHelper.GetRandomInteger(100, 100000));
        public enum EnglishTypes { BritishEnglish, AmericanEnglish };
        public static EnglishTypes English;
        internal static void Initialise()
        {
            GameFiber.StartNew(delegate
            {
                Game.LogTrivial("Assorted Callouts, developed by Albo1125, has been loaded successfully!");
                GameFiber.Wait(6000);
                Game.DisplayNotification("~b~Assorted Callouts~s~, developed by ~b~Albo1125, ~s~has been loaded ~g~successfully.");
            });
            Game.LogTrivial("Assorted Callouts is not in beta.");
            LoadInitialisationFileValues();
            RegisterCallouts();
        }

        public static InitializationFile initialiseFile()
        {
            InitializationFile ini = new InitializationFile("Plugins/LSPDFR/Assorted Callouts.ini");
            ini.Create();
            return ini;
        }

        private static void LoadInitialisationFileValues()
        {
            try {
                DetermineUnitBeatStrings();
                TalkKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Global Keybindings", "TalkKey", "Y"));
                HostageRescueKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Bank Heist", "RescueHostageKey", "D0"));
                FollowKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Bank Heist", "FollowKey", "I"));
                ToggleAlarmKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Bank Heist", "ToggleAlarmKey", "F11"));
                PetrolTheftEnabled = initialiseFile().ReadBoolean("Petrol Theft", "PetrolTheftEnabled", true);
                PetrolTheftFrequency = initialiseFile().ReadInt32("Petrol Theft", "PetrolTheftFrequency", 2);
                BankHeistEnabled = initialiseFile().ReadBoolean("Bank Heist", "BankHeistEnabled", true);
                BankHeistVoiceOvers = initialiseFile().ReadBoolean("Bank Heist", "BankHeistVoiceOvers", true);
                BankHeistFrequency = initialiseFile().ReadInt32("Bank Heist", "BankHeistFrequency", 1);
                TrafficStopBackupEnabled = initialiseFile().ReadBoolean("Traffic Stop Backup", "TrafficStopBackupEnabled", true);
                TrafficStopBackupFrequency = initialiseFile().ReadInt32("Traffic Stop Backup", "TrafficStopBackupFrequency", 2);
                OrganisedStreetRaceEnabled = initialiseFile().ReadBoolean("Organised Street Race", "OrganisedStreetRaceEnabled", true);
                OrganisedStreetRaceFrequency = initialiseFile().ReadInt32("Organised Street Race", "OrganisedStreetRaceFrequency", 2);
                PersonKnifeEnabled = initialiseFile().ReadBoolean("Person with a knife", "PersonKnifeEnabled", true);
                PersonKnifeFrequency = initialiseFile().ReadInt32("Person with a knife", "PersonKnifeFrequency", 2);
                StolenPoliceVehicleEnabled = initialiseFile().ReadBoolean("Stolen Police Vehicle", "StolenPoliceVehicleEnabled", true);
                StolenPoliceVehicleFrequency = initialiseFile().ReadInt32("Stolen Police Vehicle", "StolenPoliceVehicleFrequency", 1);
                SolicitationEnabled = initialiseFile().ReadBoolean("Solicitation", "SolicitationEnabled", true);
                SolicitationFrequency = initialiseFile().ReadInt32("Solicitation", "SolicitationFrequency", 2);

                StoreRobberyEnabled = initialiseFile().ReadBoolean("StoreRobbery", "StoreRobberyEnabled", true);
                StoreRobberyFrequency = initialiseFile().ReadInt32("StoreRobbery", "StoreRobberyFrequency", 2);

                PrisonerTransportEnabled= initialiseFile().ReadBoolean("Prisoner Transport Required", "PrisonerTransportRequiredEnabled", true);
                PrisonerTransportFrequency= initialiseFile().ReadInt32("Prisoner Transport Required", "PrisonerTransportRequiredFrequency", 2);

                ShopliftingEnabled = initialiseFile().ReadBoolean("Shoplifting", "ShopliftingEnabled", true);
                ShopliftingFrequency = initialiseFile().ReadInt32("Shoplifting", "ShopliftingFrequency", 2);

                HotPursuitEnabled = initialiseFile().ReadBoolean("HotPursuit", "HotPursuitEnabled", true);
                HotPursuitFrequency = initialiseFile().ReadInt32("HotPursuit", "HotPursuitFrequency", 2);

                English = initialiseFile().ReadEnum<EnglishTypes>("General", "Language", EnglishTypes.BritishEnglish);
                OtherUnitTakingCallAudio = initialiseFile().ReadBoolean("Bank Heist", "OtherUnitTakingCallAudio", true);
                EndCallKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Global Keybindings", "EndCallKey", "End"));

                string[] UnmarkedPoliceVehicleModelStrings = initialiseFile().ReadString("Solicitation", "UnmarkedVehicles", "POLICE4").Split(new[] { ',', ' ' } , StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in UnmarkedPoliceVehicleModelStrings)
                {
                    if (new Model(s).IsValid)
                    {
                        UnmarkedPoliceVehicleModels.Add(s);
                        Game.LogTrivial("Adding unmarked police vehicle model:" + s);
                    }
                }
                SolicitationNightOnly = initialiseFile().ReadBoolean("Solicitation", "NightOnly", false);
                IllegalImmigrantsInTruckEnabled = initialiseFile().ReadBoolean("Illegal Immigrants in Truck", "IllegalImmigrantsInTruckEnabled");
                IllegalImmigrantsInTruckFrequency  = initialiseFile().ReadInt32("Illegal Immigrants in Truck", "IllegalImmigrantsInTruckFrequency", 2);

                LightsOffForELSCars = initialiseFile().ReadBoolean("Traffic Stop Backup", "LightsOffForELSCars", false);

            }
            catch (Exception e)
            {
                TalkKey = Keys.Y;
                HostageRescueKey = Keys.D0;
                ToggleAlarmKey = Keys.F11;
                FollowKey = Keys.I;
                PetrolTheftEnabled = true;
                PetrolTheftFrequency = 2;
                BankHeistEnabled = true;
                BankHeistVoiceOvers = true;
                BankHeistFrequency = 1;
                TrafficStopBackupFrequency = 2;
                TrafficStopBackupEnabled = true;
                English = EnglishTypes.BritishEnglish;
                StolenPoliceVehicleEnabled = true;
                StolenPoliceVehicleFrequency = 1;
                Game.LogTrivial(e.ToString());
                Game.LogTrivial("Error loading Assorted Callouts INI. Replace with default; Default values set.");
                Game.DisplayNotification("~h~~r~Error loading Assorted Callouts INI. Replace with default; Default values set.");

            }
        }
        private static void RegisterCallouts()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
            if (PetrolTheftEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.PetrolTheft));
                for (int i = 1; i < PetrolTheftFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.PetrolTheft));
                }
            }
            if (BankHeistEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.BankHeist));
                for (int i = 1; i < BankHeistFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.BankHeist));
                }
            }
            if (TrafficStopBackupEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.TrafficStopBackup));
                for (int i = 1; i < TrafficStopBackupFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.TrafficStopBackup));
                }
            }
            if (StolenPoliceVehicleEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.StolenPoliceVehicle));
                for (int i = 1; i < StolenPoliceVehicleFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.StolenPoliceVehicle));
                }
            }
            if (SolicitationEnabled)
            {
                AssortedCallouts.Callouts.Solicitation.Solicitation.LoadSolicitationMenus();
                Functions.RegisterCallout(typeof(Callouts.Solicitation.Solicitation));
                for (int i = 1; i < SolicitationFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.Solicitation.Solicitation));
                }
            }
            if (PersonKnifeEnabled)
            {

                Functions.RegisterCallout(typeof(Callouts.PersonWithKnife));
                for (int i = 1; i < PersonKnifeFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.PersonWithKnife));
                }
            }
            if (OrganisedStreetRaceEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.OrganisedStreetRace));
                for (int i = 1; i < OrganisedStreetRaceFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.OrganisedStreetRace));
                }
            }
            if (IllegalImmigrantsInTruckEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.IllegalImmigrantsInTruck));
                for (int i = 1; i < IllegalImmigrantsInTruckFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.IllegalImmigrantsInTruck));
                }
            }
            if (StoreRobberyEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.StoreRelated.StoreRobbery));
                for (int i = 1; i < StoreRobberyFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.StoreRelated.StoreRobbery));
                }
            }
            if (PrisonerTransportEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.PrisonerTransportRequired));
                for (int i = 1; i < PrisonerTransportFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.PrisonerTransportRequired));
                }
            }
            if (ShopliftingEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.StoreRelated.Shoplifting));
                for (int i = 1; i < ShopliftingFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.StoreRelated.Shoplifting));
                }
            }
            if (HotPursuitEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.HotPursuit));
                for (int i = 1; i < HotPursuitFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.HotPursuit));
                }
            }

            Callouts.StoreRelated.Store.InitialiseStores();
            try
            {
                DateTime worldtime = World.DateTime;
            }
            catch (Exception e)
            {
                NativeFunction.CallByName<uint>("SET_CLOCK_DATE", NativeFunction.CallByName<int>("GET_CLOCK_DAY_OF_MONTH"), 1, NativeFunction.CallByName<int>("GET_CLOCK_YEAR"));
            }
            GameFiber.StartNew(delegate
            {
                GameFiber.Wait(3000);
                while (true)
                {
                    GameFiber.Yield();
                }
            });

        }

        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName(); if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0) { return true; }
                }
            }
            return false;
        }
        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args) { foreach (Assembly assembly in Functions.GetAllUserPlugins()) { if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower())) { return assembly; } } return null; }

        private static void DetermineUnitBeatStrings()
        {
            string Division = "DIV_" + initialiseFile().ReadInt32("General", "Division", 1).ToString("D2");
            string UnitType = initialiseFile().ReadString("General", "UnitType", "ADAM").ToUpper();
            string Beat = "BEAT_" + initialiseFile().ReadInt32("General", "Beat", 12).ToString("D2");
            DivisionUnitBeatAudioString = Division + " " + UnitType + " " + Beat;
            DivisionUnitBeat = initialiseFile().ReadString("General", "Division", "1") + "-" + initialiseFile().ReadString("General", "UnitType", "ADAM") + "-" + initialiseFile().ReadString("General", "Beat", "12");
        }
        public static bool HotPursuitEnabled = true;
        public static int HotPursuitFrequency = 2;
        public static bool ShopliftingEnabled = true;
        public static int ShopliftingFrequency = 2;
        public static bool PrisonerTransportEnabled = true;
        public static int PrisonerTransportFrequency = 2;
        public static bool StoreRobberyEnabled = true;
        public static int StoreRobberyFrequency = 2;
        public static bool IllegalImmigrantsInTruckEnabled = true;
        public static int IllegalImmigrantsInTruckFrequency = 2;
        public static bool SolicitationEnabled = true;
        public static int SolicitationFrequency = 2;
        public static bool SolicitationNightOnly = false;
        public static int PersonKnifeFrequency = 2;
        public static bool PersonKnifeEnabled = true;
        public static bool StolenPoliceVehicleEnabled { get; set; }
        public static int StolenPoliceVehicleFrequency { get; set; }
        public static bool TrafficStopBackupEnabled { get; set; }
        public static int TrafficStopBackupFrequency { get; set; }
        public static bool PetrolTheftEnabled { get; set; }
        public static bool BankHeistEnabled { get; set; }
        public static int PetrolTheftFrequency { get; set; }
        public static bool BankHeistVoiceOvers { get; set; }
        public static int BankHeistFrequency { get; set; }
        public static Keys TalkKey { get; set; }
        public static Keys FollowKey { get; set; }
        public static Keys HostageRescueKey { get; set; }
        public static Keys ToggleAlarmKey { get; set; }
        public static bool OtherUnitTakingCallAudio = true;
        public static Keys EndCallKey = Keys.End;
        public static int OrganisedStreetRaceFrequency = 2;
        public static bool OrganisedStreetRaceEnabled = true;
        
        public static List<Model> UnmarkedPoliceVehicleModels = new List<Model>() { "POLICE4" };
        

        public static string DivisionUnitBeat = "1-ADAM-12";
        public static string DivisionUnitBeatAudioString = "DIV_01 ADAM BEAT_12";

        /// <summary>
        /// When true, AI officers will not use their lights (and therefore siren) if they are driving
        /// an ELS vehicle. This prevents, for example, officers sitting on a traffic stop blaring the
        /// siren. By the way, ELS people, plz can we have api 4 control siren thx. :P
        /// </summary>
        public static bool LightsOffForELSCars = false;

        public static KeysConverter kc = new KeysConverter();
        
    }
}