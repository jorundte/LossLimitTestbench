using System;
using System.Collections.Generic;
using System.Linq;

namespace LossLimitTestbench
{
    public class Program
    {
        private static readonly Random Random = new Random();

        static void Main(string[] args)
        {
            Strategy strategy = Strategy.TakeFromBetLimitFirst;

            if (args != null && args.Length >= 1)
            {
                strategy = (Strategy) int.Parse(args[0]);
            }

            DateTime date = new DateTime(2021, 1, 1);

            var transactions = new List<BetLimitTransaction>();

            int totalAmountBlocked = 0;

            while (date <= new DateTime(2024, 1, 1))
            {
                // Only play every third day roughly
                if (date.DayOfYear % 3 != Random.Next(1, 3))
                {
                    date = date.AddDays(1);
                    continue;
                }

                int numBetsToBuy = Random.Next(0, 10);
                int amountPlayed = 0;
                int amountBlocked = 0;

                for (int betNumber = 0; betNumber < numBetsToBuy; betNumber++)
                {
                    // Play for anything from 50 to 2000 kroner
                    int amount = 50 * Random.Next(1, 40);

                    if (LossLimitEnforcer.IsAllowedToPlay(date, transactions, amount))
                    {
                        switch (strategy)
                        {
                            case Strategy.TakeFromBetLimitFirst:
                                LossLimitEnforcer.SubmitBetByAlwaysTakingFromBetLimitFirst(date, transactions, amount);
                                break;
                            case Strategy.TakeFromPrizesFirst:
                                LossLimitEnforcer.SubmitBetByAlwaysTakingFromPrizeLimitFirst(date, transactions, amount);
                                break;
                            case Strategy.TakeFromOldPrizesBeforeBetLimits:
                                LossLimitEnforcer.SubmitBetByTakingOldPrizesFirst(date, transactions, amount);
                                break;
                        }
                        amountPlayed += amount;
                    }
                    else
                    {
                        amountBlocked += amount;
                    }
                }

                totalAmountBlocked += amountBlocked;

                GeneratePrizes(date, transactions);

                var prizesWon = transactions.Where(t => t.ValidFor == date && t.TransactionType == BetTransactionType.Prize).Sum(t => t.Amount);

                var (betLimit, prizeLimit) = LossLimitEnforcer.GetLimits(date, transactions);

                Console.WriteLine($"{date:yyyy-MM-dd}.   Amount played: {amountPlayed,8}.   Amount blocked: {amountBlocked,8}.   Bet limit: {betLimit,8}.   Prize limit {prizeLimit,8}.   Prizes won {prizesWon,8}.");
                
                date = date.AddDays(1);
            }

            int totalAmountPlayed = transactions.Where(t => IsBet(t.TransactionType)).Sum(t => t.Amount);
            int totalAttempted = totalAmountPlayed + totalAmountBlocked;
            int totalBetAmount = transactions.Where(t => t.TransactionType == BetTransactionType.Bet).Sum(t => t.Amount);
            int totalPrizeBetAmount = transactions.Where(t => t.TransactionType == BetTransactionType.PrizeBet).Sum(t => t.Amount);
            int totalPrizeAmount = transactions.Where(t => t.TransactionType == BetTransactionType.Prize).Sum(t => t.Amount);

            string statusLine = $"Total attempted: {totalAttempted,8}.   Total amount played: {totalAmountPlayed,8} ({100.0f * totalAmountPlayed / totalAttempted:F2}%).   Total amount blocked: {totalAmountBlocked,8} ({100.0f * totalAmountBlocked / totalAttempted:F2}%).   Total bets: {totalBetAmount,8}.   Total prize bets: {totalPrizeBetAmount,8}.   Total prizes: {totalPrizeAmount,8}";
            string fancyLine = new string('-', statusLine.Length);
            
            Console.WriteLine();
            Console.WriteLine($"Used strategy: {strategy}");
            Console.WriteLine();
            Console.WriteLine(fancyLine);
            Console.WriteLine(statusLine);
            Console.WriteLine(fancyLine);
            Console.WriteLine();
        }

        private static void GeneratePrizes(DateTime date, IList<BetLimitTransaction> transactions)
        {
            var betsToday = transactions.Where(t => t.ValidFor == date && IsBet(t.TransactionType)).ToList();

            foreach (var bet in betsToday)
            {
                if (Random.Next(1, 10) == 7) // Lucky number
                {
                    // Every tenth bet wins from half to 17 times the prize amount back
                    transactions.Add(BetLimitTransaction.CreatePrize(date, (int) (bet.Amount * 0.5m * Random.Next(1, 34))));
                }
            }
        }

        private static bool IsBet(BetTransactionType transactionType)
        {
            return transactionType == BetTransactionType.Bet || transactionType == BetTransactionType.PrizeBet;
        }
    }
}
