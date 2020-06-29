using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public abstract class BookForManipulationDto
    {
        [Required(ErrorMessage = "You should fill out book title.")]
        [MaxLength(100, ErrorMessage = "Title can be maximum 50 characters long.")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "Description can be maximum 500 characters long.")]
        public virtual string Description { get; set; }
    }
}
