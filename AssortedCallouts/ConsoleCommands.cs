using LSPD_First_Response.Engine.Scripting;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Attributes;
using Rage.ConsoleCommands.AutoCompleters;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssortedCallouts
{
    //Currently unused as Callout Manager was released.
    internal static class ConsoleCommands
    {
        [Rage.Attributes.ConsoleCommand("Starts the LSPDFR callout with the specified callout name.")]
        private static void Command_StartCallout([ConsoleCommandParameter(AutoCompleterType = typeof(CalloutNameAutoCompleter))] string CalloutName)
        {
            if (GetRegisteredCalloutNames().Contains(CalloutName))
            {
                Game.LogTrivial("[Assorted Callouts.StartCallout]: Starting callout: " + CalloutName);
                Functions.StartCallout(CalloutName);
            }
            else
            {
                Game.LogTrivial("[Assorted Callouts.StartCallout]: " + CalloutName + " is not a registered callout.");
            }
        }

        [Serializable]
        [ConsoleCommandParameterAutoCompleterAttribute(typeof(string))]
        private class CalloutNameAutoCompleter : ConsoleCommandParameterAutoCompleter
        {
            public CalloutNameAutoCompleter(Type type)
                : base(type)
            {
            }

            public override void UpdateOptions()
            {
                this.Options.Clear();

                foreach (string name in GetRegisteredCalloutNames())
                {
                    this.Options.Add(new Rage.ConsoleCommands.AutoCompleteOption(name, null, null));
                }
            }
        }

        public static List<dynamic> GetRegisteredCalloutNames()
        {
            List<dynamic> RegisteredCalloutNames = new List<dynamic>();
            Game.LogTrivial("Adding callout names! ");
            foreach (Callout callout in ScriptComponent.GetAllByType<Callout>())
            {

                Game.LogTrivial("Adding callout name: " + callout.ScriptInfo.Name);
                RegisteredCalloutNames.Add(callout.ScriptInfo.Name);

            }
            return RegisteredCalloutNames;
        }

        public static UIMenu CalloutSelectMenu;
        public static UIMenuListItem CalloutListItem;
        public static UIMenuListItem DelayItem;
        public static UIMenuItem ConfirmItem;

        public static MenuPool _MenuPool;
        public static void CreateMenu()
        {
            GetRegisteredCalloutNames();
            //_MenuPool = new MenuPool();
            //CalloutSelectMenu = new UIMenu("Callout Selector", "By Albo1125");

            //CalloutSelectMenu.AddItem(CalloutListItem = new UIMenuListItem("Callout", GetRegisteredCalloutNames(), 0));
            //CalloutSelectMenu.AddItem(ConfirmItem = new UIMenuItem("Confirm"));

            //_MenuPool.Add(CalloutSelectMenu);
            //CalloutSelectMenu.RefreshIndex();
            //CalloutSelectMenu.MouseControlsEnabled = false;
            //CalloutSelectMenu.AllowCameraMovement = true;
            //CalloutSelectMenu.OnItemSelect += OnItemSelect;
            //Game.FrameRender += Process;
        }

        public static void OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (sender != CalloutSelectMenu) { return; }
            if (selectedItem == ConfirmItem)
            {
                string name = CalloutListItem.IndexToItem(CalloutListItem.Index);
                Command_StartCallout(name);
            }
        }
        public static void Process(object sender, GraphicsEventArgs e)
        {

            _MenuPool.ProcessMenus();


        }
    }
}
