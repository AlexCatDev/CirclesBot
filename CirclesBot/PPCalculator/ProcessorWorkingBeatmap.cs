using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Skinning;
using System;
using System.IO;
using System.Text;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace PPCalculator
{
    public class ProcessorWorkingBeatmap : WorkingBeatmap
    {
        private readonly Beatmap beatmap;

        public ProcessorWorkingBeatmap(string file, int? beatmapId = null)
            : this(readFromFile(file), beatmapId)
        {
        }

        public ProcessorWorkingBeatmap(Beatmap beatmap, int? beatmapId = null)
            : base(beatmap.BeatmapInfo, null)
        {
            this.beatmap = beatmap;

            beatmap.BeatmapInfo.Ruleset = LegacyHelper.GetRulesetFromLegacyID(beatmap.BeatmapInfo.Ruleset.OnlineID).RulesetInfo;

            if (beatmapId.HasValue)
                beatmap.BeatmapInfo.OnlineID = beatmapId.Value;
        }

        private static Beatmap readFromFile(string filename)
        {
            using (var stream = File.OpenRead(filename))
            using (var reader = new LineBufferedReader(stream))
                return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        public static ProcessorWorkingBeatmap FromString(string beatmapString, int? beatmapId = null)
        {
            Beatmap beatmap;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(beatmapString)))
            using (var reader = new LineBufferedReader(stream))
                beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            return new ProcessorWorkingBeatmap(beatmap, beatmapId);
        }

        public static ProcessorWorkingBeatmap FromFile(string filePath)
        {
            return new ProcessorWorkingBeatmap(filePath);
        }

        public static ProcessorWorkingBeatmap FromFileOrId(string fileOrId)
        {
            if (fileOrId.EndsWith(".osu"))
            {
                if (!File.Exists(fileOrId))
                    throw new ArgumentException($"Beatmap file {fileOrId} does not exist.");

                return new ProcessorWorkingBeatmap(fileOrId);
            }

            if (!int.TryParse(fileOrId, out var beatmapId))
                throw new ArgumentException("Could not parse provided beatmap ID.");

            string cachePath = Path.Combine("cache", $"{beatmapId}.osu");

            if (!File.Exists(cachePath))
            {
                Console.WriteLine($"Downloading {beatmapId}.osu...");
                new FileWebRequest(cachePath, $"https://old.ppy.sh/osu/{beatmapId}").Perform();
            }

            return new ProcessorWorkingBeatmap(cachePath, beatmapId);
        }

        protected override IBeatmap GetBeatmap() => beatmap;
        protected override Texture GetBackground() => null;
        protected override Track GetBeatmapTrack() => null;
        protected override ISkin GetSkin() => null;
        public override Stream GetStream(string storagePath) => null;
    }
}
