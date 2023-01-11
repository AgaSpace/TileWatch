using Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileWatch
{
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

            private byte _paint;
            public byte Paint
            {
                get => _paint;
            
                set { _ = this.SaveAsync(x => x.Paint, value); _paint = value; }
            }

            private byte _slope;
            public byte Slope
            {
                get => _slope;

                set { _ = this.SaveAsync(x => x.Slope, value); _slope = value; }
            }

            private byte _style;
        
            public byte Style
            {
                get => _style;

                set { _ = this.SaveAsync(x => x.Style, value); _style = value; }
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

            private bool _inactive;

            public bool Inactive
            {
                get => _inactive;

                set { _ = this.SaveAsync(x => x.Inactive, value); _inactive = value; }
            }

            private bool _wall;
            public bool Wall
            {
                get => _wall;

                set { _ = this.SaveAsync(x => x.Wall, value); _wall = value; }
            }

            private bool _object;
            public bool Object
            {
                get => _object;

                set { _ = this.SaveAsync(x => x.Object, value); _object = value; }
            }

            private int _alt;

            public int Alt
            {
                get => _alt;

                set { _ = this.SaveAsync(x => x.Alt, value); _alt = value; }
            }


            private int _rand;

            public int Rand
            {
                get => _rand;

                set { _ = this.SaveAsync(x => x.Rand, value); _rand = value; }
            }

            private bool _dir;

            public bool Direction
            {
                get => _dir;

                set { _ = this.SaveAsync(x => x.Direction, value); _dir = value; }
            }



    }

}
