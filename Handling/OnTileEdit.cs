using Auxiliary;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using System.Text;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;


namespace TileWatch.Handling
{
    public static class OnTileEdit
    {
        //these will be used to find the name of the tile/wall
        private static List<(string, int)> _tiles = Extensions.GetIds(typeof(TileID)).ToList();
        private static List<(string, int)> _walls = Extensions.GetIds(typeof(WallID)).ToList();

        public static async void Event(GetDataEventArgs e)
        {
            //if event has already been handled, return
            if (e.Handled)
                return;

            //switch statement for packet types
            switch (e.MsgID)
            {
                // on tile edits
                case PacketTypes.Tile:
                    {
                        //get player from e
                        TSPlayer player = TShock.Players[e.Msg.whoAmI];

                        if (player == null) return;
                        
                        //get all packet data from 
                        byte action = e.Msg.readBuffer[e.Index];
                        int x = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 1);
                        int y = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 3);
                        ushort flags = BitConverter.ToUInt16(e.Msg.readBuffer, e.Index + 5);
                        byte flags2 = e.Msg.readBuffer[e.Index + 7];


                        //get tile type
                        ITile? tile = Main.tile[x, y];
                        byte paint = tile.color();
                        ushort type = tile.type;
                        ushort wallType = tile.wall;
                        byte wallPaint = tile.wallColor();
                        bool inactive = tile.inActive();
                        byte slope = tile.slope();
                        bool halfbrick = tile.halfBrick();

                        bool wall = false;

                        if (action == 3 || action == 2) wall = true;

                        if (player.GetData<bool>("usinghistory"))
                        {
                            e.Handled = true;
                            TSPlayer.All.SendTileSquareCentered(x, y, 4);
                            player.SetData("usinghistory", false);

                            var info = await IModel.GetAsync(GetRequest.Bson<Tile>(t => t.X == x && t.Y == y));
                            List<Tile> editList = StorageProvider.GetMongoCollection<Tile>("Tiles").Find(t => t.X == x && t.Y == y).SortByDescending(x => x.Time).Limit(8).SortBy(x => x.Time).ToList();

                            if (editList.Count == 0)
                            {
                                player.SendErrorMessage("This tile has never been edited! (or there was no data found for it)");
                                return;
                            }
                            player.SendMessage($"Tile history at: X: {x + ", Y: " + y}", Color.LightGreen);

                            foreach (Tile b in editList)
                            {
                                string dhms = Extensions.ParseDate(b.Time);

                                string actionType = "";
                                string tileType = "";

                                switch (b.Action)
                                {
                                    case 0:
                                        actionType = "destroyed";
                                        tileType = "tile";
                                        break;
                                    case 1:
                                        actionType = "placed";
                                        tileType = "tile";
                                        break;
                                    case 2:
                                        actionType = "destroyed";
                                        tileType = "wall";
                                        break;
                                    case 3:
                                        actionType = "placed";
                                        tileType = "wall";
                                        break;
                                    case 21:
                                        actionType = "replaced";
                                        tileType = "tile";
                                        break;
                                    case 7:
                                        actionType = "halved";
                                        tileType = "tile";
                                        break;
                                    case 14:
                                        actionType = "hammered";
                                        tileType = "tile";
                                        break;
                                    //for unimplemented
                                    default:
                                        actionType = "manipulated";
                                        if (b.Wall == true)
                                            tileType = "wall";
                                        else
                                            tileType = "tile";
                                        break;

                                }
                                string name = "null";

                                if (b.Wall == true)
                                    name = _walls.Where(x => x.Item2 == b.Type).First().Item1;
                                else
                                    name = _tiles.Where(x => x.Item2 == b.Type).First().Item1;

                                if (b.RolledBack)
                                    player.SendMessage($"(ROLLED BACK) #{b.ObjectId.Increment} {b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} {actionType} the {tileType} ({name}) ({dhms})", Color.Orange);
                                else
                                    player.SendMessage($"#{b.ObjectId.Increment} {b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} {actionType} the {tileType} ({name}) ({dhms})", Color.LightYellow);

                            }

                            return;
                        }

                        Debug.OutputTileEdit(action, tile, slope, halfbrick, x, y, flags, flags2);

                        await Events.AddHistory(player, action, x, y, wall, wallType, wallPaint, type, paint, slope, halfbrick, flags, flags2, inactive);

                        if (action == 0 || action == 4 || action == 20) //killtile, killtilenoitem, trykilltile
                        {
                            // Checking for furniture "on top of the block"
                            if (Main.tileSolid[flags])
                            {
                                if (Main.tile[x, y - 1].active() && TWatch.breakableBottom[Main.tile[x, y - 1].type])
                                    await Events.AddHistory(player, action, x, (y-1), wall, wallType, wallPaint, type, paint, slope, halfbrick, flags, flags2, inactive);

                                if (Main.tile[x, y + 1].active() && TWatch.breakableTop[Main.tile[x, y + 1].type])
                                    await Events.AddHistory(player, action, x, (y+1), wall, wallType, wallPaint, type, paint, slope, halfbrick, flags, flags2, inactive);

                                if (Main.tile[x - 1, y].active() && TWatch.breakableSides[Main.tile[x - 1, y].type])
                                    await Events.AddHistory(player, action, x-1, y, wall, wallType, wallPaint, type, paint, slope, halfbrick, flags, flags2, inactive);
                                
                                if (Main.tile[x + 1, y].active() && TWatch.breakableSides[Main.tile[x + 1, y].type])
                                    await Events.AddHistory(player, action, x+1, y, wall, wallType, wallPaint, type, paint, slope, halfbrick, flags, flags2, inactive);

                            }
                        } 
                        return;
                    }
                case PacketTypes.PlaceObject:
                    {
                        var player = TShock.Players[e.Msg.whoAmI];
                        if (player == null) return;

                        //collect variables
                        int X = BitConverter.ToInt16(e.Msg.readBuffer, e.Index);
                        int Y = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 2);
                        ushort type = BitConverter.ToUInt16(e.Msg.readBuffer, e.Index + 4);
                        int style = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 6);
                        int alt = (byte)e.Msg.readBuffer[e.Index + 8];
                        int rand = (sbyte)e.Msg.readBuffer[e.Index + 9];
                        bool dir = BitConverter.ToBoolean(e.Msg.readBuffer, e.Index + 10);
                        
                        await Events.ObjectAddHistory(player, X, Y, alt, dir, rand, (byte)style, type);

                        return;
                    }


                default:
                    return;
            }
        }
    }
}
