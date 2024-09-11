using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AvansasProject2.MODEL.Models
{
    public class ApplicationUser :IdentityUser
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Surname { get; set; } 
        public string Postcode { get; set; }    
        public string City { get; set; }
        public string Neighborhood { get; set; }
        public string Address { get; set; } 
        public string Role { get; set; }    



    }
}
