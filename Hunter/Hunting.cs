using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Threading;
using System.IO;

namespace Hunting
{
    [ApiVersion(2, 1)]
    public class Hunting : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "一起享受狩猎的乐趣吧！";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "Hunting";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 4, 1, 5);

        /// <summary>
        /// Initializes a new instance of the Hunting class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public Hunting(Main game) : base(game)
        {

        }
        
        /// <summary>
        /// Handles plugin disposal logic.
        /// *Supposed* to fire when the server shuts down.
        /// You should deregister hooks and free all resources here.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                foreach (var cmd in cmds)
                {
                    Commands.ChatCommands.Remove(cmd);
                }
                Structs.Values.config.Save();
                // Deregister hooks here
            }
            base.Dispose(disposing);
        }
        List<Command> cmds = new List<Command>();
        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            cmds.Add(new Command("hunting.admin", setting, "set"));
            cmds.Add(new Command("hunting.use",join, "join"));
            cmds.Add(new Command("hunting.use",count, "count"));
            cmds.Add(new Command("hunting.admin",over, "over"));
            //cmds.Add(new Command(test, "test"));
            foreach (var cmd in cmds)
            {
                Commands.ChatCommands.Add(cmd);
            }
            Structs.Values.config = Config.GetConfig();
        }

        private void test(CommandArgs args)
        {
            using (MemoryStream data = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(data))
                {
                    writer.Write((short)8);
                    writer.Write((byte)PacketTypes.PlayerMana);
                    writer.Write((byte)args.Player.Index);
                    writer.Write((short)999);
                    writer.Write((short)999);
                }
                args.Player.SendRawData(data.ToArray());
            }
            //Utils.DropItem(new Structs.Item() { netID = 1, prefix = 0, stack = 1 }, args.Player.TileX, args.Player.TileY);
        }

        private void count(CommandArgs args)
        {
            if (Structs.Values.Game == Structs.Status.sleeping)
            {
                args.Player.SendErrorMessage("游戏未开始，暂无数据");
            }
            var PlayerRank = Utils.RankPlayers();
            int rank = PlayerRank.Count;
            foreach (var k in Utils.RankPlayers())
            {
                args.Player.SendInfoMessage($"第{rank}名：{k.Key}，总分：{k.Value}");
                rank--;
            }
            args.Player.SendInfoMessage("以下为玩家总分排名", Color.Yellow);
            var TeamRank = Utils.RankTeams();
            rank = TeamRank.Count;
            foreach (var k in TeamRank)
            {
                var color = Utils.GetTeamColor(k.Key);
                var inTeamRank = Utils.InTeamRank(k.Key);
                var _rank = inTeamRank.Count;
                foreach (var _k in inTeamRank)
                {
                    args.Player.SendInfoMessage($"{_rank}.{_k.Key}，总分：{_k.Value}");
                    _rank--;
                }
                args.Player.SendInfoMessage($"第{rank}名：{k.Key}队，总分：{k.Value}");
                rank--;
            }
            foreach (var i in Utils.GetPlayers())
            {
                args.Player.SendInfoMessage($"{Utils.GetPlayerName(i)}");
            }
            args.Player.SendInfoMessage("剩余玩家:", Color.Yellow);
        }


        private void over(CommandArgs args)
        {
            try
            {
                Utils.GameOver();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private void join(CommandArgs args)
        {
            if (Utils.IsPlayer(args.Player.Index))
            {
                args.Player.SendErrorMessage("您已加入游戏，不可重复加入");
                return;
            }
            if (args.Parameters.Count != 0 && args.Parameters[0] == "leave")
            {
                if (args.Player.TPlayer.ghost)
                {
                    //Utils.SetGhost(args.Player.Index, false);
                    Utils.SetPlayerInv(args.Player.Index, Structs.Values.VisitorInvs[args.Player.Index]);
                    Structs.Values.VisitorInvs.Remove(args.Player.Index);
                    Utils.Teleport(args.Player.Index, Structs.Values.config.Hall);
                    args.Player.SendSuccessMessage("您已被传送回大厅");
                }
                else
                {
                    args.Player.SendErrorMessage("您不在观战模式");
                }
                return;
            }
            if (!Structs.Values.config.Enable)
            {
                args.Player.SendErrorMessage("游戏未启用，请联系管理员启用");
                return;
            }
            if (Structs.Values.Game == Structs.Status.playing)
            {
                if (args.Parameters.Count == 0)
                {
                    foreach(var i in Utils.GetPlayers())
                    {
                        args.Player.SendInfoMessage(Utils.GetPlayerName(i));
                    }
                    args.Player.SendInfoMessage("当前剩余玩家");
                    args.Player.SendInfoMessage("游戏正在进行,您可输入/join 玩家名，传送至某一玩家观战");
                    return;
                }
                if(TSPlayer.FindByNameOrID(args.Parameters[0]).Count == 0)
                {
                    args.Player.SendErrorMessage("不存在玩家:" + args.Parameters[0]);
                    return;
                }
                var who = TSPlayer.FindByNameOrID(args.Parameters[0])[0];
                if (Utils.IsPlayer(who.Index))
                {
                    args.Player.Teleport(who.X, who.Y);
                    Structs.Values.VisitorInvs.Add(args.Player.Index, Utils.GetPlayerBank(args.Player.Index));
                    //Utils.SetGhost(args.Player.Index, true);
                    args.Player.SendSuccessMessage("您已传送至玩家:" + who.Name + "输入/join leave返回大厅");
                }
                return;
            }
            Structs.Values.playing[args.Player.Index] = true;
            TShock.Utils.Broadcast($"玩家{args.Player.Name}已加入，进度{Utils.GetPlayers().Count}/" +
                $"{Structs.Values.config.Starters}",Color.Blue);
            if(Utils.GetPlayers().Count>= Structs.Values.config.Starters&&Structs.Values.JoinThread==null)
            {
                Structs.Values.JoinThread = new Thread(Utils.JoinLoop);
                Structs.Values.JoinThread.IsBackground = true;
                Structs.Values.JoinThread.Start();
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            try
            {
                var items = Structs.Values.VisitorInvs[args.Who];
                Utils.SetPlayerInv(args.Who, items);
                Structs.Values.VisitorInvs.Remove(args.Who);
            }
            catch { }
            try
            {
                if (Utils.IsPlayer(args.Who))
                {
                    Utils.SetPlayerInv(args.Who, Structs.Values.PlayerInvs[args.Who]);
                }
                Structs.Values.playing.Remove(args.Who);
                Structs.Values.LastPosition.Remove(args.Who);
            }
            catch { }
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            try
            {
                Structs.Values.playing.Add(args.Who, false);
                Structs.Values.LastPosition.Add(args.Who,
                    new Vector2(TShock.Players[args.Who].X, TShock.Players[args.Who].Y));
            }
            catch { }
        }

        private void setting(CommandArgs args)
        {
            if (args.Parameters.Count == 0 || args.Parameters[0] == "help")
            {
                args.Player.SendInfoMessage("/set hall,设置当前坐标为大厅坐标");
                args.Player.SendInfoMessage("/set reload,重载配置文件");
                args.Player.SendInfoMessage("/set start,设置当前坐标为玩家初始位置");
                args.Player.SendInfoMessage("/set able,启用/禁用小游戏");
                args.Player.SendInfoMessage("/set starters 数量,设置开始玩家数量");
                args.Player.SendInfoMessage("/set items,设置初始物品");
                args.Player.SendInfoMessage("/set ran,设置随机生成物品");
                args.Player.SendInfoMessage("/set air,设置空投随机物品");
                args.Player.SendInfoMessage("/set drop 数量,设置空投最大生成数量");
                args.Player.SendInfoMessage("/set jump 时间(单位：秒),设置自动跳伞时间");
                return;
            }
            switch (args.Parameters[0])
            {
                case "jump":
                    {
                        int time = int.Parse(args.Parameters[1]);
                        Structs.Values.config.JumpTimer = time;
                        args.Player.SendSuccessMessage("设置成功，开局" + time + "s后自动跳伞");
                    }
                    break;
                case "drop":
                    {
                        int drop = int.Parse(args.Parameters[1]);
                        if (drop <= 0)
                        {
                            args.Player.SendErrorMessage("请输入正确的数值!");
                            return;
                        }
                        Structs.Values.config.MaxDrop = drop;
                        args.Player.SendSuccessMessage("设置成功，空投多生成" + drop + "个掉落物");
                    }
                    break;
                case "air":
                    {
                        if (args.Parameters.Count == 1)
                        {
                            args.Player.SendInfoMessage("/set air add 概率 id [数量] [前缀],添加空投随机生成物品");
                            args.Player.SendInfoMessage("/set air del id,删除空投随机生成物品");
                            args.Player.SendInfoMessage("/set air list，列出所有空投随机生成物品");
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "list":
                                {
                                    foreach (var r in Structs.Values.config.Airdrops)
                                    {
                                        var i = r.Item;
                                        args.Player.SendInfoMessage($"物品:{Lang.GetItemNameValue(i.netID)}[i/s{i.stack}:{i.netID}]," +
                                            $"前缀：{TShock.Utils.GetPrefixById(i.prefix)},生成概率{r.Rate}%,id:{i.netID},数量:{i.stack}");
                                    }
                                    args.Player.SendInfoMessage("随机生成物品如下");
                                }
                                break;
                            case "del":
                                {
                                    int id = int.Parse(args.Parameters[2]);
                                    if (!Utils.HasRan_air(id))
                                    {
                                        args.Player.SendErrorMessage($"不存在物品:[i:{id}]");
                                        return;
                                    }
                                    Utils.DelRan_air(id);
                                    args.Player.SendSuccessMessage($"成功删除物品[i:{id}]");
                                }
                                break;
                            case "add":
                                {
                                    int rate = int.Parse(args.Parameters[2]);
                                    int id = int.Parse(args.Parameters[3]);
                                    int stack = args.Parameters.Count >= 5 ? int.Parse(args.Parameters[4]) : 1;
                                    int prefix = args.Parameters.Count >= 6 ? int.Parse(args.Parameters[5]) : 0;
                                    if (rate > 0 && rate < 100)
                                    {
                                        if (stack <= 0)
                                        {
                                            args.Player.SendErrorMessage("请输入正确的数值!");
                                            return;
                                        }
                                        var ran = new Structs.RandomItem()
                                        {
                                            Item = new Structs.Item()
                                            {
                                                netID = id,
                                                stack = stack,
                                                prefix = (byte)prefix
                                            },
                                            Rate = rate
                                        };
                                        Utils.AddRan_air(ran);
                                    }
                                    args.Player.SendSuccessMessage($"随机物品:{Lang.GetItemNameValue(id)}[i/s{stack}:{id}]," +
                                        $"前缀：{TShock.Utils.GetPrefixById(prefix)},生成概率{rate}%,id:{id},数量:{stack},添加成功");
                                    //args.Player.SendSuccessMessage($"随机物品[i/s{stack}/p{prefix}:{id}]添加成功，生成概率:{rate}%");
                                }
                                break;
                        }
                    }
                    break;
                case "ran":
                    {
                        if(args.Parameters.Count == 1)
                        {
                            args.Player.SendInfoMessage("/set ran add 概率 id [数量] [前缀],添加随机生成物品");
                            args.Player.SendInfoMessage("/set ran del id,删除随机生成物品");
                            args.Player.SendInfoMessage("/set ran list，列出所有随机生成物品");
                            args.Player.SendInfoMessage("/set ran edit 最小值 最大值，修改随机生成物品数量");
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "eidt":
                                {
                                    int num1 = int.Parse(args.Parameters[2]);
                                    int num2 = int.Parse(args.Parameters[3]);
                                    int maxWea = int.Parse(args.Parameters[4]);
                                    int max = Math.Max(num1, num2);
                                    int min = Math.Min(num1, num2);
                                    if (min<0||max>Chest.maxItems||maxWea<1)
                                    {
                                        args.Player.SendErrorMessage("请输入正确的数值");
                                        return;
                                    }
                                    Structs.Values.config.MaxItems = max;
                                    Structs.Values.config.MinItems = min;
                                    args.Player.SendSuccessMessage($"随机生成物品数成功!箱子中将会生成{min}-{max}个物品");
                                }
                                break;
                            case "list":
                                {
                                    foreach(var r in Structs.Values.config.RandomItems)
                                    {
                                        args.Player.SendInfoMessage($"物品:{Lang.GetItemNameValue(r.Item.netID)}[i/s{r.Item.stack}:{r.Item.netID}]," +
                                            $"前缀：{TShock.Utils.GetPrefixById(r.Item.prefix)},生成概率{r.Rate}%,id:{r.Item.netID},数量:{r.Item.stack}");
                                    }
                                    args.Player.SendInfoMessage("随机生成物品如下");
                                }
                                break;
                            case "del":
                                {
                                    int id = int.Parse(args.Parameters[2]);
                                    if (!Utils.HasRan(id))
                                    {
                                        args.Player.SendErrorMessage($"不存在物品:[i:{id}]");
                                        return;
                                    }
                                    Utils.DelRan(id);
                                    args.Player.SendSuccessMessage($"成功删除物品[i:{id}]");
                                }
                                break;
                            case "add":
                                {
                                    int rate = int.Parse(args.Parameters[2]);
                                    int id = int.Parse(args.Parameters[3]);
                                    int stack = args.Parameters.Count >= 5 ? int.Parse(args.Parameters[4]) : 1;
                                    int prefix = args.Parameters.Count >= 6 ? int.Parse(args.Parameters[5]) : 0;
                                    if (rate > 0 && rate < 100)
                                    {
                                        if (stack <= 0)
                                        {
                                            args.Player.SendErrorMessage("请输入正确的数值!");
                                            return;
                                        }
                                        var ran = new Structs.RandomItem()
                                        {
                                            Item = new Structs.Item()
                                            {
                                                netID = id,
                                                stack = stack,
                                                prefix = (byte)prefix
                                            },
                                            Rate = rate
                                        };
                                        Utils.AddRan(ran);
                                    }
                                    args.Player.SendSuccessMessage($"随机物品:{Lang.GetItemNameValue(id)}[i/s{stack}:{id}]," +
                                        $"前缀：{TShock.Utils.GetPrefixById(prefix)},生成概率{rate}%,id:{id},数量:{stack},添加成功");
                                    //args.Player.SendSuccessMessage($"随机物品[i/s{stack}/p{prefix}:{id}]添加成功，生成概率:{rate}%");
                                }
                                break;
                        }
                    }
                    break;
                case "items":
                    {
                        if (args.Parameters.Count == 1)
                        {
                            args.Player.SendInfoMessage("/set items add 物品id [数量] [前缀],添加初始物品");
                            args.Player.SendInfoMessage("/set items del 物品id,删除初始物品");
                            args.Player.SendInfoMessage("/set items list,列出初始物品");
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "list":
                                {
                                    foreach(var i in Structs.Values.config.StartInv)
                                    {
                                        args.Player.SendInfoMessage($"{Lang.GetItemNameValue(i.netID)}" +
                                            $"[i/s{i.stack}:{i.netID}],id:{i.netID},前缀:" +
                                            $"{TShock.Utils.GetPrefixById(i.prefix)}数量:{i.stack}");
                                    }
                                    args.Player.SendInfoMessage("初始物品列表如下");
                                }
                                break;
                            case "del":
                                {
                                    int id = int.Parse(args.Parameters[2]);
                                    if (!Utils.HasItem(id))
                                    {
                                        args.Player.SendErrorMessage($"不存在物品:[i:{id}]");
                                        return;
                                    }
                                    Utils.DelItem(id);
                                    args.Player.SendSuccessMessage($"成功删除物品:[i:{id}]");
                                }
                                break;
                            case "add":
                                {
                                    int id = int.Parse(args.Parameters[2]);
                                    int stack=args.Parameters.Count >=4?int.Parse(args.Parameters[3]):1;
                                    int prefix=args.Parameters.Count >=5?int.Parse(args.Parameters[4]):0;
                                    if (stack <= 0)
                                        stack = 1;
                                    var item = new Structs.Item() { netID = id, stack = stack, prefix = (byte)prefix };
                                    Utils.AddItem(item);
                                    args.Player.SendSuccessMessage($"初始物品：{Lang.GetItemNameValue(id)}" +
                                        $"[i/s{stack}:{id}],id:{id},前缀:" +
                                        $"{TShock.Utils.GetPrefixById(prefix)}数量:{stack}添加成功");
                                    //args.Player.SendSuccessMessage($"初始物品[i/s{stack}/p{prefix}:{id}]添加成功!");
                                }
                                break;
                        }
                    }
                    break;
                case "starters":
                    {
                        int starters = int.Parse(args.Parameters[1]);
                        if (starters <= 0)
                        {
                            args.Player.SendErrorMessage("您输入的数值不正确");
                            return;
                        }
                        Structs.Values.config.Starters = starters;
                        args.Player.SendSuccessMessage("开始玩家数量已被设置为:" + starters);
                    }
                    break;
                case "able":
                    {
                        Structs.Values.config.Enable = !Structs.Values.config.Enable;
                        args.Player.SendInfoMessage("小游戏已" + (Structs.Values.config.Enable ? "启用" : "禁用"));
                    }
                    break;
                case "start":
                    {
                        var point = Utils.GetPoint(args.Player.Index);
                        Structs.Values.config.Start = point;
                        args.Player.SendSuccessMessage($"设置初始位置成功,X:{point.TileX},Y:{point.TileY}");
                    }
                    break;
                case "reload":
                    {
                        Structs.Values.config = Config.GetConfig();
                        args.Player.SendSuccessMessage("配置文件重载成功");
                    }
                    break;
                case "hall":
                    {
                        var point = Utils.GetPoint(args.Player.Index);
                        Structs.Values.config.Hall = point;
                        args.Player.SendSuccessMessage($"大厅已设置,X:{point.TileX},Y:{point.TileY}");
                    }
                    break;
            }
            Structs.Values.config.Save();
        }

        private void OnNetGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.ChestGetContents)
            {
                if (Structs.Values.Game != Structs.Status.playing)
                    return;
                args.Handled = !Utils.IsPlayer(args.Msg.whoAmI);
            }
            if(args.MsgID==PacketTypes.PlayerUpdate)
            {
                if (Structs.Values.Game != Structs.Status.playing)
                    return;
                using (BinaryReader re = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    var who = TShock.Players[re.ReadByte()];
                    if (!Utils.IsPlayer(who.Index))
                        return;
                    re.ReadBytes(5);
                    Single x = re.ReadSingle();
                    Single y = re.ReadSingle();
                    Structs.Values.LastPosition[who.Index] = new Vector2(x, y);
                    if (Utils.IsFalled(who.Index))
                    {
                        who.TPlayer.sleeping.isSleeping = true;
                        who.SendData(PacketTypes.PlayerUpdate);
                        try
                        {
                            var loop = Structs.Values.SavePlayerLoop[who.Index];
                            if (!Utils.CanSave(loop.who, loop.Saver))
                            {
                                loop.SavePlayerLoop.Abort();
                                Structs.Values.SavePlayerLoop.Remove(who.Index);
                                who.SendErrorMessage("救治中断");
                                TShock.Players[loop.Saver].SendErrorMessage($"救治玩家{who.Name}中断");
                            }
                        }
                        catch
                        {
                            foreach (var player in Utils.GetTeammates(who.Index))
                            {
                                if (player != who.Index && Utils.CanSave(who.Index, player))
                                {
                                    var data = new List<int>();
                                    data.Add(who.Index);
                                    data.Add(player);
                                    Structs.Values.SavePlayerLoop.Add(who.Index,
                                        new Structs.SavePlayer()
                                        {
                                            Saver = player,
                                            who = who.Index,
                                            SavePlayerLoop = new Thread(Utils.Save)
                                        });
                                    Structs.Values.SavePlayerLoop[who.Index].SavePlayerLoop.IsBackground = true;
                                    Structs.Values.SavePlayerLoop[who.Index].SavePlayerLoop.Start(data);
                                }
                            }
                        }
                    }
                    int TileX = (int)(x / 16);
                    int TileY = (int)(y / 16);
                    if (TileX <= Structs.Values.SafeMin || TileX >= Structs.Values.SafeMax)
                    {
                        if (!Utils.IsPlayerUnSafe(who.Index))
                        {
                            Thread thread = new Thread(Utils.PlayerOutSafeCircleLoop);
                            thread.IsBackground = true;
                            thread.Start(who.Index);
                            Structs.Values.UnSafePlayerLoops.Add(who.Index, thread);
                        }
                    }
                    else
                    {
                        if (Utils.IsPlayerUnSafe(who.Index))
                        {
                            Structs.Values.UnSafePlayerLoops[who.Index].Abort();
                            Structs.Values.UnSafePlayerLoops.Remove(who.Index);
                            who.SendSuccessMessage("您已进入安全区");
                        }
                    }
                }

            }
            if (args.MsgID == PacketTypes.PlayerSpawn)
            {
                if (Structs.Values.Game != Structs.Status.playing)
                    return;
                args.Handled = Utils.IsEntered(args.Msg.whoAmI);
            }
            if (args.MsgID == PacketTypes.PlayerHurtV2)
            {
                args.Handled = Structs.Values.Game != Structs.Status.playing||!Utils.IsPlayer(args.Msg.whoAmI);
                foreach (var plr in Structs.Values.playing.Keys)
                {
                    var who = TShock.Players[plr];
                    for(int i = 0; i < 22; i++)
                    {
                        if(who.TPlayer.buffType[i]==Terraria.ID.BuffID.FishMinecartLeft)
                        {
                            args.Handled = true;
                        }
                    }
                }
                using (BinaryReader re = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    var who = re.ReadByte();
                    if (!Utils.IsPlayer(who))
                    {
                        args.Handled = true;
                    }
                    if (Utils.IsFalled(args.Msg.whoAmI))
                    {
                        args.Handled = true;
                    }
                }
            }
            if (args.MsgID == PacketTypes.PlayerHp)
            {
                if (Structs.Values.Game != Structs.Status.playing)
                {
                    return;
                }
                using(BinaryReader re=new BinaryReader (new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    int who = re.ReadByte();
                    bool died = re.ReadInt16() == 0;
                    if (died)
                    {
                        if (Utils.IsFalled(who))
                        {
                            Utils.PlayerKilled(who, who);
                        }
                        else
                        {
                            Utils.PlayerFalled(who, who);
                        }
                    }
                }
            }
            if(args.MsgID==PacketTypes.PlayerDeathV2)
            {
                if (Structs.Values.Game != Structs.Status.playing)
                    return;
                args.Handled = true;
                if (Structs.Values.Game == Structs.Status.playing)
                {
                    using (BinaryReader reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                    {
                        int killed = reader.ReadByte();
                        reader.ReadByte();
                        int killer = reader.ReadByte();
                        if (!Utils.IsPlayer(killer) || !Utils.IsPlayer(killed))
                        {
                            return;
                        }
                        killer = killer == 255 ? killed : killer;                        
                        if (Utils.IsFalled(killed))
                        {
                            Utils.PlayerKilled(killed, killer);
                        }
                        else
                        {
                            Utils.PlayerFalled(killed, killer);
                        }
                    }
                }
            }
            if(args.MsgID == PacketTypes.PlayerTeam)
            {
                using(BinaryReader reader=new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    int who = reader.ReadByte();
                    int team = reader.ReadByte();
                    if (Structs.Values.Game ==Structs.Status.playing&&Utils.IsPlayer(who))
                    {
                        args.Handled = true;
                        Utils.SetTeam(who, Structs.Values.Teams[who]);
                        TShock.Players[who].SendErrorMessage("游戏已开始，禁止切换队伍");
                    }
                }
            }
            if (Structs.Values.Game == Structs.Status.playing)
            {
                //如果场上剩余一个玩家，游戏结束
                if (Utils.GetPlayers().Count <= 1)
                {
                    Utils.GameOver();
                }
                //如果场上只有一个队伍，并且没有无队伍的玩家，游戏结束
                if (Utils.GetTeams().Count <= 1)
                {
                    bool over = true;
                    foreach (var i in Utils.GetPlayers())
                    {
                        if (Structs.Values.Teams[i] == Structs.Team.无)
                        {
                            over = false;
                        }
                    }
                    if (over)
                        Utils.GameOver();
                }
            }
        }
    }
}
