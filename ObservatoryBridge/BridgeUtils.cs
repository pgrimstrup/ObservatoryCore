﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.ParameterTypes;

namespace Observatory.Bridge
{
    internal static class BridgeUtils
    {
        public static bool IsNeutronStar(this string? starType) => !String.IsNullOrWhiteSpace(starType) 
            && (starType.Contains("Neutron", StringComparison.OrdinalIgnoreCase) || starType.Equals("N", StringComparison.OrdinalIgnoreCase));
        
        public static bool IsBlackHole(this string? starType) => !String.IsNullOrWhiteSpace(starType) 
            && (starType.Contains("Black Hole", StringComparison.OrdinalIgnoreCase) || starType.Equals("H", StringComparison.OrdinalIgnoreCase) || starType.Equals("supermassiveblackhole", StringComparison.OrdinalIgnoreCase));
        
        public static bool IsWhiteDwarf(this string? starType) => !String.IsNullOrWhiteSpace(starType) 
            && (starType.Contains("White Dwarf", StringComparison.OrdinalIgnoreCase) || starType.Equals("DX", StringComparison.OrdinalIgnoreCase));
        
        public static bool IsEarthlike(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("Earthlike", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsWaterWorld(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("Water World", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsHighMetalContent(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("High Metal Content", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsAmmoniaWorld(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("Ammonia", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsMetalRich(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("Metal Rich", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsIcyBody(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains("Icy Body", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsGasGiant(this string? bodyType, string surdarskyClass) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains($"Class {surdarskyClass} Gas Giant", StringComparison.OrdinalIgnoreCase);
        
        public static bool IsGasGiant(this string? bodyType) => !String.IsNullOrWhiteSpace(bodyType) 
            && bodyType.Contains($"Gas Giant", StringComparison.OrdinalIgnoreCase);

        public static string ReplaceRomanNumerals(this string text)
        {
            var words = text.Split();
            for(int i = 0; i < words.Length; i++)
            {
                words[i] = ReplaceRomanNumeral(words[i]);
            }
            return String.Join(" ", words);
        }

        public static string ReplaceRomanNumeral(string word)
        {
            var suffix = "";
            if (word.EndsWith("."))
            {
                suffix = ".";
                word = word.TrimEnd('.');
            }
            if (word.EndsWith(","))
            {
                suffix = ",";
                word = word.TrimEnd(',');
            }

            var number = word.ToUpper() switch {
                "I" => "1",
                "II" => "2",
                "III" => "3",
                "IV" => "4",
                "V" => "5",
                "VI" => "6",
                "VII" => "7",
                "VIII" => "8",
                "IX" => "9",
                "X" => "10",
                "XI" => "11",
                "XII" => "12",
                "XIII" => "13",
                "XIV" => "14",
                "XV" => "15",
                "XVI" => "16",
                "XVII" => "17",
                "XVIII" => "18",
                "XIX" => "19",
                "XX" => "20",
                _ => word
            };
            return number + suffix;
        }


        public static string ConvertTextToSsml(string text)
        {
            StringBuilder sb = new StringBuilder();

            var words = text.Split();

            sb.Append("<speak>");
            for(int i = 0; i < words.Length; i++)
            {
                if (words[i] == ",")
                    sb.Append("<break time=\"150ms\"/>");
                else if (words[i] == ".")
                    sb.Append("<break time=\"250ms\"/>");
                else if (words[i].EndsWith(","))
                {
                    sb.Append(words[i]);
                    sb.Append("<break time=\"150ms\"/>");
                }
                else if (words[i].EndsWith("."))
                {
                    sb.Append(words[i]);
                    sb.Append("<break time=\"250ms\"/>");
                }
                else
                    sb.Append(words[i]);
            }

            sb.Append("</speak>");
            return sb.ToString();
        }


    }
}
