using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rephidock.GeneralUtilities.Randomness;
using Rephidock.GeneralUtilities.Collections;
using FlowerButtonMod.Utils;


namespace FlowerButtonMod.FlowerButton {

	internal class TimerDisplayOverride {

		#region //// Params + constructor

		System.Random Rng { get; /*init;*/ }

		public int?[] PreferredDigits { get; /*init;*/ }

		/// <param name="preferredDigits">Array of preferred digits (or null for no preference), highest digit first.</param>
		public TimerDisplayOverride(int?[] preferredDigits, System.Random rng) {
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

		#region //// Non-Preferred digit picking

		Queue<int>[] NonPreferredOrderedBags { get; /*init;*/ }

		public void RefillBag(Queue<int> bag, int? prefferedDigit) {

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

		public int GetNonPreferredDigit(int index) {
			Queue<int> bag = NonPreferredOrderedBags[index];
			if (bag.Count == 0) RefillBag(bag, PreferredDigits[index]);
			return bag.Dequeue();
		}

		int[] NonPreferredLeft { get; /*init;*/ }

		const int nonPreferredMin = 1;
		const int nonPreferredMax = 2;
		

		/// <summary>
		/// Ticks a display position and gives a digit to display for that tick.
		/// </summary>
		int TickDisplayPosition(int index) {

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

		public void TickDisplay() {

			// Tick all positions
			int?[] digits = new int?[PreferredDigits.Length];

			for (int i = 0; i < digits.Length; i++) {
				digits[i] = TickDisplayPosition(i);
			}

			// Set disaply
			SetDisplayToDigits(digits);
			
		}

		public void SetDisplayToDigits(int?[] digits, int nullDigit = -1) {

			// Format
			var sb = new StringBuilder();

			for (int i = 0; i < digits.Length - 2; i++) {
				sb.Append((char)('0' + (digits[i] ?? nullDigit)));
			}

			sb.Append(':');

			for (int i = digits.Length - 2; i < digits.Length; i++) {
				sb.Append((char)('0' + (digits[i] ?? nullDigit)));
			}

			// Commit new display
			DisplayOverride = sb.ToString();
		}

		public void SetDisplayToPreffered(int nullDigit) {
			SetDisplayToDigits(PreferredDigits, nullDigit);
		}

		public void OverruleOverride(string display) {
			DisplayOverride = display;
		}

		public string DisplayOverride { get; private set; } = "";

		#endregion

	}

}
