using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Rephidock.GeneralUtilities.Maths;
using Rephidock.GeneralUtilities.Randomness;
using Rephidock.GeneralUtilities.Collections;
using Rephidock.AtomicAnimations;
using Rephidock.AtomicAnimations.Coroutines;

using FlowerButtonMod.Utils;
using FlowerButtonMod.FlowerButton.Settings;

using TimeSpan = System.TimeSpan;


namespace FlowerButtonMod.FlowerButton {

	[RequireComponent(typeof(KMBombModule))]
	[RequireComponent(typeof(KMBombInfo))]
	[RequireComponent(typeof(KMAudio))]
	public class FlowerButtonModule : MonoBehaviour {

		#region //// State

		private enum State {
			AwaitingActivation,
			ReadyForHold,
			WindingUp,
			Held,
			SolutionCheckAnimation,
			Striking,
			SolvedRestoringTime, // Hacky way to stop overriding timer
			Solved
		}

		State state;

		// Set by Tweaks via reflection
		const int ZenModeStrikeCount = 3;
		public bool TimeModeActive;
		public bool ZenModeActive;

		// Stuff for solution
		int[] validReleaseTimes = null;
		internal SappedDisplayGenerator timerDisplayGenerator = null; // Contains preffered digits
		int chosenReleaseTime = -1;

		#endregion

		#region //// Countdown

		const string CountdownTextAwaitingLights = "  ";
		const string CountdownTextAwaitingHold = "__";
		const string CountdownTextSolved = "ΞΞ";
		const string CountdownTextError = "Er";

		KMAudio.KMAudioRef musicBoxSoundRef = null;

		const int musicBoxTotalNotes = 160;
		const double musicBoxNoteSpeed = 2.45398; // notes per real time seconds

		TimeSpan musicBoxTimeLeft; // in notes

		void UpdateModuleCountdown(float unscaledDeltaTime) {

			// Time flow
			TimeSpan musicBoxTimeLeftPrevious = musicBoxTimeLeft;
			musicBoxTimeLeft -= TimeSpan.FromSeconds(unscaledDeltaTime * musicBoxNoteSpeed);

			// Time out
			if (musicBoxTimeLeft.TotalSeconds <= 0) {
				musicBoxTimeLeft = TimeSpan.Zero;

				state = State.SolutionCheckAnimation;
				musicBoxSoundRef = null;
				animationRunner.Run(TimeRanOutRoutineRoutine().ToAnimation());
				return;
			}

			int ticksLeft = (int)System.Math.Floor(musicBoxTimeLeft.TotalSeconds);
			int ticksLeftPrevious = (int)System.Math.Floor(musicBoxTimeLeftPrevious.TotalSeconds);

			// Tick
			if (ticksLeft != ticksLeftPrevious) {
				timerDisplayGenerator?.TickDisplay();
			}
			
			// Display
			countdownText.text = GetCountdownDisplayNumber().ToString("D2");
		}

		int GetCountdownDisplayNumber() {
			if (musicBoxTimeLeft.TotalSeconds < 1) return 0;
			return (int)System.Math.Floor((musicBoxTimeLeft.TotalSeconds - 1) / 2);
		}

		void SetNotesLeftFromDisplay() {
			int? countdownNumber = countdownText.text.TryParseInt();
			if (!countdownNumber.HasValue) return;
			musicBoxTimeLeft = TimeSpan.FromSeconds(countdownNumber.Value * 2 + 1);
		}

		void StopMusicBox() {
			musicBoxSoundRef?.StopSound();
			musicBoxSoundRef = null;
		}

		#endregion

		#region //// Penalty

		TimeSpan penaltyTimeLeft = TimeSpan.Zero;
		const double penaltyTimeScale = 0.75;

		const int humanReleaseTimeThreshold = 50;
		const int minPenaltyMaxAt = 30;
		readonly static TimeSpan maxPenaltyTime = TimeSpan.FromSeconds(60);

		public TimeSpan GetPenaltyDeltaTime(float deltaTime) {

			// No penalty
			if (penaltyTimeLeft <= TimeSpan.Zero) return TimeSpan.Zero;

			TimeSpan penaltyDeltaTime = TimeSpan.FromSeconds(deltaTime * penaltyTimeScale);
			
			// Final delta time of penalty
			if (penaltyDeltaTime >= penaltyTimeLeft) {
				penaltyDeltaTime = penaltyTimeLeft;
				penaltyTimeLeft = TimeSpan.Zero;
				return penaltyDeltaTime;
			}

			// Count penalty down
			penaltyTimeLeft -= penaltyDeltaTime;
			return penaltyDeltaTime;
		}

		public void CalculateAndSetPenalty(int releaseTime, out int minPenaltyAt) {

			// Find the start of the penalties:
			// The highest human release time
			// or 30, whichever is lower.
			// Penalty is 0 at this time.
			minPenaltyAt = validReleaseTimes
							.Where(time => time <= humanReleaseTimeThreshold)
							.OrderByDescending(t => t)
							.FirstOrDefault();

			if (minPenaltyAt > minPenaltyMaxAt) minPenaltyAt = minPenaltyMaxAt;

			// If not in penalty range do nothing
			if (releaseTime >= minPenaltyAt) return;

			// Calculate penalty
			double penaltyProgress = 1 - (releaseTime / minPenaltyAt);
			penaltyTimeLeft = TimeSpan.FromSeconds(MoreMath.Lerp(0, maxPenaltyTime.TotalSeconds, penaltyProgress));
		}

		#endregion

		#region //// Components and parts

		// Passed in through inspector
		public FakeStatusLight fakeStausLightOriginal;
		public Material cameraDistortionEffectMaterialOriginal;

		// Found or Created
		internal System.Random rng;

		internal KMBombModule kmModule;
		internal KMBombInfo kmBomb;
		internal KMAudio kmAudio;

		internal FakeStatusLight statusLightProxy;

		internal BombTimerSapper timerSapper;
		internal CameraDistortionManager distortionManager;

		internal KMSelectable buttonSelectable;
		internal Transform buttonFlowerTransform;
		internal TextMesh countdownText;
		internal Light countdownLight;

		internal ModuleLogger logger;

		internal AnimationRunner animationRunner;

		internal FlowerButtonSettings settings;

		private void PrepareComponents() {

			// KM
			kmModule = GetComponent<KMBombModule>();
			kmBomb = GetComponent<KMBombInfo>();
			kmAudio = GetComponent<KMAudio>();

			kmBomb.OnBombExploded += OnBombExploded;
			kmBomb.OnBombSolved += OnBombSolved;
			kmModule.OnActivate += OnActivate;

			// Fake stauts light
			statusLightProxy = Instantiate(fakeStausLightOriginal, transform);
			statusLightProxy.GetStatusLights(transform);
			statusLightProxy.Module = kmModule;

			// Parts
			timerSapper = new BombTimerSapper(kmModule);
			distortionManager = new CameraDistortionManager(cameraDistortionEffectMaterialOriginal);

			timerSapper.OnSapError += OnSapError;
			timerSapper.OnSubtractError += OnPenaltyError;

			// Button
			var buttonTransform = transform.Find("button");
			buttonSelectable = buttonTransform.GetComponent<KMSelectable>();
			buttonFlowerTransform = buttonTransform.Find("flower");

			buttonSelectable.OnInteract += () => { OnButtonHold(); return false; };
			buttonSelectable.OnInteractEnded += OnButtonRelease;

			// Countdown
			var countdownTransform = transform.Find("countdown");
			countdownText = countdownTransform.Find("text").GetComponent<TextMesh>();
			countdownLight = countdownTransform.Find("light").GetComponent<Light>();

			countdownText.text = CountdownTextAwaitingLights;
			countdownLight.range *= transform.lossyScale.x;

			// Misc.
			logger = new ModuleLogger(kmModule);
			rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
			animationRunner = new AnimationRunner();
			settings = SettingsReader.ReadSettings();

		}

		#endregion

		#region //// Events

		void Start() {

			// Init
			PrepareComponents();
			state = State.AwaitingActivation;

			// Run the test
			#if UNITY_EDITOR
			ReleaseRuleGenerator.TestAllRules();
			#endif

		}

		void OnActivate() {

			// Allow logic
			state = State.ReadyForHold;
			countdownText.text = CountdownTextAwaitingHold;
		}

		void Update() {

			// Update animations (independant of time scale)
			animationRunner?.Update(TimeSpan.FromSeconds(Time.unscaledDeltaTime));

			// Update shader
			distortionManager.AddTime(Time.unscaledDeltaTime);

			// Update module countdown
			if (state == State.Held) UpdateModuleCountdown(Time.unscaledDeltaTime);

			// Deliver penalty
			if (penaltyTimeLeft > TimeSpan.Zero && !isAnyButtonMaupulatingTime) {
				TimeSpan penaltyDeltaTime = GetPenaltyDeltaTime(Time.deltaTime);

				// Zen mode: add time instead
				if (ZenModeActive) penaltyDeltaTime = -penaltyDeltaTime;
				timerSapper.SubtractTime(penaltyDeltaTime);
			}

		}

		void LateUpdate() {

			// Override bomb's timer display
			if (state == State.Held || state == State.SolutionCheckAnimation) {
				if (timerDisplayGenerator == null) return;
				timerSapper.SapBombTimer(timerDisplayGenerator.DisplayOverride);
			}

		}

		void OnDestroy() {
			animationRunner?.Dispose();
			distortionManager.RemoveDistortionFromCamera();
			RestoreTime();
			StopMusicBox();
		}

		void OnBombExploded() {
			distortionManager.RemoveDistortionFromCamera();
			RestoreTime();
			StopMusicBox();
			penaltyTimeLeft = TimeSpan.Zero;
		}

		void OnBombSolved() {
			penaltyTimeLeft = TimeSpan.Zero;
		}

		void OnSapError(System.Exception ex) {
			logger.LogException(ex);
			logger.LogString("Could not sap timer display. Triggering failsafe. Module solved.");
			countdownText.text = CountdownTextError;

			state = State.Solved;
			statusLightProxy.HandlePass();

			RestoreTime();
			distortionManager.RemoveDistortionFromCamera();
			StopMusicBox();
			timerSapper.UnsapBombTimer();
		}

		void OnPenaltyError(System.Exception ex) {
			logger.LogException(ex);
			logger.LogString("Could not deliver penalty. Abolishing penalty.");
			penaltyTimeLeft = TimeSpan.Zero;
		}

		#endregion

		#region //// Button

		bool isButtonPhysicallyHeldCurrently = false;

		const float buttonFlowerOffsetYWhenHeld = -0.1f;
		readonly static TimeSpan buttonFlowerTravelTime = TimeSpan.FromSeconds(0.075);

		Shift1D CreateButtonPressMovemnet() {
			return new Shift1D(
					buttonFlowerOffsetYWhenHeld,
					buttonFlowerTravelTime,
					Easing.Linear,
					(yy) => {
						var position = buttonFlowerTransform.localPosition;
						position.y += yy;
						buttonFlowerTransform.localPosition = position;
					}
			);
		}

		Shift1D CreateButtonReleaseMovemnet() {
			return new Shift1D(
					-buttonFlowerOffsetYWhenHeld,
					buttonFlowerTravelTime,
					Easing.Linear,
					(yy) => {
						var position = buttonFlowerTransform.localPosition;
						position.y += yy;
						buttonFlowerTransform.localPosition = position;
					}
			);
		}

		void OnButtonHold() {

			// Ensure the button can only be pressed if it is not held
			if (isButtonPhysicallyHeldCurrently) return;
			isButtonPhysicallyHeldCurrently = true;

			// Cue
			//buttonSelectable.AddInteractionPunch(0.5f);
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, buttonFlowerTransform);
			animationRunner.Run(CreateButtonPressMovemnet());

			// Logic Guard
			if (state != State.ReadyForHold) return;

			// Begin hold
			logger.LogString("Holding the button...");
			
			if (!TrySlowTime()) {
				logger.LogString("Another flower button is already held. Ignoring hold logic. Please hold this button later.");
				return;
			}

			// Play sound
			kmAudio.PlaySoundAtTransform(SoundCatalogue.Press, buttonFlowerTransform);

			// Generate the rule
			int?[] preferredDigits;
			ReleaseRuleGenerator.GenerateReleaseRule(rng, out preferredDigits, out validReleaseTimes);
			timerDisplayGenerator = new SappedDisplayGenerator(preferredDigits, rng);

			logger.LogStringFormat(
				"Preferred digits: " +
				preferredDigits.Select(d => d == null ? '#' : (char)('0' + d)).JoinString().Insert(2, ":")
			);

			logger.LogStringFormat(
				"Valid release times: " +
				validReleaseTimes.Select(t => t.ToString("D2")).JoinString(", ")
			);

			// Setup countdown
			musicBoxTimeLeft = TimeSpan.FromSeconds(musicBoxTotalNotes + 1);

			// "Solve" the module
			statusLightProxy.SetPass();

			// Wind-up
			state = State.WindingUp;
			animationRunner.Run(CountdownWindUpRoutine().ToAnimation());

		}

		void OnButtonRelease() {

			// Ensure the button can only be released if it is held
			if (!isButtonPhysicallyHeldCurrently) return;
			isButtonPhysicallyHeldCurrently = false;

			// Cue
			//buttonSelectable.AddInteractionPunch(0.5f);
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, buttonFlowerTransform);
			animationRunner.Run(CreateButtonReleaseMovemnet());

			// Released on windup
			if (state == State.WindingUp) {
				logger.LogString("Button released during wind-up.");
				// Do the same thing as when released normally
				state = State.Held;
				// But with time visible on display
				SetNotesLeftFromDisplay();
			}

			// Logic guard
			if (state != State.Held) return;

			// Log
			logger.LogString("Button released.");

			// State change + check solution
			StopMusicBox();
			state = State.SolutionCheckAnimation;
			animationRunner.Run(SolutionCheckRoutine().ToAnimation());

		}

		#endregion

		#region //// Time manipulation

		// Prevent multiple buttons from being held
		private static readonly object gameTimeManipulationLock = new object();
		private static bool isAnyButtonMaupulatingTime = false;

		const float slowedTimeScale = 0.001f;

		/// <returns>true on success, false if time is manipulated by some other button.</returns>
		bool TrySlowTime() {

			// Check if allowed to maniulate time
			lock (gameTimeManipulationLock) {
				if (isAnyButtonMaupulatingTime) return false;

				isAnyButtonMaupulatingTime = true;
			}

			Time.timeScale = slowedTimeScale;

			return true;
		}

		void RestoreTime() {
			lock (gameTimeManipulationLock) {
				if (!isAnyButtonMaupulatingTime) return;
				isAnyButtonMaupulatingTime = false;
				Time.timeScale = 1f;
			}
		}

		#endregion

		#region //// Routines

		readonly static TimeSpan infiniteStrikeDelta = TimeSpan.FromSeconds(0.5);
		readonly static TimeSpan strikeFlashDurationTime = TimeSpan.FromSeconds(1); // from the game

		/// <summary>Coroutine: Strikes infinitely or 3 times for zen mode.</summary>
		IEnumerable<CoroutineYield> InfiniteStrikingRoutine() {

			const float strikePunch = 0.2f;

			System.Action helper_ResetToPreHold = () => {
				state = State.ReadyForHold;
				timerDisplayGenerator = null;
				validReleaseTimes = null;

				statusLightProxy.SetInActive();
				countdownText.text = CountdownTextAwaitingHold;

				StopMusicBox();
			};

			// Disabled forced detonation: reset
			if (settings.disableForcedDetonation) {
				logger.LogString("Forced detonation disabled. Striking once and resetting.");
				statusLightProxy.HandleStrike();
				buttonSelectable.AddInteractionPunch(strikePunch);

				// Hide display
				countdownText.text = CountdownTextAwaitingLights;

				// Wait for distortion and strike to go away before resetting
				yield return CoroutineYield.Sleep(distortionDisapperanceDuration);
				yield return CoroutineYield.Sleep(strikeFlashDurationTime - distortionDisapperanceDuration);
				helper_ResetToPreHold();
				yield break;
			}


			logger.LogString("Goodbye.");

			for (int i = 0; i < ZenModeStrikeCount; i++) {
				statusLightProxy.HandleStrike();
				buttonSelectable.AddInteractionPunch(strikePunch);
				countdownText.text = validReleaseTimes.PickRandom(rng).ToString("D2");
				yield return CoroutineYield.Sleep(infiniteStrikeDelta);
			}

			// Stop striking for zen and reset
			if (ZenModeActive || TimeModeActive) {
				logger.LogString("Zen mode or Time mode detected. Resetting.");

				// Wait for strike to go away
				countdownText.text = CountdownTextAwaitingLights;
				yield return CoroutineYield.Sleep(strikeFlashDurationTime - infiniteStrikeDelta);

				helper_ResetToPreHold();
				yield break;
			}

			do {
				statusLightProxy.HandleStrike();
				buttonSelectable.AddInteractionPunch(strikePunch);
				countdownText.text = validReleaseTimes.PickRandom(rng).ToString("D2");
				yield return CoroutineYield.Sleep(infiniteStrikeDelta);
			} while (true);
		}

		readonly static TimeSpan windUpCountdownWindDuration = TimeSpan.FromSeconds(1.5);
		readonly static EasingCurve windUpCountdownWindEasing = Easing.CubicOut;
		readonly static TimeSpan windUpPostWindUpWait = TimeSpan.FromSeconds(0.75);

		readonly static TimeSpan windUpDistortionApperanceDuration = TimeSpan.FromSeconds(0.5);
		readonly static EasingCurve windUpDistortionApperanceEasing = Easing.QuadOut;

		/// <summary>Coroutine: Winds up the timer, starts music box, sets state.</summary>
		IEnumerable<CoroutineYield> CountdownWindUpRoutine() {

			// Begin camera distortion
			if (!settings.disableVisualDistortion) {
				distortionManager.AddDistortionToCamera();
				yield return new Move1D(
					0, distortionManager.DefaltMaxDistortionStrengthX,
					windUpDistortionApperanceDuration,
					windUpDistortionApperanceEasing,
					distortionManager.SetDistortionStrengthX
				);
				yield return new Move1D(
					0, distortionManager.DefaltMaxTintStrength,
					windUpDistortionApperanceDuration,
					windUpDistortionApperanceEasing,
					distortionManager.SetTintStrength
				);
			}

			// Wind up
			int windUpTarget = GetCountdownDisplayNumber();

			yield return new Move1D(
				0,
				windUpTarget,
				windUpCountdownWindDuration,
				windUpCountdownWindEasing,
				(newValue) => {
					if (state != State.WindingUp) return; // releasing button changes state
					countdownText.text = ((int)System.Math.Floor(newValue)).ToString("D2");
				}
			);
			yield return CoroutineYield.WaitPrevious;
			if (state != State.WindingUp) yield break; // releasing button changes state

			countdownText.text = windUpTarget.ToString("D2");

			// Wait after wind up
			yield return CoroutineYield.Sleep(windUpPostWindUpWait);
			if (state != State.WindingUp) yield break; // releasing button changes state

			// Play music box
			if (!settings.disableMusicbox) {
				musicBoxSoundRef = kmAudio.PlaySoundAtTransformWithRefNoLoop(SoundCatalogue.MusicBoxMusic, transform);
			}

			// Init first display override
			timerDisplayGenerator.TickDisplay();

			// Begin countdown
			countdownText.text = GetCountdownDisplayNumber().ToString("D2");
			state = State.Held;
		}

		readonly static TimeSpan distortionDisapperanceDuration = TimeSpan.FromSeconds(0.25);
		readonly static EasingCurve distortionDisapperanceEasing = Easing.QuadInOut;

		IEnumerable<CoroutineYield> EndDistortionRoutine() {
			yield return new Move1D(
				distortionManager.DefaltMaxDistortionStrengthX, 0,
				distortionDisapperanceDuration,
				distortionDisapperanceEasing,
				distortionManager.SetDistortionStrengthX
			);
			yield return new Move1D(
				distortionManager.DefaltMaxTintStrength, 0,
				distortionDisapperanceDuration,
				distortionDisapperanceEasing,
				distortionManager.SetTintStrength
			);
			yield return CoroutineYield.Sleep(distortionDisapperanceDuration);
			distortionManager.RemoveDistortionFromCamera();
		}

		readonly static TimeSpan[] solutionCheckTickTimestamps = new TimeSpan[] {
			TimeSpan.FromSeconds(0.00),
			TimeSpan.FromSeconds(0.25),
			TimeSpan.FromSeconds(0.35),
			TimeSpan.FromSeconds(0.45),
			TimeSpan.FromSeconds(1.50), //index 4
			TimeSpan.FromSeconds(1.60),
			TimeSpan.FromSeconds(1.75),
			TimeSpan.FromSeconds(1.85),
			TimeSpan.FromSeconds(2.15),
			TimeSpan.FromSeconds(2.95), //index 9
			TimeSpan.FromSeconds(3.80),
			TimeSpan.FromSeconds(4.05),
			TimeSpan.FromSeconds(4.30),
			TimeSpan.FromSeconds(4.50),
			TimeSpan.FromSeconds(4.70),
			TimeSpan.FromSeconds(4.75),
			TimeSpan.FromSeconds(4.80),
			TimeSpan.FromSeconds(4.85),
			TimeSpan.FromSeconds(4.90),
			TimeSpan.FromSeconds(4.95),
			TimeSpan.FromSeconds(5.00),
			TimeSpan.FromSeconds(5.05),
			TimeSpan.FromSeconds(5.10),
			TimeSpan.FromSeconds(5.15),
		};


		readonly static TimeSpan[] solutionCheckDistortionBoostDurations = {
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1.5),
		};

		readonly static EasingCurve[] solutionCheckDistortionBoostEasings = {
			Easing.QuadOut,
			Easing.QuadOut,
			Easing.QuadOut,
			Easing.CubicOut,
		};

		readonly static float[] solutionCheckDistortionBoostTimes = {
			5,
			9,
			5,
			10,
		};

		readonly static TimeSpan solutionCheckSecondDistortionTimestamp = solutionCheckTickTimestamps[4];
		readonly static TimeSpan solutionCheckStatusLightOffTimestamp = solutionCheckTickTimestamps[9];
		readonly static TimeSpan solutionCheckSuspenseEndTimestamp = TimeSpan.FromSeconds(5.15);
		readonly static TimeSpan solutionCheckRestoreTimeTimestamp = TimeSpan.FromSeconds(6.40);

		readonly static TimeSpan solutionCheckOopsDuration = TimeSpan.FromSeconds(0.55);

		const int solutionCheckPreferredDigitsNullTicks = 60;

		/// <summary>
		/// Coroutine: 
		/// Plays the solution check animation, 
		/// checks if the time is correct
		/// resets time and status light, 
		/// sets correct state and maybe starts the strike routine
		/// </summary>
		IEnumerable<CoroutineYield> SolutionCheckRoutine() {

			// Sound
			var releaseSoundRef = kmAudio.PlaySoundAtTransformWithRefNoLoop(SoundCatalogue.Release, buttonFlowerTransform);

			// Suspense ticks
			yield return
				// Ticks in a seprate routine
				solutionCheckTickTimestamps.Select(
					ts => {
						// Change digits
						timerDisplayGenerator?.TickDisplay();
						countdownText.text = rng.Next(100).ToString("D2");
						return new CoroutineYield() { WaitUntil = ts };
					}
				).ToAnimation();

			// First distortion boost
			yield return new Shift1D(
				solutionCheckDistortionBoostTimes[0],
				solutionCheckDistortionBoostDurations[0],
				solutionCheckDistortionBoostEasings[0],
				distortionManager.AddDistortionTime
			);
			yield return new CoroutineYield() { WaitUntil = solutionCheckSecondDistortionTimestamp };

			// Second distorton boost
			yield return new Shift1D(
				solutionCheckDistortionBoostTimes[1],
				solutionCheckDistortionBoostDurations[1],
				solutionCheckDistortionBoostEasings[1],
				distortionManager.AddDistortionTime
			);
			yield return new CoroutineYield() { WaitUntil = solutionCheckStatusLightOffTimestamp };

			// Third distrotion + Turn off stauts light
			statusLightProxy.SetInActive();

			yield return new Shift1D(
				solutionCheckDistortionBoostTimes[2],
				solutionCheckDistortionBoostDurations[2],
				solutionCheckDistortionBoostEasings[2],
				distortionManager.AddDistortionTime
			);

			// Wait until the "drop" of the sound
			yield return new CoroutineYield() { WaitUntil = solutionCheckSuspenseEndTimestamp };

			// Final distortion
			yield return new Shift1D(
				solutionCheckDistortionBoostTimes[3],
				solutionCheckDistortionBoostDurations[3],
				solutionCheckDistortionBoostEasings[3],
				distortionManager.AddDistortionTime
			);

			// Check solution
			chosenReleaseTime = GetCountdownDisplayNumber(); // calculation, not display reading
			logger.LogStringFormat("Button was released with {0:D2} on the module's countdown display.", chosenReleaseTime);
			bool isCorrect = validReleaseTimes.Contains(chosenReleaseTime);

			if (isCorrect) {

				// Solve
				logger.LogString("Release time is valid.");
				statusLightProxy.SetPass();
				countdownText.text = CountdownTextSolved;

				// Set time to prefered digits
				TimeSpan showPreferredDigitsDuration = solutionCheckRestoreTimeTimestamp - solutionCheckSuspenseEndTimestamp;
				TimeSpan nullTickDuration = TimeSpan.FromTicks(showPreferredDigitsDuration.Ticks / solutionCheckPreferredDigitsNullTicks);
				
				for (int i = 0; i < solutionCheckPreferredDigitsNullTicks - 1; i++) {
					timerDisplayGenerator.SetDisplayToPreffered(rng.Next(10));
					yield return CoroutineYield.Sleep(nullTickDuration);
				}
				timerDisplayGenerator.SetDisplayToPreffered(rng.Next(10));

				yield return new CoroutineYield() { WaitUntil = solutionCheckRestoreTimeTimestamp };

				// Set penalty
				// (wont be deducted until time is restored)
				int currentPenaltyBaseLine;
				CalculateAndSetPenalty(chosenReleaseTime, out currentPenaltyBaseLine);

				logger.LogStringFormat("Penalties start below {0:D2}.", currentPenaltyBaseLine);
				if (penaltyTimeLeft > TimeSpan.Zero) {
					logger.LogStringFormat("Time penalty of {0:F3} seconds will be delivered over time.", penaltyTimeLeft.TotalSeconds);
				} else {
					logger.LogString("No time penalty will be delivered.");
				}

				// Change state (to/and) stop sapping timer
				state = State.SolvedRestoringTime;
				timerSapper.UnsapBombTimer();

			} else {
				
				// Incorrect
				logger.LogString("Release time is invalid.");
				releaseSoundRef.StopSound();

				countdownText.text = chosenReleaseTime.ToString("D2");
				kmAudio.PlaySoundAtTransform(SoundCatalogue.VoOopsLaughter, transform);
				yield return CoroutineYield.Sleep(solutionCheckOopsDuration);

				state = State.Striking;
				timerSapper.UnsapBombTimer();
				animationRunner.Run(InfiniteStrikingRoutine().ToAnimation());
			}

			// Restore time, remove distorion
			RestoreTime();
			foreach (var @yield in EndDistortionRoutine()) yield return @yield;

			// Set solve state
			if (isCorrect) {
				state = State.Solved;
				statusLightProxy.HandlePass();
				logger.LogString("Solved!");
			}

		}

		readonly static TimeSpan timeoutBlinkFlipDelta = TimeSpan.FromSeconds(0.5);
		const int timeoutBlinksTotal = 3;

		IEnumerable<CoroutineYield> TimeRanOutRoutineRoutine() {

			logger.LogString("Time ran out.");

			// Set time display to 0
			timerDisplayGenerator.DisplayOverride = "00:00";

			// Blink
			for (int i = 0; i < timeoutBlinksTotal; i++) {
				countdownText.text = "  ";
				yield return CoroutineYield.Sleep(timeoutBlinkFlipDelta);
				countdownText.text = "00";
				yield return CoroutineYield.Sleep(timeoutBlinkFlipDelta);
			}
			countdownText.text = "  ";
			yield return CoroutineYield.Sleep(timeoutBlinkFlipDelta);

			// Reset status light and time flow
			statusLightProxy.SetInActive();
			RestoreTime();

			// Start striking
			state = State.Striking;
			animationRunner.Run(InfiniteStrikingRoutine().ToAnimation());

			// Remove distortion
			foreach (var @yield in EndDistortionRoutine()) yield return @yield;

		}

		#endregion

	}

}
