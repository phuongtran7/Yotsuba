namespace Yotsuba.Core.Models
{
    public class TaskModel
    {
        public int ID { get; set; }
        public int BoardID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string Category { get; set; }
    }
}
