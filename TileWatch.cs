using CSF.TShock;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TileWatch
{
    [ApiVersion(2, 1)]
    public class TWatch : TerrariaPlugin
    {
        private TSCommandFramework _fx;
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

        public async override void Initialize()
        {
            TerrariaApi.Server.ServerApi.Hooks.NetGetData.Register(this, TileWatch.Handling.OnTileEdit.Event);
            TerrariaApi.Server.ServerApi.Hooks.NetGreetPlayer.Register(this, NetGreet);

            await _fx.BuildModulesAsync(typeof(TWatch).Assembly);
        }

        /*
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