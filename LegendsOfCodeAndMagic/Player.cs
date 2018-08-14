using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendsOfCodeAndMagic
{
    class AttackResult
    {
        public IList<TradeResult> TradeResults { get; set; }

    }

    class TradeResult
    {
        public TradeResult()
        {
            MyCreatures = new List<Card>();
        }

        public IList<Card> MyCreatures { get; set; }
        public Card OppCreature { get; set; }

        public IList<Card> MyDeadCreatures { get; set; }
        public bool IsGoodTrade { get; set; }

        public int GetMySumDamage(bool consiedWard)
        {
            var damage = 0;
            for (int i = 0; i < MyCreatures.Count; ++i)
            {
                if (consiedWard && OppCreature.IsWard && i == 0) continue;
                damage += MyCreatures[i].Attack;
            }

            return damage;
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

            var isKilling1 = Player.IsKilling(result1.MyCreatures, result1.OppCreature);
            var isKilling2 = Player.IsKilling(result2.MyCreatures, result2.OppCreature);
            if (isKilling1 && !isKilling2) return -1;
            if (isKilling2 && !isKilling1) return 1;

            var deadCreaturesDiff = result1.MyDeadCreatures.Count - result2.MyDeadCreatures.Count;
            if (deadCreaturesDiff != 0) return deadCreaturesDiff;//если убили больше моих в 1 случае, 2 варинат лучше

            if (isKilling1) //isKilling2 тоже true
            {
                return result1.GetMySumDamage(false) - result2.GetMySumDamage(false); //если нанесли больше урона в первом случае, 2 варант лучше (в 1 наносим лишний урон)
            }
            else
            {
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

        static IList<int> GetBadCardIds()
        {
            return new List<int>() { 57, 4, 100, 140, 138, 143, 83, 86, 2 };
        }

        static IList<int> GetGoodCardIds()
        {
            return new List<int>() { 151, 53 };
        }

        static IDictionary<int, int> GetManaCurve()
        {
            return new Dictionary<int, int>() { { 1, 3 }, { 2, 4 }, { 3, 5 }, { 4, 6 }, { 5, 5 }, { 6, 4 }, { 7, 3 } };
        }

        static double GetCardWeight(Card card)
        {
            if (card.IsCreature && card.Attack == 0) return -double.MaxValue;
            //if (card.IsGreenItem && card.Attack == 0) return -double.MaxValue;
            //if (card.IsRedItem && card.Defense >= 0) return -double.MaxValue;


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
            Console.Error.WriteLine(weight);
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

                var allCreatures = allCards.Where(t => t.IsCreature).ToList();
                var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
                var allTableCreatures = GetAllTableCreatures(allCreatures, new List<Card>());
                var noItemTradeResults = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                    allAtackingCreatures,
                    allTableCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth);

                var redItemsTargets = UseRedItems(allCards.Where(c => c.IsRedItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList(),
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    noItemTradeResults);

                foreach (var item in redItemsTargets.Keys.ToList())
                {
                    manaLeft -= item.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == redItemsTargets[item]);
                    UpdateCreatureWithItem(targetCreature, item);
                    if (targetCreature.Defense <= 0)
                    {
                        allCards.Remove(targetCreature);
                        allCreatures.Remove(targetCreature);
                    }
                    resultStr += $"USE {item.InstanceId} {redItemsTargets[item]};";
                }

                allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
                noItemTradeResults = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                    allAtackingCreatures,
                    allTableCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth);

                var greenItemsTargets = UseGreenItems(allCards.Where(c => c.IsGreenItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList(),
                    new List<Card>(),
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    noItemTradeResults);
                foreach (var item in greenItemsTargets.Keys.ToList())
                {
                    manaLeft -= item.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == greenItemsTargets[item]);
                    UpdateCreatureWithItem(targetCreature, item);
                    //targetCreature.Attack += it.Key.Attack;
                    //targetCreature.Defense += it.Key.Defense;
                    resultStr += $"USE {item.InstanceId} {greenItemsTargets[item]};";
                }

                var myCreaturesOnBoardCount = allCards.Count(c => c.IsCreature && c.Location == 1);
                var summonningCreatures = GetSummonningCreatures(
                    allCards.Where(c => c.IsCreature && c.Location == 0).ToList(),
                    manaLeft,
                    BOARD_SIZE - myCreaturesOnBoardCount,
                    allCards.Where(c => c.Location == -1).ToList(),
                    myPlayerData.PlayerHealth,
                    oppPlayerData.PlayerHealth);
                foreach (var card in summonningCreatures)
                {
                    manaLeft -= card.Cost;
                    resultStr += $"SUMMON {card.InstanceId};";
                }

                greenItemsTargets = UseGreenItems(
                    allCards.Where(c =>
                        c.IsGreenItem && !greenItemsTargets.Keys.Contains(c)).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList(),
                    summonningCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    null);
                foreach (var item in greenItemsTargets.Keys.ToList())
                {
                    manaLeft -= item.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == greenItemsTargets[item]);
                    UpdateCreatureWithItem(targetCreature, item);
                    //targetCreature.Attack += it.Key.Attack;
                    //targetCreature.Defense += it.Key.Defense;
                    resultStr += $"USE {item.InstanceId} {greenItemsTargets[item]};";
                }



                var blueItemsTargets = UseBlueItems(allCards.Where(c => c.IsBlueItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList());

                foreach (var it in blueItemsTargets)
                {
                    manaLeft -= it.Key.Cost;
                    var targetCreature = allCards.SingleOrDefault(c => c.InstanceId == it.Value);
                    if (targetCreature != null)
                    {
                        targetCreature.Attack += it.Key.Attack;
                        targetCreature.Defense += it.Key.Defense;
                    }

                    resultStr += $"USE {it.Key.InstanceId} {it.Value};";
                }

                allCreatures = allCards.Where(t => t.IsCreature).ToList();
                allAtackingCreatures = GetAllAttackingCreatures(allCreatures, summonningCreatures);
                allTableCreatures = GetAllTableCreatures(allCreatures, summonningCreatures);
                var attackTargets = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                    allAtackingCreatures,
                    allTableCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth);

                foreach (var at in attackTargets)
                {
                    var targetId = at.OppCreature != null ? at.OppCreature.InstanceId : -1;
                    foreach (var myCreature in at.MyCreatures)
                        resultStr += $"ATTACK {myCreature.InstanceId} {targetId};";
                }


                Console.WriteLine(resultStr);

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

            }
        }

        static int CompareTradeResultLists(IList<TradeResult> tradeResults1, IList<TradeResult> tradeResults2)
        {
            var isHeroKill1 = tradeResults1.Any(x => x.IsGoodTrade && x.OppCreature == null);
            var isHeroKill2 = tradeResults2.Any(x => x.IsGoodTrade && x.OppCreature == null);

            if (isHeroKill1 && !isHeroKill2) return -1;//убьем героя врага
            if (!isHeroKill1 && isHeroKill2) return 1;//убьем героя врага

            var goodResultsDiff = tradeResults1.Count(x => x.IsGoodTrade) - tradeResults2.Count(x => x.IsGoodTrade);
            if (goodResultsDiff != 0) return -goodResultsDiff;

            var myDeadCreaturesValuesDiff = tradeResults1.Sum(x => x.MyDeadCreatures.Sum(c => c.Attack + c.Defense)) -
                                            tradeResults2.Sum(x => x.MyDeadCreatures.Sum(c => c.Attack + c.Defense));
            if (myDeadCreaturesValuesDiff != 0) return myDeadCreaturesValuesDiff;

            var myDeadCreaturesDiff = tradeResults1.Sum(x => x.MyDeadCreatures.Count) - tradeResults2.Sum(x => x.MyDeadCreatures.Count);
            if (myDeadCreaturesDiff != 0) return myDeadCreaturesDiff;

            var resultsDiff = tradeResults1.Count - tradeResults2.Count;
            if (resultsDiff != 0) return -resultsDiff;

            return 0;
        }

        static int PickCard(IList<Card> cards, IDictionary<int, int> manaCurve, IDictionary<int, int> handManaCurve)
        {
            var badCardIds = GetBadCardIds();
            var goodCardIds = GetGoodCardIds();

            var maxWeight = -double.MaxValue;
            var resManaCurveLack = -int.MaxValue;
            int resCardIndex = -1;


            for (int i = 0; i < cards.Count; ++i)
            {
                var card = cards[i];
                if (badCardIds.Contains(card.CardNumber)) continue;
                if (goodCardIds.Contains(card.CardNumber)) return i;

                var cost = card.Cost;
                if (cost == 0) cost = 1;
                else if (cost > 7) cost = 7;

                var manaCurveLack = manaCurve[cost] - handManaCurve[cost];

                var cardWeight = GetCardWeight(card);
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

            return resCardIndex;
        }

        #region SUMMON

        static IList<Card> GetSummonningCreatures(IList<Card> myCreatures, int manaLeft, int boardPlaceLeft, IList<Card> oppTableCreatures, int myPlayerHp, int oppPlayerHp)
        {
            var maxCards = GetMaxCards(myCreatures, new List<Card>(), manaLeft, boardPlaceLeft, oppTableCreatures, myPlayerHp, oppPlayerHp);
            return maxCards;
        }

        private static IList<Card> GetMaxCards(IList<Card> cards, IList<Card> usedCards, int manaLeft, int boardPlaceLeft,
            IList<Card> oppTableCreatures = null, int myHeroHp = 0, int oppHeroHp = 0)
        {
            var maxCards = new List<Card>();
            IList<TradeResult> bestTradeResults = null;
            if (boardPlaceLeft == 0) return maxCards;

            foreach (var card in cards)
            {
                if (usedCards.Contains(card)) continue;
                if (card.Cost > manaLeft) continue;

                var newUsedCards = new List<Card>(usedCards) { card };
                var currMaxCards = GetMaxCards(cards, newUsedCards, manaLeft - card.Cost, boardPlaceLeft - 1, oppTableCreatures, myHeroHp, oppHeroHp);

                var tmpCards = new List<Card>() { card };
                tmpCards.AddRange(currMaxCards);

                IList<TradeResult> tradeResults = null;
                if (oppTableCreatures != null)
                {
                    tradeResults = GetAttackTargets(tmpCards, new List<Card>(oppTableCreatures), oppTableCreatures, oppHeroHp,
                        myHeroHp);
                }

                if (card.Cost == 0 || card.Cost + currMaxCards.Sum(c => c.Cost) > maxCards.Sum(c => c.Cost))
                {
                    maxCards = tmpCards;
                    bestTradeResults = tradeResults;
                }
                else if (card.Cost + currMaxCards.Sum(c => c.Cost) == maxCards.Sum(c => c.Cost))
                {
                    if (tradeResults != null)
                    {
                        if (bestTradeResults == null)
                        {
                            maxCards = tmpCards;
                            bestTradeResults = tradeResults;
                        }

                        var trCompare = CompareTradeResultLists(tradeResults, bestTradeResults);

                        if (trCompare > 0) //tradeResults хуже для врага - т.е. лучше для меня
                        {
                            maxCards = tmpCards;
                            bestTradeResults = tradeResults;
                        }

                        else if (trCompare == 0)
                        {
                            if (card.Attack + currMaxCards.Sum(c => c.Attack) > maxCards.Sum(c => c.Attack))
                            {
                                maxCards = tmpCards;
                                bestTradeResults = tradeResults;
                            }
                        }
                    }
                    else
                    {
                        if (card.Attack + currMaxCards.Sum(c => c.Attack) > maxCards.Sum(c => c.Attack))
                        {
                            maxCards = new List<Card>() { card };
                            maxCards.AddRange(currMaxCards);
                        }
                    }
                }
            }

            return maxCards;
        }

        #endregion

        #region ATTACK

        static IList<Card> GetAllAttackingCreatures(IList<Card> allCreatures, IList<Card> summonningCards)
        {
            var attackingCards = new List<Card>();
            foreach (var card in allCreatures.Where(c => c.Location == 1 && c.Attack > 0))
            {
                attackingCards.Add(card);
            }

            foreach (var card in summonningCards.Where(c => c.Attack > 0))
            {
                if (card.IsCharge) attackingCards.Add(card);
            }

            return attackingCards;
        }

        static IList<Card> GetAllTableCreatures(IList<Card> allCreatures, IList<Card> summonningCards)
        {
            var tableCreatures = new List<Card>();
            foreach (var card in allCreatures.Where(c => c.Location == 1))
            {
                tableCreatures.Add(card);
            }
            foreach (var card in summonningCards)
            {
                tableCreatures.Add(card);
            }

            return tableCreatures;
        }

        static bool IsGoodTrade(Card myCreature, Card oppCreature, bool hasBetterTableCreatures, bool hasWard)
        {
            //if (myCreature.IsGuard) return false;
            if (myCreature.IsWard && !hasWard && myCreature.Attack >= oppCreature.Defense) return true;//мы со щитом убьем с 1 удара
            if (!myCreature.IsWard && !hasWard && myCreature.Attack >= oppCreature.Defense &&
                myCreature.Defense > oppCreature.Attack && !oppCreature.IsLethal) return true; //убьем с 1 удара и не помрем

            if (oppCreature.IsLethal && hasBetterTableCreatures) return true;

            if (oppCreature.Attack > myCreature.Attack && myCreature.Attack - oppCreature.Defense <= 1) return true;
            if (oppCreature.Attack + oppCreature.Defense - myCreature.Attack - myCreature.Defense >= 2) return true;
            return false;
            //return oppCreature.Attack > myCreature.Attack || oppCreature.Attack == myCreature.Attack && oppCreature.Defense > myCreature.Defense;
        }

        static bool HasBetterTableCreature(IList<Card> comparingCreatures, IList<Card> allMyTableCreatures)
        {
            var comparingSum = comparingCreatures.Where(c => !c.IsWard).Sum(c => c.Attack + c.Defense);
            var hasBetter = allMyTableCreatures.Any(c => !comparingCreatures.Any(cc => cc.InstanceId == c.InstanceId) && c.Attack + c.Defense > comparingSum);
            return hasBetter;
        }

        static TradeResult GetTradeResult(IList<Card> myCreatures, Card oppCreature, IList<Card> allMyTableCreatures)
        {
            var myDeadCreatures = myCreatures.Where(c => IsKilling(oppCreature, c)).ToList();

            bool isGoodTrade = false;
            if (oppCreature.IsLethal && oppCreature.Attack > 0 && allMyTableCreatures != null)
            {
                var hasBetterTableCreatures = HasBetterTableCreature(myCreatures, allMyTableCreatures);
                if (hasBetterTableCreatures) isGoodTrade = true;
            }
            else
                isGoodTrade = oppCreature.Attack + oppCreature.Defense >= myDeadCreatures.Sum(c => c.Attack + c.Defense);

            return new TradeResult()
            {
                IsGoodTrade = isGoodTrade,
                MyCreatures = myCreatures,
                OppCreature = oppCreature,
                MyDeadCreatures = myDeadCreatures
            };
        }

        static TradeResult GetRemoveWardTradeResult(Card myCreature, Card oppCreature)
        {
            bool isGoodTrade;
            int myDeadCreaturesNumber;

            var isKilling = IsKilling(oppCreature, myCreature);
            var myDeadCreatures = new List<Card>();
            if (isKilling) myDeadCreatures.Add(myCreature);
            return new TradeResult
            {
                IsGoodTrade = !isKilling,
                MyCreatures = new List<Card>() { myCreature },
                OppCreature = oppCreature,
                MyDeadCreatures = myDeadCreatures
            };
        }

        static bool IsKilling(Card attackingCreature, Card defendingCreature)
        {
            if (defendingCreature.IsWard) return false;
            if (attackingCreature.IsLethal) return true;
            return attackingCreature.Attack >= defendingCreature.Defense;
        }

        public static bool IsKilling(IList<Card> attackingCreatures, Card defendingCreature)
        {
            var hpLeft = defendingCreature.Defense;
            for (int i = 0; i < attackingCreatures.Count; ++i)
            {
                if (defendingCreature.IsWard && i == 0) continue;//сняли щит
                if (attackingCreatures[i].IsLethal) return true;
                hpLeft -= attackingCreatures[i].Attack;
            }

            return hpLeft <= 0;
        }


        static TradeResult GetTargetCreatureTradeResult(Card targetCreature, IList<Card> allAtackingCreatures, IList<Card> usedCards,
            int hpLeft, bool hasWard, bool isNecessaryToKill, IList<Card> allMyTableCreatures)
        {
            if (!hasWard)
            {
                if (isNecessaryToKill)
                {
                    //ищем юнита с ядом
                    var lethalCreature = allAtackingCreatures.Where(c => c.IsLethal).OrderBy(c => c.Attack + c.Defense)
                        .FirstOrDefault();

                    if (lethalCreature != null)
                    {
                        var myDeadCreatures = new List<Card>();
                        if (IsKilling(targetCreature, lethalCreature)) myDeadCreatures.Add(lethalCreature);
                        return new TradeResult()
                        {
                            IsGoodTrade = true,
                            MyCreatures = new List<Card>() { lethalCreature },
                            OppCreature = targetCreature,
                            MyDeadCreatures = myDeadCreatures
                        };
                    }
                }
            }


            TradeResult bestTradeResult = null;

            foreach (var attackingCard in allAtackingCreatures)
            {
                if (usedCards.Contains(attackingCard)) continue;

                var newUsedCards = new List<Card>(usedCards) { attackingCard };
                var isKilling = IsKilling(newUsedCards, targetCreature);

                TradeResult tradeResult;

                if (isKilling)
                {
                    tradeResult = GetTradeResult(newUsedCards, targetCreature, allMyTableCreatures);
                }
                else
                {
                    tradeResult = GetTargetCreatureTradeResult(
                        targetCreature,
                        allAtackingCreatures,
                        newUsedCards,
                        hpLeft - attackingCard.Attack,
                        hasWard,
                        isNecessaryToKill,
                        allMyTableCreatures);
                }

                var resultComparison = TradeResult.GetResultComparison(tradeResult, bestTradeResult);
                if (resultComparison < 0) bestTradeResult = tradeResult;

            }

            if (bestTradeResult == null && targetCreature.IsWard && usedCards.Any())
            {
                if (targetCreature.Attack > 0)
                    bestTradeResult = GetRemoveWardTradeResult(usedCards[0], targetCreature);
                else
                {
                    bestTradeResult = GetTradeResult(usedCards, targetCreature, allMyTableCreatures);
                }

            }

            return bestTradeResult;
        }


        static bool IsKillingOppHero(int oppHeroHp, IList<Card> attackingCreatures, bool isOppHero)
        {
            //TODO: BreakThroug
            var damage = attackingCreatures.Sum(c => c.Attack);
            if (isOppHero) damage += attackingCreatures.Where(c => c.Location == 0).Sum(c => -c.OpponentHealthChange);
            return damage >= oppHeroHp;
        }

        static IList<TradeResult> GetAttackTargets(IList<Card> oppCreatures, IList<Card> allAttackingCreatures, IList<Card> allMyTableCreatures,
            int oppHeroHp, int myHeroHp)
        {
            var attackTargets = new List<TradeResult>();

            var oppGuards = new List<Card>();
            foreach (var card in oppCreatures)
            {
                if (card.IsGuard) oppGuards.Add(card);
            }

            var orderedOppGuards = oppGuards.OrderByDescending(og => og.Defense).ToList();
            var notKillingGuards = new List<Card>();

            foreach (var guard in orderedOppGuards)
            {
                var guardAttackingCreatures = GetTargetCreatureTradeResult(guard,
                    allAttackingCreatures,
                    new List<Card>(),
                    guard.Defense,
                    guard.IsWard,
                    true,
                    allMyTableCreatures);

                if (guardAttackingCreatures == null)
                {
                    notKillingGuards.Add(guard);
                }
                else
                {
                    attackTargets.Add(guardAttackingCreatures);
                    foreach (var ac in guardAttackingCreatures.MyCreatures)
                    {
                        allAttackingCreatures.Remove(ac);
                    }
                }
            }

            var notKillingGuard = notKillingGuards.FirstOrDefault();
            if (notKillingGuard != null && allAttackingCreatures.Any())
            {
                return attackTargets;
            }

            if (IsKillingOppHero(oppHeroHp, allAttackingCreatures, true))
            {
                var killHeroTradeResult = new TradeResult() { IsGoodTrade = true, MyDeadCreatures = new List<Card>() };
                foreach (var creature in allAttackingCreatures)
                {
                    killHeroTradeResult.MyCreatures.Add(creature);
                }
                attackTargets.Add(killHeroTradeResult);

                return attackTargets;
            }

            //идем в размен
            var leftCreatures = oppCreatures
                .Where(c => !c.IsGuard).ToList();

            var orderedOppCreatures = leftCreatures.Where(c => c.IsLethal).OrderByDescending(c => c.Defense + c.Attack)
                .ToList();//сначала убиваем летальщиков
            orderedOppCreatures.AddRange(leftCreatures.Where(c => !c.IsLethal).OrderByDescending(c => c.Defense + c.Attack));

            var isNecessaryToKill =
                IsKillingOppHero(myHeroHp, oppCreatures.Where(c => !c.IsGuard).ToList(), false);

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

                foreach (var ac in currAttackingCreatures.MyCreatures)
                {
                    allAttackingCreatures.Remove(ac);
                }
            }

            if (allAttackingCreatures.Any())
            {
                var attackHeroTradeResult = new TradeResult() {IsGoodTrade = false, MyDeadCreatures = new List<Card>()};
                foreach (var creature in allAttackingCreatures)
                {
                    attackHeroTradeResult.MyCreatures.Add(creature);
                }

                attackTargets.Add(attackHeroTradeResult);
            }

            return attackTargets;
        }

        #endregion

        #region USING_ITEMS

        static Card UpdateCreatureWithItem(Card creature, Card item)
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
            if (!creature.IsWard) creature.Defense += item.Defense;
            else if (item.Defense < 0)
            {
                strBulider = new StringBuilder(creature.Abilities) {[5] = '-'};
                creature.Abilities = strBulider.ToString();
            }

            return creature;
        }

        static Card GetGreenItemCreature(Card greenItem, IList<Card> allCreatures, IList<Card> summonningCreatures,
            int oppHeroHp, int myHeroHp, IList<TradeResult> noItemTradeResults, out IList<TradeResult> outBestTradeResults)
        {
            var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, summonningCreatures);
            var allTableCreatures = GetAllTableCreatures(allCreatures, summonningCreatures);

            IList<TradeResult> bestTradeResults = null;
            int cardIndex = -1;
            for (int i = 0; i < allAtackingCreatures.Count; ++i)
            {
                var creature = allAtackingCreatures[i];
                var newCreature = UpdateCreatureWithItem(new Card(creature), greenItem);
                allAtackingCreatures[i] = newCreature;

                var attackTargets = GetAttackTargets(allCreatures.Where(c => c.Location == -1).ToList(),
                    new List<Card>(allAtackingCreatures), allTableCreatures, oppHeroHp, myHeroHp);
                var isUsefulItem = noItemTradeResults == null ||
                                   CompareTradeResultLists(attackTargets, noItemTradeResults) < 0;
                if (isUsefulItem && (bestTradeResults == null || CompareTradeResultLists(attackTargets, bestTradeResults) < 0))
                {
                    bestTradeResults = attackTargets;
                    cardIndex = i;
                }

                allAtackingCreatures[i] = creature;
            }

            outBestTradeResults = bestTradeResults;

            if (cardIndex != -1) return allAtackingCreatures[cardIndex];
            if (noItemTradeResults != null) return null;
            //            if (allCreatures.Count(c => c.Location == -1) == 0) return null;//не качаем свое существо, если на столе нет врага

            Card weakestCreature = null;
            foreach (var sc in summonningCreatures)
            {
                if (weakestCreature == null ||
                    sc.Attack + sc.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    weakestCreature = sc;
            }

            return weakestCreature;
        }

        static IDictionary<Card, int> UseGreenItems(IList<Card> items, int manaLeft, IList<Card> allCreatures, IList<Card> summonningCreatures,
            int oppHeroHp, int myHeroHp, IList<TradeResult> noItemTradeResults)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                IList<TradeResult> outBestTradeResults = null;
                Card giCreature = GetGreenItemCreature(item, allCreatures, summonningCreatures, oppHeroHp, myHeroHp, noItemTradeResults, out outBestTradeResults);
                if (giCreature != null)
                {
                    itemTargets.Add(item, giCreature.InstanceId);
                    if (outBestTradeResults != null) noItemTradeResults = outBestTradeResults;
                }
            }

            return itemTargets;
        }

        static Card GetRedItemCreature(Card redItem, IList<Card> allCreatures, int oppHeroHp, int myHeroHp, IList<TradeResult> noItemTradeResults,
            IDictionary<Card, int> redItemsTragets)
        {
            var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, new List<Card>());
            var allTableCreatures = GetAllTableCreatures(allCreatures, new List<Card>());

            var oppCreatures = allCreatures.Where(c => c.Location == -1).OrderByDescending(c => c.Attack).ToList();

            IList<TradeResult> bestTradeResults = null;
            int cardIndex = -1;
            for (int i = 0; i < oppCreatures.Count; ++i)
            {
                var creature = oppCreatures[i];
                if (redItemsTragets.Values.Any(v => v == creature.InstanceId)) continue;//TODO: монжо наложить 2 красных шмотки на 1 существо
                if (redItem.CardNumber == 151) //убивающая всех карта
                {
                    var value = creature.Attack + creature.Defense;
                    if (creature.IsWard) value *= 2;
                    else if (creature.IsLethal) value *= 2;
                    if (value < 10) continue;
                }
                else if (redItem.CardNumber == 152) //топор -7
                {
                    if (creature.IsWard) continue;
                    if (creature.Defense < 5 || creature.Attack + creature.Defense < 10) continue;
                }

                var newCreature = UpdateCreatureWithItem(new Card(creature), redItem);

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
                    new List<Card>(allAtackingCreatures), allTableCreatures, oppHeroHp, myHeroHp);

                if (newCreature.Defense <= 0)
                {
                    attackTargets.Add(new TradeResult()
                    {
                        IsGoodTrade = true,
                        MyCreatures = new List<Card>(),
                        MyDeadCreatures = new List<Card>(),
                        OppCreature = creature
                    });
                }

                var isUsefulItem = CompareTradeResultLists(attackTargets, noItemTradeResults) < 0;
                if (isUsefulItem && (bestTradeResults == null || CompareTradeResultLists(attackTargets, bestTradeResults) < 0))
                {
                    bestTradeResults = attackTargets;
                    cardIndex = i;
                }

            }

            return cardIndex >= 0 ? oppCreatures[cardIndex] : null;
        }

        static IDictionary<Card, int> UseRedItems(IList<Card> items, int manaLeft, IList<Card> allCreatures, int oppHeroHp, int myHeroHp, IList<TradeResult> noItemTradeResults)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                var riCreature = GetRedItemCreature(item, allCreatures, oppHeroHp, myHeroHp, noItemTradeResults, itemTargets);
                if (riCreature != null) itemTargets.Add(item, riCreature.InstanceId);
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
                var biCreature = GetBlueItemCreature(allCreatures);
                if (biCreature != null) itemTargets.Add(item, biCreature.InstanceId);
                else itemTargets.Add(item, -1);
            }

            return itemTargets;
        }

        #endregion

    }
}
