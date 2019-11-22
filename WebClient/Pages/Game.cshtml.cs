using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebClient.Pages
{
    public class GameModel : PageModel
    {
        private readonly ILogger<GameModel> _logger;
        private readonly GameContext _context;

        [BindProperty]
        public Game Game { get; set; }
        public GameModel(ILogger<GameModel> logger, GameContext context)
        {
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Games.Add(new Game
            {
                Id = Guid.NewGuid(),
                Name = Game.Name
            });
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}