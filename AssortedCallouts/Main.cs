using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Reflection;

namespace AssortedCallouts
{

    using LSPD_First_Response.Mod.API;
    using Rage;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows.Forms;
    using Extensions;

    /// <summary>
    /// Do not rename! Attributes or inheritance based plugins will follow when the API is more in depth.
    /// </summary>
    internal class Main : Plugin
    {
        /// <summary>
        /// Constructor for the main class, same as the class, do not rename.
        /// </summary>
        public Main()
        {

            Albo1125.Common.UpdateChecker.VerifyXmlNodeExists(PluginName, FileID, DownloadURL, Path);
            Albo1125.Common.DependencyChecker.RegisterPluginForDependencyChecks(PluginName);
        }

        /// <summary>
        /// Called when the plugin ends or is terminated to cleanup
        /// </summary>
        public override void Finally()
        {

        }

        /// <summary>
        /// Called when the plugin is first loaded by LSPDFR
        /// </summary>
        public override void Initialize()
        {
            //Event handler for detecting if the player goes on duty

            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Game.LogTrivial("Assorted Callouts " + Assembly.GetExecutingAssembly().GetName().Version.ToString() +", developed by Albo1125, has been initialised.");
            Game.LogTrivial("Go on duty to start Assorted Callouts.");


        }
        //Dependencies
        internal static Version Albo1125CommonVer = new Version("6.6.3.0");
        internal static Version MadeForGTAVersion = new Version("1.0.1604.1");
        internal static string[] AudioFilesToCheckFor = new string[] { "LSPDFR/Police Scanner/Assorted Callouts Audio/Crimes/CRIME_ROBBERY.wav", "LSPDFR/Police Scanner/Assorted Callouts Audio/Crimes/CRIME_SUSPICIOUSVEHICLE.wav" };
        internal static float MinimumRPHVersion = 0.51f;
        internal static Version RAGENativeUIVersion = new Version("1.6.3.0");
        internal static Version MadeForLSPDFRVersion = new Version("0.4.39.22580");

        internal static string DownloadURL = "https://www.lcpdfr.com/files/file/9689-assorted-callouts-shoplifting-store-robberies-petrolgas-theft-pacific-bank-heist-hot-pursuit-traffic-stop-backup-required-illegal-immigrants-in-truck/";
        internal static string FileID = "9689";
        internal static string PluginName = "AssortedCallouts";
        internal static string Path = "Plugins/LSPDFR/AssortedCallouts.dll";

        /// <summary>
        /// The event handler mentioned above,
        /// </summary>
        static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                Albo1125.Common.UpdateChecker.InitialiseUpdateCheckingProcess();
                if (Albo1125.Common.DependencyChecker.DependencyCheckMain(PluginName, Albo1125CommonVer, MinimumRPHVersion, MadeForGTAVersion, MadeForLSPDFRVersion, RAGENativeUIVersion, AudioFilesToCheckFor))
                {               
                    AssortedCalloutsHandler.Initialise();
                }
            }
        }
    }
}
