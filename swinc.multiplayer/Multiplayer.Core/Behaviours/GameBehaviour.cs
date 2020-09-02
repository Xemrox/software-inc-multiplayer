﻿using Multiplayer.Debugging;
using Multiplayer.Extensions;
using Multiplayer.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Multiplayer.Core.Utils;

namespace Multiplayer.Core
{
    public class GameBehaviour : ModBehaviour
    {
        public Button MPButton { get; set; }
        public GUIWindow MPWindow { get; set; }
        public override void OnActivate()
        {
            SceneManager.sceneLoaded += OnScene;
            if (SceneManager.GetActiveScene().name == "MainScene")
            {
                CreateButton();
            }
        }

        private void OnScene(Scene arg0, LoadSceneMode arg1)
        {
            if (!isActiveAndEnabled) return;
            if (arg0.name == "MainScene")
            {
                //HUD.Instance.AddPopupMessage("Thanks for using the Multiplayer mod.\nJoin the discord: discord.io/multiplayer-mod", "Smiley", PopupManager.NotificationSound.Good, 1);
                CreateButton();
            }
        }

        private void CreateButton()
        {
            GameObject FanPanel = WindowManager.FindElementPath("MainPanel/Holder/FanPanel").gameObject;
            MPButton = WindowManager.SpawnButton();
            MPButton.onClick.AddListener(CreateBaseMultiplayerWindow);
            MPButton.SetText("MultiplayerButton".LocDef("Multiplayer"));
            WindowManager.AddElementToElement(MPButton.gameObject, FanPanel, new Rect(274, 0, 100, 32), Rect.zero);
            Logging.Info("Initalized multiplayer button in MainScene");
        }

        private void CreateBaseMultiplayerWindow()
        {
            Logging.Info("Opened multiplayer window.");
            MPWindow = WindowManager.SpawnWindow();
            MPWindow.SetTitle("MultiplayerButton".LocDef("Multiplayer"));
            MPWindow.ShowCentered = true;
            MPWindow.MinSize = new Vector2(640, 480);
            MPWindow.SizeButton.SetActive(false);
            Button connectButton = WindowManager.SpawnButton();
            connectButton.SetText("ConnectButtonText".LocDef("Connect"));
            connectButton.onClick.AddListener(() =>
            {
                #region Connect Window
                MPWindow.gameObject.SetActive(false);

                /**
                 * Box 200, 200, 750, 200 "Multiplayer - Connect"
                 * Button 230, 275, 159, 25 "Go Back"
                 * Label 230, 230, 700, 45 "Connect to a multiplayer server via IP and Port, specify a password if the server has a password set."
                 * Input 230, 310, 159, 25 "IP Address"
                 * Input 399, 310, 159, 25 "Port"
                 * Input 230, 345, 159, 25 "Password"
                 * Button 588, 310, 159, 25 "Connect"
                **/
                
                GUIWindow connectWindow = WindowManager.SpawnWindow();
                connectWindow.SetTitle("MultiplayerButtonConnect".LocDef("Multiplayer - Connect"));
                connectWindow.ShowCentered = true;
                connectWindow.MinSize = new Vector2(750, 200);
                connectWindow.SizeButton.SetActive(false);

                Utils.Controls.Element.UIButton goBackButton = new Utils.Controls.Element.UIButton("GoBackButton".LocDef("Go Back"), new Rect(30, 75, 159, 25), () =>
                {
                    connectWindow.gameObject.SetActive(false);
                    MPWindow.gameObject.SetActive(true);
                }, connectWindow.MainPanel, "GoBackButton", "GoBackTooltip".LocDef("Go back to the main multiplayer window."));

                new Utils.Controls.Element.UILabel(
                    "ConnectLabel".LocDef("Connect to a multiplayer server via IP and Port, specify a password if the server has a password set."),
                    new Rect(30, 30, 700, 45),
                    connectWindow.MainPanel);

                Utils.Controls.Element.UITextbox IpTextBox = new Utils.Controls.Element.UITextbox(new Rect(30, 110, 159, 25), connectWindow.MainPanel, "IP", "", null, 12);
                Utils.Controls.Element.UITextbox PortTextBox = new Utils.Controls.Element.UITextbox(new Rect(35 + 159, 110, 159, 25), connectWindow.MainPanel, "PortInput".LocDef("Port"), "", null, 12);
                Utils.Controls.Element.UITextbox PasswordTextBox = new Utils.Controls.Element.UITextbox(new Rect(30, 145, 159, 25), connectWindow.MainPanel, "PasswordInput".LocDef("Password"), "", null, 12, true);
                new Utils.Controls.Element.UIButton("ConnectButtonText".LocDef("Connect"), new Rect(488, 110, 159, 25), () =>
                {
                    if (IpTextBox.obj.text.IsEmpty())
                    {
                        WindowManager.SpawnDialog("NoIPText".LocDef("Please enter a IP into the text box labeled \"Server IP\""), true, DialogWindow.DialogType.Error);
                        return;
                    }
                    else if (PortTextBox.obj.text.IsEmpty())
                    {
                        WindowManager.SpawnDialog("NoPortText".LocDef("Please enter a Port into the text box labeled \"Server Port\""), true, DialogWindow.DialogType.Error);
                    }
                    else
                    {
                        try
                        {
                            Client.Connect(IpTextBox.obj.text, ushort.Parse(PortTextBox.obj.text));
                            WindowManager.SpawnDialog("NoIPText".LocDef("Please enter a IP into the text box labeled \"Server IP\""), true, DialogWindow.DialogType.Error);
                        }
                        catch (Exception e)
                        {
                            WindowManager.SpawnDialog($"There was an error trying to connect to {IpTextBox.obj.text}:{PortTextBox.obj.text}, see console for error.", true, DialogWindow.DialogType.Error);
                            Logging.Error(e);
                            return;
                        }
                    }
                }, connectWindow.MainPanel);
                #region Window Show management.
                if (Client.Connected)
                {
                    // If user is already connected to a server.
                    GameObject diagObj = UnityEngine.Object.Instantiate(WindowManager.Instance.DialogPrefab);
                    diagObj.transform.SetParent(WindowManager.Instance.Canvas.transform, worldPositionStays: false);
                    DialogWindow diag = gameObject.GetComponent<DialogWindow>();
                    KeyValuePair<string, Action>[] actions = new KeyValuePair<string, Action>[]
                    {
                        new KeyValuePair<string, Action>("DisconnnectButton".LocDef("Disconnect"), delegate {
                            Client.Disconnect();
                            diag.Window.Close();
                            connectWindow.Show();
                        }),
                        new KeyValuePair<string, Action>("CancelButton".LocDef("Cancel"), delegate {
                            diag.Window.Close();
                        }),
                    };
                    diag.Show("AlreadyConnectedToServer".LocDef("You are already connected to a server, would you like to disconnect?"), !true, DialogWindow.DialogType.Warning, actions);
                } else
                {
                    connectWindow.Show();
                }
                #endregion
                #endregion
            });

            Button startServerButton = WindowManager.SpawnButton();
            startServerButton.SetText("StartServerButtonText".LocDef("Create Server"));

            //Chat window here, will be added to MPWindow and takes up the rest of the space minus the bottom (reserved for chat input)

            //TODO: (i am working on this -CyaCal) Input box.
            // I haven't pushed it because its broken atm, fixing it though.

            MPWindow.Show();

            MPWindow.AddElement(connectButton.gameObject, new Rect(5, 5, 96, 64), Rect.zero);
            MPWindow.AddElement(startServerButton.gameObject, new Rect(96 + 10, 5, 96, 64), Rect.zero);
        }

        public override void OnDeactivate()
        {
            SceneManager.sceneLoaded -= OnScene;
            if (MPButton != null)
                MPButton.gameObject.SetActive(false);
            Logging.Info("Destroyed multiplayer button in MainScene");
        }
    }
}