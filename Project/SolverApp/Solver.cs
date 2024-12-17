using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using SLELibrary;

namespace SolverApp
{
	public class Solver
	{
		public IPAddress IpAddress { get; private set; }
		public int Port { get; private set; }
		Socket Socket { get; set; }
		NetworkStream Stream { get; set; }

		public Solver(string ipAddress = "0.0.0.0", int port = 8080)
		{
			IpAddress = IPAddress.Parse(ipAddress);
			Port = port;
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Start()
		{
			Socket.Connect(new IPEndPoint(IpAddress, Port));
			Console.WriteLine($"Клиент подключен к серверу по адресу {IpAddress}:{Port}");

			Stream = new NetworkStream(Socket);
			byte[] message = Encoding.UTF8.GetBytes("Worker client");
			Stream.Write(message);

			GetTask();
		}

		void GetTask()
		{
			string request;
			TaskDataNormalization? taskDataNormalization;
			TaskDataCalculation? taskDataCalculation;
			byte[] message;

			while (true)
			{
				request = Read(Stream);

				if (request == "Server has shut down")
					break;

				if (request == "")
					continue;

				taskDataNormalization = JsonConvert.DeserializeObject<TaskDataNormalization>(request,
					new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
				ConvertToIterativeForm(taskDataNormalization);

				message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataNormalization));
				Stream.Write(message);

				while (true)
				{
					request = Read(Stream);

					if (request == "End of data transfer")
						break;

					taskDataCalculation = JsonConvert.DeserializeObject<TaskDataCalculation>(request,
						new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
					CalcSum(taskDataCalculation);

					message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation));
					Stream.Write(message);
				}
			}
		}

		public string Read(NetworkStream stream)
		{
			StringBuilder requestBuilder = new StringBuilder();
			byte[] buffer = new byte[1024];
			int length;

			do
			{
				length = stream.Read(buffer, 0, buffer.Length);
				requestBuilder.Append(Encoding.UTF8.GetString(buffer, 0, length));
			}
			while (stream.DataAvailable);

			return requestBuilder.ToString();
		}

		void ConvertToIterativeForm(TaskDataNormalization taskData)
		{
			double divider;

			for (int i = 0; i < taskData.A.Length; i++)
			{
				divider = taskData.A[i][taskData.Index];
				taskData.A[i][taskData.Index] = 0;

				for (int j = 0; j < taskData.A[0].Length; j++)
					if (taskData.Index != j)
						taskData.A[i][j] /= -divider;

				taskData.B[i] /= divider;
				taskData.Index++;
			}
		}

		void CalcSum(TaskDataCalculation taskData)
		{
			for (int i = 0; i < taskData.Row.Length; i++)
				taskData.Sum += taskData.Row[i] * taskData.X[i];
		}

		public void Stop()
		{
			Socket.Shutdown(SocketShutdown.Both);
			Socket.Close();
		}
	}
}