using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace TileWatch
{
    public class Debug
    {
        public static void OutputTileEdit(byte action, ITile? tile, byte slope, bool halfbrick, int x, int y, ushort flags, byte flags2)
        {
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
            return;
#endif
        }


    }
}
