using CSF.TShock;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TileWatch
{
    [ApiVersion(2, 1)]
    public class TWatch : TerrariaPlugin
    {
        private readonly TSCommandFramework _fx;
        public override string Author
        {
            get { return "Average"; }
        }

        public override string Description
        {
            get { return "A History rewrite"; }
        }

        public override string Name
        {
            get { return "TileWatch"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }


        public TWatch(Main game) : base(game)
        {
            _fx = new(new()
            {
                DefaultLogLevel = CSF.LogLevel.Warning
            });
        }

        public static bool[] breakableBottom = new bool[Terraria.ID.TileID.Count];
        public static bool[] breakableTop = new bool[Terraria.ID.TileID.Count];
        public static bool[] breakableSides = new bool[Terraria.ID.TileID.Count];
        public static bool[] breakableWall = new bool[Terraria.ID.TileID.Count];

        public async override void Initialize()
        {
            TerrariaApi.Server.ServerApi.Hooks.NetGetData.Register(this, TileWatch.Handling.OnTileEdit.Event);
            TerrariaApi.Server.ServerApi.Hooks.NetGreetPlayer.Register(this, NetGreet);

			initBreaks();

			await _fx.BuildModulesAsync(typeof(TWatch).Assembly);
        }

		void initBreaks()
		{
			breakableBottom[4] = true;
			breakableBottom[10] = true;
			breakableBottom[11] = true;
			breakableBottom[13] = true;
			breakableBottom[14] = true;
			breakableBottom[15] = true;
			breakableBottom[16] = true;
			breakableBottom[17] = true;
			breakableBottom[18] = true;
			breakableBottom[21] = true;
			breakableBottom[26] = true;
			breakableBottom[27] = true;
			breakableBottom[29] = true;
			breakableBottom[33] = true;
			breakableBottom[35] = true;
			breakableBottom[49] = true;
			breakableBottom[50] = true;
			breakableBottom[55] = true;
			breakableBottom[77] = true;
			breakableBottom[78] = true;
			breakableBottom[79] = true;
			breakableBottom[81] = true;
			breakableBottom[82] = true;
			breakableBottom[85] = true;
			breakableBottom[86] = true;
			breakableBottom[87] = true;
			breakableBottom[88] = true;
			breakableBottom[89] = true;
			breakableBottom[90] = true;
			breakableBottom[92] = true;
			breakableBottom[93] = true;
			breakableBottom[94] = true;
			breakableBottom[96] = true;
			breakableBottom[97] = true;
			breakableBottom[98] = true;
			breakableBottom[99] = true;
			breakableBottom[100] = true;
			breakableBottom[101] = true;
			breakableBottom[102] = true;
			breakableBottom[103] = true;
			breakableBottom[104] = true;
			breakableBottom[105] = true;
			breakableBottom[106] = true;
			breakableBottom[114] = true;
			breakableBottom[125] = true;
			breakableBottom[128] = true;
			breakableBottom[129] = true;
			breakableBottom[132] = true;
			breakableBottom[133] = true;
			breakableBottom[134] = true;
			breakableBottom[135] = true;
			breakableBottom[136] = true;
			breakableBottom[138] = true;
			breakableBottom[139] = true;
			breakableBottom[142] = true;
			breakableBottom[143] = true;
			breakableBottom[144] = true;
			breakableBottom[149] = true;
			breakableBottom[173] = true;
			breakableBottom[174] = true;
			breakableBottom[178] = true;
			breakableBottom[186] = true;
			breakableBottom[187] = true;
			breakableBottom[207] = true;
			breakableBottom[209] = true;
			breakableBottom[212] = true;
			breakableBottom[215] = true;
			breakableBottom[216] = true;
			breakableBottom[217] = true;
			breakableBottom[218] = true;
			breakableBottom[219] = true;
			breakableBottom[220] = true;
			//breakableBottom[227] = true; DYES, SOME GROW ON TOP?
			breakableBottom[228] = true;
			breakableBottom[231] = true;
			breakableBottom[235] = true;
			breakableBottom[237] = true;
			breakableBottom[239] = true;
			breakableBottom[243] = true;
			breakableBottom[244] = true;
			breakableBottom[247] = true;
			breakableBottom[254] = true;
			breakableBottom[269] = true;
			breakableBottom[275] = true;
			breakableBottom[276] = true;
			breakableBottom[278] = true;
			breakableBottom[279] = true;
			breakableBottom[280] = true;
			breakableBottom[281] = true;
			breakableBottom[283] = true;
			breakableBottom[285] = true;
			breakableBottom[286] = true;
			breakableBottom[287] = true;
			breakableBottom[296] = true;
			breakableBottom[297] = true;
			breakableBottom[298] = true;
			breakableBottom[299] = true;
			breakableBottom[300] = true;
			breakableBottom[301] = true;
			breakableBottom[302] = true;
			breakableBottom[303] = true;
			breakableBottom[304] = true;
			breakableBottom[305] = true;
			breakableBottom[306] = true;
			breakableBottom[307] = true;
			breakableBottom[308] = true;
			breakableBottom[309] = true;
			breakableBottom[310] = true;
			breakableBottom[316] = true;
			breakableBottom[317] = true;
			breakableBottom[318] = true;
			breakableBottom[319] = true;
			breakableBottom[320] = true;
			breakableBottom[335] = true;
			breakableBottom[337] = true;
			breakableBottom[338] = true;
			breakableBottom[339] = true;
			breakableBottom[349] = true;
			breakableBottom[354] = true;
			breakableBottom[355] = true;
			breakableBottom[356] = true;
			breakableBottom[358] = true;
			breakableBottom[359] = true;
			breakableBottom[360] = true;
			breakableBottom[361] = true;
			breakableBottom[362] = true;
			breakableBottom[363] = true;
			breakableBottom[364] = true;
			breakableBottom[372] = true;
			breakableBottom[376] = true;
			breakableBottom[377] = true;
			breakableBottom[378] = true;
			breakableBottom[380] = true;
			breakableBottom[380] = true;
			breakableBottom[388] = true;
			breakableBottom[389] = true;
			breakableBottom[390] = true;
			breakableBottom[391] = true;
			breakableBottom[392] = true;
			breakableBottom[393] = true;
			breakableBottom[394] = true;
			breakableBottom[395] = true;
			breakableBottom[405] = true;
			breakableBottom[406] = true;
			breakableBottom[410] = true;
			breakableBottom[413] = true;
			breakableBottom[414] = true;
			breakableBottom[419] = true;
			breakableBottom[425] = true;
			breakableBottom[441] = true;
			breakableBottom[442] = true;
			breakableBottom[443] = true;

			breakableTop[10] = true;
			breakableTop[11] = true;
			breakableTop[34] = true;
			breakableTop[42] = true;
			breakableTop[55] = true;
			breakableTop[91] = true;
			breakableTop[95] = true;//chinese lantern
			breakableTop[126] = true;
			breakableTop[129] = true;
			breakableTop[149] = true;
			breakableTop[270] = true;
			breakableTop[271] = true;
			breakableTop[380] = true;
			breakableTop[388] = true;
			breakableTop[389] = true;
			breakableTop[395] = true;
			breakableTop[425] = true;
			breakableTop[443] = true;

			breakableSides[4] = true;
			breakableSides[55] = true;
			breakableSides[129] = true;
			breakableSides[136] = true;
			breakableSides[149] = true;
			breakableSides[380] = true;
			breakableSides[386] = true;
			breakableSides[387] = true;
			breakableSides[395] = true;
			breakableSides[425] = true;

			breakableWall[4] = true;
			breakableWall[132] = true;
			breakableWall[136] = true;
			breakableWall[240] = true;
			breakableWall[241] = true;
			breakableWall[242] = true;
			breakableWall[245] = true;
			breakableWall[246] = true;
			breakableWall[334] = true;
			breakableWall[380] = true;
			breakableWall[395] = true;
			breakableWall[440] = true;
		}

		/*
         * I was hoping to use Dispose, since my plugin can unload and load other plugins, but CSF does not allow this
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TerrariaApi.Server.ServerApi.Hooks.NetGetData.Deregister(this, TileWatch.Handling.OnTileEdit.Event);
                TerrariaApi.Server.ServerApi.Hooks.NetGreetPlayer.Deregister(this, NetGreet);
            }
            base.Dispose(disposing);
        }
        */

		private void NetGreet(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
                return;

            player.SetData("usinghistory", false);
        }

    }
}