using PlayerIO.GameLibrary;

using System.Text;

//We are going to attempt to reproduce the gamecode of Old EE, including all of its screw ups.

namespace Oldy
{
	public class Player : BasePlayer
	{
		public bool Initiated;
	}

	public class Config
	{
		public const int Width = 100;
		public const int Height = 100;
	}

	[RoomType(nameof(FlixelWalker1))]
	public class FlixelWalker1 : Game<Player>
	{
		public int[,] World = new int[Config.Width, Config.Height];

		public override void GameStarted()
		{
			//We don't check if it's 0x0 or 8x5

			World = new int[Config.Width, Config.Height];

			for (int x = 0; x < World.GetLength(0); x++)
			{
				for (int y = 0; y < World.GetLength(1); y++)
				{
					int block;

					if ((x == 0 || x == 100 - 1)
						|| (y == 0 || y == 100 - 1))
					{
						block = 5;
					}
					else
					{
						block = 0;
					}

					World[x, y] = block;
				}
			}

			base.GameStarted();
		}

		public override void GameClosed() => base.GameClosed();

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
						player.Initiated = true;

						StringBuilder Serialize = new StringBuilder("");

						//Serialize the world data
						for (int y = 0; y < 100; y++)
						{
							Serialize.Append(World[0, y].ToString());
							for (int x = 1; x < 100; x++)
							{
								Serialize.Append(",");
								Serialize.Append(World[x, y].ToString());
							}
							if (y != 99)
							{
								Serialize.Append("\n");
							}
						}
						Broadcast("add", player.Id, 0, 16, 16);
						player.Send("init", Serialize.ToString(), player.Id);
					}
					break;

				case "face":
					if (message.Count == 1)
					{
						int id = 0;
						if (int.TryParse(message[0].ToString(), out id))
						{
							if (id >= 0 && id <= 5)
							{
								Broadcast("face", player.Id, id);
							}
						}
					}
					break;

				case "update":
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
							Broadcast("update", player.Id, Args[0], Args[1], Args[2], Args[3], Args[4], Args[5], Args[6], Args[7]);
						}
					}
					break;

				case "change":
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
									if (id > -1 && id < 21)
									{
										if (x > -1 && x < 100)
										{
											if (y > -1 && y < 100)
											{
												if (id <= 4)
												{
													if (x >= 1 && y >= 1 && x <= 98 && y <= 98)
													{
														Broadcast("change", x, y, id);
														World[x, y] = id;
													}
												}
												else
												{
													Broadcast("change", x, y, id);
													World[x, y] = id;
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