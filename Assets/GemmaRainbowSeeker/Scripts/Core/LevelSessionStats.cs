namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Accumulates per-session statistics for a level run.
    /// Pure C# — no MonoBehaviour or Unity dependency.
    /// </summary>
    public sealed class LevelSessionStats
    {
        // ── State ─────────────────────────────────────────────────────────────
        private float _elapsedSeconds;
        private int   _correctCollections;
        private int   _wrongAttempts;
        private int   _damageTaken;
        private int   _hazardsBroken;
        private int   _rainbowRestsActivated;
        private int   _checkpointRestarts;
        private int   _highestMultiplier = 1;
        private float _longestRushDuration = 0f;
        private int   _rushBreaks = 0;

        // ── Constructor ───────────────────────────────────────────────────────

        public LevelSessionStats() { }

        // ── Read-only Properties ──────────────────────────────────────────────

        /// <summary>Total elapsed time for this run in seconds.</summary>
        public float ElapsedSeconds => _elapsedSeconds;

        /// <summary>Number of correct gem collections.</summary>
        public int CorrectCollections => _correctCollections;

        /// <summary>Number of wrong-gem attempts (mismatch touches).</summary>
        public int WrongAttempts => _wrongAttempts;

        /// <summary>Number of damage hits Gemma has taken.</summary>
        public int DamageTaken => _damageTaken;

        /// <summary>Number of dash-breakable hazards destroyed.</summary>
        public int HazardsBroken => _hazardsBroken;

        /// <summary>Number of times a Rainbow Rest was activated (first-time only).</summary>
        public int RainbowRestsActivated => _rainbowRestsActivated;

        /// <summary>Number of times the player restarted from a Rainbow Rest checkpoint.</summary>
        public int CheckpointRestarts => _checkpointRestarts;

        /// <summary>Highest Rainbow Rush multiplier achieved during this run.</summary>
        public int HighestMultiplier => _highestMultiplier;

        /// <summary>Longest continuous Rainbow Rush duration in seconds.</summary>
        public float LongestRushDuration => _longestRushDuration;

        /// <summary>Total number of times Rainbow Rush was broken.</summary>
        public int RushBreaks => _rushBreaks;

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>Advances the elapsed timer by <paramref name="dt"/> seconds.</summary>
        public void Tick(float dt)
        {
            if (dt > 0f)
                _elapsedSeconds += dt;
        }

        // ── Increment & Update Methods ────────────────────────────────────────

        /// <summary>Records a correct gem collection.</summary>
        public void RecordCorrectCollection()    => _correctCollections++;

        /// <summary>Records a wrong-gem attempt.</summary>
        public void RecordWrongAttempt()         => _wrongAttempts++;

        /// <summary>Records a damage hit.</summary>
        public void RecordDamageTaken()          => _damageTaken++;

        /// <summary>Records a breakable hazard being destroyed.</summary>
        public void RecordHazardBroken()         => _hazardsBroken++;

        /// <summary>Records the first-time activation of a Rainbow Rest.</summary>
        public void RecordRainbowRestActivated() => _rainbowRestsActivated++;

        /// <summary>Records a restart from a Rainbow Rest checkpoint.</summary>
        public void RecordCheckpointRestart()    => _checkpointRestarts++;

        /// <summary>Updates rush stats from controller.</summary>
        public void UpdateRushStats(int highestMul, float longestRush, int breaks)
        {
            if (highestMul > _highestMultiplier) _highestMultiplier = highestMul;
            if (longestRush > _longestRushDuration) _longestRushDuration = longestRush;
            _rushBreaks = breaks;
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        /// <summary>Resets all stats to zero (full level restart).</summary>
        public void Reset()
        {
            _elapsedSeconds        = 0f;
            _correctCollections    = 0;
            _wrongAttempts         = 0;
            _damageTaken           = 0;
            _hazardsBroken         = 0;
            _rainbowRestsActivated = 0;
            _checkpointRestarts    = 0;
            _highestMultiplier     = 1;
            _longestRushDuration   = 0f;
            _rushBreaks            = 0;
        }
    }
}

