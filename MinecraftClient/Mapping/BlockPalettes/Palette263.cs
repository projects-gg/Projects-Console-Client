using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public class Palette263 : BlockPalette
    {
        private static readonly Dictionary<int, Material> materials = new();

        static Palette263()
        {
            for (int i = 0; i <= 0; i++)
                materials[i] = Material.Air;
            for (int i = 1; i <= 1; i++)
                materials[i] = Material.Stone;
            for (int i = 2; i <= 2; i++)
                materials[i] = Material.Granite;
            for (int i = 3; i <= 3; i++)
                materials[i] = Material.PolishedGranite;
            for (int i = 4; i <= 4; i++)
                materials[i] = Material.Diorite;
            for (int i = 5; i <= 5; i++)
                materials[i] = Material.PolishedDiorite;
            for (int i = 6; i <= 6; i++)
                materials[i] = Material.Andesite;
            for (int i = 7; i <= 7; i++)
                materials[i] = Material.PolishedAndesite;
            for (int i = 8; i <= 9; i++)
                materials[i] = Material.GrassBlock;
            for (int i = 10; i <= 10; i++)
                materials[i] = Material.Dirt;
            for (int i = 11; i <= 11; i++)
                materials[i] = Material.CoarseDirt;
            for (int i = 12; i <= 13; i++)
                materials[i] = Material.Podzol;
            for (int i = 14; i <= 14; i++)
                materials[i] = Material.Cobblestone;
            for (int i = 15; i <= 15; i++)
                materials[i] = Material.OakPlanks;
            for (int i = 16; i <= 16; i++)
                materials[i] = Material.SprucePlanks;
            for (int i = 17; i <= 17; i++)
                materials[i] = Material.BirchPlanks;
            for (int i = 18; i <= 18; i++)
                materials[i] = Material.JunglePlanks;
            for (int i = 19; i <= 19; i++)
                materials[i] = Material.AcaciaPlanks;
            for (int i = 20; i <= 20; i++)
                materials[i] = Material.CherryPlanks;
            for (int i = 21; i <= 21; i++)
                materials[i] = Material.DarkOakPlanks;
            for (int i = 22; i <= 24; i++)
                materials[i] = Material.PaleOakWood;
            for (int i = 25; i <= 25; i++)
                materials[i] = Material.PaleOakPlanks;
            for (int i = 26; i <= 26; i++)
                materials[i] = Material.MangrovePlanks;
            for (int i = 27; i <= 27; i++)
                materials[i] = Material.PoplarPlanks;
            for (int i = 28; i <= 28; i++)
                materials[i] = Material.BambooPlanks;
            for (int i = 29; i <= 29; i++)
                materials[i] = Material.BambooMosaic;
            for (int i = 30; i <= 31; i++)
                materials[i] = Material.OakSapling;
            for (int i = 32; i <= 33; i++)
                materials[i] = Material.SpruceSapling;
            for (int i = 34; i <= 35; i++)
                materials[i] = Material.BirchSapling;
            for (int i = 36; i <= 37; i++)
                materials[i] = Material.JungleSapling;
            for (int i = 38; i <= 39; i++)
                materials[i] = Material.AcaciaSapling;
            for (int i = 40; i <= 41; i++)
                materials[i] = Material.CherrySapling;
            for (int i = 42; i <= 43; i++)
                materials[i] = Material.DarkOakSapling;
            for (int i = 44; i <= 45; i++)
                materials[i] = Material.PaleOakSapling;
            for (int i = 46; i <= 85; i++)
                materials[i] = Material.MangrovePropagule;
            for (int i = 86; i <= 87; i++)
                materials[i] = Material.PoplarSapling;
            for (int i = 88; i <= 88; i++)
                materials[i] = Material.Bedrock;
            for (int i = 89; i <= 104; i++)
                materials[i] = Material.Water;
            for (int i = 105; i <= 120; i++)
                materials[i] = Material.Lava;
            for (int i = 121; i <= 121; i++)
                materials[i] = Material.Sand;
            for (int i = 122; i <= 125; i++)
                materials[i] = Material.SuspiciousSand;
            for (int i = 126; i <= 126; i++)
                materials[i] = Material.RedSand;
            for (int i = 127; i <= 127; i++)
                materials[i] = Material.Gravel;
            for (int i = 128; i <= 131; i++)
                materials[i] = Material.SuspiciousGravel;
            for (int i = 132; i <= 132; i++)
                materials[i] = Material.GoldOre;
            for (int i = 133; i <= 133; i++)
                materials[i] = Material.DeepslateGoldOre;
            for (int i = 134; i <= 134; i++)
                materials[i] = Material.IronOre;
            for (int i = 135; i <= 135; i++)
                materials[i] = Material.DeepslateIronOre;
            for (int i = 136; i <= 136; i++)
                materials[i] = Material.CoalOre;
            for (int i = 137; i <= 137; i++)
                materials[i] = Material.DeepslateCoalOre;
            for (int i = 138; i <= 138; i++)
                materials[i] = Material.NetherGoldOre;
            for (int i = 139; i <= 141; i++)
                materials[i] = Material.OakLog;
            for (int i = 142; i <= 144; i++)
                materials[i] = Material.SpruceLog;
            for (int i = 145; i <= 147; i++)
                materials[i] = Material.BirchLog;
            for (int i = 148; i <= 150; i++)
                materials[i] = Material.JungleLog;
            for (int i = 151; i <= 153; i++)
                materials[i] = Material.AcaciaLog;
            for (int i = 154; i <= 156; i++)
                materials[i] = Material.CherryLog;
            for (int i = 157; i <= 159; i++)
                materials[i] = Material.DarkOakLog;
            for (int i = 160; i <= 162; i++)
                materials[i] = Material.PaleOakLog;
            for (int i = 163; i <= 165; i++)
                materials[i] = Material.MangroveLog;
            for (int i = 166; i <= 168; i++)
                materials[i] = Material.PoplarLog;
            for (int i = 169; i <= 170; i++)
                materials[i] = Material.MangroveRoots;
            for (int i = 171; i <= 173; i++)
                materials[i] = Material.MuddyMangroveRoots;
            for (int i = 174; i <= 176; i++)
                materials[i] = Material.BambooBlock;
            for (int i = 177; i <= 179; i++)
                materials[i] = Material.StrippedSpruceLog;
            for (int i = 180; i <= 182; i++)
                materials[i] = Material.StrippedBirchLog;
            for (int i = 183; i <= 185; i++)
                materials[i] = Material.StrippedJungleLog;
            for (int i = 186; i <= 188; i++)
                materials[i] = Material.StrippedAcaciaLog;
            for (int i = 189; i <= 191; i++)
                materials[i] = Material.StrippedCherryLog;
            for (int i = 192; i <= 194; i++)
                materials[i] = Material.StrippedDarkOakLog;
            for (int i = 195; i <= 197; i++)
                materials[i] = Material.StrippedPaleOakLog;
            for (int i = 198; i <= 200; i++)
                materials[i] = Material.StrippedOakLog;
            for (int i = 201; i <= 203; i++)
                materials[i] = Material.StrippedMangroveLog;
            for (int i = 204; i <= 206; i++)
                materials[i] = Material.StrippedPoplarLog;
            for (int i = 207; i <= 209; i++)
                materials[i] = Material.StrippedBambooBlock;
            for (int i = 210; i <= 212; i++)
                materials[i] = Material.OakWood;
            for (int i = 213; i <= 215; i++)
                materials[i] = Material.SpruceWood;
            for (int i = 216; i <= 218; i++)
                materials[i] = Material.BirchWood;
            for (int i = 219; i <= 221; i++)
                materials[i] = Material.JungleWood;
            for (int i = 222; i <= 224; i++)
                materials[i] = Material.AcaciaWood;
            for (int i = 225; i <= 227; i++)
                materials[i] = Material.CherryWood;
            for (int i = 228; i <= 230; i++)
                materials[i] = Material.DarkOakWood;
            for (int i = 231; i <= 233; i++)
                materials[i] = Material.MangroveWood;
            for (int i = 234; i <= 236; i++)
                materials[i] = Material.PoplarWood;
            for (int i = 237; i <= 239; i++)
                materials[i] = Material.StrippedOakWood;
            for (int i = 240; i <= 242; i++)
                materials[i] = Material.StrippedSpruceWood;
            for (int i = 243; i <= 245; i++)
                materials[i] = Material.StrippedBirchWood;
            for (int i = 246; i <= 248; i++)
                materials[i] = Material.StrippedJungleWood;
            for (int i = 249; i <= 251; i++)
                materials[i] = Material.StrippedAcaciaWood;
            for (int i = 252; i <= 254; i++)
                materials[i] = Material.StrippedCherryWood;
            for (int i = 255; i <= 257; i++)
                materials[i] = Material.StrippedDarkOakWood;
            for (int i = 258; i <= 260; i++)
                materials[i] = Material.StrippedPaleOakWood;
            for (int i = 261; i <= 263; i++)
                materials[i] = Material.StrippedMangroveWood;
            for (int i = 264; i <= 266; i++)
                materials[i] = Material.StrippedPoplarWood;
            for (int i = 267; i <= 294; i++)
                materials[i] = Material.OakLeaves;
            for (int i = 295; i <= 322; i++)
                materials[i] = Material.SpruceLeaves;
            for (int i = 323; i <= 350; i++)
                materials[i] = Material.BirchLeaves;
            for (int i = 351; i <= 378; i++)
                materials[i] = Material.JungleLeaves;
            for (int i = 379; i <= 406; i++)
                materials[i] = Material.AcaciaLeaves;
            for (int i = 407; i <= 434; i++)
                materials[i] = Material.CherryLeaves;
            for (int i = 435; i <= 462; i++)
                materials[i] = Material.DarkOakLeaves;
            for (int i = 463; i <= 490; i++)
                materials[i] = Material.PaleOakLeaves;
            for (int i = 491; i <= 518; i++)
                materials[i] = Material.MangroveLeaves;
            for (int i = 519; i <= 546; i++)
                materials[i] = Material.RedPoplarLeaves;
            for (int i = 547; i <= 574; i++)
                materials[i] = Material.OrangePoplarLeaves;
            for (int i = 575; i <= 602; i++)
                materials[i] = Material.YellowPoplarLeaves;
            for (int i = 603; i <= 630; i++)
                materials[i] = Material.AzaleaLeaves;
            for (int i = 631; i <= 658; i++)
                materials[i] = Material.FloweringAzaleaLeaves;
            for (int i = 659; i <= 659; i++)
                materials[i] = Material.Sponge;
            for (int i = 660; i <= 660; i++)
                materials[i] = Material.WetSponge;
            for (int i = 661; i <= 661; i++)
                materials[i] = Material.Glass;
            for (int i = 662; i <= 662; i++)
                materials[i] = Material.LapisOre;
            for (int i = 663; i <= 663; i++)
                materials[i] = Material.DeepslateLapisOre;
            for (int i = 664; i <= 664; i++)
                materials[i] = Material.LapisBlock;
            for (int i = 665; i <= 676; i++)
                materials[i] = Material.Dispenser;
            for (int i = 677; i <= 677; i++)
                materials[i] = Material.Sandstone;
            for (int i = 678; i <= 678; i++)
                materials[i] = Material.ChiseledSandstone;
            for (int i = 679; i <= 679; i++)
                materials[i] = Material.CutSandstone;
            for (int i = 680; i <= 2029; i++)
                materials[i] = Material.NoteBlock;
            for (int i = 2030; i <= 2045; i++)
                materials[i] = Material.WhiteBed;
            for (int i = 2046; i <= 2061; i++)
                materials[i] = Material.OrangeBed;
            for (int i = 2062; i <= 2077; i++)
                materials[i] = Material.MagentaBed;
            for (int i = 2078; i <= 2093; i++)
                materials[i] = Material.LightBlueBed;
            for (int i = 2094; i <= 2109; i++)
                materials[i] = Material.YellowBed;
            for (int i = 2110; i <= 2125; i++)
                materials[i] = Material.LimeBed;
            for (int i = 2126; i <= 2141; i++)
                materials[i] = Material.PinkBed;
            for (int i = 2142; i <= 2157; i++)
                materials[i] = Material.GrayBed;
            for (int i = 2158; i <= 2173; i++)
                materials[i] = Material.LightGrayBed;
            for (int i = 2174; i <= 2189; i++)
                materials[i] = Material.CyanBed;
            for (int i = 2190; i <= 2205; i++)
                materials[i] = Material.PurpleBed;
            for (int i = 2206; i <= 2221; i++)
                materials[i] = Material.BlueBed;
            for (int i = 2222; i <= 2237; i++)
                materials[i] = Material.BrownBed;
            for (int i = 2238; i <= 2253; i++)
                materials[i] = Material.GreenBed;
            for (int i = 2254; i <= 2269; i++)
                materials[i] = Material.RedBed;
            for (int i = 2270; i <= 2285; i++)
                materials[i] = Material.BlackBed;
            for (int i = 2286; i <= 2301; i++)
                materials[i] = Material.StrawBed;
            for (int i = 2302; i <= 2325; i++)
                materials[i] = Material.PoweredRail;
            for (int i = 2326; i <= 2349; i++)
                materials[i] = Material.DetectorRail;
            for (int i = 2350; i <= 2361; i++)
                materials[i] = Material.StickyPiston;
            for (int i = 2362; i <= 2362; i++)
                materials[i] = Material.Cobweb;
            for (int i = 2363; i <= 2363; i++)
                materials[i] = Material.ShortGrass;
            for (int i = 2364; i <= 2364; i++)
                materials[i] = Material.Fern;
            for (int i = 2365; i <= 2365; i++)
                materials[i] = Material.DeadBush;
            for (int i = 2366; i <= 2366; i++)
                materials[i] = Material.Bush;
            for (int i = 2367; i <= 2367; i++)
                materials[i] = Material.RedShrub;
            for (int i = 2368; i <= 2368; i++)
                materials[i] = Material.ShortDryGrass;
            for (int i = 2369; i <= 2369; i++)
                materials[i] = Material.TallDryGrass;
            for (int i = 2370; i <= 2370; i++)
                materials[i] = Material.Seagrass;
            for (int i = 2371; i <= 2372; i++)
                materials[i] = Material.TallSeagrass;
            for (int i = 2373; i <= 2384; i++)
                materials[i] = Material.Piston;
            for (int i = 2385; i <= 2408; i++)
                materials[i] = Material.PistonHead;
            for (int i = 2409; i <= 2409; i++)
                materials[i] = Material.WhiteWool;
            for (int i = 2410; i <= 2410; i++)
                materials[i] = Material.OrangeWool;
            for (int i = 2411; i <= 2411; i++)
                materials[i] = Material.MagentaWool;
            for (int i = 2412; i <= 2412; i++)
                materials[i] = Material.LightBlueWool;
            for (int i = 2413; i <= 2413; i++)
                materials[i] = Material.YellowWool;
            for (int i = 2414; i <= 2414; i++)
                materials[i] = Material.LimeWool;
            for (int i = 2415; i <= 2415; i++)
                materials[i] = Material.PinkWool;
            for (int i = 2416; i <= 2416; i++)
                materials[i] = Material.GrayWool;
            for (int i = 2417; i <= 2417; i++)
                materials[i] = Material.LightGrayWool;
            for (int i = 2418; i <= 2418; i++)
                materials[i] = Material.CyanWool;
            for (int i = 2419; i <= 2419; i++)
                materials[i] = Material.PurpleWool;
            for (int i = 2420; i <= 2420; i++)
                materials[i] = Material.BlueWool;
            for (int i = 2421; i <= 2421; i++)
                materials[i] = Material.BrownWool;
            for (int i = 2422; i <= 2422; i++)
                materials[i] = Material.GreenWool;
            for (int i = 2423; i <= 2423; i++)
                materials[i] = Material.RedWool;
            for (int i = 2424; i <= 2424; i++)
                materials[i] = Material.BlackWool;
            for (int i = 2425; i <= 2504; i++)
                materials[i] = Material.WhiteWoolStairs;
            for (int i = 2505; i <= 2584; i++)
                materials[i] = Material.OrangeWoolStairs;
            for (int i = 2585; i <= 2664; i++)
                materials[i] = Material.MagentaWoolStairs;
            for (int i = 2665; i <= 2744; i++)
                materials[i] = Material.LightBlueWoolStairs;
            for (int i = 2745; i <= 2824; i++)
                materials[i] = Material.YellowWoolStairs;
            for (int i = 2825; i <= 2904; i++)
                materials[i] = Material.LimeWoolStairs;
            for (int i = 2905; i <= 2984; i++)
                materials[i] = Material.PinkWoolStairs;
            for (int i = 2985; i <= 3064; i++)
                materials[i] = Material.GrayWoolStairs;
            for (int i = 3065; i <= 3144; i++)
                materials[i] = Material.LightGrayWoolStairs;
            for (int i = 3145; i <= 3224; i++)
                materials[i] = Material.CyanWoolStairs;
            for (int i = 3225; i <= 3304; i++)
                materials[i] = Material.PurpleWoolStairs;
            for (int i = 3305; i <= 3384; i++)
                materials[i] = Material.BlueWoolStairs;
            for (int i = 3385; i <= 3464; i++)
                materials[i] = Material.BrownWoolStairs;
            for (int i = 3465; i <= 3544; i++)
                materials[i] = Material.GreenWoolStairs;
            for (int i = 3545; i <= 3624; i++)
                materials[i] = Material.RedWoolStairs;
            for (int i = 3625; i <= 3704; i++)
                materials[i] = Material.BlackWoolStairs;
            for (int i = 3705; i <= 3710; i++)
                materials[i] = Material.WhiteWoolSlab;
            for (int i = 3711; i <= 3716; i++)
                materials[i] = Material.OrangeWoolSlab;
            for (int i = 3717; i <= 3722; i++)
                materials[i] = Material.MagentaWoolSlab;
            for (int i = 3723; i <= 3728; i++)
                materials[i] = Material.LightBlueWoolSlab;
            for (int i = 3729; i <= 3734; i++)
                materials[i] = Material.YellowWoolSlab;
            for (int i = 3735; i <= 3740; i++)
                materials[i] = Material.LimeWoolSlab;
            for (int i = 3741; i <= 3746; i++)
                materials[i] = Material.PinkWoolSlab;
            for (int i = 3747; i <= 3752; i++)
                materials[i] = Material.GrayWoolSlab;
            for (int i = 3753; i <= 3758; i++)
                materials[i] = Material.LightGrayWoolSlab;
            for (int i = 3759; i <= 3764; i++)
                materials[i] = Material.CyanWoolSlab;
            for (int i = 3765; i <= 3770; i++)
                materials[i] = Material.PurpleWoolSlab;
            for (int i = 3771; i <= 3776; i++)
                materials[i] = Material.BlueWoolSlab;
            for (int i = 3777; i <= 3782; i++)
                materials[i] = Material.BrownWoolSlab;
            for (int i = 3783; i <= 3788; i++)
                materials[i] = Material.GreenWoolSlab;
            for (int i = 3789; i <= 3794; i++)
                materials[i] = Material.RedWoolSlab;
            for (int i = 3795; i <= 3800; i++)
                materials[i] = Material.BlackWoolSlab;
            for (int i = 3801; i <= 3812; i++)
                materials[i] = Material.MovingPiston;
            for (int i = 3813; i <= 3813; i++)
                materials[i] = Material.Dandelion;
            for (int i = 3814; i <= 3814; i++)
                materials[i] = Material.GoldenDandelion;
            for (int i = 3815; i <= 3815; i++)
                materials[i] = Material.Torchflower;
            for (int i = 3816; i <= 3816; i++)
                materials[i] = Material.Poppy;
            for (int i = 3817; i <= 3817; i++)
                materials[i] = Material.BlueOrchid;
            for (int i = 3818; i <= 3818; i++)
                materials[i] = Material.Allium;
            for (int i = 3819; i <= 3819; i++)
                materials[i] = Material.AzureBluet;
            for (int i = 3820; i <= 3820; i++)
                materials[i] = Material.RedTulip;
            for (int i = 3821; i <= 3821; i++)
                materials[i] = Material.OrangeTulip;
            for (int i = 3822; i <= 3822; i++)
                materials[i] = Material.WhiteTulip;
            for (int i = 3823; i <= 3823; i++)
                materials[i] = Material.PinkTulip;
            for (int i = 3824; i <= 3824; i++)
                materials[i] = Material.OxeyeDaisy;
            for (int i = 3825; i <= 3825; i++)
                materials[i] = Material.Cornflower;
            for (int i = 3826; i <= 3826; i++)
                materials[i] = Material.WitherRose;
            for (int i = 3827; i <= 3827; i++)
                materials[i] = Material.LilyOfTheValley;
            for (int i = 3828; i <= 3828; i++)
                materials[i] = Material.BrownMushroom;
            for (int i = 3829; i <= 3829; i++)
                materials[i] = Material.RedMushroom;
            for (int i = 3830; i <= 3830; i++)
                materials[i] = Material.GoldBlock;
            for (int i = 3831; i <= 3831; i++)
                materials[i] = Material.IronBlock;
            for (int i = 3832; i <= 3832; i++)
                materials[i] = Material.Bricks;
            for (int i = 3833; i <= 3834; i++)
                materials[i] = Material.Tnt;
            for (int i = 3835; i <= 3835; i++)
                materials[i] = Material.Bookshelf;
            for (int i = 3836; i <= 4091; i++)
                materials[i] = Material.ChiseledBookshelf;
            for (int i = 4092; i <= 4155; i++)
                materials[i] = Material.AcaciaShelf;
            for (int i = 4156; i <= 4219; i++)
                materials[i] = Material.BambooShelf;
            for (int i = 4220; i <= 4283; i++)
                materials[i] = Material.BirchShelf;
            for (int i = 4284; i <= 4347; i++)
                materials[i] = Material.CherryShelf;
            for (int i = 4348; i <= 4411; i++)
                materials[i] = Material.CrimsonShelf;
            for (int i = 4412; i <= 4475; i++)
                materials[i] = Material.DarkOakShelf;
            for (int i = 4476; i <= 4539; i++)
                materials[i] = Material.JungleShelf;
            for (int i = 4540; i <= 4603; i++)
                materials[i] = Material.MangroveShelf;
            for (int i = 4604; i <= 4667; i++)
                materials[i] = Material.OakShelf;
            for (int i = 4668; i <= 4731; i++)
                materials[i] = Material.PaleOakShelf;
            for (int i = 4732; i <= 4795; i++)
                materials[i] = Material.PoplarShelf;
            for (int i = 4796; i <= 4859; i++)
                materials[i] = Material.SpruceShelf;
            for (int i = 4860; i <= 4923; i++)
                materials[i] = Material.WarpedShelf;
            for (int i = 4924; i <= 4924; i++)
                materials[i] = Material.MossyCobblestone;
            for (int i = 4925; i <= 4925; i++)
                materials[i] = Material.Obsidian;
            for (int i = 4926; i <= 4926; i++)
                materials[i] = Material.Torch;
            for (int i = 4927; i <= 4930; i++)
                materials[i] = Material.WallTorch;
            for (int i = 4931; i <= 5442; i++)
                materials[i] = Material.Fire;
            for (int i = 5443; i <= 5443; i++)
                materials[i] = Material.SoulFire;
            for (int i = 5444; i <= 5444; i++)
                materials[i] = Material.Spawner;
            for (int i = 5445; i <= 5462; i++)
                materials[i] = Material.CreakingHeart;
            for (int i = 5463; i <= 5542; i++)
                materials[i] = Material.OakStairs;
            for (int i = 5543; i <= 5566; i++)
                materials[i] = Material.Chest;
            for (int i = 5567; i <= 6862; i++)
                materials[i] = Material.RedstoneWire;
            for (int i = 6863; i <= 6863; i++)
                materials[i] = Material.DiamondOre;
            for (int i = 6864; i <= 6864; i++)
                materials[i] = Material.DeepslateDiamondOre;
            for (int i = 6865; i <= 6865; i++)
                materials[i] = Material.DiamondBlock;
            for (int i = 6866; i <= 6866; i++)
                materials[i] = Material.CraftingTable;
            for (int i = 6867; i <= 6874; i++)
                materials[i] = Material.Wheat;
            for (int i = 6875; i <= 6882; i++)
                materials[i] = Material.Farmland;
            for (int i = 6883; i <= 6890; i++)
                materials[i] = Material.Furnace;
            for (int i = 6891; i <= 6922; i++)
                materials[i] = Material.OakSign;
            for (int i = 6923; i <= 6954; i++)
                materials[i] = Material.SpruceSign;
            for (int i = 6955; i <= 6986; i++)
                materials[i] = Material.BirchSign;
            for (int i = 6987; i <= 7018; i++)
                materials[i] = Material.AcaciaSign;
            for (int i = 7019; i <= 7050; i++)
                materials[i] = Material.CherrySign;
            for (int i = 7051; i <= 7082; i++)
                materials[i] = Material.JungleSign;
            for (int i = 7083; i <= 7114; i++)
                materials[i] = Material.DarkOakSign;
            for (int i = 7115; i <= 7146; i++)
                materials[i] = Material.PaleOakSign;
            for (int i = 7147; i <= 7178; i++)
                materials[i] = Material.MangroveSign;
            for (int i = 7179; i <= 7210; i++)
                materials[i] = Material.PoplarSign;
            for (int i = 7211; i <= 7242; i++)
                materials[i] = Material.BambooSign;
            for (int i = 7243; i <= 7306; i++)
                materials[i] = Material.OakDoor;
            for (int i = 7307; i <= 7314; i++)
                materials[i] = Material.Ladder;
            for (int i = 7315; i <= 7334; i++)
                materials[i] = Material.Rail;
            for (int i = 7335; i <= 7414; i++)
                materials[i] = Material.CobblestoneStairs;
            for (int i = 7415; i <= 7422; i++)
                materials[i] = Material.OakWallSign;
            for (int i = 7423; i <= 7430; i++)
                materials[i] = Material.SpruceWallSign;
            for (int i = 7431; i <= 7438; i++)
                materials[i] = Material.BirchWallSign;
            for (int i = 7439; i <= 7446; i++)
                materials[i] = Material.AcaciaWallSign;
            for (int i = 7447; i <= 7454; i++)
                materials[i] = Material.CherryWallSign;
            for (int i = 7455; i <= 7462; i++)
                materials[i] = Material.JungleWallSign;
            for (int i = 7463; i <= 7470; i++)
                materials[i] = Material.DarkOakWallSign;
            for (int i = 7471; i <= 7478; i++)
                materials[i] = Material.PaleOakWallSign;
            for (int i = 7479; i <= 7486; i++)
                materials[i] = Material.MangroveWallSign;
            for (int i = 7487; i <= 7494; i++)
                materials[i] = Material.PoplarWallSign;
            for (int i = 7495; i <= 7502; i++)
                materials[i] = Material.BambooWallSign;
            for (int i = 7503; i <= 7566; i++)
                materials[i] = Material.OakHangingSign;
            for (int i = 7567; i <= 7630; i++)
                materials[i] = Material.SpruceHangingSign;
            for (int i = 7631; i <= 7694; i++)
                materials[i] = Material.BirchHangingSign;
            for (int i = 7695; i <= 7758; i++)
                materials[i] = Material.AcaciaHangingSign;
            for (int i = 7759; i <= 7822; i++)
                materials[i] = Material.CherryHangingSign;
            for (int i = 7823; i <= 7886; i++)
                materials[i] = Material.JungleHangingSign;
            for (int i = 7887; i <= 7950; i++)
                materials[i] = Material.DarkOakHangingSign;
            for (int i = 7951; i <= 8014; i++)
                materials[i] = Material.PaleOakHangingSign;
            for (int i = 8015; i <= 8078; i++)
                materials[i] = Material.CrimsonHangingSign;
            for (int i = 8079; i <= 8142; i++)
                materials[i] = Material.WarpedHangingSign;
            for (int i = 8143; i <= 8206; i++)
                materials[i] = Material.MangroveHangingSign;
            for (int i = 8207; i <= 8270; i++)
                materials[i] = Material.PoplarHangingSign;
            for (int i = 8271; i <= 8334; i++)
                materials[i] = Material.BambooHangingSign;
            for (int i = 8335; i <= 8342; i++)
                materials[i] = Material.OakWallHangingSign;
            for (int i = 8343; i <= 8350; i++)
                materials[i] = Material.SpruceWallHangingSign;
            for (int i = 8351; i <= 8358; i++)
                materials[i] = Material.BirchWallHangingSign;
            for (int i = 8359; i <= 8366; i++)
                materials[i] = Material.AcaciaWallHangingSign;
            for (int i = 8367; i <= 8374; i++)
                materials[i] = Material.CherryWallHangingSign;
            for (int i = 8375; i <= 8382; i++)
                materials[i] = Material.JungleWallHangingSign;
            for (int i = 8383; i <= 8390; i++)
                materials[i] = Material.DarkOakWallHangingSign;
            for (int i = 8391; i <= 8398; i++)
                materials[i] = Material.PaleOakWallHangingSign;
            for (int i = 8399; i <= 8406; i++)
                materials[i] = Material.MangroveWallHangingSign;
            for (int i = 8407; i <= 8414; i++)
                materials[i] = Material.PoplarWallHangingSign;
            for (int i = 8415; i <= 8422; i++)
                materials[i] = Material.CrimsonWallHangingSign;
            for (int i = 8423; i <= 8430; i++)
                materials[i] = Material.WarpedWallHangingSign;
            for (int i = 8431; i <= 8438; i++)
                materials[i] = Material.BambooWallHangingSign;
            for (int i = 8439; i <= 8462; i++)
                materials[i] = Material.Lever;
            for (int i = 8463; i <= 8464; i++)
                materials[i] = Material.StonePressurePlate;
            for (int i = 8465; i <= 8528; i++)
                materials[i] = Material.IronDoor;
            for (int i = 8529; i <= 8530; i++)
                materials[i] = Material.OakPressurePlate;
            for (int i = 8531; i <= 8532; i++)
                materials[i] = Material.SprucePressurePlate;
            for (int i = 8533; i <= 8534; i++)
                materials[i] = Material.BirchPressurePlate;
            for (int i = 8535; i <= 8536; i++)
                materials[i] = Material.JunglePressurePlate;
            for (int i = 8537; i <= 8538; i++)
                materials[i] = Material.AcaciaPressurePlate;
            for (int i = 8539; i <= 8540; i++)
                materials[i] = Material.CherryPressurePlate;
            for (int i = 8541; i <= 8542; i++)
                materials[i] = Material.DarkOakPressurePlate;
            for (int i = 8543; i <= 8544; i++)
                materials[i] = Material.PaleOakPressurePlate;
            for (int i = 8545; i <= 8546; i++)
                materials[i] = Material.MangrovePressurePlate;
            for (int i = 8547; i <= 8548; i++)
                materials[i] = Material.PoplarPressurePlate;
            for (int i = 8549; i <= 8550; i++)
                materials[i] = Material.BambooPressurePlate;
            for (int i = 8551; i <= 8552; i++)
                materials[i] = Material.RedstoneOre;
            for (int i = 8553; i <= 8554; i++)
                materials[i] = Material.DeepslateRedstoneOre;
            for (int i = 8555; i <= 8556; i++)
                materials[i] = Material.RedstoneTorch;
            for (int i = 8557; i <= 8564; i++)
                materials[i] = Material.RedstoneWallTorch;
            for (int i = 8565; i <= 8588; i++)
                materials[i] = Material.StoneButton;
            for (int i = 8589; i <= 8596; i++)
                materials[i] = Material.Snow;
            for (int i = 8597; i <= 8597; i++)
                materials[i] = Material.Ice;
            for (int i = 8598; i <= 8598; i++)
                materials[i] = Material.SnowBlock;
            for (int i = 8599; i <= 8614; i++)
                materials[i] = Material.Cactus;
            for (int i = 8615; i <= 8615; i++)
                materials[i] = Material.CactusFlower;
            for (int i = 8616; i <= 8616; i++)
                materials[i] = Material.Clay;
            for (int i = 8617; i <= 8632; i++)
                materials[i] = Material.SugarCane;
            for (int i = 8633; i <= 8634; i++)
                materials[i] = Material.Jukebox;
            for (int i = 8635; i <= 8666; i++)
                materials[i] = Material.OakFence;
            for (int i = 8667; i <= 8667; i++)
                materials[i] = Material.Netherrack;
            for (int i = 8668; i <= 8668; i++)
                materials[i] = Material.SoulSand;
            for (int i = 8669; i <= 8669; i++)
                materials[i] = Material.SoulSoil;
            for (int i = 8670; i <= 8672; i++)
                materials[i] = Material.Basalt;
            for (int i = 8673; i <= 8675; i++)
                materials[i] = Material.PolishedBasalt;
            for (int i = 8676; i <= 8676; i++)
                materials[i] = Material.SoulTorch;
            for (int i = 8677; i <= 8680; i++)
                materials[i] = Material.SoulWallTorch;
            for (int i = 8681; i <= 8681; i++)
                materials[i] = Material.CopperTorch;
            for (int i = 8682; i <= 8685; i++)
                materials[i] = Material.CopperWallTorch;
            for (int i = 8686; i <= 8686; i++)
                materials[i] = Material.Glowstone;
            for (int i = 8687; i <= 8688; i++)
                materials[i] = Material.NetherPortal;
            for (int i = 8689; i <= 8692; i++)
                materials[i] = Material.CarvedPumpkin;
            for (int i = 8693; i <= 8696; i++)
                materials[i] = Material.JackOLantern;
            for (int i = 8697; i <= 8703; i++)
                materials[i] = Material.Cake;
            for (int i = 8704; i <= 8767; i++)
                materials[i] = Material.Repeater;
            for (int i = 8768; i <= 8768; i++)
                materials[i] = Material.WhiteStainedGlass;
            for (int i = 8769; i <= 8769; i++)
                materials[i] = Material.OrangeStainedGlass;
            for (int i = 8770; i <= 8770; i++)
                materials[i] = Material.MagentaStainedGlass;
            for (int i = 8771; i <= 8771; i++)
                materials[i] = Material.LightBlueStainedGlass;
            for (int i = 8772; i <= 8772; i++)
                materials[i] = Material.YellowStainedGlass;
            for (int i = 8773; i <= 8773; i++)
                materials[i] = Material.LimeStainedGlass;
            for (int i = 8774; i <= 8774; i++)
                materials[i] = Material.PinkStainedGlass;
            for (int i = 8775; i <= 8775; i++)
                materials[i] = Material.GrayStainedGlass;
            for (int i = 8776; i <= 8776; i++)
                materials[i] = Material.LightGrayStainedGlass;
            for (int i = 8777; i <= 8777; i++)
                materials[i] = Material.CyanStainedGlass;
            for (int i = 8778; i <= 8778; i++)
                materials[i] = Material.PurpleStainedGlass;
            for (int i = 8779; i <= 8779; i++)
                materials[i] = Material.BlueStainedGlass;
            for (int i = 8780; i <= 8780; i++)
                materials[i] = Material.BrownStainedGlass;
            for (int i = 8781; i <= 8781; i++)
                materials[i] = Material.GreenStainedGlass;
            for (int i = 8782; i <= 8782; i++)
                materials[i] = Material.RedStainedGlass;
            for (int i = 8783; i <= 8783; i++)
                materials[i] = Material.BlackStainedGlass;
            for (int i = 8784; i <= 8847; i++)
                materials[i] = Material.OakTrapdoor;
            for (int i = 8848; i <= 8911; i++)
                materials[i] = Material.SpruceTrapdoor;
            for (int i = 8912; i <= 8975; i++)
                materials[i] = Material.BirchTrapdoor;
            for (int i = 8976; i <= 9039; i++)
                materials[i] = Material.JungleTrapdoor;
            for (int i = 9040; i <= 9103; i++)
                materials[i] = Material.AcaciaTrapdoor;
            for (int i = 9104; i <= 9167; i++)
                materials[i] = Material.CherryTrapdoor;
            for (int i = 9168; i <= 9231; i++)
                materials[i] = Material.DarkOakTrapdoor;
            for (int i = 9232; i <= 9295; i++)
                materials[i] = Material.PaleOakTrapdoor;
            for (int i = 9296; i <= 9359; i++)
                materials[i] = Material.MangroveTrapdoor;
            for (int i = 9360; i <= 9423; i++)
                materials[i] = Material.PoplarTrapdoor;
            for (int i = 9424; i <= 9487; i++)
                materials[i] = Material.BambooTrapdoor;
            for (int i = 9488; i <= 9488; i++)
                materials[i] = Material.StoneBricks;
            for (int i = 9489; i <= 9489; i++)
                materials[i] = Material.MossyStoneBricks;
            for (int i = 9490; i <= 9490; i++)
                materials[i] = Material.CrackedStoneBricks;
            for (int i = 9491; i <= 9491; i++)
                materials[i] = Material.ChiseledStoneBricks;
            for (int i = 9492; i <= 9492; i++)
                materials[i] = Material.PackedMud;
            for (int i = 9493; i <= 9493; i++)
                materials[i] = Material.MudBricks;
            for (int i = 9494; i <= 9494; i++)
                materials[i] = Material.InfestedStone;
            for (int i = 9495; i <= 9495; i++)
                materials[i] = Material.InfestedCobblestone;
            for (int i = 9496; i <= 9496; i++)
                materials[i] = Material.InfestedStoneBricks;
            for (int i = 9497; i <= 9497; i++)
                materials[i] = Material.InfestedMossyStoneBricks;
            for (int i = 9498; i <= 9498; i++)
                materials[i] = Material.InfestedCrackedStoneBricks;
            for (int i = 9499; i <= 9499; i++)
                materials[i] = Material.InfestedChiseledStoneBricks;
            for (int i = 9500; i <= 9563; i++)
                materials[i] = Material.BrownMushroomBlock;
            for (int i = 9564; i <= 9627; i++)
                materials[i] = Material.RedMushroomBlock;
            for (int i = 9628; i <= 9691; i++)
                materials[i] = Material.MushroomStem;
            for (int i = 9692; i <= 9723; i++)
                materials[i] = Material.IronBars;
            for (int i = 9724; i <= 9755; i++)
                materials[i] = Material.CopperBars;
            for (int i = 9756; i <= 9787; i++)
                materials[i] = Material.ExposedCopperBars;
            for (int i = 9788; i <= 9819; i++)
                materials[i] = Material.WeatheredCopperBars;
            for (int i = 9820; i <= 9851; i++)
                materials[i] = Material.OxidizedCopperBars;
            for (int i = 9852; i <= 9883; i++)
                materials[i] = Material.WaxedCopperBars;
            for (int i = 9884; i <= 9915; i++)
                materials[i] = Material.WaxedExposedCopperBars;
            for (int i = 9916; i <= 9947; i++)
                materials[i] = Material.WaxedWeatheredCopperBars;
            for (int i = 9948; i <= 9979; i++)
                materials[i] = Material.WaxedOxidizedCopperBars;
            for (int i = 9980; i <= 9985; i++)
                materials[i] = Material.IronChain;
            for (int i = 9986; i <= 9991; i++)
                materials[i] = Material.CopperChain;
            for (int i = 9992; i <= 9997; i++)
                materials[i] = Material.ExposedCopperChain;
            for (int i = 9998; i <= 10003; i++)
                materials[i] = Material.WeatheredCopperChain;
            for (int i = 10004; i <= 10009; i++)
                materials[i] = Material.OxidizedCopperChain;
            for (int i = 10010; i <= 10015; i++)
                materials[i] = Material.WaxedCopperChain;
            for (int i = 10016; i <= 10021; i++)
                materials[i] = Material.WaxedExposedCopperChain;
            for (int i = 10022; i <= 10027; i++)
                materials[i] = Material.WaxedWeatheredCopperChain;
            for (int i = 10028; i <= 10033; i++)
                materials[i] = Material.WaxedOxidizedCopperChain;
            for (int i = 10034; i <= 10065; i++)
                materials[i] = Material.GlassPane;
            for (int i = 10066; i <= 10066; i++)
                materials[i] = Material.Pumpkin;
            for (int i = 10067; i <= 10067; i++)
                materials[i] = Material.Melon;
            for (int i = 10068; i <= 10071; i++)
                materials[i] = Material.AttachedPumpkinStem;
            for (int i = 10072; i <= 10075; i++)
                materials[i] = Material.AttachedMelonStem;
            for (int i = 10076; i <= 10083; i++)
                materials[i] = Material.PumpkinStem;
            for (int i = 10084; i <= 10091; i++)
                materials[i] = Material.MelonStem;
            for (int i = 10092; i <= 10123; i++)
                materials[i] = Material.Vine;
            for (int i = 10124; i <= 10251; i++)
                materials[i] = Material.GlowLichen;
            for (int i = 10252; i <= 10379; i++)
                materials[i] = Material.ResinClump;
            for (int i = 10380; i <= 10411; i++)
                materials[i] = Material.OakFenceGate;
            for (int i = 10412; i <= 10491; i++)
                materials[i] = Material.BrickStairs;
            for (int i = 10492; i <= 10571; i++)
                materials[i] = Material.StoneBrickStairs;
            for (int i = 10572; i <= 10651; i++)
                materials[i] = Material.MudBrickStairs;
            for (int i = 10652; i <= 10653; i++)
                materials[i] = Material.Mycelium;
            for (int i = 10654; i <= 10654; i++)
                materials[i] = Material.LilyPad;
            for (int i = 10655; i <= 10655; i++)
                materials[i] = Material.ResinBlock;
            for (int i = 10656; i <= 10656; i++)
                materials[i] = Material.ResinBricks;
            for (int i = 10657; i <= 10736; i++)
                materials[i] = Material.ResinBrickStairs;
            for (int i = 10737; i <= 10742; i++)
                materials[i] = Material.ResinBrickSlab;
            for (int i = 10743; i <= 11066; i++)
                materials[i] = Material.ResinBrickWall;
            for (int i = 11067; i <= 11067; i++)
                materials[i] = Material.ChiseledResinBricks;
            for (int i = 11068; i <= 11068; i++)
                materials[i] = Material.NetherBricks;
            for (int i = 11069; i <= 11100; i++)
                materials[i] = Material.NetherBrickFence;
            for (int i = 11101; i <= 11180; i++)
                materials[i] = Material.NetherBrickStairs;
            for (int i = 11181; i <= 11184; i++)
                materials[i] = Material.NetherWart;
            for (int i = 11185; i <= 11185; i++)
                materials[i] = Material.EnchantingTable;
            for (int i = 11186; i <= 11193; i++)
                materials[i] = Material.BrewingStand;
            for (int i = 11194; i <= 11194; i++)
                materials[i] = Material.Cauldron;
            for (int i = 11195; i <= 11197; i++)
                materials[i] = Material.WaterCauldron;
            for (int i = 11198; i <= 11198; i++)
                materials[i] = Material.LavaCauldron;
            for (int i = 11199; i <= 11201; i++)
                materials[i] = Material.PowderSnowCauldron;
            for (int i = 11202; i <= 11202; i++)
                materials[i] = Material.EndPortal;
            for (int i = 11203; i <= 11210; i++)
                materials[i] = Material.EndPortalFrame;
            for (int i = 11211; i <= 11211; i++)
                materials[i] = Material.EndStone;
            for (int i = 11212; i <= 11212; i++)
                materials[i] = Material.DragonEgg;
            for (int i = 11213; i <= 11214; i++)
                materials[i] = Material.RedstoneLamp;
            for (int i = 11215; i <= 11226; i++)
                materials[i] = Material.Cocoa;
            for (int i = 11227; i <= 11234; i++)
                materials[i] = Material.ShelfMushroom;
            for (int i = 11235; i <= 11314; i++)
                materials[i] = Material.SandstoneStairs;
            for (int i = 11315; i <= 11315; i++)
                materials[i] = Material.EmeraldOre;
            for (int i = 11316; i <= 11316; i++)
                materials[i] = Material.DeepslateEmeraldOre;
            for (int i = 11317; i <= 11324; i++)
                materials[i] = Material.EnderChest;
            for (int i = 11325; i <= 11340; i++)
                materials[i] = Material.TripwireHook;
            for (int i = 11341; i <= 11468; i++)
                materials[i] = Material.Tripwire;
            for (int i = 11469; i <= 11469; i++)
                materials[i] = Material.EmeraldBlock;
            for (int i = 11470; i <= 11549; i++)
                materials[i] = Material.SpruceStairs;
            for (int i = 11550; i <= 11629; i++)
                materials[i] = Material.BirchStairs;
            for (int i = 11630; i <= 11709; i++)
                materials[i] = Material.JungleStairs;
            for (int i = 11710; i <= 11721; i++)
                materials[i] = Material.CommandBlock;
            for (int i = 11722; i <= 11722; i++)
                materials[i] = Material.Beacon;
            for (int i = 11723; i <= 12046; i++)
                materials[i] = Material.CobblestoneWall;
            for (int i = 12047; i <= 12370; i++)
                materials[i] = Material.MossyCobblestoneWall;
            for (int i = 12371; i <= 12371; i++)
                materials[i] = Material.FlowerPot;
            for (int i = 12372; i <= 12372; i++)
                materials[i] = Material.PottedTorchflower;
            for (int i = 12373; i <= 12373; i++)
                materials[i] = Material.PottedOakSapling;
            for (int i = 12374; i <= 12374; i++)
                materials[i] = Material.PottedSpruceSapling;
            for (int i = 12375; i <= 12375; i++)
                materials[i] = Material.PottedBirchSapling;
            for (int i = 12376; i <= 12376; i++)
                materials[i] = Material.PottedJungleSapling;
            for (int i = 12377; i <= 12377; i++)
                materials[i] = Material.PottedAcaciaSapling;
            for (int i = 12378; i <= 12378; i++)
                materials[i] = Material.PottedCherrySapling;
            for (int i = 12379; i <= 12379; i++)
                materials[i] = Material.PottedDarkOakSapling;
            for (int i = 12380; i <= 12380; i++)
                materials[i] = Material.PottedPaleOakSapling;
            for (int i = 12381; i <= 12381; i++)
                materials[i] = Material.PottedPoplarSapling;
            for (int i = 12382; i <= 12382; i++)
                materials[i] = Material.PottedMangrovePropagule;
            for (int i = 12383; i <= 12383; i++)
                materials[i] = Material.PottedFern;
            for (int i = 12384; i <= 12384; i++)
                materials[i] = Material.PottedDandelion;
            for (int i = 12385; i <= 12385; i++)
                materials[i] = Material.PottedGoldenDandelion;
            for (int i = 12386; i <= 12386; i++)
                materials[i] = Material.PottedPoppy;
            for (int i = 12387; i <= 12387; i++)
                materials[i] = Material.PottedBlueOrchid;
            for (int i = 12388; i <= 12388; i++)
                materials[i] = Material.PottedAllium;
            for (int i = 12389; i <= 12389; i++)
                materials[i] = Material.PottedAzureBluet;
            for (int i = 12390; i <= 12390; i++)
                materials[i] = Material.PottedRedTulip;
            for (int i = 12391; i <= 12391; i++)
                materials[i] = Material.PottedOrangeTulip;
            for (int i = 12392; i <= 12392; i++)
                materials[i] = Material.PottedWhiteTulip;
            for (int i = 12393; i <= 12393; i++)
                materials[i] = Material.PottedPinkTulip;
            for (int i = 12394; i <= 12394; i++)
                materials[i] = Material.PottedOxeyeDaisy;
            for (int i = 12395; i <= 12395; i++)
                materials[i] = Material.PottedCornflower;
            for (int i = 12396; i <= 12396; i++)
                materials[i] = Material.PottedLilyOfTheValley;
            for (int i = 12397; i <= 12397; i++)
                materials[i] = Material.PottedWitherRose;
            for (int i = 12398; i <= 12398; i++)
                materials[i] = Material.PottedRedMushroom;
            for (int i = 12399; i <= 12399; i++)
                materials[i] = Material.PottedBrownMushroom;
            for (int i = 12400; i <= 12400; i++)
                materials[i] = Material.PottedDeadBush;
            for (int i = 12401; i <= 12401; i++)
                materials[i] = Material.PottedCactus;
            for (int i = 12402; i <= 12409; i++)
                materials[i] = Material.Carrots;
            for (int i = 12410; i <= 12417; i++)
                materials[i] = Material.Potatoes;
            for (int i = 12418; i <= 12441; i++)
                materials[i] = Material.OakButton;
            for (int i = 12442; i <= 12465; i++)
                materials[i] = Material.SpruceButton;
            for (int i = 12466; i <= 12489; i++)
                materials[i] = Material.BirchButton;
            for (int i = 12490; i <= 12513; i++)
                materials[i] = Material.JungleButton;
            for (int i = 12514; i <= 12537; i++)
                materials[i] = Material.AcaciaButton;
            for (int i = 12538; i <= 12561; i++)
                materials[i] = Material.CherryButton;
            for (int i = 12562; i <= 12585; i++)
                materials[i] = Material.DarkOakButton;
            for (int i = 12586; i <= 12609; i++)
                materials[i] = Material.PaleOakButton;
            for (int i = 12610; i <= 12633; i++)
                materials[i] = Material.MangroveButton;
            for (int i = 12634; i <= 12657; i++)
                materials[i] = Material.PoplarButton;
            for (int i = 12658; i <= 12681; i++)
                materials[i] = Material.BambooButton;
            for (int i = 12682; i <= 12713; i++)
                materials[i] = Material.SkeletonSkull;
            for (int i = 12714; i <= 12721; i++)
                materials[i] = Material.SkeletonWallSkull;
            for (int i = 12722; i <= 12753; i++)
                materials[i] = Material.WitherSkeletonSkull;
            for (int i = 12754; i <= 12761; i++)
                materials[i] = Material.WitherSkeletonWallSkull;
            for (int i = 12762; i <= 12793; i++)
                materials[i] = Material.ZombieHead;
            for (int i = 12794; i <= 12801; i++)
                materials[i] = Material.ZombieWallHead;
            for (int i = 12802; i <= 12833; i++)
                materials[i] = Material.PlayerHead;
            for (int i = 12834; i <= 12841; i++)
                materials[i] = Material.PlayerWallHead;
            for (int i = 12842; i <= 12873; i++)
                materials[i] = Material.CreeperHead;
            for (int i = 12874; i <= 12881; i++)
                materials[i] = Material.CreeperWallHead;
            for (int i = 12882; i <= 12913; i++)
                materials[i] = Material.DragonHead;
            for (int i = 12914; i <= 12921; i++)
                materials[i] = Material.DragonWallHead;
            for (int i = 12922; i <= 12953; i++)
                materials[i] = Material.PiglinHead;
            for (int i = 12954; i <= 12961; i++)
                materials[i] = Material.PiglinWallHead;
            for (int i = 12962; i <= 12965; i++)
                materials[i] = Material.Anvil;
            for (int i = 12966; i <= 12969; i++)
                materials[i] = Material.ChippedAnvil;
            for (int i = 12970; i <= 12973; i++)
                materials[i] = Material.DamagedAnvil;
            for (int i = 12974; i <= 12997; i++)
                materials[i] = Material.TrappedChest;
            for (int i = 12998; i <= 13013; i++)
                materials[i] = Material.LightWeightedPressurePlate;
            for (int i = 13014; i <= 13029; i++)
                materials[i] = Material.HeavyWeightedPressurePlate;
            for (int i = 13030; i <= 13045; i++)
                materials[i] = Material.Comparator;
            for (int i = 13046; i <= 13077; i++)
                materials[i] = Material.DaylightDetector;
            for (int i = 13078; i <= 13078; i++)
                materials[i] = Material.RedstoneBlock;
            for (int i = 13079; i <= 13079; i++)
                materials[i] = Material.NetherQuartzOre;
            for (int i = 13080; i <= 13089; i++)
                materials[i] = Material.Hopper;
            for (int i = 13090; i <= 13090; i++)
                materials[i] = Material.QuartzBlock;
            for (int i = 13091; i <= 13091; i++)
                materials[i] = Material.ChiseledQuartzBlock;
            for (int i = 13092; i <= 13094; i++)
                materials[i] = Material.QuartzPillar;
            for (int i = 13095; i <= 13174; i++)
                materials[i] = Material.QuartzStairs;
            for (int i = 13175; i <= 13198; i++)
                materials[i] = Material.ActivatorRail;
            for (int i = 13199; i <= 13210; i++)
                materials[i] = Material.Dropper;
            for (int i = 13211; i <= 13211; i++)
                materials[i] = Material.WhiteTerracotta;
            for (int i = 13212; i <= 13212; i++)
                materials[i] = Material.OrangeTerracotta;
            for (int i = 13213; i <= 13213; i++)
                materials[i] = Material.MagentaTerracotta;
            for (int i = 13214; i <= 13214; i++)
                materials[i] = Material.LightBlueTerracotta;
            for (int i = 13215; i <= 13215; i++)
                materials[i] = Material.YellowTerracotta;
            for (int i = 13216; i <= 13216; i++)
                materials[i] = Material.LimeTerracotta;
            for (int i = 13217; i <= 13217; i++)
                materials[i] = Material.PinkTerracotta;
            for (int i = 13218; i <= 13218; i++)
                materials[i] = Material.GrayTerracotta;
            for (int i = 13219; i <= 13219; i++)
                materials[i] = Material.LightGrayTerracotta;
            for (int i = 13220; i <= 13220; i++)
                materials[i] = Material.CyanTerracotta;
            for (int i = 13221; i <= 13221; i++)
                materials[i] = Material.PurpleTerracotta;
            for (int i = 13222; i <= 13222; i++)
                materials[i] = Material.BlueTerracotta;
            for (int i = 13223; i <= 13223; i++)
                materials[i] = Material.BrownTerracotta;
            for (int i = 13224; i <= 13224; i++)
                materials[i] = Material.GreenTerracotta;
            for (int i = 13225; i <= 13225; i++)
                materials[i] = Material.RedTerracotta;
            for (int i = 13226; i <= 13226; i++)
                materials[i] = Material.BlackTerracotta;
            for (int i = 13227; i <= 13258; i++)
                materials[i] = Material.WhiteStainedGlassPane;
            for (int i = 13259; i <= 13290; i++)
                materials[i] = Material.OrangeStainedGlassPane;
            for (int i = 13291; i <= 13322; i++)
                materials[i] = Material.MagentaStainedGlassPane;
            for (int i = 13323; i <= 13354; i++)
                materials[i] = Material.LightBlueStainedGlassPane;
            for (int i = 13355; i <= 13386; i++)
                materials[i] = Material.YellowStainedGlassPane;
            for (int i = 13387; i <= 13418; i++)
                materials[i] = Material.LimeStainedGlassPane;
            for (int i = 13419; i <= 13450; i++)
                materials[i] = Material.PinkStainedGlassPane;
            for (int i = 13451; i <= 13482; i++)
                materials[i] = Material.GrayStainedGlassPane;
            for (int i = 13483; i <= 13514; i++)
                materials[i] = Material.LightGrayStainedGlassPane;
            for (int i = 13515; i <= 13546; i++)
                materials[i] = Material.CyanStainedGlassPane;
            for (int i = 13547; i <= 13578; i++)
                materials[i] = Material.PurpleStainedGlassPane;
            for (int i = 13579; i <= 13610; i++)
                materials[i] = Material.BlueStainedGlassPane;
            for (int i = 13611; i <= 13642; i++)
                materials[i] = Material.BrownStainedGlassPane;
            for (int i = 13643; i <= 13674; i++)
                materials[i] = Material.GreenStainedGlassPane;
            for (int i = 13675; i <= 13706; i++)
                materials[i] = Material.RedStainedGlassPane;
            for (int i = 13707; i <= 13738; i++)
                materials[i] = Material.BlackStainedGlassPane;
            for (int i = 13739; i <= 13818; i++)
                materials[i] = Material.AcaciaStairs;
            for (int i = 13819; i <= 13898; i++)
                materials[i] = Material.CherryStairs;
            for (int i = 13899; i <= 13978; i++)
                materials[i] = Material.DarkOakStairs;
            for (int i = 13979; i <= 14058; i++)
                materials[i] = Material.PaleOakStairs;
            for (int i = 14059; i <= 14138; i++)
                materials[i] = Material.MangroveStairs;
            for (int i = 14139; i <= 14218; i++)
                materials[i] = Material.PoplarStairs;
            for (int i = 14219; i <= 14298; i++)
                materials[i] = Material.BambooStairs;
            for (int i = 14299; i <= 14378; i++)
                materials[i] = Material.BambooMosaicStairs;
            for (int i = 14379; i <= 14379; i++)
                materials[i] = Material.SlimeBlock;
            for (int i = 14380; i <= 14381; i++)
                materials[i] = Material.Barrier;
            for (int i = 14382; i <= 14413; i++)
                materials[i] = Material.Light;
            for (int i = 14414; i <= 14477; i++)
                materials[i] = Material.IronTrapdoor;
            for (int i = 14478; i <= 14478; i++)
                materials[i] = Material.Prismarine;
            for (int i = 14479; i <= 14479; i++)
                materials[i] = Material.PrismarineBricks;
            for (int i = 14480; i <= 14480; i++)
                materials[i] = Material.DarkPrismarine;
            for (int i = 14481; i <= 14560; i++)
                materials[i] = Material.PrismarineStairs;
            for (int i = 14561; i <= 14640; i++)
                materials[i] = Material.PrismarineBrickStairs;
            for (int i = 14641; i <= 14720; i++)
                materials[i] = Material.DarkPrismarineStairs;
            for (int i = 14721; i <= 14726; i++)
                materials[i] = Material.PrismarineSlab;
            for (int i = 14727; i <= 14732; i++)
                materials[i] = Material.PrismarineBrickSlab;
            for (int i = 14733; i <= 14738; i++)
                materials[i] = Material.DarkPrismarineSlab;
            for (int i = 14739; i <= 14739; i++)
                materials[i] = Material.SeaLantern;
            for (int i = 14740; i <= 14742; i++)
                materials[i] = Material.HayBlock;
            for (int i = 14743; i <= 14743; i++)
                materials[i] = Material.WhiteCarpet;
            for (int i = 14744; i <= 14744; i++)
                materials[i] = Material.OrangeCarpet;
            for (int i = 14745; i <= 14745; i++)
                materials[i] = Material.MagentaCarpet;
            for (int i = 14746; i <= 14746; i++)
                materials[i] = Material.LightBlueCarpet;
            for (int i = 14747; i <= 14747; i++)
                materials[i] = Material.YellowCarpet;
            for (int i = 14748; i <= 14748; i++)
                materials[i] = Material.LimeCarpet;
            for (int i = 14749; i <= 14749; i++)
                materials[i] = Material.PinkCarpet;
            for (int i = 14750; i <= 14750; i++)
                materials[i] = Material.GrayCarpet;
            for (int i = 14751; i <= 14751; i++)
                materials[i] = Material.LightGrayCarpet;
            for (int i = 14752; i <= 14752; i++)
                materials[i] = Material.CyanCarpet;
            for (int i = 14753; i <= 14753; i++)
                materials[i] = Material.PurpleCarpet;
            for (int i = 14754; i <= 14754; i++)
                materials[i] = Material.BlueCarpet;
            for (int i = 14755; i <= 14755; i++)
                materials[i] = Material.BrownCarpet;
            for (int i = 14756; i <= 14756; i++)
                materials[i] = Material.GreenCarpet;
            for (int i = 14757; i <= 14757; i++)
                materials[i] = Material.RedCarpet;
            for (int i = 14758; i <= 14758; i++)
                materials[i] = Material.BlackCarpet;
            for (int i = 14759; i <= 14759; i++)
                materials[i] = Material.Terracotta;
            for (int i = 14760; i <= 14760; i++)
                materials[i] = Material.CoalBlock;
            for (int i = 14761; i <= 14761; i++)
                materials[i] = Material.PackedIce;
            for (int i = 14762; i <= 14763; i++)
                materials[i] = Material.Sunflower;
            for (int i = 14764; i <= 14765; i++)
                materials[i] = Material.Lilac;
            for (int i = 14766; i <= 14767; i++)
                materials[i] = Material.RoseBush;
            for (int i = 14768; i <= 14769; i++)
                materials[i] = Material.Peony;
            for (int i = 14770; i <= 14771; i++)
                materials[i] = Material.TallGrass;
            for (int i = 14772; i <= 14773; i++)
                materials[i] = Material.LargeFern;
            for (int i = 14774; i <= 14789; i++)
                materials[i] = Material.WhiteBanner;
            for (int i = 14790; i <= 14805; i++)
                materials[i] = Material.OrangeBanner;
            for (int i = 14806; i <= 14821; i++)
                materials[i] = Material.MagentaBanner;
            for (int i = 14822; i <= 14837; i++)
                materials[i] = Material.LightBlueBanner;
            for (int i = 14838; i <= 14853; i++)
                materials[i] = Material.YellowBanner;
            for (int i = 14854; i <= 14869; i++)
                materials[i] = Material.LimeBanner;
            for (int i = 14870; i <= 14885; i++)
                materials[i] = Material.PinkBanner;
            for (int i = 14886; i <= 14901; i++)
                materials[i] = Material.GrayBanner;
            for (int i = 14902; i <= 14917; i++)
                materials[i] = Material.LightGrayBanner;
            for (int i = 14918; i <= 14933; i++)
                materials[i] = Material.CyanBanner;
            for (int i = 14934; i <= 14949; i++)
                materials[i] = Material.PurpleBanner;
            for (int i = 14950; i <= 14965; i++)
                materials[i] = Material.BlueBanner;
            for (int i = 14966; i <= 14981; i++)
                materials[i] = Material.BrownBanner;
            for (int i = 14982; i <= 14997; i++)
                materials[i] = Material.GreenBanner;
            for (int i = 14998; i <= 15013; i++)
                materials[i] = Material.RedBanner;
            for (int i = 15014; i <= 15029; i++)
                materials[i] = Material.BlackBanner;
            for (int i = 15030; i <= 15033; i++)
                materials[i] = Material.WhiteWallBanner;
            for (int i = 15034; i <= 15037; i++)
                materials[i] = Material.OrangeWallBanner;
            for (int i = 15038; i <= 15041; i++)
                materials[i] = Material.MagentaWallBanner;
            for (int i = 15042; i <= 15045; i++)
                materials[i] = Material.LightBlueWallBanner;
            for (int i = 15046; i <= 15049; i++)
                materials[i] = Material.YellowWallBanner;
            for (int i = 15050; i <= 15053; i++)
                materials[i] = Material.LimeWallBanner;
            for (int i = 15054; i <= 15057; i++)
                materials[i] = Material.PinkWallBanner;
            for (int i = 15058; i <= 15061; i++)
                materials[i] = Material.GrayWallBanner;
            for (int i = 15062; i <= 15065; i++)
                materials[i] = Material.LightGrayWallBanner;
            for (int i = 15066; i <= 15069; i++)
                materials[i] = Material.CyanWallBanner;
            for (int i = 15070; i <= 15073; i++)
                materials[i] = Material.PurpleWallBanner;
            for (int i = 15074; i <= 15077; i++)
                materials[i] = Material.BlueWallBanner;
            for (int i = 15078; i <= 15081; i++)
                materials[i] = Material.BrownWallBanner;
            for (int i = 15082; i <= 15085; i++)
                materials[i] = Material.GreenWallBanner;
            for (int i = 15086; i <= 15089; i++)
                materials[i] = Material.RedWallBanner;
            for (int i = 15090; i <= 15093; i++)
                materials[i] = Material.BlackWallBanner;
            for (int i = 15094; i <= 15094; i++)
                materials[i] = Material.RedSandstone;
            for (int i = 15095; i <= 15095; i++)
                materials[i] = Material.ChiseledRedSandstone;
            for (int i = 15096; i <= 15096; i++)
                materials[i] = Material.CutRedSandstone;
            for (int i = 15097; i <= 15176; i++)
                materials[i] = Material.RedSandstoneStairs;
            for (int i = 15177; i <= 15182; i++)
                materials[i] = Material.OakSlab;
            for (int i = 15183; i <= 15188; i++)
                materials[i] = Material.SpruceSlab;
            for (int i = 15189; i <= 15194; i++)
                materials[i] = Material.BirchSlab;
            for (int i = 15195; i <= 15200; i++)
                materials[i] = Material.JungleSlab;
            for (int i = 15201; i <= 15206; i++)
                materials[i] = Material.AcaciaSlab;
            for (int i = 15207; i <= 15212; i++)
                materials[i] = Material.CherrySlab;
            for (int i = 15213; i <= 15218; i++)
                materials[i] = Material.DarkOakSlab;
            for (int i = 15219; i <= 15224; i++)
                materials[i] = Material.PaleOakSlab;
            for (int i = 15225; i <= 15230; i++)
                materials[i] = Material.MangroveSlab;
            for (int i = 15231; i <= 15236; i++)
                materials[i] = Material.PoplarSlab;
            for (int i = 15237; i <= 15242; i++)
                materials[i] = Material.BambooSlab;
            for (int i = 15243; i <= 15248; i++)
                materials[i] = Material.BambooMosaicSlab;
            for (int i = 15249; i <= 15254; i++)
                materials[i] = Material.StoneSlab;
            for (int i = 15255; i <= 15260; i++)
                materials[i] = Material.SandstoneSlab;
            for (int i = 15261; i <= 15266; i++)
                materials[i] = Material.CutSandstoneSlab;
            for (int i = 15267; i <= 15272; i++)
                materials[i] = Material.PetrifiedOakSlab;
            for (int i = 15273; i <= 15278; i++)
                materials[i] = Material.CobblestoneSlab;
            for (int i = 15279; i <= 15284; i++)
                materials[i] = Material.BrickSlab;
            for (int i = 15285; i <= 15290; i++)
                materials[i] = Material.StoneBrickSlab;
            for (int i = 15291; i <= 15296; i++)
                materials[i] = Material.MudBrickSlab;
            for (int i = 15297; i <= 15302; i++)
                materials[i] = Material.NetherBrickSlab;
            for (int i = 15303; i <= 15308; i++)
                materials[i] = Material.QuartzSlab;
            for (int i = 15309; i <= 15314; i++)
                materials[i] = Material.RedSandstoneSlab;
            for (int i = 15315; i <= 15320; i++)
                materials[i] = Material.CutRedSandstoneSlab;
            for (int i = 15321; i <= 15321; i++)
                materials[i] = Material.SmoothStone;
            for (int i = 15322; i <= 15327; i++)
                materials[i] = Material.SmoothStoneSlab;
            for (int i = 15328; i <= 15328; i++)
                materials[i] = Material.SmoothSandstone;
            for (int i = 15329; i <= 15329; i++)
                materials[i] = Material.SmoothQuartz;
            for (int i = 15330; i <= 15330; i++)
                materials[i] = Material.SmoothRedSandstone;
            for (int i = 15331; i <= 15362; i++)
                materials[i] = Material.SpruceFenceGate;
            for (int i = 15363; i <= 15394; i++)
                materials[i] = Material.BirchFenceGate;
            for (int i = 15395; i <= 15426; i++)
                materials[i] = Material.JungleFenceGate;
            for (int i = 15427; i <= 15458; i++)
                materials[i] = Material.AcaciaFenceGate;
            for (int i = 15459; i <= 15490; i++)
                materials[i] = Material.CherryFenceGate;
            for (int i = 15491; i <= 15522; i++)
                materials[i] = Material.DarkOakFenceGate;
            for (int i = 15523; i <= 15554; i++)
                materials[i] = Material.PaleOakFenceGate;
            for (int i = 15555; i <= 15586; i++)
                materials[i] = Material.MangroveFenceGate;
            for (int i = 15587; i <= 15618; i++)
                materials[i] = Material.PoplarFenceGate;
            for (int i = 15619; i <= 15650; i++)
                materials[i] = Material.BambooFenceGate;
            for (int i = 15651; i <= 15682; i++)
                materials[i] = Material.SpruceFence;
            for (int i = 15683; i <= 15714; i++)
                materials[i] = Material.BirchFence;
            for (int i = 15715; i <= 15746; i++)
                materials[i] = Material.JungleFence;
            for (int i = 15747; i <= 15778; i++)
                materials[i] = Material.AcaciaFence;
            for (int i = 15779; i <= 15810; i++)
                materials[i] = Material.CherryFence;
            for (int i = 15811; i <= 15842; i++)
                materials[i] = Material.DarkOakFence;
            for (int i = 15843; i <= 15874; i++)
                materials[i] = Material.PaleOakFence;
            for (int i = 15875; i <= 15906; i++)
                materials[i] = Material.MangroveFence;
            for (int i = 15907; i <= 15938; i++)
                materials[i] = Material.PoplarFence;
            for (int i = 15939; i <= 15970; i++)
                materials[i] = Material.BambooFence;
            for (int i = 15971; i <= 16034; i++)
                materials[i] = Material.SpruceDoor;
            for (int i = 16035; i <= 16098; i++)
                materials[i] = Material.BirchDoor;
            for (int i = 16099; i <= 16162; i++)
                materials[i] = Material.JungleDoor;
            for (int i = 16163; i <= 16226; i++)
                materials[i] = Material.AcaciaDoor;
            for (int i = 16227; i <= 16290; i++)
                materials[i] = Material.CherryDoor;
            for (int i = 16291; i <= 16354; i++)
                materials[i] = Material.DarkOakDoor;
            for (int i = 16355; i <= 16418; i++)
                materials[i] = Material.PaleOakDoor;
            for (int i = 16419; i <= 16482; i++)
                materials[i] = Material.MangroveDoor;
            for (int i = 16483; i <= 16546; i++)
                materials[i] = Material.PoplarDoor;
            for (int i = 16547; i <= 16610; i++)
                materials[i] = Material.BambooDoor;
            for (int i = 16611; i <= 16616; i++)
                materials[i] = Material.EndRod;
            for (int i = 16617; i <= 16680; i++)
                materials[i] = Material.ChorusPlant;
            for (int i = 16681; i <= 16686; i++)
                materials[i] = Material.ChorusFlower;
            for (int i = 16687; i <= 16687; i++)
                materials[i] = Material.PurpurBlock;
            for (int i = 16688; i <= 16693; i++)
                materials[i] = Material.PurpurSlab;
            for (int i = 16694; i <= 16696; i++)
                materials[i] = Material.PurpurPillar;
            for (int i = 16697; i <= 16776; i++)
                materials[i] = Material.PurpurStairs;
            for (int i = 16777; i <= 16777; i++)
                materials[i] = Material.EndStoneBricks;
            for (int i = 16778; i <= 16779; i++)
                materials[i] = Material.TorchflowerCrop;
            for (int i = 16780; i <= 16789; i++)
                materials[i] = Material.PitcherCrop;
            for (int i = 16790; i <= 16791; i++)
                materials[i] = Material.PitcherPlant;
            for (int i = 16792; i <= 16795; i++)
                materials[i] = Material.Beetroots;
            for (int i = 16796; i <= 16796; i++)
                materials[i] = Material.DirtPath;
            for (int i = 16797; i <= 16797; i++)
                materials[i] = Material.EndGateway;
            for (int i = 16798; i <= 16809; i++)
                materials[i] = Material.RepeatingCommandBlock;
            for (int i = 16810; i <= 16821; i++)
                materials[i] = Material.ChainCommandBlock;
            for (int i = 16822; i <= 16825; i++)
                materials[i] = Material.FrostedIce;
            for (int i = 16826; i <= 16826; i++)
                materials[i] = Material.MagmaBlock;
            for (int i = 16827; i <= 16827; i++)
                materials[i] = Material.NetherWartBlock;
            for (int i = 16828; i <= 16828; i++)
                materials[i] = Material.RedNetherBricks;
            for (int i = 16829; i <= 16831; i++)
                materials[i] = Material.BoneBlock;
            for (int i = 16832; i <= 16832; i++)
                materials[i] = Material.StructureVoid;
            for (int i = 16833; i <= 16844; i++)
                materials[i] = Material.Observer;
            for (int i = 16845; i <= 16850; i++)
                materials[i] = Material.ShulkerBox;
            for (int i = 16851; i <= 16856; i++)
                materials[i] = Material.WhiteShulkerBox;
            for (int i = 16857; i <= 16862; i++)
                materials[i] = Material.OrangeShulkerBox;
            for (int i = 16863; i <= 16868; i++)
                materials[i] = Material.MagentaShulkerBox;
            for (int i = 16869; i <= 16874; i++)
                materials[i] = Material.LightBlueShulkerBox;
            for (int i = 16875; i <= 16880; i++)
                materials[i] = Material.YellowShulkerBox;
            for (int i = 16881; i <= 16886; i++)
                materials[i] = Material.LimeShulkerBox;
            for (int i = 16887; i <= 16892; i++)
                materials[i] = Material.PinkShulkerBox;
            for (int i = 16893; i <= 16898; i++)
                materials[i] = Material.GrayShulkerBox;
            for (int i = 16899; i <= 16904; i++)
                materials[i] = Material.LightGrayShulkerBox;
            for (int i = 16905; i <= 16910; i++)
                materials[i] = Material.CyanShulkerBox;
            for (int i = 16911; i <= 16916; i++)
                materials[i] = Material.PurpleShulkerBox;
            for (int i = 16917; i <= 16922; i++)
                materials[i] = Material.BlueShulkerBox;
            for (int i = 16923; i <= 16928; i++)
                materials[i] = Material.BrownShulkerBox;
            for (int i = 16929; i <= 16934; i++)
                materials[i] = Material.GreenShulkerBox;
            for (int i = 16935; i <= 16940; i++)
                materials[i] = Material.RedShulkerBox;
            for (int i = 16941; i <= 16946; i++)
                materials[i] = Material.BlackShulkerBox;
            for (int i = 16947; i <= 16950; i++)
                materials[i] = Material.WhiteGlazedTerracotta;
            for (int i = 16951; i <= 16954; i++)
                materials[i] = Material.OrangeGlazedTerracotta;
            for (int i = 16955; i <= 16958; i++)
                materials[i] = Material.MagentaGlazedTerracotta;
            for (int i = 16959; i <= 16962; i++)
                materials[i] = Material.LightBlueGlazedTerracotta;
            for (int i = 16963; i <= 16966; i++)
                materials[i] = Material.YellowGlazedTerracotta;
            for (int i = 16967; i <= 16970; i++)
                materials[i] = Material.LimeGlazedTerracotta;
            for (int i = 16971; i <= 16974; i++)
                materials[i] = Material.PinkGlazedTerracotta;
            for (int i = 16975; i <= 16978; i++)
                materials[i] = Material.GrayGlazedTerracotta;
            for (int i = 16979; i <= 16982; i++)
                materials[i] = Material.LightGrayGlazedTerracotta;
            for (int i = 16983; i <= 16986; i++)
                materials[i] = Material.CyanGlazedTerracotta;
            for (int i = 16987; i <= 16990; i++)
                materials[i] = Material.PurpleGlazedTerracotta;
            for (int i = 16991; i <= 16994; i++)
                materials[i] = Material.BlueGlazedTerracotta;
            for (int i = 16995; i <= 16998; i++)
                materials[i] = Material.BrownGlazedTerracotta;
            for (int i = 16999; i <= 17002; i++)
                materials[i] = Material.GreenGlazedTerracotta;
            for (int i = 17003; i <= 17006; i++)
                materials[i] = Material.RedGlazedTerracotta;
            for (int i = 17007; i <= 17010; i++)
                materials[i] = Material.BlackGlazedTerracotta;
            for (int i = 17011; i <= 17011; i++)
                materials[i] = Material.WhiteConcrete;
            for (int i = 17012; i <= 17012; i++)
                materials[i] = Material.OrangeConcrete;
            for (int i = 17013; i <= 17013; i++)
                materials[i] = Material.MagentaConcrete;
            for (int i = 17014; i <= 17014; i++)
                materials[i] = Material.LightBlueConcrete;
            for (int i = 17015; i <= 17015; i++)
                materials[i] = Material.YellowConcrete;
            for (int i = 17016; i <= 17016; i++)
                materials[i] = Material.LimeConcrete;
            for (int i = 17017; i <= 17017; i++)
                materials[i] = Material.PinkConcrete;
            for (int i = 17018; i <= 17018; i++)
                materials[i] = Material.GrayConcrete;
            for (int i = 17019; i <= 17019; i++)
                materials[i] = Material.LightGrayConcrete;
            for (int i = 17020; i <= 17020; i++)
                materials[i] = Material.CyanConcrete;
            for (int i = 17021; i <= 17021; i++)
                materials[i] = Material.PurpleConcrete;
            for (int i = 17022; i <= 17022; i++)
                materials[i] = Material.BlueConcrete;
            for (int i = 17023; i <= 17023; i++)
                materials[i] = Material.BrownConcrete;
            for (int i = 17024; i <= 17024; i++)
                materials[i] = Material.GreenConcrete;
            for (int i = 17025; i <= 17025; i++)
                materials[i] = Material.RedConcrete;
            for (int i = 17026; i <= 17026; i++)
                materials[i] = Material.BlackConcrete;
            for (int i = 17027; i <= 17106; i++)
                materials[i] = Material.WhiteConcreteStairs;
            for (int i = 17107; i <= 17186; i++)
                materials[i] = Material.OrangeConcreteStairs;
            for (int i = 17187; i <= 17266; i++)
                materials[i] = Material.MagentaConcreteStairs;
            for (int i = 17267; i <= 17346; i++)
                materials[i] = Material.LightBlueConcreteStairs;
            for (int i = 17347; i <= 17426; i++)
                materials[i] = Material.YellowConcreteStairs;
            for (int i = 17427; i <= 17506; i++)
                materials[i] = Material.LimeConcreteStairs;
            for (int i = 17507; i <= 17586; i++)
                materials[i] = Material.PinkConcreteStairs;
            for (int i = 17587; i <= 17666; i++)
                materials[i] = Material.GrayConcreteStairs;
            for (int i = 17667; i <= 17746; i++)
                materials[i] = Material.LightGrayConcreteStairs;
            for (int i = 17747; i <= 17826; i++)
                materials[i] = Material.CyanConcreteStairs;
            for (int i = 17827; i <= 17906; i++)
                materials[i] = Material.PurpleConcreteStairs;
            for (int i = 17907; i <= 17986; i++)
                materials[i] = Material.BlueConcreteStairs;
            for (int i = 17987; i <= 18066; i++)
                materials[i] = Material.BrownConcreteStairs;
            for (int i = 18067; i <= 18146; i++)
                materials[i] = Material.GreenConcreteStairs;
            for (int i = 18147; i <= 18226; i++)
                materials[i] = Material.RedConcreteStairs;
            for (int i = 18227; i <= 18306; i++)
                materials[i] = Material.BlackConcreteStairs;
            for (int i = 18307; i <= 18312; i++)
                materials[i] = Material.WhiteConcreteSlab;
            for (int i = 18313; i <= 18318; i++)
                materials[i] = Material.OrangeConcreteSlab;
            for (int i = 18319; i <= 18324; i++)
                materials[i] = Material.MagentaConcreteSlab;
            for (int i = 18325; i <= 18330; i++)
                materials[i] = Material.LightBlueConcreteSlab;
            for (int i = 18331; i <= 18336; i++)
                materials[i] = Material.YellowConcreteSlab;
            for (int i = 18337; i <= 18342; i++)
                materials[i] = Material.LimeConcreteSlab;
            for (int i = 18343; i <= 18348; i++)
                materials[i] = Material.PinkConcreteSlab;
            for (int i = 18349; i <= 18354; i++)
                materials[i] = Material.GrayConcreteSlab;
            for (int i = 18355; i <= 18360; i++)
                materials[i] = Material.LightGrayConcreteSlab;
            for (int i = 18361; i <= 18366; i++)
                materials[i] = Material.CyanConcreteSlab;
            for (int i = 18367; i <= 18372; i++)
                materials[i] = Material.PurpleConcreteSlab;
            for (int i = 18373; i <= 18378; i++)
                materials[i] = Material.BlueConcreteSlab;
            for (int i = 18379; i <= 18384; i++)
                materials[i] = Material.BrownConcreteSlab;
            for (int i = 18385; i <= 18390; i++)
                materials[i] = Material.GreenConcreteSlab;
            for (int i = 18391; i <= 18396; i++)
                materials[i] = Material.RedConcreteSlab;
            for (int i = 18397; i <= 18402; i++)
                materials[i] = Material.BlackConcreteSlab;
            for (int i = 18403; i <= 18403; i++)
                materials[i] = Material.WhiteConcretePowder;
            for (int i = 18404; i <= 18404; i++)
                materials[i] = Material.OrangeConcretePowder;
            for (int i = 18405; i <= 18405; i++)
                materials[i] = Material.MagentaConcretePowder;
            for (int i = 18406; i <= 18406; i++)
                materials[i] = Material.LightBlueConcretePowder;
            for (int i = 18407; i <= 18407; i++)
                materials[i] = Material.YellowConcretePowder;
            for (int i = 18408; i <= 18408; i++)
                materials[i] = Material.LimeConcretePowder;
            for (int i = 18409; i <= 18409; i++)
                materials[i] = Material.PinkConcretePowder;
            for (int i = 18410; i <= 18410; i++)
                materials[i] = Material.GrayConcretePowder;
            for (int i = 18411; i <= 18411; i++)
                materials[i] = Material.LightGrayConcretePowder;
            for (int i = 18412; i <= 18412; i++)
                materials[i] = Material.CyanConcretePowder;
            for (int i = 18413; i <= 18413; i++)
                materials[i] = Material.PurpleConcretePowder;
            for (int i = 18414; i <= 18414; i++)
                materials[i] = Material.BlueConcretePowder;
            for (int i = 18415; i <= 18415; i++)
                materials[i] = Material.BrownConcretePowder;
            for (int i = 18416; i <= 18416; i++)
                materials[i] = Material.GreenConcretePowder;
            for (int i = 18417; i <= 18417; i++)
                materials[i] = Material.RedConcretePowder;
            for (int i = 18418; i <= 18418; i++)
                materials[i] = Material.BlackConcretePowder;
            for (int i = 18419; i <= 18444; i++)
                materials[i] = Material.Kelp;
            for (int i = 18445; i <= 18445; i++)
                materials[i] = Material.KelpPlant;
            for (int i = 18446; i <= 18446; i++)
                materials[i] = Material.DriedKelpBlock;
            for (int i = 18447; i <= 18458; i++)
                materials[i] = Material.TurtleEgg;
            for (int i = 18459; i <= 18461; i++)
                materials[i] = Material.SnifferEgg;
            for (int i = 18462; i <= 18493; i++)
                materials[i] = Material.DriedGhast;
            for (int i = 18494; i <= 18494; i++)
                materials[i] = Material.DeadTubeCoralBlock;
            for (int i = 18495; i <= 18495; i++)
                materials[i] = Material.DeadBrainCoralBlock;
            for (int i = 18496; i <= 18496; i++)
                materials[i] = Material.DeadBubbleCoralBlock;
            for (int i = 18497; i <= 18497; i++)
                materials[i] = Material.DeadFireCoralBlock;
            for (int i = 18498; i <= 18498; i++)
                materials[i] = Material.DeadHornCoralBlock;
            for (int i = 18499; i <= 18499; i++)
                materials[i] = Material.TubeCoralBlock;
            for (int i = 18500; i <= 18500; i++)
                materials[i] = Material.BrainCoralBlock;
            for (int i = 18501; i <= 18501; i++)
                materials[i] = Material.BubbleCoralBlock;
            for (int i = 18502; i <= 18502; i++)
                materials[i] = Material.FireCoralBlock;
            for (int i = 18503; i <= 18503; i++)
                materials[i] = Material.HornCoralBlock;
            for (int i = 18504; i <= 18505; i++)
                materials[i] = Material.DeadTubeCoral;
            for (int i = 18506; i <= 18507; i++)
                materials[i] = Material.DeadBrainCoral;
            for (int i = 18508; i <= 18509; i++)
                materials[i] = Material.DeadBubbleCoral;
            for (int i = 18510; i <= 18511; i++)
                materials[i] = Material.DeadFireCoral;
            for (int i = 18512; i <= 18513; i++)
                materials[i] = Material.DeadHornCoral;
            for (int i = 18514; i <= 18515; i++)
                materials[i] = Material.TubeCoral;
            for (int i = 18516; i <= 18517; i++)
                materials[i] = Material.BrainCoral;
            for (int i = 18518; i <= 18519; i++)
                materials[i] = Material.BubbleCoral;
            for (int i = 18520; i <= 18521; i++)
                materials[i] = Material.FireCoral;
            for (int i = 18522; i <= 18523; i++)
                materials[i] = Material.HornCoral;
            for (int i = 18524; i <= 18525; i++)
                materials[i] = Material.DeadTubeCoralFan;
            for (int i = 18526; i <= 18527; i++)
                materials[i] = Material.DeadBrainCoralFan;
            for (int i = 18528; i <= 18529; i++)
                materials[i] = Material.DeadBubbleCoralFan;
            for (int i = 18530; i <= 18531; i++)
                materials[i] = Material.DeadFireCoralFan;
            for (int i = 18532; i <= 18533; i++)
                materials[i] = Material.DeadHornCoralFan;
            for (int i = 18534; i <= 18535; i++)
                materials[i] = Material.TubeCoralFan;
            for (int i = 18536; i <= 18537; i++)
                materials[i] = Material.BrainCoralFan;
            for (int i = 18538; i <= 18539; i++)
                materials[i] = Material.BubbleCoralFan;
            for (int i = 18540; i <= 18541; i++)
                materials[i] = Material.FireCoralFan;
            for (int i = 18542; i <= 18543; i++)
                materials[i] = Material.HornCoralFan;
            for (int i = 18544; i <= 18551; i++)
                materials[i] = Material.DeadTubeCoralWallFan;
            for (int i = 18552; i <= 18559; i++)
                materials[i] = Material.DeadBrainCoralWallFan;
            for (int i = 18560; i <= 18567; i++)
                materials[i] = Material.DeadBubbleCoralWallFan;
            for (int i = 18568; i <= 18575; i++)
                materials[i] = Material.DeadFireCoralWallFan;
            for (int i = 18576; i <= 18583; i++)
                materials[i] = Material.DeadHornCoralWallFan;
            for (int i = 18584; i <= 18591; i++)
                materials[i] = Material.TubeCoralWallFan;
            for (int i = 18592; i <= 18599; i++)
                materials[i] = Material.BrainCoralWallFan;
            for (int i = 18600; i <= 18607; i++)
                materials[i] = Material.BubbleCoralWallFan;
            for (int i = 18608; i <= 18615; i++)
                materials[i] = Material.FireCoralWallFan;
            for (int i = 18616; i <= 18623; i++)
                materials[i] = Material.HornCoralWallFan;
            for (int i = 18624; i <= 18631; i++)
                materials[i] = Material.SeaPickle;
            for (int i = 18632; i <= 18632; i++)
                materials[i] = Material.BlueIce;
            for (int i = 18633; i <= 18634; i++)
                materials[i] = Material.Conduit;
            for (int i = 18635; i <= 18635; i++)
                materials[i] = Material.BambooSapling;
            for (int i = 18636; i <= 18647; i++)
                materials[i] = Material.Bamboo;
            for (int i = 18648; i <= 18648; i++)
                materials[i] = Material.PottedBamboo;
            for (int i = 18649; i <= 18649; i++)
                materials[i] = Material.VoidAir;
            for (int i = 18650; i <= 18650; i++)
                materials[i] = Material.CaveAir;
            for (int i = 18651; i <= 18652; i++)
                materials[i] = Material.BubbleColumn;
            for (int i = 18653; i <= 18732; i++)
                materials[i] = Material.PolishedGraniteStairs;
            for (int i = 18733; i <= 18812; i++)
                materials[i] = Material.SmoothRedSandstoneStairs;
            for (int i = 18813; i <= 18892; i++)
                materials[i] = Material.MossyStoneBrickStairs;
            for (int i = 18893; i <= 18972; i++)
                materials[i] = Material.PolishedDioriteStairs;
            for (int i = 18973; i <= 19052; i++)
                materials[i] = Material.MossyCobblestoneStairs;
            for (int i = 19053; i <= 19132; i++)
                materials[i] = Material.EndStoneBrickStairs;
            for (int i = 19133; i <= 19212; i++)
                materials[i] = Material.StoneStairs;
            for (int i = 19213; i <= 19292; i++)
                materials[i] = Material.SmoothSandstoneStairs;
            for (int i = 19293; i <= 19372; i++)
                materials[i] = Material.SmoothQuartzStairs;
            for (int i = 19373; i <= 19452; i++)
                materials[i] = Material.GraniteStairs;
            for (int i = 19453; i <= 19532; i++)
                materials[i] = Material.AndesiteStairs;
            for (int i = 19533; i <= 19612; i++)
                materials[i] = Material.RedNetherBrickStairs;
            for (int i = 19613; i <= 19692; i++)
                materials[i] = Material.PolishedAndesiteStairs;
            for (int i = 19693; i <= 19772; i++)
                materials[i] = Material.DioriteStairs;
            for (int i = 19773; i <= 19778; i++)
                materials[i] = Material.PolishedGraniteSlab;
            for (int i = 19779; i <= 19784; i++)
                materials[i] = Material.SmoothRedSandstoneSlab;
            for (int i = 19785; i <= 19790; i++)
                materials[i] = Material.MossyStoneBrickSlab;
            for (int i = 19791; i <= 19796; i++)
                materials[i] = Material.PolishedDioriteSlab;
            for (int i = 19797; i <= 19802; i++)
                materials[i] = Material.MossyCobblestoneSlab;
            for (int i = 19803; i <= 19808; i++)
                materials[i] = Material.EndStoneBrickSlab;
            for (int i = 19809; i <= 19814; i++)
                materials[i] = Material.SmoothSandstoneSlab;
            for (int i = 19815; i <= 19820; i++)
                materials[i] = Material.SmoothQuartzSlab;
            for (int i = 19821; i <= 19826; i++)
                materials[i] = Material.GraniteSlab;
            for (int i = 19827; i <= 19832; i++)
                materials[i] = Material.AndesiteSlab;
            for (int i = 19833; i <= 19838; i++)
                materials[i] = Material.RedNetherBrickSlab;
            for (int i = 19839; i <= 19844; i++)
                materials[i] = Material.PolishedAndesiteSlab;
            for (int i = 19845; i <= 19850; i++)
                materials[i] = Material.DioriteSlab;
            for (int i = 19851; i <= 20174; i++)
                materials[i] = Material.BrickWall;
            for (int i = 20175; i <= 20498; i++)
                materials[i] = Material.PrismarineWall;
            for (int i = 20499; i <= 20822; i++)
                materials[i] = Material.RedSandstoneWall;
            for (int i = 20823; i <= 21146; i++)
                materials[i] = Material.MossyStoneBrickWall;
            for (int i = 21147; i <= 21470; i++)
                materials[i] = Material.GraniteWall;
            for (int i = 21471; i <= 21794; i++)
                materials[i] = Material.StoneBrickWall;
            for (int i = 21795; i <= 22118; i++)
                materials[i] = Material.MudBrickWall;
            for (int i = 22119; i <= 22442; i++)
                materials[i] = Material.NetherBrickWall;
            for (int i = 22443; i <= 22766; i++)
                materials[i] = Material.AndesiteWall;
            for (int i = 22767; i <= 23090; i++)
                materials[i] = Material.RedNetherBrickWall;
            for (int i = 23091; i <= 23414; i++)
                materials[i] = Material.SandstoneWall;
            for (int i = 23415; i <= 23738; i++)
                materials[i] = Material.EndStoneBrickWall;
            for (int i = 23739; i <= 24062; i++)
                materials[i] = Material.DioriteWall;
            for (int i = 24063; i <= 24094; i++)
                materials[i] = Material.Scaffolding;
            for (int i = 24095; i <= 24098; i++)
                materials[i] = Material.Loom;
            for (int i = 24099; i <= 24110; i++)
                materials[i] = Material.Barrel;
            for (int i = 24111; i <= 24118; i++)
                materials[i] = Material.Smoker;
            for (int i = 24119; i <= 24126; i++)
                materials[i] = Material.BlastFurnace;
            for (int i = 24127; i <= 24127; i++)
                materials[i] = Material.CartographyTable;
            for (int i = 24128; i <= 24128; i++)
                materials[i] = Material.FletchingTable;
            for (int i = 24129; i <= 24140; i++)
                materials[i] = Material.Grindstone;
            for (int i = 24141; i <= 24156; i++)
                materials[i] = Material.Lectern;
            for (int i = 24157; i <= 24157; i++)
                materials[i] = Material.SmithingTable;
            for (int i = 24158; i <= 24161; i++)
                materials[i] = Material.Stonecutter;
            for (int i = 24162; i <= 24193; i++)
                materials[i] = Material.Bell;
            for (int i = 24194; i <= 24197; i++)
                materials[i] = Material.Lantern;
            for (int i = 24198; i <= 24201; i++)
                materials[i] = Material.SoulLantern;
            for (int i = 24202; i <= 24205; i++)
                materials[i] = Material.CopperLantern;
            for (int i = 24206; i <= 24209; i++)
                materials[i] = Material.ExposedCopperLantern;
            for (int i = 24210; i <= 24213; i++)
                materials[i] = Material.WeatheredCopperLantern;
            for (int i = 24214; i <= 24217; i++)
                materials[i] = Material.OxidizedCopperLantern;
            for (int i = 24218; i <= 24221; i++)
                materials[i] = Material.WaxedCopperLantern;
            for (int i = 24222; i <= 24225; i++)
                materials[i] = Material.WaxedExposedCopperLantern;
            for (int i = 24226; i <= 24229; i++)
                materials[i] = Material.WaxedWeatheredCopperLantern;
            for (int i = 24230; i <= 24233; i++)
                materials[i] = Material.WaxedOxidizedCopperLantern;
            for (int i = 24234; i <= 24265; i++)
                materials[i] = Material.Campfire;
            for (int i = 24266; i <= 24297; i++)
                materials[i] = Material.SoulCampfire;
            for (int i = 24298; i <= 24301; i++)
                materials[i] = Material.SweetBerryBush;
            for (int i = 24302; i <= 24304; i++)
                materials[i] = Material.WarpedStem;
            for (int i = 24305; i <= 24307; i++)
                materials[i] = Material.StrippedWarpedStem;
            for (int i = 24308; i <= 24310; i++)
                materials[i] = Material.WarpedHyphae;
            for (int i = 24311; i <= 24313; i++)
                materials[i] = Material.StrippedWarpedHyphae;
            for (int i = 24314; i <= 24314; i++)
                materials[i] = Material.WarpedNylium;
            for (int i = 24315; i <= 24315; i++)
                materials[i] = Material.WarpedFungus;
            for (int i = 24316; i <= 24316; i++)
                materials[i] = Material.WarpedWartBlock;
            for (int i = 24317; i <= 24317; i++)
                materials[i] = Material.WarpedRoots;
            for (int i = 24318; i <= 24318; i++)
                materials[i] = Material.NetherSprouts;
            for (int i = 24319; i <= 24321; i++)
                materials[i] = Material.CrimsonStem;
            for (int i = 24322; i <= 24324; i++)
                materials[i] = Material.StrippedCrimsonStem;
            for (int i = 24325; i <= 24327; i++)
                materials[i] = Material.CrimsonHyphae;
            for (int i = 24328; i <= 24330; i++)
                materials[i] = Material.StrippedCrimsonHyphae;
            for (int i = 24331; i <= 24331; i++)
                materials[i] = Material.CrimsonNylium;
            for (int i = 24332; i <= 24332; i++)
                materials[i] = Material.CrimsonFungus;
            for (int i = 24333; i <= 24333; i++)
                materials[i] = Material.Shroomlight;
            for (int i = 24334; i <= 24359; i++)
                materials[i] = Material.WeepingVines;
            for (int i = 24360; i <= 24360; i++)
                materials[i] = Material.WeepingVinesPlant;
            for (int i = 24361; i <= 24386; i++)
                materials[i] = Material.TwistingVines;
            for (int i = 24387; i <= 24387; i++)
                materials[i] = Material.TwistingVinesPlant;
            for (int i = 24388; i <= 24388; i++)
                materials[i] = Material.CrimsonRoots;
            for (int i = 24389; i <= 24389; i++)
                materials[i] = Material.CrimsonPlanks;
            for (int i = 24390; i <= 24390; i++)
                materials[i] = Material.WarpedPlanks;
            for (int i = 24391; i <= 24396; i++)
                materials[i] = Material.CrimsonSlab;
            for (int i = 24397; i <= 24402; i++)
                materials[i] = Material.WarpedSlab;
            for (int i = 24403; i <= 24404; i++)
                materials[i] = Material.CrimsonPressurePlate;
            for (int i = 24405; i <= 24406; i++)
                materials[i] = Material.WarpedPressurePlate;
            for (int i = 24407; i <= 24438; i++)
                materials[i] = Material.CrimsonFence;
            for (int i = 24439; i <= 24470; i++)
                materials[i] = Material.WarpedFence;
            for (int i = 24471; i <= 24534; i++)
                materials[i] = Material.CrimsonTrapdoor;
            for (int i = 24535; i <= 24598; i++)
                materials[i] = Material.WarpedTrapdoor;
            for (int i = 24599; i <= 24630; i++)
                materials[i] = Material.CrimsonFenceGate;
            for (int i = 24631; i <= 24662; i++)
                materials[i] = Material.WarpedFenceGate;
            for (int i = 24663; i <= 24742; i++)
                materials[i] = Material.CrimsonStairs;
            for (int i = 24743; i <= 24822; i++)
                materials[i] = Material.WarpedStairs;
            for (int i = 24823; i <= 24846; i++)
                materials[i] = Material.CrimsonButton;
            for (int i = 24847; i <= 24870; i++)
                materials[i] = Material.WarpedButton;
            for (int i = 24871; i <= 24934; i++)
                materials[i] = Material.CrimsonDoor;
            for (int i = 24935; i <= 24998; i++)
                materials[i] = Material.WarpedDoor;
            for (int i = 24999; i <= 25030; i++)
                materials[i] = Material.CrimsonSign;
            for (int i = 25031; i <= 25062; i++)
                materials[i] = Material.WarpedSign;
            for (int i = 25063; i <= 25070; i++)
                materials[i] = Material.CrimsonWallSign;
            for (int i = 25071; i <= 25078; i++)
                materials[i] = Material.WarpedWallSign;
            for (int i = 25079; i <= 25082; i++)
                materials[i] = Material.StructureBlock;
            for (int i = 25083; i <= 25094; i++)
                materials[i] = Material.Jigsaw;
            for (int i = 25095; i <= 25098; i++)
                materials[i] = Material.TestBlock;
            for (int i = 25099; i <= 25099; i++)
                materials[i] = Material.TestInstanceBlock;
            for (int i = 25100; i <= 25108; i++)
                materials[i] = Material.Composter;
            for (int i = 25109; i <= 25124; i++)
                materials[i] = Material.Target;
            for (int i = 25125; i <= 25148; i++)
                materials[i] = Material.BeeNest;
            for (int i = 25149; i <= 25172; i++)
                materials[i] = Material.Beehive;
            for (int i = 25173; i <= 25173; i++)
                materials[i] = Material.HoneyBlock;
            for (int i = 25174; i <= 25174; i++)
                materials[i] = Material.HoneycombBlock;
            for (int i = 25175; i <= 25175; i++)
                materials[i] = Material.NetheriteBlock;
            for (int i = 25176; i <= 25176; i++)
                materials[i] = Material.AncientDebris;
            for (int i = 25177; i <= 25177; i++)
                materials[i] = Material.CryingObsidian;
            for (int i = 25178; i <= 25182; i++)
                materials[i] = Material.RespawnAnchor;
            for (int i = 25183; i <= 25183; i++)
                materials[i] = Material.PottedCrimsonFungus;
            for (int i = 25184; i <= 25184; i++)
                materials[i] = Material.PottedWarpedFungus;
            for (int i = 25185; i <= 25185; i++)
                materials[i] = Material.PottedCrimsonRoots;
            for (int i = 25186; i <= 25186; i++)
                materials[i] = Material.PottedWarpedRoots;
            for (int i = 25187; i <= 25187; i++)
                materials[i] = Material.Lodestone;
            for (int i = 25188; i <= 25188; i++)
                materials[i] = Material.Blackstone;
            for (int i = 25189; i <= 25268; i++)
                materials[i] = Material.BlackstoneStairs;
            for (int i = 25269; i <= 25592; i++)
                materials[i] = Material.BlackstoneWall;
            for (int i = 25593; i <= 25598; i++)
                materials[i] = Material.BlackstoneSlab;
            for (int i = 25599; i <= 25599; i++)
                materials[i] = Material.PolishedBlackstone;
            for (int i = 25600; i <= 25600; i++)
                materials[i] = Material.PolishedBlackstoneBricks;
            for (int i = 25601; i <= 25601; i++)
                materials[i] = Material.CrackedPolishedBlackstoneBricks;
            for (int i = 25602; i <= 25602; i++)
                materials[i] = Material.ChiseledPolishedBlackstone;
            for (int i = 25603; i <= 25608; i++)
                materials[i] = Material.PolishedBlackstoneBrickSlab;
            for (int i = 25609; i <= 25688; i++)
                materials[i] = Material.PolishedBlackstoneBrickStairs;
            for (int i = 25689; i <= 26012; i++)
                materials[i] = Material.PolishedBlackstoneBrickWall;
            for (int i = 26013; i <= 26013; i++)
                materials[i] = Material.GildedBlackstone;
            for (int i = 26014; i <= 26093; i++)
                materials[i] = Material.PolishedBlackstoneStairs;
            for (int i = 26094; i <= 26099; i++)
                materials[i] = Material.PolishedBlackstoneSlab;
            for (int i = 26100; i <= 26101; i++)
                materials[i] = Material.PolishedBlackstonePressurePlate;
            for (int i = 26102; i <= 26125; i++)
                materials[i] = Material.PolishedBlackstoneButton;
            for (int i = 26126; i <= 26449; i++)
                materials[i] = Material.PolishedBlackstoneWall;
            for (int i = 26450; i <= 26450; i++)
                materials[i] = Material.ChiseledNetherBricks;
            for (int i = 26451; i <= 26451; i++)
                materials[i] = Material.CrackedNetherBricks;
            for (int i = 26452; i <= 26452; i++)
                materials[i] = Material.QuartzBricks;
            for (int i = 26453; i <= 26468; i++)
                materials[i] = Material.Candle;
            for (int i = 26469; i <= 26484; i++)
                materials[i] = Material.WhiteCandle;
            for (int i = 26485; i <= 26500; i++)
                materials[i] = Material.OrangeCandle;
            for (int i = 26501; i <= 26516; i++)
                materials[i] = Material.MagentaCandle;
            for (int i = 26517; i <= 26532; i++)
                materials[i] = Material.LightBlueCandle;
            for (int i = 26533; i <= 26548; i++)
                materials[i] = Material.YellowCandle;
            for (int i = 26549; i <= 26564; i++)
                materials[i] = Material.LimeCandle;
            for (int i = 26565; i <= 26580; i++)
                materials[i] = Material.PinkCandle;
            for (int i = 26581; i <= 26596; i++)
                materials[i] = Material.GrayCandle;
            for (int i = 26597; i <= 26612; i++)
                materials[i] = Material.LightGrayCandle;
            for (int i = 26613; i <= 26628; i++)
                materials[i] = Material.CyanCandle;
            for (int i = 26629; i <= 26644; i++)
                materials[i] = Material.PurpleCandle;
            for (int i = 26645; i <= 26660; i++)
                materials[i] = Material.BlueCandle;
            for (int i = 26661; i <= 26676; i++)
                materials[i] = Material.BrownCandle;
            for (int i = 26677; i <= 26692; i++)
                materials[i] = Material.GreenCandle;
            for (int i = 26693; i <= 26708; i++)
                materials[i] = Material.RedCandle;
            for (int i = 26709; i <= 26724; i++)
                materials[i] = Material.BlackCandle;
            for (int i = 26725; i <= 26726; i++)
                materials[i] = Material.CandleCake;
            for (int i = 26727; i <= 26728; i++)
                materials[i] = Material.WhiteCandleCake;
            for (int i = 26729; i <= 26730; i++)
                materials[i] = Material.OrangeCandleCake;
            for (int i = 26731; i <= 26732; i++)
                materials[i] = Material.MagentaCandleCake;
            for (int i = 26733; i <= 26734; i++)
                materials[i] = Material.LightBlueCandleCake;
            for (int i = 26735; i <= 26736; i++)
                materials[i] = Material.YellowCandleCake;
            for (int i = 26737; i <= 26738; i++)
                materials[i] = Material.LimeCandleCake;
            for (int i = 26739; i <= 26740; i++)
                materials[i] = Material.PinkCandleCake;
            for (int i = 26741; i <= 26742; i++)
                materials[i] = Material.GrayCandleCake;
            for (int i = 26743; i <= 26744; i++)
                materials[i] = Material.LightGrayCandleCake;
            for (int i = 26745; i <= 26746; i++)
                materials[i] = Material.CyanCandleCake;
            for (int i = 26747; i <= 26748; i++)
                materials[i] = Material.PurpleCandleCake;
            for (int i = 26749; i <= 26750; i++)
                materials[i] = Material.BlueCandleCake;
            for (int i = 26751; i <= 26752; i++)
                materials[i] = Material.BrownCandleCake;
            for (int i = 26753; i <= 26754; i++)
                materials[i] = Material.GreenCandleCake;
            for (int i = 26755; i <= 26756; i++)
                materials[i] = Material.RedCandleCake;
            for (int i = 26757; i <= 26758; i++)
                materials[i] = Material.BlackCandleCake;
            for (int i = 26759; i <= 26759; i++)
                materials[i] = Material.AmethystBlock;
            for (int i = 26760; i <= 26760; i++)
                materials[i] = Material.BuddingAmethyst;
            for (int i = 26761; i <= 26772; i++)
                materials[i] = Material.AmethystCluster;
            for (int i = 26773; i <= 26784; i++)
                materials[i] = Material.LargeAmethystBud;
            for (int i = 26785; i <= 26796; i++)
                materials[i] = Material.MediumAmethystBud;
            for (int i = 26797; i <= 26808; i++)
                materials[i] = Material.SmallAmethystBud;
            for (int i = 26809; i <= 26809; i++)
                materials[i] = Material.Tuff;
            for (int i = 26810; i <= 26815; i++)
                materials[i] = Material.TuffSlab;
            for (int i = 26816; i <= 26895; i++)
                materials[i] = Material.TuffStairs;
            for (int i = 26896; i <= 27219; i++)
                materials[i] = Material.TuffWall;
            for (int i = 27220; i <= 27220; i++)
                materials[i] = Material.PolishedTuff;
            for (int i = 27221; i <= 27226; i++)
                materials[i] = Material.PolishedTuffSlab;
            for (int i = 27227; i <= 27306; i++)
                materials[i] = Material.PolishedTuffStairs;
            for (int i = 27307; i <= 27630; i++)
                materials[i] = Material.PolishedTuffWall;
            for (int i = 27631; i <= 27631; i++)
                materials[i] = Material.ChiseledTuff;
            for (int i = 27632; i <= 27632; i++)
                materials[i] = Material.TuffBricks;
            for (int i = 27633; i <= 27638; i++)
                materials[i] = Material.TuffBrickSlab;
            for (int i = 27639; i <= 27718; i++)
                materials[i] = Material.TuffBrickStairs;
            for (int i = 27719; i <= 28042; i++)
                materials[i] = Material.TuffBrickWall;
            for (int i = 28043; i <= 28043; i++)
                materials[i] = Material.ChiseledTuffBricks;
            for (int i = 28044; i <= 28044; i++)
                materials[i] = Material.Sulfur;
            for (int i = 28045; i <= 28049; i++)
                materials[i] = Material.PotentSulfur;
            for (int i = 28050; i <= 28055; i++)
                materials[i] = Material.SulfurSlab;
            for (int i = 28056; i <= 28135; i++)
                materials[i] = Material.SulfurStairs;
            for (int i = 28136; i <= 28459; i++)
                materials[i] = Material.SulfurWall;
            for (int i = 28460; i <= 28460; i++)
                materials[i] = Material.PolishedSulfur;
            for (int i = 28461; i <= 28466; i++)
                materials[i] = Material.PolishedSulfurSlab;
            for (int i = 28467; i <= 28546; i++)
                materials[i] = Material.PolishedSulfurStairs;
            for (int i = 28547; i <= 28870; i++)
                materials[i] = Material.PolishedSulfurWall;
            for (int i = 28871; i <= 28871; i++)
                materials[i] = Material.SulfurBricks;
            for (int i = 28872; i <= 28877; i++)
                materials[i] = Material.SulfurBrickSlab;
            for (int i = 28878; i <= 28957; i++)
                materials[i] = Material.SulfurBrickStairs;
            for (int i = 28958; i <= 29281; i++)
                materials[i] = Material.SulfurBrickWall;
            for (int i = 29282; i <= 29282; i++)
                materials[i] = Material.ChiseledSulfur;
            for (int i = 29283; i <= 29283; i++)
                materials[i] = Material.Cinnabar;
            for (int i = 29284; i <= 29289; i++)
                materials[i] = Material.CinnabarSlab;
            for (int i = 29290; i <= 29369; i++)
                materials[i] = Material.CinnabarStairs;
            for (int i = 29370; i <= 29693; i++)
                materials[i] = Material.CinnabarWall;
            for (int i = 29694; i <= 29694; i++)
                materials[i] = Material.PolishedCinnabar;
            for (int i = 29695; i <= 29700; i++)
                materials[i] = Material.PolishedCinnabarSlab;
            for (int i = 29701; i <= 29780; i++)
                materials[i] = Material.PolishedCinnabarStairs;
            for (int i = 29781; i <= 30104; i++)
                materials[i] = Material.PolishedCinnabarWall;
            for (int i = 30105; i <= 30105; i++)
                materials[i] = Material.CinnabarBricks;
            for (int i = 30106; i <= 30111; i++)
                materials[i] = Material.CinnabarBrickSlab;
            for (int i = 30112; i <= 30191; i++)
                materials[i] = Material.CinnabarBrickStairs;
            for (int i = 30192; i <= 30515; i++)
                materials[i] = Material.CinnabarBrickWall;
            for (int i = 30516; i <= 30516; i++)
                materials[i] = Material.ChiseledCinnabar;
            for (int i = 30517; i <= 30517; i++)
                materials[i] = Material.Calcite;
            for (int i = 30518; i <= 30518; i++)
                materials[i] = Material.TintedGlass;
            for (int i = 30519; i <= 30519; i++)
                materials[i] = Material.PowderSnow;
            for (int i = 30520; i <= 30615; i++)
                materials[i] = Material.SculkSensor;
            for (int i = 30616; i <= 30999; i++)
                materials[i] = Material.CalibratedSculkSensor;
            for (int i = 31000; i <= 31000; i++)
                materials[i] = Material.Sculk;
            for (int i = 31001; i <= 31128; i++)
                materials[i] = Material.SculkVein;
            for (int i = 31129; i <= 31130; i++)
                materials[i] = Material.SculkCatalyst;
            for (int i = 31131; i <= 31138; i++)
                materials[i] = Material.SculkShrieker;
            for (int i = 31139; i <= 31139; i++)
                materials[i] = Material.CopperBlock;
            for (int i = 31140; i <= 31140; i++)
                materials[i] = Material.ExposedCopper;
            for (int i = 31141; i <= 31141; i++)
                materials[i] = Material.WeatheredCopper;
            for (int i = 31142; i <= 31142; i++)
                materials[i] = Material.OxidizedCopper;
            for (int i = 31143; i <= 31143; i++)
                materials[i] = Material.WaxedCopperBlock;
            for (int i = 31144; i <= 31144; i++)
                materials[i] = Material.WaxedExposedCopper;
            for (int i = 31145; i <= 31145; i++)
                materials[i] = Material.WaxedWeatheredCopper;
            for (int i = 31146; i <= 31146; i++)
                materials[i] = Material.WaxedOxidizedCopper;
            for (int i = 31147; i <= 31147; i++)
                materials[i] = Material.CopperOre;
            for (int i = 31148; i <= 31148; i++)
                materials[i] = Material.DeepslateCopperOre;
            for (int i = 31149; i <= 31149; i++)
                materials[i] = Material.CutCopper;
            for (int i = 31150; i <= 31150; i++)
                materials[i] = Material.ExposedCutCopper;
            for (int i = 31151; i <= 31151; i++)
                materials[i] = Material.WeatheredCutCopper;
            for (int i = 31152; i <= 31152; i++)
                materials[i] = Material.OxidizedCutCopper;
            for (int i = 31153; i <= 31153; i++)
                materials[i] = Material.WaxedCutCopper;
            for (int i = 31154; i <= 31154; i++)
                materials[i] = Material.WaxedExposedCutCopper;
            for (int i = 31155; i <= 31155; i++)
                materials[i] = Material.WaxedWeatheredCutCopper;
            for (int i = 31156; i <= 31156; i++)
                materials[i] = Material.WaxedOxidizedCutCopper;
            for (int i = 31157; i <= 31157; i++)
                materials[i] = Material.ChiseledCopper;
            for (int i = 31158; i <= 31158; i++)
                materials[i] = Material.ExposedChiseledCopper;
            for (int i = 31159; i <= 31159; i++)
                materials[i] = Material.WeatheredChiseledCopper;
            for (int i = 31160; i <= 31160; i++)
                materials[i] = Material.OxidizedChiseledCopper;
            for (int i = 31161; i <= 31161; i++)
                materials[i] = Material.WaxedChiseledCopper;
            for (int i = 31162; i <= 31162; i++)
                materials[i] = Material.WaxedExposedChiseledCopper;
            for (int i = 31163; i <= 31163; i++)
                materials[i] = Material.WaxedWeatheredChiseledCopper;
            for (int i = 31164; i <= 31164; i++)
                materials[i] = Material.WaxedOxidizedChiseledCopper;
            for (int i = 31165; i <= 31244; i++)
                materials[i] = Material.CutCopperStairs;
            for (int i = 31245; i <= 31324; i++)
                materials[i] = Material.ExposedCutCopperStairs;
            for (int i = 31325; i <= 31404; i++)
                materials[i] = Material.WeatheredCutCopperStairs;
            for (int i = 31405; i <= 31484; i++)
                materials[i] = Material.OxidizedCutCopperStairs;
            for (int i = 31485; i <= 31564; i++)
                materials[i] = Material.WaxedCutCopperStairs;
            for (int i = 31565; i <= 31644; i++)
                materials[i] = Material.WaxedExposedCutCopperStairs;
            for (int i = 31645; i <= 31724; i++)
                materials[i] = Material.WaxedWeatheredCutCopperStairs;
            for (int i = 31725; i <= 31804; i++)
                materials[i] = Material.WaxedOxidizedCutCopperStairs;
            for (int i = 31805; i <= 31810; i++)
                materials[i] = Material.CutCopperSlab;
            for (int i = 31811; i <= 31816; i++)
                materials[i] = Material.ExposedCutCopperSlab;
            for (int i = 31817; i <= 31822; i++)
                materials[i] = Material.WeatheredCutCopperSlab;
            for (int i = 31823; i <= 31828; i++)
                materials[i] = Material.OxidizedCutCopperSlab;
            for (int i = 31829; i <= 31834; i++)
                materials[i] = Material.WaxedCutCopperSlab;
            for (int i = 31835; i <= 31840; i++)
                materials[i] = Material.WaxedExposedCutCopperSlab;
            for (int i = 31841; i <= 31846; i++)
                materials[i] = Material.WaxedWeatheredCutCopperSlab;
            for (int i = 31847; i <= 31852; i++)
                materials[i] = Material.WaxedOxidizedCutCopperSlab;
            for (int i = 31853; i <= 31916; i++)
                materials[i] = Material.CopperDoor;
            for (int i = 31917; i <= 31980; i++)
                materials[i] = Material.ExposedCopperDoor;
            for (int i = 31981; i <= 32044; i++)
                materials[i] = Material.WeatheredCopperDoor;
            for (int i = 32045; i <= 32108; i++)
                materials[i] = Material.OxidizedCopperDoor;
            for (int i = 32109; i <= 32172; i++)
                materials[i] = Material.WaxedCopperDoor;
            for (int i = 32173; i <= 32236; i++)
                materials[i] = Material.WaxedExposedCopperDoor;
            for (int i = 32237; i <= 32300; i++)
                materials[i] = Material.WaxedWeatheredCopperDoor;
            for (int i = 32301; i <= 32364; i++)
                materials[i] = Material.WaxedOxidizedCopperDoor;
            for (int i = 32365; i <= 32428; i++)
                materials[i] = Material.CopperTrapdoor;
            for (int i = 32429; i <= 32492; i++)
                materials[i] = Material.ExposedCopperTrapdoor;
            for (int i = 32493; i <= 32556; i++)
                materials[i] = Material.WeatheredCopperTrapdoor;
            for (int i = 32557; i <= 32620; i++)
                materials[i] = Material.OxidizedCopperTrapdoor;
            for (int i = 32621; i <= 32684; i++)
                materials[i] = Material.WaxedCopperTrapdoor;
            for (int i = 32685; i <= 32748; i++)
                materials[i] = Material.WaxedExposedCopperTrapdoor;
            for (int i = 32749; i <= 32812; i++)
                materials[i] = Material.WaxedWeatheredCopperTrapdoor;
            for (int i = 32813; i <= 32876; i++)
                materials[i] = Material.WaxedOxidizedCopperTrapdoor;
            for (int i = 32877; i <= 32878; i++)
                materials[i] = Material.CopperGrate;
            for (int i = 32879; i <= 32880; i++)
                materials[i] = Material.ExposedCopperGrate;
            for (int i = 32881; i <= 32882; i++)
                materials[i] = Material.WeatheredCopperGrate;
            for (int i = 32883; i <= 32884; i++)
                materials[i] = Material.OxidizedCopperGrate;
            for (int i = 32885; i <= 32886; i++)
                materials[i] = Material.WaxedCopperGrate;
            for (int i = 32887; i <= 32888; i++)
                materials[i] = Material.WaxedExposedCopperGrate;
            for (int i = 32889; i <= 32890; i++)
                materials[i] = Material.WaxedWeatheredCopperGrate;
            for (int i = 32891; i <= 32892; i++)
                materials[i] = Material.WaxedOxidizedCopperGrate;
            for (int i = 32893; i <= 32896; i++)
                materials[i] = Material.CopperBulb;
            for (int i = 32897; i <= 32900; i++)
                materials[i] = Material.ExposedCopperBulb;
            for (int i = 32901; i <= 32904; i++)
                materials[i] = Material.WeatheredCopperBulb;
            for (int i = 32905; i <= 32908; i++)
                materials[i] = Material.OxidizedCopperBulb;
            for (int i = 32909; i <= 32912; i++)
                materials[i] = Material.WaxedCopperBulb;
            for (int i = 32913; i <= 32916; i++)
                materials[i] = Material.WaxedExposedCopperBulb;
            for (int i = 32917; i <= 32920; i++)
                materials[i] = Material.WaxedWeatheredCopperBulb;
            for (int i = 32921; i <= 32924; i++)
                materials[i] = Material.WaxedOxidizedCopperBulb;
            for (int i = 32925; i <= 32948; i++)
                materials[i] = Material.CopperChest;
            for (int i = 32949; i <= 32972; i++)
                materials[i] = Material.ExposedCopperChest;
            for (int i = 32973; i <= 32996; i++)
                materials[i] = Material.WeatheredCopperChest;
            for (int i = 32997; i <= 33020; i++)
                materials[i] = Material.OxidizedCopperChest;
            for (int i = 33021; i <= 33044; i++)
                materials[i] = Material.WaxedCopperChest;
            for (int i = 33045; i <= 33068; i++)
                materials[i] = Material.WaxedExposedCopperChest;
            for (int i = 33069; i <= 33092; i++)
                materials[i] = Material.WaxedWeatheredCopperChest;
            for (int i = 33093; i <= 33116; i++)
                materials[i] = Material.WaxedOxidizedCopperChest;
            for (int i = 33117; i <= 33148; i++)
                materials[i] = Material.CopperGolemStatue;
            for (int i = 33149; i <= 33180; i++)
                materials[i] = Material.ExposedCopperGolemStatue;
            for (int i = 33181; i <= 33212; i++)
                materials[i] = Material.WeatheredCopperGolemStatue;
            for (int i = 33213; i <= 33244; i++)
                materials[i] = Material.OxidizedCopperGolemStatue;
            for (int i = 33245; i <= 33276; i++)
                materials[i] = Material.WaxedCopperGolemStatue;
            for (int i = 33277; i <= 33308; i++)
                materials[i] = Material.WaxedExposedCopperGolemStatue;
            for (int i = 33309; i <= 33340; i++)
                materials[i] = Material.WaxedWeatheredCopperGolemStatue;
            for (int i = 33341; i <= 33372; i++)
                materials[i] = Material.WaxedOxidizedCopperGolemStatue;
            for (int i = 33373; i <= 33396; i++)
                materials[i] = Material.LightningRod;
            for (int i = 33397; i <= 33420; i++)
                materials[i] = Material.ExposedLightningRod;
            for (int i = 33421; i <= 33444; i++)
                materials[i] = Material.WeatheredLightningRod;
            for (int i = 33445; i <= 33468; i++)
                materials[i] = Material.OxidizedLightningRod;
            for (int i = 33469; i <= 33492; i++)
                materials[i] = Material.WaxedLightningRod;
            for (int i = 33493; i <= 33516; i++)
                materials[i] = Material.WaxedExposedLightningRod;
            for (int i = 33517; i <= 33540; i++)
                materials[i] = Material.WaxedWeatheredLightningRod;
            for (int i = 33541; i <= 33564; i++)
                materials[i] = Material.WaxedOxidizedLightningRod;
            for (int i = 33565; i <= 33565; i++)
                materials[i] = Material.DripstoneBlock;
            for (int i = 33566; i <= 33585; i++)
                materials[i] = Material.PointedDripstone;
            for (int i = 33586; i <= 33605; i++)
                materials[i] = Material.SulfurSpike;
            for (int i = 33606; i <= 33657; i++)
                materials[i] = Material.CaveVines;
            for (int i = 33658; i <= 33659; i++)
                materials[i] = Material.CaveVinesPlant;
            for (int i = 33660; i <= 33660; i++)
                materials[i] = Material.SporeBlossom;
            for (int i = 33661; i <= 33661; i++)
                materials[i] = Material.Azalea;
            for (int i = 33662; i <= 33662; i++)
                materials[i] = Material.FloweringAzalea;
            for (int i = 33663; i <= 33663; i++)
                materials[i] = Material.MossCarpet;
            for (int i = 33664; i <= 33679; i++)
                materials[i] = Material.PinkPetals;
            for (int i = 33680; i <= 33695; i++)
                materials[i] = Material.Wildflowers;
            for (int i = 33696; i <= 33711; i++)
                materials[i] = Material.LeafLitter;
            for (int i = 33712; i <= 33712; i++)
                materials[i] = Material.MossBlock;
            for (int i = 33713; i <= 33744; i++)
                materials[i] = Material.BigDripleaf;
            for (int i = 33745; i <= 33752; i++)
                materials[i] = Material.BigDripleafStem;
            for (int i = 33753; i <= 33768; i++)
                materials[i] = Material.SmallDripleaf;
            for (int i = 33769; i <= 33770; i++)
                materials[i] = Material.HangingRoots;
            for (int i = 33771; i <= 33771; i++)
                materials[i] = Material.RootedDirt;
            for (int i = 33772; i <= 33772; i++)
                materials[i] = Material.Mud;
            for (int i = 33773; i <= 33775; i++)
                materials[i] = Material.Deepslate;
            for (int i = 33776; i <= 33776; i++)
                materials[i] = Material.CobbledDeepslate;
            for (int i = 33777; i <= 33856; i++)
                materials[i] = Material.CobbledDeepslateStairs;
            for (int i = 33857; i <= 33862; i++)
                materials[i] = Material.CobbledDeepslateSlab;
            for (int i = 33863; i <= 34186; i++)
                materials[i] = Material.CobbledDeepslateWall;
            for (int i = 34187; i <= 34187; i++)
                materials[i] = Material.PolishedDeepslate;
            for (int i = 34188; i <= 34267; i++)
                materials[i] = Material.PolishedDeepslateStairs;
            for (int i = 34268; i <= 34273; i++)
                materials[i] = Material.PolishedDeepslateSlab;
            for (int i = 34274; i <= 34597; i++)
                materials[i] = Material.PolishedDeepslateWall;
            for (int i = 34598; i <= 34598; i++)
                materials[i] = Material.DeepslateTiles;
            for (int i = 34599; i <= 34678; i++)
                materials[i] = Material.DeepslateTileStairs;
            for (int i = 34679; i <= 34684; i++)
                materials[i] = Material.DeepslateTileSlab;
            for (int i = 34685; i <= 35008; i++)
                materials[i] = Material.DeepslateTileWall;
            for (int i = 35009; i <= 35009; i++)
                materials[i] = Material.DeepslateBricks;
            for (int i = 35010; i <= 35089; i++)
                materials[i] = Material.DeepslateBrickStairs;
            for (int i = 35090; i <= 35095; i++)
                materials[i] = Material.DeepslateBrickSlab;
            for (int i = 35096; i <= 35419; i++)
                materials[i] = Material.DeepslateBrickWall;
            for (int i = 35420; i <= 35420; i++)
                materials[i] = Material.ChiseledDeepslate;
            for (int i = 35421; i <= 35421; i++)
                materials[i] = Material.CrackedDeepslateBricks;
            for (int i = 35422; i <= 35422; i++)
                materials[i] = Material.CrackedDeepslateTiles;
            for (int i = 35423; i <= 35425; i++)
                materials[i] = Material.InfestedDeepslate;
            for (int i = 35426; i <= 35426; i++)
                materials[i] = Material.SmoothBasalt;
            for (int i = 35427; i <= 35427; i++)
                materials[i] = Material.RawIronBlock;
            for (int i = 35428; i <= 35428; i++)
                materials[i] = Material.RawCopperBlock;
            for (int i = 35429; i <= 35429; i++)
                materials[i] = Material.RawGoldBlock;
            for (int i = 35430; i <= 35430; i++)
                materials[i] = Material.PottedAzaleaBush;
            for (int i = 35431; i <= 35431; i++)
                materials[i] = Material.PottedFloweringAzaleaBush;
            for (int i = 35432; i <= 35434; i++)
                materials[i] = Material.OchreFroglight;
            for (int i = 35435; i <= 35437; i++)
                materials[i] = Material.VerdantFroglight;
            for (int i = 35438; i <= 35440; i++)
                materials[i] = Material.PearlescentFroglight;
            for (int i = 35441; i <= 35441; i++)
                materials[i] = Material.Frogspawn;
            for (int i = 35442; i <= 35442; i++)
                materials[i] = Material.ReinforcedDeepslate;
            for (int i = 35443; i <= 35458; i++)
                materials[i] = Material.DecoratedPot;
            for (int i = 35459; i <= 35506; i++)
                materials[i] = Material.Crafter;
            for (int i = 35507; i <= 35518; i++)
                materials[i] = Material.TrialSpawner;
            for (int i = 35519; i <= 35550; i++)
                materials[i] = Material.Vault;
            for (int i = 35551; i <= 35552; i++)
                materials[i] = Material.HeavyCore;
            for (int i = 35553; i <= 35553; i++)
                materials[i] = Material.PaleMossBlock;
            for (int i = 35554; i <= 35715; i++)
                materials[i] = Material.PaleMossCarpet;
            for (int i = 35716; i <= 35717; i++)
                materials[i] = Material.PaleHangingMoss;
            for (int i = 35718; i <= 35718; i++)
                materials[i] = Material.OpenEyeblossom;
            for (int i = 35719; i <= 35719; i++)
                materials[i] = Material.ClosedEyeblossom;
            for (int i = 35720; i <= 35720; i++)
                materials[i] = Material.PottedOpenEyeblossom;
            for (int i = 35721; i <= 35721; i++)
                materials[i] = Material.PottedClosedEyeblossom;
            for (int i = 35722; i <= 35722; i++)
                materials[i] = Material.FireflyBush;
        }

        // <auto-generated block-state-properties>
        private static readonly BlockStateDefinition[] stateDefinitions =
        [
            new(8, 2,
            [
                new("snowy", ["true", "false"], 1)
            ]),
            new(12, 2,
            [
                new("snowy", ["true", "false"], 1)
            ]),
            new(22, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(30, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(32, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(34, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(36, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(38, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(40, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(42, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(44, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(46, 40,
            [
                new("age", ["0", "1", "2", "3", "4"], 8),
                new("hanging", ["true", "false"], 4),
                new("stage", ["0", "1"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(86, 2,
            [
                new("stage", ["0", "1"], 1)
            ]),
            new(89, 16,
            [
                new("level", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(105, 16,
            [
                new("level", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(122, 4,
            [
                new("dusted", ["0", "1", "2", "3"], 1)
            ]),
            new(128, 4,
            [
                new("dusted", ["0", "1", "2", "3"], 1)
            ]),
            new(139, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(142, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(145, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(148, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(151, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(154, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(157, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(160, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(163, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(166, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(169, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(171, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(174, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(177, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(180, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(183, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(186, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(189, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(192, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(195, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(198, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(201, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(204, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(207, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(210, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(213, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(216, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(219, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(222, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(225, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(228, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(231, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(234, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(237, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(240, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(243, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(246, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(249, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(252, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(255, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(258, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(261, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(264, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(267, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(295, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(323, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(351, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(379, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(407, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(435, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(463, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(491, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(519, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(547, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(575, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(603, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(631, 28,
            [
                new("distance", ["1", "2", "3", "4", "5", "6", "7"], 4),
                new("persistent", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(665, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("triggered", ["true", "false"], 1)
            ]),
            new(680, 1350,
            [
                new("instrument", ["harp", "basedrum", "snare", "hat", "bass", "flute", "bell", "guitar", "chime", "xylophone", "iron_xylophone", "cow_bell", "didgeridoo", "bit", "banjo", "pling", "trumpet", "trumpet_exposed", "trumpet_oxidized", "trumpet_weathered", "zombie", "skeleton", "creeper", "dragon", "wither_skeleton", "piglin", "custom_head"], 50),
                new("note", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(2030, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2046, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2062, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2078, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2094, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2110, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2126, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2142, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2158, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2174, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2190, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2206, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2222, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2238, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2254, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2270, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2286, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("occupied", ["true", "false"], 2),
                new("part", ["head", "foot"], 1)
            ]),
            new(2302, 24,
            [
                new("powered", ["true", "false"], 12),
                new("shape", ["north_south", "east_west", "ascending_east", "ascending_west", "ascending_north", "ascending_south"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2326, 24,
            [
                new("powered", ["true", "false"], 12),
                new("shape", ["north_south", "east_west", "ascending_east", "ascending_west", "ascending_north", "ascending_south"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2350, 12,
            [
                new("extended", ["true", "false"], 6),
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(2371, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(2373, 12,
            [
                new("extended", ["true", "false"], 6),
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(2385, 24,
            [
                new("type", ["normal", "sticky"], 1),
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("short", ["true", "false"], 2)
            ]),
            new(2425, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2505, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2585, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2665, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2745, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2825, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2905, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(2985, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3065, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3145, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3225, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3305, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3385, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3465, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3545, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3625, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3705, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3711, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3717, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3723, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3729, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3735, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3741, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3747, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3753, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3759, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3765, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3771, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3777, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3783, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3789, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3795, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(3801, 12,
            [
                new("type", ["normal", "sticky"], 1),
                new("facing", ["north", "east", "south", "west", "up", "down"], 2)
            ]),
            new(3833, 2,
            [
                new("unstable", ["true", "false"], 1)
            ]),
            new(3836, 256,
            [
                new("facing", ["north", "south", "west", "east"], 64),
                new("slot_0_occupied", ["true", "false"], 32),
                new("slot_1_occupied", ["true", "false"], 16),
                new("slot_2_occupied", ["true", "false"], 8),
                new("slot_3_occupied", ["true", "false"], 4),
                new("slot_4_occupied", ["true", "false"], 2),
                new("slot_5_occupied", ["true", "false"], 1)
            ]),
            new(4092, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4156, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4220, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4284, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4348, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4412, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4476, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4540, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4604, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4668, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4732, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4796, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4860, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("powered", ["true", "false"], 8),
                new("side_chain", ["unconnected", "right", "center", "left"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(4927, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(4931, 512,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(5445, 18,
            [
                new("axis", ["x", "y", "z"], 6),
                new("creaking_heart_state", ["uprooted", "dormant", "awake"], 2),
                new("natural", ["true", "false"], 1)
            ]),
            new(5463, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(5543, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(5567, 1296,
            [
                new("east", ["up", "side", "none"], 432),
                new("north", ["up", "side", "none"], 144),
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 9),
                new("south", ["up", "side", "none"], 3),
                new("west", ["up", "side", "none"], 1)
            ]),
            new(6867, 8,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(6875, 8,
            [
                new("moisture", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(6883, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("lit", ["true", "false"], 1)
            ]),
            new(6891, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(6923, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(6955, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(6987, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7019, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7051, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7083, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7115, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7147, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7179, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7211, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7243, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(7307, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7315, 20,
            [
                new("shape", ["north_south", "east_west", "ascending_east", "ascending_west", "ascending_north", "ascending_south", "south_east", "south_west", "north_west", "north_east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7335, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7415, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7423, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7431, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7439, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7447, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7455, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7463, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7471, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7479, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7487, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7495, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7503, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7567, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7631, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7695, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7759, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7823, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7887, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(7951, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8015, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8079, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8143, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8207, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8271, 64,
            [
                new("attached", ["true", "false"], 32),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8335, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8343, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8351, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8359, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8367, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8375, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8383, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8391, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8399, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8407, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8415, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8423, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8431, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8439, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(8463, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8465, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(8529, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8531, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8533, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8535, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8537, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8539, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8541, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8543, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8545, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8547, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8549, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(8551, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(8553, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(8555, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(8557, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("lit", ["true", "false"], 1)
            ]),
            new(8565, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(8589, 8,
            [
                new("layers", ["1", "2", "3", "4", "5", "6", "7", "8"], 1)
            ]),
            new(8599, 16,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(8617, 16,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(8633, 2,
            [
                new("has_record", ["true", "false"], 1)
            ]),
            new(8635, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(8670, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(8673, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(8677, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(8682, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(8687, 2,
            [
                new("axis", ["x", "z"], 1)
            ]),
            new(8689, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(8693, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(8697, 7,
            [
                new("bites", ["0", "1", "2", "3", "4", "5", "6"], 1)
            ]),
            new(8704, 64,
            [
                new("delay", ["1", "2", "3", "4"], 16),
                new("facing", ["north", "south", "west", "east"], 4),
                new("locked", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(8784, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8848, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8912, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(8976, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9040, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9104, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9168, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9232, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9296, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9360, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9424, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9500, 64,
            [
                new("down", ["true", "false"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9564, 64,
            [
                new("down", ["true", "false"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9628, 64,
            [
                new("down", ["true", "false"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9692, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9724, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9756, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9788, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9820, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9852, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9884, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9916, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9948, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(9980, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9986, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9992, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(9998, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10004, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10010, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10016, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10022, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10028, 6,
            [
                new("axis", ["x", "y", "z"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10034, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(10068, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(10072, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(10076, 8,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(10084, 8,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(10092, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(10124, 128,
            [
                new("down", ["true", "false"], 64),
                new("east", ["true", "false"], 32),
                new("north", ["true", "false"], 16),
                new("south", ["true", "false"], 8),
                new("up", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(10252, 128,
            [
                new("down", ["true", "false"], 64),
                new("east", ["true", "false"], 32),
                new("north", ["true", "false"], 16),
                new("south", ["true", "false"], 8),
                new("up", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(10380, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(10412, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10492, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10572, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10652, 2,
            [
                new("snowy", ["true", "false"], 1)
            ]),
            new(10657, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10737, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(10743, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(11069, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(11101, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11181, 4,
            [
                new("age", ["0", "1", "2", "3"], 1)
            ]),
            new(11186, 8,
            [
                new("has_bottle_0", ["true", "false"], 4),
                new("has_bottle_1", ["true", "false"], 2),
                new("has_bottle_2", ["true", "false"], 1)
            ]),
            new(11195, 3,
            [
                new("level", ["1", "2", "3"], 1)
            ]),
            new(11199, 3,
            [
                new("level", ["1", "2", "3"], 1)
            ]),
            new(11203, 8,
            [
                new("eye", ["true", "false"], 4),
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(11213, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(11215, 12,
            [
                new("age", ["0", "1", "2"], 4),
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(11227, 8,
            [
                new("age", ["0", "1"], 4),
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(11235, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11317, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11325, 16,
            [
                new("attached", ["true", "false"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(11341, 128,
            [
                new("attached", ["true", "false"], 64),
                new("disarmed", ["true", "false"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("powered", ["true", "false"], 4),
                new("south", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(11470, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11550, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11630, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(11710, 12,
            [
                new("conditional", ["true", "false"], 6),
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(11723, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(12047, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(12402, 8,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(12410, 8,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7"], 1)
            ]),
            new(12418, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12442, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12466, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12490, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12514, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12538, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12562, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12586, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12610, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12634, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12658, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12682, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12714, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12722, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12754, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12762, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12794, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12802, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12834, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12842, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12874, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12882, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12914, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12922, 32,
            [
                new("powered", ["true", "false"], 16),
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(12954, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(12962, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(12966, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(12970, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(12974, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(12998, 16,
            [
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(13014, 16,
            [
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(13030, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("mode", ["compare", "subtract"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(13046, 32,
            [
                new("inverted", ["true", "false"], 16),
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(13080, 10,
            [
                new("enabled", ["true", "false"], 5),
                new("facing", ["down", "north", "south", "west", "east"], 1)
            ]),
            new(13092, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(13095, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(13175, 24,
            [
                new("powered", ["true", "false"], 12),
                new("shape", ["north_south", "east_west", "ascending_east", "ascending_west", "ascending_north", "ascending_south"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(13199, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("triggered", ["true", "false"], 1)
            ]),
            new(13227, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13259, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13291, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13323, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13355, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13387, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13419, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13451, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13483, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13515, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13547, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13579, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13611, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13643, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13675, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13707, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(13739, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(13819, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(13899, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(13979, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14059, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14139, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14219, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14299, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14380, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14382, 32,
            [
                new("level", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14414, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14481, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14561, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14641, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14721, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14727, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14733, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(14740, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(14762, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14764, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14766, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14768, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14770, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14772, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(14774, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14790, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14806, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14822, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14838, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14854, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14870, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14886, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14902, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14918, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14934, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14950, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14966, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14982, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(14998, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(15014, 16,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(15030, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15034, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15038, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15042, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15046, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15050, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15054, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15058, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15062, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15066, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15070, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15074, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15078, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15082, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15086, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15090, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(15097, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15177, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15183, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15189, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15195, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15201, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15207, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15213, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15219, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15225, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15231, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15237, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15243, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15249, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15255, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15261, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15267, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15273, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15279, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15285, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15291, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15297, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15303, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15309, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15315, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15322, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(15331, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15363, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15395, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15427, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15459, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15491, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15523, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15555, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15587, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15619, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(15651, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15683, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15715, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15747, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15779, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15811, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15843, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15875, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15907, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15939, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(15971, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16035, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16099, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16163, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16227, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16291, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16355, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16419, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16483, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16547, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16611, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16617, 64,
            [
                new("down", ["true", "false"], 32),
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("up", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(16681, 6,
            [
                new("age", ["0", "1", "2", "3", "4", "5"], 1)
            ]),
            new(16688, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(16694, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(16697, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(16778, 2,
            [
                new("age", ["0", "1"], 1)
            ]),
            new(16780, 10,
            [
                new("age", ["0", "1", "2", "3", "4"], 2),
                new("half", ["upper", "lower"], 1)
            ]),
            new(16790, 2,
            [
                new("half", ["upper", "lower"], 1)
            ]),
            new(16792, 4,
            [
                new("age", ["0", "1", "2", "3"], 1)
            ]),
            new(16798, 12,
            [
                new("conditional", ["true", "false"], 6),
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16810, 12,
            [
                new("conditional", ["true", "false"], 6),
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16822, 4,
            [
                new("age", ["0", "1", "2", "3"], 1)
            ]),
            new(16829, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(16833, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(16845, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16851, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16857, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16863, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16869, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16875, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16881, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16887, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16893, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16899, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16905, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16911, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16917, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16923, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16929, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16935, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16941, 6,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 1)
            ]),
            new(16947, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16951, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16955, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16959, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16963, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16967, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16971, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16975, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16979, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16983, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16987, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16991, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16995, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(16999, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(17003, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(17007, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(17027, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17107, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17187, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17267, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17347, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17427, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17507, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17587, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17667, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17747, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17827, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17907, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(17987, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18067, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18147, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18227, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18307, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18313, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18319, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18325, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18331, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18337, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18343, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18349, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18355, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18361, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18367, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18373, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18379, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18385, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18391, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18397, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18419, 26,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25"], 1)
            ]),
            new(18447, 12,
            [
                new("eggs", ["1", "2", "3", "4"], 3),
                new("hatch", ["0", "1", "2"], 1)
            ]),
            new(18459, 3,
            [
                new("hatch", ["0", "1", "2"], 1)
            ]),
            new(18462, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("hydration", ["0", "1", "2", "3"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18504, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18506, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18508, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18510, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18512, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18514, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18516, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18518, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18520, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18522, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18524, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18526, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18528, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18530, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18532, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18534, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18536, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18538, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18540, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18542, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18544, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18552, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18560, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18568, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18576, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18584, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18592, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18600, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18608, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18616, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18624, 8,
            [
                new("pickles", ["1", "2", "3", "4"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18633, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18636, 12,
            [
                new("age", ["0", "1"], 6),
                new("leaves", ["none", "small", "large"], 2),
                new("stage", ["0", "1"], 1)
            ]),
            new(18651, 2,
            [
                new("drag", ["true", "false"], 1)
            ]),
            new(18653, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18733, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18813, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18893, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(18973, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19053, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19133, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19213, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19293, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19373, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19453, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19533, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19613, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19693, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19773, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19779, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19785, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19791, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19797, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19803, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19809, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19815, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19821, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19827, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19833, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19839, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19845, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(19851, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(20175, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(20499, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(20823, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(21147, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(21471, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(21795, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(22119, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(22443, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(22767, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(23091, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(23415, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(23739, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(24063, 32,
            [
                new("bottom", ["true", "false"], 16),
                new("distance", ["0", "1", "2", "3", "4", "5", "6", "7"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24095, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(24099, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("open", ["true", "false"], 1)
            ]),
            new(24111, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("lit", ["true", "false"], 1)
            ]),
            new(24119, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("lit", ["true", "false"], 1)
            ]),
            new(24129, 12,
            [
                new("face", ["floor", "wall", "ceiling"], 4),
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(24141, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("has_book", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24158, 4,
            [
                new("facing", ["north", "south", "west", "east"], 1)
            ]),
            new(24162, 32,
            [
                new("attachment", ["floor", "ceiling", "single_wall", "double_wall"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24194, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24198, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24202, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24206, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24210, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24214, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24218, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24222, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24226, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24230, 4,
            [
                new("hanging", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24234, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("lit", ["true", "false"], 4),
                new("signal_fire", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24266, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("lit", ["true", "false"], 4),
                new("signal_fire", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24298, 4,
            [
                new("age", ["0", "1", "2", "3"], 1)
            ]),
            new(24302, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24305, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24308, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24311, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24319, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24322, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24325, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24328, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(24334, 26,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25"], 1)
            ]),
            new(24361, 26,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25"], 1)
            ]),
            new(24391, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24397, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24403, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(24405, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(24407, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(24439, 32,
            [
                new("east", ["true", "false"], 16),
                new("north", ["true", "false"], 8),
                new("south", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(24471, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24535, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24599, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24631, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("in_wall", ["true", "false"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24663, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24743, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(24823, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24847, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24871, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24935, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(24999, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25031, 32,
            [
                new("rotation", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25063, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25071, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25079, 4,
            [
                new("mode", ["save", "load", "corner", "data"], 1)
            ]),
            new(25083, 12,
            [
                new("orientation", ["down_east", "down_north", "down_south", "down_west", "up_east", "up_north", "up_south", "up_west", "west_up", "east_up", "north_up", "south_up"], 1)
            ]),
            new(25095, 4,
            [
                new("mode", ["start", "log", "fail", "accept"], 1)
            ]),
            new(25100, 9,
            [
                new("level", ["0", "1", "2", "3", "4", "5", "6", "7", "8"], 1)
            ]),
            new(25109, 16,
            [
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 1)
            ]),
            new(25125, 24,
            [
                new("facing", ["north", "south", "west", "east"], 6),
                new("honey_level", ["0", "1", "2", "3", "4", "5"], 1)
            ]),
            new(25149, 24,
            [
                new("facing", ["north", "south", "west", "east"], 6),
                new("honey_level", ["0", "1", "2", "3", "4", "5"], 1)
            ]),
            new(25178, 5,
            [
                new("charges", ["0", "1", "2", "3", "4"], 1)
            ]),
            new(25189, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25269, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(25593, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25603, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25609, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(25689, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(26014, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26094, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26100, 2,
            [
                new("powered", ["true", "false"], 1)
            ]),
            new(26102, 24,
            [
                new("face", ["floor", "wall", "ceiling"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(26126, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(26453, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26469, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26485, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26501, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26517, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26533, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26549, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26565, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26581, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26597, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26613, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26629, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26645, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26661, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26677, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26693, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26709, 16,
            [
                new("candles", ["1", "2", "3", "4"], 4),
                new("lit", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26725, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26727, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26729, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26731, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26733, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26735, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26737, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26739, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26741, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26743, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26745, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26747, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26749, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26751, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26753, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26755, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26757, 2,
            [
                new("lit", ["true", "false"], 1)
            ]),
            new(26761, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26773, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26785, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26797, 12,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26810, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26816, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(26896, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(27221, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(27227, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(27307, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(27633, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(27639, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(27719, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(28045, 5,
            [
                new("potent_sulfur_state", ["dry", "wet", "dormant", "erupting", "continuous"], 1)
            ]),
            new(28050, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28056, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28136, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(28461, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28467, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28547, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(28872, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28878, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(28958, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(29284, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(29290, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(29370, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(29695, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(29701, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(29781, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(30106, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(30112, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(30192, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(30520, 96,
            [
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 6),
                new("sculk_sensor_phase", ["inactive", "active", "cooldown"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(30616, 384,
            [
                new("facing", ["north", "south", "west", "east"], 96),
                new("power", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"], 6),
                new("sculk_sensor_phase", ["inactive", "active", "cooldown"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31001, 128,
            [
                new("down", ["true", "false"], 64),
                new("east", ["true", "false"], 32),
                new("north", ["true", "false"], 16),
                new("south", ["true", "false"], 8),
                new("up", ["true", "false"], 4),
                new("waterlogged", ["true", "false"], 2),
                new("west", ["true", "false"], 1)
            ]),
            new(31129, 2,
            [
                new("bloom", ["true", "false"], 1)
            ]),
            new(31131, 8,
            [
                new("can_summon", ["true", "false"], 4),
                new("shrieking", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31165, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31245, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31325, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31405, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31485, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31565, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31645, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31725, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31805, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31811, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31817, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31823, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31829, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31835, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31841, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31847, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(31853, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(31917, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(31981, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32045, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32109, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32173, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32237, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32301, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["upper", "lower"], 8),
                new("hinge", ["left", "right"], 4),
                new("open", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32365, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32429, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32493, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32557, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32621, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32685, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32749, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32813, 64,
            [
                new("facing", ["north", "south", "west", "east"], 16),
                new("half", ["top", "bottom"], 8),
                new("open", ["true", "false"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32877, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32879, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32881, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32883, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32885, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32887, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32889, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32891, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32893, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32897, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32901, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32905, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32909, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32913, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32917, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32921, 4,
            [
                new("lit", ["true", "false"], 2),
                new("powered", ["true", "false"], 1)
            ]),
            new(32925, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32949, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32973, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(32997, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33021, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33045, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33069, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33093, 24,
            [
                new("type", ["single", "left", "right"], 2),
                new("facing", ["north", "south", "west", "east"], 6),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33117, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33149, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33181, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33213, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33245, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33277, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33309, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33341, 32,
            [
                new("copper_golem_pose", ["standing", "sitting", "running", "star"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33373, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33397, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33421, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33445, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33469, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33493, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33517, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33541, 24,
            [
                new("facing", ["north", "east", "south", "west", "up", "down"], 4),
                new("powered", ["true", "false"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33566, 20,
            [
                new("thickness", ["tip_merge", "tip", "frustum", "middle", "base"], 4),
                new("vertical_direction", ["up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33586, 20,
            [
                new("thickness", ["tip_merge", "tip", "frustum", "middle", "base"], 4),
                new("vertical_direction", ["up", "down"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33606, 52,
            [
                new("age", ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25"], 2),
                new("berries", ["true", "false"], 1)
            ]),
            new(33658, 2,
            [
                new("berries", ["true", "false"], 1)
            ]),
            new(33664, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("flower_amount", ["1", "2", "3", "4"], 1)
            ]),
            new(33680, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("flower_amount", ["1", "2", "3", "4"], 1)
            ]),
            new(33696, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("segment_amount", ["1", "2", "3", "4"], 1)
            ]),
            new(33713, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("tilt", ["none", "unstable", "partial", "full"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33745, 8,
            [
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33753, 16,
            [
                new("facing", ["north", "south", "west", "east"], 4),
                new("half", ["upper", "lower"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33769, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33773, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(33777, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33857, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(33863, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(34188, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(34268, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(34274, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(34599, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(34679, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(34685, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(35010, 80,
            [
                new("facing", ["north", "south", "west", "east"], 20),
                new("half", ["top", "bottom"], 10),
                new("shape", ["straight", "inner_left", "inner_right", "outer_left", "outer_right"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(35090, 6,
            [
                new("type", ["top", "bottom", "double"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(35096, 324,
            [
                new("east", ["none", "low", "tall"], 108),
                new("north", ["none", "low", "tall"], 36),
                new("south", ["none", "low", "tall"], 12),
                new("up", ["true", "false"], 6),
                new("waterlogged", ["true", "false"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(35423, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(35432, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(35435, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(35438, 3,
            [
                new("axis", ["x", "y", "z"], 1)
            ]),
            new(35443, 16,
            [
                new("cracked", ["true", "false"], 8),
                new("facing", ["north", "south", "west", "east"], 2),
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(35459, 48,
            [
                new("crafting", ["true", "false"], 24),
                new("orientation", ["down_east", "down_north", "down_south", "down_west", "up_east", "up_north", "up_south", "up_west", "west_up", "east_up", "north_up", "south_up"], 2),
                new("triggered", ["true", "false"], 1)
            ]),
            new(35507, 12,
            [
                new("ominous", ["true", "false"], 6),
                new("trial_spawner_state", ["inactive", "waiting_for_players", "active", "waiting_for_reward_ejection", "ejecting_reward", "cooldown"], 1)
            ]),
            new(35519, 32,
            [
                new("facing", ["north", "south", "west", "east"], 8),
                new("ominous", ["true", "false"], 4),
                new("vault_state", ["inactive", "active", "unlocking", "ejecting"], 1)
            ]),
            new(35551, 2,
            [
                new("waterlogged", ["true", "false"], 1)
            ]),
            new(35554, 162,
            [
                new("bottom", ["true", "false"], 81),
                new("east", ["none", "low", "tall"], 27),
                new("north", ["none", "low", "tall"], 9),
                new("south", ["none", "low", "tall"], 3),
                new("west", ["none", "low", "tall"], 1)
            ]),
            new(35716, 2,
            [
                new("tip", ["true", "false"], 1)
            ]),
        ];
        // </auto-generated block-state-properties>

        protected override Dictionary<int, Material> GetDict()
        {
            return materials;
        }

        protected override BlockStateDefinition[] GetStateDefinitions()
        {
            return stateDefinitions;
        }
    }
}
