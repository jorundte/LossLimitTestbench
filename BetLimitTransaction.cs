using System;

namespace LossLimitTestbench
{
    public class BetLimitTransaction
    {
        public DateTime EntryTime { get; protected set; }

        public BetTransactionType TransactionType { get; protected set; }

        public int Amount { get; protected set; }

        public DateTime ValidFor { get; protected set; }

        public static BetLimitTransaction CreateBet(DateTime date, int amount)
        {
            return new BetLimitTransaction
            {
                EntryTime = date,
                TransactionType = BetTransactionType.Bet,
                Amount = amount,
                ValidFor = date
            };
        }

        public static BetLimitTransaction CreatePrize(DateTime date, int amount)
        {
            return new BetLimitTransaction
            {
                EntryTime = date,
                TransactionType = BetTransactionType.Prize,
                Amount = amount,
                ValidFor = date
            };
        }

        public static BetLimitTransaction CreatePrizeBet(DateTime date, int amount, DateTime validFor)
        {
            return new BetLimitTransaction
            {
                EntryTime = date,
                TransactionType = BetTransactionType.PrizeBet,
                Amount = amount,
                ValidFor = validFor
            };
        }
    }
}
