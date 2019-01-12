using PlayerIO.GameLibrary;

using System;
using System.Text;

//OldyChecked fixes some of the mistakes that the original Old EE gamecode didn't fix.

namespace OldyChecked
{
	public class Player : BasePlayer
	{
		public bool Initiated = false;
		public double X = 0;
		public double Y = 0;
		public int Face = 0;
	}

	/// <summary>
	/// We need to configure our servers
	/// </summary>
	public class Config
	{
		public const int MaxSmilies = 5;
		public const int MinSmilies = 0;

		public const int WorldWidth = 100;
		public const int WorldHeight = 100;

		public const int MaxBlocks = 20;
		public const int MinBlocks = 0;
	}

	[RoomType("FlixelWalker1")]
	public class FlixelWalker1 : Game<Player>
	{
		public Config cnf;
		public int[,] World;

		/// <summary>
		/// We only want to send messages to the players that have initialized with "init"
		/// </summary>
		/// <param name="e"></param>
		private void BroadcastJoined(Message e)
		{
			ForEachPlayer(delegate (Player i)
			{
				if (i.Initiated)
				{
					i.Send(e);
				}
			});
		}

		private void BroadcastJoined(string bas, params object[] pms) => BroadcastJoined(Message.Create(bas, pms));

		public override void GameStarted()
		{
			cnf = new Config();
			//We don't check if it's 0x0 or 8x5

			World = new int[Config.WorldWidth, Config.WorldHeight];

			for (int x = 0; x < Config.WorldWidth; x++)
			{
				for (int y = 0; y < Config.WorldHeight; y++)
				{
					if ((x == 0 || x == Config.WorldWidth - 1)
						|| (y == 0 || y == Config.WorldHeight - 1))
					{
						World[x, y] = 5;
					}
					else
					{
						World[x, y] = 0;
					}
				}
			}

			base.GameStarted();
		}

		public override void GameClosed() => base.GameClosed();

		public override void UserJoined(Player player) => base.UserJoined(player);

		public override void UserLeft(Player player)
		{
			Broadcast("left", player.Id);
			base.UserLeft(player);
		}

		public override void GotMessage(Player player, Message message)
		{
			switch (message.Type)
			{
				case "init":
					if (!player.Initiated)
					{
						StringBuilder Serialize = new StringBuilder("");

						//Serialize the world data
						for (int y = 0; y < Config.WorldHeight; y++)
						{
							Serialize.Append(World[0, y].ToString());
							for (int x = 1; x < Config.WorldWidth; x++)
							{
								Serialize.Append(",");
								Serialize.Append(World[x, y].ToString());
							}
							if (y != Config.WorldHeight - 1)
							{
								Serialize.Append("\n");
							}
						}
						BroadcastJoined("add", player.Id, 0, 16, 16);
						player.Send("init", Serialize.ToString(), player.Id);
						ForEachPlayer(delegate (Player i)
						{
							if (i.Initiated)
							{
								player.Send("add", i.Id, i.Face, Convert.ToInt32(i.X), Convert.ToInt32(i.Y));
							}
						});
						player.Initiated = true;
					}
					break;

				case "face":
					if (player.Initiated)
					{
						if (message.Count == 1)
						{
							int id = 0;
							if (int.TryParse(message[0].ToString(), out id))
							{
								if (id >= Config.MinSmilies && id <= Config.MaxSmilies)
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
								if (!double.TryParse(message[i].ToString(), out Args[i]))
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
							if (int.TryParse(message[0].ToString(), out x))
							{
								if (int.TryParse(message[1].ToString(), out y))
								{
									if (int.TryParse(message[2].ToString(), out id))
									{
										if (id >= Config.MinBlocks && id <= Config.MaxBlocks)
										{
											if (x >= 0 && x < Config.WorldWidth)
											{
												if (y >= 0 && y < Config.WorldHeight)
												{
													if (id <= 4)
													{
														if (x >= 1 && y >= 1 && x <= Config.WorldHeight - 2 && y <= Config.WorldHeight - 2)
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
					break;
			}
			base.GotMessage(player, message);
		}
	}
}