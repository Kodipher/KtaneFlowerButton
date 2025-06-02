using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rephidock.GeneralUtilities.Randomness;
using Rephidock.GeneralUtilities.Collections;
using FlowerButtonMod.Utils;


namespace FlowerButtonMod.FlowerButton {

	internal class SappedDisplayGenerator {

		#region //// Params + constructor

		System.Random Rng { get; /*init;*/ }

		public int?[] PreferredDigits { get; /*init;*/ }

		/// <param name="preferredDigits">Array of preferred digits (or null for no preference), highest digit first.</param>
		public SappedDisplayGenerator(int?[] preferredDigits, System.Random rng) {
			PreferredDigits = preferredDigits;
			Rng = rng;

			// Init non preferred count and bags
			NonPreferredLeft = EnumerableExtensions.RepeatFactory(
									() => rng.Next(0, nonPreferredMax+1),
									preferredDigits.Length
								)
								.ToArray();

			NonPreferredOrderedBags = EnumerableExtensions.RepeatFactory(
									() => new Queue<int>(),
									preferredDigits.Length
								)
								.ToArray();
		}

		#endregion

		#region //// Digit Picking

		/// <summary>
		/// Queues of non-preferred digits, 
		/// for each position.
		/// </summary>
		Queue<int>[] NonPreferredOrderedBags { get; /*init;*/ }

		/// <summary>
		/// Count of non-preferred left to show before showing preferred, 
		/// for each position.
		/// </summary>
		int[] NonPreferredLeft { get; /*init;*/ }

		const int nonPreferredMin = 1;
		const int nonPreferredMax = 2;

		public int GetNonPreferredDigit(int index) {
			Queue<int> bag = NonPreferredOrderedBags[index];
			if (bag.Count == 0) RefillBag(index);
			return bag.Dequeue();
		}

		public void RefillBag(int index) {

			Queue<int> bag = NonPreferredOrderedBags[index];
			int? prefferedDigit = PreferredDigits[index];

			int[] allDigits = Enumerable.Range(0, 10).ToArray().Shuffle(Rng) as int[];

			if (prefferedDigit == null) {
				// No preference: add all
				foreach (var digit in allDigits) {
					bag.Enqueue(digit);
				}
			} else {
				// Preference: add all except preference
				foreach (var digit in allDigits) {
					if (digit == prefferedDigit) continue;
					bag.Enqueue(digit);
				}
			}

		}

		/// <summary>
		/// Ticks a display position and gives a digit to display for that tick.
		/// </summary>
		int GetNextDigit(int index) {

			if (PreferredDigits[index] == null) {
				return GetNonPreferredDigit(index);
			}

			if (NonPreferredLeft[index] == 0) {
				NonPreferredLeft[index] = Rng.Next(nonPreferredMin, nonPreferredMax + 1);
				return PreferredDigits[index].Value;
			}

			NonPreferredLeft[index] = NonPreferredLeft[index] - 1;
			return GetNonPreferredDigit(index);
		}

		#endregion

		#region //// Display

		/// <summary>Generates a new <see cref="DisplayOverride"/></summary>
		public void TickDisplay() {

			// Tick all positions
			int?[] digits = new int?[PreferredDigits.Length];

			for (int i = 0; i < digits.Length; i++) {
				digits[i] = GetNextDigit(i);
			}

			// Set display
			DisplayOverride = FormatDisplayFromDigits(digits);
		}

		public string FormatDisplayFromDigits(int?[] digits, int nullDigit = -1) {

			var sb = new StringBuilder();

			for (int i = 0; i < digits.Length - 2; i++) {
				sb.Append((char)('0' + (digits[i] ?? nullDigit)));
			}

			sb.Append(':');

			for (int i = digits.Length - 2; i < digits.Length; i++) {
				sb.Append((char)('0' + (digits[i] ?? nullDigit)));
			}

			return sb.ToString();
		}

		public void SetDisplayToPreffered(int nullDigit) {
			FormatDisplayFromDigits(PreferredDigits, nullDigit);
		}

		/// <summary>The current sapped display to be used</summary>
		public string DisplayOverride { get; set; } = "";

		#endregion

	}

}
