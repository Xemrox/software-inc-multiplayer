﻿using Multiplayer.Debugging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Multiplayer.Networking
{
    public static partial class Client
    {
        public static bool Connected { get { return client.Connected; } }
        public static Telepathy.Client client = new Telepathy.Client();
        public static string Username = "Player";
        public static string ServerPassword = "";
        public static async void Connect(string ip, ushort port)
        {
            if (client.Connecting)
            {
                Logging.Warn("[Client] You're already connecting to a server!");
                return;
            }
            // create and connect the client
            ChatMessages = new List<string>();
            ChatLogMessages = new List<string>();
            try
            {
                Username = Steamworks.SteamFriends.GetPersonaName();
            }
            catch (Exception ex)
            {
                Logging.Warn("[Client] Couldn't fetch username from Steam! If you've a DRM-Free version thats why. => " + ex.Message);
            }
            client.MaxMessageSize = int.MaxValue;
            client.Connect(ip, port);
            Logging.Info("[Client] Trying to connect!");
            await Task.Run(() =>
            {
                while (client.Connecting)
                {

                }
                if (client.Connected)
                {
                    Logging.Info("[Client] Connected to the Server!");
                    OnServerChatRecieved(new Helpers.TcpServerChat($"Connected to the server.", Helpers.TcpServerChatType.Info));
                    Read();
                    GameWorld.Client client = new GameWorld.Client();
                }
            });
            if (!client.Connected)
            {
                //WindowManager.SpawnDialog("Couldn't connect to the Server!", true, DialogWindow.DialogType.Warning);
                //Logging.Warn("[Client] Couldn't connect to the Server!");
                throw new Exception("[Client] Couldn't connect to the Server");
            }
        }

        private static async void Read()
        {
            Logging.Info("[Client] Starts reading");
            await Task.Run(() =>
            {
                while (Connected)
                {
                    Telepathy.Message msg;
                    while (client.GetNextMessage(out msg))
                    {
                        switch (msg.eventType)
                        {
                            case Telepathy.EventType.Connected:
                                Logging.Info("[Client] Connected");

                                break;
                            case Telepathy.EventType.Data:
                                Receive(msg.data);
                                break;
                            case Telepathy.EventType.Disconnected:
                                Logging.Info("[Client] Disconnected");
                                break;
                        }
                    }
                }
            });
            Logging.Info("[Client] Ends reading");
        }

        private static void Receive(byte[] data)
        {
            Logging.Info("[Client] Data from Server: " + data.Length + " bytes");

            //Handle TcpResponse
            Helpers.TcpResponse tcpresponse = Helpers.TcpResponse.Deserialize(data);
            if (tcpresponse != null && tcpresponse.Header == "response")
                OnServerResponse(tcpresponse);

            //Handle TcpServerChat
            Helpers.TcpServerChat tcpServerChat = Helpers.TcpServerChat.Deserialize(data);
            if (tcpServerChat != null && tcpServerChat.Header == "serverchat")
                OnServerChatRecieved(tcpServerChat);

            Helpers.TcpPrivateChat tcpPrivateChat = Helpers.TcpPrivateChat.Deserialize(data);
            if (tcpPrivateChat != null && tcpPrivateChat.Header == "pm")
                OnPrivateChatRecieved(tcpPrivateChat);

            //Handle TcpChat
            Helpers.TcpChat tcpchat = Helpers.TcpChat.Deserialize(data);
            if (tcpchat != null && tcpchat.Header == "chat")
                OnChatReceived(tcpchat);

            //Handle GameWorld
            Helpers.TcpGameWorld tcpworld = Helpers.TcpGameWorld.Deserialize(data);
            if (tcpworld != null && tcpworld.Header == "gameworld")
                OnGameWorldReceived(tcpworld);

            //Handle Gamespeed
            Helpers.TcpGamespeed tcpspeed = Helpers.TcpGamespeed.Deserialize(data);
            if (tcpspeed != null && tcpspeed.Header == "gamespeed")
                OnGamespeedChange(tcpspeed);
        }
        private static void OnGamespeedChange(Helpers.TcpGamespeed tcpspeed)
        {
            Logging.Info("gamespeedchange...");
            int type = (int)tcpspeed.Data.GetValue("type");
            int speed = (int)tcpspeed.Data.GetValue("speed");
            if (type == 0)
            {
                GameSettings.GameSpeed = speed;
                //HUD.Instance.GameSpeed = (int)speed;
            }
            OnServerChatRecieved(new Helpers.TcpServerChat($"The gamespeed has been changed to {speed}", Helpers.TcpServerChatType.Info));
        }

        private static void OnServerResponse(Helpers.TcpResponse response)
        {
            object type = response.Data.GetValue("type");
            if (type == null)
            {
                Logging.Warn("[Client] Type is null!");
                return;
            }
            if ((string)type == "login_request")
            {
                Send(new Helpers.TcpLogin(Username, ServerPassword));
            }
            else if ((string)type == "login_response")
            {
                string res = (string)response.Data.GetValue("data");
                if (res == "ok")
                {
                    //Login ok
                    Logging.Info("[Client] You're logged in now!");
                    //Send request to get GameWorld
                    Send(new Helpers.TcpRequest("gameworld"));

                }
                else if (res == "max_players")
                {
                    //Server full
                    Logging.Warn("[Client] The server is full");
                }
                else if (res == "wrong_password")
                {
                    //Wrong password
                    Logging.Warn("[Client] You did enter the wrong password");
                }
            }
        }

        #region Messages
        public static void Send(Helpers.TcpLogin login)
        {
            Logging.Info("[Client] Sending login message");
            client.Send(login.Serialize());
        }       
        public static void Send(Helpers.TcpRequest request)
        {
            Logging.Info("[Client] Sending request");
            client.Send(request.Serialize());
        }

        public static void Send(Helpers.TcpResponse response)
        {
            Logging.Info("[Client] Sending response");
            client.Send(response.Serialize());
        }

        public static void Send(Helpers.TcpGamespeed speed)
        {
            Logging.Info("[Client] Sending gamespeed");
            client.Send(speed.Serialize());
        }
        #endregion

        public static void Disconnect()
        {
            if (!Connected)
            {
                Logging.Warn("[Client] You can't disconnect a client that isn't connected...");
                return;
            }
            if (!ChatMessages.Contains($"<color=orange>The server has been stopped and you have been disconnected from it.</color>"))
            {
                ChatMessages.Add($"<color=orange>The server has been stopped and you have been disconnected from it.</color>");
            }
            client.Disconnect();
            CreateChatLogFile();
            CreatePChatLogFile();
        }
    }
}
