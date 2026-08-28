using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Legacy wrapper for LevelDefinition.
    /// All level configuration is handled by LevelDefinition.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelRules",
        menuName = "GemmaRainbowSeeker/Level Rules (Legacy)",
        order = 1)]
    public class LevelRules : LevelDefinition
    {
    }
}

