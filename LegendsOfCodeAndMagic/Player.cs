using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LegendsOfCodeAndMagic
{
    
    class TradeResult
    {
        public TradeResult(IList<Card> myCards, Card oppCreature, IList<Card> allMyTableCreatures, int oppPlayerHp)
        {
            MyCards = myCards;
            OppCreature = oppCreature;
            AllMyTableCreatures = allMyTableCreatures;
            OppPlayerHp = oppPlayerHp;
        }

        public IList<Card> MyCards { get; set; }
        public Card OppCreature { get; set; }
        public IList<Card> AllMyTableCreatures { get; set; }
        public int OppPlayerHp { get; set; }

        public IEnumerable<Card> MyDeadCreatures
        {
            get
            {
                foreach (var c in MyCards.Where(c => c.IsCreature))
                {
                    if (OppCreature != null && Player.IsKilling(OppCreature, c)) yield return c;
                }
            }
        }

        public bool IsKilling
        {
            get
            {
                if (OppCreature == null) return false;

                var tmpOppCreature = new Card(OppCreature);
                for (int i = 0; i < MyCards.Count; ++i)
                {
                    var card = MyCards[i];
                    if (card.IsCreature) tmpOppCreature = Player.UpdateCreatureWithCreature(tmpOppCreature, card);
                    else tmpOppCreature = Player.UpdateCreatureWithItem(tmpOppCreature, card);
                }

                return tmpOppCreature.Defense <= 0;
            }
        }

        //public int SumDamage
        //{
        //    get
        //    {
        //        var damage = 0;
        //        for (int i = 0; i < MyCards.Count; ++i)
        //        {
        //            var card = MyCards[i];
        //            if (card.IsCreature) damage += card.Attack;
        //            else damage += Math.Abs(card.Defense);
        //        }

        //        return damage;
        //    }
        //}


        //public IList<Card> MyDeadCreatures { get; set; }
        //public bool IsGoodTrade { get; set; }//м.б. снятие щита - тоже IsGoodTrade, но не убийство

        public int GetMySumDamage(bool considerWard)
        {
            var damage = 0;
            for (int i = 0; i < MyCards.Count; ++i)
            {
                if (considerWard && OppCreature.IsWard && i == 0) continue;
                if (MyCards[i].IsCreature)
                {
                    var currDamage = MyCards[i].Attack;
                    if (MyCards[i].IsLethal) currDamage *= 2;
                    damage += currDamage;
                }
                else damage += Math.Abs(MyCards[i].Defense);
            }

            return damage;
        }

        public bool IsGoodTrade
        {
            get
            {
                bool isGoodTrade;
                if (OppCreature != null)
                {
                    var isKilling = IsKilling;

                    if (!isKilling) //снимаем щит
                    {
                        isGoodTrade = OppCreature.IsWard && 
                                      (!MyDeadCreatures.Any() || //никто из моих не умерт
                                      !MyCards.Any(c => c.IsCreature && c.IsLethal && c.Attack > 0) && //не умрут мои летальщики
                                       MyDeadCreatures.Sum(c => c.Attack) <= 3);
                    }
                    else if (OppCreature.IsGuard) //стенку всегда надо сносить
                    {
                        isGoodTrade = true;
                    }
                    else if (!MyDeadCreatures.Any())//мои не умрут - это хороший размен
                    {
                        isGoodTrade = true;
                    }
                    //else if (MyCards.Count == 1 && MyCards[0].IsCreature && MyCards[0].IsLethal && MyCards[0].Attack > 0)//убиваем врага одним летальщиком
                    //{
                    //    isGoodTrade = true;
                    //}
                    else if (OppCreature.IsLethal && OppCreature.Attack > 0)//убиваем летальщика врага
                    {
                        isGoodTrade = MyDeadCreatures.Sum(c => c.Attack) <= 3 ||
                                      Player.HasBetterTableCreature(MyCards.Where(c => c.IsCreature),
                                          AllMyTableCreatures) ||
                                      OppCreature.Attack + OppCreature.Defense >= MyDeadCreatures.Sum(c => c.Attack + c.Defense);
                    }
                    else //обычный размен
                    {
                        isGoodTrade = OppCreature.Attack + OppCreature.Defense >= MyDeadCreatures.Sum(c => c.Attack + c.Defense);

                        ////TODO: отдаем под щит любого
                        //if (!OppCreature.IsWard)
                        //    isGoodTrade = OppCreature.Attack + OppCreature.Defense >= MyDeadCreatures.Sum(c => c.Attack + c.Defense);
                        //else
                        //{
                        //    if (!MyCards.Any()) isGoodTrade = true;
                        //    else
                        //    {
                        //        if (!MyCards[0].IsCreature)
                        //        {
                        //            isGoodTrade = OppCreature.Attack + OppCreature.Defense >= MyDeadCreatures.Sum(c => c.Attack + c.Defense);
                        //        }
                        //        else
                        //        {
                        //            isGoodTrade = OppCreature.Attack + OppCreature.Defense >= MyDeadCreatures.Where(c => !Equals(c, MyCards[0])).Sum(c => c.Attack + c.Defense);
                        //        }
                        //    }
                        //}
                            
                    }
                }
                else
                {
                    var damage = 0;
                    foreach (var card in MyCards)
                    {
                        if (card.IsCreature) damage += card.Attack;
                        else if (card.IsBlueItem) damage += Math.Abs(card.Defense);
                        else throw new Exception("Unknown card type");
                    }
                    isGoodTrade = damage >= OppPlayerHp;
                }

                return isGoodTrade;
            }
        }


        /// <summary>
        /// Сравнение 2 вариантов размена
        /// </summary>
        /// <param name="result1"></param>
        /// <param name="result2"></param>
        /// <returns>-1, елси лучше 1 результат. 1, если лучше 2 результат. 0, если результаты равны</returns>
        public static int GetResultComparison(TradeResult result1, TradeResult result2)
        {
            if (result1 == null && result2 == null) return 0;
            if (result1 == null) return 1;
            if (result2 == null) return -1;

            if (result1.IsGoodTrade && !result2.IsGoodTrade) return -1;
            if (!result1.IsGoodTrade && result2.IsGoodTrade) return 1;

            if (result1.IsKilling && !result2.IsKilling) return -1;
            if (!result1.IsKilling && result2.IsKilling) return 1;

            var deadCreaturesDiff = result1.MyDeadCreatures.Count() - result2.MyDeadCreatures.Count();
            if (deadCreaturesDiff != 0) return deadCreaturesDiff;//если убили больше моих в 1 случае, 2 варинат лучше

            if (result1.IsKilling) //isKilling2 тоже true
            {
                var res1Value = result1.OppCreature.Attack + result1.OppCreature.Defense;
                if (result1.OppCreature.IsLethal) res1Value *= 2;
                var res2Value = result2.OppCreature.Attack + result2.OppCreature.Defense;
                if (result2.OppCreature.IsLethal) res2Value *= 2;
                var resDiff = res1Value - res2Value;
                if (resDiff != 0) return -resDiff;

                var myDeadDiff= result1.MyDeadCreatures.Sum(c => c.Attack + c.Defense) -
                       result2.MyDeadCreatures.Sum(c => c.Attack + c.Defense);
                if (myDeadDiff != 0) return myDeadDiff;

                var mySumDamageDiff = result1.GetMySumDamage(false) - result2.GetMySumDamage(false);
                if (mySumDamageDiff != 0) return mySumDamageDiff; //если нанесли больше урона в первом случае, 2 варант лучше (в 1 наносим лишний урон)

                return 0;
            }
            else//снимаем щит
            {
                var myDeadCreturesDiff = result1.MyDeadCreatures.Sum(c => c.Attack + c.Defense) -
                                         result2.MyDeadCreatures.Sum(c => c.Attack + c.Defense);
                if (myDeadCreturesDiff != 0) return myDeadCreturesDiff;
                return -(result1.GetMySumDamage(true) - result2.GetMySumDamage(true));//надо нанести больше урона
            }
        }
    }

    class PlayerData
    {
        public int PlayerHealth { get; set; }
        public int PlayerMana { get; set; }
        public int PlayerDeck { get; set; }
        public int PlayerRune { get; set; }

        public PlayerData(int playerHealth, int playerMana, int playerDeck, int playerRune)
        {
            PlayerHealth = playerHealth;
            PlayerMana = playerMana;
            PlayerDeck = playerDeck;
            PlayerRune = playerRune;
        }
    }

    class Card
    {
        public int CardNumber { get; set; }
        public int InstanceId { get; set; }
        public int Location { get; set; }
        public int CardType { get; set; }
        public int Cost { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public string Abilities { get; set; }
        public int MyHealthChange { get; set; }
        public int OpponentHealthChange { get; set; }
        public int CardDraw { get; set; }

        public Card(int cardNumber, int instanceId, int location, int cardType, int cost, int attack, int defense,
            string abilities, int myHealthChange, int opponentHealthChange, int cardDraw)
        {
            CardNumber = cardNumber;
            InstanceId = instanceId;
            Location = location;
            CardType = cardType;
            Cost = cost;
            Attack = attack;
            Defense = defense;
            Abilities = abilities;
            MyHealthChange = myHealthChange;
            OpponentHealthChange = opponentHealthChange;
            CardDraw = cardDraw;
        }

        public Card(Card card)
        {
            CardNumber = card.CardNumber;
            InstanceId = card.InstanceId;
            Location = card.Location;
            CardType = card.CardType;
            Cost = card.Cost;
            Attack = card.Attack;
            Defense = card.Defense;
            Abilities = card.Abilities;
            MyHealthChange = card.MyHealthChange;
            OpponentHealthChange = card.OpponentHealthChange;
            CardDraw = card.CardDraw;
        }

        public bool IsBreakthrough => Abilities[0] == 'B';
        public bool IsCharge => Abilities[1] == 'C';
        public bool IsDrain => Abilities[2] == 'D';
        public bool IsGuard => Abilities[3] == 'G';
        public bool IsLethal => Abilities[4] == 'L';
        public bool IsWard => Abilities[5] == 'W';


        public bool IsCreature => CardType == 0;
        public bool IsGreenItem => CardType == 1;
        public bool IsRedItem => CardType == 2;
        public bool IsBlueItem => CardType == 3;
    }

    class Player
    {
        private const int BOARD_SIZE = 6;
        private const double TOLERANCE = 1E-3;
        private static IList<Card> _handCards = new List<Card>();


        static IDictionary<int, int> GetManaCurve()
        {
            return new Dictionary<int, int>() {{1, 3}, {2, 4}, {3, 5}, {4, 6}, {5, 5}, {6, 4}, {7, 3}};
        }

        static IDictionary<int, double> GetConstCardWeights()
        {
            return new Dictionary<int, double>()
            {


                {140, -5},
                {138, -5},
                {143, -5},
                {2, -5},
                {142, -5},
                {154, -5},

                {57, -4.5},


                {16, -0.5},
                {20, -0.5},
                {24, -0.5},
                

                {100, -0.5},
                {131, -0.5},
                {149, -0.5},

                {1, -0.25},
                {14, -0.25},
                {13, -0.25},
                {39, -0.25},
                {56, -0.25},
                {71, -0.25},
                {72, -0.25},

                {91, -0.25},
                {93, -0.25},
                {123, -0.25},
                {130, -0.25},

                {27, -0.01},

                {5, 0},
                {11, 0},
                {36, 0},
                {61, 0},
                {70, 0},
                
                {86, 0},
                {90, 0},
                {112, 0},
                {118, 0},
                {125, 0},


                {136, 0},

              
                {4, 0.01},
               
                {34, 0.01},
                {47, 0.01},
                {83, 0.01},
                {104, 0.01},

                {21, 0.25},
                {38, 0.25},
                {41, 0.25},
                {81, 0.25},

                {94, 0.25},
                {97, 0.25},
                {106, 0.25},
                {111, 0.25},
                {119, 0.25},
                {120, 0.25},
                {121, 0.25},
                {126, 0.25},
                {129, 0.25},
                {135, 0.25},
                {137, 0.25},
                {145, 0.25},


                {157, 0.25},
                {159, 0.25},



                {85, 0.5},
                {87, 0.5},
                {88, 0.5},
                {115, 0.5},
                
                {141, 0.5},
                {147, 0.5},
                {155, 0.5},



                {95, 0.501},
                {96, 0.501},
                {103, 0.502},

                {50, 0.75},
                {54, 0.75},

               
                {128, 0.75},
                {144, 0.75},

                {150, 0.75},
               
                {158, 0.75},
                {148, 0.751},
                {152, 0.751},


                {52, 1},
                {64, 1},

               


                {139, 1.25},

                {66, 1.5},
                {82, 1.5},
               


                {7, 2},
                {44, 2},
                {67, 2},
                {133, 2},

                {80, 2.002},

                {151, 5},
                {53, 5},
            };
        }

        static double GetCalcCardWeight(Card card)
        {
            var weight = 0d;

            if (!card.IsRedItem)
            {
                if (card.IsLethal)
                {
                    weight += card.Defense;
                    if (card.IsWard) weight += card.Defense;
                    weight += 2;
                }
                else
                {
                    weight += card.Attack;
                    weight += card.Defense;

                    if (card.IsWard)
                    {
                        weight += card.Attack;
                        weight += card.Defense;
                    }

                    weight /= 2;
                }

                if (card.IsCreature)
                {
                    if (card.IsCharge) weight += 0.5;
                }
            }
            else
            {
                weight += Math.Abs(card.Attack);
                weight += Math.Abs(card.Defense);
                weight /= 2d;

                if (card.Abilities == "BCDGLW") weight += 1;
            }

            weight += card.CardDraw * 2;

            weight -= card.Cost;
            return weight;
        }

        static IList<int> GetBadManyCardNumbers()
        {
            return new List<int>(){152};
        }

        static double GetCardWeight(Card card, IDictionary<int, int> handManaCurve)
        {
            if (card.IsCreature && card.Attack == 0 || card.CardNumber == 153)
            {
                Console.Error.WriteLine("- infinity");
                return -double.MaxValue;
            }
            //if (card.IsGreenItem && card.Attack == 0) return -double.MaxValue;
            //if (card.IsRedItem && card.Defense >= 0) return -double.MaxValue;

            var constCardWeights = GetConstCardWeights();
            double weight;
            if (constCardWeights.ContainsKey(card.CardNumber))
            {
                weight = constCardWeights[card.CardNumber];
                Console.Error.WriteLine($"{weight} const");
            }
            else
            {
                weight = GetCalcCardWeight(card);
                Console.Error.WriteLine($"{weight}");
            }

            var badManyCardNumbers = GetBadManyCardNumbers();
            if (badManyCardNumbers.Contains(card.CardNumber))
            {
                var handCount = _handCards.Count(c => c.CardNumber == card.CardNumber);
                if (handCount >= 2)
                {
                    weight -= (handCount - 1);
                    Console.Error.WriteLine($"{weight} big count weight");
                }
            }

            if (card.IsGreenItem )
            {
                var handCount = _handCards.Count(c => c.IsGreenItem);
                if (handCount >= 5)
                {
                    weight -= (handCount - 4);
                    Console.Error.WriteLine($"{weight} many green weight");
                }
            }

            var cardDraw = _handCards.Sum(c => c.CardDraw);
            if (card.IsCreature && card.CardDraw > 0 && cardDraw > 5)
            {
                weight -= card.CardDraw;
                Console.Error.WriteLine($"{weight} many card draw weight");
            }

            if (card.Cost >= 7 && handManaCurve[7] >= 5)
            {
                weight -= (handManaCurve[7] - 4);
                Console.Error.WriteLine($"{weight} many big cards weight");
            }

            var creaturesCount = _handCards.Count(c => c.IsCreature);
            if (card.IsCreature && _handCards.Count > 20 && creaturesCount < 15)
            {
                var creaturesLack = 15 - creaturesCount;
                var roundsLeft = 30 - _handCards.Count;
                weight += creaturesLack * 1d / roundsLeft;
                Console.Error.WriteLine($"{creaturesLack} {roundsLeft} {weight} not enough creatures");
            }

            return weight;
        }

        static IDictionary<int, int> GetHandManaCuvre(IList<Card> handCards)
        {
            var handManaCurve = new Dictionary<int, int>();
            for (int i = 1; i <= 7; ++i)
            {
                handManaCurve.Add(i, 0);
            }

            foreach (var card in handCards)
            {
                var cost = card.Cost;
                if (cost == 0) cost = 1;
                else if (cost > 7) cost = 7;

                handManaCurve[cost]++;
            }

            return handManaCurve;
        }

        static IList<Card> GetPosition(IList<Card> allCreatures, IList<TradeResult> tradeResults, 
            int manaLeft, int oppPlayerHp, int myPlayerHp)
        {
            foreach (var at in tradeResults)
            {
                if (at.OppCreature == null) continue;//проатакуем героя в конце
                
                foreach (var myCreature in at.MyCards.Where(c => c.IsCreature))
                {

                    var oppCreature = allCreatures.Single(c => c.InstanceId == at.OppCreature.InstanceId);
                    var oppCreatureIndex = allCreatures.IndexOf(oppCreature);
                    var oppCreatureTmp = new Card(allCreatures[oppCreatureIndex]);

                    var realMyCreature = allCreatures.Single(c => c.InstanceId == myCreature.InstanceId);

                    var myCreatureIndex = allCreatures.IndexOf(realMyCreature);
                    var myCreatureTmp =  new Card(allCreatures[myCreatureIndex]);

                    UpdateCreatureWithCreature(oppCreatureTmp, myCreatureTmp);
                    UpdateCreatureWithCreature(myCreatureTmp, oppCreatureTmp);

                    if (oppCreatureTmp.Defense <= 0)
                    {
                        allCreatures.RemoveAt(oppCreatureIndex);
                    }
                    else
                    {
                        allCreatures[oppCreatureIndex] = oppCreatureTmp;
                    }

                    if (myCreatureTmp.Defense <= 0)
                    {
                        allCreatures.RemoveAt(myCreatureIndex);
                    }
                    else
                    {
                        allCreatures[myCreatureIndex] = myCreatureTmp;
                    }
                }
            }

            var myCreaturesOnBoardCount = allCreatures.Count(c => c.Location == 1);
            var summonningCreatures = GetSummonningCreatures(
                allCreatures.Where(c => c.Location == 0).ToList(),
                manaLeft,
                BOARD_SIZE - myCreaturesOnBoardCount,
                allCreatures.Where(c => c.Location == -1).ToList(),
                allCreatures.Where(c => c.Location == 1).ToList(),
                myPlayerHp,
                oppPlayerHp);

            foreach (var sc in summonningCreatures)
            {
                var index = allCreatures.IndexOf(sc);
                allCreatures[index] = new Card(allCreatures[index]) {Location = 1};
            }

            return allCreatures;
        }

        static void Main(string[] args)
        {
            string[] inputs;

            // game loop
            while (true)
            {
                PlayerData myPlayerData = null;
                PlayerData oppPlayerData = null;
                for (int i = 0; i < 2; i++)
                {
                    var str = Console.ReadLine();
                    Console.Error.WriteLine(str);

                    inputs = str.Split(' ');

                    int playerHealth = int.Parse(inputs[0]);
                    int playerMana = int.Parse(inputs[1]);
                    int playerDeck = int.Parse(inputs[2]);
                    int playerRune = int.Parse(inputs[3]);
                    var playerData = new PlayerData(playerHealth, playerMana, playerDeck, playerRune);
                    if (i == 0) myPlayerData = playerData;
                    else oppPlayerData = playerData;
                }

                int opponentHand = int.Parse(Console.ReadLine());
                Console.Error.WriteLine(opponentHand);

                int cardCount = int.Parse(Console.ReadLine());
                Console.Error.WriteLine(cardCount);

                var allCards = new List<Card>();

                for (int i = 0; i < cardCount; i++)
                {
                    var str = Console.ReadLine();
                    Console.Error.WriteLine(str);

                    inputs = str.Split(' ');
                    int cardNumber = int.Parse(inputs[0]);
                    int instanceId = int.Parse(inputs[1]);
                    int location = int.Parse(inputs[2]);
                    int cardType = int.Parse(inputs[3]);
                    int cost = int.Parse(inputs[4]);
                    int attack = int.Parse(inputs[5]);
                    int defense = int.Parse(inputs[6]);
                    string abilities = inputs[7];
                    int myHealthChange = int.Parse(inputs[8]);
                    int opponentHealthChange = int.Parse(inputs[9]);
                    int cardDraw = int.Parse(inputs[10]);

                    var card = new Card(cardNumber,
                        instanceId,
                        location,
                        cardType,
                        cost,
                        attack,
                        defense,
                        abilities,
                        myHealthChange,
                        opponentHealthChange,
                        cardDraw);

                    allCards.Add(card);
                }

                var isDraftPhase = myPlayerData.PlayerMana == 0;
                if (isDraftPhase)
                {
                    var manaCurve = GetManaCurve();
                    var handManaCuvre = GetHandManaCuvre(_handCards);
                    var pickedCardId = PickCard(allCards, manaCurve, handManaCuvre);
                    _handCards.Add(allCards[pickedCardId]);
                    Console.WriteLine($"PICK {pickedCardId}");
                    continue;
                }

                var manaLeft = myPlayerData.PlayerMana;
                var resultStr = "";

                //var allCreatures = allCards.Where(t => t.IsCreature).ToList();
                //var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
                //var allTableCreatures = GetAllTableCreatures(allCreatures, new List<Card>());
                //var noItemTradeResults = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                //    allAtackingCreatures,
                //    allTableCreatures,
                //    oppPlayerData.PlayerHealth,
                //    myPlayerData.PlayerHealth);

                //var redItemsTargets = UseRedItems(allCards.Where(c => c.IsRedItem).ToList(),
                //    manaLeft,
                //    allCards.Where(c => c.IsCreature).ToList(),
                //    oppPlayerData.PlayerHealth,
                //    myPlayerData.PlayerHealth,
                //    noItemTradeResults);

                //foreach (var item in redItemsTargets.Keys.ToList())
                //{
                //    manaLeft -= item.Cost;
                //    var targetCreature = allCards.Single(c => c.InstanceId == redItemsTargets[item]);
                //    UpdateCreatureWithItem(targetCreature, item);
                //    if (targetCreature.Defense <= 0)
                //    {
                //        allCards.Remove(targetCreature);
                //        allCreatures.Remove(targetCreature);
                //    }
                //    resultStr += $"USE {item.InstanceId} {redItemsTargets[item]};";
                //}

                //allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
                //noItemTradeResults = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                //    allAtackingCreatures,
                //    allTableCreatures,
                //    oppPlayerData.PlayerHealth,
                //    myPlayerData.PlayerHealth);

                //var greenItemsTargets = UseGreenItems(allCards.Where(c => c.IsGreenItem).ToList(),
                //    manaLeft,
                //    allCards.Where(c => c.IsCreature).ToList(),
                //    new List<Card>(),
                //    oppPlayerData.PlayerHealth,
                //    myPlayerData.PlayerHealth,
                //    noItemTradeResults);
                //foreach (var item in greenItemsTargets.Keys.ToList())
                //{
                //    manaLeft -= item.Cost;
                //    var targetCreature = allCards.Single(c => c.InstanceId == greenItemsTargets[item]);
                //    UpdateCreatureWithItem(targetCreature, item);
                //    //targetCreature.Attack += it.Key.Attack;
                //    //targetCreature.Defense += it.Key.Defense;
                //    resultStr += $"USE {item.InstanceId} {greenItemsTargets[item]};";
                //}

                var allCreaturesCurr = allCards.Where(t => t.IsCreature).ToList();
                var allAtackingCreaturesCurr = GetAllAttackingCreatures(allCreaturesCurr, new List<Card>());
                var allTableCreaturesCurr = GetAllTableCreatures(allCreaturesCurr, manaLeft, myPlayerData.PlayerHealth, oppPlayerData.PlayerHealth);
                var noItemTradeResults = GetAttackTargets(allCreaturesCurr.Where(c => c.Location == -1).ToList(),
                    allAtackingCreaturesCurr,
                    allTableCreaturesCurr,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    allCards.Where(c => c.IsBlueItem).ToList(),
                    manaLeft);
                var noItemsPosition = GetPosition(allCreaturesCurr, noItemTradeResults, manaLeft,
                    oppPlayerData.PlayerHealth, myPlayerData.PlayerHealth);
                var bestPoisition = noItemsPosition;
                var bestTradeResults = noItemTradeResults;

                var tradeCards = new List<Card>();
                if (!bestTradeResults.Any(tr => tr.OppCreature == null && tr.IsGoodTrade))
                {
                    var killingOppCreatures = new List<Card>();
                    while (true)
                    {
                        allCreaturesCurr = allCards.Where(t => t.IsCreature).ToList();
                        var chargeSummonningCreatures =
                            allCreaturesCurr.Where(c => c.Location == 0 && c.IsCharge).ToList();

                        IDictionary<Card, IList<TradeResult>> tradeResults = null;
                        var tradeCardsResult = PlayTradeCards(allCreaturesCurr,
                            chargeSummonningCreatures,
                            allCards.Where(c => c.IsRedItem || c.IsBlueItem).ToList(),
                            allCards.Where(c => c.IsGreenItem).ToList(),
                            oppPlayerData.PlayerHealth,
                            myPlayerData.PlayerHealth,
                            manaLeft,
                            out tradeResults);

                        if (!tradeCardsResult.Any()) break;
                        Card bestTradeCard = null;

                        var winCard = tradeResults.Keys.FirstOrDefault(key =>
                            tradeResults[key].Any(tr => tr.OppCreature == null && tr.IsGoodTrade));
                        if (winCard != null)
                        {
                            bestTradeCard = winCard;
                            bestTradeResults = tradeResults[winCard];
                        }

                        else
                        {
                            foreach (var tradeCard in tradeCardsResult)
                            {
                                allCreaturesCurr = allCards.Where(t => t.IsCreature).ToList();
                                if (tradeCard.Key.IsCreature)
                                {
                                    var index = allCreaturesCurr.IndexOf(tradeCard.Key);
                                    allCreaturesCurr[index] = new Card(allCreaturesCurr[index]) {Location = 1};
                                }
                                else
                                {

                                    var targetCreature = allCreaturesCurr.Single(c => c.InstanceId == tradeCard.Value);
                                    var index = allCreaturesCurr.IndexOf(targetCreature);
                                    var tmpTragetCreature = new Card(targetCreature);
                                    UpdateCreatureWithItem(tmpTragetCreature, tradeCard.Key);
                                    if (tmpTragetCreature.Defense <= 0)
                                    {
                                        allCreaturesCurr.RemoveAt(index);
                                    }
                                    else
                                    {
                                        allCreaturesCurr[index] = tmpTragetCreature;
                                    }
                                }

                                var position = GetPosition(allCreaturesCurr,
                                    tradeResults[tradeCard.Key],
                                    manaLeft - tradeCard.Key.Cost,
                                    oppPlayerData.PlayerHealth,
                                    myPlayerData.PlayerHealth);

                                if ((tradeCard.Key.IsRedItem || tradeCard.Key.IsBlueItem) &&
                                    allCreaturesCurr.Any(c => c.InstanceId == tradeCard.Value))
                                    continue; //TODO: снятие щита
                                if (tradeCard.Key.IsGreenItem)
                                {
                                    var allMyExist = position.Where(c => c.Location == 1)
                                        .All(c => noItemsPosition.Any(cc => cc.InstanceId == c.InstanceId));
                                    var allOppExist = noItemsPosition.Where(c => c.Location == -1 && !killingOppCreatures.Contains(c))
                                        .All(c => position.Any(cc => cc.InstanceId == c.InstanceId));
                                    if (allMyExist && allOppExist) continue;
                                }



                                var isBetterPosition = ComparePositions(position,
                                    bestPoisition,
                                    tradeCard.Key,
                                    bestTradeCard) < 0;
                                if (isBetterPosition)
                                {
                                    bestPoisition = position;
                                    bestTradeCard = tradeCard.Key;
                                    bestTradeResults = tradeResults[tradeCard.Key];
                                }
                            }
                        }


                        if (bestTradeCard == null) break;
                        tradeCards.Add(bestTradeCard);
                        manaLeft -= bestTradeCard.Cost;
                        if (bestTradeCard.IsCreature)
                        {
                            bestTradeCard.Location = 1;
                            resultStr += $"SUMMON {bestTradeCard.InstanceId};";
                        }
                        else
                        {
                            var targetCreature = allCards.Single(c => c.InstanceId == tradeCardsResult[bestTradeCard]);
                            UpdateCreatureWithItem(targetCreature, bestTradeCard);
                            if (targetCreature.Defense <= 0)
                            {
                                allCards.Remove(targetCreature);
                                allCreaturesCurr.Remove(targetCreature);
                                killingOppCreatures.Add(targetCreature);
                            }

                            allCards.Remove(bestTradeCard);
                            resultStr += $"USE {bestTradeCard.InstanceId} {tradeCardsResult[bestTradeCard]};";
                        }
                    }
                }



                //var allCreatures = allCards.Where(t => t.IsCreature).ToList();
                //var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
                //var allTableCreatures = GetAllTableCreatures(allCreatures, new List<Card>());
                //var attackTargets = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                //    allAtackingCreatures,
                //    allTableCreatures,
                //    oppPlayerData.PlayerHealth,
                //    myPlayerData.PlayerHealth);

                foreach (var at in bestTradeResults)
                {
                    if (at.OppCreature == null) continue;//проатакуем героя в конце

                    var targetId = at.OppCreature.InstanceId;
                    foreach (var myCreature in at.MyCards.Where(c => c.IsCreature))
                    {
                        UpdateCreatureWithCreature(myCreature, at.OppCreature);
                        UpdateCreatureWithCreature(at.OppCreature, myCreature);
                        if (myCreature.Defense <= 0) allCards.Remove(myCreature);
                        if (at.OppCreature.Defense <= 0) allCards.Remove(at.OppCreature);

                        resultStr += $"ATTACK {myCreature.InstanceId} {targetId};";

                    }
                }

                var summonningCreatures = new List<Card>();
                foreach (var creature in bestPoisition.Where(c => c.Location == 1))
                {
                    var allCardsCreature = allCards.Single(c => c.InstanceId == creature.InstanceId);
                    if (allCardsCreature.Location == 0)
                    {
                        manaLeft -= creature.Cost;
                        summonningCreatures.Add(allCardsCreature);
                        allCardsCreature.Location = 1;
                        resultStr += $"SUMMON {allCardsCreature.InstanceId};";
                    }
                }


                //var myCreaturesOnBoardCount = allCards.Count(c => c.IsCreature && c.Location == 1);
                //var summonningCreatures = GetSummonningCreatures(
                //    allCards.Where(c => c.IsCreature && c.Location == 0).ToList(),
                //    manaLeft,
                //    BOARD_SIZE - myCreaturesOnBoardCount,
                //    allCards.Where(c => c.Location == -1).ToList(),
                //    myPlayerData.PlayerHealth,
                //    oppPlayerData.PlayerHealth);
                //foreach (var card in summonningCreatures)
                //{
                //    manaLeft -= card.Cost;
                //    resultStr += $"SUMMON {card.InstanceId};";
                //}

                var heroAttackTradeResult = bestTradeResults.SingleOrDefault(x => x.OppCreature == null);

                //TODO: не только атакующие героя, но и просто стоящие
                //var greenItemsTargets = UseGreenItems(
                //    allCards.Where(c =>
                //        c.IsGreenItem && !tradeCards.Any(tc => tc.InstanceId == c.InstanceId)).ToList(),
                //    manaLeft,
                //    heroAttackTradeResult != null
                //        ? heroAttackTradeResult.MyCards.Where(c => c.IsCreature).ToList()
                //        : new List<Card>(),
                //    summonningCreatures);
                //foreach (var item in greenItemsTargets.Keys.ToList())
                //{
                //    manaLeft -= item.Cost;
                //    var targetCreature = allCards.Single(c => c.InstanceId == greenItemsTargets[item]);
                //    UpdateCreatureWithItem(targetCreature, item);
                //    //targetCreature.Attack += it.Key.Attack;
                //    //targetCreature.Defense += it.Key.Defense;
                //    resultStr += $"USE {item.InstanceId} {greenItemsTargets[item]};";
                //}


                var redItemsTargets = UseRedItems(allCards.Where(c => c.IsRedItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList());
                foreach(var it in redItemsTargets)
                {
                    manaLeft -= it.Key.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == it.Value);
                    UpdateCreatureWithItem(targetCreature, it.Key);

                    if (targetCreature.Defense <= 0)
                    {
                        allCards.Remove(targetCreature);
                    }

                    resultStr += $"USE {it.Key.InstanceId} {it.Value};";
                }


                var blueItemsTargets = UseBlueItems(allCards.Where(c => c.IsBlueItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList());
                foreach (var it in blueItemsTargets)
                {
                    manaLeft -= it.Key.Cost;
                    var targetCreature = allCards.SingleOrDefault(c => c.InstanceId == it.Value);
                    if (targetCreature == null) continue;
                    UpdateCreatureWithItem(targetCreature, it.Key);

                    if (targetCreature.Defense <= 0)
                    {
                        allCards.Remove(targetCreature);
                    }

                    resultStr += $"USE {it.Key.InstanceId} {it.Value};";
                }

               
                if (heroAttackTradeResult != null)
                {
                    foreach (var card in heroAttackTradeResult.MyCards)
                    {
                        if (card.IsCreature)
                            resultStr += $"ATTACK {card.InstanceId} -1;";
                        else
                            resultStr += $"USE {card.InstanceId} -1;";
                    }
                }

                foreach (var summCreature in summonningCreatures.Where(c => c.IsCharge))
                {
                    resultStr += $"ATTACK {summCreature.InstanceId} -1;";
                }

                Console.WriteLine(resultStr);

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

            }
        }

        static int GetPositionWeight(IList<Card> position)
        {
            var myCreatures = position.Where(x => x.Location == 1);
            var oppCreatures = position.Where(x => x.Location == -1);

            var myCreaturesWeight = myCreatures.Sum(c => c.Attack + c.Defense);
            var oppCreaturesWeight = oppCreatures.Sum(c => !c.IsLethal ? c.Attack + c.Defense : (c.Attack + c.Defense) * 2);
            return myCreaturesWeight - oppCreaturesWeight;
        }

        static int ComparePositions(IList<Card> position, IList<Card> bestPoisition, Card tradeCard, Card bestTradeCard)
        {
            var isKillingStrong = bestPoisition.Any(c =>
                                    c.Location == -1 && c.Attack >= 5 &&
                                    !position.Any(cc => cc.InstanceId == c.InstanceId));
            var isBestPositionKillingStrong = position.Any(c =>
                c.Location == -1 && c.Attack >= 5 &&
                !bestPoisition.Any(cc => cc.InstanceId == c.InstanceId));

            var positionWeight = GetPositionWeight(position);
            var bestPositionWeight = GetPositionWeight(bestPoisition);

            var isSameOppCreatures =
                position.Where(c => c.Location == -1).All(c => bestPoisition.Any(cc => cc.InstanceId == c.InstanceId)) &&
                bestPoisition.Where(c => c.Location == -1).All(c => position.Any(cc => cc.InstanceId == c.InstanceId));

            var isSameMyCreatures =
                position.Where(c => c.Location == 1).All(c => bestPoisition.Any(cc => cc.InstanceId == c.InstanceId && cc.Location == 1)) &&
                bestPoisition.Where(c => c.Location == 1).All(c => position.Any(cc => cc.InstanceId == c.InstanceId && cc.Location == 1));

            var isBetterPosition = isKillingStrong && !isBestPositionKillingStrong ||
                                   positionWeight > bestPositionWeight ||
                                   positionWeight == bestPositionWeight && bestTradeCard != null &&
                                   (tradeCard.IsBlueItem || tradeCard.IsRedItem) && bestTradeCard.IsCreature ||
                                   positionWeight == bestPositionWeight && bestTradeCard != null &&
                                   (tradeCard.IsRedItem || tradeCard.IsBlueItem) &&
                                   (bestTradeCard.IsRedItem || bestTradeCard.IsBlueItem) &&
                                   (tradeCard.Defense > bestTradeCard.Defense || tradeCard.Defense == bestTradeCard.Defense && !tradeCard.IsLethal && bestTradeCard.IsLethal) ||
                                   isSameOppCreatures && isSameMyCreatures && bestTradeCard != null &&
                                    tradeCard.IsGreenItem && bestTradeCard.IsGreenItem &&
                                   (tradeCard.Attack + tradeCard.Defense < bestTradeCard.Attack + bestTradeCard.Defense);
            if (isSameOppCreatures)
            {
                var posCount = position.Count(c => c.Location == 1);
                var bestPosCount = bestPoisition.Count(c => c.Location == 1);
                if (posCount > bestPosCount || posCount == bestPosCount && isBetterPosition)
                {
                    return -1;
                }
            }
            else
            {
                if (isSameMyCreatures)
                {
                    var posCount = position.Count(c => c.Location == -1);
                    var bestPosCount = bestPoisition.Count(c => c.Location == -1);
                    if (posCount < bestPosCount || posCount == bestPosCount && isBetterPosition)
                    {
                        return -1;
                    }
                }
                else if (isBetterPosition) return -1;
            }

            return 1;

        }

        static int CompareTradeResultLists(IList<TradeResult> tradeResults1, IList<TradeResult> tradeResults2, bool isNoItemsComparing)
        {
            var isHeroKill1 = tradeResults1.Any(x => x.IsGoodTrade && x.OppCreature == null);
            var isHeroKill2 = tradeResults2.Any(x => x.IsGoodTrade && x.OppCreature == null);

            if (isHeroKill1 && !isHeroKill2) return -1;//убьем героя врага
            if (!isHeroKill1 && isHeroKill2) return 1;//убьем героя врага

            var goodResultsDiff = tradeResults1.Count(x => x.IsGoodTrade) - tradeResults2.Count(x => x.IsGoodTrade);
            if (goodResultsDiff != 0) return -goodResultsDiff;//количество успешных разменов

            var killDiffs =
                tradeResults1.Count(x => x.OppCreature != null && x.IsKilling) -
                tradeResults2.Count(x => x.OppCreature != null && x.IsKilling);
            if (killDiffs != 0) return -killDiffs; //количество убитых существ врага

            var myDeadCreaturesValuesDiff = tradeResults1.Sum(x => x.MyDeadCreatures.Sum(c => c.Attack + c.Defense)) -
                                            tradeResults2.Sum(x => x.MyDeadCreatures.Sum(c => c.Attack + c.Defense));
            if (myDeadCreaturesValuesDiff != 0) return myDeadCreaturesValuesDiff;//сумма хар-ик моих убиты существ

            var myDeadCreaturesDiff = tradeResults1.Sum(x => x.MyDeadCreatures.Count()) - tradeResults2.Sum(x => x.MyDeadCreatures.Count());
            if (myDeadCreaturesDiff != 0) return myDeadCreaturesDiff; //кол-во моих убитых существ

            //var resultsDiff = tradeResults1.Count(x => x.OppCreature != null) - tradeResults2.Count(x => x.OppCreature != null);
            //if (resultsDiff != 0) return -resultsDiff;//кол-во разменов

            var wards1 = tradeResults1.Where(tr => tr.OppCreature != null && tr.OppCreature.Attack > 0).Sum(tr => tr.MyCards.Count(x => x.IsCreature && x.IsWard));
            var wards2 = tradeResults2.Where(tr => tr.OppCreature != null && tr.OppCreature.Attack > 0).Sum(tr => tr.MyCards.Count(x => x.IsCreature && x.IsWard));
            var wardDiff = wards1 - wards2;
            if (wardDiff != 0) return wardDiff; //сколько щитов я потеряю

            if (isNoItemsComparing) return 0;

            var mySumAttack1 = tradeResults1.Where(x => x.IsGoodTrade && x.OppCreature != null).Sum(tr => tr.MyCards.Sum(c => c.IsCreature ? c.Attack : Math.Abs(c.Defense)));
            var mySumAttack2 = tradeResults2.Where(x => x.IsGoodTrade && x.OppCreature != null).Sum(tr => tr.MyCards.Sum(c => c.IsCreature ? c.Attack : Math.Abs(c.Defense)));
            var mySumAttackDiff = mySumAttack1 - mySumAttack2;
            if (mySumAttackDiff != 0) return mySumAttackDiff;//сумма атак моих существ

            var oppSumDamage1 = 0;
            foreach (var tr in tradeResults1.Where(x => x.OppCreature != null && x.IsKilling))
            {
                oppSumDamage1 += tr.OppCreature.Attack + tr.OppCreature.Defense;
                if (tr.OppCreature.IsLethal) oppSumDamage1 += tr.OppCreature.Attack + tr.OppCreature.Defense;
            }
            var oppSumDamage2 = 0;
            foreach (var tr in tradeResults2.Where(x => x.OppCreature != null && x.IsKilling))
            {
                oppSumDamage2 += tr.OppCreature.Attack + tr.OppCreature.Defense;
                if (tr.OppCreature.IsLethal) oppSumDamage2 += tr.OppCreature.Attack + tr.OppCreature.Defense;
            }
            var oppSumDamageDiff = oppSumDamage1 - oppSumDamage2;
            if (oppSumDamageDiff != 0) return -oppSumDamageDiff;//сумма навыков существ противника, которых мы будем бить


            var oppSumAttack1 = 0;
            foreach (var tr in tradeResults1.Where(x => x.OppCreature != null && x.IsKilling))
            {
                oppSumAttack1 += tr.OppCreature.Attack;
                if (tr.OppCreature.IsLethal) oppSumAttack1 += tr.OppCreature.Attack;
            }
            var oppSumAttack2 = 0;
            foreach (var tr in tradeResults2.Where(x => x.OppCreature != null && x.IsKilling))
            {
                oppSumAttack2 += tr.OppCreature.Attack;
                if (tr.OppCreature.IsLethal) oppSumAttack2 += tr.OppCreature.Attack;
            }
            var oppSumAttackDiff = oppSumAttack1 - oppSumAttack2;
            if (oppSumAttackDiff != 0) return -oppSumAttackDiff;//сумма атак существ противника, которых мы будем бить

            var mySumDamage1 = 0;
            foreach (var tr in tradeResults1.Where(x => x.OppCreature != null))
            {
                foreach (var card in tr.MyCards.Where(c => c.IsCreature && !tr.MyDeadCreatures.Contains(c)))
                {
                    if (card.IsWard) continue;
                    mySumDamage1 += tr.OppCreature.Attack;
                }
            }
            var mySumDamage2 = 0;
            foreach (var tr in tradeResults2.Where(x => x.OppCreature != null))
            {
                foreach (var card in tr.MyCards.Where(c => c.IsCreature && !tr.MyDeadCreatures.Contains(c)))
                {
                    if (card.IsWard) continue;
                    mySumDamage2 += tr.OppCreature.Attack;
                }
            }
            var mySumDamageDiff = mySumDamage1 - mySumDamage2;
            if (mySumDamageDiff != 0) return mySumDamageDiff;//урон по мне


            var heroDamage1 = 0;
            var heroTradeResult1 = tradeResults1.SingleOrDefault(x => x.OppCreature == null);
            if (heroTradeResult1 != null) heroDamage1 = heroTradeResult1.MyCards.Sum(c => c.IsCreature ? c.Attack : Math.Abs(c.Defense));

            var heroDamage2 = 0;
            var heroTradeResult2 = tradeResults2.SingleOrDefault(x => x.OppCreature == null);
            if (heroTradeResult2 != null) heroDamage2 = heroTradeResult2.MyCards.Sum(c => c.IsCreature ? c.Attack : Math.Abs(c.Defense));

            var heroDamageDiff = heroDamage1 - heroDamage2;
            if (heroDamageDiff != 0) return -heroDamageDiff;//урон по герою врага

            return 0;
        }

        static int PickCard(IList<Card> cards, IDictionary<int, int> manaCurve, IDictionary<int, int> handManaCurve)
        {
            //var badCardIds = GetBadCardIds();
            //var goodCardIds = GetGoodCardIds();

            var maxWeight = -double.MaxValue;
            var resManaCurveLack = -int.MaxValue;
            int resCardIndex = -1;


            for (int i = 0; i < cards.Count; ++i)
            {
                var card = cards[i];
                //if (badCardIds.Contains(card.CardNumber)) continue;
                //if (goodCardIds.Contains(card.CardNumber)) return i;

                var cost = card.Cost;
                if (cost == 0) cost = 1;
                else if (cost > 7) cost = 7;

                var manaCurveLack = manaCurve[cost] - handManaCurve[cost];

                var cardWeight = GetCardWeight(card, handManaCurve);
                if (cardWeight - maxWeight > TOLERANCE)
                {
                    resCardIndex = i;
                    maxWeight = cardWeight;
                    resManaCurveLack = manaCurveLack;
                }

                else if (resCardIndex >= 0 && Math.Abs(cardWeight - maxWeight) < TOLERANCE)
                {

                    if (manaCurveLack > resManaCurveLack)
                    {
                        resCardIndex = i;
                        maxWeight = cardWeight;
                        resManaCurveLack = manaCurveLack;
                    }
                    else if (manaCurveLack == resManaCurveLack)
                    {
                        if (Math.Abs(card.Attack - card.Defense) < Math.Abs(cards[resCardIndex].Attack - cards[resCardIndex].Defense))
                        {
                            resCardIndex = i;
                            maxWeight = cardWeight;
                            resManaCurveLack = manaCurveLack;
                        }
                    }


                }

                //var isOkCard = (card.IsCreature || card.IsGreenItem) && card.Attack > 0 || card.IsRedItem && card.Defense < 0;
                //if (!isOkCard) continue;

                //var cost = card.Cost;
                //if (cost == 0) cost = 1;
                //else if (cost > 7) cost = 7;

                //var manaCurveLack = manaCurve[cost] - handManaCurve[cost];
                //if (maxLackCardIndex == -1 || manaCurveLack > maxLack)
                //{
                //    maxLackCardIndex = i;
                //    maxLack = manaCurveLack;
                //}
                //else if (manaCurveLack == maxLack)
                //{
                //    if (card.Attack > cards[maxLackCardIndex].Attack)
                //    {
                //        maxLackCardIndex = i;
                //        maxLack = manaCurveLack;
                //    }
                //    else if (card.Attack == cards[maxLackCardIndex].Attack)
                //    {
                //        maxLackCardIndex = i;
                //        maxLack = manaCurveLack;
                //    }
                //}
            }

            return resCardIndex >= 0 ? resCardIndex : 0;
        }

        #region SUMMON

        static IList<Card> GetSummonningCreatures(IList<Card> myCreatures, int manaLeft, int boardPlaceLeft, IList<Card> oppTableCreatures, IList<Card> myTableCreatures, int myPlayerHp, int oppPlayerHp)
        {
            var maxCards = GetMaxCards(myCreatures, new List<Card>(), manaLeft, boardPlaceLeft, oppTableCreatures, myTableCreatures, myPlayerHp, oppPlayerHp);
            return maxCards;
        }

        private static IList<Card> GetMaxCards(IList<Card> cards, IList<Card> usedCards, int manaLeft, int boardPlaceLeft,
            IList<Card> oppTableCreatures = null, IList<Card> myTableCreatures = null, int myHeroHp = 0, int oppHeroHp = 0)
        {
            var maxCards = new List<Card>();
            IList<TradeResult> bestTradeResults = null;
            if (boardPlaceLeft == 0) return maxCards;

            foreach (var card in cards)
            {
                if (usedCards.Contains(card)) continue;
                if (card.Cost > manaLeft) continue;

                var newUsedCards = new List<Card>(usedCards) { card };
                var currMaxCards = GetMaxCards(cards, newUsedCards, manaLeft - card.Cost, boardPlaceLeft - 1, oppTableCreatures, myTableCreatures, myHeroHp, oppHeroHp);

                var tmpCards = new List<Card>() { card };
                tmpCards.AddRange(currMaxCards);
                

                IList<TradeResult> tradeResults = null;
                if (oppTableCreatures != null)
                {
                    var targets = new List<Card>(tmpCards);
                    if (myTableCreatures != null) targets.AddRange(myTableCreatures);
                    tradeResults = GetAttackTargets(targets, new List<Card>(oppTableCreatures), oppTableCreatures,myHeroHp, oppHeroHp,
                        new List<Card>(), 0);
                }

                if (card.Cost == 0 || card.Cost + currMaxCards.Sum(c => c.Cost) > maxCards.Sum(c => c.Cost))
                {
                    maxCards = tmpCards;
                    bestTradeResults = tradeResults;
                }
                else if (card.Cost + currMaxCards.Sum(c => c.Cost) == maxCards.Sum(c => c.Cost))
                {
                    //if (tmpCards.Count > maxCards.Count)
                    //{
                    //    maxCards = tmpCards;
                    //    bestTradeResults = tradeResults;
                    //}
                    //else
                    //{
                    if (tradeResults != null)
                    {
                        if (bestTradeResults == null)
                        {
                            maxCards = tmpCards;
                            bestTradeResults = tradeResults;
                        }

                        var trCompare = CompareTradeResultLists(tradeResults, bestTradeResults, false);

                        if (trCompare > 0) //tradeResults хуже для врага - т.е. лучше для меня
                        {
                            maxCards = tmpCards;
                            bestTradeResults = tradeResults;
                        }

                        else if (trCompare == 0)
                        {
                            if (tmpCards.Count > maxCards.Count)
                            {
                                maxCards = tmpCards;
                                bestTradeResults = tradeResults;
                            }

                            else if (card.Attack + card.Defense + currMaxCards.Sum(c => c.Attack + c.Defense) >
                                     maxCards.Sum(c => c.Attack + c.Defense))
                            {
                                maxCards = tmpCards;
                                bestTradeResults = tradeResults;
                            }
                        }
                    }
                    else
                    {
                        if (tmpCards.Count > maxCards.Count)
                        {
                            maxCards = new List<Card>() { card };
                            maxCards.AddRange(currMaxCards);
                        }

                        else if (card.Attack + card.Defense + currMaxCards.Sum(c => c.Attack + c.Defense) >
                                 maxCards.Sum(c => c.Attack + c.Defense))
                        {
                            maxCards = new List<Card>() {card};
                            maxCards.AddRange(currMaxCards);
                        }
                    }

                    //}
                }
            }

            return maxCards;
        }

        #endregion

        #region ATTACK

        static IList<Card> GetAllAttackingCreatures(IList<Card> allCreatures, IList<Card> summonningCards)
        {
            var attackingCards = new List<Card>();
            foreach (var card in allCreatures.Where(c => c.Location == 1 ))
            {
                attackingCards.Add(card);
            }

            foreach (var card in summonningCards)
            {
                if (card.IsCharge) attackingCards.Add(card);
            }

            return attackingCards;
        }

        static IList<Card> GetAllTableCreatures(IList<Card> allCreatures, int manaLeft, int myPlayerHp, int oppPlayerHp)
        {

            var tableCreatures = new List<Card>();
            foreach (var card in allCreatures.Where(c => c.Location == 1))
            {
                tableCreatures.Add(card);
            }

            var myCreaturesOnBoardCount = allCreatures.Count(c => c.Location == 1);
            var summonningCreatures = GetSummonningCreatures(
                allCreatures.Where(c => c.Location == 0).ToList(),
                manaLeft,
                BOARD_SIZE - myCreaturesOnBoardCount,
                allCreatures.Where(c => c.Location == -1).ToList(),
                allCreatures.Where(c => c.Location == 1).ToList(),
                myPlayerHp,
                oppPlayerHp);

            foreach (var card in summonningCreatures)
            {
                tableCreatures.Add(card);
            }

            return tableCreatures;
        }

        public static bool HasBetterTableCreature(IEnumerable<Card> comparingCreatures, IList<Card> allMyTableCreatures)
        {
            var comparingSum = comparingCreatures.Where(c => !c.IsWard).Sum(c => c.Attack + c.Defense);
            var hasBetter = allMyTableCreatures.Any(c => !comparingCreatures.Any(cc => cc.InstanceId == c.InstanceId) && c.Attack + c.Defense > comparingSum);
            return hasBetter;
        }

        static TradeResult GetTargetCreatureTradeResult(Card targetCreature, IList<Card> allAtackingCreatures, IList<Card> usedCards,
           int hpLeft, bool hasWard, bool isNecessaryToKill, IList<Card> allMyTableCreatures)
        {
            //if (!hasWard)
            //{
            //    if (isNecessaryToKill)
            //    {
            //        //TODO: не всегда!
            //        //ищем юнита с ядом
            //        var lethalCreature = allAtackingCreatures.Where(c => c.IsLethal && c.Attack > 0 && !usedCards.Contains(c)).OrderBy(c => c.Attack + c.Defense)
            //            .FirstOrDefault();

            //        if (lethalCreature != null)
            //        {
            //            var cards = new List<Card>(usedCards) {lethalCreature};
            //            return new TradeResult(cards,
            //                targetCreature,
            //                allMyTableCreatures,
            //                Int32.MaxValue);
            //        }
            //    }
            //}


            TradeResult bestTradeResult = null;

            foreach (var attackingCard in allAtackingCreatures)
            {
                if (usedCards.Contains(attackingCard)) continue;

                var newUsedCards = new List<Card>(usedCards) { attackingCard };
                var isKilling = IsKilling(newUsedCards, targetCreature);

                TradeResult tradeResult;

                if (isKilling)
                {
                    tradeResult = new TradeResult(newUsedCards, targetCreature, allMyTableCreatures, Int32.MaxValue); 
                }
                else
                {
                    tradeResult = GetTargetCreatureTradeResult(
                        targetCreature,
                        allAtackingCreatures,
                        newUsedCards,
                        hpLeft - attackingCard.Attack,
                        attackingCard.Attack > 0 ? false : hasWard,
                        isNecessaryToKill,
                        allMyTableCreatures);
                }

                var resultComparison = TradeResult.GetResultComparison(tradeResult, bestTradeResult);
                if (resultComparison < 0) bestTradeResult = tradeResult;

            }

            if ((bestTradeResult == null || !bestTradeResult.IsGoodTrade) && targetCreature.IsWard && usedCards.Any())
            {
                bestTradeResult =
                    new TradeResult(targetCreature.Attack > 0 ? new List<Card>() {usedCards[0]} : usedCards,
                        targetCreature,
                        allMyTableCreatures,
                        Int32.MaxValue);
            }

            return bestTradeResult;
        }

        public static bool IsKilling(Card attackingCreature, Card defendingCreature)
        {
            if (defendingCreature.IsWard) return false;
            if (attackingCreature.IsLethal && attackingCreature.Attack > 0) return true;
            return attackingCreature.Attack >= defendingCreature.Defense;
        }

        public static bool IsKilling(IList<Card> attackingCreatures, Card defendingCreature)
        {
            var hpLeft = defendingCreature.Defense;
            for (int i = 0; i < attackingCreatures.Count; ++i)
            {
                if (defendingCreature.IsWard && i == 0) continue;//сняли щит
                if (attackingCreatures[i].IsLethal && attackingCreatures[i].Attack > 0) return true;
                hpLeft -= attackingCreatures[i].Attack;
            }

            return hpLeft <= 0;
        }


       


        static IList<Card> IsKillingOppHero(int oppHeroHp, IList<Card> attackingCreatures, bool isOppHero, IList<Card> blueCards, int manaLeft)
        {
            var res = new List<Card>();
            //TODO: BreakThroug
            var damage = attackingCreatures.Sum(c => c.Attack);
            IList<Card> maxBlueCards = new List<Card>();
            if (isOppHero)
            {
                damage += attackingCreatures.Where(c => c.Location == 0).Sum(c => -c.OpponentHealthChange);
                maxBlueCards = GetMaxCards(blueCards, new List<Card>(), manaLeft, int.MaxValue);
                foreach (var bc in maxBlueCards)
                {
                    damage += Math.Abs(bc.Defense);
                }
            }

            var isKilled = damage >= oppHeroHp;
            if (isKilled)
            {
                res.AddRange(attackingCreatures);
                res.AddRange(maxBlueCards);
            }

            return res;
        }

        static IList<TradeResult> GetAttackTargets(IList<Card> oppCreatures, IList<Card> allAttackingCreatures, IList<Card> allMyTableCreatures,
            int oppHeroHp, int myHeroHp, IList<Card> blueCards, int manaLeft)
        {
            var attackTargets = new List<TradeResult>();

            var oppGuards = new List<Card>();
            foreach (var card in oppCreatures)
            {
                if (card.IsGuard) oppGuards.Add(card);
            }

            while (allAttackingCreatures.Any())
            {
                TradeResult bestTradeResult = null;
                foreach (var guard in oppGuards)
                {
                    var guardAttackingCreatures = GetTargetCreatureTradeResult(guard,
                        allAttackingCreatures,
                        new List<Card>(),
                        guard.Defense,
                        guard.IsWard,
                        true,
                        allMyTableCreatures);

                    if (guardAttackingCreatures == null) continue;

                    if (bestTradeResult == null ||
                        TradeResult.GetResultComparison(guardAttackingCreatures, bestTradeResult) < 0)
                    {
                        bestTradeResult = guardAttackingCreatures;
                    }
                }
                if (bestTradeResult != null)
                {
                    attackTargets.Add(bestTradeResult);
                    foreach (var ac in bestTradeResult.MyCards)
                    {
                        allAttackingCreatures.Remove(ac);
                    }

                    oppGuards.Remove(bestTradeResult.OppCreature);
                }
                else break;
            }

            var notKillingGuard = oppGuards.FirstOrDefault();
            if (notKillingGuard != null && allAttackingCreatures.Any())
            {
                return attackTargets;
            }

            var killingCards = IsKillingOppHero(oppHeroHp, allAttackingCreatures, true, blueCards, manaLeft);
            if (killingCards.Any())
            {
                var killHeroTradeResult = new TradeResult(killingCards, null, allMyTableCreatures, oppHeroHp);
                attackTargets.Add(killHeroTradeResult);

                return attackTargets;
            }

            var bestStepAttackingCreatures = new List<Card>(allAttackingCreatures);
            var bestStepTradeResults =
                GetBestStepTradeResults(oppCreatures, myHeroHp, bestStepAttackingCreatures, allMyTableCreatures);

            var orderedOppentsAttackingCreatures = new List<Card>(allAttackingCreatures);
            var orderedOpponentsTradeResult =
                GetOrderedOpponentsTradeResult(oppCreatures, myHeroHp, orderedOppentsAttackingCreatures, allMyTableCreatures);

            IList<TradeResult> finalTradeResults;
            if (CompareTradeResultLists(bestStepTradeResults, orderedOpponentsTradeResult, false) <= 0)
            {
                finalTradeResults = bestStepTradeResults;
                allAttackingCreatures = bestStepAttackingCreatures;
            }
            else
            {
                finalTradeResults = orderedOpponentsTradeResult;
                allAttackingCreatures = orderedOppentsAttackingCreatures;
            }

            foreach (var tr in finalTradeResults)
            {
                attackTargets.Add(tr);
            }

            if (allAttackingCreatures.Any())
            {
                var attackHeroTradeResult =
                    new TradeResult(allAttackingCreatures, null, allMyTableCreatures, oppHeroHp); 
                attackTargets.Add(attackHeroTradeResult);
            }

            return attackTargets;
        }


        private static IList<TradeResult> GetBestStepTradeResults(
            IList<Card> oppCreatures, int myHeroHp, IList<Card> allAttackingCreatures, IList<Card> allMyTableCreatures)
        {
            var attackTargets = new List<TradeResult>();

            var leftCreatures = oppCreatures
                .Where(c => !c.IsGuard).ToList();

            var isNecessaryToKill =
                IsKillingOppHero(myHeroHp, oppCreatures.Where(c => !c.IsGuard).ToList(), false, new List<Card>(), 0).Any();

            while (allAttackingCreatures.Any())
            {
                TradeResult bestTradeResult = null;
                foreach (var creature in leftCreatures)
                {
                    var currAttackingCreatures = GetTargetCreatureTradeResult(creature,
                        allAttackingCreatures,
                        new List<Card>(),
                        creature.Defense,
                        creature.IsWard,
                        isNecessaryToKill,
                        allMyTableCreatures);

                    if (currAttackingCreatures == null) continue;
                    if (!currAttackingCreatures.IsGoodTrade && !isNecessaryToKill) continue;

                    if (bestTradeResult == null ||
                        TradeResult.GetResultComparison(currAttackingCreatures, bestTradeResult) < 0)
                    {
                        bestTradeResult = currAttackingCreatures;
                    }
                }

                if (bestTradeResult != null)
                {
                    attackTargets.Add(bestTradeResult);
                    foreach (var ac in bestTradeResult.MyCards)
                    {
                        allAttackingCreatures.Remove(ac);
                    }

                    leftCreatures.Remove(bestTradeResult.OppCreature);
                }
                else break;
            }

            return attackTargets;
        }

        private static IList<TradeResult> GetOrderedOpponentsTradeResult(
            IList<Card> oppCreatures, int myHeroHp, IList<Card> allAttackingCreatures, IList<Card> allMyTableCreatures)
        {
            var attackTargets = new List<TradeResult>();

            var leftCreatures = oppCreatures
                .Where(c => !c.IsGuard).ToList();

            var orderedOppCreatures = leftCreatures.Where(c => c.IsLethal).OrderByDescending(c => c.Defense + c.Attack)
                .ToList();//сначала убиваем летальщиков
            orderedOppCreatures.AddRange(leftCreatures.Where(c => !c.IsLethal).OrderByDescending(c => c.Defense + c.Attack));

            var isNecessaryToKill =
                IsKillingOppHero(myHeroHp, oppCreatures.Where(c => !c.IsGuard).ToList(), false, new List<Card>(), 0).Any();

            foreach (var creature in orderedOppCreatures)
            {
                var currAttackingCreatures = GetTargetCreatureTradeResult(creature,
                    allAttackingCreatures,
                    new List<Card>(),
                    creature.Defense,
                    creature.IsWard,
                    isNecessaryToKill,
                    allMyTableCreatures);

                if (currAttackingCreatures == null) continue;
                if (!currAttackingCreatures.IsGoodTrade && !isNecessaryToKill) continue;

                attackTargets.Add(currAttackingCreatures);

                foreach (var ac in currAttackingCreatures.MyCards)
                {
                    allAttackingCreatures.Remove(ac);
                }
            }

            return attackTargets;
        }

        #endregion

        #region USING_ITEMS

        public static Card UpdateCreatureWithItem(Card creature, Card item)
        {

            var strBulider = new StringBuilder(creature.Abilities);
            for (int i = 0; i < strBulider.Length; ++i)
            {
                if (item.IsGreenItem)
                {
                    if (strBulider[i] == '-') strBulider[i] = item.Abilities[i];
                }
                else//RED ITEM
                {
                    if (item.Abilities[i] != '-') strBulider[i] = '-';
                }
            }
            creature.Abilities = strBulider.ToString();

            creature.Attack += item.Attack;
            if (creature.Attack < 0) creature.Attack = 0;
            if (!creature.IsWard)
            {
                creature.Defense += item.Defense;
                if (creature.Defense < 0) creature.Defense = 0;
            }
            else if (item.Defense < 0)
            {
                strBulider = new StringBuilder(creature.Abilities) {[5] = '-'};
                creature.Abilities = strBulider.ToString();
            }

            return creature;
        }

        public static Card UpdateCreatureWithCreature(Card creature, Card attackingCreature)
        {
            if (creature.IsWard && attackingCreature.Attack > 0)
            {
                var strBulider = new StringBuilder(creature.Abilities) {[5] = '-'};
                creature.Abilities = strBulider.ToString();
                return creature;
            }

            if (IsKilling(attackingCreature, creature))
            {
                creature.Defense = 0;
                return creature;
            }

            creature.Defense -= attackingCreature.Attack;
            if (creature.Defense < 0) creature.Defense = 0;
            return creature;
        }
       

        static IDictionary<Card, int> PlayTradeCards(IList<Card> allCreatures, IList<Card> chargeSummonnigCreatures, IList<Card> redAndBlueItems,
            IList<Card> greenItems, int oppHeroHp, int myHeroHp, int manaLeft, out IDictionary<Card, IList<TradeResult>> bestTradeResults)
        {
            var oppCreatures = allCreatures.Where(c => c.Location == -1).ToList();

            bestTradeResults = new Dictionary<Card, IList<TradeResult>>();
            var cardToPlay = new Dictionary<Card, int>();
            var blueItems = redAndBlueItems.Where(i => i.IsBlueItem).ToList();

            foreach (var chargeCreature in chargeSummonnigCreatures)
            {
                if (chargeCreature.Cost > manaLeft) continue;

                var summoningCreatures = new List<Card> {chargeCreature};
                var chargeAllAtackingCreatures = GetAllAttackingCreatures(allCreatures, summoningCreatures);
                var chargeAllMyTableCreatures = GetAllTableCreatures(new List<Card>(allCreatures) {chargeCreature},
                    manaLeft - chargeCreature.Cost,
                    myHeroHp,
                    oppHeroHp);
                var chargeTradeResult = GetAttackTargets(oppCreatures,
                    chargeAllAtackingCreatures,
                    chargeAllMyTableCreatures,
                    oppHeroHp,
                    myHeroHp,
                    blueItems,
                    manaLeft - chargeCreature.Cost);

                var targetId = -1;
                var chargeTr = chargeTradeResult.SingleOrDefault(x =>
                    x.OppCreature != null && x.MyCards.Any(c => c.InstanceId == chargeCreature.InstanceId));
                if (chargeTr != null) targetId = chargeTr.OppCreature.InstanceId;
                //if (bestTradeResults == null || CompareTradeResultLists(chargeTradeResult, bestTradeResults, false) < 0)
                //{
                    bestTradeResults.Add(chargeCreature, chargeTradeResult);
                    cardToPlay.Add(chargeCreature, targetId);
                //}
            }

            var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
            var allMyTableCreatures = GetAllTableCreatures(allCreatures, manaLeft, myHeroHp, oppHeroHp);

            foreach (var rbItem in redAndBlueItems)
            {
                if (rbItem.Cost > manaLeft) continue;

                IList<TradeResult> currBestResults = null;
                var creatureIndex = -1;
                for (int i = 0; i < oppCreatures.Count; ++i)
                {
                    var creature = oppCreatures[i];

                    var newCreature = UpdateCreatureWithItem(new Card(creature), rbItem);

                    IList<Card> newOppCreatures = new List<Card>(oppCreatures);
                    if (newCreature.Defense <= 0)
                    {
                        newOppCreatures.RemoveAt(i);
                    }
                    else
                    {
                        newOppCreatures[i] = newCreature;
                    }
                    
                    var attackTargets = GetAttackTargets(newOppCreatures,
                        new List<Card>(allAtackingCreatures), allMyTableCreatures, oppHeroHp, myHeroHp,
                        blueItems, manaLeft - rbItem.Cost);

                    if (!attackTargets.Any(at => at.OppCreature == null && at.IsGoodTrade))
                    {
                        if (rbItem.CardNumber == 151) //убивающая всех карта
                        {
                            double value = creature.Attack + creature.Defense;
                            if (creature.IsWard) value *= 2;
                            if (creature.IsLethal) value *= 1.5;
                            if (value < 9) continue;
                        }
                        else if (rbItem.CardNumber == 152) //топор -7
                        {
                            if (creature.IsWard) continue;
                            if (creature.Defense < 5 && creature.Attack + creature.Defense < 9) continue;
                        }
                        else if (rbItem.CardNumber == 148)
                        {
                            if (!creature.IsWard && !creature.IsLethal && creature.Attack <= 2) continue;
                        }
                    }

                    var tr = attackTargets.SingleOrDefault(x =>
                        x.OppCreature != null && x.OppCreature.InstanceId == creature.InstanceId);
                    if (tr != null) tr.MyCards.Insert(0, rbItem);
                    else attackTargets.Add(new TradeResult(new List<Card>() { rbItem }, creature, allMyTableCreatures, oppHeroHp));


                    if (currBestResults == null || CompareTradeResultLists(attackTargets, currBestResults, false) < 0)
                    {
                        currBestResults = attackTargets;
                        creatureIndex = creature.InstanceId;
                    }

                }

                if (currBestResults != null)
                {
                    bestTradeResults.Add(rbItem, currBestResults);
                    cardToPlay.Add(rbItem, creatureIndex);
                }
            }

            allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
            foreach (var greenItem in greenItems)
            {
                if (greenItem.Cost > manaLeft) continue;

                IList<TradeResult> currBestResults = null;
                var creatureIndex = -1;
                for (int i = 0; i < allAtackingCreatures.Count; ++i)
                {
                    var creature = allAtackingCreatures[i];
                    var newCreature = UpdateCreatureWithItem(new Card(creature), greenItem);
                    allAtackingCreatures[i] = newCreature;

                    var attackTargets = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                        new List<Card>(allAtackingCreatures), allMyTableCreatures, oppHeroHp, myHeroHp, blueItems, manaLeft - greenItem.Cost);
                    if (currBestResults == null || CompareTradeResultLists(attackTargets, currBestResults, false) < 0)
                    {
                        currBestResults = attackTargets;
                        creatureIndex = creature.InstanceId;
                    }

                    allAtackingCreatures[i] = creature;
                }

                if (currBestResults != null)
                {
                    bestTradeResults.Add(greenItem, currBestResults);
                    cardToPlay.Add(greenItem, creatureIndex);
                }

            }

            return cardToPlay;
        }

        static Card GetGreenItemCreature(Card item, IList<Card> notTradingCreatures, IList<Card> summonningCreatures)
        {
            Card weakestCreature = null;
            foreach (var c in notTradingCreatures)
            {
                if (c.IsWard && item.IsWard) continue;
                if (c.IsLethal && item.IsLethal) continue;

                if (weakestCreature == null ||
                    c.Attack + c.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    weakestCreature = c;
            }

            foreach (var sc in summonningCreatures.Where(x => x.IsCharge))
            {
                if (sc.IsWard && item.IsWard) continue;
                if (sc.IsLethal && item.IsLethal) continue;

                if (weakestCreature == null ||
                    sc.Attack + sc.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    weakestCreature = sc;
            }

            if (weakestCreature != null) return weakestCreature;

            foreach (var sc in summonningCreatures.Where(x => !x.IsCharge))
            {
                if (sc.IsWard && item.IsWard) continue;
                if (sc.IsLethal && item.IsLethal) continue;

                if (weakestCreature == null ||
                    sc.Attack + sc.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    weakestCreature = sc;
            }

            return weakestCreature;
        }

        static IDictionary<Card, int> UseGreenItems(IList<Card> items, int manaLeft, IList<Card> notTradingCreatures, 
            IList<Card> summonningCreatures
            )
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                Card giCreature = GetGreenItemCreature(item, notTradingCreatures, summonningCreatures);
                if (giCreature != null)
                {
                    itemTargets.Add(item, giCreature.InstanceId);
                }
            }

            return itemTargets;
        }

        static Card GetBlueItemCreature(IList<Card> allCreatures)
        {
            return null;
        }

        static IDictionary<Card, int> UseBlueItems(IList<Card> items, int manaLeft, IList<Card> allCreatures)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                if (item.Defense < 0) continue;//не кидаем item в героя, если можно в существо
                var biCreature = GetBlueItemCreature(allCreatures);
                if (biCreature != null) itemTargets.Add(item, biCreature.InstanceId);
                else itemTargets.Add(item, -1);
            }

            return itemTargets;
        }


        static IDictionary<Card, int> UseRedItems(IList<Card> items, int manaLeft, IList<Card> allCreatures)
        {
            var itemTargets = new Dictionary<Card, int>();

            var oppCreatures = allCreatures.Where(c => c.Location == -1)
                .OrderByDescending(c => c.Attack + c.Defense).ToList();
            if (!oppCreatures.Any()) return itemTargets;

            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue).OrderByDescending(i => i.Defense).ToList();

            foreach (var creature in oppCreatures)
            {
                if (creature.IsWard)
                {
                    foreach (var item in maxItems.Where(x => !itemTargets.ContainsKey(x)))
                    {
                        if (item.Defense == 0 && item.IsWard || item.Defense >= -2)
                        {
                            itemTargets.Add(item, creature.InstanceId);
                            break;
                        }

                        if (item.CardNumber == 151)
                        {
                            double value = creature.Attack + creature.Defense;
                            if (creature.IsWard) value *= 2;
                            if (creature.IsLethal) value *= 1.5;
                            if (value >= 10)
                            {
                                itemTargets.Add(item, creature.InstanceId);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var defence = creature.Defense;
                    var killingItems = new List<Card>();
                    foreach (var rbItem in maxItems.Where(x => !itemTargets.ContainsKey(x)))
                    {
                        if (rbItem.Defense >= 0) continue;//карта без урона

                        if (rbItem.CardNumber == 151) //убивающая всех карта
                        {
                            double value = creature.Attack + creature.Defense;
                            if (creature.IsWard) value *= 2;
                            if (creature.IsLethal) value *= 1.5;
                            if (value < 9) continue;
                        }
                        else if (rbItem.CardNumber == 152) //топор -7
                        {
                            if (creature.IsWard) continue;
                            if (creature.Defense < 5 && creature.Attack + creature.Defense < 9) continue;
                        }
                        else if (rbItem.CardNumber == 148)
                        {
                            if (!creature.IsWard && !creature.IsGuard && creature.Attack <= 2) continue; 
                        }

                        if (rbItem.Defense + creature.Defense <= 0)
                        {
                            defence = 0;
                            killingItems = new List<Card>(){rbItem};
                            break;
                        }

                        defence += rbItem.Defense;
                        killingItems.Add(rbItem);
                        if (defence <= 0) break;
                    }

                    if (defence <= 0)
                    {
                        foreach (var ki in killingItems)
                        {
                            itemTargets.Add(ki, creature.InstanceId);
                        }
                    }
                }
            }
            //foreach (var item in maxItems)
            //{


            //    if (item.Attack > 0 || !item.IsWard) continue;
            //    var oppWardCreature = oppWardCreatures.FirstOrDefault();
            //    if (oppWardCreature != null)
            //    {
            //        itemTargets.Add(item, oppWardCreature.InstanceId);
            //        oppWardCreatures.Remove(oppWardCreature);
            //    }
            //}

            return itemTargets;
        }

        #endregion

    }
}
