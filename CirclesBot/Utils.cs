using Discord;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CirclesBot
{
    public static class Utils
    {
        //private static PerformanceCounter cpuCounter;
        public static double CPUFrequency { get; private set; }
        public static string CPUName { get; private set; }

        static Utils()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //cpuCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
                using (ManagementClass managementClass = new ManagementClass("Win32_Processor"))
                {
                    foreach (ManagementObject objMO in managementClass.GetInstances())
                    {
                        CPUFrequency = Convert.ToDouble(objMO["MaxClockSpeed"]);
                        CPUName = Convert.ToString(objMO["Name"]);
                    }
                }
            }
        }

        public static string GetCPUInfo()
        {
            string output = string.Empty;

            //            if (cpuCounter is not null)
            //            {
            //                double cpuValue = 0;

            //                Utils.Benchmark(() =>
            //                {
            //                    while (cpuValue == 0)
            //                    {
            //#pragma warning disable CA1416 // Validate platform compatibility
            //                        puValue = cpuCounter.NextValue();
            //#pragma warning restore CA1416 // Validate platform compatibility
            //                    }
            //                }, "Query CPU", LogLevel.Info);

            //                double turboSpeed = ((CPUFrequency / 1000) * cpuValue) / 100;

            //                output += $"CPU: **{CPUName}**\n" +
            //                    $"CPU Frequency: **{turboSpeed:F2}GHz**\n";
            //            }
            output += $"CPU: **{CPUName}**\n";

            output += $"CPU Cores: **{Environment.ProcessorCount}**\n";

            return output;
        }

        private static Random rng = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            return rng.Next(min, max + 1);
        }

        public static bool GetRandomChance(double chancePercentage)
        {
            return rng.NextDouble() < (chancePercentage/100.0);
        }

        public static double GetRandomDouble() => rng.NextDouble();

        public static string ToFriendlyString(this Mods mod)
        {
            string output = mod.ToString().Replace(", ", "");

            if (mod.HasFlag(Mods.NC))
                output = output.Replace("DT", "");

            if (mod.HasFlag(Mods.PF))
                output = output.Replace("SD", "");

            return output;
        }

        //Tries to convert a continous string of mods to a Mods enum
        public static Mods StringToMod(string modString)
        {
            //Start with our output at null
            int output = (int)Mods.Null;

            //Split the input string into chunks of two characters per string to read mods: Ex. (NM, DT, FL, EZ) etc..
            var chunks = modString.SplitInParts(2);

            foreach (var chunk in chunks)
            {
                //check if the current two characters is a valid mod
                bool isMod = Enum.TryParse(chunk.ToString(), true, out Mods result);
                if (isMod)
                {
                    //If it's a valid mod, check if our output is set to null, if it is, set it to 0 so we can add from a clean position
                    if (((Mods)output) == Mods.Null)
                        output = 0;

                    //If the input mod was nomod, the above would already have it set to nomod, the if statement under will not go through

                    //If the output mods doesnt already contain the input mod example being given (DT, DT, DT) Only take 1 dt and add it to the output
                    if(((Mods)output).HasFlag(result) == false)
                        output += (int)result;

                    //Remember output is a flag so everything is additive
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

            if (int.TryParse(beatmapset, out int result))
                return result;
            else
                return 0;
        }

        public static void Benchmark(Action a, string name, LogLevel level = LogLevel.Info)
        {
            Stopwatch sw = Stopwatch.StartNew();
            a?.Invoke();
            sw.Stop();
            double time = ((double)sw.ElapsedTicks / Stopwatch.Frequency) * 1000.0;

            time = Math.Round(time, 2);
            Logger.Log($"{name} took {time} milliseconds", level);
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
                    return "<:RankF:847994614370795570>";
                case "D":
                    return "<:RankD:847994064020439062>";
                case "C":
                    return "<:RankC:847994063856467970>";
                case "B":
                    return "<:RankB:847994063944155197>";
                case "A":
                    return "<:RankA:847994064242868225>";
                case "S":
                    return "<:RankS:847994064220717086>";
                case "X":
                    return "<:RankX:847994064238018560>";
                case "SH":
                    return "<:RankSH:847994063977185322>";
                case "XH":
                    return "<:RankXH:847994064174579753>";
                default:
                    return ":sunglasses:";
            }
        }

        public static void Save<T>(this T t, string filename)
        {
            string json = JsonConvert.SerializeObject(t);

            File.WriteAllText($"./{filename}.json", json);
        }

        public static T Load<T>(string filename)
        {
            string path = $"./{filename}.json";

            if (!File.Exists(path))
            {
                Logger.Log($"Could not load from {path} no such file exists, create instance used instead", LogLevel.Info);
                return Activator.CreateInstance<T>();
            }

            string json = File.ReadAllText(path);
            
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
