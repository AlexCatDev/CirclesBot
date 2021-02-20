﻿using Discord;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace CirclesBot
{
    public static class Utils
    {
        private static Random rng = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            return rng.Next(min, max + 1);
        }

        public static bool GetRandomChance(double chancePercentage)
        {
            return rng.NextDouble() < (chancePercentage/100.0);
        }

        public static string ToFriendlyString(this Mods mod)
        {
            string output = mod.ToString().Replace(", ", "");

            if (mod.HasFlag(Mods.NC))
                output = output.Replace("DT", "");

            if (mod.HasFlag(Mods.PF))
                output = output.Replace("SD", "");

            return output;
        }

        public static Mods StringToMod(string modString)
        {
            int output = 0;

            var chunks = modString.SplitInParts(2);
            foreach (var chunk in chunks)
            {
                foreach (Mods mod in Enum.GetValues(typeof(Mods)))
                {
                    if (mod.ToString().StartsWith(chunk.ToString().ToUpper()))
                        output += (int)mod;
                }
            }

            return (Mods)output;
        }

        public static int FindBeatmapsetID(string mapData)
        {
            int index = mapData.ToLower().IndexOf("beatmapsetid:");

            int offset = index + "beatmapsetid:".Length;

            string beatmapset = "";

            while (mapData[offset] != '\r')
            {
                beatmapset += mapData[offset++];
            }

            int result = 0;
            if (int.TryParse(beatmapset, out result))
                return result;
            else
                return 0;
        }

        public static int Benchmark(Action a)
        {
            Stopwatch sw = Stopwatch.StartNew();
            a?.Invoke();
            sw.Stop();
            double time = ((double)sw.ElapsedTicks / Stopwatch.Frequency) * 1000.0;
            return (int)time;
        }

        //Credits: https://github.com/raresica1234
        public static string FormatTime(TimeSpan time, bool ago = true, int statCount = 3)
        {
            int[] stats = new int[] { (int)(time.Days / 365), 0, time.Days, time.Hours, time.Minutes, time.Seconds.Clamp(1, 60) };
            stats[2] -= stats[0] * 365;
            stats[1] = stats[2] / 30;
            stats[2] -= stats[1] * 30;
            string[] names = new string[] { " Years ", " Months ", " Days ", " Hours ", " Minutes ", " Seconds " };
            string output = "";
            for (int i = 0; i < stats.Length && statCount != 0; i++)
            {
                if (stats[i] != 0)
                {
                    output += stats[i] + names[i];
                    statCount--;
                }
            }

            if (ago)
                output += "Ago";

            return output;
        }

        public static string GetEmoteForRankLetter(string rankLetter)
        {
            switch (rankLetter)
            {
                case "F":
                    return "<:rankF:756892070177931407>";
                case "D":
                    return "<:rankD:756833574342098994>";
                case "C":
                    return "<:rankC:756833574123995218>";
                case "B":
                    return "<:rankB:756833574296092672>";
                case "A":
                    return "<:rankA:756833574228983958>";
                case "S":
                    return "<:rankS:756833574216400896>";
                case "X":
                    return "<:rankSS:756833574228852796>";
                case "SH":
                    return "<:rankSH:756833574187171880>";
                case "XH":
                    return "<:rankSSH:756833574111674390>";
                default:
                    return ":sunglasses:";
            }
        }
    }
}
