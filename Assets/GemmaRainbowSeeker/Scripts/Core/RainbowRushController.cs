using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Reason why Rainbow Rush was reset back to x1.
    /// </summary>
    public enum RushResetReason
    {
        None,
        WrongColour,
        Damage,
        KnockedOut,
        Restart,
        TimerExpired,
        Stopped
    }

    /// <summary>
    /// Core multiplier and timer controller for Gemma Beaker: Rainbow Seeker.
    /// Replaces the old combo system with a single Rainbow Rush system:
    /// - Multiplier starts at x1.
    /// - First correct gem scores at x1, then raises Rush to x2.
    /// - Subsequent correct gems score at current multiplier, then raise it by 1 tier (up to x5).
    /// - Correct gems refresh the remaining Rush timer.
    /// - Resets immediately on wrong colour, damage, knockout, restart, timer expiry,
    ///   or when Gemma stops moving for longer than 0.45s (input < deadzone AND speed < threshold).
    /// - Speed bonus applied to Gemma's swim speed: x1=0%, x2=6%, x3=12%, x4=18%, x5=24%.
    /// Pure C# — no MonoBehaviour dependency for easy unit testing.
    /// </summary>
    public sealed class RainbowRushController
    {
        // ── Constants & Defaults ──────────────────────────────────────────────
        public const int MinMultiplier = 1;
        public const int MaxMultiplier = 5;
        public const float DefaultStopGraceDuration = 0.45f;
        public const float DefaultStopInputDeadzone = 0.1f;
        public const float DefaultStopSpeedThreshold = 0.2f;

        // ── State ─────────────────────────────────────────────────────────────
        private int _multiplier = MinMultiplier;
        private float _remainingTime = 0f;
        private float _rushWindow = 30f;
        private float _stopTimer = 0f;
        private float _stopGraceDuration = DefaultStopGraceDuration;
        private float _stopInputDeadzone = DefaultStopInputDeadzone;
        private float _stopSpeedThreshold = DefaultStopSpeedThreshold;

        private RushResetReason _lastResetReason = RushResetReason.None;
        private int _highestMultiplierAchieved = MinMultiplier;
        private float _longestRushDuration = 0f;
        private float _currentRushDuration = 0f;
        private int _rushBreakCount = 0;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Raised when the multiplier changes. Carries the new multiplier (1..5).</summary>
        public event Action<int> OnMultiplierChanged;

        /// <summary>Raised when the rush countdown timer updates. Carries (remainingTime, totalWindow).</summary>
        public event Action<float, float> OnTimerUpdated;

        /// <summary>Raised when Rainbow Rush is reset back to x1. Carries the reset reason.</summary>
        public event Action<RushResetReason> OnRushReset;

        /// <summary>Raised on correct gem collection. Carries (scoringMultiplier, newMultiplier).</summary>
        public event Action<int, int> OnRushTierIncreased;

        /// <summary>Raised when maximum multiplier x5 is reached.</summary>
        public event Action OnMaxRushReached;

        // ── Constructors ───────────────────────────────────────────────────────
        public RainbowRushController(float rushWindow = 30f, float stopGrace = DefaultStopGraceDuration)
        {
            _rushWindow = Mathf.Max(1f, rushWindow);
            _stopGraceDuration = Mathf.Max(0.05f, stopGrace);
            _multiplier = MinMultiplier;
            _remainingTime = 0f;
            _stopTimer = 0f;
            _highestMultiplierAchieved = MinMultiplier;
        }

        public RainbowRushController(LevelDefinition definition)
            : this(definition != null ? definition.RainbowRushTimeWindow : 30f)
        {
        }

        // ── Properties ────────────────────────────────────────────────────────
        public int Multiplier => _multiplier;
        public float RemainingTime => _remainingTime;
        public float RushWindow => _rushWindow;
        public bool IsRushActive => _multiplier > MinMultiplier;
        public float StopTimer => _stopTimer;
        public float StopGraceDuration => _stopGraceDuration;
        public float StopInputDeadzone
        {
            get => _stopInputDeadzone;
            set => _stopInputDeadzone = Mathf.Max(0f, value);
        }
        public float StopSpeedThreshold
        {
            get => _stopSpeedThreshold;
            set => _stopSpeedThreshold = Mathf.Max(0f, value);
        }

        public RushResetReason LastResetReason => _lastResetReason;
        public int HighestMultiplierAchieved => _highestMultiplierAchieved;
        public float LongestRushDuration => _longestRushDuration;
        public float CurrentRushDuration => _currentRushDuration;
        public int RushBreakCount => _rushBreakCount;

        /// <summary>Speed bonus fraction for Gemma (0.00 to 0.24).</summary>
        public float CurrentSpeedBonus => GetSpeedBonusForTier(_multiplier);

        /// <summary>Total speed multiplier (1.00 to 1.24).</summary>
        public float CurrentSpeedMultiplier => 1f + CurrentSpeedBonus;

        // ── Speed Bonus Helper ────────────────────────────────────────────────
        public static float GetSpeedBonusForTier(int tier)
        {
            switch (tier)
            {
                case 2: return 0.06f;
                case 3: return 0.12f;
                case 4: return 0.18f;
                case 5: return 0.24f;
                default: return 0f;
            }
        }

        public static string GetResetReasonDescription(RushResetReason reason)
        {
            switch (reason)
            {
                case RushResetReason.Stopped: return "Stopped moving";
                case RushResetReason.TimerExpired: return "Timer expired";
                case RushResetReason.WrongColour: return "Wrong colour";
                case RushResetReason.Damage: return "Took damage";
                case RushResetReason.KnockedOut: return "Knocked out";
                case RushResetReason.Restart: return "Checkpoint restart";
                default: return "Rush lost";
            }
        }

        // ── Rush Lifecycle Operations ─────────────────────────────────────────

        /// <summary>
        /// Registers a correct gem collection:
        /// - Returns the scoring multiplier to use for awarding points for THIS gem.
        /// - If multiplier < 5, increases multiplier by 1 tier.
        /// - Refreshes the remaining Rush timer back to RushWindow.
        /// - Resets the stop timer.
        /// </summary>
        public int RegisterCorrectCollection()
        {
            int scoringMultiplier = _multiplier;
            int previousMultiplier = _multiplier;

            if (_multiplier < MaxMultiplier)
            {
                _multiplier++;
            }

            _remainingTime = _rushWindow;
            _stopTimer = 0f;

            if (_multiplier > _highestMultiplierAchieved)
            {
                _highestMultiplierAchieved = _multiplier;
            }

            OnMultiplierChanged?.Invoke(_multiplier);
            OnTimerUpdated?.Invoke(_remainingTime, _rushWindow);
            OnRushTierIncreased?.Invoke(scoringMultiplier, _multiplier);

            if (_multiplier == MaxMultiplier && previousMultiplier < MaxMultiplier)
            {
                OnMaxRushReached?.Invoke();
            }

            return scoringMultiplier;
        }

        /// <summary>
        /// Immediately resets Rainbow Rush back to multiplier x1.
        /// </summary>
        public void ResetRush(RushResetReason reason)
        {
            _lastResetReason = reason;

            if (_multiplier > MinMultiplier)
            {
                _rushBreakCount++;
                _multiplier = MinMultiplier;
                _remainingTime = 0f;
                _stopTimer = 0f;
                _currentRushDuration = 0f;

                OnRushReset?.Invoke(reason);
                OnMultiplierChanged?.Invoke(MinMultiplier);
                OnTimerUpdated?.Invoke(0f, _rushWindow);
            }
            else
            {
                _stopTimer = 0f;
                _remainingTime = 0f;
            }
        }

        /// <summary>
        /// Ticks the Rush controller for the current frame:
        /// - If suspended (paused, tutorial, results), pauses countdown and stop detection.
        /// - If Rush is active (multiplier > 1), decrements timer and checks stop condition.
        /// </summary>
        public void Tick(float deltaTime, Vector2 moveInput, Vector2 velocity, bool isSuspended)
        {
            if (isSuspended || deltaTime <= 0f)
            {
                return;
            }

            if (_multiplier <= MinMultiplier)
            {
                _stopTimer = 0f;
                return;
            }

            // Accumulate active rush duration
            _currentRushDuration += deltaTime;
            if (_currentRushDuration > _longestRushDuration)
            {
                _longestRushDuration = _currentRushDuration;
            }

            // 1. Countdown timer
            _remainingTime -= deltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                ResetRush(RushResetReason.TimerExpired);
                return;
            }

            // 2. Stop detection: requires BOTH input < deadzone AND speed < threshold
            bool inputStopped = moveInput.sqrMagnitude < (_stopInputDeadzone * _stopInputDeadzone);
            bool speedStopped = velocity.sqrMagnitude < (_stopSpeedThreshold * _stopSpeedThreshold);

            if (inputStopped && speedStopped)
            {
                _stopTimer += deltaTime;
                if (_stopTimer >= _stopGraceDuration)
                {
                    ResetRush(RushResetReason.Stopped);
                    return;
                }
            }
            else
            {
                _stopTimer = 0f;
            }

            OnTimerUpdated?.Invoke(_remainingTime, _rushWindow);
        }

        /// <summary>
        /// Resets accumulated session statistics (used on full level restart).
        /// </summary>
        public void ResetStats()
        {
            _multiplier = MinMultiplier;
            _remainingTime = 0f;
            _stopTimer = 0f;
            _currentRushDuration = 0f;
            _longestRushDuration = 0f;
            _highestMultiplierAchieved = MinMultiplier;
            _rushBreakCount = 0;
            _lastResetReason = RushResetReason.None;
        }
    }
}
