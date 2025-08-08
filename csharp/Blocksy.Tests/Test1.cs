using Blocksy.Core;
using Blocksy.Core.Generators;
using Blocksy.Core.Generators.BasicHill;

namespace Blocksy.Tests
{
	[TestClass]
	public sealed class Test1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var prng = PRNG.Create(new Random());
			string seed = prng.Serialize();

			for (int i = 0; i < 1000; i++)
			{
				var hill = BasicHillGenerator.Create(prng);
				Assert.IsNotNull(hill, $"from seed: {seed}");
			}
		}

		[TestMethod]
		public void TestMethod2()
		{
			var prng = PRNG.Create(new Random());
			string seed = prng.Serialize();

			for (int i = 0; i < 1000; i++)
			{
				var hill = BasicHill2.Create(prng, width: 50);
				Assert.IsNotNull(hill, $"from seed: {seed}");
			}
		}

		[TestMethod]
		public void TestMethod3()
		{
			var prng = PRNG.Create(new Random());
			string seed = prng.Serialize();

			for (int i = 0; i < 1000; i++)
			{
				var hill = Hillish.Create(prng);
				Assert.IsNotNull(hill, $"from seed: {seed}");
			}
		}
	}
}
