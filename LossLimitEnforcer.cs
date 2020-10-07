using System;
using System.Collections.Generic;
using System.Linq;

namespace LossLimitTestbench
{
    public class LossLimitEnforcer
    {
        private const int LimitPerMonth = 20_000;

        public static bool IsAllowedToPlay(DateTime date, IList<BetLimitTransaction> transactions, int amount)
        {
            var (betLimit, prizeLimit) = GetLimits(date, transactions);

            return amount < betLimit + prizeLimit;
        }

        public static (int betLimit, int prizeLimit) GetLimits(DateTime date, IList<BetLimitTransaction> transactions)
        {
            int betAmountFrom61To90 = transactions.Where(t => t.TransactionType == BetTransactionType.Bet && t.ValidFor >= date.AddDays(-90) && t.ValidFor < date.AddDays(-60)).Sum(t => t.Amount);
            int betAmountFrom31To60 = transactions.Where(t => t.TransactionType == BetTransactionType.Bet && t.ValidFor >= date.AddDays(-60) && t.ValidFor < date.AddDays(-30)).Sum(t => t.Amount);
            int betAmountFrom00To30 = transactions.Where(t => t.TransactionType == BetTransactionType.Bet && t.ValidFor >= date.AddDays(-30)).Sum(t => t.Amount);

            int betAmountToTransferFrom61To90 = betAmountFrom61To90 >= LimitPerMonth ? 0 : LimitPerMonth - betAmountFrom61To90;
            int betAmountToTransferFrom31To90 = betAmountFrom31To60 >= LimitPerMonth + betAmountToTransferFrom61To90 ? 0 : LimitPerMonth + betAmountToTransferFrom61To90 - betAmountFrom31To60;

            int betLimit = LimitPerMonth + betAmountToTransferFrom31To90 - betAmountFrom00To30;

            int prizes = transactions.Where(t => t.TransactionType == BetTransactionType.Prize && t.ValidFor >= date.AddDays(-365)).Sum(t => t.Amount);
            int prizeBets = transactions.Where(t => t.TransactionType == BetTransactionType.PrizeBet && t.ValidFor >= date.AddDays(-365)).Sum(t => t.Amount);

            return (betLimit, prizes - prizeBets);
        }

        public static void SubmitBetByAlwaysTakingFromBetLimitFirst(DateTime date, IList<BetLimitTransaction> transactions, int amount)
        {
            var (betLimit, _) = GetLimits(date, transactions);

            if (amount <= betLimit)
            {
                transactions.Add(BetLimitTransaction.CreateBet(date, amount));
                return;
            }
            if (betLimit > 0)
            {
                transactions.Add(BetLimitTransaction.CreateBet(date, betLimit));
            }

            var amountLeft = amount - betLimit;
            while (amountLeft > 0)
            {
                var (availableDate, availablePrizeBetAmount) = FindUnusedPrize(date, transactions);
                var prizeBetAmount = Math.Min(amountLeft, availablePrizeBetAmount);
                transactions.Add(BetLimitTransaction.CreatePrizeBet(date, prizeBetAmount, availableDate));
                amountLeft -= prizeBetAmount;
            }
        }

        public static void SubmitBetByAlwaysTakingFromPrizeLimitFirst(DateTime date, IList<BetLimitTransaction> transactions, int amount)
        {
            var amountLeft = amount;

            while (amountLeft > 0)
            {
                var (availableDate, availablePrizeBetAmount) = FindUnusedPrize(date, transactions);
                if (availablePrizeBetAmount == 0)
                    break;

                var prizeBetAmount = Math.Min(amountLeft, availablePrizeBetAmount);
                transactions.Add(BetLimitTransaction.CreatePrizeBet(date, prizeBetAmount, availableDate));
                amountLeft -= prizeBetAmount;
            }

            transactions.Add(BetLimitTransaction.CreateBet(date, amountLeft));
        }

        public static void SubmitBetByTakingOldPrizesFirst(DateTime date, IList<BetLimitTransaction> transactions, int amount)
        {
            var amountLeft = amount;

            var onlyOldPrizes = transactions.Where(t => t.ValidFor >= date.AddDays(-365) && t.ValidFor < date.AddDays(-365 + 89)).ToList();

            while (amountLeft > 0)
            {
                var (availableDate, availablePrizeBetAmount) = FindUnusedPrize(date, onlyOldPrizes);
                if (availablePrizeBetAmount == 0)
                    break;

                var prizeBetAmount = Math.Min(amountLeft, availablePrizeBetAmount);
                transactions.Add(BetLimitTransaction.CreatePrizeBet(date, prizeBetAmount, availableDate));
                amountLeft -= prizeBetAmount;
            }

            SubmitBetByAlwaysTakingFromBetLimitFirst(date, transactions, amount);
        }

        private static (DateTime availableDate, int availablePrizeBetAmount) FindUnusedPrize(DateTime date, IList<BetLimitTransaction> transactions)
        {
            var datesWithPrizes = transactions.Where(t => t.TransactionType == BetTransactionType.Prize && t.ValidFor >= date.AddDays(-365)).Select(t => t.ValidFor).Distinct().OrderBy(t => t.Date);
            foreach (var dateWithPrizes in datesWithPrizes)
            {
                var prizesOnDate = transactions.Where(t => t.ValidFor == dateWithPrizes && t.TransactionType == BetTransactionType.Prize).Sum(t => t.Amount);
                var prizeBetsOnDate = transactions.Where(t => t.ValidFor == dateWithPrizes && t.TransactionType == BetTransactionType.PrizeBet).Sum(t => t.Amount);

                if (prizesOnDate > prizeBetsOnDate)
                    return (dateWithPrizes, prizesOnDate - prizeBetsOnDate);
            }

            return (DateTime.MinValue, 0);
        }
    }
}
