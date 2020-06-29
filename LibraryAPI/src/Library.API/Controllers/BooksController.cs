using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILogger<BooksController> _logger;
        private ILibraryRepository _libraryRepository;
        
        //Here ILogger<T> - logger will automatically create logger with category name as type name
        public BooksController(ILibraryRepository libraryRepository, 
            ILogger<BooksController> logger)
        {
            this._logger = logger;
            _libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var booksForAuthor = AutoMapper.Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);
            return Ok(booksForAuthor);
        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null) return NotFound();

            var bookForAuthor = AutoMapper.Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(bookForAuthor);
        }


        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId,
            [FromBody] BookForCreationDto book)
        {
            if (book == null)//if book was properly deserialized
                return BadRequest();

            /***********Validation using model state****************/
            //custom rule to check "title!= description"

            if(book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "Title and descriptiom have to be different");
            }

            if (!ModelState.IsValid)
            {
                //return 422 i.e, Unprocessableentity 
                return new UnprocesableEntityObjectResult(ModelState);
            }
            /***************************/

            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var bookEntity = AutoMapper.Mapper.Map<Book>(book);
            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"creating book for author {authorId } failed on save");
            }

            var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookEntity);
            return CreatedAtRoute("GetBookForAuthor",
                new { authorId, id = bookToReturn.Id },
                bookToReturn);

        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound("Author not found");
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null) return NotFound("Book For Author not found");
            _libraryRepository.DeleteBook(bookForAuthorFromRepo);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Deleting book {id} for author {authorId} failed on save");
            }
            this._logger.LogInformation(100, $"Book {id} for author {authorId} has been deleted");

            return NoContent();
        }



        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] BookForUpdateDto book)
        {
            if (book == null) return BadRequest();

            if (book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "Title and descriptiom have to be different");
            }
            if (!ModelState.IsValid)
            {
                return new UnprocesableEntityObjectResult(ModelState);
            }

            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                //   return NotFound("Book For Author not found");
                #region "Upserting"
                var bookToAdd = AutoMapper.Mapper.Map<Book>(book);
                bookToAdd.Id = id;
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting  book {id} for author {authorId} failed on save");
                }
                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId, id = bookToReturn.Id }, bookToReturn);
                #endregion 
            }
           

            //we need to copy values from [FromBody] book to the book record in DB represented by bookForAuthorFromRepo
            AutoMapper.Mapper.Map(book, bookForAuthorFromRepo);

            //UpdateBookForAuthor is an empyty method and does nothing, 
            //bcoz in EF Core entities are tracked by Context. We call Maper.map(source, destn)
            //which copies new values from dto into actual entity
            //then we call repository.save() which sends changes to DB so below method can be skipped
            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Updating boook {id} for author {authorId} failed on save");
            }
            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null) return BadRequest();

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound("Author not found");

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
                return NotFound("Book For Author not found");

            var bookToPatch = AutoMapper.Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);
        //    patchDoc.ApplyTo(bookToPatch, ModelState);
            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Title == bookToPatch.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),"Title and descriptiom have to be different");
            }
            //This is important while updating. Since Description is required in BokForUpdateDto
            /* [
                    {
                    "op": "remove",
                    "path": "/description" 
                    } 
                ]*/
            //ModelState is valid even if descriptionis null bcoz inputted model is not BookForUpdateDto
            //Its jsonPatch doc sowe need to manually cross chk that
            //So we do TryUpdateModel
            TryUpdateModelAsync(bookToPatch);
            if (!ModelState.IsValid)
            {
                return new UnprocesableEntityObjectResult(ModelState);
            }

            //valiation of patchDoc ...pending....
            //now we need to do same as incase of PUT

            AutoMapper.Mapper.Map(bookToPatch, bookForAuthorFromRepo);
            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Patching book {id} for author {authorId} failed on save");
            }
            return NoContent();
        }

    }
}
