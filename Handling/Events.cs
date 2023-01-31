using Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace TileWatch.Handling
{
    public class Events
    {
        public async static Task<Tile> AddHistory(TSPlayer player, byte action, int x, int y, bool wall, ushort wallType, byte wallPaint, ushort type, byte paint, byte slope, bool halfbrick, ushort flags, byte flags2, bool inactive)
        {
            return await IModel.CreateAsync(CreateRequest.Bson<Tile>(t =>
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
        }

        public async static Task<Tile> ObjectAddHistory(TSPlayer player, int _x, int y, int alt, bool direction, int random, byte style, ushort type)
        {
            return await IModel.CreateAsync(CreateRequest.Bson<Tile>(x =>
            {
                x.Action = 1;
                x.Wall = false;
                x.Alt = alt;
                x.Direction = direction;
                x.Rand = random;
                x.Style = style;
                x.Type = type;
                x.X = _x;
                x.Y = y;
                x.Inactive = false;
                x.Object = true;
                x.Player = player.Account.ID;
                x.Time = DateTime.Now;
                x.RolledBack = false;
            }));
        }

    }
}
