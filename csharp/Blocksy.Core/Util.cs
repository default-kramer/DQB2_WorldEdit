using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocksy.Core;

public static class Util
{
	sealed class Translator<T> : I2DSampler<T>
	{
		private readonly I2DSampler<T> sampler;
		private readonly XZ translation;
		public BoundingBox Box { get; }

		public Translator(I2DSampler<T> sampler, XZ translation)
		{
			this.sampler = sampler;
			this.Box = sampler.Box.Translate(translation);
			this.translation = translation;
		}

		public T Sample(XZ xz) => sampler.Sample(xz.Subtract(translation));
	}

	public static I2DSampler<T> Translate<T>(this I2DSampler<T> sampler, XZ xz)
	{
		return new Translator<T>(sampler, xz);
	}
}
