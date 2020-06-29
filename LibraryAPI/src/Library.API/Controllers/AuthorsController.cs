using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        public ILibraryRepository _libraryRepository { get; }
        public AuthorsController(ILibraryRepository LibraryRepository)
        {
            this._libraryRepository = LibraryRepository;
        }

        public IActionResult GetAuthors()
        {
            var authorsFromRepo = _libraryRepository.GetAuthors();
            var authors = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            return new JsonResult(authors);
        }
        [HttpGet("{id}", Name ="GetAuthor")]

        public IActionResult GetAuthor(Guid id) {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
                return NotFound();

            var author = AutoMapper.Mapper.Map<AuthorDto>(authorFromRepo);
            return Ok(author);
        }


        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();
            var authorEntity = AutoMapper.Mapper.Map<Author>(author);
            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save");
            }
            //if save is sucessful authorEntity will have Id generated,
            //so we need to populate AuthorDto with authorEntity
            var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);
            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpDelete("{id}")]

        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null) return NotFound();

            _libraryRepository.DeleteAuthor(authorFromRepo);

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Deleting author {id} failed on save");
            }
            return NoContent();
        }
    }
}