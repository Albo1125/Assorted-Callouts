using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using AssortedCallouts.Extensions;
using LSPD_First_Response.Mod.Callouts;
using System.IO;
using Rage.Native;
using System.Drawing;
using System.Windows.Forms;
using RAGENativeUI.Elements;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts.Callouts
{
    [CalloutInfo("Bank Heist", CalloutProbability.Low)]
    internal class BankHeist : AssortedCallout
    {
        private Vector3 PacificBank = new Vector3(231.5777f, 215.1532f, 106.2815f);
        private const ulong _DOOR_CONTROL = 0x9b12f9a24fabedb0;
        private List<Ped> Robbers = new List<Ped>();

        private bool fighting = false;
        private bool CalloutRunning = false;
        private System.Media.SoundPlayer AlarmPlayer = new System.Media.SoundPlayer("LSPDFR/audio/scanner/Assorted Callouts Audio/ALARM_BELL.wav");
        private bool AudioStateChanged = false;

        private bool AlarmPlaying = false;

        private bool TalkedToWells = false;
        private bool HandlingRespawn = false;
        private bool EvaluatedWithWells = false;
        private int TimesDied = 0;
        private int FightingPacksUsed = 0;
        private int RobbersKilled = 0;
        private AudioState CurrentAudioState = AudioState.None;
        private bool DoneFighting = false;
        private Blip SideDoorBlip;
        private bool TalkedToWells2nd = false;
        private bool NegotiationResultSurrender;
        private bool Surrendering = false;
        private bool SWATFollowing = false;
        private int SWATUnitsdied = 0;


        private bool FightingPrepared = false;

        private bool SurrenderComplete = false;

        private Vector3[] PacificBankDoors = new Vector3[] { new Vector3(229.7984f, 214.4494f, 105.5554f), new Vector3(258.3625f, 200.4897f, 104.9758f) };
        private Vector3[] PacificBankInsideChecks = new Vector3[] { new Vector3(235.9762f, 220.6012f, 106.2868f), new Vector3(238.3628f, 214.8286f, 106.2868f), new Vector3(261.084f, 208.12f, 106.2832f), new Vector3(235.2972f, 217.1385f, 106.2867f) };
        private Vector3[] PacificBankDoorsInside = new Vector3[] { new Vector3(259.5908f, 204.1841f, 106.2832f), new Vector3(232.4167f, 215.6826f, 106.2866f) };

        private Vector3 InsideBankVault = new Vector3(252.3106f, 222.5586f, 101.6834f);
        private Vector3 OutsideBankVault = new Vector3(257.3354f, 225.5874f, 101.8757f);

        private Vector3 HostageSafeLocation = new Vector3(241.8676f, 176.3772f, 105.1341f);
        private float HostageSafeHeading = 158.8192f;
        //Police Barriers
        private List<Ped> BarrierPeds = new List<Ped>();
        private List<Rage.Object> InvisWalls = new List<Rage.Object>();
        private List<Rage.Object> Barriers = new List<Rage.Object>();
        private List<Vector3> BarrierLocations = new List<Vector3>() { new Vector3(215.393f, 203.157f, 104.454f), new Vector3(215.1232f, 205.6814f, 104.4652f), new Vector3(218.4388f, 196.256f, 104.5912f), new Vector3(233.0477f, 191.5893f, 104.3578f),
        new Vector3(235.1332f, 191.1562f, 104.3189f), new Vector3(237.6775f, 190.3424f, 104.2726f), new Vector3(247.1391f, 188.03f, 104.0998f), new Vector3(244.9249f, 187.9552f, 104.1492f), new Vector3(218.238f, 213.5867f, 104.4652f), new Vector3(218.7885f, 216.0675f, 104.4652f), new Vector3(219.6092f, 218.8511f, 104.4652f) };
        private List<float> BarrierHeadings = new List<float>() { 286.3633f, 290.2363f, 344.4589f, 346.3031f, 341.4462f, 342.168f, 25.01121f, 1.558372f, 255.0954f, 255.0954f, 267.3944f };

        //Police Vehicles
        private List<Vector3> POLICECarLocations = new List<Vector3>() { new Vector3(222.4914f, 196.139f, 105.2151f), new Vector3(228.7804f, 193.9648f, 105.0773f), new Vector3(250.7617f, 190.7597f, 104.5666f) };
        private List<float> POLICECarHeadings = new List<float>() { 251.93f, 70.06104f, 291.0875f };
        private List<Vehicle> AllSpawnedPoliceVehicles = new List<Vehicle>();

        private List<Vector3> POLICE2CarLocations = new List<Vector3>() { new Vector3(216.4797f, 199.8008f, 105.1088f), new Vector3(216.3862f, 209.5035f, 105.1084f) };
        private List<float> POLICE2CarHeadings = new List<float>() { 11.46685f, 339.02f };

        private List<Vector3> POLICE3CarLocations = new List<Vector3>() { new Vector3(241.5773f, 190.1744f, 104.9979f), new Vector3(223.8036f, 221.5969f, 105.2692f) };
        private List<float> POLICE3CarHeadings = new List<float>() { 246.1062f, 305.6254f };

        //Police Officers
        private static string[] LSPDModels = new string[] { "s_m_y_cop_01", "S_F_Y_COP_01" };

        private List<Vector3> PoliceOfficersStandingLocations = new List<Vector3>() { new Vector3(215.3605f, 199.1968f, 105.542f), new Vector3(214.3272f, 203.5561f, 105.4791f), new Vector3(239.7187f, 189.4415f, 105.2328f), new Vector3(217.9366f, 215.7689f, 105.5233f) };
        private List<float> PoliceOfficersStandingHeadings = new List<float>() { 116.6588f, 121.1613f, 154.7706f, 66.40781f };
        private List<Ped> PoliceOfficersStandingSpawned = new List<Ped>();

        private List<Vector3> PoliceOfficersAimingLocations = new List<Vector3>() { new Vector3(215.3038f, 210.3652f, 105.5509f), new Vector3(229.6182f, 192.2897f, 105.4265f), new Vector3(223.2215f, 194.5566f, 105.5815f), new Vector3(242.2608f, 188.373f, 105.1962f), new Vector3(252.175f, 189.5349f, 104.8857f), new Vector3(221.073f, 221.157f, 105.4611f) };
        private List<float> PoliceOfficersAimingHeadings = new List<float>() { 284.9829f, 352.2892f, 338.3747f, 302.2641f, 333.9143f, 237.1773f };
        private List<Ped> PoliceOfficersAimingSpawned = new List<Ped>();

        private List<Ped> PoliceOfficersArresting = new List<Ped>();

        private List<Ped> PoliceOfficersSpawned = new List<Ped>();
        private List<Ped> PoliceOfficersTargetsToShoot = new List<Ped>();

        //Swat teams
        private List<Vector3> SWATTeam1Locations = new List<Vector3>() { new Vector3(260.5645f, 200.5741f, 104.9401f), new Vector3(262.1003f, 200.0121f, 104.9125f), new Vector3(256.6042f, 202.0044f, 105.0125f), new Vector3(255.1498f, 202.5428f, 105.0388f), new Vector3(253.9746f, 203.0882f, 105.0599f), new Vector3(263.3704f, 199.6684f, 104.8904f) };
        private List<float> SWATTeam1Headings = new List<float>() { 71.64834f, 68.8295f, 251.649f, 248.7861f, 248.8271f, 68.8268f };
        private List<Vector3> SWATTeam2Locations = new List<Vector3>() { new Vector3(230.4205f, 222.8963f, 105.5488f), new Vector3(229.3888f, 219.9169f, 105.5496f), new Vector3(230.0146f, 221.7818f, 105.549f), new Vector3(234.2444f, 210.1959f, 105.4067f), new Vector3(235.6489f, 209.7039f, 105.3825f), new Vector3(236.8931f, 209.2961f, 105.3615f) };
        private List<float> SWATTeam2Headings = new List<float>() { 159.7311f, 159.7311f, 159.7311f, 68.82679f, 68.82679f, 68.82679f };

        private List<Ped> SWATTeam1 = new List<Ped>();
        private List<Ped> SWATTeam2 = new List<Ped>();
        private string[] SWATWeapons = new string[] { "WEAPON_CARBINERIFLE", "WEAPON_ASSAULTSMG" };
        private List<Ped> SWATUnitsSpawned = new List<Ped>();

        private string[] Grenades = new string[] { "WEAPON_GRENADE", "WEAPON_SMOKEGRENADE" };

        //Hostages
        private List<Vector3> HostagesLocations = new List<Vector3>() { new Vector3(253.4743f, 217.7294f, 106.2868f), new Vector3(240.2256f, 223.8581f, 106.2869f), new Vector3(247.4731f, 215.7981f, 106.2869f), new Vector3(235.0374f, 218.5802f, 110.2827f), new Vector3(243.3186f, 210.8154f, 110.283f), new Vector3(265.409f, 214.4903f, 110.2873f), new Vector3(256.4999f, 225.638f, 106.2868f), new Vector3(257.9439f, 227.4617f, 101.6833f) };
        private List<float> HostagesHeadings = new List<float>() { 26.3351f, 333.7643f, 308.5362f, 183.8941f, 69.50799f, 250.7003f, 344.9573f, 66.61107f };
        private List<Ped> SpawnedHostages = new List<Ped>();
        private string[] HostageModels = new string[] { "A_F_M_BUSINESS_02", "A_M_M_BUSINESS_01", "A_F_Y_FEMALEAGENT", "A_M_Y_BUSINESS_03" };
        private List<Ped> RescuedHostages = new List<Ped>();
        private List<Ped> SafeHostages = new List<Ped>();
        private List<Ped> AllHostages = new List<Ped>();
        private int AliveHostagesCount = 0;
        private int SafeHostagesCount = 0;
        private int TotalHostagesCount = 0;


        //RIOT vans
        private List<Vector3> RiotLocations = new List<Vector3>() { new Vector3(224.1989f, 207.5056f, 105.1199f), new Vector3(263.7726f, 193.397f, 104.4452f) };
        private List<float> RiotHeadings = new List<float>() { 193.6453f, 214.4147f };
        private List<Vehicle> RiotVans = new List<Vehicle>();

        //Robbers
        private List<Vector3> RobbersNegotiationLocations = new List<Vector3>() { new Vector3(235.2906f, 217.1142f, 106.2867f), new Vector3(254.4529f, 217.6757f, 106.2868f), new Vector3(243.2524f, 222.3944f, 106.2868f), new Vector3(257.5506f, 223.6651f, 106.2863f), new Vector3(242.9586f, 213.7329f, 110.283f),
        new Vector3(261.4425f, 223.766f, 101.6833f) ,new Vector3(266.9025f, 219.2729f, 104.8833f) };
        private List<float> RobbersNegotiationHeadings = new List<float>() { 109.0629f, 230.5565f, 78.30953f, 139.71f, 333.9886f, 246.4011f, 93.847f, };

        private List<Vector3> RobbersSneakyLocations = new List<Vector3>() { new Vector3(235.5733f, 228.3068f, 110.2827f), new Vector3(256.7757f, 205.0848f, 110.283f), new Vector3(265.3547f, 222.4385f, 101.6833f), new Vector3(263.0323f, 215.4664f, 110.2877f), new Vector3(255.1933f, 222.045f, 106.2869f), new Vector3(238.6139f, 228.2485f, 106.2834f), new Vector3(238.6164f, 227.2258f, 110.2827f), new Vector3(261.3226f, 210.6962f, 110.2877f), new Vector3(265.8036f, 215.6155f, 110.283f) };
        private List<float> RobbersSneakyHeadings = new List<float>() { 248.8268f, 339.1755f, 153.5341f, 159.1769f, 341.1396f, 69.37666f, 68.82679f, 166.2358f, 334.0478f };
        private List<Ped> RobbersSneakySpawned = new List<Ped>();
        private string[] RobbersSneakyWeapons = new string[] { "WEAPON_PISTOL50", "WEAPON_KNIFE", "WEAPON_ASSAULTSMG", "WEAPON_ASSAULTSHOTGUN", "WEAPON_CROWBAR", "WEAPON_HAMMER", "WEAPON_ASSAULTRIFLE" };

        private List<Vector3> RobbersVaultLocations = new List<Vector3>() { new Vector3(253.9261f, 221.6735f, 101.6834f), new Vector3(252.686f, 221.9205f, 101.6834f), new Vector3(251.4069f, 222.5131f, 101.6834f) };
        private List<float> RobbersVaultHeadings = new List<float>() { 353.9462f, 343.3658f, 335.4267f };
        private List<Ped> RobbersVault = new List<Ped>();

        private Vector3 MiniGunRobberLocation = new Vector3(267.0747f, 224.5822f, 110.2829f);
        private Vector3 MiniGunFireLocation = new Vector3(257.7627f, 223.3801f, 106.2863f);
        private float MiniGunRobberHeading = 92.93042f;
        private Ped MiniGunRobber;
        private bool MiniGunRobberFiring = false;
        private Vector3 BehindGlassDoorLocation = new Vector3(265.4374f, 217.0231f, 110.283f);

        private Ped Maria;
        private Model MariaModel = new Model("A_F_Y_EASTSA_01");

        private Ped MariaCop;
        private Vector3 MariaSpawnPoint = new Vector3(179.9932f, 115.8559f, 94.61918f);
        private float MariaSpawnHeading = 338.6956f;
        private Vector3 MariaCopDestination = new Vector3(235.0675f, 180.2976f, 104.8821f);
        private Vehicle MariaCopCar;
        //Robbers when assault
        private List<Vector3> RobbersAssaultLocations = new List<Vector3>() { new Vector3(267.0549f, 221.0715f, 110.283f), new Vector3(238.9634f, 234.4014f, 108.0783f), new Vector3(237.994f, 225.1579f, 110.2827f), new Vector3(254.5219f, 209.5221f, 110.283f), new Vector3(263.1249f, 208.023f, 106.2832f), new Vector3(259.1342f, 209.493f, 106.2832f), new Vector3(262.4411f, 208.0968f, 110.2865f),
        new Vector3(254.8814f, 226.924f, 101.7847f), new Vector3(261.9492f, 203.3163f, 106.2832f)};
        private List<float> RobbersAssaultHeadings = new List<float>() { 62.3679f, 167.0135f, 251.0168f, 339.1998f, 150.2972f, 238.0847f, 61.37086f, 237.4072f, 69.37666f };

        //When surrender
        private List<Vector3> RobbersSurrenderLocations = new List<Vector3>() { new Vector3(230.8764f, 207.1935f, 105.4408f), new Vector3(233.9847f, 206.0195f, 105.3878f), new Vector3(237.1756f, 205.175f, 105.3347f), new Vector3(239.5613f, 203.8999f, 105.2908f), new Vector3(242.2058f, 203.3801f, 105.2473f), new Vector3(245.231f, 201.896f, 105.1919f), new Vector3(248.4907f, 201.2163f, 105.1362f) };
        private List<float> RobbersSurrenderHeadings = new List<float>() { 146.3448f, 148.8101f, 152.9936f, 175.8113f, 170.111f, 151.4013f, 138.7258f };



        private string[] RobbersWeapons = new string[] { "WEAPON_SAWNOFFSHOTGUN", "WEAPON_ASSAULTRIFLE", "WEAPON_PUMPSHOTGUN", "WEAPON_ASSAULTSHOTGUN", "WEAPON_ADVANCEDRIFLE" };

        //Captain Wells
        private Ped CaptainWells;
        private Blip CaptainWellsBlip;
        private Vector3 CaptainWellsLocation = new Vector3(261.6116f, 192.8469f, 104.8786f);
        private float CaptainWellsHeading = 49.73652f;

        //EMS &Fire
        private List<Vector3> AmbulanceLocations = new List<Vector3>() { new Vector3(260.8994f, 166.494f, 104.5317f), new Vector3(239.1898f, 172.3954f, 104.8571f) };
        private List<float> AmbulanceHeadings = new List<float>() { 199.5887f, 158.5571f };
        private List<Vehicle> AmbulancesList = new List<Vehicle>();

        private List<Vector3> ParamedicLocations = new List<Vector3>() { new Vector3(242.0103f, 174.1728f, 105.1191f), new Vector3(243.9722f, 170.8235f, 105.0307f) };
        private List<float> ParamedicHeadings = new List<float>() { 330.9329f, 341.8453f };
        private List<Ped> ParamedicsList = new List<Ped>();

        private List<Vector3> FireTruckLocations = new List<Vector3>() { new Vector3(246.3588f, 167.8176f, 104.9527f) };
        private List<float> FireTruckHeadings = new List<float>() { 249.4772f };
        private List<Vehicle> FireTrucksList = new List<Vehicle>();

        private List<Ped> FiremenList = new List<Ped>();

        //All callout entities
        private List<Entity> AllBankHeistEntities = new List<Entity>();

        private void LoadModels()
        {
            foreach (string s in LSPDModels)
            {
                GameFiber.Yield();
                new Model(s).Load();
            }
            foreach (string s in HostageModels)
            {
                GameFiber.Yield();
                new Model(s).Load();
            }
            new Model("s_m_y_robber_01").Load();

        }
        //Dialogue 

        private List<string> AlarmAnswers = new List<string>() { "You: Yeah definitely, I can't hear myself think with that thing going.", "You: Nah, it doesn't bother me that much." };
        private Rage.Object MobilePhone;
        private void ToggleMobilePhone(Ped ped, bool toggle)
        {

            if (toggle)
            {
                NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, false);
                ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), -1, true);
                MobilePhone = new Rage.Object(new Model("prop_police_phone"), new Vector3(0, 0, 0));
                int boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(ped, (int)PedBoneId.RightPhHand);
                NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(MobilePhone, ped, boneIndex, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, false, 2, 1);
                ped.Tasks.PlayAnimation("cellphone@", "cellphone_call_listen_base", 1.3f, AnimationFlags.Loop | AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask);

            }
            else
            {
                NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, true);
                ped.Tasks.Clear();
                if (GameFiber.CanSleepNow)
                {
                    GameFiber.Wait(800);
                }
                if (MobilePhone.Exists()) { MobilePhone.Delete(); }
            }
        }

        private void GetMaria()
        {
            Game.LocalPlayer.Character.IsPositionFrozen = true;
            MariaCopCar = new Vehicle("POLICE", MariaSpawnPoint, MariaSpawnHeading);
            Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(MariaCopCar);

            MariaCopCar.IsSirenOn = true;
            MariaCop = MariaCopCar.CreateRandomDriver();
            MariaCop.MakeMissionPed();
            Maria = new Ped(MariaModel, MariaSpawnPoint, MariaSpawnHeading);
            Maria.MakeMissionPed();
            Maria.WarpIntoVehicle(MariaCopCar, 0);
            AllBankHeistEntities.Add(Maria);
            AllBankHeistEntities.Add(MariaCop);
            AllBankHeistEntities.Add(MariaCopCar);

            MariaCop.Tasks.DriveToPosition(MariaCopDestination, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
            while (true)
            {
                GameFiber.Yield();
                if (Vector3.Distance(MariaCopCar.Position, MariaCopDestination) < 6f)
                {
                    break;
                }
            }
            Maria.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
            Maria.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeRight * 1.5f), Game.LocalPlayer.Character.Heading, 1.9f).WaitForCompletion(60000);
            Game.LocalPlayer.Character.IsPositionFrozen = false;
        }

        private void NegotiationIntro()
        {
            SpeechHandler.YouLineAudioCount = 1;
            ToggleMobilePhone(Game.LocalPlayer.Character, true);
            GameFiber.Wait(2000);
            SpeechHandler.PlayPhoneCallingSound(2);
            NegotiationResultSurrender = false;
            List<string> IntroLines = new List<string>() { "Robber: Who the hell is this? I'm busy!", "You: I'm an officer with the LSPD. Who am I speaking to?", "Robber: Shut up! I'm in control here!" };
            SpeechHandler.HandleBankHeistSpeech(IntroLines, LineFolderModifier: "NegotiationIntro");
            List<string> IntroAnswers = new List<string>() { "You: Look, the bank is surrounded, we all want to go home, just come out peacefully.", "You: OK, what do you want?", "You: Can't we make a deal?" };
            int res = SpeechHandler.DisplayAnswers(IntroAnswers);

            if (res == 0)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationOne");
                NegotiationOne();
            }
            else if (res == 1)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationTwo");
                NegotiationTwo();
            }
            else if (res == 2)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationThree");
                NegotiationThree();
            }
            if (!Maria.Exists())
            {
                SpeechHandler.PlayPhoneBusySound(3);
            }
            ToggleMobilePhone(Game.LocalPlayer.Character, false);

            GameFiber.Wait(2000);
            if (NegotiationResultSurrender)
            {
                NegotiationRobbersSurrender();
                return;
            }
            else
            {

                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to signal the SWAT teams to move in.");

                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "~b~You: ~s~SWAT Team Alpha, ~g~green light!~s~ Move in!" }, WaitAfterLastLine: false);
                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.FollowKey) + " ~s~to make the SWAT teams follow you.");
                        fighting = true;
                        break;
                    }
                }
            }

        }

        private void NegotiationOne()
        {

            List<string> Robber1 = new List<string>() { "Robber: Fuck you! I'll kill a hostage if you come anywhere near that door!" };
            SpeechHandler.HandleBankHeistSpeech(Robber1, LineFolderModifier: "NegotiationOne");
            List<string> Answers1 = new List<string>() { "You: Calm down. We don't want anyone to get hurt.", "You: OK. What do you want?", "You: You do that and every cop in the city will be in that bank!" };
            int res1 = SpeechHandler.DisplayAnswers(Answers1);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: Don't tell me what to do pig!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Look we all want to get out of this alive, just surrender. Do it for your wife, Maria.", "You: I'm sorry, I just don't want anyone to get hurt.", "You: I'm trying to save everyone here." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How do you know about her?! Leave my family alone!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Unfortunatly people are going to." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: I'm trying to save my family and dying here will only kill them...", "OK, we're coming out." }, LineFolderModifier: "NegotiationOne");
                    //Surrender
                    NegotiationResultSurrender = true;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set)

            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 4;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: We want a bus to take us to Los Santos International Airport!", "Robber: From there, we want a plane to fly us to Liberty City." };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Release the hostages and I'll cut your sentences by 5 years.", "You: I'll get one fuelled up for you, but I need a hostage in return.", "You: I can try, but it might take a while." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: 5 years! We can't go to jail, but we can't die either...", "Robber: We surrender!" }, LineFolderModifier: "NegotiationOne");
                    //Surrender
                    NegotiationResultSurrender = true;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 10;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: We can't do that officer and you know it, you're just wasting our time." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 11;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Sorry, we don't have time to wait." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 5;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: And every last hostage will be dead before they get in!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Don't say things like that, or you might get a meet and greet with a SWAT team.", "You: What would Maria say to you if she heard you say that?", "You: Don't you want to see your family again? Your hostages do, let them go!" };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 12;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck you pig! You and everyone else are going to die!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 13;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How do you know about Maria?! Fuck off pig!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck you pig! You and everyone else are going to die!" }, LineFolderModifier: "NegotiationOne");
                        //Fight
                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Of course I do.... OK, we are coming out. Don't shoot!" }, LineFolderModifier: "NegotiationOne");
                        //Surrender
                        NegotiationResultSurrender = true;
                    }

                }
            }

        }
        private void NegotiationTwo()
        {
            List<string> Robber2 = new List<string>() { "Robber: We want a bus to take us to Los Santos International.", "Robber: From there, we want a plane to fly us to Liberty City." };
            SpeechHandler.HandleBankHeistSpeech(Robber2, LineFolderModifier: "NegotiationTwo");
            List<string> Answers2 = new List<string>() { "You: I'll try to work on it but I can't guarantee anything.", "You: OK, we have a bus on the way, but you need to release the hostages first.", "You: Release the hostages and I'll cut your sentences by 5 years." };
            int res1 = SpeechHandler.DisplayAnswers(Answers2);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Don't tell me what you might be able to do!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: Look, it's the best I can do!", "You: Give me a hostage and you get your bus.", "You: I'm working on it but it could take some time." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Your best isn't good enough." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How about a bullet instead?" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Time is something we don't have." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set):*
            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 4;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Fuck you! They come out when I see the bus!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: The bus won't be there unless we get something from you.", "You: I have your wife Maria here, she wants to talk to you.", "You: It has to go both ways. If you give me a hostage your bus will arrive more quickly." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 9;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You aren't getting shit from me other than a bullet!" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    //50% chance to bring Maria in!
                    SpeechHandler.YouLineAudioCount = 10;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You have my wife? Tell her I love her and I'm doing this for our kids." }, LineFolderModifier: "NegotiationTwo");

                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1] }, LineFolderModifier: "NegotiationTwo");
                        SpeechHandler.YouLineAudioCount = 100;
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Robber: Let me talk to her.", "You: Hang on for a minute, she's coming!" }, LineFolderModifier: "NegotiationTwo");
                        GetMaria();
                        ToggleMobilePhone(Game.LocalPlayer.Character, false);
                        ToggleMobilePhone(Maria, true);
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Maria: Baby please come out, think about our kids!", "Maria: Please don't do this, honey!", "Robber: I'm so sorry, Maria...", "Robber: I really don't know why I'm here.", "Robber: I love you, Maria!", "Robber: I'm coming out!" }, LineFolderModifier: "NegotiationTwo");
                        SpeechHandler.PlayPhoneBusySound(3);
                        ToggleMobilePhone(Maria, false);
                        GameFiber.Wait(1500);

                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "You: Thank you, Maria. For your safety, please get back in the police car.", "Maria: Please don't shoot my husband!" }, LineFolderModifier: "NegotiationTwo");
                        Maria.Tasks.FollowNavigationMeshToPosition(MariaCopCar.GetOffsetPosition(Vector3.RelativeRight * 2f), MariaCopCar.Heading, 1.9f).WaitForCompletion(15000);
                        Maria.Tasks.EnterVehicle(MariaCopCar, 5000, 0).WaitForCompletion();
                        MariaCop.Tasks.CruiseWithVehicle(MariaCopCar, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                        NegotiationResultSurrender = true;
                    }
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Nah, it goes only my way." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Look man, we have families and we just need the money.", "Robber: We don't want anyone to get hurt!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: Think about your family! Put down your weapons and come out with your hands up!", "You: Think about them! The people you're holding in there have families too!", "You: If you don't want people to get hurt, surrender and save everyone." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You aren't getting shit from me other than a bullet!" }, LineFolderModifier: "NegotiationTwo");
                        //Fight
                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: They couldn't survive without me. OK, we are coming out!" }, LineFolderModifier: "NegotiationTwo");
                        //Surrender
                        NegotiationResultSurrender = true;
                    }

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck their families! I need money for MY family!" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Sadly people get hurt in this cruel world." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }
        }
        private void NegotiationThree()
        {
            List<string> Robber3 = new List<string>() { "Robber: Pig, what type of deal could you make that would interest us?!" };
            SpeechHandler.HandleBankHeistSpeech(Robber3, LineFolderModifier: "NegotiationThree");
            List<string> Answers3 = new List<string>() { "You: Look, the bank is surrounded. We all want to go home here, so just come out peacefully!", "You: What are you interested in then?", "You: No idea, but I'm sure we can still make a deal." };
            int res1 = SpeechHandler.DisplayAnswers(Answers3);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: I'll kill a hostage if you come anywhere near that door! " };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: Calm down, we don't want anyone to get hurt!", "You: You do that and you may be featured on the morning news.", "You: You don't want to hurt innocent people." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: People get hurt every day." }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Interesting propsect, I'll take it!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Innocent? In this world? Hah!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set):*
            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: We're interested in a free plane ticket to Liberty City, business class!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: You know that won't get approved. Don't you want to see your family again?", "You: That might take some time to handle. Giving us a hostage could speed it up!", "You: How about some business class pizza instead?" };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 9;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Of course I do. Alright, we are coming out, don't shoot!" }, LineFolderModifier: "NegotiationThree");
                    //Surrender
                    NegotiationResultSurrender = true;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Time is scarce, and so is human life." }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Pizza? You mean the one topped with hostage heads?" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: You'd better start making good deals soon then, or someone may die here." };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: I can give you something you want for a few hostages.", "You: Peacefully give me all the hostages and you'll be OK!", "You: I can shorten all of your sentences and make sure your family sees you if you surrender." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 12;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: I want this bank's money here!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: There is no peace in this world!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Alright, we'll come out. Just wait a minute..." }, LineFolderModifier: "NegotiationThree");
                    //Fight < Robbers first
                    NegotiationResultSurrender = false;
                }
            }
        }



        private void DetermineInitialDialogue()
        {
            List<string> InitialDialogue = new List<string>() { "You: Sir, what's the situation?", "Cpt. Wells: Well, we have multiple armed suspects in the bank with hostages.", "You: How many robbers are there?", "Cpt. Wells: We don't know how many are in there, officer.", "Cpt. Wells: The way I see it you have two options.", "Cpt. Wells: You could attempt to negotiate with them.", "Cpt. Wells: Alternatively, you can go in with SWAT Team Alpha." };
            SpeechHandler.CptWellsLineAudioCount = 1;
            SpeechHandler.YouLineAudioCount = 1;
            SpeechHandler.HandleBankHeistSpeech(InitialDialogue, LineFolderModifier: "Intro");
            List<string> NegOrAss = new List<string>() { "You: I'm going to try to talk to them first and see how that goes.", "You: Let's take these bastards out! " };
            int result = SpeechHandler.DisplayAnswers(NegOrAss);
            //If negotiate
            if (result == 0)
            {

                SpeechHandler.CptWellsLineAudioCount = 6;
                SpeechHandler.YouLineAudioCount = 3;
                List<string> NegotiationDialogue = new List<string>() { NegOrAss[result], "Cpt. Wells: Alright. The SWAT team will have your back!", "Cpt. Wells: Also, do you want our tech team to kill that alarm?" };
                SpeechHandler.HandleBankHeistSpeech(NegotiationDialogue, LineFolderModifier: "Negotiation");

                int negresult = SpeechHandler.DisplayAnswers(AlarmAnswers);
                if (negresult == 0)
                {
                    SpeechHandler.CptWellsLineAudioCount = 8;
                    SpeechHandler.YouLineAudioCount = 4;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[negresult], "Cpt. Wells: Tech team... cut the alarm." }, LineFolderModifier: "Negotiation");

                    AlarmPlayer.Stop();
                    CurrentAudioState = AudioState.None;
                    SpeechHandler.YouLineAudioCount = 5;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good luck officer. Let's try to end this thing peacefully.", "You: Copy that, sir." }, LineFolderModifier: "Negotiation");
                }
                else if (negresult == 1)
                {
                    SpeechHandler.CptWellsLineAudioCount = 10;
                    SpeechHandler.YouLineAudioCount = 6;
                    CurrentAudioState = AudioState.Alarm;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[negresult], "Cpt. Wells: You sure? If you change your mind, radio the tech team and let us know.", "You: Alright, I'm going to get suited up.", "Cpt. Wells: Good luck!" }, LineFolderModifier: "Negotiation");
                }
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.ToggleAlarmKey) + " ~s~at any time to toggle the alarm.");
                GameFiber.Wait(4000);
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to initiate the ~b~negotiation call~s~ with the robbers.");
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        break;
                    }

                }
                Game.HideHelp();
                NegotiationIntro();
            }

            //If assault
            else if (result == 1)
            {
                SpeechHandler.CptWellsLineAudioCount = 12;
                SpeechHandler.YouLineAudioCount = 100; //To be changed
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { NegOrAss[result] }, LineFolderModifier: "Assault");
                SpeechHandler.YouLineAudioCount = 8;
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good idea, officer. There's no time for talking.", "Cpt. Wells: The robbers are holding 8 hostages. Rescuing them is the top priority.", "You: Roger that, sir. Where can I get some gear?", "Cpt. Wells: There's gear in the back of the riot vans.", "Cpt. Wells: SWAT Team Alpha is on standby near the doors, let them know when you're ready.", "You: Alright, let's do this! ", "Cpt. Wells: Also, do you want the tech team to kill the alarm?" }, LineFolderModifier: "Assault");
                int alarmres = SpeechHandler.DisplayAnswers(AlarmAnswers);

                if (alarmres == 0)
                {
                    SpeechHandler.YouLineAudioCount = 4;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[alarmres], "Cpt. Wells: Tech team... shut it down." }, LineFolderModifier: "Assault");
                    AlarmPlayer.Stop();

                    CurrentAudioState = AudioState.None;
                    SpeechHandler.YouLineAudioCount = 5;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good luck officer, let's get this over with.", "You: Copy that, sir." }, LineFolderModifier: "Assault");
                }
                else if (alarmres == 1)
                {
                    SpeechHandler.CptWellsLineAudioCount = 19;
                    CurrentAudioState = AudioState.Alarm;
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[alarmres], "Cpt. Wells: You sure? If you change your mind, radio the tech team.", "You: Alright, I'm going to get suited up.", "Cpt. Wells: Good luck!" }, LineFolderModifier: "Assault");
                }
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.ToggleAlarmKey) + " ~s~at any time to toggle the alarm.");
                GameFiber.Wait(4500);
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to signal the SWAT teams to move in.");

                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "~b~You: ~s~SWAT Team Alpha, ~g~green light!~s~ Move in!" }, WaitAfterLastLine: false);
                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.FollowKey) + " ~s~to make the SWAT teams follow you.");
                        fighting = true;
                        break;
                    }
                }
            }


        }

        private void NegotiationRobbersSurrender()
        {
            SurrenderComplete = false;
            Surrendering = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    Game.DisplayNotification("~b~Cpt. Wells:~s~ The ~r~robbers ~s~seem to be surrendering. Get in position behind a ~b~police car.");
                    GameFiber.Wait(6000);
                    Game.DisplayNotification("~b~Other officers~s~ will perform the ~b~arrests~s~ and then ~b~deal with the robbers.");
                    GameFiber.Wait(6000);
                    Game.DisplayNotification("~b~Hold your position~s~ and keep the robbers under control by ~b~aiming in their direction.");
                    bool AllRobbersAtLocation = false;
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        GameFiber.Yield();
                        Robbers[i].Tasks.PlayAnimation("random@getawaydriver", "idle_2_hands_up", 1f, AnimationFlags.UpperBodyOnly | AnimationFlags.StayInEndFrame | AnimationFlags.SecondaryTask);
                        Robbers[i].Tasks.FollowNavigationMeshToPosition(RobbersSurrenderLocations[i], RobbersSurrenderHeadings[i], 1.45f);
                        Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(Robbers[i], false);
                    }
                    int waitcount = 0;
                    while (!AllRobbersAtLocation)
                    {
                        GameFiber.Yield();
                        waitcount++;
                        if (waitcount >= 10000)
                        {
                            for (int i = 0; i < Robbers.Count; i++)
                            {
                                Robbers[i].Position = RobbersSurrenderLocations[i];
                                Robbers[i].Heading = RobbersSurrenderHeadings[i];

                            }
                            break;
                        }
                        for (int i = 0; i < Robbers.Count; i++)
                        {
                            GameFiber.Yield();

                            if (Vector3.Distance(Robbers[i].Position, RobbersSurrenderLocations[i]) < 0.8f)
                            {
                                AllRobbersAtLocation = true;
                            }
                            else
                            {
                                AllRobbersAtLocation = false;
                                break;
                            }
                        }
                        for (int i = 0; i < SWATUnitsSpawned.Count; i++)
                        {
                            GameFiber.Wait(100);
                            Ped robber = Robbers[AssortedCalloutsHandler.rnd.Next(Robbers.Count)];
                            Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(SWATUnitsSpawned[i], robber.Position.X, robber.Position.Y, robber.Position.Z, -1, false, false);
                        }
                    }
                    GameFiber.Wait(1000);
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        GameFiber.Yield();

                        Robbers[i].Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                        NativeFunction.Natives.SET_PED_DROPS_WEAPON(Robbers[i]);
                        if (PoliceOfficersSpawned.Count >= i + 1)
                        {
                            PoliceOfficersArresting.Add(PoliceOfficersSpawned[i]);
                            PoliceOfficersSpawned[i].Tasks.FollowNavigationMeshToPosition(Robbers[i].GetOffsetPosition(Vector3.RelativeBack * 0.7f), Robbers[i].Heading, 1.55f);
                            Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(PoliceOfficersSpawned[i], false);
                        }

                    }
                    GameFiber.Wait(1000);

                    bool AllArrestingOfficersAtLocation = false;
                    waitcount = 0;
                    while (!AllArrestingOfficersAtLocation)
                    {
                        GameFiber.Yield();
                        waitcount++;
                        if (waitcount >= 10000)
                        {
                            for (int i = 0; i < PoliceOfficersArresting.Count; i++)
                            {
                                PoliceOfficersArresting[i].Position = Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f);
                                PoliceOfficersArresting[i].Heading = Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].Heading;

                            }
                            break;
                        }
                        for (int i = 0; i < PoliceOfficersArresting.Count; i++)
                        {

                            if (Vector3.Distance(PoliceOfficersArresting[i].Position, Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f)) < 0.8f)
                            {
                                AllArrestingOfficersAtLocation = true;
                            }
                            else
                            {
                                PoliceOfficersArresting[i].Tasks.FollowNavigationMeshToPosition(Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f), Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].Heading, 1.55f).WaitForCompletion(500);
                                AllArrestingOfficersAtLocation = false;
                                break;
                            }
                        }
                    }
                    foreach (Ped swatunit in SWATUnitsSpawned)
                    {
                        swatunit.Tasks.Clear();
                    }
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        Robbers[i].Tasks.PlayAnimation("mp_arresting", "idle", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        Robbers[i].Tasks.FollowNavigationMeshToPosition(AllSpawnedPoliceVehicles[i].GetOffsetPosition(Vector3.RelativeLeft * 2f), AllSpawnedPoliceVehicles[i].Heading, 1.58f);
                        PoliceOfficersArresting[i].Tasks.FollowNavigationMeshToPosition(AllSpawnedPoliceVehicles[i].GetOffsetPosition(Vector3.RelativeLeft * 2f), AllSpawnedPoliceVehicles[i].Heading, 1.55f);
                    }
                    GameFiber.Wait(5000);
                    SurrenderComplete = true;
                    GameFiber.Wait(12000);
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        Robbers[i].BlockPermanentEvents = true;
                        Robbers[i].Tasks.EnterVehicle(AllSpawnedPoliceVehicles[i], 11000, 1);
                        PoliceOfficersArresting[i].BlockPermanentEvents = true;
                        PoliceOfficersArresting[i].Tasks.EnterVehicle(AllSpawnedPoliceVehicles[i], 11000, -1);
                    }
                    GameFiber.Wait(11100);
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                }
            });
        }

        private void CheckForRobbersOutside()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    if (fighting)
                    {

                        foreach (Vector3 Location in PacificBankDoors)
                        {
                            foreach (Ped robber in World.GetEntities(Location, 1.6f, GetEntitiesFlags.ConsiderAllPeds))
                            {
                                if (robber.Exists())
                                {
                                    if (Vector3.Distance(robber.Position, Location) < 1.5f)
                                    {
                                        if (robber.IsAlive)
                                        {
                                            if (Robbers.Contains(robber))
                                            {
                                                if (!PoliceOfficersTargetsToShoot.Contains(robber))
                                                {

                                                    PoliceOfficersTargetsToShoot.Add(robber);
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            });
        }
        private void CopsReturnToLocation()
        {
            for (int i = 0; i < PoliceOfficersStandingSpawned.Count; i++)
            {
                if (PoliceOfficersStandingSpawned[i].Exists())
                {
                    if (PoliceOfficersStandingSpawned[i].IsAlive)
                    {
                        if (Vector3.Distance(PoliceOfficersStandingSpawned[i].Position, PoliceOfficersStandingLocations[i]) > 0.5f)
                        {
                            PoliceOfficersStandingSpawned[i].BlockPermanentEvents = true;
                            PoliceOfficersStandingSpawned[i].Tasks.FollowNavigationMeshToPosition(PoliceOfficersStandingLocations[i], PoliceOfficersStandingHeadings[i], 2f);
                        }
                    }
                }

            }
            for (int i = 0; i < PoliceOfficersAimingSpawned.Count; i++)
            {
                if (PoliceOfficersAimingSpawned[i].Exists())
                {
                    if (PoliceOfficersAimingSpawned[i].IsAlive)
                    {
                        if (Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PoliceOfficersAimingLocations[i]) > 0.5f)
                        {
                            PoliceOfficersAimingSpawned[i].BlockPermanentEvents = true;
                            PoliceOfficersAimingSpawned[i].Tasks.FollowNavigationMeshToPosition(PoliceOfficersAimingLocations[i], PoliceOfficersAimingHeadings[i], 2f);
                        }
                        else
                        {

                            Vector3 AimPoint;
                            if (Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PacificBankDoors[0]) < Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PacificBankDoors[1]))
                            {
                                AimPoint = PacificBankDoors[0];
                            }
                            else
                            {
                                AimPoint = PacificBankDoors[1];
                            }
                            Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(PoliceOfficersAimingSpawned[i], AimPoint.X, AimPoint.Y, AimPoint.Z, -1, false, false);
                        }
                    }
                }

            }
        }
        private void SneakyRobbersAI()
        {
            GameFiber.StartNew(delegate
            {

                while (CalloutRunning)
                {
                    try
                    {
                        GameFiber.Yield();

                        foreach (Ped sneakyrobber in RobbersSneakySpawned)
                        {
                            if (sneakyrobber.Exists())
                            {
                                if (sneakyrobber.IsAlive)
                                {
                                    if (!SneakyRobbersFighting.Contains(sneakyrobber))
                                    {
                                        if (Vector3.Distance(sneakyrobber.Position, RobbersSneakyLocations[RobbersSneakySpawned.IndexOf(sneakyrobber)]) > 0.7f)
                                        {

                                            sneakyrobber.Tasks.FollowNavigationMeshToPosition(RobbersSneakyLocations[RobbersSneakySpawned.IndexOf(sneakyrobber)], RobbersSneakyHeadings[RobbersSneakySpawned.IndexOf(sneakyrobber)], 2f).WaitForCompletion(300);
                                        }
                                        else
                                        {
                                            if (!NativeFunction.Natives.IS_ENTITY_PLAYING_ANIM<bool>(sneakyrobber, "cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 3))
                                            {

                                                sneakyrobber.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 2f, AnimationFlags.StayInEndFrame).WaitForCompletion(20);
                                            }

                                        }
                                        Ped[] nearestPeds = sneakyrobber.GetNearbyPeds(3);
                                        if (nearestPeds.Length > 0)
                                        {
                                            foreach (Ped nearestPed in nearestPeds)
                                            {
                                                if (nearestPed != null)
                                                {
                                                    if (nearestPed.Exists())
                                                    {
                                                        if (nearestPed.IsAlive)
                                                        {
                                                            if (nearestPed.RelationshipGroup == "PLAYER" || nearestPed.RelationshipGroup == "COP")
                                                            {
                                                                if (Vector3.Distance(nearestPed.Position, sneakyrobber.Position) < 3.9f)
                                                                {
                                                                    if (Math.Abs(nearestPed.Position.Z - sneakyrobber.Position.Z) < 0.9f)
                                                                    {

                                                                        SneakyRobberFight(sneakyrobber, nearestPed);
                                                                        break;
                                                                    }
                                                                }

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }


                    }
                    catch (Exception e) { Game.LogTrivial(e.ToString()); }
                }
            });
        }
        private List<Ped> SneakyRobbersFighting = new List<Ped>();
        private Entity entityPlayerAimingAtSneakyRobber = null;
        private void SneakyRobberFight(Ped sneakyrobber, Ped nearestPed)
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    SneakyRobbersFighting.Add(sneakyrobber);
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!nearestPed.Exists()) { break; }
                        if (!sneakyrobber.Exists()) { break; }
                        if (!sneakyrobber.IsAlive) { break; }
                        if (!nearestPed.IsAlive) { break; }
                        if (Vector3.Distance(nearestPed.Position, sneakyrobber.Position) > 5.1f)
                        {
                            break;
                        }
                        else if (Vector3.Distance(nearestPed.Position, sneakyrobber.Position) < 1.70f)
                        {
                            break;
                        }
                        try
                        {
                            unsafe
                            {
                                uint entityHandle;
                                NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                                entityPlayerAimingAtSneakyRobber = World.GetEntityByHandle<Rage.Entity>(entityHandle);

                            }
                        }
                        catch (Exception e)
                        {

                        }
                        if (entityPlayerAimingAtSneakyRobber == sneakyrobber)
                        {
                            break;
                        }
                        if (RescuingHostage) { break; }
                    }
                    if (sneakyrobber.Exists())
                    {
                        sneakyrobber.Tasks.FightAgainstClosestHatedTarget(15f);
                        sneakyrobber.RelationshipGroup = "ROBBERS";
                    }
                    while (CalloutRunning)
                    {

                        GameFiber.Yield();
                        if (!sneakyrobber.Exists()) { break; }
                        if (!nearestPed.Exists()) { break; }
                        Rage.Native.NativeFunction.Natives.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(sneakyrobber);
                        if (nearestPed.IsDead)
                        {
                            foreach (Ped hostage in SpawnedHostages)
                            {
                                if (Math.Abs(hostage.Position.Z - sneakyrobber.Position.Z) < 0.6f)
                                {
                                    if (Vector3.Distance(hostage.Position, sneakyrobber.Position) < 14f)
                                    {


                                        int waitCount = 0;
                                        while (hostage.IsAlive)
                                        {
                                            GameFiber.Yield();
                                            waitCount++;
                                            if (waitCount > 450)
                                            {
                                                hostage.Kill();
                                            }
                                        }

                                        break;
                                    }
                                }
                            }

                            break;

                        }

                        if (sneakyrobber.IsDead) { break; }

                    }
                }
                catch (Exception e)
                {

                }
                finally
                {
                    SneakyRobbersFighting.Remove(sneakyrobber);
                }
            });
        }
        private void HandleVaultRobbers()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    try
                    {
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, OutsideBankVault) < 4f)
                        {
                            GameFiber.Wait(2000);

                            RobbersVault[2].Tasks.FollowNavigationMeshToPosition(OutsideBankVault, RobbersVault[2].Heading, 2f).WaitForCompletion(500);
                            World.SpawnExplosion(new Vector3(252.2609f, 225.3824f, 101.6835f), 2, 0.2f, true, false, 0.6f);
                            CurrentAudioState = AudioState.Alarm;
                            AudioStateChanged = true;
                            GameFiber.Wait(900);
                            foreach (Ped vaultrobber in RobbersVault)
                            {
                                vaultrobber.Tasks.FightAgainstClosestHatedTarget(23f);

                            }
                            GameFiber.Wait(3000);
                            foreach (Ped vaultrobber in RobbersVault)
                            {
                                Robbers.Add(vaultrobber);
                            }
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                    }
                }
            });
        }
        private bool RescuingHostage = false;
        private void HandleHostages()
        {
            Game.FrameRender += DrawHostageCount;
            GameFiber.StartNew(delegate
            {
                int waitCountForceAttack = 0;
                int enterAmbulanceCount = 0;
                int deleteSafeHostageCount = 0;
                int subtitleCount = 0;
                Ped closeHostage = null;
                while (CalloutRunning)
                {
                    try
                    {
                        waitCountForceAttack++;
                        enterAmbulanceCount++;

                        GameFiber.Yield();
                        if (waitCountForceAttack > 250)
                        {
                            waitCountForceAttack = 0;
                        }
                        if (enterAmbulanceCount > 101)
                        {
                            enterAmbulanceCount = 101;
                        }
                        foreach (Ped hostage in SpawnedHostages)
                        {
                            GameFiber.Yield();
                            if (hostage.Exists())
                            {
                                if (hostage.IsAlive)
                                {
                                    if (Functions.IsPedGettingArrested(hostage) || Functions.IsPedArrested(hostage))
                                    {
                                        SpawnedHostages[SpawnedHostages.IndexOf(hostage)] = hostage.ClonePed();
                                    }
                                    hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                                    if (!Game.LocalPlayer.Character.IsShooting)
                                    {
                                        if (Vector3.Distance(hostage.Position, Game.LocalPlayer.Character.Position) < 1.45f)
                                        {
                                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(AssortedCalloutsHandler.HostageRescueKey))
                                            {
                                                Vector3 directionFromPlayerToHostage = (hostage.Position - Game.LocalPlayer.Character.Position);
                                                directionFromPlayerToHostage.Normalize();
                                                RescuingHostage = true;
                                                Game.LocalPlayer.Character.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPlayerToHostage)).WaitForCompletion(1200);
                                                hostage.RelationshipGroup = "COP";
                                                SpeechHandler.HandleBankHeistSpeech(new List<string>() { "You: Come on! It's safe, get to the ambulance outside!" }, WaitAfterLastLine: false);
                                                Game.LocalPlayer.Character.Tasks.PlayAnimation("random@rescue_hostage", "bystander_helping_girl_loop", 1.5f, AnimationFlags.None).WaitForCompletion(3000);

                                                if (hostage.IsAlive)
                                                {
                                                    hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 0.9f, AnimationFlags.None).WaitForCompletion(6000);
                                                    Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                                    if (hostage.IsAlive)
                                                    {
                                                        hostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f);

                                                        RescuedHostages.Add(hostage);
                                                        SpawnedHostages.Remove(hostage);
                                                    }
                                                    else
                                                    {
                                                        Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                                    }
                                                }
                                                else
                                                {
                                                    Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                                }
                                                RescuingHostage = false;

                                            }
                                            else
                                            {
                                                subtitleCount++;
                                                closeHostage = hostage;

                                                if (subtitleCount > 10)
                                                {

                                                    Game.DisplaySubtitle("~s~Hold ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.HostageRescueKey) + " ~s~to release the hostage.", 500);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (hostage == closeHostage)
                                            {
                                                subtitleCount = 0;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    SpawnedHostages.Remove(hostage);
                                    AliveHostagesCount--;
                                }
                            }
                            else
                            {
                                SpawnedHostages.Remove(hostage);
                                AliveHostagesCount--;
                            }
                        }
                        foreach (Ped rescuedHostage in RescuedHostages)
                        {

                            if (rescuedHostage.Exists() && rescuedHostage.IsAlive)
                            {
                                if (SpawnedHostages.Contains(rescuedHostage))
                                {
                                    SpawnedHostages.Remove(rescuedHostage);
                                }
                                if (Vector3.Distance(rescuedHostage.Position, HostageSafeLocation) < 3f)
                                {
                                    SafeHostages.Add(rescuedHostage);
                                    SafeHostagesCount++;
                                }
                                if (Functions.IsPedGettingArrested(rescuedHostage) || Functions.IsPedArrested(rescuedHostage))
                                {
                                    RescuedHostages[RescuedHostages.IndexOf(rescuedHostage)] = rescuedHostage.ClonePed();
                                }
                                rescuedHostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f).WaitForCompletion(200);

                                if (waitCountForceAttack > 150)
                                {
                                    Ped nearestPed = rescuedHostage.GetNearbyPeds(2)[0];
                                    if (nearestPed == Game.LocalPlayer.Character)
                                    {
                                        nearestPed = rescuedHostage.GetNearbyPeds(2)[1];
                                    }
                                    if (Robbers.Contains(nearestPed))
                                    {

                                        nearestPed.Tasks.FightAgainst(rescuedHostage);
                                        waitCountForceAttack = 0;

                                    }
                                }
                            }
                            else
                            {
                                RescuedHostages.Remove(rescuedHostage);
                                AliveHostagesCount--;
                            }
                        }
                        foreach (Ped safeHostage in SafeHostages)
                        {
                            if (safeHostage.Exists())
                            {
                                if (RescuedHostages.Contains(safeHostage))
                                {
                                    RescuedHostages.Remove(safeHostage);
                                }
                                safeHostage.IsInvincible = true;
                                if (!safeHostage.IsInAnyVehicle(true))
                                {

                                    if (enterAmbulanceCount > 100)
                                    {
                                        if (AmbulancesList[1].IsSeatFree(2))
                                        {
                                            safeHostage.Tasks.EnterVehicle(AmbulancesList[1], 2);

                                        }
                                        else if (AmbulancesList[1].IsSeatFree(1))
                                        {
                                            safeHostage.Tasks.EnterVehicle(AmbulancesList[1], 1);

                                        }
                                        else
                                        {
                                            AmbulancesList[1].GetPedOnSeat(2).Delete();
                                            safeHostage.Tasks.EnterVehicle(AmbulancesList[1], 2);

                                        }

                                        enterAmbulanceCount = 0;
                                    }
                                }
                                else
                                {
                                    deleteSafeHostageCount++;
                                    if (deleteSafeHostageCount > 50)
                                    {
                                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, safeHostage.Position) > 22f)
                                        {
                                            if (safeHostage.IsInAnyVehicle(false))
                                            {

                                                safeHostage.Delete();

                                                deleteSafeHostageCount = 0;
                                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOORS_SHUT(AmbulancesList[1], true);
                                            }
                                        }
                                    }
                                }

                            }
                            else
                            {
                                SafeHostages.Remove(safeHostage);
                            }
                        }



                    }
                    catch (Exception e) { continue; }
                }
            });
        }
        private void DrawHostageCount(System.Object sender, Rage.GraphicsEventArgs e)
        {
            if (fighting || (SurrenderComplete && TalkedToWells2nd))
            {

                e.Graphics.DrawText("Hostages Rescued: " + SafeHostagesCount.ToString() + "/" + AliveHostagesCount.ToString(), "Aharoni Bold", 20.0f, new PointF(1, 6), Color.LightBlue);
                if (TotalHostagesCount - AliveHostagesCount > 0)
                {
                    e.Graphics.DrawText("Hostages Killed: " + (TotalHostagesCount - AliveHostagesCount).ToString(), "Aharoni Bold", 20.0f, new PointF(1, 30), Color.Red);
                }
            }
            if (!CalloutRunning || DoneFighting)
            {
                Game.FrameRender -= DrawHostageCount;
            }

        }
        private enum AudioState { Alarm, None };
        private void HandleAudio()
        {


            GameFiber.StartNew(delegate
            {

                CurrentAudioState = AudioState.None;

                while (CalloutRunning)
                {
                    try
                    {
                        GameFiber.Yield();
                        if (!HandlingRespawn)
                        {

                            if (!AlarmPlaying)
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) < 55f)
                                {
                                    AlarmPlaying = true;
                                    CurrentAudioState = AudioState.Alarm;
                                    SuspectBlip.IsRouteEnabled = false;
                                    AudioStateChanged = true;
                                }
                            }
                            else
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) > 70f)
                                {
                                    AlarmPlaying = false;
                                    CurrentAudioState = AudioState.None;
                                    SuspectBlip.IsRouteEnabled = true;
                                    AudioStateChanged = true;
                                }
                            }



                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.ToggleAlarmKey))
                            {
                                if (CurrentAudioState != AudioState.None)
                                {
                                    CurrentAudioState += 1;
                                }
                                else
                                {
                                    CurrentAudioState = AudioState.Alarm;
                                }
                                AudioStateChanged = true;

                            }

                            if (AudioStateChanged)
                            {
                                switch (CurrentAudioState)
                                {
                                    case AudioState.Alarm:

                                        AlarmPlayer.PlayLooping();
                                        break;

                                    case AudioState.None:
                                        AlarmPlayer.Stop();

                                        break;
                                }
                                AudioStateChanged = false;
                            }


                        }
                    }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                    }
                }

            });
        }

        private void HandleOpenBackRiotVan()
        {
            GameFiber.StartNew(delegate
            {
                int CoolDown = 0;
                while (CalloutRunning)
                {
                    try
                    {
                        GameFiber.Yield();
                        if (CoolDown > 0) { CoolDown--; }
                        if (HandlingRespawn) { CoolDown = 0; }

                        if (Vector3.Distance(RiotVans[0].GetOffsetPosition(Vector3.RelativeBack * 4f), Game.LocalPlayer.Character.Position) < 2f)
                        {

                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(System.Windows.Forms.Keys.Enter))
                            {
                                if (CoolDown > 0)
                                {
                                    Game.DisplayNotification("The gear has temporarily run out.");
                                }
                                else
                                {
                                    CoolDown = 3500;
                                    Game.LocalPlayer.Character.Tasks.EnterVehicle(RiotVans[0], 1).WaitForCompletion();
                                    Game.LocalPlayer.Character.Armor = 100;
                                    Game.LocalPlayer.Character.Health = Game.LocalPlayer.Character.MaxHealth;
                                    Game.LocalPlayer.Character.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 150, true);

                                    Game.LocalPlayer.Character.Inventory.GiveNewWeapon(new WeaponAsset(Grenades[1]), 3, false);
                                    Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                                    Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                                    FightingPacksUsed++;
                                }

                            }
                            else
                            {
                                if (CoolDown == 0)
                                {
                                    Game.DisplaySubtitle("~h~Press ~b~Enter ~s~to retrieve gear from the van.", 500);
                                }
                            }
                        }
                        else if (Vector3.Distance(RiotVans[1].GetOffsetPosition(Vector3.RelativeBack * 4f), Game.LocalPlayer.Character.Position) < 2f)
                        {

                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(System.Windows.Forms.Keys.Enter))
                            {
                                if (CoolDown > 0)
                                {
                                    Game.DisplayNotification("The gear has temporarily run out.");
                                }
                                else
                                {
                                    CoolDown = 3500;
                                    Game.LocalPlayer.Character.Tasks.EnterVehicle(RiotVans[1], 1).WaitForCompletion();
                                    Game.LocalPlayer.Character.Armor = 100;
                                    Game.LocalPlayer.Character.Health = Game.LocalPlayer.Character.MaxHealth;
                                    Game.LocalPlayer.Character.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 150, true);

                                    Game.LocalPlayer.Character.Inventory.GiveNewWeapon(new WeaponAsset(Grenades[1]), 3, false);
                                    Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                                    Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                                    FightingPacksUsed++;
                                }
                            }
                            else
                            {
                                if (CoolDown == 0)
                                {
                                    Game.DisplaySubtitle("~h~Press ~b~Enter ~s~to retrieve gear from the van.", 500);
                                }
                            }
                        }

                    }
                    catch (Exception e) { }
                }
            });
        }
        private Entity entityPlayerAimingAt;
        private void RobbersFightingAI()
        {
            GameFiber.StartNew(delegate
            {

                while (CalloutRunning)
                {

                    try
                    {
                        GameFiber.Yield();
                        if (fighting)
                        {
                            foreach (Ped robber in Robbers)
                            {
                                GameFiber.Yield();
                                if (robber.Exists())
                                {
                                    float Distance;
                                    if (Vector3.Distance(robber.Position, PacificBankInsideChecks[0]) < Vector3.Distance(robber.Position, PacificBankInsideChecks[1]))
                                    {
                                        Distance = Vector3.Distance(robber.Position, PacificBankInsideChecks[0]);
                                    }
                                    else
                                    {
                                        Distance = Vector3.Distance(robber.Position, PacificBankInsideChecks[1]);
                                    }

                                    if (Distance < 16.5f) { Distance = 16.5f; }
                                    else if (Distance > 21f) { Distance = 21f; }
                                    robber.RegisterHatedTargetsAroundPed(Distance);
                                    robber.Tasks.FightAgainstClosestHatedTarget(Distance);
                                    //Rage.Native.NativeFunction.CallByName<uint>("TASK_GUARD_CURRENT_POSITION", robber, 10.0f, 10.0f, true);
                                }
                            }
                            if (MiniGunRobber.Exists())
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, MiniGunFireLocation) < Vector3.Distance(Game.LocalPlayer.Character.Position, BehindGlassDoorLocation))
                                {
                                    if (Vector3.Distance(MiniGunFireLocation, Game.LocalPlayer.Character.Position) < 4.7f)
                                    {
                                        MiniGunRobberFiring = true;

                                    }
                                    else if (Vector3.Distance(MiniGunFireLocation, Game.LocalPlayer.Character.Position) > 12f)
                                    {
                                        MiniGunRobberFiring = false;
                                    }

                                }
                                else
                                {
                                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, BehindGlassDoorLocation) < 2.1f)
                                    {
                                        MiniGunRobberFiring = true;
                                    }
                                    else if (Vector3.Distance(Game.LocalPlayer.Character.Position, BehindGlassDoorLocation) > 6f)
                                    {
                                        MiniGunRobberFiring = false;
                                    }
                                }
                                if (MiniGunRobberFiring)
                                {

                                    Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(MiniGunRobber, Game.LocalPlayer.Character, 2700, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                                }
                                else
                                {
                                    MiniGunRobber.Tasks.FollowNavigationMeshToPosition(MiniGunRobberLocation, MiniGunRobberHeading, 2f);

                                }

                            }

                            try
                            {
                                unsafe
                                {
                                    uint entityHandle;
                                    NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                                    entityPlayerAimingAt = World.GetEntityByHandle<Rage.Entity>(entityHandle);
                                }
                            }
                            catch (Exception e) { Game.LogTrivial(e.ToString()); }

                            if (Robbers.Contains(entityPlayerAimingAt))
                            {

                                Ped pedAimingAt = (Ped)entityPlayerAimingAt;
                                pedAimingAt.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            }
                            GameFiber.Sleep(3000);
                        }

                    }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                    }
                }
            });

        }

        private GameFiber CopFightingAIGameFiber;
        private void CopFightingAI()
        {
            CopFightingAIGameFiber = GameFiber.StartNew(delegate
            {

                while (CalloutRunning)
                {
                    try
                    {
                        GameFiber.Yield();

                        if (fighting)
                        {
                            if (PoliceOfficersTargetsToShoot.Count > 0)
                            {
                                if (PoliceOfficersTargetsToShoot[0].Exists())
                                {
                                    if (PoliceOfficersTargetsToShoot[0].IsAlive)
                                    {
                                        foreach (Ped cop in PoliceOfficersSpawned)
                                        {

                                            cop.Tasks.FightAgainst(PoliceOfficersTargetsToShoot[0]);
                                        }
                                    }
                                    else
                                    {
                                        PoliceOfficersTargetsToShoot.RemoveAt(0);
                                    }
                                }
                                else
                                {
                                    PoliceOfficersTargetsToShoot.RemoveAt(0);
                                }

                            }
                            else
                            {
                                CopsReturnToLocation();
                            }
                        }
                        if (fighting || SWATFollowing)
                        {
                            foreach (Ped cop in SWATTeam1)
                            {
                                GameFiber.Yield();
                                if (cop.Exists())
                                {
                                    if (!SWATFollowing)
                                    {
                                        cop.RegisterHatedTargetsAroundPed(60f);
                                        cop.Tasks.FightAgainstClosestHatedTarget(60f);
                                    }
                                    else
                                    {
                                        if (Math.Abs(Game.LocalPlayer.Character.Position.Z - cop.Position.Z) > 1f)
                                        {
                                            cop.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading, 1.6f, 1f);
                                        }
                                        else
                                        {
                                            cop.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading, 1.6f, 4f);
                                        }
                                    }
                                }
                            }
                            foreach (Ped cop in SWATTeam2)
                            {
                                GameFiber.Yield();
                                if (cop.Exists())
                                {
                                    if (!SWATFollowing)
                                    {
                                        cop.RegisterHatedTargetsAroundPed(60f);
                                        cop.Tasks.FightAgainstClosestHatedTarget(60f);
                                    }
                                    else
                                    {
                                        if (Math.Abs(Game.LocalPlayer.Character.Position.Z - cop.Position.Z) > 1f)
                                        {
                                            cop.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading, 1.6f, 1f);
                                        }
                                        else
                                        {
                                            cop.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading, 1.6f, 4f);
                                        }
                                    }
                                }
                            }
                            GameFiber.Sleep(4000);
                        }


                    }

                    catch (Exception e) { }
                }
            });
        }
        private Vector3 DoorSide1 = new Vector3(265.542f, 217.4402f, 110.283f);
        private Vector3 DoorSide2 = new Vector3(265.8473f, 218.1096f, 110.283f);


        private void CalloutHandler()
        {
            CalloutRunning = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    SuspectBlip = new Blip(PacificBank);
                    SideDoorBlip = new Blip(new Vector3(258.3625f, 200.4897f, 104.9758f));
                    SuspectBlip.IsRouteEnabled = true;
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Wait(4800);
                        Game.DisplayNotification("Copy that, responding ~b~CODE 3 ~s~to the ~b~Pacific Bank~s~, over.");
                        Functions.PlayScannerAudio("COPY_THAT_MOVING_RIGHT_NOW REPORT_RESPONSE_COPY PROCEED_WITH_CAUTION_ASSORTED");
                        GameFiber.Wait(3400);

                        Game.DisplayNotification("Roger that, ~r~proceed with caution!");

                    });

                    LoadModels();
                    while (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) > 350f)
                    {
                        GameFiber.Yield();
                    }
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        AllBankHeistEntities.Add(Game.LocalPlayer.Character.CurrentVehicle);
                        Ped[] passengers = Game.LocalPlayer.Character.CurrentVehicle.Passengers;
                        if (passengers.Length > 0)
                        {
                            foreach (Ped passenger in passengers)
                            {
                                AllBankHeistEntities.Add(passenger);
                            }
                        }
                    }
                    GameFiber.Yield();
                    CreateSpeedZone();
                    ClearUnrelatedEntities();
                    Game.LogTrivial("Unrelated entities cleared");
                    GameFiber.Yield();
                    SpawnAllBarriers();

                    SpawnAllPoliceCars();
                    GameFiber.Yield();
                    SpawnBothSwatTeams();
                    GameFiber.Yield();
                    SpawnNegotiationRobbers();
                    GameFiber.Yield();
                    SpawnAllPoliceOfficers();
                    GameFiber.Yield();
                    SpawnSneakyRobbers();


                    SpawnHostages();
                    GameFiber.Yield();
                    SpawnEMSAndFire();
                    GameFiber.Yield();
                    if (AssortedCalloutsHandler.rnd.Next(10) < 2)
                    {
                        SpawnVaultRobbers();
                    }

                    Game.LogTrivial("Done spawning");

                    MakeNearbyPedsFlee();

                    SneakyRobbersAI();
                    HandleHostages();
                    HandleOpenBackRiotVan();
                    HandleAudio();
                    Game.LogTrivial("Initialisation complete, entering loop");

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        //Constants
                        Game.LocalPlayer.Character.CanAttackFriendlies = false;
                        Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("COP", "PLAYER", Relationship.Respect);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "COP", Relationship.Respect);
                        Game.SetRelationshipBetweenRelationshipGroups("HOSTAGE", "PLAYER", Relationship.Respect);
                        Game.SetRelationshipBetweenRelationshipGroups("SNEAKYROBBERS", "PLAYER", Relationship.Hate);
                        Game.LocalPlayer.Character.IsInvincible = false;
                        Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DEFENSE_MODIFIER(Game.LocalPlayer, 0.45f);
                        Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DAMAGE_MODIFIER(Game.LocalPlayer, 0.92f);
                        Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER(1f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 4072696575, 256.3116f, 220.6579f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 746855201, 262.1981f, 222.5188f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 110411286, 258.2022f, 204.1005f, 106.4049f, false, 0f, 0f, 0f);


                        //When player has just arrived
                        if (!TalkedToWells && !fighting)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, CaptainWells.Position) < 4f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {

                                        TalkedToWells = true;
                                        if (ComputerPlusRunning)
                                        {
                                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Spoken with Captain Wells.");

                                        }
                                        DetermineInitialDialogue();
                                    }
                                }
                                else
                                {
                                    Game.DisplayHelp("~h~Officer, please report to ~g~Captain Wells ~s~for briefing.");
                                }
                            }
                        }
                        //If fighting is initialised
                        if (!FightingPrepared)
                        {
                            if (fighting)
                            {
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Preparing to enter the bank with SWAT.");

                                }
                                SpawnAssaultRobbers();
                                SpawnMiniGunRobber();
                                CopFightingAI();
                                RobbersFightingAI();

                                CheckForRobbersOutside();


                                FightingPrepared = true;

                            }
                        }

                        //If player talks to cpt wells during fight
                        if (fighting)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, CaptainWells.Position) < 3f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        SpeechHandler.CptWellsLineAudioCount = 24;
                                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Go on! There are still hostages in there!" }, LineFolderModifier: "Assault", WaitAfterLastLine: false);
                                    }
                                }
                            }
                        }


                        //Make everyone fight if player enters bank
                        if (!fighting && !Surrendering)
                        {
                            foreach (Vector3 check in PacificBankInsideChecks)
                            {
                                if (Vector3.Distance(check, Game.LocalPlayer.Character.Position) < 2.3f)
                                {
                                    fighting = true;
                                }
                            }
                        }
                        //If all hostages rescued break
                        if (SafeHostagesCount == AliveHostagesCount)
                        {

                            break;
                        }

                        //If surrendered
                        if (SurrenderComplete)
                        {
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Robbers have surrendered. Going in to save hostages.");

                            }
                            break;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.FollowKey))
                        {
                            SWATFollowing = !SWATFollowing;
                            if (SWATFollowing)
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are now following you.", 3000);
                            }
                            else
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                            }


                        }
                        if (SWATFollowing)
                        {
                            if (Game.LocalPlayer.Character.IsShooting)
                            {
                                SWATFollowing = false;
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                                Game.LogTrivial("Follow off - shooting");
                            }
                        }

                    }
                    //When surrendered
                    if (SurrenderComplete) { CopFightingAI(); }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        //Constants

                        Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("COP", "PLAYER", Relationship.Companion);
                        Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "COP", Relationship.Companion);
                        Game.SetRelationshipBetweenRelationshipGroups("HOSTAGE", "PLAYER", Relationship.Companion);
                        Game.SetRelationshipBetweenRelationshipGroups("SNEAKYROBBERS", "PLAYER", Relationship.Hate);
                        Game.LocalPlayer.Character.IsInvincible = false;
                        Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DEFENSE_MODIFIER(Game.LocalPlayer, 0.45f);
                        Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DAMAGE_MODIFIER(Game.LocalPlayer, 0.93f);
                        Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER(1f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 4072696575, 256.3116f, 220.6579f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 746855201, 262.1981f, 222.5188f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 110411286, 258.2022f, 204.1005f, 106.4049f, false, 0f, 0f, 0f);
                        //If all host rescued
                        if (SafeHostagesCount == AliveHostagesCount)
                        {
                            GameFiber.Wait(3000);
                            break;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.FollowKey))
                        {
                            SWATFollowing = !SWATFollowing;

                            if (SWATFollowing)
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are following you.", 3000);
                            }
                            else
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                            }
                        }
                        if (SWATFollowing)
                        {
                            if (Game.LocalPlayer.Character.IsShooting)
                            {
                                SWATFollowing = false;
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                                Game.LogTrivial("Follow off - shooting");
                            }
                        }

                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, CaptainWells.Position) < 4f)
                            {
                                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                {
                                    if (!TalkedToWells2nd)
                                    {

                                        List<string> CptWellsSurrenderedDialogue = new List<string>() { "Cpt. Wells: Amazing job, officer! It seems the robbers surrendered!", "Cpt. Wells: Your job now is to rescue all the hostages from the bank.", "Cpt. Wells: Please take care, you never know what the robbers left inside.", "Cpt. Wells: We have no idea if there are still robbers inside.", "You: Roger that, sir. This situation will be over in no time!", "You: Where can I get geared up?", "Cpt. Wells: There's gear in the back of the riot vans." };
                                        SpeechHandler.HandleBankHeistSpeech(CptWellsSurrenderedDialogue);
                                        TalkedToWells2nd = true;
                                        fighting = true;
                                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.FollowKey + " ~s~to make the SWAT teams follow you.");
                                    }
                                    else
                                    {

                                        SpeechHandler.CptWellsLineAudioCount = 24;
                                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Go on! There are still more hostages in there!" }, LineFolderModifier: "Assault", WaitAfterLastLine: false);
                                    }
                                }
                            }
                            else
                            {
                                if (!TalkedToWells2nd)
                                {
                                    Game.DisplayHelp("~h~Officer, please report to ~g~Captain Wells.");
                                }
                            }
                        }
                    }



                    //The end

                    SWATFollowing = false;
                    DoneFighting = true;
                    CurrentAudioState = AudioState.None;
                    AudioStateChanged = true;
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 4072696575, 256.3116f, 220.6579f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 746855201, 262.1981f, 222.5188f, 106.4296f, false, 0f, 0f, 0f);
                        NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 110411286, 258.2022f, 204.1005f, 106.4049f, false, 0f, 0f, 0f);
                        if (!EvaluatedWithWells)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, CaptainWells.Position) < 4f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        TalkedToWells = true;
                                        FinalDialogue();
                                        GameFiber.Wait(4000);
                                        DetermineResults();

                                        GameFiber.Wait(9000);
                                        break;
                                    }
                                }
                                else
                                {
                                    Game.DisplayHelp("~h~Talk to ~g~Captain Wells~s~.");
                                }
                            }
                        }
                    }
                    if (CalloutRunning)
                    {
                        Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                        Game.DisplayNotification("~o~Bank Heist ~s~callout is ~g~CODE 4.");
                        CalloutFinished = true;

                    }
                    End();
                }

                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    End();
                }
            });
        }

        private void FinalDialogue()
        {
            List<string> FinalDialogue = new List<string>() { "Cpt. Wells: Thank god you've brought this all to an end!", "You: Yeah, it was pretty hectic in there!", "Cpt. Wells: I will send you the operation report soon.", "Cpt. Wells: Good work today officer, thank you.", "You: Just doing my job, sir. " };
            SpeechHandler.CptWellsLineAudioCount = 21;
            SpeechHandler.YouLineAudioCount = 10;
            SpeechHandler.HandleBankHeistSpeech(FinalDialogue, LineFolderModifier: "Outro");

        }
        private void DetermineResults()
        {
            int HostagesDead = TotalHostagesCount - AliveHostagesCount;
            foreach (Ped robber in Robbers)
            {
                if (robber.Exists())
                {
                    if (robber.IsDead)
                    {
                        RobbersKilled++;

                    }
                }
            }
            foreach (Ped robber in RobbersSneakySpawned)
            {
                if (robber.Exists())
                {
                    if (robber.IsDead)
                    {
                        RobbersKilled++;
                    }
                }
            }
            foreach (Ped swatunit in SWATUnitsSpawned)
            {
                if (swatunit.Exists())
                {
                    if (swatunit.IsDead)
                    {
                        SWATUnitsdied++;
                    }
                }
            }
            if (MiniGunRobber.Exists())
            {
                if (MiniGunRobber.IsDead)
                {
                    RobbersKilled++;
                }
            }
            Game.DisplayNotification("mphud", "mp_player_ready", "~h~Captain Wells", "Operation Report", "Hostages Rescued: " + SafeHostagesCount.ToString() + "~n~Hostages Dead: " + HostagesDead.ToString() + "~n~Robbers Killed: " + RobbersKilled.ToString() + "~n~Robbers Surrendered: " + SurrenderComplete.ToString());
            Game.DisplayNotification("mphud", "mp_player_ready", "~h~Captain Wells", "Operation Report - Continued", "Times died: " + TimesDied.ToString() + "~n~Times gear resupplied: " + FightingPacksUsed.ToString() + "~n~SWAT units died: " + SWATUnitsdied.ToString() + "~n~~b~End of report.");
            if (HostagesDead == 0)
            {
                BigMessageThread bigMessage = new BigMessageThread(true);
                bigMessage.MessageInstance.ShowMissionPassedMessage("All hostages were saved! Great job!", time: 8000);

            }
        }
        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = PacificBank;
            if (Vector3.Distance(SpawnPoint, Game.LocalPlayer.Character.Position) < 90f)
            {
                return false;
            }
            else if (Vector3.Distance(SpawnPoint, Game.LocalPlayer.Character.Position) > 2800f)
            {
                return false;
            }
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            CalloutPosition = SpawnPoint;
            CalloutMessage = "Pacific Bank Heist";
            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Pacific Bank Heist", "Bank Heist", SpawnPoint, 1, "Reports of a major bank heist at the Pacific Bank. Multiple emergency services on scene. Respond as a tactical commander.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_BANKHEIST IN_OR_ON_POSITION ", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {

            AlarmPlayer.Load();
            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
            {
                AllBankHeistEntities.Add(Game.LocalPlayer.Character.CurrentVehicle);
            }
            if (!CalloutRunning)
            {
                CalloutHandler();
            }


            return base.OnCalloutAccepted();
        }
        private void MakeNearbyPedsFlee()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {

                    GameFiber.Yield();

                    foreach (Ped entity in World.GetEntities(SpawnPoint, 80f, GetEntitiesFlags.ConsiderAllPeds | GetEntitiesFlags.ExcludePlayerPed | GetEntitiesFlags.ExcludePoliceOfficers))
                    {
                        GameFiber.Yield();
                        if (AllBankHeistEntities.Contains(entity))
                        {
                            continue;
                        }
                        if (entity != null)
                        {
                            if (entity.IsValid())
                            {

                                if (entity.Exists())
                                {
                                    if (entity != Game.LocalPlayer.Character)
                                    {
                                        if (entity != Game.LocalPlayer.Character.CurrentVehicle)
                                        {

                                            if (!entity.CreatedByTheCallingPlugin)
                                            {

                                                if (!AllBankHeistEntities.Contains(entity))
                                                {
                                                    if (Vector3.Distance(entity.Position, SpawnPoint) < 74f)
                                                    {
                                                        if (entity.IsInAnyVehicle(false))
                                                        {
                                                            if (entity.CurrentVehicle != null)
                                                            {

                                                                entity.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);


                                                            }
                                                        }
                                                        else
                                                        {
                                                            Rage.Native.NativeFunction.CallByName<uint>("TASK_SMART_FLEE_COORD", entity, SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z, 75f, 6000, true, true);
                                                        }

                                                    }
                                                    if (Vector3.Distance(entity.Position, SpawnPoint) < 65f)
                                                    {
                                                        if (entity.IsInAnyVehicle(false))
                                                        {
                                                            if (entity.CurrentVehicle.Exists())
                                                            {
                                                                entity.CurrentVehicle.Delete();
                                                            }
                                                        }
                                                        if (entity.Exists())
                                                        {
                                                            entity.Delete();
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
        private void CreateSpeedZone()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();

                    foreach (Vehicle veh in World.GetEntities(SpawnPoint, 75f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludePoliceCars | GetEntitiesFlags.ExcludeFiretrucks | GetEntitiesFlags.ExcludeAmbulances))
                    {
                        GameFiber.Yield();
                        if (AllBankHeistEntities.Contains(veh))
                        {
                            continue;
                        }
                        if (veh != null)
                        {
                            if (veh.Exists())
                            {
                                if (veh != Game.LocalPlayer.Character.CurrentVehicle)
                                {
                                    if (!veh.CreatedByTheCallingPlugin)
                                    {
                                        if (!AllBankHeistEntities.Contains(veh))
                                        {
                                            if (veh.Velocity.Length() > 0f)
                                            {
                                                Vector3 velocity = veh.Velocity;
                                                velocity.Normalize();
                                                velocity *= 0f;
                                                veh.Velocity = velocity;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }


        private void SpawnHostages()
        {
            for (int i = 0; i < HostagesLocations.Count; i++)
            {
                Ped hostage = new Ped(new Model(HostageModels[AssortedCalloutsHandler.rnd.Next(HostageModels.Length)]), HostagesLocations[i], HostagesHeadings[i]);

                hostage.IsPersistent = true;
                hostage.BlockPermanentEvents = true;
                Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(hostage, false);
                hostage.RelationshipGroup = "HOSTAGE";
                hostage.CanAttackFriendlies = false;
                AllHostages.Add(hostage);
                SpawnedHostages.Add(hostage);
                AllBankHeistEntities.Add(hostage);
                hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                hostage.Armor = 0;
                hostage.Health = 100;
                GameFiber.Yield();

                AliveHostagesCount++;
                TotalHostagesCount++;

            }

        }

        private void SpawnEMSAndFire()
        {
            for (int i = 0; i < AmbulanceLocations.Count; i++)
            {
                Vehicle ambulance = new Vehicle(new Model("AMBULANCE"), AmbulanceLocations[i], AmbulanceHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(ambulance);
                ambulance.IsPersistent = true;
                ambulance.IsSirenOn = true;
                ambulance.IsSirenSilent = true;
                AmbulancesList.Add(ambulance);
                AllBankHeistEntities.Add(ambulance);
            }
            for (int i = 0; i < ParamedicLocations.Count; i++)
            {
                Ped para = new Ped(new Model("S_M_M_PARAMEDIC_01"), ParamedicLocations[i], ParamedicHeadings[i]);
                para.IsPersistent = true;
                para.BlockPermanentEvents = true;
                ParamedicsList.Add(para);
                AllBankHeistEntities.Add(para);

            }
            for (int i = 0; i < FireTruckLocations.Count; i++)
            {
                Vehicle firetruck = new Vehicle(new Model("FIRETRUK"), FireTruckLocations[i], FireTruckHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(firetruck);
                firetruck.IsPersistent = true;
                firetruck.IsSirenOn = true;
                firetruck.IsSirenSilent = true;
                AmbulancesList.Add(firetruck);
                AllBankHeistEntities.Add(firetruck);
                Ped fireman = new Ped(new Model("S_M_Y_FIREMAN_01"), SpawnPoint, 0f);
                fireman.WarpIntoVehicle(firetruck, -1);
                fireman.BlockPermanentEvents = true;
                fireman.IsPersistent = true;
                FiremenList.Add(fireman);
                AllBankHeistEntities.Add(fireman);
            }
        }

        private void SpawnAllPoliceOfficers()
        {
            for (int i = 0; i < PoliceOfficersStandingLocations.Count; i++)
            {
                Ped officer = new Ped(new Model(LSPDModels[AssortedCalloutsHandler.rnd.Next(LSPDModels.Length)]), PoliceOfficersStandingLocations[i], PoliceOfficersStandingHeadings[i]);
                Functions.SetPedAsCop(officer);
                Functions.SetCopAsBusy(officer, true);
                officer.CanBeTargetted = false;
                officer.IsPersistent = true;
                officer.BlockPermanentEvents = true;
                officer.Inventory.GiveNewWeapon("WEAPON_PISTOL50", 10000, true);
                officer.RelationshipGroup = "COP";
                PoliceOfficersStandingSpawned.Add(officer);
                PoliceOfficersSpawned.Add(officer);
                AllBankHeistEntities.Add(officer);
                officer.CanAttackFriendlies = false;

            }
            for (int i = 0; i < PoliceOfficersAimingLocations.Count; i++)
            {
                Ped officer = new Ped(new Model(LSPDModels[AssortedCalloutsHandler.rnd.Next(LSPDModels.Length)]), PoliceOfficersAimingLocations[i], PoliceOfficersAimingHeadings[i]);
                Functions.SetPedAsCop(officer);
                Functions.SetCopAsBusy(officer, true);
                officer.IsPersistent = true;
                officer.CanBeTargetted = false;
                officer.BlockPermanentEvents = true;
                officer.Inventory.GiveNewWeapon("WEAPON_PISTOL50", 10000, true);
                officer.RelationshipGroup = "COP";
                PoliceOfficersAimingSpawned.Add(officer);
                PoliceOfficersSpawned.Add(officer);
                AllBankHeistEntities.Add(officer);
                officer.CanAttackFriendlies = false;
                Vector3 AimPoint;
                if (Vector3.Distance(officer.Position, PacificBankDoors[0]) < Vector3.Distance(officer.Position, PacificBankDoors[1]))
                {
                    AimPoint = PacificBankDoors[0];
                }
                else
                {
                    AimPoint = PacificBankDoors[1];
                }
                Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(officer, AimPoint.X, AimPoint.Y, AimPoint.Z, -1, false, false);

            }
            CaptainWells = new Ped(new Model("ig_fbisuit_01"), CaptainWellsLocation, CaptainWellsHeading);
            Functions.SetPedCantBeArrestedByPlayer(CaptainWells, true);

            CaptainWells.BlockPermanentEvents = true;
            CaptainWells.IsPersistent = true;
            CaptainWells.IsInvincible = true;
            CaptainWells.RelationshipGroup = "COP";
            CaptainWellsBlip = CaptainWells.AttachBlip();
            CaptainWellsBlip.Color = System.Drawing.Color.Green;
            AllBankHeistEntities.Add(CaptainWells);
        }

        private void SpawnAllBarriers()
        {
            for (int i = 0; i < BarrierLocations.Count; i++)
            {
                Rage.Object Barrier = PlaceBarrier(BarrierLocations[i], BarrierHeadings[i]);
                Barriers.Add(Barrier);
                AllBankHeistEntities.Add(Barrier);


            }

        }
        private void SpawnAllPoliceCars()
        {
            for (int i = 0; i < POLICECarLocations.Count; i++)
            {
                Vehicle car = new Vehicle("POLICE", POLICECarLocations[i], POLICECarHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(car);
                car.IsPersistent = true;
                car.IsSirenOn = true;
                car.IsSirenSilent = true;
                AllSpawnedPoliceVehicles.Add(car);
                AllBankHeistEntities.Add(car);
            }
            for (int i = 0; i < POLICE2CarLocations.Count; i++)
            {
                Vehicle car = new Vehicle("POLICE2", POLICE2CarLocations[i], POLICE2CarHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(car);
                car.IsPersistent = true;
                car.IsSirenOn = true;
                car.IsSirenSilent = true;
                AllSpawnedPoliceVehicles.Add(car);
                AllBankHeistEntities.Add(car);
            }
            for (int i = 0; i < POLICE3CarLocations.Count; i++)
            {
                Vehicle car = new Vehicle("POLICE3", POLICE3CarLocations[i], POLICE3CarHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(car);
                car.IsPersistent = true;
                car.IsSirenOn = true;
                car.IsSirenSilent = true;
                AllSpawnedPoliceVehicles.Add(car);
                AllBankHeistEntities.Add(car);
            }
            for (int i = 0; i < RiotLocations.Count; i++)
            {
                Vehicle car = new Vehicle("RIOT", RiotLocations[i], RiotHeadings[i]);
                Albo1125.Common.CommonLibrary.ExtensionMethods.RandomiseLicencePlate(car);
                car.IsPersistent = true;
                car.IsSirenOn = true;
                car.IsSirenSilent = true;
                AllSpawnedPoliceVehicles.Add(car);
                RiotVans.Add(car);
                AllBankHeistEntities.Add(car);
            }
        }
        private void SpawnBothSwatTeams()
        {
            for (int i = 0; i < SWATTeam1Locations.Count; i++)
            {

                Ped unit = new Ped("s_m_y_swat_01", SWATTeam1Locations[i], SWATTeam1Headings[i]);
                Functions.SetPedAsCop(unit);
                Functions.SetCopAsBusy(unit, true);
                unit.CanBeTargetted = false;
                unit.BlockPermanentEvents = true;
                unit.IsPersistent = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset(SWATWeapons[AssortedCalloutsHandler.rnd.Next(SWATWeapons.Length)]), 10000, true);
                unit.RelationshipGroup = "COP";
                //Rage.Native.NativeFunction.CallByName<uint>("SET_PED_TO_LOAD_COVER", unit, true);
                unit.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 1f, AnimationFlags.StayInEndFrame);
                //Rage.Native.NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( unit, 1, 1, 1, 1);
                Rage.Native.NativeFunction.Natives.SET_PED_PROP_INDEX(unit, 0, 0, 0, 2);
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 2);
                unit.CanAttackFriendlies = false;

                unit.Health = 209;
                unit.Armor = 92;

                SWATUnitsSpawned.Add(unit);
                SWATTeam1.Add(unit);
                AllBankHeistEntities.Add(unit);
            }
            for (int i = 0; i < SWATTeam2Locations.Count; i++)
            {


                Ped unit = new Ped("s_m_y_swat_01", SWATTeam2Locations[i], SWATTeam2Headings[i]);
                Functions.SetPedAsCop(unit);
                Functions.SetCopAsBusy(unit, true);
                unit.CanBeTargetted = false;

                unit.BlockPermanentEvents = true;
                unit.IsPersistent = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset(SWATWeapons[AssortedCalloutsHandler.rnd.Next(SWATWeapons.Length)]), 10000, true);
                unit.RelationshipGroup = "COP";
                //Rage.Native.NativeFunction.CallByName<uint>("SET_PED_TO_LOAD_COVER", unit, true);
                //Rage.Native.NativeFunction.Natives.SET_PED_COMPONENT_VARIATION( unit, 1, 1, 1, 1);
                unit.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 1f, AnimationFlags.StayInEndFrame);
                Rage.Native.NativeFunction.Natives.SET_PED_PROP_INDEX(unit, 0, 0, 0, 2);
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 2);
                unit.CanAttackFriendlies = false;

                unit.Health = 209;
                unit.Armor = 92;

                SWATUnitsSpawned.Add(unit);
                SWATTeam2.Add(unit);
                AllBankHeistEntities.Add(unit);
            }
        }
        private void SpawnNegotiationRobbers()
        {
            for (int i = 0; i < RobbersNegotiationLocations.Count; i++)
            {
                Ped unit = new Ped("mp_g_m_pros_01", RobbersNegotiationLocations[i], RobbersNegotiationHeadings[i]);
                if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 2, 1, 0);
                }
                else
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 1, 1, 0);
                }
                Functions.SetPedCantBeArrestedByPlayer(unit, true);

                unit.IsPersistent = true;
                unit.BlockPermanentEvents = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset(RobbersWeapons[AssortedCalloutsHandler.rnd.Next(RobbersWeapons.Length)]), 10000, true);
                unit.RelationshipGroup = "ROBBERS";
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
                unit.CanAttackFriendlies = false;

                unit.Armor = 145;
                unit.Health += 190;
                Robbers.Add(unit);
                AllBankHeistEntities.Add(unit);
            }

        }

        private void SpawnSneakyRobbers()
        {
            for (int i = 0; i < RobbersSneakyLocations.Count; i++)
            {
                if (AssortedCalloutsHandler.rnd.Next(5) >= 3)
                {
                    Ped unit = new Ped("mp_g_m_pros_01", RobbersSneakyLocations[i], RobbersSneakyHeadings[i]);
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 2, 1, 0);
                    }
                    else
                    {
                        NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 1, 1, 0);
                    }
                    Functions.SetPedCantBeArrestedByPlayer(unit, true);
                    unit.IsPersistent = true;
                    unit.BlockPermanentEvents = true;
                    unit.Inventory.GiveNewWeapon(new WeaponAsset(RobbersSneakyWeapons[AssortedCalloutsHandler.rnd.Next(RobbersSneakyWeapons.Length)]), 10000, true);
                    unit.RelationshipGroup = "SNEAKYROBBERS";
                    Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
                    unit.CanAttackFriendlies = false;
                    unit.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 1f, AnimationFlags.StayInEndFrame);
                    unit.Armor = 80;
                    unit.Health += 185;
                    RobbersSneakySpawned.Add(unit);
                    AllBankHeistEntities.Add(unit);

                }
                else
                {
                    RobbersSneakySpawned.Add(null);
                }
            }
        }

        private void SpawnAssaultRobbers()
        {
            for (int i = 0; i < RobbersAssaultLocations.Count; i++)
            {
                Ped unit = new Ped("mp_g_m_pros_01", RobbersAssaultLocations[i], RobbersAssaultHeadings[i]);
                if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 2, 1, 0);
                }
                else
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 1, 1, 0);
                }
                Functions.SetPedCantBeArrestedByPlayer(unit, true);
                unit.IsPersistent = true;
                unit.BlockPermanentEvents = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset(RobbersWeapons[AssortedCalloutsHandler.rnd.Next(RobbersWeapons.Length)]), 10000, true);
                unit.Inventory.GiveNewWeapon(new WeaponAsset(Grenades[AssortedCalloutsHandler.rnd.Next(Grenades.Length)]), 4, false);
                unit.RelationshipGroup = "ROBBERS";
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
                unit.CanAttackFriendlies = false;
                unit.Armor = 238;
                unit.Health += 280;
                Robbers.Add(unit);
                AllBankHeistEntities.Add(unit);
            }
        }

        private void SpawnVaultRobbers()
        {
            for (int i = 0; i < RobbersVaultLocations.Count; i++)
            {
                Ped unit = new Ped("mp_g_m_pros_01", RobbersVaultLocations[i], RobbersVaultHeadings[i]);
                if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 2, 1, 0);
                }
                else
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, 1, 1, 0);
                }
                Functions.SetPedCantBeArrestedByPlayer(unit, true);
                unit.IsPersistent = true;
                unit.BlockPermanentEvents = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTSMG"), 10000, true);
                unit.RelationshipGroup = "ROBBERS";
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
                unit.CanAttackFriendlies = false;
                unit.Armor = 95;
                unit.Health += 230;
                RobbersVault.Add(unit);
                AllBankHeistEntities.Add(unit);
            }
            HandleVaultRobbers();
        }
        private void SpawnMiniGunRobber()
        {
            MiniGunRobber = new Ped("mp_g_m_pros_01", MiniGunRobberLocation, MiniGunRobberHeading);
            if (AssortedCalloutsHandler.rnd.Next(2) == 0)
            {
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(MiniGunRobber, 9, 2, 1, 0);
            }
            else
            {
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(MiniGunRobber, 9, 1, 1, 0);
            }
            Functions.SetPedCantBeArrestedByPlayer(MiniGunRobber, true);
            MiniGunRobber.IsPersistent = true;
            MiniGunRobber.BlockPermanentEvents = true;
            MiniGunRobber.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_COMBATMG"), 1000, true);
            //MiniGunRobber.IsPositionFrozen = true;
            Rage.Native.NativeFunction.Natives.SetPedCombatAbility(MiniGunRobber, 3);
            MiniGunRobber.RelationshipGroup = "ROBBERS";
            MiniGunRobber.CanAttackFriendlies = false;
            Rage.Native.NativeFunction.Natives.SET_PED_DROPS_WEAPONS_WHEN_DEAD(MiniGunRobber, false);
            Rage.Native.NativeFunction.Natives.SET_PED_SHOOT_RATE(MiniGunRobber, 1000);
            MiniGunRobber.Armor = 60;
            MiniGunRobber.Health += 185;
            AllBankHeistEntities.Add(MiniGunRobber);
        }

        private Rage.Object PlaceBarrier(Vector3 Location, float Heading)
        {
            Rage.Object Barrier = new Rage.Object("prop_barrier_work05", Location);
            Barrier.Heading = Heading;
            Barrier.IsPositionFrozen = true;
            Barrier.IsPersistent = true;
            Rage.Object invWall = new Rage.Object("p_ice_box_01_s", Barrier.Position);
            Ped invPed = new Ped(invWall.Position);
            invPed.IsVisible = false;
            invPed.IsPositionFrozen = true;
            invPed.BlockPermanentEvents = true;
            invPed.IsPersistent = true;
            invWall.Heading = Heading;
            invWall.IsVisible = false;
            invWall.IsPersistent = true;

            InvisWalls.Add(invWall);
            BarrierPeds.Add(invPed);
            return Barrier;

        }
        private void ClearUnrelatedEntities()
        {

            foreach (Ped entity in World.GetEntities(SpawnPoint, 50f, GetEntitiesFlags.ConsiderAllPeds))
            {
                GameFiber.Yield();
                if (entity != null)
                {
                    if (entity.IsValid())
                    {
                        if (entity.Exists())
                        {
                            if (entity != Game.LocalPlayer.Character)
                            {
                                if (entity != Game.LocalPlayer.Character.CurrentVehicle)
                                {
                                    if (!entity.CreatedByTheCallingPlugin)
                                    {

                                        if (!AllBankHeistEntities.Contains(entity))
                                        {
                                            if (Vector3.Distance(entity.Position, SpawnPoint) < 50f)
                                            {
                                                entity.Delete();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (Vehicle entity in World.GetEntities(SpawnPoint, 50f, GetEntitiesFlags.ConsiderGroundVehicles))
            {
                GameFiber.Yield();
                if (entity != null)
                {
                    if (entity.IsValid())
                    {
                        if (entity.Exists())
                        {
                            if (entity != Game.LocalPlayer.Character)
                            {
                                if (entity != Game.LocalPlayer.Character.CurrentVehicle)
                                {
                                    if (!entity.CreatedByTheCallingPlugin)
                                    {

                                        if (!AllBankHeistEntities.Contains(entity))
                                        {
                                            if (Vector3.Distance(entity.Position, SpawnPoint) < 50f)
                                            {
                                                entity.Delete();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void HandleCustomRespawn()
        {
            HandlingRespawn = true;
            SWATFollowing = false;
            MiniGunRobberFiring = false;
            TimesDied++;
            AudioState OldAudioState = CurrentAudioState;
            CurrentAudioState = AudioState.None;
            AudioStateChanged = true;
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.IsScreenFadedOut)
                    {
                        break;
                    }
                }
                GameFiber.Sleep(1000);
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.LocalPlayer.Character.Exists())
                    {
                        if (Game.LocalPlayer.Character.IsAlive)
                        {
                            break;
                        }
                    }
                }
                Game.LocalPlayer.HasControl = false;
                Game.FadeScreenOut(1, true);
                Game.LocalPlayer.Character.WarpIntoVehicle(AmbulancesList[0], 2);


                Game.FadeScreenIn(2500, true);
                Game.LocalPlayer.HasControl = true;
                CurrentAudioState = OldAudioState;
                AudioStateChanged = true;
                Game.LocalPlayer.Character.WarpIntoVehicle(AmbulancesList[0], 2);
                GameFiber.Yield();
                if (Game.LocalPlayer.Character.IsInVehicle(AmbulancesList[0], false))
                {
                    Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                }
                while (true)
                {
                    GameFiber.Yield();
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, AmbulancesList[0].Position) < 70f)
                    {
                        break;
                    }
                    if (Game.LocalPlayer.Character.IsAlive)
                    {
                        Game.DisplayHelp("Press ~b~Enter ~s~when spawned to spawn to the ambulance.");
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.Enter))
                        {
                            Game.LocalPlayer.Character.WarpIntoVehicle(AmbulancesList[0], 2);
                            Game.HideHelp();
                            GameFiber.Sleep(1000);
                        }
                    }
                }
                MiniGunRobberFiring = false;
                HandlingRespawn = false;

            });
        }


        public override void Process()
        {
            base.Process();

            if (CalloutRunning)
            {
                if (!HandlingRespawn)
                {
                    if (Game.LocalPlayer.Character.IsDead)
                    {
                        HandleCustomRespawn();
                    }
                }
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
            AlarmPlayer.Stop();
            SpeechHandler.DisplayTime = false;
            SpeechHandler.DisplayingBankHeistSpeech = false;
            Game.LocalPlayer.Character.IsPositionFrozen = false;
            Game.LocalPlayer.HasControl = true;
            //Game.LocalPlayer.Character.CanAttackFriendlies = false;
            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DEFENSE_MODIFIER(Game.LocalPlayer, 1f);
            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DAMAGE_MODIFIER(Game.LocalPlayer, 1f);
            Rage.Native.NativeFunction.Natives.RESET_AI_WEAPON_DAMAGE_MODIFIER();
            Rage.Native.NativeFunction.Natives.RESET_AI_MELEE_WEAPON_DAMAGE_MODIFIER();
            if (SideDoorBlip.Exists()) { SideDoorBlip.Delete(); }
            if (MobilePhone.Exists()) { MobilePhone.Delete(); }
            ToggleMobilePhone(Game.LocalPlayer.Character, false);
            if (!CalloutFinished)
            {
                if (Maria.Exists()) { Maria.Delete(); }
                if (MariaCop.Exists()) { MariaCop.Delete(); }
                if (MariaCopCar.Exists()) { MariaCopCar.Delete(); }
                if (CaptainWells.Exists())
                {
                    CaptainWells.Delete();
                }
                if (CaptainWellsBlip.Exists())
                {
                    CaptainWellsBlip.Delete();
                }
                if (MiniGunRobber.Exists())
                {
                    MiniGunRobber.Delete();
                }

                foreach (Ped i in Robbers)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in RobbersVault)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in FiremenList)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in ParamedicsList)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in RobbersSneakySpawned)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in AllHostages)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in PoliceOfficersSpawned)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in SWATTeam1)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in SWATTeam2)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }

                foreach (Vehicle i in AllSpawnedPoliceVehicles)
                {
                    if (i.Exists()) { i.Delete(); }
                }
                foreach (Vehicle i in FireTrucksList)
                {
                    if (i.Exists()) { i.Delete(); }
                }
                foreach (Vehicle i in AmbulancesList)
                {
                    if (i.Exists()) { i.Delete(); }
                }
                foreach (Rage.Object i in Barriers)
                {
                    if (i.Exists()) { i.Delete(); }
                }
                foreach (Rage.Object i in InvisWalls)
                {
                    if (i.Exists()) { i.Delete(); }
                }
                foreach (Ped i in BarrierPeds)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
            }
            else
            {
                foreach (Ped i in RobbersVault)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                if (Maria.Exists()) { Maria.Dismiss(); }
                if (MariaCop.Exists()) { MariaCop.Dismiss(); }
                if (MariaCopCar.Exists()) { MariaCopCar.Dismiss(); }
                if (CaptainWells.Exists())
                {
                    CaptainWells.Dismiss();
                }
                if (CaptainWellsBlip.Exists())
                {
                    CaptainWellsBlip.Delete();
                }
                if (MiniGunRobber.Exists())
                {
                    if (MiniGunRobber.IsAlive) { MiniGunRobber.Delete(); }
                    else
                    {
                        MiniGunRobber.Dismiss();
                    }
                }
                foreach (Ped i in FiremenList)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in ParamedicsList)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Vehicle i in FireTrucksList)
                {
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }
                foreach (Vehicle i in AmbulancesList)
                {
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }
                foreach (Ped i in RobbersSneakySpawned)
                {
                    if (i.Exists())
                    {
                        if (i.IsAlive) { i.Delete(); }
                        else
                        {
                            i.Dismiss();
                        }
                    }
                }
                foreach (Ped i in Robbers)
                {
                    if (i.Exists())
                    {
                        if (i.IsAlive) { i.Delete(); }
                        else
                        {
                            i.Dismiss();
                        }
                    }
                }
                foreach (Ped i in AllHostages)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in PoliceOfficersSpawned)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in SWATTeam1)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in SWATTeam2)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Vehicle i in AllSpawnedPoliceVehicles)
                {
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }
                foreach (Ped i in BarrierPeds)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Rage.Object i in InvisWalls)
                {
                    if (i.Exists()) { i.Delete(); }
                }

                foreach (Rage.Object i in Barriers)
                {
                    if (i.Exists()) { i.Delete(); }
                }
            }
        }
    }
}
