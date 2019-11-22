using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebClient.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GameContext _context;

        public List<Game> Games { get; set; }

        public IndexModel(ILogger<IndexModel> logger, GameContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            this.Games = new List<Game>();
            this.Games.AddRange(_context.Games.ToList());
            this.Games.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }
}
