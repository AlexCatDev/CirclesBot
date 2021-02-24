using System;

namespace CirclesBot
{
    [Flags]
    public enum Mods
    {
        Null = -1, //Null mod (literally the absence of even Nomod)
        NM = 0, //Nomod
        NF = 1, //Nofail
        EZ = 2, //Easy
        TD = 4, //Touch device
        HD = 8, //Hidden
        HR = 16, //Hardrock
        SD = 32, //Sudden death
        DT = 64, //Double time
        RX = 128, //Relax
        HT = 256, //Half time
        NC = 512, //Nightcore Only set along with DoubleTime. i.e: NC only gives 
        FL = 1024, //Flashlight
        Auto = 2048,
        SO = 4096, //Spun out
        AP = 8192, // Autopilot
        PF = 16384, // Only set along with SuddenDeath. i.e: PF only gives 16416  
        Key4 = 32768,
        Key5 = 65536,
        Key6 = 131072,
        Key7 = 262144,
        Key8 = 524288,
        FadeIn = 1048576,
        Random = 2097152,
        Cinema = 4194304,
        Target = 8388608,
        Key9 = 16777216,
        KeyCoop = 33554432,
        Key1 = 67108864,
        Key3 = 134217728,
        Key2 = 268435456,
        ScoreV2 = 536870912,
        Mirror = 1073741824,
        KeyMod = Key1 | Key2 | Key3 | Key4 | Key5 | Key6 | Key7 | Key8 | Key9 | KeyCoop,
        FreeModAllowed = NF | EZ | HD | HR | SD | FL | FadeIn | RX | AP | SO | KeyMod,
        ScoreIncreaseMods = HD | HR | DT | FL | FadeIn
    }
}
