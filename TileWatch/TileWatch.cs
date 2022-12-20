using Auxiliary;
using CSF.TShock;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace TileWatch
{
    [ApiVersion(2, 1)]
    public class TWatch : TerrariaPlugin
    {
        private static List<(string, int)> _tiles;
        private static List<(string, int)> _walls;
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
                DefaultLogLevel = CSF.LogLevel.Warning,
            });
        }

        public async override void Initialize()
        {
            TerrariaApi.Server.ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            TerrariaApi.Server.ServerApi.Hooks.NetGreetPlayer.Register(this, NetGreet);

            await _fx.BuildModulesAsync(typeof(TWatch).Assembly);
            _tiles = Extensions.GetIds(typeof(TileID)).ToList();
        }

        private void NetGreet(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
                return;

            player.SetData("usinghistory", false);
        }

        private async void OnGetData(GetDataEventArgs e)
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
                        

                            foreach(Tile b in editList)
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
                                    else if(b.Action == 2)
                                    {
                                        var name = _walls.Where(x => x.Item2 == b.Type).First().Item1;

                                        player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} destroyed this wall. ({dhms})", Color.LightYellow);

                                    }
                                    else if(b.Action == 3)
                                    {
                                        var name = _walls.Where(x => x.Item2 == b.Type).First().Item1;


                                        player.SendMessage($"{b.Time} - {TShock.UserAccounts.GetUserAccountByID(b.Player)} placed the wall {name}. ({dhms})", Color.LightYellow);

                                    }
                                    else if(b.Action == 21)
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
                        t.Type = type;
                        t.Player = player.Account.ID;
                        t.Time = DateTime.Now;
                        t.Paint = tile.color();
                        
               

                        break;
                    }


                default:
                    break;
            }




            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TerrariaApi.Server.ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);

            }
            base.Dispose(disposing);
        }

        public class Tile : BsonModel
        {
            private int _player;
            public int Player
            {
                get => _player;
                set { _ = this.SaveAsync(x => x.Player, value); _player = value; }
            }


            private ushort _type;
            public ushort Type
            {
                get => _type;

                set { _ = this.SaveAsync(x => x.Type, value); _type = value; }
            }

            private int _paint;
            public int Paint
            {
                get => _paint;

                set { _ = this.SaveAsync(x => x.Paint, value); _paint = value; }
            }

            private int _slope;
            public int Slope
            {
                get => _slope;

                set { _ = this.SaveAsync(x => x.Slope, value); _slope = value; }
            }

            private int _action;

            public int Action
            {
                get => _action;

                set { this.SaveAsync(x => x.Type, value); _action = value; }
            }

            private DateTime _when;
            public DateTime Time
            {
                get => _when;

                set { _ = this.SaveAsync(x => x.Time, value); _when = value; }
            }

            private int _x;
            public int X
            {
                get => _x;

                set { _ = this.SaveAsync(x => x.X, value); _x = value; }
            }

            private int _y;
            public int Y
            {
                get => _y;

                set { _ = this.SaveAsync(x => x.Y, value); _y = value; }
            }

        }


    }
}