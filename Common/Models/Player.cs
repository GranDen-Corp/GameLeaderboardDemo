using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Models
{
    public class Player
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
