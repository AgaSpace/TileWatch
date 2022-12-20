using Auxiliary;
using CSF;
using CSF.TShock;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using MongoDB.Bson;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using TShockAPI;
using MongoDB.Driver.Linq;
using Org.BouncyCastle.Utilities;
using ZstdSharp.Unsafe;
using System.Runtime.Serialization;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class RollbackCommand :TSModuleBase<TSCommandContext>
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

            List<TWatch.Tile> tiles = StorageProvider.GetMongoCollection<TWatch.Tile>("Tiles").Find(x => x.Player == player.Account.ID).ToList();
            tiles = tiles.FindAll(x => (DateTime.UtcNow.Subtract(x.Time).TotalSeconds <= rollbackTime));

            foreach (TWatch.Tile t in tiles)
            {
                if ((t.X >= lowX && t.X <= hiX) == false && (t.Y >= lowY && t.Y <= hiY))
                {
                    continue;
                }
                    var i = Main.tile[t.X, t.Y];

                    if(t.Action == 0)
                    {
                        i.active(true);
                    }
                    if (t.Action == 1)
                    {
                        i.active(false);
                    }

                    if(t.Wall == true)
                    {
                        WorldGen.PlaceWall(t.X,t.Y,t.Type);
                        i.wall = t.Type;
                        i.wallColor(t.Paint);
                        player.SendData(PacketTypes.PaintWall, "", t.X, t.Y, t.Paint);

                    }
                    else
                        {
                        switch (t.Type)
                        {
                            case 16:
                            case 17:
                            case 18:
                            case 77:
                            case 134:
                            i.active(true);
                            i.frameX = -1;
                            i.frameY = -1;
                            i.type = t.Type;
                            player.SendData(PacketTypes.PlaceObject, "", t.X, t.Y, t.Type);
                            break;

                            default:
                            i.ResetToType((ushort)t.Type);
                            WorldGen.PlaceTile(t.X, t.Y, t.Type);
                            i.slope(t.Slope);
                            i.inActive(t.Inactive);
                            break;
                        }

                       
                        i.color(t.Paint);
                        player.SendData(PacketTypes.PaintTile, "", t.X, t.Y, t.Paint);

                }
                player.SendTileSquareCentered(t.X, t.Y);

                

            }

            return Success("Rollback complete!");

        }

    }
}
