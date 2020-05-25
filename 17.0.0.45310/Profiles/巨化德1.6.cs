﻿using System;
using System.Collections.Generic;
using System.Linq;
using SmartBot.Database;
using SmartBot.Plugins.API;
using SmartBotAPI.Plugins.API;

/* Explanation on profiles :
 * 
 * 配置文件中定义的所有值都是百分比修饰符，这意味着它将影响基本配置文件的默认值。
 * 
 * 修饰符值可以在[-10000;范围内设置。 10000]（负修饰符有相反的效果）
 * 您可以为非全局修改器指定目标，这些目标特定修改器将添加到卡的全局修改器+修改器之上（无目标）
 * 
 * 应用的总修改器=全局修改器+无目标修改器+目标特定修改器
 * 
 * GlobalDrawModifier --->修改器应用于卡片绘制值
 * GlobalWeaponsAttackModifier --->修改器适用于武器攻击的价值，它越高，人工智能攻击武器的可能性就越小
 * 
 * GlobalCastSpellsModifier --->修改器适用于所有法术，无论它们是什么。修饰符越高，AI玩法术的可能性就越小
 * GlobalCastMinionsModifier --->修改器适用于所有仆从，无论它们是什么。修饰符越高，AI玩仆从的可能性就越小
 * 
 * GlobalAggroModifier --->修改器适用于敌人的健康值，越高越好，人工智能就越激进
 * GlobalDefenseModifier --->修饰符应用于友方的健康值，越高，hp保守的将是AI
 * 
 * CastSpellsModifiers --->你可以为每个法术设置个别修饰符，修饰符越高，AI玩法术的可能性越小
 * CastMinionsModifiers --->你可以为每个小兵设置单独的修饰符，修饰符越高，AI玩仆从的可能性越小
 * CastHeroPowerModifier --->修饰符应用于heropower，修饰符越高，AI玩它的可能性就越小
 * 
 * WeaponsAttackModifiers --->适用于武器攻击的修饰符，修饰符越高，AI攻击它的可能性越小
 * 
 * OnBoardFriendlyMinionsValuesModifiers --->修改器适用于船上友好的奴才。修饰语越高，AI就越保守。
 * OnBoardBoardEnemyMinionsModifiers --->修改器适用于船上的敌人。修饰符越高，AI就越会将其视为优先目标。
 *
 */

namespace SmartBotProfiles
{
    [Serializable]
    public class WildSecretMage : Profile
    {
        //幸运币
        private const Card.Cards TheCoin = Card.Cards.GAME_005;

        //猎人
        private const Card.Cards SteadyShot = Card.Cards.DS1h_292;
        //德鲁伊
        private const Card.Cards Shapeshift = Card.Cards.CS2_017;
        //术士
        private const Card.Cards LifeTap = Card.Cards.CS2_056;
        //法师
        private const Card.Cards Fireblast = Card.Cards.CS2_034;
        //圣骑士
        private const Card.Cards Reinforce = Card.Cards.CS2_101;
        //战士
        private const Card.Cards ArmorUp = Card.Cards.CS2_102;
        //牧师
        private const Card.Cards LesserHeal = Card.Cards.CS1h_001;
        //潜行者
        private const Card.Cards DaggerMastery = Card.Cards.CS2_083b;
		private Board _board;
        private bool _coin;

        //英雄能力优先级
        private readonly Dictionary<Card.Cards, int> _heroPowersPriorityTable = new Dictionary<Card.Cards, int>
        {
            {SteadyShot, 8},
            {LifeTap, 7},
            {DaggerMastery, 6},
            {Reinforce, 5},
            {Shapeshift, 4},
            {Fireblast, 3},
            {ArmorUp, 2},
            {LesserHeal, 1}
        };
		
		
		
        //直伤法术伤害表
        private static readonly Dictionary<Card.Cards, int> _spellDamagesTable = new Dictionary<Card.Cards, int>
        {
            {Card.Cards.LOE_002, 3},
            {Card.Cards.LOE_002t, 6},
            {Card.Cards.CS2_029, 6},
        };

        //具体策略
        public ProfileParameters GetParameters(Board board)
        {
            var p = new ProfileParameters(BaseProfile.Rush);
            p.DiscoverSimulationValueThresholdPercent = 10;
            //自己少于15血，控场
            if (board.HeroFriend.CurrentHealth <= 18)
            {
                p = new ProfileParameters(BaseProfile.Default);
                p.DiscoverSimulationValueThresholdPercent = 10;
            }
            //5费以下提高大铡蟹优先级
            if (board.MinionEnemy.Count >= 5)
            {
                p.CastMinionsModifiers.AddOrUpdate(Card.Cards.BOT_700, new Modifier(-50));//大铡蟹优先级增加
            }
            //当坟场有飞翼守护者，提高巫术时刻优先级
            if (board.FriendGraveyard.Contains(Card.Cards.YOD_003)//飞翼守护者
                   && board.HasCardInHand(Card.Cards.GIL_571)//巫术时刻
                  )
            {
                p.CastSpellsModifiers.AddOrUpdate(Card.Cards.GIL_571, new Modifier(-900));//提高巫术时刻优先级
            }
            //当费用等于9不使用过度生长
            if (board.ManaAvailable ==9
                  )
            {
                p.CastSpellsModifiers.AddOrUpdate(Card.Cards.BT_130, new Modifier(9999));//降低过度生长优先级
            }
            //当费用等于9不使用过度生长
            if (board.ManaAvailable >=7
                  )
            {
                p.CastSpellsModifiers.AddOrUpdate(Card.Cards.BT_132, new Modifier(-99));//提高过度生长优先级
            }
            //当坟场有女巫森林灰熊，提高巫术时刻优先级
            if (board.FriendGraveyard.Contains(Card.Cards.GIL_623)//飞翼守护者
                       && board.HasCardInHand(Card.Cards.GIL_571)//巫术时刻
                      )
            {
                p.CastSpellsModifiers.AddOrUpdate(Card.Cards.GIL_571, new Modifier(-900));//提高巫术时刻优先级
            }
            //当手上有森然巨化时，提高大法师优先级
            if (board.HasCardInHand(Card.Cards.DRG_315)//森然巨化
               && (board.HasCardInHand(Card.Cards.DAL_558)//大法师瓦格斯
                      ))
            {
                p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DAL_558, new Modifier(-200));//提高大法师瓦格斯优先级
            }
           
            //当坟场有飞翼守护者，提高巫术时刻优先级
            if (board.FriendGraveyard.Contains(Card.Cards.YOD_003)//飞翼守护者
                       && board.HasCardInHand(Card.Cards.GIL_571)//巫术时刻
                       && (board.HasCardInHand(Card.Cards.DAL_558)//大法师瓦格斯
                      ))

            {
                p.CastSpellsModifiers.AddOrUpdate(Card.Cards.GIL_571, new Modifier(-900));//提高巫术时刻优先级
                p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DAL_558, new Modifier(-200));//提高大法师瓦格斯优先级
            }

            //对面是牧师，信天翁随便下
            if (board.MinionEnemy.Any(minion => minion.Template.Id == Card.Cards.DAL_039))//无面渗透者 
            {
                p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_071t, new Modifier(-9999));//信天翁
            }
            //对面无面渗透者，火车王随便下DRG_318
            if (board.EnemyClass == Card.CClass.PRIEST)
            {
                p.CastMinionsModifiers.AddOrUpdate(Card.Cards.EX1_116, new Modifier(-9999));//火车王里诺艾
            }
            //我方
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_081, new Modifier(100));//瑞麟骑士
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.BT_155, new Modifier(-65));//废料场巨像
            p.CastSpellsModifiers.AddOrUpdate(Card.Cards.DRG_051, new Modifier(275));//任务
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.NEW1_023, new Modifier(65));//精灵龙
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.YOD_003, new Modifier(-80));//飞翼守护者
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_313, new Modifier(-70));//翡翠龙探险者
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_054, new Modifier(-40));//雏龙巨婴
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_320, new Modifier(-75));//觉醒巨龙伊瑟拉 
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.EX1_572, new Modifier(-20));//伊瑟拉
            p.CastSpellsModifiers.AddOrUpdate(Card.Cards.GIL_571, new Modifier(500));//巫术时刻
            p.CastSpellsModifiers.AddOrUpdate(Card.Cards.BT_130, new Modifier(-70));//过度生长
            p.CastSpellsModifiers.AddOrUpdate(Card.Cards.ULD_273, new Modifier(-20));//溢流
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.ULD_138, new Modifier(80));//阿努比萨斯防御者
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.GIL_623, new Modifier(-10));//女巫森林灰熊 
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_089, new Modifier(100));//红龙女王阿莱克丝塔萨
            p.CastMinionsModifiers.AddOrUpdate(Card.Cards.DRG_071, new Modifier(-50));//厄运信天翁
            p.CastSpellsModifiers.AddOrUpdate(Card.Cards.DRG_318, new Modifier(-20));//梦境吐息
            //敌方
            //螃蟹应该解
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BOT_700, new Modifier(200));
            //炸弹牛仔应该解
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.YOD_024, new Modifier(200));
            //提高相位威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_252, new Modifier(200));
            //提高热气球威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_057, new Modifier(200));
            //提高空中私掠者威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.YOD_016, new Modifier(200));
            //提高搬水工威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.ULD_270, new Modifier(200));
            //提高凶恶的废钢猎犬威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DAL_759, new Modifier(200));
            //提高神殿狂战士威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.ULD_185, new Modifier(200));
            //大法师瓦格斯
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DAL_558, new Modifier(200));
            //提高厄运信天翁
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_071, new Modifier(200));
            //提高高级跟班厄尔克
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.YOD_035, new Modifier(200));
            //提高狂奔的魔刃豹
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.YOD_006, new Modifier(200));
            //提高卡雷苟斯
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DAL_609, new Modifier(200));
            //提高希亚玛特
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.ULD_178, new Modifier(200));
            //提高齐恩瓦拉
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_104, new Modifier(200));
            //提高机械袋鼠
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BOT_445, new Modifier(200));
            //提高机械袋鼠宝宝
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BOT_445t, new Modifier(300));
            //提高雏龙
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.EX1_116t, new Modifier(200));
            //提高铸甲师
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.EX1_402, new Modifier(200));
            //提高无面渗透者威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DAL_039, new Modifier(200));
            //提高鲨鱼之灵威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.TRL_092, new Modifier(200));
            //提高灭龙弩炮威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_256, new Modifier(200));
            //提高飞刺威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DRG_037, new Modifier(200));
            //提高奇利亚斯威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BOT_548, new Modifier(200));
            //提高流放者奥图里斯威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BT_937, new Modifier(200));
            //提高暗影珠宝师汉纳尔威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BT_188, new Modifier(200));
            //提高萨特监工威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BT_352, new Modifier(200));
            //提高怒鳞纳迦威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.BT_355, new Modifier(200));
            //提高特殊坐骑商人威胁值
            p.OnBoardBoardEnemyMinionsModifiers.AddOrUpdate(Card.Cards.DAL_774, new Modifier(200));



            //我方送
            //厄运信天翁
            p.OnBoardFriendlyMinionsValuesModifiers.AddOrUpdate(Card.Cards.DRG_071, new Modifier(10));


            p.ChoicesModifiers.AddOrUpdate(Cards.OasisSurger, new Modifier(6));//水人抉择
			        

		
			
            return p;
        }
		
		//防止法术被使用
		private void PreventSpellFromBeingPlayedOnMinions(ref ProfileParameters parameters, Board board)
		{
			//遍历场上不是嘲讽的敌方随从
			foreach (var card in board.MinionEnemy.FindAll(x => !x.IsTaunt))
			{
                //修改火球的修正值为“Rush”配置文件中定义的基值的500％
                parameters.CastSpellsModifiers.AddOrUpdate(Card.Cards.CS2_029, new Modifier(1000, card.Id));

                //修改炽热的火把的修正值为“Rush”配置文件中定义的基值的500％
                parameters.CastSpellsModifiers.AddOrUpdate(Card.Cards.LOE_002t, new Modifier(500, card.Id));				
			}
		}

        //芬利·莫格顿爵士技能选择
        public Card.Cards SirFinleyChoice(List<Card.Cards> choices)
        {
            var filteredTable = _heroPowersPriorityTable.Where(x => choices.Contains(x.Key)).ToList();
            return filteredTable.First(x => x.Value == filteredTable.Max(y => y.Value)).Key;
        }
		


        //卡扎库斯选择
        public Card.Cards KazakusChoice(List<Card.Cards> choices)
        {
            return choices[0];
        }
		
		//甲板工具类
        public static class BoardHelper
		{
			//得到敌方的血量和护甲之和
            public static int GetEnemyHealthAndArmor(Board board)
            {
                return board.HeroEnemy.CurrentHealth + board.HeroEnemy.CurrentArmor;
            }
			
			//得到自己的法强
            public static int GetSpellPower(Board board)
            {
				//计算没有被沉默的随从的法术强度之和
                return board.MinionFriend.FindAll(x => x.IsSilenced == false).Sum(x => x.SpellPower);
            }
			
			//获得第二轮斩杀血线
            public static int GetSecondTurnLethalRange(Board board)
			{
				//敌方英雄的生命值和护甲之和减去可释放法术的伤害总和
                return GetEnemyHealthAndArmor(board) - GetPlayableSpellSequenceDamages(board);
			}
			
			//下一轮是否可以斩杀敌方英雄
            public static bool HasPotentialLethalNextTurn(Board board)
			{
				//如果敌方随从没有嘲讽并且造成伤害
				//(敌方生命值和护甲的总和 减去 下回合能生存下来的当前场上随从的总伤害 减去 下回合能攻击的可使用随从伤害总和)
				//后的血量小于总法术伤害
				if(!board.MinionEnemy.Any(x => x.IsTaunt) && 
					(GetEnemyHealthAndArmor(board)-GetPotentialMinionDamages(board)-GetPlayableMinionSequenceDamages(GetPlayableMinionSequence(board),board))
						<= GetTotalBlastDamagesInHand(board))
				{
					return true;
				}
				//法术释放过敌方英雄的血量是否大于等于第二轮斩杀血线
				return GetRemainingBlastDamagesAfterSequence(board) >= GetSecondTurnLethalRange(board);
			}
			
			//获得下回合能生存下来的当前场上随从的总伤害
            public static int GetPotentialMinionDamages(Board board)
            {
                return GetPotentialMinionAttacker(board).Sum(x => x.CurrentAtk);
            }
			
			//获得下回合能生存下来的当前场上随从集合
            public static List<Card> GetPotentialMinionAttacker(Board board)
			{
				//下回合能生存下来的当前场上随从集合
				var minionscopy = board.MinionFriend.ToArray().ToList();
				
				//遍历 以敌方随从攻击力 降序排序 的 场上敌方随从集合
                foreach (var mi in board.MinionEnemy.OrderByDescending(x => x.CurrentAtk))
				{
					//以友方随从攻击力 降序排序 的 场上的所有友方随从集合，如果该集合存在生命值大于与敌方随从攻击力
					if(board.MinionFriend.OrderByDescending(x => x.CurrentAtk).Any(x => x.CurrentHealth <= mi.CurrentAtk))
					{
						//以友方随从攻击力 降序排序 的 场上的所有友方随从集合,找出该集合中友方随从的生命值小于等于敌方随从的攻击力的随从
						var tar = board.MinionFriend.OrderByDescending(x => x.CurrentAtk).FirstOrDefault(x => x.CurrentHealth <= mi.CurrentAtk);
						//将该随从移除掉
						minionscopy.Remove(tar);
					}
				}
				
				return minionscopy;
			}
			
			//获取可以使用的随从集合
            public static List<Card.Cards> GetPlayableMinionSequence(Board board)
			{
				//卡片集合
                var ret = new List<Card.Cards>();
				
				//当前剩余的法力水晶
                var manaAvailable = board.ManaAvailable;
				
				//遍历以手牌中费用降序排序的集合
                foreach (var card in board.Hand.OrderByDescending(x => x.CurrentCost))
				{
					//如果当前卡牌不为随从，继续执行
                    if (card.Type != Card.CType.MINION) continue;
					
					//当前法力值小于卡牌的费用，继续执行
                    if (manaAvailable < card.CurrentCost) continue;
					
					//添加到容器里
                    ret.Add(card.Template.Id);
					
					//修改当前使用随从后的法力水晶
                    manaAvailable -= card.CurrentCost;
				}
				
				return ret;
			}			
			
			//获取下回合能攻击的可使用随从伤害总和
            public static int GetPlayableMinionSequenceDamages(List<Card.Cards> minions, Board board)
			{
				//下回合能攻击的可使用随从集合攻击力相加
                return GetPlayableMinionSequenceAttacker(minions, board).Sum(x => CardTemplate.LoadFromId(x).Atk);
			}
			
			//获取下回合能攻击的可使用随从集合
            public static List<Card.Cards> GetPlayableMinionSequenceAttacker(List<Card.Cards> minions, Board board)
			{
				//未处理的下回合能攻击的可使用随从集合
                var minionscopy = minions.ToArray().ToList();
				
				//遍历 以敌方随从攻击力 降序排序 的 场上敌方随从集合
                foreach (var mi in board.MinionEnemy.OrderByDescending(x => x.CurrentAtk))
				{
					//以友方随从攻击力 降序排序 的 场上的所有友方随从集合，如果该集合存在生命值大于与敌方随从攻击力
					if(minions.OrderByDescending(x => CardTemplate.LoadFromId(x).Atk).Any(x => CardTemplate.LoadFromId(x).Health <= mi.CurrentAtk))
					{
						//以友方随从攻击力 降序排序 的 场上的所有友方随从集合,找出该集合中友方随从的生命值小于等于敌方随从的攻击力的随从
						var tar = minions.OrderByDescending(x => CardTemplate.LoadFromId(x).Atk).FirstOrDefault(x => CardTemplate.LoadFromId(x).Health <= mi.CurrentAtk);
						//将该随从移除掉
						minionscopy.Remove(tar);
					}
				}
				
				return minionscopy;
			}
			
			//获取当前回合手牌中的总法术伤害
            public static int GetTotalBlastDamagesInHand(Board board)
            {
				//从手牌中找出法术伤害表存在的法术的伤害总和(包括法强)
                return
                    board.Hand.FindAll(x => _spellDamagesTable.ContainsKey(x.Template.Id))
                        .Sum(x => _spellDamagesTable[x.Template.Id] + GetSpellPower(board));
            }

			//获取可以使用的法术集合
            public static List<Card.Cards> GetPlayableSpellSequence(Board board)
            {
				//卡片集合
                var ret = new List<Card.Cards>();
				
				//当前剩余的法力水晶
                var manaAvailable = board.ManaAvailable;
				
				//遍历以手牌中费用降序排序的集合
                foreach (var card in board.Hand.OrderBy(x => x.CurrentCost))
                {
					//如果手牌中又不在法术序列的法术牌，继续执行
                    if (_spellDamagesTable.ContainsKey(card.Template.Id) == false) continue;
					
					//当前法力值小于卡牌的费用，继续执行
                    if (manaAvailable < card.CurrentCost) continue;
					
					//添加到容器里
                    ret.Add(card.Template.Id);
					
					//修改当前使用随从后的法力水晶
                    manaAvailable -= card.CurrentCost;
                }

                return ret;
            }
			
			//获取存在于法术列表中的法术集合的伤害总和(包括法强)
            public static int GetSpellSequenceDamages(List<Card.Cards> sequence, Board board)
            {
                return
                    sequence.FindAll(x => _spellDamagesTable.ContainsKey(x))
                        .Sum(x => _spellDamagesTable[x] + GetSpellPower(board));
            }
			
			//得到可释放法术的伤害总和
            public static int GetPlayableSpellSequenceDamages(Board board)
            {
                return GetSpellSequenceDamages(GetPlayableSpellSequence(board), board);
            }
			
			//计算在法术释放过敌方英雄的血量
            public static int GetRemainingBlastDamagesAfterSequence(Board board)
			{
				//当前回合总法术伤害减去可释放法术的伤害总和
				return GetTotalBlastDamagesInHand(board) - GetPlayableSpellSequenceDamages(board);
			}

			//在没有法术的情况下有潜在致命的下一轮
            public static bool HasPotentialLethalNextTurnWithoutSpells(Board board)
			{
				if(!board.MinionEnemy.Any(x => x.IsTaunt) &&
					(GetEnemyHealthAndArmor(board) -
                     GetPotentialMinionDamages(board) -
                     GetPlayableMinionSequenceDamages(GetPlayableMinionSequence(board), board) <=
                     0))
				{
					return true;
				}
				return false;
			}
		}
    }
}
