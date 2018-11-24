using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;

namespace AssortedCallouts.Callouts
{

    internal abstract class AssortedCallout : Callout
    {
        public Model[] GroundVehiclesToSelectFrom = new Model[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR",
            "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO",
        "BATI", "BATI2", "AKUMA", "BAGGER", "DOUBLE", "NEMESIS", "HEXER"};
        public Model[] CarsToSelectFrom = new Model[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR",
            "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };
        public Model[] MotorBikesToSelectFrom = new Model[] { "BATI", "BATI2", "AKUMA", "BAGGER", "DOUBLE", "NEMESIS", "HEXER" };

        public enum SuspectStates { InPursuit, Arrested, Dead, Escaped };

        public Ped Suspect;
        public Vehicle SuspectCar;
        public Vector3 SpawnPoint;
        public Blip SuspectBlip;
        public LHandle Pursuit;
        public bool CalloutFinished = false;
        public Vector3 SearchAreaLocation;
        public Blip SearchArea;
        public float SpawnHeading;
        public Guid CalloutID;
        public bool ComputerPlusRunning = false;

        public override void OnCalloutNotAccepted()
        {

            base.OnCalloutNotAccepted();
            if (ComputerPlusRunning)
            {
                API.ComputerPlusFuncs.AssignCallToAIUnit(CalloutID);
                
            }
            if (Suspect.Exists()) { Suspect.Delete(); }
            if (SuspectCar.Exists()) { SuspectCar.Delete(); }
            if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
            if (AssortedCalloutsHandler.OtherUnitTakingCallAudio)
            {
                Functions.PlayScannerAudio("OTHER_UNIT_TAKING_CALL");
            }

        }
        public override bool OnBeforeCalloutDisplayed()
        {

            return base.OnBeforeCalloutDisplayed();
        }
        public override bool OnCalloutAccepted()
        {
            if (ComputerPlusRunning)
            {
                API.ComputerPlusFuncs.SetCalloutStatusToUnitResponding(CalloutID);
                Game.DisplayHelp("Further details about this call can be checked using ~b~Computer+.");
            }
            return base.OnCalloutAccepted();
        }


        public override void End()
        {
            base.End();
            try
            {

                if (SearchArea.Exists()) { SearchArea.Delete(); }
                if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
                if (!CalloutFinished)
                {
                    if (Suspect.Exists()) { Suspect.Delete(); }
                    if (SuspectCar.Exists()) { SuspectCar.Delete(); }
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.CancelCallout(CalloutID);
                    }
                }
                else
                {
                    if (ComputerPlusRunning)
                    {
                        API.ComputerPlusFuncs.ConcludeCallout(CalloutID);
                    }
                    if (Suspect.Exists())
                    {
                        if (!Functions.IsPedArrested(Suspect))
                        {
                            Suspect.Dismiss();
                        }
                    }
                    if (SuspectCar.Exists()) { SuspectCar.Dismiss(); }
                    
                }
            }
            catch (Exception e) { }
        }
    }
}
