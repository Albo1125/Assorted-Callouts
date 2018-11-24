using Albo1125.Common.CommonLibrary;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssortedCallouts.Callouts.StoreRelated
{
    internal class Store
    {
        public string Name;
        
        public TupleList<Vector3, float> ShopKeeperSpawnData;
        public TupleList<Vector3, float> RobberSpawnData;

        public TupleList<Vector3, float> ShopliftingSecuritySpawnData;
        public TupleList<Vector3, float> ShopliftingOffenderSpawnData;
        public Store(string _Name, TupleList<Vector3, float> _ShopKeeperSpawnData, TupleList<Vector3, float> _RobberSpawnData, TupleList<Vector3, float> _ShopliftingSecuritySpawnData, TupleList<Vector3, float> _ShopliftingOffenderSpawnData)
        {
            this.Name = _Name;
            this.ShopKeeperSpawnData = _ShopKeeperSpawnData;
            this.RobberSpawnData = _RobberSpawnData;
            this.ShopliftingSecuritySpawnData = _ShopliftingSecuritySpawnData;
            this.ShopliftingOffenderSpawnData = _ShopliftingOffenderSpawnData;
        }
        public static Model[] StoreRobberModels = new Model[] { "mp_g_m_pros_01" };
        public static Model[] ShoplifterModels = new Model[] { "a_m_y_mexthug_01", "a_f_m_downtown_01" };
        public static Model[] StoreShopkeeperModels = new Model[] { "s_m_m_autoshop_01", "mp_m_shopkeep_01", "s_f_m_sweatshop_01" };
        public static Store[] Stores;

        public static void InitialiseStores()
        {
            List<Store> StoresList = new List<Store>();
            for (int i = 0; i < StoreNames.Count; i++)
            {
                StoresList.Add(new Store(StoreNames[i], StoreShopKeeperSpawnData[i], StoreRobberSpawnData[i], StoreShopliftingSecuritySpawnData[i], StoreShopliftingOffenderSpawnData[i]));

            }
            Game.LogTrivial("Stores initialised");
            Stores = StoresList.ToArray();
        }
        #region SpawnData
        private static List<string> StoreNames = new List<string>()
        {
            "Strawberry Discount Store",
            "Textile City Binco Clothes",
            "Vespucci Binco Clothes",
            "Grapeseed Discount Store",
            "Zancudo River Discount Store",
            "Grand Senora Desert Discount Store",
            "Paleto Bay Discount Store",
            "Downtown Vinewood Supermarket",
            "Mirror Park Gas Station",
            "Murrieta Heights Supermarket",
            "Strawberry Supermarket",
            "Davis Gas Station",
            "Little Seoul Gas Station",
            "Vespucci Canals Rob's Liquor",
            "Morningwood Rob's Liquor",
            "Richman Gas Station",
            "Banham Canyon Rob's Liquor",
            "Banham Canyon Supermarket",
            "Chumash Supermarket",
            "Harmony Supermarket",
            "Grand Senora Desert Rob's Liquor",
            "Sandy Shores Supermarket",
            "Grand Senora Desert Supermarket",
            "Grapeseed Gas station",
            "Chiliad Supermarket",
            "Tavatiam Supermarket",
        };

        private static List<TupleList<Vector3, float>> StoreShopKeeperSpawnData = new List<TupleList<Vector3, float>>()
        {

            new TupleList<Vector3, float>
            {
            { new Vector3(73.87572f, -1392.849f, 29.37613f), 263.9599f },
            { new Vector3(75.60529f, -1387.597f, 29.37598f), 168.2955f },
            { new Vector3(77.6536f, -1387.38f, 29.37613f), 169.2318f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(427.145f, -806.8593f, 29.49114f), 97.54604f },
            { new Vector3(426.1632f, -811.7161f, 29.49112f), 16.15875f },
            { new Vector3(422.6245f, -811.7634f, 29.49112f), 1.964871f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-822.653f, -1071.912f, 11.32811f), 208.5814f },
            { new Vector3(-818.0529f, -1070.228f, 11.3281f), 120.7465f },
            { new Vector3(-816.6532f, -1072.882f, 11.3281f), 138.5147f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1695.573f, 4822.746f, 42.06311f), 95.36308f },
            { new Vector3(1695.482f, 4817.43f, 42.06308f), 30.9859f },
            { new Vector3(1691.995f, 4816.905f, 42.06309f), 3.808188f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1102.171f, 2711.92f, 19.10787f), 244.3523f },
            { new Vector3(-1097.944f, 2714.826f, 19.10785f), 156.7725f },
            { new Vector3(-1095.329f, 2712.077f, 19.10785f), 137.8905f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1197.127f, 2711.719f, 38.22263f), 155.001f },
            { new Vector3(1202.171f, 2710.635f, 38.2226f), 96.27752f },
            { new Vector3(1202.168f, 2707.25f, 38.2226f), 99.16246f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(5.792656f, 6511.06f, 31.87785f), 53.38403f },
            { new Vector3(1.226573f, 6508.191f, 31.87783f), 320.5744f },
            { new Vector3(-1.193835f, 6511.018f, 31.87783f), 310.5506f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(372.285f, 326.7229f, 103.5664f), 245.4788f },
            { new Vector3(372.8542f, 328.5057f, 103.5664f), 248.5461f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1164.891f, -321.9834f, 69.20512f), 109.593f },
            { new Vector3(1165.305f, -324.1071f, 69.20512f), 91.33096f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1133.908f, -981.8345f, 46.41584f), 277.1535f },
            { new Vector3(1133.994f, -983.168f, 46.41584f), 273.8027f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(24.27961f, -1347.195f, 29.49703f), 268.7898f },
            { new Vector3(24.29598f, -1344.917f, 29.49703f), 242.9734f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-46.29862f, -1757.924f, 29.42101f), 59.70727f },
            { new Vector3(-47.25236f, -1759.288f, 29.42101f), 42.86699f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-705.9761f, -913.3507f, 19.21559f), 96.05964f },
            { new Vector3(-705.6188f, -915.0143f, 19.21559f), 88.70708f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1222.418f, -909.0228f, 12.32635f), 39.99239f },
            { new Vector3(-1220.993f, -908.1355f, 12.32635f), 37.10778f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1485.597f, -378.1989f, 40.16342f), 131.8357f },
            { new Vector3(-1486.325f, -377.1536f, 40.16342f), 136.0929f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1820.003f, 794.6925f, 138.0811f), 130.5226f },
            { new Vector3(-1819.079f, 793.6499f, 138.0773f), 126.3863f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-2966.031f, 390.1378f, 15.04331f), 81.13749f },
            { new Vector3(-2965.983f, 391.4838f, 15.04331f), 86.91148f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3039.178f, 584.0132f, 7.90893f), 13.24055f },
            { new Vector3(-3040.889f, 583.5419f, 7.90893f), 350.3043f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3242.512f, 999.69f, 12.83072f), 352.1508f },
            { new Vector3(-3244.52f, 999.9537f, 12.83072f), 339.3682f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(549.5076f, 2671.187f, 42.15652f), 88.31507f },
            { new Vector3(549.7433f, 2669.421f, 42.15652f), 94.43654f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1166.607f, 2711.081f, 38.1577f), 182.275f },
            { new Vector3(1165.059f, 2711.137f, 38.1577f), 186.4945f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1959.749f, 3739.731f, 32.34375f), 292.6686f },
            { new Vector3(1958.865f, 3741.605f, 32.34375f), 289.5545f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2677.738f, 3279.161f, 55.24114f), 333.0525f },
            { new Vector3(2676.018f, 3280.233f, 55.24114f), 318.0694f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1698.301f, 4922.422f, 42.06366f), 336.4959f },
            { new Vector3(1696.598f, 4923.424f, 42.06366f), 317.921f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1727.516f, 6415.369f, 35.03723f), 243.5433f },
            { new Vector3(1728.483f, 6417.018f, 35.03723f), 253.799f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2557.073f, 380.4679f, 108.623f), 356.9159f },
            { new Vector3(2555.545f, 380.6382f, 108.623f), 1.510959f },
            },


        };
        private static List<TupleList<Vector3, float>> StoreRobberSpawnData = new List<TupleList<Vector3, float>>()
        {

             new TupleList<Vector3, float>
            {
            { new Vector3(75.91751f, -1391.883f, 29.37615f), 114.3616f },
            { new Vector3(76.16069f, -1389.982f, 29.37615f), 19.93308f },
            { new Vector3(78.69604f, -1389.494f, 29.37615f), 7.40131f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(425.0366f, -807.4084f, 29.49113f), 291.1364f },
            { new Vector3(424.7376f, -809.5753f, 29.49224f), 217.1934f },
            { new Vector3(422.1451f, -809.3337f, 29.49114f), 201.4584f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-820.9958f, -1073.383f, 11.32811f), 43.43138f },
            { new Vector3(-819.3976f, -1072.761f, 11.32906f), 335.4849f },
            { new Vector3(-818.3046f, -1074.598f, 11.32811f), 298.9607f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1693.365f, 4821.716f, 42.06312f), 289.7472f },
            { new Vector3(1693.838f, 4819.305f, 42.0641f), 215.6228f },
            { new Vector3(1691.562f, 4818.95f, 42.06312f), 222.3499f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1100.294f, 2710.429f, 19.10785f), 48.32264f },
            { new Vector3(-1098.755f, 2712.339f, 19.10868f), 338.6742f },
            { new Vector3(-1096.751f, 2709.972f, 19.10787f), 322.1796f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1197.668f, 2709.616f, 38.2226f), 13.32392f },
            { new Vector3(1200.074f, 2709.42f, 38.22372f), 318.5346f },
            { new Vector3(1200.087f, 2707.18f, 38.22263f), 286.6486f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(3.368677f, 6512.238f, 31.87785f), 242.7346f },
            { new Vector3(1.847083f, 6510.697f, 31.87863f), 164.7863f },
            { new Vector3(0.07227093f, 6512.912f, 31.87785f), 143.9319f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(374.295f, 325.8951f, 103.5664f), 71.65144f },
            { new Vector3(374.8215f, 327.8206f, 103.5664f), 67.57134f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1163.107f, -322.1323f, 69.20507f), 279.6259f },
            { new Vector3(1163.093f, -324.2025f, 69.20507f), 279.2135f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1135.899f, -981.3351f, 46.41584f), 106.3927f },
            { new Vector3(1136.195f, -982.9218f, 46.41584f), 84.65549f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(26.07287f, -1347.799f, 29.49703f), 69.87182f },
            { new Vector3(25.96026f, -1345.437f, 29.49703f), 93.74947f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-47.55311f, -1756.105f, 29.421f), 220.5662f },
            { new Vector3(-49.16573f, -1757.922f, 29.421f), 241.7435f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-707.8306f, -913.1216f, 19.21559f), 265.6182f },
            { new Vector3(-707.9714f, -914.6878f, 19.21559f), 266.9786f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1223.807f, -907.2291f, 12.32635f), 213.2617f },
            { new Vector3(-1222.49f, -906.4473f, 12.32635f), 204.7606f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1487.121f, -379.896f, 40.16343f), 316.503f },
            { new Vector3(-1488.341f, -378.5851f, 40.16343f), 300.8844f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1821.694f, 793.3987f, 138.1235f), 316.2558f },
            { new Vector3(-1820.754f, 792.1705f, 138.1204f), 310.1198f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-2968.357f, 390.2774f, 15.04331f), 269.6022f },
            { new Vector3(-2968.377f, 391.8663f, 15.04331f), 257.6937f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3039.274f, 586.0203f, 7.908931f), 194.1356f },
            { new Vector3(-3041.396f, 585.567f, 7.908931f), 203.9648f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3241.84f, 1001.37f, 12.83072f), 173.9005f },
            { new Vector3(-3244.12f, 1001.509f, 12.83072f), 176.7522f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(547.292f, 2671.695f, 42.15514f), 280.7893f },
            { new Vector3(547.6602f, 2669.136f, 42.1565f), 284.808f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1166.563f, 2709.253f, 38.1577f), 8.957232f },
            { new Vector3(1165.26f, 2709.188f, 38.1577f), 341.7537f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1961.445f, 3740.613f, 32.34375f), 104.4002f },
            { new Vector3(1960.316f, 3742.599f, 32.34375f), 121.1306f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2679.14f, 3280.535f, 55.24114f), 146.3951f },
            { new Vector3(2676.704f, 3281.756f, 55.24114f), 155.5236f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1699.643f, 4924.007f, 42.06364f), 133.3625f },
            { new Vector3(1697.902f, 4925.208f, 42.06364f), 148.3102f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1729.161f, 6414.143f, 35.03723f), 63.85295f },
            { new Vector3(1730.235f, 6416.45f, 35.03723f), 70.0782f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2557.419f, 382.5004f, 108.623f), 185.8276f },
            { new Vector3(2555.261f, 382.7857f, 108.623f), 183.1302f },
            },
                    


        };
        private static List<TupleList<Vector3, float>> StoreShopliftingSecuritySpawnData = new List<TupleList<Vector3, float>>()
        {

            new TupleList<Vector3, float>
            {
            { new Vector3(71.72543f, -1389.92f, 29.38223f), 177.8002f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(429.3726f, -809.4645f, 29.49723f), 352.8965f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-821.4285f, -1068.608f, 11.33421f), 110.044f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1697.865f, 4819.82f, 42.06919f), 359.9569f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1101.442f, 2715.665f, 19.11395f), 128.402f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1200.19f, 2713.786f, 38.2287f), 77.59583f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(4.642973f, 6507.379f, 31.88394f), 295.7683f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(376.9604f, 333.0494f, 103.5664f), 258.8246f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1161.192f, -315.7093f, 69.20507f), 10.38225f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1130.385f, -979.7842f, 46.41584f), 177.5976f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(26.95461f, -1339.391f, 29.49703f), 256.414f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-43.71674f, -1750.918f, 29.42101f), 329.7601f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-708.4822f, -906.0571f, 19.21559f), 349.9077f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1222.499f, -912.7741f, 12.32636f), 302.9733f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1481.693f, -377.9337f, 40.16342f), 37.03875f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1826.825f, 798.1627f, 138.1584f), 36.33418f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-2963.307f, 387.5336f, 15.04331f), 348.7149f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3047.105f, 584.5887f, 7.908928f), 2.994713f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3249.588f, 1003.759f, 12.83071f), 359.0358f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(547.0899f, 2663.224f, 42.1565f), 102.9531f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1169.04f, 2714.217f, 38.1577f), 77.46152f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1958.752f, 3748.135f, 32.34375f), 300.3714f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2672.532f, 3285.331f, 55.24114f), 329.1084f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1705.425f, 4920.527f, 42.06364f), 228.7043f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1733.975f, 6420.666f, 35.03723f), 243.9718f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2549.797f, 384.1049f, 108.623f), 356.4645f },
            },
    

        };
        private static List<TupleList<Vector3, float>> StoreShopliftingOffenderSpawnData = new List<TupleList<Vector3, float>>()
        {

             new TupleList<Vector3, float>
            {
            { new Vector3(71.34902f, -1391.999f, 29.37614f), 5.389905f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(429.6338f, -807.4318f, 29.49114f), 177.0755f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-823.696f, -1069.546f, 11.32811f), 293.2254f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1697.955f, 4822.426f, 42.06312f), 180.6367f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1103.394f, 2714.06f, 19.10787f), 312.5538f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1197.552f, 2714.153f, 38.22263f), 270.7698f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(6.939451f, 6508.849f, 31.87785f), 126.6059f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(380.2518f, 331.9175f, 103.5664f), 72.75279f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1160.614f, -313.4612f, 69.20507f), 188.0524f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1130.49f, -982.498f, 46.41584f), 359.6723f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(29.71065f, -1339.849f, 29.49703f), 75.00872f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-42.51632f, -1749.066f, 29.42101f), 135.9664f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-708.6992f, -904.007f, 19.21559f), 182.4857f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1220.306f, -911.7875f, 12.32636f), 116.0872f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1483.106f, -375.5905f, 40.16342f), 202.0603f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-1828.516f, 799.7982f, 138.1692f), 231.8204f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-2962.479f, 389.6163f, 15.04331f), 149.6196f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3047.729f, 587.5536f, 7.908929f), 187.4691f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(-3249.053f, 1006.284f, 12.83071f), 153.784f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(543.9949f, 2663.222f, 42.15653f), 271.1956f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1166.501f, 2714.638f, 38.1577f), 249.95f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1961.019f, 3749.131f, 32.34375f), 130.2937f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2674.455f, 3287.662f, 55.24114f), 127.3502f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1706.996f, 4919.216f, 42.06364f), 48.64163f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(1736.183f, 6419.188f, 35.03723f), 48.66093f },
            },
            new TupleList<Vector3, float>
            {
            { new Vector3(2550.023f, 386.597f, 108.623f), 167.7013f },
            },


        };
        #endregion
    }
}
