using BattleNET;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

namespace Whitelister
{
	internal class Program
	{
		private BattlEyeLoginCredentials loginCredentials;

		private IBattleNET b;

		private bool running = true;

		private string host = "";

		private int port = 0;

		private int interval = 0;
		
		private int allowedPlayers = 0;
		
		private int numPlayers = 0;

		private string password = "";

		private string reason = "";

		private StreamWriter writer;

		private string playerResult;

		private int runNr = 1;

		private string sqlHost = "";

		private string sqlPort = "";

		private string sqlDatabase = "";

		private string sqlTable = "";

		private string sqlUser = "";

		private string sqlPassword = "";

		private string sqlSource = "";

		private static void Main(string[] args)
		{
			Console.Title = "Player Whitelister";
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Running!");
			Console.Write("------------------------------------------------------------------------\n");
			Program program = new Program();
			bool flag = program.loadConfig();
			if (flag)
			{
				AppDomain.CurrentDomain.ProcessExit += new EventHandler(program.exit);
				program.startWhitelist();
			}
			else
			{
				Console.WriteLine("ERROR: Could not read the config file!");
			}
		}

		private void startWhitelist()
		{
			try
			{
				this.writer = new StreamWriter("console.log", true);
			}
			catch
			{
				this.write("Could not access the log file!", ConsoleColor.DarkRed);
			}
			this.write("Connecting please wait...", ConsoleColor.DarkMagenta);
			this.setLoginData(this.host, this.port, this.password);
			this.connectClient();
			new Thread(new ThreadStart(this.checkPlayers))
			{
				IsBackground = true
			}.Start();
		}

		private bool loadConfig()
		{
			bool result;
			try
			{
				FileInfo fileInfo = new FileInfo("config.txt");
				StreamReader streamReader = fileInfo.OpenText();
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					if (!text.StartsWith("//"))
					{
						if (text.StartsWith("host"))
						{
							this.host = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("port"))
						{
							this.port = int.Parse(text.Split(new char[]
							{
								'='
							})[1]);
						}
						else if (text.StartsWith("password"))
						{
							this.password = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("reason"))
						{
							this.reason = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("interval"))
						{
							this.interval = int.Parse(text.Split(new char[]
							{
								'='
							})[1]);
						}
						else if (text.StartsWith("players"))
						{
							this.allowedPlayers = int.Parse(text.Split(new char[]
							{
								'='
							})[1]);
						}
						else if (text.StartsWith("sqlHost"))
						{
							this.sqlHost = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("sqlPort"))
						{
							this.sqlPort = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("sqlDatabase"))
						{
							this.sqlDatabase = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("sqlTable"))
						{
							this.sqlTable = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("sqlUser"))
						{
							this.sqlUser = text.Split(new char[]
							{
								'='
							})[1];
						}
						else if (text.StartsWith("sqlPassword"))
						{
							this.sqlPassword = text.Split(new char[]
							{
								'='
							})[1];
						}
					}
				}
				streamReader.Close();
				streamReader.Dispose();
				if (this.sqlHost != "" && this.sqlPort != "" && this.sqlDatabase != "" && this.sqlUser != "")
				{
					this.sqlSource = string.Concat(new string[]
					{
						"Data Source=",
						this.sqlHost,
						";Port=",
						this.sqlPort,
						";Database=",
						this.sqlDatabase,
						";User ID=",
						this.sqlUser,
						";Password=",
						this.sqlPassword
					});
				}
				if (this.host != "" && this.port != 0 && this.password != "" && this.reason != "")
				{
					result = true;
				}
				else
				{
					result = false;
				}
			}
			catch
			{
				this.write("Failed to load config!", ConsoleColor.DarkRed);
				result = false;
			}
			return result;
		}
		private void HandleMessage(BattlEyeMessageEventArgs args)
		{
			if (args.Message != null)
			{
				if (args.Message.StartsWith("Player #") && args.Message.EndsWith("(unverified)"))
				{
					string message = args.Message;
					string[] array = Regex.Split(message, " - GUID: ");
					string text = array[0].Replace("Player #", "");
					this.write("Debug1: " + text, ConsoleColor.Yellow);
					string numPlayers2 = text.Split(' ')[0];
					this.write("Debug2: " + numPlayers2, ConsoleColor.Yellow);
					int numPlayers1 = Int32.Parse(numPlayers2);
					numPlayers = numPlayers1 + 1;
					string[] array2 = text.Split(new char[]
					{
						' '
					}, 2, StringSplitOptions.RemoveEmptyEntries);
					string id = array2[0];
					string text2 = array2[1];
					string guid = array[1].Replace(" (unverified)", "");
					this.write(text2 + " tried to connect...", ConsoleColor.DarkCyan);
					if (this.isWhitelisted(guid, text2))
					{
						this.write("    " + text2 + " is whitelisted.", ConsoleColor.DarkCyan);
					}
					else
					{
						this.write("    kicking " + text2, ConsoleColor.Yellow);
						this.kick(id);
					}
				}
				else if (args.Message.StartsWith("Players on server:"))
				{
					this.playerResult = args.Message + "\n";
				}
				else if (args.Message.StartsWith("RCon admin #") && args.Message.EndsWith("logged in"))
				{
					Console.WriteLine(args.Message);
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
				Console.WriteLine("Connection lost.");
				break;
			case EBattlEyeDisconnectionType.SocketException:
				Console.WriteLine("Invalid host");
				break;
			case EBattlEyeDisconnectionType.LoginFailed:
				Console.WriteLine("Login invalid!");
				break;
			case EBattlEyeDisconnectionType.ConnectionFailed:
				this.write("Could not connect to Battleye. (Is Battleye enabled?)", ConsoleColor.DarkRed);
				break;
			default:
				Console.WriteLine("Unknown Battleye error!");
				break;
			}
		}

		private void setLoginData(string host, int port, string password)
		{
			this.write("Starting Login Data...", ConsoleColor.DarkMagenta);
			this.loginCredentials = new BattlEyeLoginCredentials
			{
				Host = host,
				Port = port,
				Password = password
			};
		}

		private void connectClient()
		{
			this.write("Starting Connection to Client...", ConsoleColor.DarkMagenta);
			do
			{
				this.b = new BattlEyeClient(this.loginCredentials);
				this.b.MessageReceivedEvent += new BattlEyeMessageEventHandler(this.HandleMessage);
				this.b.DisconnectEvent += new BattlEyeDisconnectEventHandler(this.HandleDisconnect);
				this.b.ReconnectOnPacketLoss(true);
				this.b.Connect();
				Thread.Sleep(3000);
			}
			while (!this.b.IsConnected());
		}

		private void disconnectClient()
		{
			this.write("Starting Disconnect to Client...", ConsoleColor.DarkGreen);
			this.b.SendCommandPacket(EBattlEyeCommand.Logout);
			this.b.Disconnect();
		}

		private void kick(string id)
		{
			this.b.SendCommandPacket(EBattlEyeCommand.Kick, id + " " + this.reason);
		}

		public void say(string message)
		{
			this.b.SendCommandPacket(EBattlEyeCommand.Say, "-1 " + message);
		}

		private void checkPlayers()
		{
			this.write("Starting Player Check...", ConsoleColor.DarkMagenta);
			Thread.Sleep(this.interval);
			while (this.running)
			{
				this.runNr++;
				List<Player> players = this.getPlayers();
				foreach (Player current in players)
				{
					if (current.guid.Length == 32)
					{
						if (!this.isWhitelisted(current.guid, current.name))
						{
							this.kick(current.number);
							this.write("    Kicking " + current.name, ConsoleColor.Yellow);
						}
					}
				}
				Thread.Sleep(this.interval);
			}
		}
		
		private List<Player> getPlayers()
		{
			List<Player> list = new List<Player>();
			bool flag = false;
			int num = 0;
			bool flag2;
			do
			{
				list.Clear();
				num++;
				flag2 = true;
				while (!flag)
				{
					this.b.SendCommandPacket(EBattlEyeCommand.Players);
					Thread.Sleep(1000);
					flag = (this.playerResult != null);
				}
				StringReader stringReader = new StringReader(this.playerResult);
				int num2 = 0;
				string text;
				while ((text = stringReader.ReadLine()) != null)
				{
					num2++;
					if (num2 > 3 && !text.StartsWith("(") && text.Length > 0)
					{
						string[] array = text.Split(new char[]
						{
							' '
						}, 5, StringSplitOptions.RemoveEmptyEntries);
						if (array.Length == 5)
						{
							string number = array[0];
							string ip = array[1].Split(new char[]
							{
								':'
							})[0];
							string ping = array[2];
							string guid = array[3].Replace("(OK)", "").Replace("(?)", "");
							string name = array[4].Replace(" (Lobby)", "");
							list.Add(new Player(number, ip, ping, guid, name));
						}
						else
						{
							flag2 = false;
						}
					}
				}
				stringReader.Dispose();
				stringReader.Close();
				flag = false;
				this.playerResult = null;
			}
			while (!flag2);
			List<Player> result;
			if (flag2)
			{
				result = list;
			}
			else
			{
				list.Clear();
				Console.WriteLine("Player request timed out!");
				result = list;
			}
			return result;
		}
		
		private void write(string message, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			if (this.writer != null)
			{
				this.writer.WriteLine(message);
				this.writer.Flush();
			}
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		private bool isWhitelisted(string guid, string name)
		{   
			bool result = true;
			try
			{
				MySqlConnection mySqlConnection = new MySqlConnection(this.sqlSource);
				mySqlConnection.Open();
				string cmdText = "SELECT * FROM " + this.sqlTable + " WHERE guid = @guid AND name = @name";
				MySqlDataReader mySqlDataReader = new MySqlCommand(cmdText, mySqlConnection)
				{
					Parameters = 
					{
						new MySqlParameter("@guid", guid),
						new MySqlParameter("@name", name)
					}
				}.ExecuteReader();
				if (mySqlDataReader.Read())
				{
					result = true;
				}
				else if (numPlayers > this.allowedPlayers)
				{
					this.write("    " + name + " disconnected, not a donator. GUID: " + guid, ConsoleColor.Yellow);
					this.write("Player Slots Allowed: " + this.allowedPlayers, ConsoleColor.Yellow);
					this.write("Player Number: " + numPlayers, ConsoleColor.Yellow);
					result = false;
				}
				else
				{
					this.write("    " + name + " connected, but is not a donator. GUID: " + guid, ConsoleColor.Yellow);
					this.write("Player Slots Allowed: " + this.allowedPlayers, ConsoleColor.Yellow);
					this.write("Player Number: " + numPlayers, ConsoleColor.Yellow);
					result = true;					
				}
				mySqlConnection.Close();
			}
			catch
			{
				this.write("Could not get to the whitelist!\nPlease check all MYSQL settings\nAnd check the config.txt\nAnd make shure the user has rights to the Database.\nGood luck xD", ConsoleColor.Red);
			}
			return result;
		}

		private void exit(object sender, EventArgs e)
		{
			Console.WriteLine("Disconnecting...");
			this.running = false;
			this.disconnectClient();
			if (this.writer != null)
			{
				this.writer.Close();
				this.writer.Dispose();
			}
		}
	}
}
