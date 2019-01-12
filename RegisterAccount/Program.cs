using PlayerIOClient;

using System;

namespace RegisterAccount
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			//In the client, you will need to change the password to a space.
			PlayerIO.QuickConnect.SimpleRegister("oldy-l95uohocfu66qcnb6swlma", "whatever", " ", "", "", "", null, "", null);

			//.SimpleRegister("oldy-l95uohocfu66qcnb6swlma", "whatever", "", "whatever@whatever.com", "", "", null, "", null);
			Client join = PlayerIO.Connect("oldy-l95uohocfu66qcnb6swlma", "public", "whatever", "", "");
			Connection x = join.Multiplayer.CreateJoinRoom("0x0", "FlixelWalker1", true, null, null);
			Console.WriteLine("Registered Account.");
			Console.ReadKey();
		}
	}
}