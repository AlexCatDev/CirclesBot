using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CirclesBot
{
    public static class BeatmapManager
    {
        public const string MapDirectory = "./Maps";

        private static object lockObject = new object();

        static BeatmapManager()
        {
            if (!Directory.Exists(MapDirectory))
            {
                Directory.CreateDirectory(MapDirectory);
                Logger.Log("Map directory didn't exist and has been created", LogLevel.Warning);
            }
        }

        public static string GetBeatmap(ulong id, bool reDownload = false)
        {
            lock (lockObject)
            {
                //Load map from disk (if available)
                if (File.Exists($"{MapDirectory}/{id}") && reDownload == false)
                {
                    string bm = File.ReadAllText($"{MapDirectory}/{id}");
                    return bm;
                }
                else
                {
                    if (reDownload == true)
                        Logger.Log("Doing a map redownload!", LogLevel.Info);

                    //If no file, download beatmap from osu.ppy.sh
                    using (WebClient wc = new WebClient())
                    {
                        Logger.Log($"Downloading beatmap: {id}");
                        string beatmap = "";

                        Utils.Benchmark(() =>
                        {
                            beatmap = wc.DownloadString($"https://osu.ppy.sh/osu/{id}");
                        }, "\t");

                        string filename = $"{id}";

                        Logger.Log($"Saving beatmap [{filename}]");
                        //Save map to disk
                        Utils.Benchmark(() =>
                        {
                            File.WriteAllText($"{MapDirectory}/{filename}", beatmap);
                        }, "\t");

                        return beatmap;
                    }
                }
            }
        }
    }
}
