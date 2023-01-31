using Auxiliary;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;

namespace TileWatch
{
    public static class Extensions
    {
        public static IEnumerable<(string, int)> GetIds(Type type)
        {
            foreach (FieldInfo info in type.GetFields())
            {
                if (info.IsLiteral && !info.IsInitOnly)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < info.Name.Length; i++)
                    {
                        if (char.IsUpper(info.Name[i]) && i > 0)
                            sb.Append(' ').Append(info.Name[i]);

                        else
                            sb.Append(info.Name[i]);
                    }

                    var value = info.GetValue(null);

                    yield return (sb.ToString(), Convert.ToInt32(value));
                }


            }
        }

        public enum CommandType {
            Rollback = 0,
            Undo = 1
        }

        public static int AdjustForUndo(int _action)
        {
            int action = _action;

            switch (action)
            {
                //undoing a kill tile, should set action to place tile
                case 0:
                case 4:
                case 20:
                    action = 1;
                    break;
                case 1: // undoing a place tile, should set action to kill tile
                    action = 0;
                    break;
                case 2: // killing a wall, should set action to place wall
                    action = 3;
                    break;
                case 3: //place wall -> kill wall
                    action = 2;
                    break;
                case 5: //place wire -> kill wire
                    action = 6;
                    break;
                case 6: //kill wire -> place wire
                    action = 5;
                    break;
                case 8://place actuator -> kill actuator
                    action = 9;
                    break;
                case 9://kill actuator -> place actuator
                    action = 8;
                    break;
                case 10://placewire2 -> killwire2
                    action = 11;
                    break;
                case 11://killwire2 => placewire2
                    action = 10;
                    break;
                case 12://placewire3 -> killwire3
                    action = 13;
                    break;
                case 13://killwire3 -> placewire3
                    action = 12;
                    break;
                case 16://killwire4 -> placewire4
                    action = 17;
                    break;
                case 17://placewire4-> killwire4
                    action = 16;
                    break;
/* need to do this later when im not exhausted */
              /*  case 21:
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
                    break;*/

            }
            

            return action;
        }

        public static void CompleteAction(CommandType type, TSPlayer player, Tile t, List<Tile> tiles, int index, ref List<Trial> Trials)
        {
            //get the Terraria tile within the world
            ITile? i = Main.tile[t.X, t.Y];
            int action = t.Action;

            //the tile may be referencing another tile, if it isn't, add it to the Trials list as the first Trial.
            if (t.reference == 0)
                Trials.Add(new Trial { index = index, trials = 0 });

            if (type == CommandType.Undo)
                action = AdjustForUndo(action);

            switch (action)
            {
                #region KILL TILE
                case 0:
                case 4://del tile
                case 20://trykilltile 
                    if (Main.tileSand[t.Type]) //compensates for falling sand (need to check up for top of sand)
                    {
                        int newY = t.Y;
                        while (newY > 0 && Main.tile[t.X, newY].active() && Main.tile[t.X, newY].type == t.Type)
                            newY--;

                        if (Main.tile[t.X, newY].active())
                            return;
                        t.Y = newY;
                    }

                    // if tree, grow another
                    else if (t.Type == TileID.Trees)
                    { WorldGen.GrowTree(t.X, t.Y + 1); return; }

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
                        return;
                    }

                    //conditional statement checking if tile is the same as the one it would be replaced with
                    if (Main.tile[t.X, t.Y].active() && Main.tile[t.X, t.Y].type == t.Type)
                    {
                        if (t.Type == TileID.MinecartTrack || t.Type == TileID.ItemFrame)
                            goto frameOnly;
                        return;
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
                                t.reference = index;

                            if (Trials.Any(x => x.index == t.reference))
                                Trials.Find(x => x.index == t.reference).trials++;

                            tiles.Add(t);
                            return;
                        }
                        else
                        {
                            return;
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
                #endregion
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





        #region Stole these from OG History

        static void getPlaceData(ushort type, ref int which, ref int div)
        {
            switch (type)
            {
                //WHICH block style is in 0:X   1:Y
                case 314: //minecart ????
                    which = 0;
                    div = 1;
                    break;
                case 13: //bottle
                case 36: //present
                         //case 49: //water candle Removing - No different styles?
                case 174: //platinum candle
                          //case 78: //clay pot
                case 82: //herb
                case 83: //herb
                case 84: //herb
                case 91: //banner
                case 144: //timer
                case 149: //christmas light
                case 178: //gems
                case 184:
                case 239: //bars
                case 419:
                    which = 0;
                    div = 18;
                    break;
                case 19: //platforms
                case 135: //pressure plates
                case 136: //switch (state)
                case 137: //traps
                case 141: //explosives
                case 210: //land mine
                case 380: //planter box
                case 420: //L Gate
                case 423: // L Sensor
                case 424: //Junction Box
                case 428: // Weighted Presure Plate
                case 429: //Wire bulb
                case 445: //Pixel Box
                    which = 1;
                    div = 18;
                    break;
                case 4: //torch
                case 33: //candle
                case 324: //beach piles
                    which = 1;
                    div = 22;
                    break;
                case 227: //dye plants
                    which = 0;
                    div = 34;
                    break;
                case 16: //anvil
                case 18: //work bench
                case 21: //chest
                case 27: //sunflower (randomness)
                case 29:
                case 55: // sign
                case 85: //tombstone
                case 103:
                case 104: //grandfather
                case 128: // mannequin (orient)
                case 132: //lever (state)
                case 134:
                case 207: // water fountains
                case 245: //2x3 wall picture
                case 254:
                case 269: // womannequin
                case 320: //more statues
                case 337: //     statues
                case 376: //fishing crates
                case 378: //target dummy
                case 386: //trapdoor open
                case 395:
                case 410: //lunar monolith
                case 411: //Detonator
                case 425: // Announcement (Sign)
                case 441:
                case 443: //Geyser
                    which = 0;
                    div = 36;
                    break;
                case 35://jack 'o lantern??
                case 42://lanterns
                case 79://beds (orient)
                case 90://bathtub (orient)
                case 139://music box
                case 246:// 3x2 wall painting
                case 270:
                case 271:
                    which = 1;
                    div = 36;
                    break;
                case 172: //sinks
                    which = 1;
                    div = 38;
                    break;
                case 15://chair
                case 20:
                case 216:
                case 338:
                    which = 1;
                    div = 40;
                    break;
                case 14:
                case 17:
                case 26:
                case 86:
                case 87:
                case 88:
                case 89:
                case 114:
                case 186:
                case 187:
                case 215:
                case 217:
                case 218:
                case 237:
                case 244:
                case 285:
                case 286:
                case 298:
                case 299:
                case 310:
                case 106:
                case 170:
                case 171:
                //case 172:
                case 212:
                case 219:
                case 220:
                case 228:
                case 231:
                case 243:
                case 247:
                case 283:
                case 300:
                case 301:
                case 302:
                case 303:
                case 304:
                case 305:
                case 306:
                case 307:
                case 308:
                case 77:
                case 101:
                case 102:
                case 133:
                case 339:
                case 235: //teleporter
                case 377: //sharpening station
                case 405: //fireplace
                    which = 0;
                    div = 54;
                    break;
                case 10:
                case 11: //door
                case 34: //chandelier
                case 93: //tikitorch
                case 241: //4x3 wall painting
                    which = 1;
                    div = 54;
                    break;
                case 240: //3x3 painting, style stored in both
                case 440:
                    which = 2;
                    div = 54;
                    break;
                case 209:
                    which = 0;
                    div = 72;
                    break;
                case 242:
                    which = 1;
                    div = 72;
                    break;
                case 388: //tall gate closed
                case 389: //tall gate open
                    which = 1;
                    div = 94;
                    break;
                case 92: // lamp post
                    which = 1;
                    div = 108;
                    break;
                case 105: // Statues
                    which = 3;
                    break;
                default:
                    break;
            }
        }

        static Vector2 destFrame(ushort type)
        {
            Vector2 dest;
            switch (type)//(x,y) is from top left
            {
                case 16:
                case 18:
                case 29:
                case 42:
                case 91:
                case 103:
                case 134:
                case 270:
                case 271:
                case 386:
                case 387:
                case 388:
                case 389:
                case 395:
                case 443:
                    dest = new Vector2(0, 0);
                    break;
                case 15:
                case 21:
                case 35:
                case 55:
                case 85:
                case 139:
                case 216:
                case 245:
                case 338:
                case 390:
                case 425:
                    dest = new Vector2(0, 1);
                    break;
                case 34:
                case 95:
                case 126:
                case 246:
                case 235:// (1,0)
                    dest = new Vector2(1, 0);
                    break;
                case 14:
                case 17:
                case 26:
                case 77:
                case 79:
                case 86:
                case 87:
                case 88:
                case 89:
                case 90:
                case 94:
                case 96:
                case 97:
                case 98:
                case 99:
                case 100:
                case 114:
                case 125:
                case 132:
                case 133:
                case 138:
                case 142:
                case 143:
                case 172: //sinks
                case 173:
                case 186:
                case 187:
                case 215:
                case 217:
                case 218:
                case 237:
                case 240:
                case 241:
                case 244:
                case 254:
                case 282:
                case 285:
                case 286:
                case 287:
                case 288:
                case 289:
                case 290:
                case 291:
                case 292:
                case 293:
                case 294:
                case 295:
                case 298:
                case 299:
                case 310:
                case 316:
                case 317:
                case 318:
                case 319:
                case 334:
                case 335:
                case 339:
                case 354:
                case 355:
                case 360:
                case 361:
                case 362:
                case 363:
                case 364:
                case 376:
                case 377:
                case 391:
                case 392:
                case 393:
                case 394:
                case 405:
                case 411:
                case 440:
                case 441:// (1,1)
                    dest = new Vector2(1, 1);
                    break;
                case 105:// Statues use (0,2) from PlaceTile, but (1,2) from PlaceObject, strange
                case 106:
                case 209:
                case 212:
                case 219:
                case 220:
                case 228:
                case 231:
                case 243:
                case 247:
                case 283:
                case 300:
                case 301:
                case 302:
                case 303:
                case 304:
                case 305:
                case 306:
                case 307:
                case 308:
                case 349:
                case 356:
                case 378:
                case 406:
                case 410:
                case 412:// (1,2)
                    dest = new Vector2(1, 2);
                    break;
                case 101:
                case 102:// (1,3)
                    dest = new Vector2(1, 3);
                    break;
                case 242:// (2,2)
                    dest = new Vector2(2, 2);
                    break;
                case 10:
                case 11: // Door, Ignore framex*18 for 10, not 11
                case 93:
                case 128:
                case 269:
                case 320:
                case 337: // (0,2)
                    dest = new Vector2(0, 2);
                    break;
                case 207:
                case 27: // (0,3)
                    dest = new Vector2(0, 3);
                    break;
                case 104: // (0,4)
                    dest = new Vector2(0, 4);
                    break;
                case 92: // (0,5)
                    dest = new Vector2(0, 5);
                    break;
                case 275:
                case 276:
                case 277:
                case 278:
                case 279:
                case 280:
                case 281:
                case 296:
                case 297:
                case 309:// (3,1)
                    dest = new Vector2(3, 1);
                    break;
                case 358:
                case 359:
                case 413:
                case 414:
                    dest = new Vector2(3, 2);
                    break;
                default:
                    dest = new Vector2(-1, -1);
                    break;
            }
            return dest;
        }
        //This finds the 0,0 of a furniture
        static Vector2 adjustDest(ref Vector2 dest, ITile tile, int which, int div, byte style)
        {
            Vector2 relative = new Vector2(0, 0);
            if (dest.X < 0)
            {
                //no destination
                dest.X = dest.Y = 0;
                return relative;
            }
            int frameX = tile.frameX;
            int frameY = tile.frameY;
            int relx = 0;
            int rely = 0;
            //Remove data from Mannequins before adjusting
            if (tile.type == 128 || tile.type == 269)
                frameX %= 100;
            switch (which)
            {
                case 0:
                    relx = (frameX % div) / 18;
                    rely = (frameY) / 18;
                    break;
                case 1:
                    relx = (frameX) / 18;
                    rely = (frameY % div) / 18;
                    break;
                case 2:
                    relx = (frameX % div) / 18;
                    rely = (frameY % div) / 18;
                    break;
                case 3: // Statues have style split, possibly more use this?
                    rely = (frameY % 54) / 18;
                    relx = (frameX % 36) / 18;
                    break;
                default:
                    relx = (frameX) / 18;
                    rely = (frameY) / 18;
                    break;
            }
            if (tile.type == 55 || tile.type == 425)//sign
            {
                switch (style)
                {
                    case 1:
                    case 2:
                        dest.Y--;
                        break;
                    case 3:
                        dest.Y--;
                        dest.X++;
                        break;
                }
            }
            else if (tile.type == 11)//opened door
            {
                if (frameX / 36 > 0)
                {
                    relx -= 2;
                    dest.X++;
                }
            }
            else if (tile.type == 10 || tile.type == 15)// random frames, ignore X
            {
                relx = 0;
            }
            else if (tile.type == 209)//cannoonn
            {
                rely = (frameY % 37) / 18;
            }
            else if (tile.type == 79 || tile.type == 90)//bed,bathtub
            {
                relx = (frameX % 72) / 18;
            }
            else if (tile.type == 14 && style == 25)
            {
                dest.Y--;
            }
            else if (tile.type == 334)
            {
                rely = frameY / 18;
                int tx = frameX;
                if (frameX > 5000)
                    tx = ((frameX / 5000) - 1) * 18;
                if (tx >= 54)
                    tx = (tx - 54);
                relx = tx / 18;
            }
            relative = new Vector2(relx, rely);

            return relative;
        }

        public static Vector2? adjustFurniture(ref int x, ref int y, ref byte style, bool origin = false)
        {
            int which = 10; // An invalid which, to skip cases if it never changes.
            int div = 1;
            ITile tile = Main.tile[x, y];
            getPlaceData(tile.type, ref which, ref div);
            switch (which)
            {
                case 0:
                    style = (byte)(tile.frameX / div);
                    break;
                case 1:
                    style = (byte)(tile.frameY / div);
                    break;
                case 2:
                    style = (byte)((tile.frameY / div) * 36 + (tile.frameX / div));
                    break;
                case 3: //Just statues for now
                    style = (byte)((tile.frameX / 36) + (tile.frameY / 54) * 55);
                    break;
                default:
                    break;
            }
            
            if (style < 0) style = 0;
            if (!Main.tileFrameImportant[tile.type]) return null;
            if (div == 1) div = 0xFFFF;
            Vector2 dest = destFrame(tile.type);
            Vector2 relative = adjustDest(ref dest, tile, which, div, style);
            if (origin) dest = new Vector2(0, 0);
            x += (int)(dest.X - relative.X);
            y += (int)(dest.Y - relative.Y);
            return new Vector2(x, y);
        }

        public static byte GetStyle(Terraria.ITile tile)
        {
            int which = 10;
            int div = 1;
            getPlaceData(tile.type, ref which, ref div);
            byte style = 0;
            switch (which)
            {
                case 0:
                    style = (byte)(tile.frameX / div);
                    break;
                case 1:
                    style = (byte)(tile.frameY / div);
                    break;
                case 2:
                    style = (byte)((tile.frameY / div) * 36 + (tile.frameX / div));
                    break;
                case 3: //Just statues for now
                    style = (byte)((tile.frameX / 36) + (tile.frameY / 54) * 55);
                    break;
                default:
                    break;
            }
            if (style < 0) style = 0;
            return style;
        }

        #endregion

        public static string ParseDate(DateTime time)
        {
            TimeSpan timeDiff = DateTime.UtcNow - time;
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

            return dhms.ToString();
        }
    }


    public class Trial
    {
        public int index;
        public int trials;
        public int[] child;
    }
}
