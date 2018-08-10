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

        public int MyDeadCreaturesNumber { get; set; }
        public bool IsGoodTrade { get; set; }
        public int MySumDamage
        {
            get { return MyCreatures.Sum(c => c.Attack); }
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

            var deadCreaturesDiff = result1.MyDeadCreaturesNumber - result2.MyDeadCreaturesNumber;
            if (deadCreaturesDiff != 0) return deadCreaturesDiff;//если убили больше моих в 1 случае, 2 варинат лучше

            var sumDamageDiff = result1.MySumDamage - result2.MySumDamage;
            return sumDamageDiff; //если нанесли больше урона в первом случае, 2 варант лучше (в 1 наносим лишний урон)
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
            return new List<int>(){57, 4};
        }

        static IList<int> GetGoodCardIds()
        {
            return new List<int>(){151};
        }

        static IDictionary<int, int> GetManaCurve()
        {
            return new Dictionary<int, int>() {{1, 3}, {2, 4}, {3, 5}, {4, 6}, {5, 5}, {6, 4}, {7, 3}};
        }

        static double GetCardWeight(Card card)
        {
            if (card.IsCreature && card.Attack == 0) return -double.MaxValue;
            if (card.IsGreenItem && card.Attack == 0) return -double.MaxValue;
            if (card.IsRedItem && card.Defense >= 0) return -double.MaxValue;


            var weight = 0d;

            if (card.IsLethal)
            {
                weight += card.Defense;
                if (card.IsWard) weight += card.Defense;
                weight += 1;
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


            weight += card.CardDraw;
            if (card.IsCreature) weight += 0.1;

            weight -= card.Cost;
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

                var redItemsTargets = UseRedItems(allCards.Where(c => c.IsRedItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList());

                foreach (var it in redItemsTargets)
                {
                    manaLeft -= it.Key.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == it.Value);
                    targetCreature.Attack += it.Key.Attack;
                    targetCreature.Defense += it.Key.Defense;
                    resultStr += $"USE {it.Key.InstanceId} {it.Value};";
                }

                var myCreaturesOnBoardCount = allCards.Count(c => c.IsCreature && c.Location == 1);
                var summonningCreatures = GetSummonningCreatures(
                    allCards.Where(c => c.IsCreature && c.Location == 0).ToList(),
                    manaLeft,
                    BOARD_SIZE - myCreaturesOnBoardCount);
                foreach (var card in summonningCreatures)
                {
                    manaLeft -= card.Cost;
                    resultStr += $"SUMMON {card.InstanceId};";
                }

                //TODO: есть смысл играть items раньше, чем creatures
                var greenItemsTargets = UseGreenItems(allCards.Where(c => c.IsGreenItem).ToList(),
                    manaLeft,
                    allCards.Where(c => c.IsCreature).ToList(),
                    summonningCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    redItemsTargets);
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

                var allCreatures = allCards.Where(t => t.IsCreature).ToList();
                var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, summonningCreatures);
                var attackTargets = GetAttackTargets(allCreatures,
                    allAtackingCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth,
                    redItemsTargets);

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
            if (tradeResults1.Any(x => x.IsGoodTrade && x.OppCreature == null)) return -1;//убьем героя врага
            if (tradeResults2.Any(x => x.IsGoodTrade && x.OppCreature == null)) return 1;//убьем героя врага

            var goodResultsDiff = tradeResults1.Count(x => x.IsGoodTrade) - tradeResults2.Count(x => x.IsGoodTrade);
            if (goodResultsDiff != 0) return -goodResultsDiff;

            var myDeadCreaturesDiff = tradeResults1.Sum(x => x.MyDeadCreaturesNumber) - tradeResults2.Sum(x => x.MyDeadCreaturesNumber);
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
            int resCardIndex = -1;
            for (int i = 0; i < cards.Count; ++i)
            {
                var card = cards[i];
                if (badCardIds.Contains(card.CardNumber)) continue;
                if (goodCardIds.Contains(card.CardNumber)) return i;

                var cardWeight = GetCardWeight(card);
                if (cardWeight > maxWeight)
                {
                    resCardIndex = i;
                    maxWeight = cardWeight;
                }

                else if (resCardIndex >= 0 && Math.Abs(cardWeight - maxWeight) < TOLERANCE)
                {
                    if (card.Attack > cards[resCardIndex].Attack)
                    {
                        resCardIndex = i;
                        maxWeight = cardWeight;
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
       
        static IList<Card> GetSummonningCreatures(IList<Card> myCreatures, int manaLeft, int boardPlaceLeft)
        {
            var maxCards = GetMaxCards(myCreatures, new List<Card>(), manaLeft, boardPlaceLeft);
            return maxCards;
        }

        private static IList<Card> GetMaxCards(IList<Card> cards, IList<Card> usedCards, int manaLeft, int boardPlaceLeft)
        {
            var maxCards = new List<Card>();
            if (boardPlaceLeft == 0) return maxCards;

            foreach (var card in cards)
            {
                if (usedCards.Contains(card)) continue;
                if (card.Cost > manaLeft) continue;

                var newUsedCards = new List<Card>(usedCards) {card};
                var currMaxCards = GetMaxCards(cards, newUsedCards, manaLeft - card.Cost, boardPlaceLeft - 1);

                if (card.Cost == 0 || card.Cost + currMaxCards.Sum(c => c.Cost) > maxCards.Sum(c => c.Cost))
                {
                    maxCards = new List<Card>(){card};
                    maxCards.AddRange(currMaxCards);
                }
                else if (card.Cost + currMaxCards.Sum(c => c.Cost) == maxCards.Sum(c => c.Cost))
                {
                    if (card.Attack + currMaxCards.Sum(c => c.Attack) > maxCards.Sum(c => c.Attack))
                    {
                        maxCards = new List<Card>() { card };
                        maxCards.AddRange(currMaxCards);
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

        static TradeResult GetTradeResult(IList<Card> myCreatures, Card oppCreature)
        {
            var myDeadCreatures = myCreatures.Where(c => IsKilling(oppCreature, c)).ToList();
            var isGoodTrade =  oppCreature.Attack + oppCreature.Defense >= myDeadCreatures.Sum(c => c.Attack + c.Defense);

            return new TradeResult()
            {
                IsGoodTrade = isGoodTrade,
                MyCreatures = myCreatures,
                OppCreature = oppCreature,
                MyDeadCreaturesNumber = myDeadCreatures.Count
            };
        }

        static bool IsKilling(Card attackingCreature, Card defendingCreature)
        {
            if (defendingCreature.IsWard) return false;
            if (attackingCreature.IsLethal) return true;
            return attackingCreature.Attack >= defendingCreature.Defense;
        }

        static bool IsKilling(IList<Card> attackingCreatures, Card defendingCreature)
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
            int hpLeft, bool hasWard, bool isNecessaryToKill, IList<Card> constAllAtackingCreatures)
        {
            if (!hasWard)
            {
                //ищем юнита, который убьет одним ударом
                //var killCreature = allAtackingCreatures.Where(c => !c.IsLethal).OrderBy(c => c.Attack + c.Defense)
                //    .FirstOrDefault(c => c.Attack >= hpLeft);
                //if (killCreature != null) return new List<Card>(){killCreature};

                if (isNecessaryToKill)
                {
                    //ищем юнита с ядом
                    var lethalCreature = allAtackingCreatures.Where(c => c.IsLethal).OrderBy(c => c.Attack + c.Defense)
                        .FirstOrDefault();
                    if (lethalCreature != null)
                        return new TradeResult()
                        {
                            IsGoodTrade = true,
                            MyCreatures = new List<Card>() {lethalCreature},
                            OppCreature = targetCreature,
                            MyDeadCreaturesNumber = IsKilling(targetCreature, lethalCreature) ? 1 : 0
                        };
                }
            }
            //else //ищем самого слабого юнита, чтобы снять щит
            //{
            //    //if (!isNecessaryToKill) return new List<Card>();//не занимаемся снятие щита с юнита, который нам не критичен

            //    var notLethalCreatures = allAtackingCreatures.Where(c => !c.IsLethal).ToList();

            //    Card breakWardCreature = null;
            //    if (notLethalCreatures.Any())
            //    {
            //        //сначала берем своих со щитами
            //        breakWardCreature = notLethalCreatures.Where(c => c.IsWard).OrderBy(c => c.Attack + c.Defense)
            //            .FirstOrDefault();
            //    }

            //    if (breakWardCreature == null)
            //    {
            //        breakWardCreature = notLethalCreatures.Any()
            //            ? notLethalCreatures
            //                .OrderBy(c => c.Attack + c.Defense).First()
            //            : allAtackingCreatures.OrderBy(c => c.Attack + c.Defense).FirstOrDefault();
            //    }

            //    if (breakWardCreature == null) return new List<Card>();

            //    var newUsedCards = new List<Card>() {breakWardCreature};
            //    var noWardAttackingCreatures =
            //        GetTargetCreatureTradeResult(targetCreature,
            //            allAtackingCreatures,
            //            newUsedCards,
            //            hpLeft,
            //            false,
            //            isNecessaryToKill,
            //            constAllAtackingCreatures);

            //    if (noWardAttackingCreatures.Any())
            //    {
            //        var resCards = new List<Card>() {breakWardCreature};
            //        resCards.AddRange(noWardAttackingCreatures);
            //        return resCards;
            //    }
            //    return new List<Card>();
            //}


            TradeResult bestTradeResult = null;
            //if (hpLeft <= 0) return new List<Card>();

            foreach (var attackingCard in allAtackingCreatures)
            {
                if (usedCards.Contains(attackingCard)) continue;

                var newUsedCards = new List<Card>(usedCards) {attackingCard};
                var isKilling = IsKilling(newUsedCards, targetCreature);

                TradeResult tradeResult;

                if (isKilling)
                {
                    tradeResult = GetTradeResult(newUsedCards, targetCreature);
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
                        constAllAtackingCreatures);
                }

                var resultComparison = TradeResult.GetResultComparison(tradeResult, bestTradeResult);
                if (resultComparison < 0) bestTradeResult = tradeResult;


                //var currDamage = attackingCard.Attack + currMinKillDamageCards.Sum(c => c.Attack);
                //if (currDamage >= hpLeft)//хватит урона, чтобы убить
                //{
                //    var tmpMinKillDamageCards = new List<Card>(){attackingCard};
                //    tmpMinKillDamageCards.AddRange(currMinKillDamageCards);

                //    var tradeResult = GetTradeResult(tmpMinKillDamageCards, targetCreature);
                //    if (isNecessaryToKill || tradeResult.IsGoodTrade) //важно убить сущ-во, либо это выгодный размен
                //    {
                //        var resultComparison = TradeResult.GetResultComparison(tradeResult, bestTradeResult);
                //        if (resultComparison < 0) bestTradeResult = tradeResult;
                //    }
                //}
            }

            return bestTradeResult;
        }


        static bool IsKillingOppHero(int oppHeroHp, IList<Card> attackingCreatures)
        {
            return attackingCreatures.Sum(c => c.Attack) >= oppHeroHp;
        }

        static IList<TradeResult> GetAttackTargets(IList<Card> allCreatures, IList<Card> allAttackingCreatures,
            int oppHeroHp, int myHeroHp, IDictionary<Card, int> redItemsTargets)
        {
            var attackTargets = new List<TradeResult>();
            var constAllAtackingCreatures = new List<Card>(allAttackingCreatures);

            var oppGuards = new List<Card>();
            foreach (var card in allCreatures.Where(c =>
                c.Location == -1 && !redItemsTargets.Values.Contains(c.InstanceId)))
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
                    constAllAtackingCreatures);

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

            if (IsKillingOppHero(oppHeroHp, allAttackingCreatures))
            {
                var killHeroTradeResult = new TradeResult() {IsGoodTrade = true, MyDeadCreaturesNumber = 0};
                foreach (var creature in allAttackingCreatures)
                {
                    killHeroTradeResult.MyCreatures.Add(creature);
                }
                attackTargets.Add(killHeroTradeResult);

                return attackTargets;
            }

            //идем в размен
            var leftCreatures = allCreatures
                .Where(c => c.Location == -1 && !c.IsGuard && !redItemsTargets.Values.Contains(c.InstanceId)).ToList();

            var orderedOppCreatures = leftCreatures.Where(c => c.IsLethal).OrderByDescending(c => c.Defense + c.Attack)
                .ToList();//сначала убиваем летальщиков
            orderedOppCreatures.AddRange(leftCreatures.Where(c => !c.IsLethal).OrderByDescending(c => c.Defense + c.Attack));

            var isNecessaryToKill =
                IsKillingOppHero(myHeroHp, allCreatures.Where(c => c.Location == -1 && !c.IsGuard).ToList());
            Console.Error.WriteLine($"I CAN BE KILLED: {isNecessaryToKill}");

            foreach (var creature in orderedOppCreatures)
            {
                var currAttackingCreatures = GetTargetCreatureTradeResult(creature,
                    allAttackingCreatures,
                    new List<Card>(),
                    creature.Defense,
                    creature.IsWard,
                    isNecessaryToKill,
                    constAllAtackingCreatures);

                if (currAttackingCreatures == null) continue;
                if (!currAttackingCreatures.IsGoodTrade && !isNecessaryToKill) continue;

                attackTargets.Add(currAttackingCreatures);

                foreach (var ac in currAttackingCreatures.MyCreatures)
                {
                    allAttackingCreatures.Remove(ac);
                }
            }

            var attackHeroTradeResult = new TradeResult() {IsGoodTrade = false, MyDeadCreaturesNumber = 0};
            foreach (var creature in allAttackingCreatures)
            {
                attackHeroTradeResult.MyCreatures.Add(creature);
            }
            attackTargets.Add(attackHeroTradeResult);

            return attackTargets;
        }

        #endregion

        #region USING_ITEMS

        static Card UpdateCreatureWithItem(Card creature, Card item)
        {
            creature.Attack += item.Attack;
            creature.Defense += item.Defense;

            var strBulider = new StringBuilder(creature.Abilities);
            for (int i = 0; i < strBulider.Length; ++i)
            {
                if (strBulider[i] == '-') strBulider[i] = item.Abilities[i];
            }

            creature.Abilities = strBulider.ToString();
            return creature;
        }

        static Card GetGreenItemCreature(Card greenItem, IList<Card> allCreatures, IList<Card> summonningCreatures,
            int oppHeroHp, int myHeroHp, IDictionary<Card, int> redItemsTargets)
        {
            var allAtackingCreatures = GetAllAttackingCreatures(allCreatures, summonningCreatures);

            IList<TradeResult> bestTradeResults = null;
            int cardIndex = -1;
            for (int i = 0; i < allAtackingCreatures.Count; ++i)
            {
                var creature = allAtackingCreatures[i];
                var newCreature = UpdateCreatureWithItem(new Card(creature), greenItem);
                allAtackingCreatures[i] = newCreature;

                var attackTargets = GetAttackTargets(allCreatures, new List<Card>(allAtackingCreatures),oppHeroHp, myHeroHp, redItemsTargets);
                if (bestTradeResults == null || CompareTradeResultLists(attackTargets, bestTradeResults) < 0)
                {
                    bestTradeResults = attackTargets;
                    cardIndex = i;
                }

                allAtackingCreatures[i] = creature;
            }

            if (cardIndex != -1) return allAtackingCreatures[cardIndex];

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
            int oppHeroHp, int myHeroHp, IDictionary<Card, int> redItemsTargets)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                Card giCreature = GetGreenItemCreature(item, allCreatures, summonningCreatures,oppHeroHp, myHeroHp, redItemsTargets);
                if (giCreature != null)
                {
                    itemTargets.Add(item, giCreature.InstanceId);
                }
            }

            return itemTargets;
        }

        static Card GetRedItemCreature(Card redItem, IList<Card> allCreatures)
        {
            var oppCreatures = allCreatures.Where(c => c.Location == -1).OrderByDescending(c => c.Attack).ToList();

            Card maxHpOppCreature = null;
            foreach (var creature in oppCreatures)
            {
                if (Math.Abs(redItem.Defense) < creature.Defense) continue;//TODO: можно наносить часть урона
                if (creature.IsWard && redItem.Abilities != "BCDGLW") continue; //TODO: абилки врага - это не только щит

                if (redItem.Abilities == "BCDGLW")
                {
                    if (creature.Defense > 5 || creature.IsWard && creature.Defense + creature.Attack >= 5)
                    {
                        if (maxHpOppCreature == null || creature.Defense > maxHpOppCreature.Defense)
                        {
                            maxHpOppCreature = creature;
                        }
                    }
                }
                else
                {
                    if (maxHpOppCreature == null || creature.Defense > maxHpOppCreature.Defense)
                    {
                        maxHpOppCreature = creature;
                    }
                }
            }

            return maxHpOppCreature;
        }

        static IDictionary<Card, int> UseRedItems(IList<Card> items, int manaLeft, IList<Card> allCreatures)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                var riCreature = GetRedItemCreature(item, allCreatures);
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
