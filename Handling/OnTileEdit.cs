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

                        //get all packet data from e
                        var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length));
                        var action = reader.ReadByte();
                        var x = reader.ReadInt16();
                        var y = reader.ReadInt16();
                        var flags = reader.ReadInt16();
                        var flags2 = reader.ReadByte();


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

                                if (b.Action == 0)
                                {
                                    var name = _tiles.Where(x => x.Item2 == b.Type).First().Item1;


                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} destroyed the tile {name}. ({dhms})", Color.LightYellow);
                                }
                                else if (b.Action == 1)
                                {
                                    var name = _tiles.Where(x => x.Item2 == b.Type).First().Item1;


                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} placed the tile {name}. ({dhms})", Color.LightYellow);
                                }
                                else if (b.Action == 2)
                                {
                                    var name = _walls.Where(x => x.Item2 == b.Type).FirstOrDefault().Item1;
                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} destroyed this wall. ({dhms})", Color.LightYellow);

                                }
                                else if (b.Action == 3)
                                {
                                    var name = _walls.Where(x => x.Item2 == b.Type).First().Item1;

                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} placed the wall {name}. ({dhms})", Color.LightYellow);

                                }
                                else if (b.Action == 21)
                                {

                                    var name = _tiles.Where(x => x.Item2 == b.Type).First().Item1;


                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} replaced the tile with {name}. ({dhms})", Color.LightYellow);
                                }
                                else if (b.Action == 7)
                                {
                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} halved this tile. ({dhms})", Color.LightYellow);
                                }
                                else if (b.Action == 14)
                                {
                                    player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} hammered this tile. ({dhms})", Color.LightYellow);
                                }



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


                        var t = await IModel.CreateAsync(CreateRequest.Bson<Tile>(x => x.Action = (int)action));
                        t.X = x;
                        t.Y = y;
                        if (wall == true)
                        {
                            t.Type = wallType;
                            t.Wall = wall;
                            t.Paint = wallPaint;
                        }
                        else
                        {
                            t.Paint = paint;
                            t.Type = type;

                        }

                        t.Player = player.Account.ID;
                        t.Time = DateTime.Now;
                        t.Inactive = inactive;
                        t.Slope = slope;
                        t.Style = flags2;

                        if(Terraria.ObjectData.TileObjectData.CustomPlace(type, flags2))
                        {
                            t.Object = true;
                            Console.WriteLine("destroyed OBJECT");
                        }
                        else
                        {
                            t.Object = false;
                        }
                        

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

/*                    var t = await IModel.CreateAsync(CreateRequest.Bson<Tile>(x => x.Action = (int)));
*/

                        break;
                }


                default:
                    break;
            }





        }

    }
}
