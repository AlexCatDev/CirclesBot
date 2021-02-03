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

        private static Dictionary<ulong, string> mapCache = new Dictionary<ulong, string>();

        public static int CachedMapCount => mapCache.Count;

        private static object lockObject = new object();

        static BeatmapManager()
        {
            if (!Directory.Exists(MapDirectory))
            {
                Directory.CreateDirectory(MapDirectory);
                Logger.Log("Map directory didn't exist and has been created", LogLevel.Info);
            }
        }

        public static string GetBeatmap(ulong id)
        {
            lock (lockObject)
            {
                //First check if map is already cached in memory
                if (mapCache.TryGetValue(id, out string bmp))
                {
                    //Logger.Log("Just read a map from memory cache", LogLevel.Info);
                    return bmp;
                }
                //If not then look for the file on disk
                if (File.Exists($"{MapDirectory}/{id}"))
                {
                    string bm = File.ReadAllText($"{MapDirectory}/{id}");
                    mapCache.Add(id, bm);
                    return bm;
                }
                else
                {
                    //If no file, download beatmap from osu.ppy.sh
                    using (WebClient wc = new WebClient())
                    {
                        int time = 0;

                        Logger.Log($"Downloading beatmap: {id}");
                        string beatmap = "";

                        time = Utils.Benchmark(() =>
                        {
                            beatmap = wc.DownloadString($"https://osu.ppy.sh/osu/{id}");
                        });
                        Logger.Log($"\tIt took {time} milliseconds", LogLevel.Success);

                        bool ranked = true;

                        Logger.Log($"Checking status");
                        time =  Utils.Benchmark(() =>
                        {
                            string scores = wc.DownloadString($"https://osu.ppy.sh/beatmaps/{id}/scores");
                            ranked = scores.Contains("\"status\":\"ranked\"");
                        });
                        Logger.Log($"\tIt took {time} milliseconds IsRanked: {ranked}", LogLevel.Success);

                        if (ranked)
                        {
                            string filename = $"{id}";

                            Logger.Log($"Saving beatmap [{filename}]");
                            //Save map to disk
                            time = Utils.Benchmark(() =>
                            {
                                File.WriteAllText($"{MapDirectory}/{filename}", beatmap);
                            });

                            Logger.Log($"\tIt took {time} milliseconds", LogLevel.Success);
                        }
                        //Add map to cache
                        mapCache.Add(id, beatmap);
                        return beatmap;
                    }
                }
            }
        }
    }
}
