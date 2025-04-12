using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace FlowerButtonMod.Utils {

	public static class EnumerableExtensions {

		/// <summary>
		/// Generates a sequence that contains values returned by a delegate.
		/// </summary>
		/// <param name="factory">The delegate to call to create each item</param>
		/// <param name="count">The number of items</param>
		/// <returns>A new <see cref="IEnumerable{T}"/></returns>
		/// <exception cref="ArgumentNullException"><paramref name="factory"/> is null</exception>
		public static IEnumerable<TResult> RepeatFactory<TResult>(Func<TResult> factory, int count) {
			// Guard
			if (null == factory) throw new ArgumentNullException("factory");

			for (int i = 0; i < count; i++) {
				yield return factory();
			}
		}

		/// <summary>
		/// Casts the elements of an IEnumerable to the specified type.
		/// </summary>
		public static IEnumerable Cast(this IEnumerable source, Type targetType) {

			if (null == source) throw new ArgumentNullException(nameof(source));
			if (null == targetType) throw new ArgumentNullException(nameof(targetType));

			var castMethod =
				typeof(Enumerable)
				.GetMethod("Cast", BindingFlags.Static | BindingFlags.Public)
				.MakeGenericMethod(new Type[] { targetType });

			return castMethod.Invoke(null, new object[] { source }) as IEnumerable;
		}

	}

}
