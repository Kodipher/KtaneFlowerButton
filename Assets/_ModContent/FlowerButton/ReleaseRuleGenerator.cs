using System.Collections.Generic;
using System.Linq;
using Rephidock.GeneralUtilities.Randomness;
using Rephidock.GeneralUtilities.Collections;


namespace FlowerButtonMod.FlowerButton {

	internal static class ReleaseRuleGenerator {

		#region //// Rule display digits

		// Subject, Verb and Object are ordered according to the manual

		enum SubjectDigit : int {
			UnitsDigit				= 1,
			TensDigit				= 6,
			EveryDigit				= 2,
			EitherDigit				= 0,
			NeitherDigit			= 3,
			DigitsProduct			= 5,                                        
			DigitsDifference		= 7,
			DigitsSum				= 8,                                            
			Display					= 4,
			DisplayReadBackwards	= 9
		}

		enum VerbDigit : int {
			Is				= 1,
			IsNot			= 5,
			Contains		= 4,
			DoesNotContain	= 7,
			IsDivisibleBy	= 0,
			IsLessThan		= 3,
			IsGreaterThan	= 8,
			BeginsWith		= 6,
			EndsWith		= 2,
			Is1AwayFrom		= 9
		}

		enum ObjectDigit : int {
			Digit0			= 0,
			Digit1Or7		= 2,
			Digit2Or8		= 4,
			Digit4Or6		= 6,
			TheSame			= 5,
			Prime			= 1,
			UnitsDigit		= 3,
			EitherDigit		= 7,
			TensDigit		= 8,
			AdditionalDigit	= 9 
		}

		enum AdditionalDigitReqirement {
			None,
			Any,
			Zero,
			One,
			TwoOrThree,
			SevenOrEight,
			TwoOrGreater,
			ThreeOrLess,
			ThreeThroughSeven,
			NotZero,
			NotNine
		}

		readonly static Dictionary<int, int> additionalDigitToDisplayDigit = new Dictionary<int, int>() {
			{ 4, 0 },
			{ 5, 1 },
			{ 2, 2 },
			{ 8, 3 },
			{ 1, 4 },
			{ 0, 5 },
			{ 3, 6 },
			{ 9, 7 },
			{ 7, 8 },
			{ 6, 9 },		
		};

		#endregion

		#region //// Boolean operators on seqeuence

		delegate bool BoolOperator(IEnumerable<bool> arguments);

		static class BoolOps {
			public static bool AnyTrue(IEnumerable<bool> seq) => seq.Any(x => x);
			public static bool AllTrue(IEnumerable<bool> seq) => seq.All(x => x);
			public static bool AllFalse(IEnumerable<bool> seq) => seq.All(x => !x);
		}

		#endregion

		#region //// Tree node definitions

		delegate IEnumerable<int> SubjectValueGenerator(int time);

		struct SubjectNode {

			public SubjectDigit DisplayDigit { get; }
			public SubjectValueGenerator ValueGenerator { get; }
			public BoolOperator ValidatorCombiner { get; }
			public VerbNode[] PossibleVerbs { get; }

			public SubjectNode(
				SubjectDigit displayDigit,
				SubjectValueGenerator valueGenerator,
				BoolOperator validatorCombiner,
				params VerbNode[] possibleVerbs
			) {
				DisplayDigit = displayDigit;
				ValueGenerator = valueGenerator;
				ValidatorCombiner = validatorCombiner;
				PossibleVerbs = possibleVerbs;
			}

		}


		delegate bool VerbNodeValidator(int subject, int @object);

		struct VerbNode {

			public VerbDigit DisplayDigit { get; set; }
			public VerbNodeValidator Validator { get; }
			public BoolOperator ValidatorCombiner { get; }
			public ObjectNode[] PossibleObjects { get; }

			public VerbNode(
				VerbDigit displayDigit,
				VerbNodeValidator validator,
				BoolOperator validatorCombiner,
				ObjectNode[] objects = null
			) {
				DisplayDigit = displayDigit;
				Validator = validator;
				ValidatorCombiner = validatorCombiner;
				PossibleObjects = objects;
			}

			public VerbNode WithObjects(params ObjectNode[] objects) {
				return new VerbNode(DisplayDigit, Validator, ValidatorCombiner, objects);
			}

		}


		delegate IEnumerable<int> ObjectValueGenerator(int time);

		struct ObjectNode {

			public ObjectDigit DisplayDigit { get; }
			public AdditionalDigitReqirement AdditionalDigit { get; }
			public ObjectValueGenerator ValueGenerator { get; }

			public ObjectNode(
				ObjectDigit DisplayDigit,
				ObjectValueGenerator valueGenerator,
				AdditionalDigitReqirement additionalDigit = AdditionalDigitReqirement.None
			) {
				this.DisplayDigit = DisplayDigit;
				AdditionalDigit = additionalDigit;
				ValueGenerator = valueGenerator;
			}

			public static ObjectNode FromObjects(ObjectDigit displayDigit, params int[] objects) {
				return new ObjectNode(displayDigit, (_) => objects);
			}

			public ObjectNode WithAdditionalDigit(AdditionalDigitReqirement reqirement = AdditionalDigitReqirement.Any) {
				return new ObjectNode(DisplayDigit, ValueGenerator, reqirement);
			}

		}

		#endregion

		#region //// Rule tree common nodes

		static class CommonNodes {

			// Note: all subjects and objects are within range 00..99

			// Verbs
			public static readonly VerbNode Is = 
				new VerbNode(VerbDigit.Is, (sub, obj) => sub == obj, BoolOps.AnyTrue);

			public static readonly VerbNode IsNot = 
				new VerbNode(VerbDigit.IsNot, Is.Validator, BoolOps.AllFalse);
			
			public static readonly VerbNode IsDivisbleBy = 
				new VerbNode(VerbDigit.IsDivisibleBy, (sub, obj) => obj != 0 && sub % obj == 0, BoolOps.AnyTrue);
			
			public static readonly VerbNode IsLessThan =
				new VerbNode(VerbDigit.IsLessThan, (sub, obj) => sub < obj, BoolOps.AnyTrue);

			public static readonly VerbNode IsGreaterThan =
				new VerbNode(VerbDigit.IsGreaterThan, (sub, obj) => sub > obj, BoolOps.AnyTrue);
			
			public static readonly VerbNode Is1Away = 
				new VerbNode(VerbDigit.Is1AwayFrom, (sub, obj) => sub+1 == obj || sub-1 == obj, BoolOps.AnyTrue);
			
			public static readonly VerbNode Contains = new VerbNode(
															VerbDigit.Contains, 
															(sub, obj) => {
																// single digit
																if (sub < 10) {
																	// cant contain 2 digit
																	if (obj >= 10) return false;
																	// can only contain 1 digit by equality
																	return sub == obj;
																}
																// double digit
																// can only contain 2 digit by equality
																if (obj >= 10) return sub == obj;
																// can contain 1 digit in either position
																return sub%10 == obj || sub/10 == obj;
															},
															BoolOps.AnyTrue
														);

			public static readonly VerbNode DoesNotContain = 
				new VerbNode(VerbDigit.DoesNotContain, Contains.Validator, BoolOps.AllFalse);

			public static readonly VerbNode BeginsWith = new VerbNode(
															VerbDigit.BeginsWith,
															(sub, obj) => {
																// 1 digit in 2 digit
																if (obj < 10 && sub >= 10) return sub / 10 == obj;
																// Otherwise (2 in 1, 2 in 2, 1 in 1)
																return sub == obj;
															},
															BoolOps.AnyTrue
														);

			public static readonly VerbNode EndsWith = new VerbNode(
															VerbDigit.EndsWith,
															(sub, obj) => {
																// 1 digit in 2 digit
																if (obj < 10 && sub >= 10) return sub % 10 == obj;
																// Otherwise (2 in 1, 2 in 2, 1 in 1)
																return sub == obj;
															},
															BoolOps.AnyTrue
														);

			public static readonly VerbNode ContainsWithSubjectWithLeadingZero = new VerbNode(
															VerbDigit.Contains,
															(sub, obj) => {
																// Same as contains but
																// 0X always contains leading zero
																if (sub < 10 && obj == 0) return true;
																return Contains.Validator(sub, obj);
															},
															BoolOps.AnyTrue
														);

			public static readonly VerbNode DoesNotContainWithSubjectWithLeadingZero =
				new VerbNode(VerbDigit.DoesNotContain, ContainsWithSubjectWithLeadingZero.Validator, BoolOps.AllFalse);

			public static readonly VerbNode BeginsWithWithSubjectWithLeadingZero = new VerbNode(
															VerbDigit.BeginsWith,
															(sub, obj) => {
																// 1 digit in 2 digit
																if (obj < 10) return sub / 10 == obj;
																// 2 digit in 2 digit
																return sub == obj;
															},
															BoolOps.AnyTrue
														);

			// Objects
			public static readonly ObjectNode Digit0 = ObjectNode.FromObjects(ObjectDigit.Digit0, 0);
			public static readonly ObjectNode Digit1Or7 = ObjectNode.FromObjects(ObjectDigit.Digit1Or7, 1, 7);
			public static readonly ObjectNode Digit2Or8 = ObjectNode.FromObjects(ObjectDigit.Digit2Or8, 2, 8);
			public static readonly ObjectNode Digit4Or6 = ObjectNode.FromObjects(ObjectDigit.Digit4Or6, 4, 6);

			public static readonly ObjectNode Prime = ObjectNode.FromObjects(
															ObjectDigit.Prime,
															// Written out directly because otherwise can be a null ref
															// because of some wrong order of init or whatever
															new int[] {
																2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 
																31, 37, 41, 43, 47, 53, 59, 61, 67, 
																71, 73, 79, 83, 89, 97
															}
														);

			public static readonly ObjectNode EitherDigit = new ObjectNode(
															ObjectDigit.EitherDigit,  
															(time) => new int[] { time % 10, time / 10 }
														);

			public static readonly ObjectNode UnitsDigit = new ObjectNode(
															ObjectDigit.UnitsDigit,
															(time) => new int[] { time % 10 }
														);

			public static readonly ObjectNode TensDigit = new ObjectNode(
															ObjectDigit.TensDigit,
															(time) => new int[] { time / 10 }
														);

			public static readonly ObjectNode AdditionalOnlyAny = new ObjectNode(
															ObjectDigit.AdditionalDigit, 
															(_) => Enumerable.Empty<int>(), 
															AdditionalDigitReqirement.Any
														);

			public static readonly ObjectNode AdditionalOnlyOne = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.One
														);

			public static readonly ObjectNode AdditionalOnlyTwoOrThree = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.TwoOrThree
														);

			public static readonly ObjectNode AdditionalOnlyThreeOrLess = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.ThreeOrLess
														);

			public static readonly ObjectNode AdditionalOnlySevenOrEight = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.SevenOrEight
														);

			public static readonly ObjectNode AdditionalOnlyThreeThroughSeven = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.ThreeThroughSeven
														);

			public static readonly ObjectNode AdditionalOnlyNotZero = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.NotZero
														);

			public static readonly ObjectNode AdditionalOnlyNotNine = new ObjectNode(
															ObjectDigit.AdditionalDigit,
															(_) => Enumerable.Empty<int>(),
															AdditionalDigitReqirement.NotNine
														);
		}

		#endregion

		#region //// Rule Tree

		readonly static SubjectNode[] ruleTree = new SubjectNode[] {

			#region //// -> UnitsDigit

			new SubjectNode(
				SubjectDigit.UnitsDigit,
				(time) => (time % 10).Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.Prime,
					CommonNodes.TensDigit,
					CommonNodes.Digit0.WithAdditionalDigit(),
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit0.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit1Or7.WithAdditionalDigit(),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit()
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.Prime,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					CommonNodes.AdditionalOnlyAny,
					// Hacky weighting
					CommonNodes.TensDigit,
					CommonNodes.TensDigit
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.AdditionalOnlyTwoOrThree,
					CommonNodes.TensDigit
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyNotZero,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					// Hacky weighting
					CommonNodes.TensDigit,
					CommonNodes.TensDigit
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.AdditionalOnlySevenOrEight,
					CommonNodes.AdditionalOnlyNotNine,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					// Hacky weighting
					CommonNodes.TensDigit,
					CommonNodes.TensDigit,
					CommonNodes.TensDigit
				)

			),

			#endregion

			#region //// -> TensDigit

			new SubjectNode(
				SubjectDigit.TensDigit,
				(time) => (time / 10).Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater),
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.UnitsDigit,
					CommonNodes.UnitsDigit.WithAdditionalDigit(AdditionalDigitReqirement.One)
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit0.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrThree),
					// Hacky weighting
					CommonNodes.UnitsDigit.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater),
					CommonNodes.UnitsDigit.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater)
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Prime,
					// Hacky weighting
					CommonNodes.UnitsDigit,
					CommonNodes.UnitsDigit
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					CommonNodes.UnitsDigit
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.UnitsDigit
				)

			),

			#endregion

			#region //// -> EveryDigit

			new SubjectNode(
				SubjectDigit.EveryDigit,
				(time) => new int[] { time % 10, time / 10 },
				BoolOps.AllTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.One),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.One)
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.NotZero)
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Prime
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyThreeThroughSeven
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.Digit0
				)

			),

			#endregion

			#region //// EveryDigit is the same

			new SubjectNode(
				SubjectDigit.EveryDigit,
				(time) => {
					// Hacky way to move validation here (from the verb):
					// return subject of 1 on pass
					// return subject of 0 on fail
					return (time % 10 == time / 10 ? 1 : 0).Yield();
				},
				BoolOps.AnyTrue,

				new VerbNode(
					VerbDigit.Is,
					(subject, _) => subject == 1,
					BoolOps.AnyTrue,
					new ObjectNode[] {
						// 1 literal object so that the checking lambda runs
						ObjectNode.FromObjects(ObjectDigit.TheSame, new int[1])
					}
				)

			),

			#endregion

			#region //// -> EitherDigit

			new SubjectNode(
				SubjectDigit.EitherDigit,
				(time) => new int[] { time % 10, time / 10 },
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					// Hacky weighting
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit1Or7.WithAdditionalDigit(),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit()
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					// Hacky weighting
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.AdditionalOnlySevenOrEight
				)

			),

			#endregion

			#region //// -> NeitherDigit

			new SubjectNode(
				SubjectDigit.NeitherDigit,
				(time) => new int[] { time % 10, time / 10 },
				BoolOps.AllFalse,

				CommonNodes.Is.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit0.WithAdditionalDigit(AdditionalDigitReqirement.ThreeThroughSeven),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrGreater),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.SevenOrEight)
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.AdditionalOnlyThreeThroughSeven
				)

			),

			#endregion

			#region //// -> DigitsProduct

			new SubjectNode(
				SubjectDigit.DigitsProduct,
				(time) => ((time % 10)*(time / 10)).Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.EitherDigit,
					CommonNodes.UnitsDigit,
					CommonNodes.TensDigit
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Digit0
				),

				CommonNodes.Contains.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.TwoOrThree),
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6
				),

				CommonNodes.DoesNotContain.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit0,
					CommonNodes.Digit0.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.AdditionalOnlySevenOrEight
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyNotZero
				),

				CommonNodes.BeginsWith.WithObjects(
					CommonNodes.AdditionalOnlyOne
				),

				CommonNodes.EndsWith.WithObjects(
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.NotZero)
				)

			),

			#endregion

			#region //// -> DigitsDifference

			new SubjectNode(
				SubjectDigit.DigitsDifference,
				(time) => System.Math.Abs((time % 10) - (time / 10)).Yield(),
				BoolOps.AnyTrue,

				/*
				key: abs difference, value: number of occurences in 00..79
				┌──────┬───────────┬────────────────────────┐
				│ Name │ Value     │ Type                   │
				├──────┼───────────┼────────────────────────┤
				│ [0]  │ { 0, 8 }  │ KeyValuePair<int, int> │
				│ [1]  │ { 1, 15 } │ KeyValuePair<int, int> │
				│ [2]  │ { 2, 14 } │ KeyValuePair<int, int> │
				│ [3]  │ { 3, 12 } │ KeyValuePair<int, int> │
				│ [4]  │ { 4, 10 } │ KeyValuePair<int, int> │
				│ [5]  │ { 5, 8 }  │ KeyValuePair<int, int> │
				│ [6]  │ { 6, 6 }  │ KeyValuePair<int, int> │
				│ [7]  │ { 7, 4 }  │ KeyValuePair<int, int> │
				│ [8]  │ { 8, 2 }  │ KeyValuePair<int, int> │
				│ [9]  │ { 9, 1 }  │ KeyValuePair<int, int> │
				└──────┴───────────┴────────────────────────┘
				*/

				CommonNodes.Is.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit4Or6,
					CommonNodes.Prime,
					CommonNodes.EitherDigit,
					CommonNodes.AdditionalOnlyThreeOrLess
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.ThreeOrLess),
					CommonNodes.Prime
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.Digit2Or8,
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.UnitsDigit,
					CommonNodes.TensDigit,
					// Hacky weighting
					CommonNodes.AdditionalOnlyTwoOrThree,
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.UnitsDigit,
					// Hacky weighting
					CommonNodes.AdditionalOnlyTwoOrThree,
					CommonNodes.AdditionalOnlyTwoOrThree
				)

			),

			#endregion

			#region //// -> DigitsSum

			new SubjectNode(
				SubjectDigit.DigitsSum,
				(time) => ((time % 10) + (time / 10)).Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Prime,
					CommonNodes.EitherDigit,
					CommonNodes.Digit4Or6,
					CommonNodes.Digit2Or8,
					CommonNodes.AdditionalOnlySevenOrEight
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit1Or7.WithAdditionalDigit(),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit()
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyTwoOrThree,
					CommonNodes.AdditionalOnlySevenOrEight,
					CommonNodes.AdditionalOnlyThreeThroughSeven
				),

				CommonNodes.Contains.WithObjects(
					CommonNodes.Digit0.WithAdditionalDigit(AdditionalDigitReqirement.One),
					CommonNodes.Digit1Or7,
					CommonNodes.AdditionalOnlyOne
				),

				CommonNodes.DoesNotContain.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.One),
					CommonNodes.Digit4Or6.WithAdditionalDigit(AdditionalDigitReqirement.One)
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyTwoOrThree
				),

				CommonNodes.IsGreaterThan.WithObjects(
					CommonNodes.AdditionalOnlySevenOrEight
				),

				CommonNodes.IsLessThan.WithObjects(
					CommonNodes.AdditionalOnlyThreeThroughSeven
				),

				CommonNodes.BeginsWith.WithObjects(
					CommonNodes.Digit1Or7,
					CommonNodes.AdditionalOnlyOne
				),

				CommonNodes.EndsWith.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit4Or6,
					CommonNodes.Digit2Or8
				)

			),

			#endregion

			#region //// -> Display

			new SubjectNode(
				SubjectDigit.Display,
				(time) => time.Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero)
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero)
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Prime
				),

				CommonNodes.ContainsWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					// Hacky weighting
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.DoesNotContainWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit()
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					CommonNodes.TensDigit
				),

				CommonNodes.BeginsWithWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.One),
					CommonNodes.UnitsDigit,
					CommonNodes.UnitsDigit.WithAdditionalDigit(AdditionalDigitReqirement.One)
				),

				CommonNodes.EndsWith.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit0.WithAdditionalDigit(),
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.TensDigit,
					CommonNodes.AdditionalOnlyAny
				)

			),

			#endregion

			#region //// -> DisplayReadBackwards

			new SubjectNode(
				SubjectDigit.DisplayReadBackwards,
				(time) => ((time % 10)*10 + time/10).Yield(),
				BoolOps.AnyTrue,

				CommonNodes.Is.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero)
				),

				CommonNodes.IsNot.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero)
				),

				CommonNodes.Is1Away.WithObjects(
					CommonNodes.Prime
				),

				CommonNodes.ContainsWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					// Hacky weighting
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny,
					CommonNodes.AdditionalOnlyAny
				),

				CommonNodes.DoesNotContainWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Prime.WithAdditionalDigit(),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.NotZero),
					CommonNodes.Digit2Or8.WithAdditionalDigit(),
					CommonNodes.Digit4Or6.WithAdditionalDigit()
				),

				CommonNodes.IsDivisbleBy.WithObjects(
					CommonNodes.Digit4Or6,
					CommonNodes.AdditionalOnlyThreeThroughSeven,
					CommonNodes.UnitsDigit
				),

				CommonNodes.EndsWith.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Prime.WithAdditionalDigit(AdditionalDigitReqirement.Zero),
					CommonNodes.Digit1Or7.WithAdditionalDigit(AdditionalDigitReqirement.Any),
					CommonNodes.Digit2Or8.WithAdditionalDigit(AdditionalDigitReqirement.One),
					CommonNodes.UnitsDigit,
					CommonNodes.UnitsDigit.WithAdditionalDigit(AdditionalDigitReqirement.One)
				),

				CommonNodes.BeginsWithWithSubjectWithLeadingZero.WithObjects(
					CommonNodes.Prime,
					CommonNodes.Digit0.WithAdditionalDigit(),
					CommonNodes.Digit0,
					CommonNodes.Digit1Or7,
					CommonNodes.Digit2Or8,
					CommonNodes.Digit4Or6,
					CommonNodes.TensDigit,
					CommonNodes.AdditionalOnlyAny
				)

			),

			#endregion

		};

		#endregion

		/// <summary>
		/// Generates a rule for the release of the timer button.
		/// At least one release time will be valid.
		/// </summary>
		public static void GenerateReleaseRule(System.Random rng, out int?[] preferredDigits, out int[] validReleaseTimes) {

			// Pick one rule form the tree
			var subjectNode = ruleTree.PickRandom(rng);
			var verbNode = subjectNode.PossibleVerbs.PickRandom(rng);
			var objectNode = verbNode.PossibleObjects.PickRandom(rng);
			int? additionalDigit;

			// Generate additional digit
			switch (objectNode.AdditionalDigit) {

				case AdditionalDigitReqirement.None:
					additionalDigit = null;
					break;

				case AdditionalDigitReqirement.Any:
					additionalDigit = rng.Next(10);
					break;

				case AdditionalDigitReqirement.Zero:
					additionalDigit = 0;
					break;

				case AdditionalDigitReqirement.One:
					additionalDigit = 1;
					break;

				case AdditionalDigitReqirement.TwoOrGreater:
					additionalDigit = rng.Next(2, 10);
					break;

				case AdditionalDigitReqirement.TwoOrThree:
					additionalDigit = rng.Next(2, 4);
					break;

				case AdditionalDigitReqirement.ThreeOrLess:
					additionalDigit = rng.Next(4);
					break;

				case AdditionalDigitReqirement.SevenOrEight:
					additionalDigit = rng.Next(7, 9);
					break;

				case AdditionalDigitReqirement.ThreeThroughSeven:
					additionalDigit = rng.Next(3, 8);
					break;

				case AdditionalDigitReqirement.NotZero:
					additionalDigit = rng.Next(1, 10);
					break;

				case AdditionalDigitReqirement.NotNine:
					additionalDigit = rng.Next(0, 9);
					break;

				default:
					additionalDigit = null;
					break;
			}

			// Hard override additional digit if it overlaps with literal objects
			switch (objectNode.DisplayDigit) {

				case ObjectDigit.Digit0:
					if (additionalDigit == 0) additionalDigit = null;
					break;

				case ObjectDigit.Digit1Or7:
					if (additionalDigit == 1 || additionalDigit == 7) additionalDigit = null;
					break;

				case ObjectDigit.Digit2Or8:
					if (additionalDigit == 2 || additionalDigit == 8) additionalDigit = null;
					break;

				case ObjectDigit.Digit4Or6:
					if (additionalDigit == 4 || additionalDigit == 6) additionalDigit = null;
					break;
				
			}

			// Compile preferred digits
			preferredDigits = new int?[4];
			preferredDigits[0] = (int)subjectNode.DisplayDigit;
			preferredDigits[1] = (int)verbNode.DisplayDigit;
			preferredDigits[2] = (int)objectNode.DisplayDigit;
			preferredDigits[3] = additionalDigit.HasValue ? additionalDigitToDisplayDigit[additionalDigit.Value] : (int?)null;

			// Create validation pipeline
			System.Func<int, bool> ruleValidator = ReleaseRuleCreateValidatorDelegate(
				subjectNode,
				verbNode,
				objectNode,
				additionalDigit
			);

			// Validate all times
			validReleaseTimes = Enumerable.Range(0, 100).Where(ruleValidator).ToArray();

			// Just in case fail safe
			// to prevent softlocks
			if (validReleaseTimes.Length == 0) validReleaseTimes = Enumerable.Range(0, 100).ToArray();

		}

		static System.Func<int, bool> ReleaseRuleCreateValidatorDelegate(
			SubjectNode subjectNode,
			VerbNode verbNode,
			ObjectNode objectNode,
			int? additionalDigit
		) {

			return (time) => {

				IEnumerable<int> subjects = subjectNode.ValueGenerator(time);
				IEnumerable<int> objects = objectNode.ValueGenerator(time);

				if (additionalDigit.HasValue) {
					objects = objects.Append(additionalDigit.Value);
				}

				IEnumerable<bool> perSubjectResults =
									subjects
									.Select(
										sub => {
											var perObjectResults = objects.Select(obj => verbNode.Validator(sub, obj));
											return verbNode.ValidatorCombiner(perObjectResults);
										}
									);

				return subjectNode.ValidatorCombiner(perSubjectResults);

			};

		}

		#region //// Testing

		/// <summary>
		/// Editor only.
		/// Tests all rules to make sure no rule is impossible.
		/// </summary>
		public static void TestAllRules() {

			var logger = new FlowerButtonMod.Utils.ModuleLogger("FLOWER BUTTON DEBUG", null);
			logger.LogString("Starting the test of all possible rules");

			bool allPassing = true;
			List<int> validTimeCounts = new List<int>();
			List<int> validTimeCountsUnderThirty = new List<int>();

			// For each possible node combination excluding additional digit
			foreach (var subjectNode in ruleTree)
			foreach (var verbNode in subjectNode.PossibleVerbs)
			foreach (var objectNode in verbNode.PossibleObjects) {

				// Generate additional digits
				IEnumerable<int?> additionalDigits = (null as int?).Yield();

				switch (objectNode.AdditionalDigit) {

					case AdditionalDigitReqirement.None:
						additionalDigits = (null as int?).Yield();
						break;

					case AdditionalDigitReqirement.Any:
						additionalDigits = Enumerable.Range(0, 10).Select(n => n as int?);
						break;

					case AdditionalDigitReqirement.Zero:
						additionalDigits = (0 as int?).Yield();
						break;

					case AdditionalDigitReqirement.One:
						additionalDigits = (1 as int?).Yield();
						break;

					case AdditionalDigitReqirement.TwoOrGreater:
						additionalDigits = Enumerable.Range(2, 10 - 2).Select(n => n as int?);
						break;

					case AdditionalDigitReqirement.TwoOrThree:
						additionalDigits = new int?[] { 2, 3 };
						break;

					case AdditionalDigitReqirement.ThreeOrLess:
						additionalDigits = Enumerable.Range(0, 4).Select(n => n as int?);
						break;

					case AdditionalDigitReqirement.SevenOrEight:
						additionalDigits = new int?[] { 7, 8 };
						break;

					case AdditionalDigitReqirement.ThreeThroughSeven:
						additionalDigits = new int?[] { 3, 4, 5, 6, 7 };
						break;

					case AdditionalDigitReqirement.NotZero:
						additionalDigits = Enumerable.Range(1, 10 - 1).Select(n => n as int?);
						break;

					case AdditionalDigitReqirement.NotNine:
						additionalDigits = Enumerable.Range(0, 10 - 1).Select(n => n as int?);
						break;

					default:
						additionalDigits = (null as int?).Yield();
						break;
				}

				// For all additional digits
				foreach (int? additionalDigitItValue in additionalDigits) {

					// Copy iterator value because iteration value is not writable
					int? additionalDigit = additionalDigitItValue;

					// Hard override additional digit if it overlaps with literal objects
					switch (objectNode.DisplayDigit) {

						case ObjectDigit.Digit0:
							if (additionalDigit == 0) additionalDigit = null;
							break;

						case ObjectDigit.Digit1Or7:
							if (additionalDigit == 1 || additionalDigit == 7) additionalDigit = null;
							break;

						case ObjectDigit.Digit2Or8:
							if (additionalDigit == 2 || additionalDigit == 8) additionalDigit = null;
							break;

						case ObjectDigit.Digit4Or6:
							if (additionalDigit == 4 || additionalDigit == 6) additionalDigit = null;
							break;

					}

					// Create validation pipeline
					System.Func<int, bool> ruleValidator = ReleaseRuleCreateValidatorDelegate(
						subjectNode,
						verbNode,
						objectNode,
						additionalDigit
					);

					// Get all valid release times
					IEnumerable<int> allTimes = Enumerable.Range(0, 81);
					int[] validReleaseTimes = allTimes.Where(ruleValidator).ToArray();
					validTimeCounts.Add(validReleaseTimes.Length);
					validTimeCountsUnderThirty.Add(validReleaseTimes.Where(t => t < 30).Count());

					// Check
					string currentCombinationLog = $"{subjectNode.DisplayDigit} {verbNode.DisplayDigit} {objectNode.DisplayDigit}";
					if (additionalDigit.HasValue) currentCombinationLog += $" (or) {additionalDigit.Value}";

					if (validReleaseTimes.Min() > 20) {
						logger.LogStringFormat("ISSUE: {0} has min release time of over 20", currentCombinationLog);
						allPassing = false;
					}

					if (validReleaseTimes.Where(time => time < 65).Max() < 19) {
						logger.LogStringFormat("ISSUE: {0} has a max release of under 19 among those under 65", currentCombinationLog);
						allPassing = false;
					}

					if (validReleaseTimes.Length == 0) {
						logger.LogStringFormat("ISSUE: {0} has no valid times", currentCombinationLog);
						allPassing = false;
					}

					if (allTimes.Except(validReleaseTimes).Count() == 0) {
						logger.LogStringFormat("ISSUE: {0} has all times valid", currentCombinationLog);
						allPassing = false;
					}

					/*
					if (validReleaseTimes.Length > 50) {
						logger.LogStringFormat("ISSUE: {0} is too easy", currentCombinationLog);
						allPassing = false;
					}
					*/

				}
			}

			if (allPassing) logger.LogString("All passed!");
			logger.LogStringFormat("Average realse times count: {0}", validTimeCounts.Average());
			logger.LogStringFormat("Average realse times count (29 ticks or less): {0}", validTimeCountsUnderThirty.Average());

			// End of TestAllRules
			return;
		}

		#endregion

	}

}
