using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using MatrixLibrary;

namespace SLELibrary
{
	public class SLE
	{
		[JsonProperty("system_matrix")]
		public double[][] A { get; private set; }
		[JsonProperty("free_members_vector")]
		public double[] B { get; private set; }
		[JsonIgnore]
		public double[]? X { get; private set; }
		[JsonProperty("roots")]
		public double[]? Result
		{
			get
			{
				if (X == null)
					return null;

				double[] result = new double[X.Length];

				for (int i = 0; i < X.Length; i++)
					result[i] = Math.Round(X[i], 2);
				
				return result;
			}
		}

		public SLE(double[][] a, double[] b)
		{
			A = a;
			B = b;
			X = new double[A[0].Length];
		}

		// Метод сопряжённых градиентов
		public void ConjugateGradientMethod(double e, int n)
		{
			double[][] a = Matrix.Multiply(Matrix.Transpose(A), A);
			double[] b = Matrix.Multiply(Matrix.Transpose(A), B);

			double divider;

			for (int i = 0; i < a.Length; i++)
			{
				divider = a[i][i];
				a[i][i] = 0;

				for (int j = 0; j < A[0].Length; j++)
					if (i != j)
						a[i][j] /= -divider;

				b[i] /= divider;
			}

			X = new double[a[0].Length];
			double[] prevX = new double[X.Length];
			int step = 0;
			double sum;
			bool flag;

			while (step < n)
			{
				step += 1;

				Array.Copy(X!, prevX, X!.Length);

				for (int i = 0; i < a.Length; i++)
				{
					sum = 0;

					for (int j = 0; j < i; j++)
						sum += a[i][j] * X[j];

					for (int j = i; j < a[0].Length; j++)
						sum += a[i][j] * prevX[j];

					X[i] = b[i] + sum;
				}

				flag = true;

				for (int i = 0; i < X.Length; i++)
					if (Math.Abs(X[i] - prevX[i]) > e)
						flag = false;

				if (flag)
					break;

				if (step == n)
					X = null;
			}
		}

		// Распараллеленный метод сопряжённых градиентов
		public void ConjugateGradientMethodParallel(double e, int n, List<Socket> solvers)
		{
			double[][] a = Matrix.Multiply(Matrix.Transpose(A), A);
			double[] b = Matrix.Multiply(Matrix.Transpose(A), B);

			int rowsPerSolver;
			Task[] tasks;
			double[][] aPart;
			double[] bPart;
			int leftBorder;
			int rightBorder;
			TaskDataNormalization taskDataNormalization;
			Mutex mutex = new Mutex();

			if (solvers.Count > a.Length)
			{
				rowsPerSolver = 1;
				tasks = new Task[a.Length];
			}
			else
			{
				rowsPerSolver = a.Length / solvers.Count;
				tasks = new Task[solvers.Count];
			}

			for (int i = 0; i < tasks.Length; i++)
			{
				(aPart, bPart) = GetPartSLE(a, b, i, rowsPerSolver, tasks.Length);
				leftBorder = i * rowsPerSolver;
				rightBorder = (i == tasks.Length - 1) ? a.Length : leftBorder + rowsPerSolver;
				taskDataNormalization = new TaskDataNormalization(aPart, bPart, leftBorder, rightBorder);
				NetworkStream stream = new NetworkStream(solvers[i]);
				stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataNormalization)));

				tasks[i] = Task.Run(() =>
				{
					TaskDataNormalization? answer = Read<TaskDataNormalization>(stream);

					mutex.WaitOne();

					for (int i = answer.StartIndex, j = 0; i < answer.EndIndex; i++, j++)
					{
						a[i] = answer.A[j];
						b[i] = answer.B[j];
					}

					mutex.ReleaseMutex();
				});
			}

			Task.WaitAll(tasks);

			X = new double[a[0].Length];
			double[] prevX = new double[a[0].Length];
			int elemsPerSolver;
			int step = 0;
			double sum;
			bool flag;
			double[] rowPart = [];
			double[] xPart;
			TaskDataCalculation taskDataCalculation;

			while (step < n)
			{
				Array.Copy(X!, prevX, X!.Length);

				for (int i = 0; i < a.Length; i++)
				{
					sum = 0;

					if (solvers.Count > i)
					{
						elemsPerSolver = 1;
						tasks = new Task[i];
					}
					else
					{
						elemsPerSolver = i / solvers.Count;
						tasks = new Task[solvers.Count];
					}

					for (int j = 0; j < tasks.Length; j++)
					{
						rowPart = GetPartRow(a[i][0..i], j, elemsPerSolver, tasks.Length);
						xPart = GetPartRow(X[0..i], j, elemsPerSolver, tasks.Length);

						if (rowPart.Length == 0)
							continue;

						taskDataCalculation = new TaskDataCalculation(rowPart, xPart);
						NetworkStream stream = new NetworkStream(solvers[j]);
						stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation)));

						tasks[j] = Task.Run(() =>
						{
							TaskDataCalculation? answer = Read<TaskDataCalculation>(stream);

							mutex.WaitOne();
							sum += answer.Sum;
							mutex.ReleaseMutex();
						});
					}

					if (rowPart.Length != 0)
						Task.WaitAll(tasks);

					if (solvers.Count > a.Length - i)
					{
						elemsPerSolver = 1;
						tasks = new Task[a.Length - i];
					}
					else
					{
						elemsPerSolver = (a.Length - i) / solvers.Count;
						tasks = new Task[solvers.Count];
					}

					for (int j = 0; j < tasks.Length; j++)
					{
						rowPart = GetPartRow(a[i][i..a.Length], j, elemsPerSolver, tasks.Length);
						xPart = GetPartRow(prevX[i..a.Length], j, elemsPerSolver, tasks.Length);

						if (rowPart.Length == 0)
							continue;

						taskDataCalculation = new TaskDataCalculation(rowPart, xPart);
						NetworkStream stream = new NetworkStream(solvers[j]);
						stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation)));

						tasks[j] = Task.Run(() =>
						{
							TaskDataCalculation? answer = Read<TaskDataCalculation>(stream);

							mutex.WaitOne();
							sum += answer.Sum;
							mutex.ReleaseMutex();
						});
					}

					if (rowPart.Length != 0)
						Task.WaitAll(tasks);

					X[i] = b[i] + sum;
				}

				flag = true;

				for (int i = 0; i < X.Length; i++)
				{
					if (Math.Abs(X[i] - prevX[i]) > e)
						flag = false;
				}

				if (flag)
				{
					for (int i = 0; i < solvers.Count; i++)
					{
						NetworkStream stream = new NetworkStream(solvers[i]);
						stream.Write(Encoding.UTF8.GetBytes("End of data transfer"));
					}

					break;
				}

				step += 1;

				if (step == n)
					X = null;
			}
		}

		// Метод Гаусса
		public void GaussianMethod()
		{
			double[][] a = new double[A.Length][];
			double[] b = new double[A.Length];
			X = new double[A[0].Length];
			Array.Copy(A, a, A.Length);
			Array.Copy(B, b, A.Length);
			double factor;

			// Приведение расширенной матрицы к треугольному виду (прямой ход)
			for (int i = 0; i < A[0].Length - 1; i++)
			{
				for (int j = i + 1; j < A.Length; j++)
				{
					factor = a[j][i] / a[i][i];

					for (int k = 0; k < A[0].Length; k++)
						a[j][k] -= a[i][k] * factor;

					b[j] -= b[i] * factor;
				}
			}

			double sum;

			// Вычисление корней (обратный ход)
			for (int i = A.Length - 1; i >= 0; i--)
			{
				sum = 0;

				for (int j = i + 1; j < A[0].Length; j++)
					sum += a[i][j] * X[j];

				X[i] = (b[i] - sum) / a[i][i];
			}
		}

		(double[][], double[]) GetPartSLE(double[][] a, double[] b, int number, int size, int length)
		{
			int leftBorder = number * size;
			int rightBorder = (number == length - 1) ? a.Length : leftBorder + size;
			double[][] aPart = a[leftBorder..rightBorder];
			double[] bPart = b[leftBorder..rightBorder];

			return (aPart, bPart);
		}

		double[] GetPartRow(double[] row, int number, int size, int length)
		{
			if (row.Length <= 1)
				return row;

			int leftBorder = number * size;
			int rightBorder = (number == length - 1) ? row.Length : leftBorder + size;
			double[] rowPart = row[leftBorder..rightBorder];

			return rowPart;
		}

		T? Read<T>(NetworkStream stream)
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

			T? answer = JsonConvert.DeserializeObject<T>(requestBuilder.ToString(), new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

			return answer;
		}
	}
}