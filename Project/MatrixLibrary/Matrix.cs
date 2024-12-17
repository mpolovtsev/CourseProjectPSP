namespace MatrixLibrary
{
	public static class Matrix
	{
		public static double[][] Transpose(double[][] matrix)
		{
			double[][] result = new double[matrix[0].Length][];

			for (int i = 0; i < result.Length; i++)
				result[i] = new double[matrix.Length];

			for (int i = 0; i < matrix.Length; i++)
			{
				for (int j = 0; j < matrix[0].Length; j++)
					result[j][i] = matrix[i][j];
			}

			return result;
		}

		public static double[][] Multiply(double[][] matrix1, double[][] matrix2)
		{
			//***************
			if (matrix1.Length != matrix2[0].Length)
				throw new Exception();
			//***************

			double[][] result = new double[matrix1.Length][];

			for (int i = 0; i < matrix1.Length; i++)
			{
				result[i] = new double[matrix2[0].Length];

				for (int j = 0; j < matrix2[0].Length; j++)
				{
					result[i][j] = 0;

					for (int k = 0; k < matrix1[0].Length; k++)
						result[i][j] += matrix1[i][k] * matrix2[k][j];
				}
			}

			return result;
		}

		public static double[] Multiply(double[][] matrix1, double[] matrix2)
		{
			//***************
			if (matrix1.Length != matrix2.Length)
				throw new Exception();
			//***************

			double[] result = new double[matrix1.Length];

			for (int i = 0; i < matrix1.Length; i++)
			{
				result[i] = 0;

				for (int j = 0; j < matrix1[0].Length; j++)
					result[i] += matrix1[i][j] * matrix2[j];
			}

			return result;
		}

		//***************
		public static double[][] Multiply(double[] matrix1, double[][] matrix2)
		{
			//***************
			if (matrix1.Length != matrix2[0].Length)
				throw new Exception();
			//***************

			double[][] result = new double[matrix1.Length][];

			for (int i = 0; i < matrix1.Length; i++)
			{
				result[i] = new double[matrix2[0].Length];

				for (int j = 0; j < matrix2[0].Length; j++)
				{
					result[i][j] = 0;
					result[i][j] += matrix1[i] * matrix2[i][j];
				}
			}

			return result;
		}
	}
}