using Microsoft.Xna.Framework;
using System.Reflection;
using System.Text;
using Terraria;

namespace TileWatch
{
    internal class Extensions
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

        #endregion
    }


    public class Trial
    {
        public int index;
        public int trials;
        public int[] child;
    }
}
