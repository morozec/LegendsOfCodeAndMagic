using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendsOfCodeAndMagic
{
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
            return new List<int>(){24};
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
                    var badCardIds = GetBadCardIds();
                    var pickedCardId = PickCard(allCards, manaCurve, handManaCuvre, badCardIds);
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
                    summonningCreatures);
                foreach (var it in greenItemsTargets)
                {
                    manaLeft -= it.Key.Cost;
                    var targetCreature = allCards.Single(c => c.InstanceId == it.Value);
                    targetCreature.Attack += it.Key.Attack;
                    targetCreature.Defense += it.Key.Defense;
                    resultStr += $"USE {it.Key.InstanceId} {it.Value};";
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

                var attackTargets = GetAttackTargets(allCards.Where(t => t.IsCreature).ToList(),
                    summonningCreatures,
                    oppPlayerData.PlayerHealth,
                    myPlayerData.PlayerHealth);

                var gotCards = new List<Card>();
                foreach (var at in attackTargets)//сначала сносим стенки
                {
                    var target = allCards.SingleOrDefault(c => c.InstanceId == at.Item2 && c.IsGuard);
                    if (target != null)
                    {
                        resultStr += $"ATTACK {at.Item1.InstanceId} {at.Item2};";
                        gotCards.Add(at.Item1);
                    }
                }
                foreach (var at in attackTargets.Where(x => !gotCards.Contains(x.Item1)))
                {
                    resultStr += $"ATTACK {at.Item1.InstanceId} {at.Item2};";
                }

                Console.WriteLine(resultStr);

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

            }
        }

        static int PickCard(IList<Card> cards, IDictionary<int, int> manaCurve, IDictionary<int, int> handManaCurve, IList<int> badCardIds)
        {
            var maxWeight = -double.MaxValue;
            int resCardIndex = -1;
            for (int i = 0; i < cards.Count; ++i)
            {
                var card = cards[i];

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

        static IList<Card> GetAllAttackingCreatures(IList<Card> allCards, IList<Card> summonningCards)
        {
            var attackingCards = new List<Card>();
            foreach (var card in allCards.Where(c => c.Location == 1 && c.Attack > 0))
            {
                attackingCards.Add(card);
            }

            foreach (var card in summonningCards.Where(c => c.Attack > 0))
            {
                if (card.IsCharge) attackingCards.Add(card);
            }

            return attackingCards;
        }

        static bool IsGoodTrade(Card myCreature, Card oppCreature, bool hasBetterTableCreatures)
        {
            if (myCreature.IsGuard) return false;
            if (myCreature.IsWard && !oppCreature.IsWard && myCreature.Attack >= oppCreature.Defense) return true;//мы со щитом убьем с 1 удара
            if (!myCreature.IsWard && !oppCreature.IsWard && myCreature.Attack >= oppCreature.Defense &&
                myCreature.Defense > oppCreature.Attack && !oppCreature.IsLethal) return true; //убьем с 1 удара и не помрем

            if (oppCreature.IsLethal && hasBetterTableCreatures) return true;

            return oppCreature.Attack > myCreature.Attack && myCreature.Attack - oppCreature.Defense <= 1;
            //return oppCreature.Attack > myCreature.Attack || oppCreature.Attack == myCreature.Attack && oppCreature.Defense > myCreature.Defense;
        }

        static bool IsKilling(Card sourceCreature, Card destCreature)
        {
            if (destCreature.IsWard) return false;
            if (sourceCreature.IsLethal) return true;
            return sourceCreature.Attack >= destCreature.Defense;
        }
        

        static IList<Card> GetCurrentTargetAttackingCreatures(Card targetCreature, IList<Card> allAtackingCreatures, IList<Card> usedCards,
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
                    if (lethalCreature != null) return new List<Card>() {lethalCreature};
                }
            }
            else //ищем самого слабого юнита, чтобы снять щит
            {
                //if (!isNecessaryToKill) return new List<Card>();//не занимаемся снятие щита с юнита, который нам не критичен

                var notLethalCreatures = allAtackingCreatures.Where(c => !c.IsLethal).ToList();

                Card breakWardCreature = null;
                if (notLethalCreatures.Any())
                {
                    //сначала берем своиз со щитами
                    breakWardCreature = notLethalCreatures.Where(c => c.IsWard).OrderBy(c => c.Attack + c.Defense)
                        .FirstOrDefault();
                }

                if (breakWardCreature == null)
                {
                    breakWardCreature = notLethalCreatures.Any()
                        ? notLethalCreatures
                            .OrderBy(c => c.Attack + c.Defense).First()
                        : allAtackingCreatures.OrderBy(c => c.Attack + c.Defense).FirstOrDefault();
                }

                if (breakWardCreature == null) return new List<Card>();

                var newUsedCards = new List<Card>() {breakWardCreature};
                var noWardAttackingCreatures =
                    GetCurrentTargetAttackingCreatures(targetCreature,
                        allAtackingCreatures,
                        newUsedCards,
                        hpLeft,
                        false,
                        isNecessaryToKill,
                        constAllAtackingCreatures);

                var resCards = new List<Card>() {breakWardCreature};
                resCards.AddRange(noWardAttackingCreatures);
                return resCards;
            }


            var minKillDamageCards = new List<Card>();
            if (hpLeft <= 0) return minKillDamageCards;

            Card notNecToKillCreature = null;

            foreach (var attackingCard in allAtackingCreatures)
            {
                if (usedCards.Contains(attackingCard)) continue;

                if (!isNecessaryToKill || attackingCard.IsGuard && !targetCreature.IsGuard)
                {
                    var hasBetterTableCreatures = constAllAtackingCreatures.Any(c =>
                        !c.IsWard && c.Attack + c.Defense > attackingCard.Attack + attackingCard.Defense);
                    var isGoodTrade = IsGoodTrade(attackingCard, targetCreature, hasBetterTableCreatures);
                    if (!isGoodTrade) continue;

                    if (attackingCard.Attack >= hpLeft || attackingCard.IsLethal)
                    {
                        if (notNecToKillCreature == null)
                        {
                            notNecToKillCreature = attackingCard;
                        }
                        else if (IsKilling(targetCreature, notNecToKillCreature) &&
                                 !IsKilling(targetCreature, attackingCard))
                        {
                            notNecToKillCreature = attackingCard;
                        }
                        else if (attackingCard.Attack < notNecToKillCreature.Attack)
                        {
                            notNecToKillCreature = attackingCard;
                        }
                    }
                    continue;
                }

                var newUsedCards = new List<Card>(usedCards) {attackingCard};
                var currMinKillDamageCards =
                    GetCurrentTargetAttackingCreatures(
                        targetCreature,
                        allAtackingCreatures,
                        newUsedCards,
                        hpLeft - attackingCard.Attack,
                        false,
                        true,
                        constAllAtackingCreatures);
                var currDamage = attackingCard.Attack + currMinKillDamageCards.Sum(c => c.Attack);
                if (currDamage >= hpLeft)
                {
                    if (!minKillDamageCards.Any() || currDamage < minKillDamageCards.Sum(c => c.Attack))
                    {
                        minKillDamageCards = new List<Card>() {attackingCard};
                        minKillDamageCards.AddRange(currMinKillDamageCards);
                    }
                }
            }

            if (notNecToKillCreature != null) return new List<Card>(){notNecToKillCreature};

            return minKillDamageCards;
        }

        static bool IsKillingOppHero(int oppHeroHp, IList<Card> attackingCreatures)
        {
            return attackingCreatures.Sum(c => c.Attack) >= oppHeroHp;
        }

        static IList<Tuple<Card, int>> GetAttackTargets(IList<Card> allCreatures, IList<Card> summonningCreatures, int oppHeroHp, int myHeroHp)
        {
            var attackTargets = new List<Tuple<Card, int>>();
            var allAttackingCreatures = GetAllAttackingCreatures(allCreatures, summonningCreatures);
            var constAllAtackingCreatures = new List<Card>(allAttackingCreatures);

            //foreach (var card in allAttackingCreatures)
            //    attackTargets.Add(card, -1);

            var oppGuards = new List<Card>();
            foreach (var card in allCreatures.Where(c => c.Location == -1))
            {
                if (card.IsGuard) oppGuards.Add(card);
            }

            //if (!oppGuards.Any()) return attackTargets;
            
            var orderedOppGuards = oppGuards.OrderByDescending(og => og.Defense).ToList();
            var notKillingGuards = new List<Card>();

            foreach (var guard in orderedOppGuards)
            {
                var guardAttackingCreatures = GetCurrentTargetAttackingCreatures(guard,
                    allAttackingCreatures,
                    new List<Card>(),
                    guard.Defense,
                    guard.IsWard,
                    true,
                    constAllAtackingCreatures);

                if (!guardAttackingCreatures.Any())
                {
                    notKillingGuards.Add(guard);
                }

                foreach (var ac in guardAttackingCreatures)
                {
                    attackTargets.Add(new Tuple<Card, int>(ac, guard.InstanceId));
                    allAttackingCreatures.Remove(ac);
                }
            }

            var notKillingGuard = notKillingGuards.FirstOrDefault();
            if (notKillingGuard != null && allAttackingCreatures.Any())
            {
                //foreach (var card in allAttackingCreatures)
                //{
                //    attackTargets.Add(new Tuple<Card, int>(card, notKillingGuard.InstanceId));
                //}

                return attackTargets;
            }
            
            if (IsKillingOppHero(oppHeroHp, allAttackingCreatures))
            {
                foreach (var card in allAttackingCreatures)
                {
                    attackTargets.Add(new Tuple<Card, int>(card, -1));
                }
                return attackTargets;
            }

            //идем в размен
            var orderedOppCreatures = allCreatures.Where(c => c.Location == -1 && !c.IsGuard).OrderByDescending(c => c.Attack + c.Defense).ToList();

            var isNecessaryToKill = IsKillingOppHero(myHeroHp, allCreatures.Where(c => c.Location == -1 && !c.IsGuard).ToList());
            Console.Error.WriteLine($"I CAN BE KILLED: {isNecessaryToKill}");

            foreach (var creature in orderedOppCreatures)
            {
                var currAttackingCreatures = GetCurrentTargetAttackingCreatures(creature, 
                    allAttackingCreatures,
                    new List<Card>(),
                    creature.Defense,
                    creature.IsWard,
                    isNecessaryToKill,
                    constAllAtackingCreatures);

                foreach (var ac in currAttackingCreatures)
                {
                    attackTargets.Add(new Tuple<Card, int>(ac, creature.InstanceId));
                    allAttackingCreatures.Remove(ac);
                }
            }

            foreach (var card in allAttackingCreatures)
            {
                attackTargets.Add(new Tuple<Card, int>(card, -1));
            }


            return attackTargets;
        }

        #endregion

        #region USING_ITEMS

        static Card GetGreenItemCreature(IList<Card> allCreatures, IList<Card> summonningCreatures)
        {
            Card weakestCreature = null;
            foreach (var c in allCreatures.Where(c => c.Location == 1))
            {
                if (weakestCreature == null)
                {
                    weakestCreature = c;
                }
                else
                {
                    if (c.Attack + c.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    {
                        weakestCreature = c;
                    }
                }
            }

            if (weakestCreature != null) return weakestCreature;

            foreach (var c in summonningCreatures.Where(c => c.IsCharge))
            {
                if (weakestCreature == null)
                {
                    weakestCreature = c;
                }
                else
                {
                    if (c.Attack + c.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    {
                        weakestCreature = c;
                    }
                }
            }
            if (weakestCreature != null) return weakestCreature;

            foreach (var c in summonningCreatures)
            {
                if (weakestCreature == null)
                {
                    weakestCreature = c;
                }
                else
                {
                    if (c.Attack + c.Defense < weakestCreature.Attack + weakestCreature.Defense)
                    {
                        weakestCreature = c;
                    }
                }
            }

            return weakestCreature;
        }

        static IDictionary<Card, int> UseGreenItems(IList<Card> items, int manaLeft, IList<Card> allCreatures, IList<Card> summonningCreatures)
        {
            var itemTargets = new Dictionary<Card, int>();
            var maxItems = GetMaxCards(items, new List<Card>(), manaLeft, int.MaxValue);

            foreach (var item in maxItems)
            {
                var giCreature = GetGreenItemCreature(allCreatures, summonningCreatures);
                if (giCreature != null) itemTargets.Add(item, giCreature.InstanceId);
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

                if (maxHpOppCreature == null || creature.Defense > maxHpOppCreature.Defense)
                {
                    maxHpOppCreature = creature;
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
