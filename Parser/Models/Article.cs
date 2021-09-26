using System.Collections.Generic;

namespace Parser.Models
{
    public class Article
    {
        public string Title { get; set; }
        public List<string> Hubs { get; set; }
        public List<string> Tags { get; set; }
        public string Content { get; set; }
    }
}