using Auxiliary;
using CSF;
using CSF.TShock;
using MongoDB.Driver;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Utilities;
using TShockAPI;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.rollback")]
    internal class RollbackCommand : TSModuleBase<TSCommandContext>
    {
        [Command("rollback", "rback")]
        public async Task<IResult> Rollback(string perp = "", string time = "", int radius = 9999)
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
                //get the tile object from the list "tiles"
                Tile t = tiles[e];

                //get the Terraria tile within the world
                ITile? i = Main.tile[t.X, t.Y];

                //the tile may be referencing another tile, if it isn't, add it to the Trials list as the first Trial.
                if (t.reference == 0)
                    Trials.Add(new Trial { index = e, trials = 0 });

                switch (t.Action)
                {
                    case 0:
                    case 4://del tile
                    case 20://trykilltile 
                        if (Main.tileSand[t.Type]) //compensates for falling sand (need to check up for top of sand)
                        {
                            int newY = t.Y;
                            while (newY > 0 && Main.tile[t.X, newY].active() && Main.tile[t.X, newY].type == t.Type)
                                newY--;

                            if (Main.tile[t.X, newY].active())
                                continue;
                            t.Y = newY;
                        }

                        // if tree, grow another
                        else if (t.Type == TileID.Trees)
                        { WorldGen.GrowTree(t.X, t.Y + 1); continue; }

                        //place respective grass
                        else if (t.Type == TileID.Grass
                            || t.Type == TileID.CorruptGrass
                            || t.Type == TileID.JungleGrass
                            || t.Type == TileID.MushroomGrass
                            || t.Type == TileID.HallowedGrass
                            || t.Type == TileID.CrimsonGrass
                            || t.Type == TileID.AshGrass)
                        {
                            Main.tile[t.X, t.Y].type = t.Type;
                            Main.tile[t.X, t.Y].color((byte)(t.Paint & 127));
                            Main.tile[t.X, t.Y].active(true);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                            continue;
                        }

                        //conditional statement checking if tile is the same as the one it would be replaced with
                        if (Main.tile[t.X, t.Y].active() && Main.tile[t.X, t.Y].type == t.Type)
                        {
                            if (t.Type == TileID.MinecartTrack || t.Type == TileID.ItemFrame)
                                goto frameOnly;
                            continue;
                        }

                        int TrialCount;
                        if (t.reference != 0)
                            TrialCount = Trials.FirstOrDefault(x => x.index == t.reference).trials;
                        else
                            TrialCount = 0;

                        //bool success = false;
                        if (Terraria.ObjectData.TileObjectData.CustomPlace(t.Type, t.Style) && t.Type != 82 && TrialCount < 2)
                        {
                            var success = WorldGen.PlaceObject(t.X, t.Y, t.Type, false, style: t.Style, alternate: t.Alt, random: t.Rand, direction: t.Direction ? 1 : -1);
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
                                if (t.reference == 0)
                                    t.reference = e;

                                if (Trials.Any(x => x.index == t.reference))
                                    Trials.Find(x => x.index == t.reference).trials++;

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
                        if (t.Type == TileID.Signs || t.Type == TileID.Tombstones || t.Type == TileID.AnnouncementBox)
                        {
                            int signI = Sign.ReadSign(t.X, t.Y);
                            if (signI >= 0)
                                Sign.TextSign(signI, "");
                        }
                        //Mannequins
                        else if (t.Type == TileID.Mannequin || t.Type == TileID.Womannequin)
                        {
                            //x,y should be bottom left, Direction is already done via PlaceObject so we add the item values.
                            Main.tile[t.X, t.Y - 2].frameX += (short)(t.Paint * 100);
                            Main.tile[t.X, t.Y - 1].frameX += (short)((t.Alt & 0x3FF) * 100);
                            Main.tile[t.X, t.Y].frameX += (short)((t.Alt >> 10) * 100);
                        }
                        // Restore Weapon Rack if it had a netID
                        else if ((t.Type == TileID.WeaponsRack || t.Type == TileID.WeaponsRack2) && t.Alt > 0)
                        {
                            int mask = 5000;// +(direction ? 15000 : 0);
                            Main.tile[t.X - 1, t.Y].frameX = (short)(t.Alt + mask + 100);
                            Main.tile[t.X, t.Y].frameX = (short)(t.Paint + mask + 5000);
                        }
                        // Restore Item Frame
                        else if (t.Type == TileID.ItemFrame)
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
                            if (t.Type == TileID.GrandfatherClocks)
                                TSPlayer.All.SendTileSquareCentered(t.X, t.Y - 2, 8);
                            else
                                TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 8);//This can be very large, or too small in some cases
                        else
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        break;
                    case 1://add tile
                        bool delete = Main.tile[t.X, t.Y].active();
                        if (!delete && Main.tileSand[t.Type])//sand falling compensation (it may have fallen down)
                        {
                            int newY = t.Y + 1;
                            while (newY < Main.maxTilesY - 1 && !Main.tile[t.X, newY].active())
                                newY++;
                            
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
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 3://add wall
                        if (Main.tile[t.X, t.Y].wall != 0)
                        {
                            Main.tile[t.X, t.Y].wall = 0;
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 5://placewire
                        if (Main.tile[t.X, t.Y].wire())
                        {
                            WorldGen.KillWire(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 6://killwire
                        if (!Main.tile[t.X, t.Y].wire())
                        {
                            WorldGen.PlaceWire(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 7://poundtile
                        WorldGen.PoundTile(t.X, t.Y);
                        TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        break;
                    case 8://placeactuator
                        if (Main.tile[t.X, t.Y].actuator())
                        {
                            WorldGen.KillActuator(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 9://killactuator
                        if (!Main.tile[t.X, t.Y].actuator())
                        {
                            WorldGen.PlaceActuator(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 10://placewire2
                        if (Main.tile[t.X, t.Y].wire2())
                        {
                            WorldGen.KillWire2(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 11://killwire2
                        if (!Main.tile[t.X, t.Y].wire2())
                        {
                            WorldGen.PlaceWire2(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 12://placewire3
                        if (Main.tile[t.X, t.Y].wire3())
                        {
                            WorldGen.KillWire3(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 13://killwire3
                        if (!Main.tile[t.X, t.Y].wire3())
                        {
                            WorldGen.PlaceWire3(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 14://slopetile
                        Main.tile[t.X, t.Y].slope((byte)(t.Paint >> 8));
                        Main.tile[t.X, t.Y].halfBrick((t.Paint & 128) == 128);
                        TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        break;
                    case 15: //frame track
                             //see above
                        break;
                    case 16:
                        if (Main.tile[t.X, t.Y].wire4())
                        {
                            WorldGen.KillWire4(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 17:
                        if (!Main.tile[t.X, t.Y].wire4())
                        {
                            WorldGen.PlaceWire4(t.X, t.Y);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 21:
                        if (Main.tile[t.X, t.Y].active())
                        {
                            int prevTile = t.Type & 0xFFFF;
                            int placedTile = (t.Type >> 16) & 0xFFFF;

                            WorldGen.PlaceTile(t.X, t.Y, prevTile, false, true, -1, style: t.Style);
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
                        }
                        break;
                    case 22:
                        int prevWall = t.Type & 0xFFFF;
                        int placedWall = (t.Type >> 16) & 0xFFFF;
                        if (Main.tile[t.X, t.Y].wall != prevWall) //change if not what was replaced
                        {
                            Main.tile[t.X, t.Y].wall = (byte)prevWall;
                            TSPlayer.All.SendTileSquareCentered(t.X, t.Y, 1);
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
