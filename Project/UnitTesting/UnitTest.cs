using Newtonsoft.Json;
using SLELibrary;

namespace UnitTesting
{
	[TestClass]
	public class UnitTest
	{
		[TestMethod]
		public void TestConjugateGradientMethod()
		{
			string json = File.ReadAllText("D:\\Course Project(NAP)\\Project\\UnitTesting\\test_sle.json");
			SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;
			sle.ConjugateGradientMethod(0.0001, 10000);
			double[] solutionSeidelMethod = sle.X!;
			sle.GaussianMethod();
			double[] solutionGaussianMethod = sle.X!;

			for (int i = 0; i < solutionSeidelMethod.Length; i++)
				Assert.IsTrue(Math.Abs(solutionSeidelMethod[i] - solutionGaussianMethod[i]) < 0.1);
		}

		[TestMethod]
		public void TestGaussianMethod()
		{
			string json = File.ReadAllText("D:\\Course Project(NAP)\\Project\\UnitTesting\\test_sle.json");
			SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;
			sle.ConjugateGradientMethod(0.0001, 10000);
			double[] solutionSeidelMethod = sle.X!;
			sle.GaussianMethod();
			double[] solutionGaussianMethod = sle.X!;

			for (int i = 0; i < solutionSeidelMethod.Length; i++)
				Assert.IsTrue(Math.Abs(solutionSeidelMethod[i] - solutionGaussianMethod[i]) < 0.1);
		}
	}
}