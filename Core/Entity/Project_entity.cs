using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Project_entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List <int> Cardcounts { get; set; }
        public List <int> CardIds { get; set; }

        public int Ownerid { get; set; }

        public List <int> AddedUserIds { get; set; } 
    }
}
