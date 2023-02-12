using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Hunting
{
    class Config
    {
        /// <summary>
        /// Config路径
        /// </summary>
        private const string path = "tshock\\Hunting.json";
        /// <summary>
        /// 出生点
        /// </summary>
        public Structs.Point Hall { get; set; }
        /// <summary>
        /// 起始位置
        /// </summary>
        public Structs.Point Start { get; set; }
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }
        /// <summary>
        /// 最大加入限时
        /// </summary>
        public int MaxJoinSec { get; set; }
        /// <summary>
        /// 开始玩家人数
        /// </summary>
        public int Starters { get; set; }
        /// <summary>
        /// 安全区缩小设置
        /// </summary>
        public Structs.Circle Circle { get; set; }
        /// <summary>
        /// 初始背包
        /// </summary>
        public Structs.Item[] StartInv { get; set; }
        /// <summary>
        /// 箱子内最多随机生成物品
        /// </summary>
        public int MaxItems { get; set; }
        /// <summary>
        /// 箱子内最少随机生成物品
        /// </summary>
        public int MinItems { get; set; }
        /// <summary>
        /// 最大空投物品
        /// </summary>
        public int MaxDrop { get; set; }
        /// <summary>
        /// 轰炸区大小
        /// </summary>
        public int DestroySize { get; set; }
        /// <summary>
        /// 弹幕生成高度
        /// </summary>
        public int DestroyTileY { get; set; }
        /// <summary>
        /// 玩家初始生命
        /// </summary>
        public int StartLife { get; set; }
        /// <summary>
        /// 进入毒圈buff
        /// </summary>
        public int[] UnSafeBuffs { get; set; }
        /// <summary>
        /// 玩家被击倒buff
        /// </summary>
        public int[] FalledBuffs { get; set; }
        /// <summary>
        /// 箱子内的随机物品
        /// </summary>
        public Structs.RandomItem[] RandomItems { get; set; }
        /// <summary>
        /// 空投奖池
        /// </summary>
        public Structs.RandomItem[] Airdrops { get; set; }
        /// <summary>
        /// 自动跳伞倒计时
        /// </summary>
        public int JumpTimer { get; set; }
        public Config(Structs.Point hall, Structs.Point start,
            bool enable, int starters, Structs.Circle circle,
            int maxItem, int minItem, int maxDrop, int destroySize, int destroyTileY,
            int maxJoinSec,
            int[] unSafeBuffs, int[] falledBuffs,int startLife,
            Structs.Item[] startInv,Structs.RandomItem[] randomItems,
            Structs.RandomItem[] airdrops,int jumpTimer)
        {
            Hall = hall;
            Start = start;
            Enable = enable;
            Starters = starters;
            MaxItems = maxItem;
            MinItems = minItem;
            MaxDrop = maxDrop;
            Circle = circle;
            MaxJoinSec = maxJoinSec;
            UnSafeBuffs = unSafeBuffs;
            FalledBuffs = falledBuffs;
            StartLife = startLife;
            DestroySize = destroySize;
            DestroyTileY = destroyTileY;
            StartInv = startInv;
            RandomItems = randomItems;
            Airdrops = airdrops;
            JumpTimer = jumpTimer;
        }
        public static Config GetConfig()
        {
            Config result = new Config(null, null, false, 5,new Structs.Circle()
            {
                Break = 60 * 3,
                Num = 5,
                Smaller = 70
            },5,1,1,400,200,30,new int[2] { 39, 32 },
            new int[2] { 197, 20 },200,new Structs.Item[0],
            new Structs.RandomItem[0],new Structs.RandomItem[0],60);
            if (File.Exists(path))
            {
                using (StreamReader re=new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<Config>(re.ReadToEnd());
                }
            }
            else
            {
                result.Save();
            }
            return result;
        }
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter(path))
            {
                wr.WriteLine(JsonConvert.SerializeObject(this,Formatting.Indented));
            }
            //Console.WriteLine("配置文件已保存！");
        }
    }
}
