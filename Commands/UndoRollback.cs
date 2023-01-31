using Auxiliary;
using CSF;
using CSF.TShock;
using MongoDB.Driver;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Utilities;
using TShockAPI;
using static TileWatch.Extensions;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class UndoRollback : TSModuleBase<TSCommandContext>
    {
        [Command("ur", "rundo", "rollbackundo", "undorollback")]
        public async Task<IResult> UndoRollbackCommand(string perp = "", string time = "", int radius = 9999)
        {
            // if no player has been specified, return an error
            if (perp == "") return Error("Please enter a valid player name. Ex. /rollback Average 10d 150");

            int rollbackTime;
            // if no time has been specified, or if the time is invalid, return an error
            if (time != "")
            {
                var success = TShock.Utils.TryParseTime(time, out rollbackTime);
                if (success == false) return Error("Invalid time format!");
            }
            else return Error("Invalid time string! Proper format: _d_h_m_s, with at least one time specifier.");

            //retrieve command sender
            TSPlayer player = Context.Player;

            //must be a valid player
            if (player == null) return Error("You must be a player to use this command.");

            //retrieve tile radius
            int lowX = ((int)player.X / 16) - radius;
            int lowY = ((int)player.Y / 16) - radius;
            int hiX = ((int)player.X / 16) + radius + 1;
            int hiY = ((int)player.Y / 16) + radius + 2;

            //find all tiles that match the criteria, in the given timeframe and radius
            List<Tile> tiles = StorageProvider.GetMongoCollection<Tile>("Tiles").Find(
                x => x.Player == player.Account.ID
                && x.RolledBack == false).ToList();

            tiles = tiles.Where(x => DateTime.UtcNow.Subtract(x.Time).TotalSeconds <= rollbackTime)
                .Where(x => lowX <= x.X && x.X <= hiX && lowY <= x.Y && x.Y <= hiY)
                .ToList();

            //assign value to Main.rand
            //was done in History, not quite sure why but for the sake of parity, this is here
            if (Main.rand == null)
                Main.rand = new UnifiedRandom();

            //initialize a Trials list, for furniture or objects that can't be placed yet because other tiles (specifically underneath them) have not been placed
            List<Trial> Trials = new List<Trial>();

            //for each tile in the list, perform the appropriate action
            for (int e = 0; e < tiles.Count; e++)
            {
                Extensions.CompleteAction(CommandType.Undo, player, tiles[e], tiles, e, ref Trials);
            }

            foreach (Tile t in tiles)
            {
                var rolledBackTile = await IModel.GetAsync(GetRequest.Bson<Tile>(v => v.X == t.X && v.Y == t.Y));
                if (rolledBackTile != null)
                    rolledBackTile.RolledBack = true;
            }


            return Success("Rollback complete!");



        }

    }
}
