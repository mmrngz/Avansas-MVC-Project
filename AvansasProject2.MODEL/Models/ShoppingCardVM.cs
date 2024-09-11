using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvansasProject2.MODEL.Models
{
    public class ShoppingCardVM
    {
        public IEnumerable<ShoppingCard> ListCard { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}
