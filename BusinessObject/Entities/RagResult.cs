using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Entities
{
    public class RagResult
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
    }

}
