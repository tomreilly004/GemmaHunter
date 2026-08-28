using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    public enum TutorialTriggerEvent
    {
        OnLevelStart,
        OnFirstCorrectGem,
        OnFirstWrongGem,
        OnFirstHazard,
        OnFirstDash,
        OnFirstBank,
        OnRainbowComplete,
        OnRushBroken,
        Manual
    }

    /// <summary>
    /// Data asset defining an ordered sequence of data-driven tutorial steps for a level.
    /// Supports highlight targets, optional gameplay pause, event triggers, and show-once rules.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TutorialSequence",
        menuName = "GemmaRainbowSeeker/Tutorial Sequence",
        order = 2)]
    public class TutorialSequence : ScriptableObject
    {
        [Serializable]
        public struct TutorialStep
        {
            [Tooltip("Unique identifier for this tutorial step.")]
            public string stepId;

            [Tooltip("Gameplay event that triggers this step.")]
            public TutorialTriggerEvent triggerEvent;

            [Tooltip("Title text displayed at the top of the tutorial banner.")]
            public string title;

            [TextArea(2, 4)]
            [Tooltip("Instructional body text explaining the mechanic.")]
            public string body;

            [Tooltip("Control prompt or hint (e.g. 'MOVE: Joystick / WASD').")]
            public string controlsHint;

            [Tooltip("Optional name of a UI element or world object to highlight.")]
            public string highlightTargetName;

            [Tooltip("If true, pauses gameplay while this tutorial is displayed.")]
            public bool pauseGameplay;

            [Tooltip("If true, this tutorial step will only show once per profile.")]
            public bool showOnce;

            [Tooltip("Time in seconds before auto-dismissing (0 = requires player dismiss / event).")]
            public float displayDuration;
        }

        [SerializeField] private List<TutorialStep> _steps = new List<TutorialStep>();

        public IReadOnlyList<TutorialStep> Steps => _steps;

        public void AddStep(TutorialStep step)
        {
            _steps.Add(step);
        }

        public void ClearSteps()
        {
            _steps.Clear();
        }
    }
}

