using Blocktavius.Core;
using Blocktavius.Core.Generators;
using Blocktavius.Core.Generators.BasicHill;

namespace Blocktavius.Tests
{
	[TestClass]
	public sealed class Test1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var prng = PRNG.Create(new Random());
			Console.WriteLine(prng.Serialize());

			for (int i = 0; i < 1000; i++)
			{
				var hill = BasicHillGenerator.Create(prng);
				Assert.IsNotNull(hill);
			}
		}

		[TestMethod]
		public void TestMethod2()
		{
			var prng = PRNG.Create(new Random());
			Console.WriteLine(prng.Serialize());

			for (int i = 0; i < 1000; i++)
			{
				var hill = BasicHill2.Create(prng, width: 50);
				Assert.IsNotNull(hill);
			}
		}

		[TestMethod]
		public void TestMethod3()
		{
			var prng = PRNG.Create(new Random());
			Console.WriteLine(prng.Serialize());

			for (int i = 0; i < 1000; i++)
			{
				var hill = Hillish.Create(prng);
				Assert.IsNotNull(hill);
			}
		}

		[TestMethod]
		public void TestMethod4()
		{
			var prng = PRNG.Create(new Random());
			Console.WriteLine(prng.Serialize());

			for (int i = 0; i < 1000; i++)
			{
				var hill = ShinBasicHill.Generate(prng, 100, 60);
				Assert.IsNotNull(hill);
			}
		}
	}
}
