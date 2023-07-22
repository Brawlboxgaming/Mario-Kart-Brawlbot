using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class ToolData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string Creators { get; set; }
        public string Description { get; set; }
        public string Download { get; set; }
    }
}