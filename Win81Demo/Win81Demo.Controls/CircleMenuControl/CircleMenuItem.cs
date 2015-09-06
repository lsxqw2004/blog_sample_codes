namespace Win81Demo.Controls.CircleMenuControl
{
    public class CircleMenuItem
    {
        public CircleMenuItem()
        {
        }

        public CircleMenuItem(int id, string title)
        {
            Id = id;
            Title = title;

        }

        public int Id { get; set; }

        public string Title { get; set; }

    }
}
