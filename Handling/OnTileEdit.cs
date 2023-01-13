using Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using TShockAPI;
using Terraria;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using Terraria.ID;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Terraria.WorldBuilding;
using TileWatch.Commands;
using System.ComponentModel;

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

                        if (player == null)
                            return;

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

                        var wall = false;

                        if (action == 3 || action == 2)
                        {
                            wall = true;
                        }

                        if (player.GetData<bool>("usinghistory"))
                        {

                            //revert changes to the tile
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
                                        if(b.Wall == true)
                                            tileType = "wall";
                                        else
                                            tileType = "tile";
                                        break;

                                }
                                var name = "null";
                                
                                if(b.Wall == true)
                                    name = _walls.Where(x => x.Item2 == b.Type).First().Item1;
                                else
                                    name = _tiles.Where(x => x.Item2 == b.Type).First().Item1;

                                if (b.RolledBack)
                                    player.SendMessage($"(ROLLED BACK) #{b.ObjectId.Increment} {b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} {actionType} the {tileType} ({dhms})", Color.Orange);
                                else
                                    player.SendMessage($"#{b.ObjectId.Increment} {b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} {actionType} the {tileType} ({dhms})", Color.LightYellow);

                            }

                            return;
                        }

                        Console.WriteLine("Action: " + action);
                        Console.WriteLine($"EditData: {tile.type}");
                        Console.WriteLine($"Paint: {tile.color()}");
                        Console.WriteLine($"Slope: {tile.slope()}");
                        Console.WriteLine($"X: {x}");
                        Console.WriteLine($"Y: {y}");
                        Console.WriteLine($"Flags: {flags}");
                        Console.WriteLine($"Flags2: {flags2}");


                        await IModel.CreateAsync(CreateRequest.Bson<Tile>(t => {

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
                            t.Style = flags2;

                            if (Terraria.ObjectData.TileObjectData.CustomPlace(type, flags2))
                                t.Object = true;
                            else
                                t.Object = false;

                        }));
                        break;
                    }
                case PacketTypes.PlaceObject:
                    {

                        //get player from e
                        var player = TShock.Players[e.Msg.whoAmI];

                        if (player == null)
                            return;

                        int X = BitConverter.ToInt16(e.Msg.readBuffer, e.Index);
                        int Y = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 2);
                        ushort type = BitConverter.ToUInt16(e.Msg.readBuffer, e.Index + 4);
                        int style = BitConverter.ToInt16(e.Msg.readBuffer, e.Index + 6);
                        //DEBUG:
                        //TSPlayer.All.SendInfoMessage($"Style: {style}");
                        int alt = (byte)e.Msg.readBuffer[e.Index + 8];
                        //TSPlayer.All.SendInfoMessage($"Alternate: {alt}");
                        int rand = (sbyte)e.Msg.readBuffer[e.Index + 9];
                        //TSPlayer.All.SendInfoMessage($"Random: {rand}");
                        bool dir = BitConverter.ToBoolean(e.Msg.readBuffer, e.Index + 10);

                        await IModel.CreateAsync(CreateRequest.Bson<Tile>(x =>
                        {

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
                        }));

                        break;
                    }


                default:
                    break;
            }





        }

    }
}
