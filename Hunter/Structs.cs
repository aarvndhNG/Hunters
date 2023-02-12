using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hunting
{
    class Structs
    {
        public enum Team
        {
            无,
            红,
            绿,
            蓝,
            黄,
            紫
        }
        public class SavePlayer
        {
            public int Saver { get; set; }
            public int who { get; set; }
            public Thread SavePlayerLoop { get; set; }
        }
        /// <summary>
        /// 全局参数
        /// </summary>
        public static class Values
        {
            public static Dictionary<int, Item[]> VisitorInvs = new Dictionary<int, Item[]>();
            public static Dictionary<int, SavePlayer> SavePlayerLoop = new Dictionary<int, SavePlayer>();
            public static Thread JoinThread = null;
            public static Dictionary<int, Thread> UnSafePlayerLoops = new Dictionary<int, Thread>();
            /// <summary>
            /// 玩家原始最大生命值
            /// </summary>
            public static Dictionary<int, int> PlayerHP = new Dictionary<int, int>();
            /// <summary>
            /// 游戏状态
            /// </summary>
            public static Status Game = Status.sleeping;
            /// <summary>
            /// 全局随机因子
            /// </summary>
            public static Random random = new Random();
            /// <summary>
            /// 被击倒的玩家
            /// </summary>
            public static List<int> Falled = new List<int>();
            /// <summary>
            /// 被击杀的玩家
            /// </summary>
            public static List<int> Killed = new List<int>();
            /// <summary>
            /// 所有玩家， 是否已加入游戏字典
            /// </summary>
            public static Dictionary<int, bool> playing = new Dictionary<int, bool>();
            /// <summary>
            /// 全局config参数
            /// </summary>
            public static Config config;
            /// <summary>
            /// 玩家背包字典
            /// </summary>
            public static Dictionary<int, Item[]> PlayerInvs = new Dictionary<int, Item[]>();
            /// <summary>
            /// 全局安全区最大Tilex参数
            /// </summary>
            public static int SafeMin;
            /// <summary>
            /// 全局安全区最小Tilex参数
            /// </summary>
            public static int SafeMax;
            /// <summary>
            /// 全局是否正在轰炸参数
            /// </summary>
            public static bool IsDestroy = false;
            /// <summary>
            /// 全局轰炸区最大Tilex参数
            /// </summary>
            public static int DestroyMax;
            /// <summary>
            /// 全局轰炸区最小TilleX参数
            /// </summary>
            public static int DestroyMin;
            /// <summary>
            /// 所有玩家队伍
            /// </summary>
            public static Dictionary<int, Team> Teams = new Dictionary<int, Team>();
            /// <summary>
            /// 所有玩家上个坐标字典
            /// </summary>
            public static Dictionary<int, Vector2> LastPosition = new Dictionary<int, Vector2>();
            /// <summary>
            /// 玩家分数字典
            /// </summary>
            public static Dictionary<string, int> Grades = new Dictionary<string, int>();
            /// <summary>
            /// 安全区缩小进程
            /// </summary>
            public static Thread SafeCircleLoop;
            /// <summary>
            /// 空投生成进程
            /// </summary>
            public static Thread DropItemLoop;
            /// <summary>
            /// 随机轰炸区进程
            /// </summary>
            public static Thread DestroyCircleLoop;
        }
        public enum Status
        {
            sleeping,
            playing,
            end
        }
        public class Circle
        {
            public int Smaller { get; set; }
            public int Num { get; set; }
            public int Break { get; set; }
        }
        public class RandomItem
        {
            public int Rate { get; set; }
            public Item Item { get; set; }
        }
        public class Point
        {
            public int TileX { get; set; }
            public int TileY { get; set; }
        }
        public class Item
        {
            public int netID { get; set; }
            public int stack { get; set; }
            public byte prefix { get; set; }
            public static Item Parse(Terraria.Item i)
            {
                return new Item { netID = i.netID, stack = i.stack, prefix = i.prefix };
            }
            public static Item[] Parse(Terraria.Item[] i)
            {
                List<Item> items = new List<Item>();
                foreach (var it in i)
                {
                    items.Add(Parse(it));
                }
                return items.ToArray();
            }
        }
    }
}
