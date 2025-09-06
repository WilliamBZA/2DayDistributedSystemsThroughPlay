namespace PuzzleController.Messages
{
    public class CustomerTotalIncreased
    {
        public decimal IncreaseAmount { get; set; }
        public decimal TotalAfterIncrease { get; set; }
    }
}