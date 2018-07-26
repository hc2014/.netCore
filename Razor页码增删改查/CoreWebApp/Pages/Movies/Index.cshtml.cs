using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CoreWebApp.Pages.Movies
{
    public class IndexModel : PageModel
    {

        private readonly MovieContext _context;

        public IndexModel(MovieContext context)
        {
            _context = context;
            if (_context.MovieList.Count() == 0)
            {
                _context.MovieList.AddRange(
                    new Movie { Genre = "张三", ID = 1, Price = 10.5m, ReleaseDate = DateTime.Now, Title = "张三的歌" },
                    new Movie { Genre = "李四", ID = 2, Price = 10.5m, ReleaseDate = DateTime.Now, Title = "李四的故事" }
                    );
                _context.SaveChanges();
            }
        }

        public IList<Movie> Movie { get; set; }

        public async Task OnGetAsync()
        {
            Movie = await _context.MovieList.ToListAsync();
        }

        //public void OnGet()
        //{

        //}
    }
}