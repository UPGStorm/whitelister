using BattleNET;
using System;

namespace Whitelister
{
	public class RCon
	{
		private BattlEyeLoginCredentials loginCredentials;

		private IBattleNET b;

		public void setLoginData(string host, int port, string password)
		{
			this.loginCredentials = new BattlEyeLoginCredentials
			{
				Host = host,
				Port = port,
				Password = password
			};
		}

		public void connectClient()
		{
			this.b = new BattlEyeClient(this.loginCredentials);
			this.b.MessageReceivedEvent += new BattlEyeMessageEventHandler(this.HandleMessage);
			this.b.DisconnectEvent += new BattlEyeDisconnectEventHandler(this.HandleDisconnect);
			this.b.ReconnectOnPacketLoss(true);
			this.b.Connect();
		}

		public void disconnectClient()
		{
			this.b.SendCommandPacket(EBattlEyeCommand.Logout);
			this.b.Disconnect();
		}

		private void HandleMessage(BattlEyeMessageEventArgs args)
		{
			if (args.Message != null)
			{
				if (args.Message.StartsWith("Player #") && args.Message.EndsWith("(unverified)"))
				{
					string message = args.Message;
					string[] array = message.Split(new char[]
					{
						' '
					}, 6, StringSplitOptions.RemoveEmptyEntries);
					string text = array[1].Replace("#", "");
					string text2 = array[2];
					string text3 = array[5];
					Console.WriteLine(string.Concat(new string[]
					{
						"#",
						text,
						" ",
						text2,
						" with GUID ",
						text3,
						" joined the game."
					}));
				}
			}
		}

		private void HandleDisconnect(BattlEyeDisconnectEventArgs args)
		{
			switch (args.DisconnectionType)
			{
			case EBattlEyeDisconnectionType.Manual:
				break;
			case EBattlEyeDisconnectionType.ConnectionLost:
				Console.WriteLine("Connection lost. Trying reconnect...");
				break;
			case EBattlEyeDisconnectionType.SocketException:
				Console.WriteLine("Invalid host");
				break;
			case EBattlEyeDisconnectionType.LoginFailed:
				Console.WriteLine("Login invalid!");
				break;
			default:
				Console.WriteLine("Unknown error");
				break;
			}
		}
	}
}
