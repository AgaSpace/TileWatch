using Auxiliary;
using CSF;
using CSF.TShock;
using IL.Terraria.GameContent.Drawing;
using MongoDB.Driver;
using Terraria;
using TShockAPI;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class RollbackCommand : TSModuleBase<TSCommandContext>
    {
        [Command("rollback", "rback")]
        public async Task<IResult> Rollback(string perp = "", string time = "", int radius = 9999)
        {
            if (perp == "")
                return Error("Please enter a valid player name. Ex. /rollback Average 10d 150");

            var rollbackTime = -1;
            if (time != "")
            {
                var success = TShock.Utils.TryParseTime(time, out rollbackTime);
                if (success == false)
                    return Error("Invalid time format!");
                //turn time into DateTime
                Console.WriteLine(time);
            }
            else
            {
                return Error("Invalid time string! Proper format: _d_h_m_s, with at least one time specifier.");
            }

            var player = Context.Player;
            if (player == null)
                return Error("You must be a player to use this command.");
            if (perp == "")
                return Error("Please enter a player name!");

            int lowX = ((int)player.X / 16) - radius;
            int lowY = ((int)player.Y / 16) - radius;
            int hiX = ((int)player.X / 16) + radius + 1;
            int hiY = ((int)player.Y / 16) + radius + 2;

            List<Tile> tiles = StorageProvider.GetMongoCollection<Tile>("Tiles").Find(
                x => x.Player == player.Account.ID
                && x.RolledBack == false
                ).ToList();

            tiles = tiles.Where(x => DateTime.UtcNow.Subtract(x.Time).TotalSeconds <= rollbackTime)
                .Where(x => lowX <= x.X && x.X <= hiX  && lowY <= x.Y && x.Y <= hiY)
                .ToList();

            foreach (Tile t in tiles)
            {
                var i = Main.tile[t.X, t.Y];

                switch (t.Action)
                {
                    case 0:
                        WorldGen.PlaceTile(t.X, t.Y, t.Type, style: t.Style);
                        WorldGen.paintTile(t.X, t.Y, t.Paint);
                        player.SendData(PacketTypes.PaintTile, "", t.X, t.Y, t.Paint);
                        break;
                    case 1:
                        WorldGen.KillTile(t.X, t.Y, noItem: true);
                        break;
                    case 2:
                        WorldGen.PlaceWall(t.X, t.Y, t.Type);
                        WorldGen.paintWall(t.X, t.Y, t.Paint);
                        player.SendData(PacketTypes.PaintWall, "", t.X, t.Y, t.Paint);
                        break;
                    case 3:
                        WorldGen.KillWall(t.X, t.Y);
                        break;
                    default:
                        break;
                }

                if (t.Object == true && t.Type != 82)
                {
                    Main.tile[t.X, t.Y].type = t.Type;
                    WorldGen.PlaceObject(t.X, t.Y, t.Type, false, style: t.Style, alternate: t.Alt, random: -1, direction: t.Direction ? 1 : -1);
                    Console.WriteLine(t.Type);
                    var pf = new Auxiliary.Packets.PacketFactory()
                        .SetType((byte)PacketTypes.PlaceObject)
                        .PackInt16((short)t.X)
                        .PackInt16((short)t.Y)
                        .PackInt16((short)t.Type)
                        .PackInt16((short)t.Style)
                        .PackByte((byte)t.Alt)
                        .PackSByte((sbyte)t.Rand)
                        .PackBool(t.Direction)
                        .GetByteData();
                    TSPlayer.All.SendRawData(pf);
                    player.SendTileSquareCentered(t.X, t.Y);
                }

                player.SendTileSquareCentered(t.X, t.Y);
                var rolledBackTile = await IModel.GetAsync(GetRequest.Bson<Tile>(v => v.X == t.X && v.Y == t.Y), null);
                if(rolledBackTile != null)
                    rolledBackTile.RolledBack = true;
            }


            return Success("Rollback complete!");

        }


    }

}
