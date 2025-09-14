using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class BookController : BaseController
    {
        public BookController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var books = await _context.Books
                .Include(b => b.Company)
                .Where(b => b.CompanyId == companyId)
                .OrderBy(b => b.BookTitle)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.Categories = await _context.Books
                .Where(b => b.CompanyId == companyId && !string.IsNullOrEmpty(b.Category))
                .Select(b => b.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Book Management";
            return View(books);
        }

        [HttpGet]
        public async Task<IActionResult> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Company)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            return Json(new
            {
                bookId = book.BookId,
                companyId = book.CompanyId,
                bookCode = book.BookCode,
                bookTitle = book.BookTitle,
                author = book.Author,
                publisher = book.Publisher,
                isbn = book.ISBN,
                category = book.Category,
                description = book.Description,
                isActive = book.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookViewModel model)
        {
            try
            {
                // Check if book code already exists in the same company
                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => b.BookCode == model.BookCode && b.CompanyId == model.CompanyId);
                if (existingBook != null)
                {
                    return Json(new { success = false, message = "Book code already exists in this company." });
                }

                // Check if ISBN already exists (if provided)
                if (!string.IsNullOrEmpty(model.ISBN))
                {
                    var existingISBN = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == model.ISBN && b.CompanyId == model.CompanyId);
                    if (existingISBN != null)
                    {
                        return Json(new { success = false, message = "ISBN already exists in this company." });
                    }
                }

                var book = new Book
                {
                    CompanyId = model.CompanyId,
                    BookCode = model.BookCode,
                    BookTitle = model.BookTitle,
                    Author = model.Author,
                    Publisher = model.Publisher,
                    ISBN = model.ISBN,
                    Category = model.Category,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Book created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating book: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(BookViewModel model)
        {
            try
            {
                var book = await _context.Books.FindAsync(model.BookId);
                if (book == null)
                {
                    return Json(new { success = false, message = "Book not found." });
                }

                // Check if book code already exists in the same company (excluding current book)
                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => b.BookCode == model.BookCode && b.CompanyId == model.CompanyId && b.BookId != model.BookId);
                if (existingBook != null)
                {
                    return Json(new { success = false, message = "Book code already exists in this company." });
                }

                // Check if ISBN already exists (if provided, excluding current book)
                if (!string.IsNullOrEmpty(model.ISBN))
                {
                    var existingISBN = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == model.ISBN && b.CompanyId == model.CompanyId && b.BookId != model.BookId);
                    if (existingISBN != null)
                    {
                        return Json(new { success = false, message = "ISBN already exists in this company." });
                    }
                }

                book.CompanyId = model.CompanyId;
                book.BookCode = model.BookCode;
                book.BookTitle = model.BookTitle;
                book.Author = model.Author;
                book.Publisher = model.Publisher;
                book.ISBN = model.ISBN;
                book.Category = model.Category;
                book.Description = model.Description;
                book.IsActive = model.IsActive;
                book.UpdatedBy = GetCurrentUserId();
                book.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Book updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating book: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                {
                    return Json(new { success = false, message = "Book not found." });
                }

                // Check if book has grade assignments
                var hasGradeBooks = await _context.GradeBooks.AnyAsync(gb => gb.BookId == id);
                if (hasGradeBooks)
                {
                    return Json(new { success = false, message = "Cannot delete book that is assigned to grades." });
                }

                // Check if book has inventory records
                var hasInventory = await _context.Inventories.AnyAsync(i => i.BookId == id);
                if (hasInventory)
                {
                    return Json(new { success = false, message = "Cannot delete book that has inventory records." });
                }

                // Check if book has stock movements
                var hasStockMovements = await _context.StockMovements.AnyAsync(sm => sm.BookId == id);
                if (hasStockMovements)
                {
                    return Json(new { success = false, message = "Cannot delete book that has stock movement history." });
                }

                // Check if book has requisition details
                var hasRequisitions = await _context.BookRequisitionDetails.AnyAsync(brd => brd.BookId == id);
                if (hasRequisitions)
                {
                    return Json(new { success = false, message = "Cannot delete book that has requisition records." });
                }

                // Check if book has prices
                var hasPrices = await _context.BookPrices.AnyAsync(bp => bp.BookId == id);
                if (hasPrices)
                {
                    return Json(new { success = false, message = "Cannot delete book that has price records." });
                }

                // Soft delete - set IsActive to false
                book.IsActive = false;
                book.UpdatedBy = GetCurrentUserId();
                book.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Book deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting book: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompaniesByUser()
        {
            var userLevel = GetCurrentUserLevel();
            var companyId = GetCurrentCompanyId();

            var companies = userLevel == "SUPER"
                ? await _context.Companies
                    .Where(c => c.IsActive)
                    .Select(c => new { value = c.CompanyId, text = c.CompanyName })
                    .OrderBy(c => c.text)
                    .ToListAsync()
                : await _context.Companies
                    .Where(c => c.CompanyId == companyId && c.IsActive)
                    .Select(c => new { value = c.CompanyId, text = c.CompanyName })
                    .OrderBy(c => c.text)
                    .ToListAsync();

            return Json(companies);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesByCompany(int companyId)
        {
            var categories = await _context.Books
                .Where(b => b.CompanyId == companyId && !string.IsNullOrEmpty(b.Category))
                .Select(b => b.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Json(categories);
        }
    }
}
