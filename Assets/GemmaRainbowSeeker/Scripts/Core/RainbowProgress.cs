using System;
using System.Collections.Generic;
using System.Linq;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Tracks Gemma's progress through an ordered rainbow sequence from a LevelDefinition.
    /// Supports sequences of any length (1 to 10+), repeated colours (e.g. Red, Red, Orange),
    /// banking at checkpoints, and resetting.
    /// Pure C# — no MonoBehaviour or Unity dependency.
    /// </summary>
    public sealed class RainbowProgress
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly RainbowColour[] _sequence;
        private int _collectedCount;
        private int _bankedCount;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when the collected count changes (correct collection, restore, or reset).</summary>
        public event Action ProgressChanged;

        /// <summary>Raised when the current target colour changes.</summary>
        public event Action TargetChanged;

        /// <summary>Raised when a correct colour is collected. Carries the collected colour.</summary>
        public event Action<RainbowColour> CorrectColourCollected;

        /// <summary>Raised when a wrong colour is attempted. Carries the attempted colour.</summary>
        public event Action<RainbowColour> IncorrectColourAttempted;

        /// <summary>Raised when progress is banked at a Rainbow Rest.</summary>
        public event Action ProgressBanked;

        /// <summary>Raised when all colours in the sequence are collected.</summary>
        public event Action RainbowCompleted;

        // ── Constructors ───────────────────────────────────────────────────────

        /// <param name="sequence">
        /// Ordered array of colours Gemma must collect. Must not be null or empty.
        /// </param>
        public RainbowProgress(RainbowColour[] sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sequence.Length == 0) throw new ArgumentException("Sequence must not be empty.", nameof(sequence));

            _sequence = new RainbowColour[sequence.Length];
            sequence.CopyTo(_sequence, 0);

            _collectedCount = 0;
            _bankedCount    = 0;
        }

        /// <summary>
        /// Initializes progression from an enumerable colour sequence.
        /// </summary>
        public RainbowProgress(IEnumerable<RainbowColour> sequence)
            : this(sequence != null ? sequence.ToArray() : null)
        {
        }

        /// <summary>
        /// Initializes progression from a LevelDefinition asset.
        /// </summary>
        public RainbowProgress(LevelDefinition definition)
            : this(definition != null ? definition.ColourSequence : null)
        {
        }

        // ── Read-only Properties ──────────────────────────────────────────────

        /// <summary>A copy of the ordered colour sequence for this level.</summary>
        public RainbowColour[] Sequence
        {
            get
            {
                var copy = new RainbowColour[_sequence.Length];
                _sequence.CopyTo(copy, 0);
                return copy;
            }
        }

        /// <summary>Total number of colours in the sequence.</summary>
        public int TotalCount => _sequence.Length;

        /// <summary>Number of colours collected so far in this run.</summary>
        public int CollectedCount => _collectedCount;

        /// <summary>Number of colours that were banked at the last Rainbow Rest.</summary>
        public int BankedCount => _bankedCount;

        /// <summary>True when all colours in the sequence have been collected.</summary>
        public bool IsComplete => _collectedCount >= _sequence.Length;

        /// <summary>
        /// The zero-based index of the current required gem in the sequence.
        /// Equal to TotalCount when IsComplete is true.
        /// </summary>
        public int CurrentTargetIndex => _collectedCount;

        /// <summary>
        /// The colour Gemma must collect next, or null if the rainbow is already complete.
        /// Supports duplicate colours (e.g. Red, Red, Orange).
        /// </summary>
        public RainbowColour? CurrentTarget =>
            IsComplete ? (RainbowColour?)null : _sequence[_collectedCount];

        /// <summary>True while there is still a colour to collect.</summary>
        public bool HasTarget => !IsComplete;

        /// <summary>Returns the colour at a specific index in the sequence.</summary>
        public RainbowColour GetColourAt(int index)
        {
            if (index < 0 || index >= _sequence.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _sequence[index];
        }

        // ── Operations ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to collect <paramref name="colour"/>.
        /// Returns true and advances progress only if colour matches the current target.
        /// Raises IncorrectColourAttempted (and returns false) on a mismatch.
        /// Raises RainbowCompleted if this collection finishes the rainbow.
        /// </summary>
        public bool TryCollect(RainbowColour colour)
        {
            if (IsComplete) return false;

            if (colour != _sequence[_collectedCount])
            {
                IncorrectColourAttempted?.Invoke(colour);
                return false;
            }

            _collectedCount++;
            CorrectColourCollected?.Invoke(colour);
            ProgressChanged?.Invoke();
            TargetChanged?.Invoke();

            if (IsComplete)
            {
                RainbowCompleted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Records the current collected count as the banked count (at a Rainbow Rest).
        /// Raises ProgressBanked.
        /// </summary>
        public void BankCurrentProgress()
        {
            _bankedCount = _collectedCount;
            ProgressBanked?.Invoke();
            ProgressChanged?.Invoke();
        }

        /// <summary>
        /// Restores collected count to the banked count (used on restart-from-Rest).
        /// Raises ProgressChanged and TargetChanged.
        /// </summary>
        public void RestoreBankedProgress()
        {
            if (_collectedCount != _bankedCount)
            {
                _collectedCount = _bankedCount;
                ProgressChanged?.Invoke();
                TargetChanged?.Invoke();
            }
        }

        /// <summary>
        /// Clears both collected and banked counts back to zero (full level restart).
        /// Raises ProgressChanged and TargetChanged.
        /// </summary>
        public void ResetLevelProgress()
        {
            _collectedCount = 0;
            _bankedCount    = 0;
            ProgressChanged?.Invoke();
            TargetChanged?.Invoke();
        }
    }
}

