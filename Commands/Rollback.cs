using Auxiliary;
using CSF;
using CSF.TShock;
using MongoDB.Driver;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;
using static MonoMod.InlineRT.MonoModRule;
using static System.Net.Mime.MediaTypeNames;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class RollbackCommand : TSModuleBase<TSCommandContext>
    {
        [Command("rollback", "rback")]
        public async Task<IResult> Rollback(string perp = "", string time = "", int radius = 9999)
        {
            if (perp == "") return Error("Please enter a valid player name. Ex. /rollback Average 10d 150");

            var rollbackTime = -1;
            if (time != "")
            {
                var success = TShock.Utils.TryParseTime(time, out rollbackTime);
                if (success == false) return Error("Invalid time format!");
            }

            else return Error("Invalid time string! Proper format: _d_h_m_s, with at least one time specifier.");
            var player = Context.Player;

            if (player == null) return Error("You must be a player to use this command.");
            if (perp == "") return Error("Please enter a player name!");

            int lowX = ((int)player.X / 16) - radius;
            int lowY = ((int)player.Y / 16) - radius;
            int hiX = ((int)player.X / 16) + radius + 1;
            int hiY = ((int)player.Y / 16) + radius + 2;

            List<Tile> tiles = StorageProvider.GetMongoCollection<Tile>("Tiles").Find(
                x => x.Player == player.Account.ID
                && x.RolledBack == false).ToList();

            tiles = tiles.Where(x => DateTime.UtcNow.Subtract(x.Time).TotalSeconds <= rollbackTime)
                .Where(x => lowX <= x.X && x.X <= hiX && lowY <= x.Y && x.Y <= hiY)
                .ToList();

            List<Trial> Trials = new List<Trial>();

            for (var e = 0; e < tiles.Count; e++)
            {
                var t = tiles[e];
                var i = Main.tile[t.X, t.Y];

                if (t.reference == 0)
                    Trials.Add(new Trial { index = e, trials = 0});
                
                switch (t.Action)
                {
                    case 0:
                    case 4://del tile
                    case 20://trykilltile 
                        if (Main.tileSand[t.Type])//sand falling compensation (need to check up for top of sand)
                        {
                            int newY = t.Y;
                            while (newY > 0 && Main.tile[t.X, newY].active() && Main.tile[t.X, newY].type == t.Type)
                            {
                                newY--;
                            }
                            if (Main.tile[t.X, newY].active())
                                continue;
                            t.Y = newY;
                        }
                        else if (t.Type == 5)//tree, grow another?
                        {
                            WorldGen.GrowTree(t.X, t.Y + 1);
                            continue;
                        }
                        else if (t.Type == 2 || t.Type == 23 || t.Type == 60 || t.Type == 70 || t.Type == 109 || t.Type == 199)// grasses need to place manually, not from placeTile
                        {
                            Main.tile[t.X, t.Y].type = t.Type;
                            Main.tile[t.X, t.Y].color((byte)(t.Paint & 127));
                            Main.tile[t.X, t.Y].active(true);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                            continue;
                        }
                        //maybe already repaired?
                        if (Main.tile[t.X, t.Y].active() && Main.tile[t.X, t.Y].type == t.Type)
                        {
                            if (t.Type == 314 || t.Type == 395)
                                goto frameOnly;
                            continue;
                        }
                        int TrialCount;

                        if (t.reference != 0)
                            TrialCount = Trials.FirstOrDefault((x => x.index == t.reference)).trials;
                        else
                            TrialCount = 0;

                        //bool success = false;
                        if (Terraria.ObjectData.TileObjectData.CustomPlace(t.Type, t.Style) && t.Type != 82 && TrialCount < 2)
                        {
                            var success = WorldGen.PlaceObject(t.X, t.Y, t.Type, false, style: t.Style, alternate: t.Alt, random: -1, direction: t.Direction ? 1 : -1);
                            if (success)
                            {
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
                            else if (TrialCount < 2)
                            {
                                if(t.reference == 0)
                                    t.reference = e;

                                if (Trials.Any(x => x.index == t.reference))
                                {
                                    Trials.Find(x => x.index == t.reference).trials++;
                                }

                                tiles.Add(t);
                                continue;
                            }
                            else
                            {
                                continue;
                            }

                        }
                        else
                        {
                            WorldGen.PlaceTile(t.X, t.Y, t.Type, false, true, -1, style: t.Style);
                            WorldGen.paintTile(t.X, t.Y, t.Paint);
                        }
                    frameOnly:
                        //restore slopes
                        if (t.Halfbrick)
                            WorldGen.PoundTile(t.X, t.Y);
                        else
                            WorldGen.SlopeTile(t.X, t.Y, t.Slope);

                        //restore sign text
                        if (t.Type == 55 || t.Type == 85 || t.Type == 425)
                        {
                            int signI = Sign.ReadSign(t.X, t.Y);
                            if (signI >= 0)
                                Sign.TextSign(signI, "");
                        }
                        //Mannequins
                        else if (t.Type == 128 || t.Type == 269)
                        {
                            //x,y should be bottom left, Direction is already done via PlaceObject so we add the item values.
                            Main.tile[t.X, t.Y - 2].frameX += (short)(t.Paint * 100);
                            Main.tile[t.X, t.Y - 1].frameX += (short)((t.Alt & 0x3FF) * 100);
                            Main.tile[t.X, t.Y].frameX += (short)((t.Alt >> 10) * 100);
                        }
                        // Restore Weapon Rack if it had a netID
                        else if (t.Type == 334 && t.Alt > 0)
                        {
                            int mask = 5000;// +(direction ? 15000 : 0);
                            Main.tile[t.X - 1, t.Y].frameX = (short)(t.Alt + mask + 100);
                            Main.tile[t.X, t.Y].frameX = (short)(t.Paint + mask + 5000);
                        }
                        // Restore Item Frame
                        else if (t.Type == 395)
                        {
                            /*TileEntity TE;
                            // PlaceObject should already place a blank entity.
                            if (TileEntity.ByPosition.TryGetValue(new Point16(t.X, t.Y), out TE))
                            {
                                Console.WriteLine("Frame had Entity, changing item.");
                                TEItemFrame frame = (TEItemFrame)TE;
                                frame.item.netDefaults(t.Alt);
                                frame.item.Prefix(random);
                                frame.item.stack = 1;
                                NetMessage.SendData(86, -1, -1, "", frame.ID, (float)x, (float)y, 0f, 0, 0, 0);
                            }
                            else
                                Console.WriteLine("This Frame restore had no entity");*/
                        }
                        //Send larger area for furniture
                        if (Main.tileFrameImportant[t.Type])
                            if (t.Type == 104)
                                TSPlayer.All.SendTileSquare(t.X, t.Y - 2, 8);
                            else
                                TSPlayer.All.SendTileSquare(t.X, t.Y, 8);//This can be very large, or too small in some cases
                        else
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        break;
                    case 1://add tile
                        bool delete = Main.tile[t.X, t.Y].active();
                        if (!delete && Main.tileSand[t.Type])//sand falling compensation (it may have fallen down)
                        {
                            int newY = t.Y + 1;
                            while (newY < Main.maxTilesY - 1 && !Main.tile[t.X, newY].active())
                            {
                                newY++;
                            }
                            if (Main.tile[t.X, newY].type == t.Type)
                            {
                                t.Y = newY;
                                delete = true;
                            }
                        }
                        if (delete)
                        {
                            WorldGen.KillTile(t.X, t.Y, false, false, true);
                            NetMessage.SendData(17, -1, -1, NetworkText.Empty, 0, t.X, t.Y);
                        }
                        break;
                    case 2://del wall
                        if (Main.tile[t.X, t.Y].wall != t.Type) //change if not what was deleted
                        {
                            Main.tile[t.X, t.Y].wall = (byte)t.Type;
                            Main.tile[t.X, t.Y].wallColor((byte)t.Paint);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 3://add wall
                        if (Main.tile[t.X, t.Y].wall != 0)
                        {
                            Main.tile[t.X, t.Y].wall = 0;
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 5://placewire
                        if (Main.tile[t.X, t.Y].wire())
                        {
                            WorldGen.KillWire(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 6://killwire
                        if (!Main.tile[t.X, t.Y].wire())
                        {
                            WorldGen.PlaceWire(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 7://poundtile
                        WorldGen.PoundTile(t.X, t.Y);
                        TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        break;
                    case 8://placeactuator
                        if (Main.tile[t.X, t.Y].actuator())
                        {
                            WorldGen.KillActuator(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 9://killactuator
                        if (!Main.tile[t.X, t.Y].actuator())
                        {
                            WorldGen.PlaceActuator(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 10://placewire2
                        if (Main.tile[t.X, t.Y].wire2())
                        {
                            WorldGen.KillWire2(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 11://killwire2
                        if (!Main.tile[t.X, t.Y].wire2())
                        {
                            WorldGen.PlaceWire2(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 12://placewire3
                        if (Main.tile[t.X, t.Y].wire3())
                        {
                            WorldGen.KillWire3(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 13://killwire3
                        if (!Main.tile[t.X, t.Y].wire3())
                        {
                            WorldGen.PlaceWire3(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 14://slopetile
                        Main.tile[t.X, t.Y].slope((byte)(t.Paint >> 8));
                        Main.tile[t.X, t.Y].halfBrick((t.Paint & 128) == 128);
                        TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        break;
                    case 15: //frame track
                             //see above
                        break;
                    case 16:
                        if (Main.tile[t.X, t.Y].wire4())
                        {
                            WorldGen.KillWire4(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 17:
                        if (!Main.tile[t.X, t.Y].wire4())
                        {
                            WorldGen.PlaceWire4(t.X, t.Y);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 21:
                        if (Main.tile[t.X, t.Y].active())
                        {
                            int prevTile = t.Type & 0xFFFF;
                            int placedTile = (t.Type >> 16) & 0xFFFF;

                            WorldGen.PlaceTile(t.X, t.Y, prevTile, false, true, -1, style: t.Style);
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 22:
                        int prevWall = t.Type & 0xFFFF;
                        int placedWall = (t.Type >> 16) & 0xFFFF;
                        if (Main.tile[t.X, t.Y].wall != prevWall) //change if not what was replaced
                        {
                            Main.tile[t.X, t.Y].wall = (byte)prevWall;
                            TSPlayer.All.SendTileSquare(t.X, t.Y, 1);
                        }
                        break;
                    case 25://t.Paint tile
                        if (Main.tile[t.X, t.Y].active())
                        {
                            Main.tile[t.X, t.Y].color((byte)t.Paint);
                            NetMessage.SendData(63, -1, -1, NetworkText.Empty, t.X, t.Y, t.Paint, 0f, 0);
                        }
                        break;
                    case 26://t.Paint wall
                        if (Main.tile[t.X, t.Y].wall > 0)
                        {
                            Main.tile[t.X, t.Y].wallColor((byte)t.Paint);
                            NetMessage.SendData(64, -1, -1, NetworkText.Empty, t.X, t.Y, t.Paint, 0f, 0);
                        }
                        break;
                    case 27://updatesign
                        int sI = Sign.ReadSign(t.X, t.Y); //This should be an existing sign, but use coords instead of index anyway
                        if (sI >= 0)
                        {
                            Sign.TextSign(sI, "");
                        }
                        break;
            }

                player.SendTileSquareCentered(t.X, t.Y);
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
