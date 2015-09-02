namespace Wpf.Control.Flow
{
    public class FlowItem
    {
        public FlowItem()
        {
        }

        public FlowItem(int id, string title,double offsetRate)
        {
            Id = id;
            Title = title;
            OffsetRate = offsetRate;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public double OffsetRate { get; set; }
    }
}
