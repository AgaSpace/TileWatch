using Auxiliary;
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
        private static List<(string, int)> _tiles = Extensions.GetIds(typeof(TileID)).ToList();
        private static List<(string, int)> _walls = Extensions.GetIds(typeof(WallID)).ToList();

        public static async void Event(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.MsgID)
            {
                case PacketTypes.Tile:
                    {
                        //get player from e
                        var player = TShock.Players[e.Msg.whoAmI];

                        if (player == null) return;
                        
                        //get all packet data from 
                        byte action = e.Msg.readBuffer[e.Index];
                        int x = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 1);
                        int y = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 3);
                        ushort flags = BitConverter.ToUInt16(e.Msg.readBuffer, e.Index + 5);
                        byte flags2 = e.Msg.readBuffer[e.Index + 7];


                        //get tile type
                        var tile = Main.tile[x, y];
                        var paint = tile.color();
                        var type = tile.type;
                        var wallType = tile.wall;
                        var wallPaint = tile.wallColor();
                        var inactive = tile.inActive();
                        var slope = tile.slope();
                        var halfbrick = tile.halfBrick();

                        var wall = false;

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
                                TimeSpan timeDiff = DateTime.UtcNow - b.Time;
                                StringBuilder dhms = new StringBuilder();
                                if (timeDiff.Days != 0)
                                {
                                    dhms.Append(timeDiff.Days + "d");
                                }
                                if (timeDiff.Hours != 0)
                                {
                                    dhms.Append(timeDiff.Hours + "h");
                                }
                                if (timeDiff.Minutes != 0)
                                {
                                    dhms.Append(timeDiff.Minutes + "m");
                                }
                                if (timeDiff.Seconds != 0)
                                {
                                    dhms.Append(timeDiff.Seconds + "s");
                                }

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
                                var name = "null";

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
                        // Why keep deleting some code when you can just leave it in DEBUG?)
#if DEBUG
                        Console.WriteLine("Action: " + action);
                        Console.WriteLine($"EditData: {tile.type}");
                        Console.WriteLine($"Wall: {tile.wall}");
                        Console.WriteLine($"Paint: {tile.color()}");
                        Console.WriteLine($"Slope: {slope}");
                        Console.WriteLine($"Halved: {halfbrick}");
                        Console.WriteLine($"X: {x}");
                        Console.WriteLine($"Y: {y}");
                        Console.WriteLine($"Flags: {flags}");
                        Console.WriteLine($"Flags2: {flags2}");
#endif
                        await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
                        {
                            t.Action = (int)action;
                            t.X = x;
                            t.Y = y;

                            if (wall == true)
                            {
                                if (action == 3)
                                    t.Type = flags;
                                else 
                                    t.Type = wallType;

                                t.Wall = wall;
                                t.Paint = wallPaint;
                            }
                            else
                            {
                                t.Paint = paint;
                                if (action == 1)
                                    t.Type = flags;
                                else 
                                    t.Type = type;
                            }

                            t.Player = player.Account.ID;
                            t.Time = DateTime.Now;
                            t.Inactive = inactive;
                            t.Slope = slope;
                            t.Halfbrick = halfbrick;
                            t.Style = flags2;
                            t.RolledBack = false;
                        }));

                        if (action == 0 || action == 4 || action == 20) //killtile, killtilenoitem, trykilltile
                        {
                            // Checking for furniture "on top of the block"
                            if (Main.tileSolid[flags])
                            {
                                if (Main.tile[x, y - 1].active() && TWatch.breakableBottom[Main.tile[x, y - 1].type])
                                    await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
                                    {
                                        t.Action = (int)action;
                                        t.X = x;
                                        t.Y = (y - 1);

                                        t.Type = Main.tile[t.X, t.Y].type;
                                        t.Style = Extensions.GetStyle(Main.tile[t.X, t.Y]);

                                        t.Player = player.Account.ID;
                                        t.Time = DateTime.Now;
                                        t.Object = true;
                                    }));
                                if (Main.tile[x, y + 1].active() && TWatch.breakableTop[Main.tile[x, y + 1].type])
                                    await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
                                    {
                                        t.Action = (int)action;
                                        t.X = x;
                                        t.Y = (y + 1);

                                        t.Type = Main.tile[t.X, t.Y].type;
                                        t.Style = Extensions.GetStyle(Main.tile[t.X, t.Y]);

                                        t.Player = player.Account.ID;
                                        t.Time = DateTime.Now;
                                        t.Object = true;
                                    }));
                                if (Main.tile[x - 1, y].active() && TWatch.breakableSides[Main.tile[x - 1, y].type])
                                    await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
                                    {
                                        t.Action = (int)action;
                                        t.X = (x - 1);
                                        t.Y = y;

                                        t.Type = Main.tile[t.X, t.Y].type;
                                        t.Style = Extensions.GetStyle(Main.tile[t.X, t.Y]);

                                        t.Player = player.Account.ID;
                                        t.Time = DateTime.Now;
                                        t.Object = true;
                                    }));
                                if (Main.tile[x + 1, y].active() && TWatch.breakableSides[Main.tile[x + 1, y].type])
                                    await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
                                    {
                                        t.Action = (int)action;
                                        t.X = (x + 1);
                                        t.Y = y;

                                        t.Type = Main.tile[t.X, t.Y].type;
                                        t.Style = Extensions.GetStyle(Main.tile[t.X, t.Y]);

                                        t.Player = player.Account.ID;
                                        t.Time = DateTime.Now;
                                        t.Object = true;
                                    }));
                            }
                        } 
                        return;
                    }
                case PacketTypes.PlaceObject:
                    {
                        var player = TShock.Players[e.Msg.whoAmI];
                        if (player == null) return;

                        int X = BitConverter.ToInt16(e.Msg.readBuffer, e.Index);
                        int Y = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 2);
                        ushort type = BitConverter.ToUInt16(e.Msg.readBuffer, e.Index + 4);
                        int style = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 6);
                        int alt = (byte)e.Msg.readBuffer[e.Index + 8];
                        int rand = (sbyte)e.Msg.readBuffer[e.Index + 9];
                        bool dir = BitConverter.ToBoolean(e.Msg.readBuffer, e.Index + 10);

                        await IModel.CreateAsync(CreateRequest.Bson<Tile>(x =>
                        {
                            x.Action = 1;
                            x.Wall = false;
                            x.Alt = alt;
                            x.Direction = dir;
                            x.Rand = rand;
                            x.Style = (byte)style;
                            x.Type = type;
                            x.X = X;
                            x.Y = Y;
                            x.Inactive = false;
                            x.Object = true;
                            x.Player = player.Account.ID;
                            x.Time = DateTime.Now;
                            x.RolledBack = false;
                        }));

                        return;
                    }


                default:
                    return;
            }
        }
    }
}
