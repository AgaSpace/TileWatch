using Auxiliary;
using CSF;
using CSF.TShock;
using MongoDB.Driver;
using Terraria;
using TShockAPI;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class RollbackCommand : TSModuleBase<TSCommandContext>
    {
        [Command("rollback", "rback")]
        public async Task<IResult> Rollback(string perp = "", int radius = 9999, string time = "10:10:10")
        {
            double rollbackTime = -1;
            if (time != "")
            {
                rollbackTime = System.TimeSpan.Parse(time).TotalSeconds;
                //turn time into DateTime
                Console.WriteLine(time);
            }

            var player = Context.Player;
            if (player == null)
                return Error("You must be a player to use this command.");
            if (perp == "")
                return Error("Please enter a player name!");

            int lowX = ((int)player.X) - radius;
            int lowY = ((int)player.Y) - radius;
            int hiX = ((int)player.Y) + radius;
            int hiY = ((int)player.Y) + radius;

            List<Tile> tiles = StorageProvider.GetMongoCollection<Tile>("Tiles").Find(x => x.Player == player.Account.ID).ToList();
            tiles = tiles.FindAll(x => (DateTime.UtcNow.Subtract(x.Time).TotalSeconds <= rollbackTime));

            foreach (Tile t in tiles)
            {
                if ((t.X >= lowX && t.X <= hiX) == false && (t.Y >= lowY && t.Y <= hiY) == false)
                {
                    continue;
                }

                var i = Main.tile[t.X, t.Y];

                if (t.Action == 0)
                {
                    i.active(true);
                }
                if (t.Action == 1)
                {
                    i.active(false);
                }

                if (t.Wall == true)
                {
                    WorldGen.PlaceWall(t.X, t.Y, t.Type);
                    i.wall = t.Type;
                    i.wallColor(t.Paint);
                    player.SendData(PacketTypes.PaintWall, "", t.X, t.Y, t.Paint);

                }

                else if (t.Object == true && t.Type != 82 || t.Type == 4)
                {

                    switch (t.Type)
                    {
                        case 15:
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
                                break;
                            }
                        case 18:
                            {
                                WorldGen.Place2x1(t.X, t.Y, t.Type, t.Style);
                                break;
                            }
                        default:
                            Main.tile[t.X, t.Y].type = t.Type;
                            WorldGen.PlaceObject(t.X, t.Y, t.Type, true, 0, -1, -1, -1);
                            Console.WriteLine(t.Type);
                            player.SendTileSquareCentered(t.X, t.Y);
                            break;


                    }
                }
                else
                    WorldGen.PlaceTile(t.X, t.Y, t.Type, false, true, -1, style: t.Style);

                i.color(t.Paint);
                player.SendData(PacketTypes.PaintTile, "", t.X, t.Y, t.Paint);
                player.SendTileSquareCentered(t.X, t.Y);

            }


            return Success("Rollback complete!");

        }


    }

}
