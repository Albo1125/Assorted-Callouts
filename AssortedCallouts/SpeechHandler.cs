using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using AssortedCallouts.Extensions;
using Rage.Native;
using Albo1125.Common.CommonLibrary;

namespace AssortedCallouts
{
    internal static class SpeechHandler
    {
        public static bool HandlingSpeech;
        public static void HandleSpeech (string PersonTalking, List<string> Lines, Ped TalkingPed = null)
        {
            HandlingSpeech = true;
            Vector3 PlayerPos = Game.LocalPlayer.Character.Position;
            float PlayerHeading = Game.LocalPlayer.Character.Heading;
            GameFiber.StartNew(delegate
            {
                while (HandlingSpeech)
                {
                    GameFiber.Yield();
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, PlayerPos) > 2.5f)
                    {
                        Game.LocalPlayer.Character.Tasks.FollowNavigationMeshToPosition(PlayerPos, PlayerHeading, 1f).WaitForCompletion(1000);
                    }
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1800);
                    }
                }
            });
            if (TalkingPed != null)
            {
                if (TalkingPed.Exists())
                {
                    if (!TalkingPed.IsInAnyVehicle(false))
                    {


                        TalkingPed.Tasks.PlayAnimation("special_ped@jessie@monologue_1@monologue_1f", "jessie_ig_1_p1_heydudes555_773", 1f, AnimationFlags.Loop);
                    }
                }
            }
            for (int i=0; i<Lines.Count; i++)
            {
                
                Game.DisplaySubtitle("~b~" + PersonTalking + ": ~s~" + Lines[i], 10000);
                while (i < Lines.Count - 1)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        break;
                    }
                }
                if (!HandlingSpeech) { break; }
            }
            HandlingSpeech = false;
            if (TalkingPed != null)
            {
                TalkingPed.Tasks.ClearImmediately();
            }
            Game.HideHelp();
        }
        public static void HandleSpeech(string PersonTalking, string line, Ped TalkingPed = null)
        {
            HandleSpeech(PersonTalking, new List<string>() { line }, TalkingPed: TalkingPed);
        }
        
        private static WMPLib.WindowsMediaPlayer wmp1 = new WMPLib.WindowsMediaPlayer();
        //private static WMPLib.WindowsMediaPlayer wmp2 = new WMPLib.WindowsMediaPlayer();
        public static void PlayPhoneCallingSound(int TimesToPlay)
        {
            for (int i = 0; i < TimesToPlay; i++)
            {
                wmp1.settings.volume = 100;
                wmp1.URL = "LSPDFR/audio/scanner/Assorted Callouts Audio/PHONE_CALLING.wav";
                GameFiber.Wait(3000);
            }
        }
        public static void PlayPhoneBusySound(int TimesToPlay)
        {
            for (int i = 0; i < TimesToPlay; i++)
            {
                wmp1.settings.volume = 100;
                wmp1.URL = "LSPDFR/audio/scanner/Assorted Callouts Audio/PHONE_BUSY.wav";

                GameFiber.Wait(800);
            }
        }
         
        public static int CptWellsLineAudioCount = 1;
        public static int YouLineAudioCount = 1;
        public static int RobberAudioCount = 1;
        public static bool DisplayingBankHeistSpeech;
        public static void HandleBankHeistSpeech(List<string> Lines, string LineFolderModifier = "",  Ped CaptainWells = null, bool WaitAfterLastLine = true)
        {


            Vector3 PlayerPos = Game.LocalPlayer.Character.Position;
            float PlayerHeading = Game.LocalPlayer.Character.Heading;
            DisplayingBankHeistSpeech = true;
            GameFiber.StartNew(delegate
            {
                while (DisplayingBankHeistSpeech)
                {
                    GameFiber.Yield();
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, PlayerPos) > 2.5f)
                    {
                        Game.LocalPlayer.Character.Tasks.FollowNavigationMeshToPosition(PlayerPos, PlayerHeading, 1f).WaitForCompletion(1000);
                    }
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1800);
                    }
                }
            });
            for (int i = 0; i < Lines.Count; i++)
            {

                try {
                    int J = Lines[i].IndexOf(":");
                    string PersonTalking = Lines[i].Substring(0, J);
                    if (AssortedCalloutsHandler.BankHeistVoiceOvers)
                    {
                        if (PersonTalking == "Cpt. Wells")
                        {
                            try
                            {
                                wmp1.controls.stop();
                                if (File.Exists("LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + CptWellsLineAudioCount.ToString() + ".wav"))
                                {
                                    wmp1.settings.volume = 100;
                                    wmp1.URL = "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + CptWellsLineAudioCount.ToString() + ".wav";
                                }
                                else
                                {
                                    Game.LogTrivial("Audio File not found at " + "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + CptWellsLineAudioCount.ToString() + ".wav");
                                }
                            }
                            catch (Exception e) { Game.LogTrivial("Audio File not found at " + "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + CptWellsLineAudioCount.ToString() + ".wav"); }
                            CptWellsLineAudioCount++;
                        }
                        else if (PersonTalking == "You")
                        {
                            try
                            {
                                wmp1.controls.stop();
                                if (File.Exists("LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + YouLineAudioCount.ToString() + ".wav"))
                                {
                                    wmp1.settings.volume = 100;
                                    wmp1.URL = "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + YouLineAudioCount.ToString() + ".wav";
                                }
                                else
                                {
                                    Game.LogTrivial("Audio File not found at " + "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + YouLineAudioCount.ToString() + ".wav");
                                }
                            }
                            catch (Exception e) { Game.LogTrivial("Audio File not found at " + "LSPDFR/audio/scanner/Assorted Callouts Audio/Lines/" + LineFolderModifier + "/" + PersonTalking + "/Line" + YouLineAudioCount.ToString() + ".wav"); }
                            YouLineAudioCount++;

                        }
                    }
                    Game.DisplaySubtitle("~b~" + PersonTalking + ": ~s~" + Lines[i].Substring(J + 2), 10000);
                    while (i < Lines.Count)
                    {
                        GameFiber.Yield();

                        Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                        {
                            break;
                        }
                        //if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D1))
                        //{
                        //    wmp1.controls.play();
                        //}
                        //if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D2))
                        //{
                        //    wmp2.controls.play();
                        //}
                        if (!WaitAfterLastLine)
                        {
                            if (i == Lines.Count - 1)
                            {
                                break;
                            }
                        }
                        if (!DisplayingBankHeistSpeech) { break; }
                    }
                }
                catch (Exception e) { Game.LogTrivial(e.ToString()); continue; }
            }
            DisplayingBankHeistSpeech = false;
            
            Game.HideHelp();
        }








        public static bool DisplayTime = false;
        private static List<string> Answers;
        //public enum AnswersResults { Positive, Negative, Neutral, Null};
        public static int DisplayAnswers(List<string> PossibleAnswers, bool Shuffle=true)
        {
            Game.RawFrameRender += DrawAnswerWindow;
            DisplayTime = true;
            if (Shuffle)
            {
                Answers = new List<string>(PossibleAnswers.Shuffle());
            }
            else
            {
                Answers = new List<string>(PossibleAnswers);
            }
            
            string AnswerGiven = "";
            //Game.LocalPlayer.Character.IsPositionFrozen = true;
            
            GameFiber.StartNew(delegate
            {
                while (DisplayTime)
                {
                    GameFiber.Yield();
                    
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D1))
                    {
                        if (Answers.Count >= 1)
                        {
                            AnswerGiven = Answers[0];
                            
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D2))
                    {
                        if (Answers.Count >= 2)
                        {
                            AnswerGiven = Answers[1];
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D3))
                    {
                        if (Answers.Count >= 3)
                        {
                            AnswerGiven = Answers[2];
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D4))
                    {
                        if (Answers.Count >= 4)
                        {
                            AnswerGiven = Answers[3];
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D5))
                    {
                        if (Answers.Count >= 5)
                        {
                            AnswerGiven = Answers[4];
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.D6))
                    {
                        if (Answers.Count >= 6)
                        {
                            AnswerGiven = Answers[5];
                        }
                    }
                }
            });
            NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(Game.LocalPlayer.Character, false);
            Vector3 PlayerPos = Game.LocalPlayer.Character.Position;
            float PlayerHeading = Game.LocalPlayer.Character.Heading;
            while (AnswerGiven == "")
            {
                GameFiber.Yield();
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, PlayerPos) > 4f)
                {
                    Game.LocalPlayer.Character.Tasks.FollowNavigationMeshToPosition(PlayerPos, PlayerHeading, 1.2f).WaitForCompletion(1500);
                }
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    Game.LocalPlayer.Character.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1800);
                }
                if (!DisplayTime) { break; }
            }
            NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(Game.LocalPlayer.Character, true);
            DisplayTime = false;
            //Game.LocalPlayer.Character.IsPositionFrozen = false;
            
            return PossibleAnswers.IndexOf(AnswerGiven);


        }

        private static void DrawAnswerWindow(System.Object sender, Rage.GraphicsEventArgs e)
        {
            if (DisplayTime)
            {
                Rectangle drawRect = new Rectangle(Game.Resolution.Width / 5, Game.Resolution.Height / 7, 700, 180);
                Rectangle drawBorder = new Rectangle(Game.Resolution.Width / 5 - 5, Game.Resolution.Height / 7 - 5, 700, 180);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawRectangle(drawBorder, Color.FromArgb(90, Color.Black));
                e.Graphics.DrawRectangle(drawRect, Color.Black);

                e.Graphics.DrawText("Select with Number Keys", "Aharoni Bold", 18.0f, new PointF(drawBorder.X + 150, drawBorder.Y + 2), Color.White, drawBorder);
                
                int YIncreaser = 30;
                for (int i = 0; i < Answers.Count ; i++)
                {

                    e.Graphics.DrawText("[" + (i + 1).ToString() + "] " + Answers[i], "Arial Bold", 15.0f, new PointF(drawRect.X + 10, drawRect.Y + YIncreaser), Color.White, drawRect);
                    YIncreaser += 25;
                }


            }
            else
            {
                Game.FrameRender -= DrawAnswerWindow;
            }


        }
    }
}
