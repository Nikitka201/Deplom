using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class User_entity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        
        public string Password { get; set; }

        public List <int> projectids { get; set; }
    }
}
