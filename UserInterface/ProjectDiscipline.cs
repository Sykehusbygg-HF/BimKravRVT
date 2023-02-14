using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimkravRvt.Utils;

namespace BimkravRvt.UserInterface
{
    public class ProjectDiscipline : NotifierBase
    {
        private string name;
        public string Name { get => name; set => SetNotify(ref name, value); }
        private string tag;
        public string Tag { get => tag; set => SetNotify(ref tag, value); }
        public override string ToString() 
        { 
            return $"{Tag} - {Name}";
        }
        public ProjectDiscipline(string name, string tag)
        {
            Name = name;
            Tag = tag;
        }
    }
}
