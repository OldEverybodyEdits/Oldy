using PlayerIO.GameLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//OldyChecked fixes some of the mistakes that the original Old EE gamecode didn't fix.

namespace OldyChecked
{
	public class Player : BasePlayer
	{
		public bool Initiated = false;
		public bool Bot = false;
		public double X = 0;
		public double Y = 0;
		public int Face = 0;
	}

	/// <summary>
	/// We need to configure our servers
	/// </summary>
	public class Config
	{
		public int MaxSmilies = 5;
		public int MinSmilies = 0;

		public int WorldWidth = 100;
		public int WorldHeight = 100;

		public int MaxBlocks = 20;
		public int MinBlocks = 0;
	}

	[RoomType("FlixelWalker1")]
	public class FlixelWalker1 : Game<Player>
	{
		public Config cnf;
		public int[,] World;
		public bool Loaded = false;

		/// <summary>
		/// We only want to send messages to the players that have initialized with "init"
		/// </summary>
		/// <param name="e"></param>
		private void BroadcastJoined(Message e)
		{
			ForEachPlayer(delegate(Player i)
			{
				if (i.Initiated)
				{
					i.Send(e);
				}
			});
		}

		private void BroadcastJoined(string bas, params object[] pms)
		{
			BroadcastJoined(Message.Create(bas, pms));
		}

		/// <summary>
		/// We only want to send messages to the players who are bots
		/// </summary>
		/// <param name="e"></param>
		private void BroadcastBot(Message e)
		{
			ForEachPlayer(delegate(Player i)
			{
				if (i.Bot)
				{
					i.Send(e);
				}
			});
		}

		private void BroadcastBot(string bas, params object[] pms)
		{
			BroadcastBot(Message.Create(bas, pms));
		}

		private string Serialize()
		{
			StringBuilder Serialize = new StringBuilder("");

			//Serialize the world data
			for (int y = 0; y < cnf.WorldHeight; y++)
			{
				Serialize.Append(World[0, y].ToString());
				for (int x = 1; x < cnf.WorldWidth; x++)
				{
					Serialize.Append(",");
					Serialize.Append(World[x, y].ToString());
				}
				if (y != cnf.WorldHeight - 1)
				{
					Serialize.Append("\n");
				}
			}
			return Serialize.ToString();
		}

		private string Serialize(int[,] WorldData)
		{
			StringBuilder Serialize = new StringBuilder("");

			//Serialize the world data
			for (int y = 0; y < cnf.WorldHeight; y++)
			{
				Serialize.Append(WorldData[0, y].ToString());
				for (int x = 1; x < cnf.WorldWidth; x++)
				{
					Serialize.Append(",");
					Serialize.Append(WorldData[x, y].ToString());
				}
				if (y != cnf.WorldHeight - 1)
				{
					Serialize.Append("\n");
				}
			}
			return Serialize.ToString();
		}

		/// <summary>
		/// Add saving and loading of worlds
		/// </summary>
		public override void GameStarted()
		{
			cnf = new Config();
			//We don't check if it's 0x0 or 8x5

			World = new int[cnf.WorldWidth, cnf.WorldHeight];

			for (int x = 0; x < cnf.WorldWidth; x++)
			{
				for (int y = 0; y < cnf.WorldHeight; y++)
				{
					if ((x == 0 || x == cnf.WorldWidth - 1)
						|| (y == 0 || y == cnf.WorldHeight - 1))
					{
						World[x, y] = 5;
					}
					else
					{
						World[x, y] = 0;
					}
				}
			}

			PlayerIO.BigDB.LoadOrCreate("Worlds", RoomId, delegate(DatabaseObject i)
			{
				if (i.Contains("width") && i.Contains("height") && i.Contains("data"))
				{
					World = new int[i.GetInt("width"), i.GetInt("height")];
					cnf.WorldWidth = i.GetInt("width");
					cnf.WorldHeight = i.GetInt("height");

					int X = 0, Y = 0;
					string[] RoomData = i.GetString("data").Replace("\r", "").Split('X');
					foreach (string y in RoomData)
					{
						foreach (string x in y.Split(','))
						{
							World[X, Y] = Convert.ToInt32(x);
							X++;
						}
						X = 0;
						Y++;
					}
				}
			}, delegate(PlayerIOError i)
			{

			});
			Loaded = true;
			base.GameStarted();
		}

		public override void GameClosed()
		{
			//Serialize the world than save it to the BigDb
			string Save = Serialize().Replace('\n', 'X');
			PlayerIO.BigDB.LoadOrCreate("Worlds", RoomId, delegate(DatabaseObject i)
			{
				i.Set("data", Save);
				i.Set("width", cnf.WorldWidth);
				i.Set("height", cnf.WorldHeight);
				i.Save();
			});
			base.GameClosed();
		}

		public override void UserJoined(Player player)
		{
			while(!Loaded)
			{

			}
			PlayerIO.BigDB.LoadOrCreate("SaveData", player.IPAddress.ToString(), delegate(DatabaseObject i)
			{
				if(!i.Contains("face"))
				{
					int Zero = 0;
					i.Set("face", Zero);
					i.Save();
				}
				else
				{
					player.Face = i.GetInt("face");
				}
			});
			base.UserJoined(player);
		}

		public override void UserLeft(Player player)
		{
			Broadcast("left", player.Id);
			PlayerIO.BigDB.LoadOrCreate("SaveData", player.IPAddress.ToString(), delegate(DatabaseObject i)
			{
				i.Set("face", player.Face);
				i.Save();
			});
			base.UserLeft(player);
		}

		public override void GotMessage(Player player, Message message)
		{
			switch (message.Type)
			{
				case "bot":
					if(player.Initiated)
					{
						player.Bot = true;
					}
					break;
				case "init":
					if (!player.Initiated)
					{
						while(!Loaded)
						{

						}

						//Different code for the first player joining
						if (player.Id == 0)
						{
							PlayerIO.BigDB.LoadOrCreate("Worlds", RoomId, delegate(DatabaseObject i)
							{
								if (i.GetString("data") != Serialize())
								{
									if (i.Contains("width") && i.Contains("height") && i.Contains("data"))
									{
										World = new int[i.GetInt("width"), i.GetInt("height")];
										cnf.WorldWidth = i.GetInt("width");
										cnf.WorldHeight = i.GetInt("height");

										int X = 0, Y = 0;
										string[] RoomData = i.GetString("data").Replace("\r", "").Split('X');
										foreach (string y in RoomData)
										{
											foreach (string x in y.Split(','))
											{
												World[X, Y] = Convert.ToInt32(x);
												X++;
											}
											X = 0;
											Y++;
										}
										player.Send("init", Serialize(World), player.Id);
									}
									else
									{
										player.Send("init", Serialize(World), player.Id);
										player.Send("change", 0, 0, 4);
									}
								}
							}, delegate(PlayerIOError i)
							{
								player.Send("init", Serialize(), player.Id);
							});
						}
						else
						{
							BroadcastJoined("add", player.Id, player.Face, 16, 16);
							player.Send("init", Serialize(), player.Id);
							ForEachPlayer(delegate(Player i)
							{
								if (i.Initiated)
								{
									player.Send("add", i.Id, i.Face, Convert.ToInt32(i.X), Convert.ToInt32(i.Y));
								}
							});
						}

						player.Initiated = true;
					}
					break;
				case "face":
					if (player.Initiated)
					{
						if (message.Count == 1)
						{
							int id = 0;
							if (Int32.TryParse(message[0].ToString(), out id))
							{
								if (id >= cnf.MinSmilies && id <= cnf.MaxSmilies)
								{
									player.Face = id;
									BroadcastJoined("face", player.Id, id);
								}
							}
						}
					}
					break;
				case "update":
					if (player.Initiated)
					{
						if (message.Count == 8)
						{
							double[] Args = new double[8];
							bool Successful = true;
							for (uint i = 0; i <= 7; i++)
							{
								if (!Double.TryParse(message[i].ToString(), out Args[i]))
								{
									Successful = false;
								}
							}

							if (Successful)
							{
								player.X = Args[0];
								player.Y = Args[1];
								BroadcastJoined("update", player.Id, Args[0], Args[1], Args[2], Args[3], Args[4], Args[5], Args[6], Args[7]);
							}
						}
					}
					break;
				case "change":
					if (player.Initiated)
					{
						if (message.Count == 3)
						{
							int x = 0;
							int y = 0;
							int id = 0;
							if (Int32.TryParse(message[0].ToString(), out x))
							{
								if (Int32.TryParse(message[1].ToString(), out y))
								{
									if (Int32.TryParse(message[2].ToString(), out id))
									{
										if (World[x, y] != id)
										{
											if (id >= cnf.MinBlocks && id <= cnf.MaxBlocks)
											{
												if (x >= 0 && x < cnf.WorldWidth)
												{
													if (y >= 0 && y < cnf.WorldHeight)
													{
														if (id <= 4)
														{
															if (x >= 1 && y >= 1 && x <= cnf.WorldHeight - 2 && y <= cnf.WorldHeight - 2)
															{
																BroadcastJoined("change", x, y, id);
																World[x, y] = id;
															}
														}
														else
														{
															BroadcastJoined("change", x, y, id);
															World[x, y] = id;
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
					break;
				case "c":
					if(message.Count == 1)
					{
						if (player.Bot)
						{
							BroadcastBot("c", message[0].ToString());
						}
					}
					break;
			}
			base.GotMessage(player, message);
		}
	}
}
