using Newtonsoft.Json;
using ServerApp;
using SLELibrary;
using SolverApp;
using System;

namespace LoadTesting
{
	class LoadTest
	{
		const int NumberOfTests = 100;
		const string IPADDRESS = "127.0.0.1";
		const int PORT = 8080;

		static void Main()
		{
			Console.ForegroundColor = ConsoleColor.Black;
			Console.BackgroundColor = ConsoleColor.White;
			Console.Clear();

			Server server = new Server(IPADDRESS, PORT);
			server.Start();

			for (int i = 0; i < 5; i++)
			{
				Solver solver = new Solver(IPADDRESS, PORT);
				ThreadPool.QueueUserWorkItem(_ => solver.Start());
				Thread.Sleep(10);
			}

			string json = File.ReadAllText("D:\\Course Project(NAP)\\Project\\UnitTesting\\test_sle.json");
			SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;

			for (int i = 0; i < NumberOfTests; i++)
			{
				sle.SeidelMethodParallel(0.0001, 10000, server.Solvers);
				Console.WriteLine($"Задача #{i + 1} решена.");
			}
        }
	}
}