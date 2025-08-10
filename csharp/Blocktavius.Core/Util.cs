using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocktavius.Core;

public static class Util
{
	sealed class Translator<T> : I2DSampler<T>
	{
		private readonly I2DSampler<T> sampler;
		private readonly XZ translation;
		public Rect Bounds { get; }

		public Translator(I2DSampler<T> sampler, XZ translation)
		{
			this.sampler = sampler;
			this.Bounds = sampler.Bounds.Translate(translation);
			this.translation = translation;
		}

		public T Sample(XZ xz) => sampler.Sample(xz.Subtract(translation));
	}

	public static I2DSampler<T> Translate<T>(this I2DSampler<T> sampler, XZ xz)
	{
		return new Translator<T>(sampler, xz);
	}
}
