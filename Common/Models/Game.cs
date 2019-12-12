using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Common.Models
{
    public class Game
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
