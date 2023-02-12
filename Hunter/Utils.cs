using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using Terraria.ID;

namespace Hunting
{
    static class Utils
    {
		public static void Jump()
        {
			foreach (var plr in Structs.Values.playing)
            {
				if(plr.Value)
                {
					TShock.Players[plr.Key].TPlayer.ClearBuff(BuffID.FishMinecartLeft);
                }
            }
        }
		/// <summary>
		/// 玩家加入方法
		/// </summary>
		public static void JoinLoop()
        {
			int i = 0;
			TShock.Utils.Broadcast($"游戏将在{Structs.Values.config.MaxJoinSec}s后开始，请未加入游戏的玩家" +
				$"尽快输入/join加入游戏，并请各位玩家确认自己的队伍，游戏开始后将不能更换队伍", Color.Yellow);
			for (int j = 0; j < Structs.Values.config.MaxJoinSec; j++)
			{
				Thread.Sleep(1000);
				i++;
                if (Structs.Values.config.MaxJoinSec - j <= 10)
                {
					TShock.Utils.Broadcast($"游戏倒计时:{Structs.Values.config.MaxJoinSec - j}", Color.Red);
                }
				else if (i == 10)
                {
					i = 0;
					TShock.Utils.Broadcast($"游戏将在{Structs.Values.config.MaxJoinSec-j}s后开始，请未加入游戏的玩家" +
						$"尽快输入/join加入游戏，并请各位玩家确认自己的队伍，游戏开始后将不能更换队伍", Color.Yellow);
				}
			}
			Utils.Initialize();
			TShock.Utils.Broadcast("猎杀时刻！", Color.Red);
			Structs.Values.Game = Structs.Status.playing;
			Structs.Values.JoinThread = null;
		}
		/// <summary>
		/// 玩家死亡
		/// </summary>
		/// <param name="killed">被击杀者</param>
		/// <param name="killer">击杀玩家</param>
		public static void PlayerKilled(int killed,int killer)
		{
            if (killed != killer)
			{
				if (IsFalled(killed))
					Structs.Values.Falled.Remove(killed);
				Structs.Values.Killed.Add(killed);
				Structs.Values.playing[killed] = false;
				Structs.Values.Grades[Utils.GetPlayerName(killer)]++;
				TShock.Utils.Broadcast($"{Utils.GetPlayerName(killed)}已被" +
					$"{Utils.GetPlayerName(killer)}击杀,{Utils.GetPlayerName(killer)}" +
					$"总分+1，{Utils.GetPlayerName(killer)}当前总分:{Structs.Values.Grades[Utils.GetPlayerName(killer)]}", Utils.GetTeamColor(
						Structs.Values.Teams[killer]));
				TShock.Players[killed].SendErrorMessage("您已出局，可输入/join 玩家名，传送至某一玩家进行观战");
			}
            else
			{
				if (IsFalled(killed))
					Structs.Values.Falled.Remove(killed);
				Structs.Values.Killed.Add(killed);
				Structs.Values.playing[killed] = false;
				TShock.Utils.Broadcast($"{Utils.GetPlayerName(killed)}已出局", Utils.GetTeamColor(Structs.Values.Teams[killed]));
				TShock.Players[killed].SendErrorMessage("您已出局，可输入/join 玩家名，传送至某一玩家进行观战");
			}
			CountGrades();
			var x = TShock.Players[killed].TileX;
			var y = TShock.Players[killed].TileY;
			TShock.Players[killed].Spawn(PlayerSpawnContext.ReviveFromDeath);
			var items = GetPlayerBank(killed);
			SetPlayerInv(killed, Structs.Values.PlayerInvs[killed]);
			foreach(var i in items)
            {
				DropItem(i, x, y);
            }
		}
		/// <summary>
		/// 玩家被击倒
		/// </summary>
		/// <param name="killed">被击倒的玩家</param>
		/// <param name="killer">击杀者</param>
		public static void PlayerFalled(int killed,int killer)
		{
			if(Structs.Values.Teams[killed]==Structs.Team.无||GetTeammates(killed).Count == 0)
            {
				PlayerKilled(killed, killer);
            }
            else
            {
				if (killed != killer)
				{
					Structs.Values.Falled.Add(killed);
					Structs.Values.Grades[Utils.GetPlayerName(killer)]++;
					TShock.Utils.Broadcast($"{Utils.GetPlayerName(killed)}已被" +
						$"{Utils.GetPlayerName(killer)}击倒,{Utils.GetPlayerName(killer)}" +
						$"总分+1，{Utils.GetPlayerName(killer)}当前总分:{Structs.Values.Grades[Utils.GetPlayerName(killer)]}", Utils.GetTeamColor(
							Structs.Values.Teams[killer]));
					foreach (var k in GetTeammates(killed))
					{
						TShock.Players[k].SendErrorMessage($"玩家{GetPlayerName(killed)}已倒地，" +
							$"可通过在其身边10格范围内停留10s将其扶起");
					}
				}
                else
				{
					Structs.Values.Falled.Add(killed);
					TShock.Utils.Broadcast($"{Utils.GetPlayerName(killed)}已倒地", Utils.GetTeamColor(
							Structs.Values.Teams[killed]));
					foreach (var k in GetTeammates(killed))
					{
						TShock.Players[k].SendErrorMessage($"玩家{GetPlayerName(killed)}已倒地，" +
							$"可通过在其身边10格范围内停留10s将其扶起");
					}
				}
				var position = Structs.Values.LastPosition[killed];
				TShock.Players[killed].Spawn(PlayerSpawnContext.ReviveFromDeath);
				TShock.Players[killed].Teleport(position.X, position.Y);
				ThreadPool.QueueUserWorkItem(PlayerFallLoop, killed);
            }
		}
		/// <summary>
		/// 获取同队玩家
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>所有同队玩家的id</returns>
		public static List<int> GetTeammates(int who)
        {
			var team = Structs.Values.Teams[who];
			List<int> Players = new List<int>();
			foreach(var k in Structs.Values.Teams)
            {
				if (k.Value == team && k.Key != who && IsPlayer(k.Key))
				{
					Players.Add(k.Key);
				}
            }
			return Players;
        }
		/// <summary>
		/// 是否可以救治玩家
		/// </summary>
		/// <param name="hurter">被救治的玩家id</param>
		/// <param name="saver">救治者</param>
		/// <returns>是否可以救治</returns>
		public static bool CanSave(int hurter,int saver)
        {
			var p1 = TShock.Players[hurter];
			var p2 = TShock.Players[saver];
			return p2.X <= p1.X + 16 * 10 
				&& p2.X >= p1.X - 16 * 10 
				&& p2.Y >= p1.Y - 16 * 10 
				&& p2.Y <= p1.Y + 16 * 10;
        }
		/// <summary>
		/// 玩家倒地执行方法
		/// </summary>
		/// <param name="o">玩家id</param>
		public static void PlayerFallLoop(object o)
        {
			int who = (int)o;
			var player = TShock.Players[who];
			//扶起玩家方法
			while (IsFalled(who)&&IsPlayer(who))
            {
				foreach(var b in Structs.Values.config.FalledBuffs)
                {
					player.SetBuff(b, 2);
                }
				Thread.Sleep(1000);
            }
		}
		public static void Save(Object o)
		{
            try
			{
				var data = (List<int>)o;
				int who = data[0];
				var player = TShock.Players[who];
				int safer = data[1];
				TShock.Players[safer].SendSuccessMessage($"正在救治玩家{player.Name},10s后可将其扶起");
				player.SendSuccessMessage($"您正在被{GetPlayerName(safer)}救治，10s后您将被扶起");
				Thread.Sleep(10000);
				Structs.Values.Falled.Remove(who);
				TShock.Utils.Broadcast($"{GetPlayerName(safer)}成功将{player.Name}救起，" +
					$"{GetPlayerName(safer)}总分+1,{GetPlayerName(safer)}总分：" +
					$"{Structs.Values.Grades[GetPlayerName(safer)]}",
					GetTeamColor(Structs.Values.Teams[safer]));
				Structs.Values.Grades[GetPlayerName(safer)]++;
				Structs.Values.SavePlayerLoop.Remove(safer);
			}
            catch(Exception ex)
            {
				Console.WriteLine("在调用方法‘save’时报错:" + ex);
            }
		}
		/// <summary>
		/// 玩家是否被击倒
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家是否被击倒</returns>
		public static bool IsFalled(int who)
        {
			return Structs.Values.Falled.FindAll((int i) => i == who).Count != 0;
        }
		/// <summary>
		/// 设置玩家队伍
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <param name="team">玩家队伍</param>
		public static void SetTeam(int who,Structs.Team team)
        {
			using(MemoryStream data=new MemoryStream())
            {
				using (BinaryWriter writer=new BinaryWriter(data))
                {
					writer.Write((short)5);
					writer.Write((byte)PacketTypes.PlayerTeam);
					writer.Write((byte)who);
					writer.Write((byte)team);
                }
				TShock.Players[who].SendRawData(data.ToArray());
            }
        }
		/// <summary>
		/// 玩家是否已死亡
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家是否已死亡</returns>
		public static bool IsKilled(int who)
        {
			return Structs.Values.Killed.FindAll((int i) => i == who).Count != 0;
        }
		/// <summary>
		/// 排序玩家总分
		/// </summary>
		/// <returns>玩家总分降序</returns>
		public static Dictionary<string,int> RankPlayers()
        {
			return Structs.Values.Grades.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
        }
		/// <summary>
		/// 排序所有参赛队伍
		/// </summary>
		/// <returns>所有参赛队伍总分降序字典</returns>
		public static Dictionary<Structs.Team, int> RankTeams()
        {
			var teams = GetTeams();
			var result = new Dictionary<Structs.Team, int>();
			foreach(var t in teams)
            {
				result.Add(t, 0);
				foreach(var p in InTeamRank(t))
                {
					result[t] += p.Value;
                }
            }
			return result.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
        }
		/// <summary>
		/// 队内排序
		/// </summary>
		/// <param name="team">队伍</param>
		/// <returns>队内玩家总分降序字典</returns>
		public static Dictionary<string,int> InTeamRank(Structs.Team team)
        {
			var players = new Dictionary<string, int>();
			foreach(var k in Structs.Values.Teams)
            {
                if (k.Value == team)
                {
					players.Add(GetPlayerName(k.Key), Structs.Values.Grades[GetPlayerName(k.Key)]);
                }
            }
			return players.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
        }
		/// <summary>
		/// 获取所有参赛队伍
		/// </summary>
		/// <returns>参赛队伍列表</returns>
		public static List<Structs.Team> GetTeams()
        {
			var teams = new List<Structs.Team>();
			foreach(var k in Structs.Values.Teams)
            {
				if (IsPlayer(k.Key) && k.Value != Structs.Team.无 && teams.FindAll((Structs.Team t) => t == k.Value).Count == 0)
				{
					teams.Add(k.Value);
				}
            }
			return teams;
        }
		/// <summary>
		/// 唱分
		/// </summary>
		public static void CountGrades()
        {
			var PlayerRank = RankPlayers();
			int rank = PlayerRank.Count;
			foreach(var k in RankPlayers())
            {
				var color = Color.White;
                try
                {
					color = GetTeamColor(Structs.Values.Teams[TSPlayer.FindByNameOrID(k.Key)[0].Index]);
				}
                catch { }
				TShock.Utils.Broadcast($"第{rank}名：{k.Key}，总分：{k.Value}",color);
				rank--;
            }
			TShock.Utils.Broadcast("以下为玩家总分排名", Color.Yellow);
			var TeamRank = RankTeams();
			rank = TeamRank.Count;
			foreach (var k in TeamRank)
            {
				var color = GetTeamColor(k.Key);
				var inTeamRank = InTeamRank(k.Key);
				var _rank = inTeamRank.Count;
				foreach(var _k in inTeamRank)
				{
					TShock.Utils.Broadcast($"{_rank}.{_k.Key}，总分：{_k.Value}",color);
					_rank--;
				}
				TShock.Utils.Broadcast($"第{rank}名：{k.Key}队，总分：{k.Value}",color);
				rank--;
            }
			foreach (var i in GetPlayers())
            {
				var color = Utils.GetTeamColor(Structs.Values.Teams[i]);
				TShock.Utils.Broadcast($"{GetPlayerName(i)}", color);
            }
			TShock.Utils.Broadcast("剩余玩家:", Color.Yellow);
        }
		/// <summary>
		/// 获取队伍颜色
		/// </summary>
		/// <param name="team">队伍</param>
		/// <returns>颜色</returns>
		public static Color GetTeamColor(Structs.Team team)
        {
            switch (team)
            {
				case Structs.Team.紫:
					return Color.Purple;
				case Structs.Team.红:
					return Color.Red;
				case Structs.Team.绿:
					return Color.Green;
				case Structs.Team.蓝:
					return Color.Blue;
				case Structs.Team.黄:
					return Color.Yellow;
            }
			return Color.White;
        }
		/// <summary>
		/// 游戏结束方法
		/// </summary>
		public static void GameOver()
		{
			TShock.Utils.Broadcast("游戏结束,正在清理掉落物", Color.Green);
			Commands.HandleCommand(TSPlayer.Server, "/clear item 99999");
			//结束轰炸区进程
			Structs.Values.DestroyCircleLoop.Abort();
			//结束空投进程
			Structs.Values.DropItemLoop.Abort();
			//结束安全区进程
			Structs.Values.SafeCircleLoop.Abort();
			TShock.Utils.Broadcast("游戏结束,正在返回大厅",Color.Green);
			//结束游戏
			Structs.Values.Game = Structs.Status.end;
			//将所有未死亡的玩家传送回大厅
			/*foreach(int i in GetPlayers())
            {
				if (IsKilled(i))
					continue;
				Teleport(i, Structs.Values.config.Hall);
            }*/
			//传送所有玩家
			foreach(var i in Structs.Values.VisitorInvs)
            {
                try
				{
					Teleport(i.Key, Structs.Values.config.Hall);
					SetPlayerInv(i.Key, i.Value);
				}
                catch { }
            }
			Structs.Values.VisitorInvs = new Dictionary<int, Structs.Item[]>();
			foreach(int i in GetAll())
				Teleport(i, Structs.Values.config.Hall);
			CountGrades();
			//重置玩家
			List<int> players = GetPlayers();
			foreach(int i in players)
            {
                try
				{
					Structs.Values.playing[i] = false;
					//重置玩家生命为原始生命
					PlayerHP(i, Structs.Values.PlayerHP[i], Structs.Values.PlayerHP[i]);
					//归还原始物品
					SetPlayerInv(i, Structs.Values.PlayerInvs[i]);
				}
                catch { }
            }
			Structs.Values.Killed = new List<int>();
			Structs.Values.Falled = new List<int>();
			Structs.Values.Teams = new Dictionary<int, Structs.Team>();
			Structs.Values.PlayerHP = new Dictionary<int, int>();
			Structs.Values.PlayerInvs = new Dictionary<int, Structs.Item[]>();
			//重置计分板
			//Structs.Values.Grades = new Dictionary<string, int>();
        }
		/// <summary>
		/// 传送玩家到指定地点
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <param name="p">地点</param>
		public static void Teleport(int who,Structs.Point p)
        {
			TShock.Players[who].Teleport(p.TileX * 16, p.TileY * 16);
		}
		/// <summary>
		/// 清除玩家buff
		/// </summary>
		/// <param name="player">玩家id</param>
		public static void ClearPlayerBuff(int player)
		{
			var who = TShock.Players[player];
			for (int i = 0; i < 22; i++)
			{
				who.TPlayer.DelBuff(i);
			}
		}
		/// <summary>
		/// 游戏初始化
		/// </summary>
		public static void Initialize()
		{
            try
			{
				TShock.Utils.Broadcast("正在初始化服务器", Color.Yellow);
				var random = Structs.Values.random;
				//清空世界箱子
				//并随机生成物品
				{
					int min = Structs.Values.config.MinItems;
					int max = Structs.Values.config.MaxItems;
					int i = 0;
					void ShowProgress(object o)
					{
						while (i < Main.maxChests)
						{
							TShock.Utils.Broadcast($"正在初始化箱子,进度:{(float)i * 100 / (float)Main.maxChests}%", Color.Yellow);
							Thread.Sleep(1000);
						}
						TShock.Utils.Broadcast("箱子初始化完成", Color.Yellow);
					}
					ThreadPool.QueueUserWorkItem(ShowProgress);
					for (i = 0; i < Main.maxChests; i++)
					{
						if (Main.chest[i] != null)
						{
							foreach (var item in Main.chest[i].item)
								item.netID = 0;
							int total = Structs.Values.random.Next(min, max);
							for (int j = 0; j < total; j++)
							{
								var item = Next();
								Main.chest[i].item[j].netID = item.netID;
								Main.chest[i].item[j].stack = item.stack;
								Main.chest[i].item[j].prefix = item.prefix;
							}

						}
					}
				}
				TShock.Utils.Broadcast("正在初始化玩家", Color.Yellow);
				//保存并清除所有游戏玩家背包
				//发放初始物品
				Structs.Values.Teams = new Dictionary<int, Structs.Team>();
				Structs.Values.PlayerInvs = new Dictionary<int, Structs.Item[]>();
				Structs.Values.VisitorInvs = new Dictionary<int, Structs.Item[]>();
				foreach (var i in GetPlayers())
				{
					//记录玩家队伍
					Structs.Values.Teams.Add(i, (Structs.Team)TShock.Players[i].Team);
					//记录玩家背包
					Structs.Values.PlayerInvs.Add(i, GetPlayerBank(i));
					//初始化玩家背包
					InvitatizePlayerInv(i);
				}
				Thread.Sleep(5000);
				TShock.Utils.Broadcast("正在初始化buff", Color.Yellow);
				foreach (var i in GetPlayers())
				{
					//清除玩家buff
					ClearPlayerBuff(i);
				}
				Thread.Sleep(5000);
				TShock.Utils.Broadcast("正在初始化生命", Color.Yellow);
				Structs.Values.PlayerHP = new Dictionary<int, int>();
				Structs.Values.Grades = new Dictionary<string, int>();
				foreach (var i in GetPlayers())
				{
					//记录玩家原始生命
					Structs.Values.PlayerHP.Add(i, TShock.Players[i].TPlayer.statLifeMax);
					//设置玩家生命为设定初始值
					PlayerHP(i, Structs.Values.config.StartLife, Structs.Values.config.StartLife);
					//开始计分
					Structs.Values.Grades.Add(GetPlayerName(i), 0);
				}
				Thread.Sleep(5000);
				TShock.Utils.Broadcast("初始化玩家完成,正在传送至轨道", Color.Yellow);
				foreach (var i in GetPlayers())
				{
					var player = TShock.Players[i];
					Teleport(i, Structs.Values.config.Start);
					player.SetBuff(Terraria.ID.BuffID.FishMinecartLeft);
					player.TPlayer.velocity.X = Structs.Values.random.Next(-300, -100);
					player.SendData(PacketTypes.PlayerUpdate);
				}
				TShock.Utils.Broadcast("游戏初始化完成，安全区开始缩小!", Color.Yellow);
				Structs.Values.SafeCircleLoop = new Thread(SafeCircleLoop);
				Structs.Values.SafeCircleLoop.IsBackground = true;
				Structs.Values.SafeCircleLoop.Start();
				Structs.Values.DropItemLoop = new Thread(DropItemLoop);
				Structs.Values.DropItemLoop.IsBackground = true;
				Structs.Values.DropItemLoop.Start();
				Structs.Values.DestroyCircleLoop = new Thread(DestroyCircleLoop);
				Structs.Values.DestroyCircleLoop.IsBackground = true;
				Structs.Values.DestroyCircleLoop.Start();
			}
			catch (Exception ex)
            {
				throw new Exception(ex.StackTrace);
            }
		}
		/// <summary>
		/// 设置玩家生命
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <param name="life">玩家当前生命</param>
		/// <param name="maxLife">玩家最大生命</param>
		public static void PlayerHP(int who,int life,int maxLife)
        {
			var player = TShock.Players[who];
			using (MemoryStream data=new MemoryStream())
            {
				using (BinaryWriter writer=new BinaryWriter(data))
                {
					writer.Write((short)8);
					writer.Write((byte)16);
					writer.Write((byte)who);
					writer.Write((short)life);
					writer.Write((short)maxLife);
				}
				player.SendRawData(data.ToArray());
            }
        }
		/// <summary>
		/// 轰炸区
		/// </summary>
		public static void DestroyCircleLoop()
		{
			int _break = Structs.Values.config.Circle.Break / 3;
			Thread.Sleep(Structs.Values.config.Circle.Break);
			while (true)
			{
				//轰炸间隔
				Thread.Sleep(Structs.Values.random.Next( _break,2*_break) * 1000);
				//轰炸位置
				int x = Structs.Values.random.Next(Structs.Values.SafeMin, Structs.Values.SafeMax);
				int size = Structs.Values.config.DestroySize;
				int tiley = Structs.Values.config.DestroyTileY;
				int min = x, max = x + size;
                if (x + size > Structs.Values.SafeMax)
                {
					max = Structs.Values.SafeMax;
					min = x - (x + size - Structs.Values.SafeMax);
                }
				Structs.Values.DestroyMax = max;
				Structs.Values.DestroyMin = min;
				Structs.Values.IsDestroy = true;
				TShock.Utils.Broadcast($"正在进行随机轰炸,范围：{GetTileXInfo(min)}-{GetTileXInfo(max)}", Color.Yellow);
				for(int i = 0; i < 30; i++)
                {
					Thread.Sleep(1000);
					NewProj(240, Structs.Values.random.Next(min, max), tiley, 0, 1);
                }
				Structs.Values.IsDestroy = false;
				TShock.Utils.Broadcast($"随机轰炸已结束", Color.Yellow);
			}
		}
		/// <summary>
		/// 生成弹幕
		/// </summary>
		/// <param name="id">弹幕id</param>
		/// <param name="tilex">坐标x</param>
		/// <param name="tiley">坐标y</param>
		/// <param name="vx">x方向加速度</param>
		/// <param name="vy">y方向加速度</param>
		public static void NewProj(int id,int tilex,int tiley,int vx,int vy)
        {
            try
			{
				Projectile.NewProjectile(null,tilex * 16, tiley * 16, vx * 16, vy * 16, id, 100, 10);
				//Projectile.NewProjectile(new Vector2(tilex * 16, tiley * 16), new Vector2(vx*16, vy*16), id, 100, 10);
			}
			catch(Exception ex)
            {
				Console.WriteLine(ex);
            }
        }
		/// <summary>
		/// 设置幽灵模式
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <param name="ghost">是否为幽灵模式</param>
		public static void SetGhost(int who,bool ghost)
        {
			var player = TShock.Players[who];
			for (int i = 0; i < 3; i++)
			{
				player.TPlayer.ghost = ghost;
				player.SendData(PacketTypes.PlayerUpdate);
			}
        }
		/// <summary>
		/// 获取玩家名
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家名</returns>
		public static string GetPlayerName(int who)
        {
			return TShock.Players[who].Name;
        }
		/// <summary>
		/// 生成掉落物
		/// </summary>
		/// <param name="item">物品id</param>
		/// <param name="tileX">X轴坐标</param>
		/// <param name="tileY">y轴坐标</param>
		public static void DropItem(Structs.Item item,int tileX,int tileY)
		{
			int number = Item.NewItem((int)tileX*16, (int)tileY*16, 0, 0, item.netID, item.stack, true, item.prefix, true, false);
			TSPlayer.All.SendData(PacketTypes.ItemDrop, "", number, 0f, 0f, 0f, 0);
		}
		/// <summary>
		/// 空投物品
		/// </summary>
		public static void DropItemLoop()
        {
			int _break = Structs.Values.config.Circle.Break / 3;
			Thread.Sleep(Structs.Values.config.Circle.Break);
            while (true)
			{
				//空投间隔
				Thread.Sleep(Structs.Values.random.Next(_break, 2 * _break) * 1000);
				//空投位置
				int x = Structs.Values.random.Next(Structs.Values.SafeMin, Structs.Values.SafeMax);
				TShock.Utils.Broadcast($"正在生成空投,坐标:{GetTileXInfo(x)}", Color.Yellow);
				var items = RandomItemDrop();
				foreach(var i in items)
                {
					DropItem(i, x, Structs.Values.config.DestroyTileY);
                }
			}
        }
		public static bool IsPlayerUnSafe(int who)
        {
			return Structs.Values.UnSafePlayerLoops.ToList().FindAll((KeyValuePair<int, Thread> k)
				=> k.Key == who).Count != 0;
        }
		public static void PlayerOutSafeCircleLoop(Object o)
        {
			var who = TShock.Players[(int)o];
			int alarm = 0;
			who.SendErrorMessage($"您已离开安全区范围,安全区范围:{GetTileXInfo(Structs.Values.SafeMin)}" +
				$"-{GetTileXInfo(Structs.Values.SafeMax)}");
            while (IsPlayer(who.Index))//(Structs.Values.LastPosition[who.Index].X <= Structs.Values.SafeMin || Structs.Values.LastPosition[who.Index].Y >= Structs.Values.SafeMax)
            {
				foreach (var i in Structs.Values.config.UnSafeBuffs)
                {
					who.SetBuff(i, 2);
                }
				Thread.Sleep(1000);
				alarm++;
				if(alarm == 10)
                {
					alarm = 0;
					who.SendErrorMessage($"您已离开安全区范围,安全区范围:{GetTileXInfo(Structs.Values.SafeMin)}" +
						$"-{GetTileXInfo(Structs.Values.SafeMax)}");
				}
            }
        }
		/// <summary>
		/// 生成随机空投物品
		/// </summary>
		/// <returns>随机空投物品</returns>
		public static List<Structs.Item> RandomItemDrop()
        {
			int max = Structs.Values.config.MaxDrop;
			Structs.Item item = null;
			int rate=100;
			for(int i = 0; i < max; i++)
            {
				var ite = Next_air(Structs.Values.random);
				if (GetRate_air(ite.netID) < rate)
                {
					rate = GetRate_air(ite.netID);
					item = ite;
                }
            }
			var items = new List<Structs.Item>();
			items.Add(item);
			int num = Structs.Values.random.Next(0, max);
			for(int i = 0; i < num; i++)
            {
				items.Add(Next_air(Structs.Values.random));
            }
			return items;
        }
		/// <summary>
		/// 安全区缩小方法
		/// </summary>
		public static void SafeCircleLoop()
        {
			var circle = Structs.Values.config.Circle;
			TShock.Utils.Broadcast($"安全区将在{circle.Break}秒后缩小至{circle.Smaller}%!",Color.Red);
			Structs.Values.SafeMax = Main.maxTilesX;
			Structs.Values.SafeMin = 0;
			Thread.Sleep(circle.Break * 1000);
			int sec = circle.Break;
			for(int i = 0; i < circle.Num; i++)
			{
				//随机缩圈算法v1.0
				sec *= circle.Smaller;
				sec /= 100;
				//计算缩圈时间
				int c = Structs.Values.random.Next(Structs.Values.SafeMin, Structs.Values.SafeMax);
				//随机在安全区内生成一个坐标
				int size= Structs.Values.SafeMax- Structs.Values.SafeMin;
				size *= circle.Smaller;
				size /= 100;
				//计算缩圈后的大小
				int max = Math.Min(Structs.Values.SafeMax, size + c);
				int min = c;
				if (size + c > Structs.Values.SafeMax)
				{
					min = c - (size + c - Structs.Values.SafeMax);
				}
				int resize = (Structs.Values.SafeMax - Structs.Values.SafeMin) - size;
				//计算缩圈后的min，max
				int each = resize / sec;
				TShock.Utils.Broadcast($"安全区开始缩小,范围为{GetTileXInfo(min)}-" +
					$"{GetTileXInfo(max)},缩圈时间:{sec}s",Color.Red);
				int alarm = 0;
				bool SmallerMax = true;
				for(int j = 0; j < sec; j++)
                {
					SmallerMax = !SmallerMax;
					alarm++;
                    if (alarm == 30)
                    {
						alarm = 0;
						TShock.Utils.Broadcast($"剩余缩圈时间:{sec - j},当前安全区范围{GetTileXInfo(Structs.Values.SafeMin)}" +
							$"-{GetTileXInfo(Structs.Values.SafeMax)}", Color.Red);

					}
                    if (SmallerMax&&Structs.Values.SafeMax >max)
                    {
						Structs.Values.SafeMax -= each;
                    }
                    else if(Structs.Values.SafeMin<min)
                    {
						Structs.Values.SafeMin -= each;
                    }
					Thread.Sleep(1000);
				}
				Structs.Values.SafeMax = max;
				Structs.Values.SafeMin = min;
				TShock.Utils.Broadcast($"安全区已停止缩小{circle.Break}s后开始第{i}/{circle.Num}缩圈", Color.Red);
				Thread.Sleep(circle.Break * 1000);
            }
			{
				sec *= circle.Smaller;
				sec /= 100;
				TShock.Utils.Broadcast("安全区开始缩小，范围为:全图,缩圈时间:" + sec + $"当前安全区范围为{GetTileXInfo(Structs.Values.SafeMin)}-{GetTileXInfo(Structs.Values.SafeMax)}", Color.Red);
				int size = Structs.Values.SafeMax - Structs.Values.SafeMin;
				int each = size / sec;
				int alarm = 0;
				bool SmallerMax = true;
				int c = Structs.Values.random.Next(Structs.Values.SafeMin, Structs.Values.SafeMax);
				for (int j = 0; j < sec; j++)
				{
					SmallerMax = !SmallerMax;
					alarm++;
					if (alarm == 30)
					{
						alarm = 0;
						TShock.Utils.Broadcast($"剩余缩圈时间:{sec - j},当前安全区范围{GetTileXInfo(Structs.Values.SafeMin)}" +
							$"-{GetTileXInfo(Structs.Values.SafeMax)}", Color.Red);
					}
			 		if (SmallerMax && Structs.Values.SafeMax > c)
					{
						Structs.Values.SafeMax -= each;
					}
					else if (Structs.Values.SafeMin < c)
					{
						Structs.Values.SafeMin -= each;
					}
					Thread.Sleep(1000);
				}
				Structs.Values.SafeMax = 0;
				Structs.Values.SafeMin = 0;
			}
			GameOver();
		}
		public static string GetTileXInfo(int x)
		{
			int c = Main.maxTilesX / 2;
			if (x > c)
			{
				return x - c + "以东";
			}
			else if (c > x)
			{
				return c - x + "以西";
			}
			else
				return "中心";
		}
		/// <summary>
		/// 删除随机生成物品
		/// </summary>
		/// <param name="id">物品id</param>
		public static void DelRan(int id)
		{
			var ran = Structs.Values.config.RandomItems.ToList().Find((Structs.RandomItem rand
				  ) => rand.Item.netID == id);
			var rans = Structs.Values.config.RandomItems.ToList();
			rans.Remove(ran);
			Structs.Values.config.RandomItems = rans.ToArray();

		}
		/// <summary>
		/// 删除空投随机生成物品
		/// </summary>
		/// <param name="id">物品id</param>
		public static void DelRan_air(int id)
		{
			var ran = Structs.Values.config.Airdrops.ToList().Find((Structs.RandomItem rand
				  ) => rand.Item.netID == id);
			var rans = Structs.Values.config.Airdrops.ToList();
			rans.Remove(ran);
			Structs.Values.config.Airdrops = rans.ToArray();

		}
		/// <summary>
		/// 该随机生成物品是否存在
		/// </summary>
		/// <param name="id">物品id</param>
		/// <returns></returns>
		public static bool HasRan(int id)
		{
			return Structs.Values.config.RandomItems.ToList().FindAll((
				Structs.RandomItem ran) => ran.Item.netID == id).Count != 0;
		}
		/// <summary>
		/// 该空投随机生成物品是否存在
		/// </summary>
		/// <param name="id">物品id</param>
		/// <returns></returns>
		public static bool HasRan_air(int id)
		{
			return Structs.Values.config.Airdrops.ToList().FindAll((
				Structs.RandomItem ran) => ran.Item.netID == id).Count != 0;
		}
		/// <summary>
		/// 增加随机生成物品
		/// </summary>
		/// <param name="ran">随机因子</param>
		public static void AddRan(Structs.RandomItem ran)
		{
			var rans = Structs.Values.config.RandomItems.ToList();
			rans.Add(ran);
			Structs.Values.config.RandomItems = rans.ToArray();
		}
		/// <summary>
		/// 增加空投随机生成物品
		/// </summary>
		/// <param name="ran">随机因子</param>
		public static void AddRan_air(Structs.RandomItem ran)
		{
			var rans = Structs.Values.config.Airdrops.ToList();
			rans.Add(ran);
			Structs.Values.config.Airdrops = rans.ToArray();
		}
		/// <summary>
		/// 删除初始物品
		/// </summary>
		/// <param name="id">物品id</param>
		public static void DelItem(int id)
		{
			var items = Structs.Values.config.StartInv.ToList();
			var item = Structs.Values.config.StartInv.ToList().Find((Structs.Item i) => i.netID == id);
			items.Remove(item);
			Structs.Values.config.StartInv = items.ToArray();
		}
		/// <summary>
		/// Config.StartInv中是否存在物品
		/// </summary>
		/// <param name="id">物品id</param>
		/// <returns>存在:true,不存在：false</returns>
		public static bool HasItem(int id)
        {
			return Structs.Values.config.StartInv.ToList().FindAll((Structs.Item i) => i.netID == id).Count != 0;
		}
		/// <summary>
		/// 增加初始物品
		/// </summary>
		/// <param name="item">物品</param>
		public static void AddItem(Structs.Item item)
        {
			var items = Structs.Values.config.StartInv.ToList();
			items.Add(item);
			Structs.Values.config.StartInv = items.ToArray();
        }
		/// <summary>
		/// 获取玩家当前坐标
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家坐标</returns>
		public static Structs.Point GetPoint(int who)
        {
			return new Structs.Point() { TileY = TShock.Players[who].TileY, TileX = TShock.Players[who].TileX };
        }
		/// <summary>
		/// 玩家是否已进入地图
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家是否已进入地图</returns>
		public static bool IsEntered(int who)
        {
			return GetAll().FindAll((int i) => i == who).Count != 0;
        }
		/// <summary>
		/// 获取所有已进入地图的玩家id
		/// </summary>
		/// <returns>所有已进入地图的玩家id</returns>
		public static List<int> GetAll()
        {
			List<int> result = new List<int>();
			foreach (var k in Structs.Values.playing)
				result.Add(k.Key);
			return result;
        }
		/// <summary>
		/// 判断玩家是否已加入游戏
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <returns>玩家是否已加入游戏</returns>
		public static bool IsPlayer(int who)
        {
			return GetPlayers().FindAll((int i) => i == who).Count != 0 && Structs.Values.playing[who];
        }
		public static List<Structs.Team> GetPlayingTeams()
        {
			var teams = new List<Structs.Team>();
			foreach (var i in GetPlayers())
            {
				var team = Structs.Values.Teams[i];
				if(team!=Structs.Team.无 &&teams.FindAll((Structs.Team t) => t == team).Count == 0)
                {
					teams.Add(team);
                }
            }
			return teams;
        }
		/// <summary>
		/// 获取所有已加入的玩家id
		/// </summary>
		/// <returns>所有已加入的玩家id</returns>
		public static List<int> GetPlayers()
        {
			List<int> result = new List<int>();
			foreach(var k in Structs.Values.playing)
            {
				if (k.Value)
					result.Add(k.Key);
            }
			return result;
		}
		/// <summary>
		/// 获取物品掉落概率
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static int GetRate(Structs.Item item)
		{
			return Structs.Values.config.RandomItems.ToList().Find((Structs.RandomItem ite) =>
			ite.Item == item).Rate;
		}
		/// <summary>
		/// 获取空投物品掉落概率
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static int GetRate_air(int netID)
		{
			return Structs.Values.config.Airdrops.ToList().Find((Structs.RandomItem ite) =>
			ite.Item.netID == netID).Rate;
		}
		/// <summary>
		/// 下一个随机物品
		/// </summary>
		/// <param name="random">随机类</param>
		/// <returns>物品</returns>
		public static Structs.Item Next(Random random = null)
		{
			Structs.RandomItem[] items = Config.GetConfig().RandomItems;
			random = random == null ? Structs.Values.random : random;
			int total = 0;
			foreach (var i in items)
			{
				total += i.Rate;
			}
			int result = random.Next(0, total);
			int min, max = 0;
			foreach (var i in items)
			{
				max += i.Rate;
				min = max - i.Rate;
				if (result >= min && result <= max)
				{

					var item = i.Item;
					item.stack = random.Next(1, item.stack);
					//if (item.stack != 1)
					//	Console.WriteLine(i.Item.netID + ":" + item.stack + "/" + i.Item.stack);
					return item;
				}
			}
			return null;
		}
		/// <summary>
		/// 下一个随机空投物品
		/// </summary>
		/// <param name="random">随机类</param>
		/// <returns>物品</returns>
		public static Structs.Item Next_air(Random random = null)
		{
			Structs.RandomItem[] items = Config.GetConfig().Airdrops;
			random = random == null ? new Random() : random;
			int total = 0;
			foreach (var i in items)
			{
				total += i.Rate;
			}
			int result = random.Next(0, total+1);
			int min, max = 0;
			foreach (var i in items)
			{
				max += i.Rate;
				min = max - i.Rate;
				if (result >= min && result <= max)
				{
					var item = i.Item;
					item.stack = random.Next(1, item.stack+1);
					return item;
				}
            }
			{
				var index = random.Next(0, items.Length);
				var item = items[index].Item;
				item.stack = random.Next(1, item.stack + 1);
				return item;
			}
		}
		/// <summary>
		/// 复活玩家
		/// </summary>
		/// <param name="who">玩家id</param>
		/// <param name="TileX">坐标X</param>
		/// <param name="TileY">坐标Y</param>
		public static void SpwanPlayer(int who,int TileX,int TileY)
        {
            TShock.Players[who].Spawn(Terraria.PlayerSpawnContext.ReviveFromDeath);
            TShock.Players[who].Teleport(TileX * 16, TileY * 16);
        }

		/// <summary>
		/// 获取玩家背包item[]
		/// </summary>
		/// <param name="index">玩家id</param>
		/// <returns></returns>
		public static Structs.Item[] GetPlayerBank(int index)
		{
			var player = TShock.Players[index];
			List<Structs.Item> items = Structs.Item.Parse(player.TPlayer.inventory).ToList();
			items.AddRange(Structs.Item.Parse(player.TPlayer.armor));
			items.AddRange(Structs.Item.Parse(player.TPlayer.dye));
			items.AddRange(Structs.Item.Parse(player.TPlayer.miscEquips));
			items.AddRange(Structs.Item.Parse(player.TPlayer.miscDyes));
			items.Add(Structs.Item.Parse(player.TPlayer.trashItem));
			return items.ToArray();
		}
		/// <summary>
		/// 清空玩家背包
		/// </summary>
		/// <param name="who"></param>
		public static void InvitatizePlayerInv(int who)
        {
			var item = new Structs.Item() { netID = 0, stack = 0, prefix = 0 };
			var player = TShock.Players[who];
			int lenth = player.TPlayer.inventory.Length + player.TPlayer.armor.Length
				+ player.TPlayer.dye.Length + player.TPlayer.miscEquips.Length +
				player.TPlayer.miscDyes.Length+1;
			var items = Structs.Values.config.StartInv;
			SetPlayerInv(who, items);
			for (int i = items.Length; i < lenth; i++)
				SetPlayerInvSlot(who, i, item);
		}
		/// <summary>
		/// 设置玩家背包
		/// </summary>
		/// <param name="player">玩家id</param>
		/// <param name="index">背包格子id</param>
		/// <param name="item">物品</param>
		public static void SetPlayerInvSlot(int player, int index, Structs.Item item)
		{
			using (MemoryStream data=new MemoryStream())
            {
				using (BinaryWriter wr=new BinaryWriter(data))
                {
					wr.Write((short)11);
					wr.Write((byte)5);
					wr.Write((byte)player);
					wr.Write((short)index);
					wr.Write((short)item.stack);
					wr.Write((byte)item.prefix);
					wr.Write((short)item.netID);
                }
				TShock.Players[player].SendRawData(data.ToArray());
            }
			/*var user = TShock.Players[player];
			//Terraria.Item item = new Terraria.Item() { netID = ite.netID, stack = ite.stack, prefix = ite.prefix };
			if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + user.TPlayer.bank3.item.Length + 1))
			{
				user.TPlayer.bank4.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + user.TPlayer.bank3.item.Length + 1) - 1].netID = item.netID;
				user.TPlayer.bank4.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + user.TPlayer.bank3.item.Length + 1) - 1].stack = item.stack;
				user.TPlayer.bank4.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + user.TPlayer.bank3.item.Length + 1) - 1].prefix = item.prefix;
			}
			else
			{
				if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + 1))
				{
					user.TPlayer.bank3.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + 1) - 1].netID = item.netID;
					user.TPlayer.bank3.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + 1) - 1].stack = item.stack;
					user.TPlayer.bank3.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length + 1) - 1].prefix = item.prefix;
				}
				else
				{
					if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length + user.TPlayer.bank2.item.Length))
					{
						user.TPlayer.trashItem.netID = item.netID;
						user.TPlayer.trashItem.stack = item.stack;
						user.TPlayer.trashItem.prefix = item.prefix;
					}
					else
					{
						if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length))
						{
							user.TPlayer.bank2.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length) - 1].stack = item.stack;
							user.TPlayer.bank2.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length) - 1].netID = item.netID;
							user.TPlayer.bank2.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length + user.TPlayer.bank.item.Length) - 1].prefix = item.prefix;
						}
						else
						{
							if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length))
							{
								user.TPlayer.bank.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length) - 1].netID = item.netID;
								user.TPlayer.bank.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length) - 1].prefix = item.prefix;
								user.TPlayer.bank.item[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length + user.TPlayer.miscDyes.Length) - 1].stack = item.stack;
							}
							else
							{
								if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length))
								{
									user.TPlayer.miscDyes[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length) - 1].netID = item.netID;
									user.TPlayer.miscDyes[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length) - 1].stack = item.stack;
									user.TPlayer.miscDyes[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length + user.TPlayer.miscEquips.Length) - 1].prefix = item.prefix;
								}
								else
								{
									if (index > (float)(58 + user.TPlayer.armor.Length + user.TPlayer.dye.Length))
									{
										user.TPlayer.miscEquips[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length) - 1].netID = item.netID;
										user.TPlayer.miscEquips[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length) - 1].prefix = item.prefix;
										user.TPlayer.miscEquips[(int)index - 58 - (user.TPlayer.armor.Length + user.TPlayer.dye.Length) - 1].stack = item.stack;
									}
									else
									{
										if (index > (float)(58 + user.TPlayer.armor.Length))
										{
											user.TPlayer.dye[(int)index - 58 - user.TPlayer.armor.Length - 1].stack = item.stack;
											user.TPlayer.dye[(int)index - 58 - user.TPlayer.armor.Length - 1].netID = item.netID;
											user.TPlayer.dye[(int)index - 58 - user.TPlayer.armor.Length - 1].prefix = item.prefix;
										}
										else
										{
											if (index > 58f)
											{
												user.TPlayer.armor[(int)index - 58 - 1].stack = item.stack;
												user.TPlayer.armor[(int)index - 58 - 1].prefix = item.prefix;
												user.TPlayer.armor[(int)index - 58 - 1].netID = item.netID;
											}
											else
											{
												user.TPlayer.inventory[(int)index].netID = item.netID;
												user.TPlayer.inventory[(int)index].prefix = item.prefix;
												user.TPlayer.inventory[(int)index].stack = item.stack;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			user.SendData((PacketTypes)5, "", player, index, item.prefix);*/
		}
		/// <summary>
		/// 使用item数组数据覆盖玩家背包
		/// </summary>
		/// <param name="player">玩家id</param>
		/// <param name="items">背包数据</param>
		public static void SetPlayerInv(int player, Structs.Item[] items)
		{
			for (int i = 0; i < items.Length; i++)
			{
				//NetMessage.SendData((int)PacketTypes.PlayerSlot, player, -1, Lang.GetItemName(items[i].netID).ToNetworkText(), player, 0, items[i].prefix);
				SetPlayerInvSlot(player, i, items[i]);
				//TShock.Players[player].GiveItem(items[i].netID, items[i].stack, items[i].prefix);
			}
		}

	}
}
