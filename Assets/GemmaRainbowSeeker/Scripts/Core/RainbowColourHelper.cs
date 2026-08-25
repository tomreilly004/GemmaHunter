using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Utility helper providing exact colour values, character markers and display names
    /// for the RainbowColour enum.
    /// </summary>
    public static class RainbowColourHelper
    {
        public static readonly Color RedColor    = new Color(1.0f, 0.2509804f, 0.3411765f, 1.0f); // #FF4057
        public static readonly Color OrangeColor = new Color(1.0f, 0.5411765f, 0.1411765f, 1.0f); // #FF8A24
        public static readonly Color YellowColor = new Color(1.0f, 0.8509804f, 0.2392157f, 1.0f); // #FFD93D
        public static readonly Color GreenColor  = new Color(0.2627451f, 0.8196079f, 0.4784314f, 1.0f); // #43D17A
        public static readonly Color BlueColor   = new Color(0.2117647f, 0.6549020f, 1.0f, 1.0f); // #36A7FF
        public static readonly Color IndigoColor = new Color(0.3568628f, 0.3372549f, 0.8392157f, 1.0f); // #5B56D6
        public static readonly Color VioletColor = new Color(0.7254902f, 0.2980392f, 1.0f, 1.0f); // #B94CFF

        public static Color GetColor(RainbowColour colour)
        {
            switch (colour)
            {
                case RainbowColour.Red:    return RedColor;
                case RainbowColour.Orange: return OrangeColor;
                case RainbowColour.Yellow: return YellowColor;
                case RainbowColour.Green:  return GreenColor;
                case RainbowColour.Blue:   return BlueColor;
                case RainbowColour.Indigo: return IndigoColor;
                case RainbowColour.Violet: return VioletColor;
                default:                   return Color.white;
            }
        }

        public static char GetMarkerChar(RainbowColour colour)
        {
            switch (colour)
            {
                case RainbowColour.Red:    return 'R';
                case RainbowColour.Orange: return 'O';
                case RainbowColour.Yellow: return 'Y';
                case RainbowColour.Green:  return 'G';
                case RainbowColour.Blue:   return 'B';
                case RainbowColour.Indigo: return 'I';
                case RainbowColour.Violet: return 'V';
                default:                   return '?';
            }
        }

        public static string GetMarkerString(RainbowColour colour)
        {
            return GetMarkerChar(colour).ToString();
        }

        public static string GetHex(RainbowColour colour)
        {
            switch (colour)
            {
                case RainbowColour.Red:    return "#FF4057";
                case RainbowColour.Orange: return "#FF8A24";
                case RainbowColour.Yellow: return "#FFD93D";
                case RainbowColour.Green:  return "#43D17A";
                case RainbowColour.Blue:   return "#36A7FF";
                case RainbowColour.Indigo: return "#5B56D6";
                case RainbowColour.Violet: return "#B94CFF";
                default:                   return "#FFFFFF";
            }
        }
    }
}
