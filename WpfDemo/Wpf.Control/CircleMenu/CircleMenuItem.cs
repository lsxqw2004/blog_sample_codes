namespace Wpf.Control.CircleMenu
{
    public class CircleMenuItem
    {
        public CircleMenuItem()
        {
        }

        public CircleMenuItem(int id, string title,double offsetRate)
        {
            Id = id;
            Title = title;
        }

        public int Id { get; set; }

        public string Title { get; set; }
    }
}
