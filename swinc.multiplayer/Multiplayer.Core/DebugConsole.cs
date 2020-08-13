﻿using Multiplayer.Debugging;
using Multiplayer.Networking;
using Multiplayer.Networking.GameWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Multiplayer.Core
{
	class DebugConsole : ModBehaviour
	{
		bool inmain = false;

		public override void OnActivate()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
			Logging.Info("[DebugConsole] Adding console commands");
			DevConsole.Command<ushort> startservercmd = new DevConsole.Command<ushort>("MULTIPLAYER_START", OnStartServer);
			DevConsole.Console.AddCommand(startservercmd);
			DevConsole.Command<string, ushort> connectclientcmd = new DevConsole.Command<string, ushort>("MULTIPLAYER_CONNECT", OnClientConnect);
			DevConsole.Console.AddCommand(connectclientcmd);
			DevConsole.Command<string> sendchatcmd = new DevConsole.Command<string>("MULTIPLAYER_CHAT", OnSendChat);
			DevConsole.Console.AddCommand(sendchatcmd);
			DevConsole.Command closeserver = new DevConsole.Command("MULTIPLAYER_STOP", OnServerStop);
			DevConsole.Console.AddCommand(closeserver);
		}

		private void OnServerStop()
		{
			Networking.Client.Disconnect();
			Networking.Server.Stop();
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if(scene.name == "MainScene")
			{
				inmain = true;
			}
			else
			{
				inmain = false;
			}
		}

		private void OnClientConnect(string ip, ushort port)
		{
			if(!inmain)
			{
				Logging.Warn("[DebugConsole] You can't use this command outside of the MainScene!");
				return;
			}
			Networking.Server.Stop();
			Networking.Client.Connect(ip, port);
		}

		private void OnSendChat(string arg0)
		{
			if (!inmain || !Networking.Client.Connected)
			{
				Logging.Warn("[DebugConsole] You can't use this command outside of the MainScene!");
				return;
			}
			Networking.Client.Send(new Helpers.TcpChat(arg0));
		}

		private void OnStartServer(ushort port)
		{
			if (!inmain)
			{
				Logging.Warn("[DebugConsole] You can't use this command outside of the MainScene!");
				return;
			}
			Networking.Server.Start(port);
			Networking.Client.Connect("127.0.0.1", port);
		}

		public override void OnDeactivate()
		{
			Logging.Info("[DebugConsole] Removing console commands");
			DevConsole.Console.RemoveCommand("MULTIPLAYER_START");
			DevConsole.Console.RemoveCommand("MULTIPLAYER_CONNECT");
			DevConsole.Console.RemoveCommand("MULTIPLAYER_CHAT");
			DevConsole.Console.RemoveCommand("MULTIPLAYER_STOP");
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}
	}
}
