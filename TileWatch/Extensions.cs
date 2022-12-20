using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    }
}
