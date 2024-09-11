using AvansasProject2.DAL.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AvansasProject2.UI.ViewComponents
{
    public class CategoryList : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public CategoryList(ApplicationDbContext db)
        {
            _db = db;
        }
        public IViewComponentResult Invoke()
        {
            var category = _db.Categories.ToList();
            return View(category);
        }
    }
}
