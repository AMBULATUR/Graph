using System;
using System.Collections.Generic;
using System.Text;

namespace Graph.Abstract
{
    public class Serie //TODO struct if needed
    {
        public Serie(int id,string name)
        {
            this.Id = id;
            this.Name = name;
            Values = new List<object>();
        }
        public int Id { get; private set; }
        public string Name { get; private set; }
        public List<object> Values { get; private set; }
    }
}
